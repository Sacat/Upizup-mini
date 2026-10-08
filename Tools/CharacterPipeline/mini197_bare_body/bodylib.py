import bpy,bmesh,numpy as np,colorsys
def load(fbx):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=fbx,use_anim=False)
    arm=[o for o in bpy.context.scene.objects if o.type=='ARMATURE'][0]
    body=[o for o in bpy.context.scene.objects if o.type=='MESH'][0]
    arm.data.pose_position='REST'
    return arm,body
def weld(body,dist=0.01):
    bm=bmesh.new(); bm.from_mesh(body.data)
    n0=len(bm.verts); bmesh.ops.remove_doubles(bm,verts=bm.verts,dist=dist)
    bm.to_mesh(body.data); bm.free(); body.data.update()
    return n0,len(body.data.vertices)
def world_co(body):
    me=body.data; co=np.zeros(len(me.vertices)*3); me.vertices.foreach_get('co',co); co=co.reshape(-1,3)
    M=np.array(body.matrix_world); return co@M[:3,:3].T+M[:3,3]
def set_world_co(body,W):
    M=np.array(body.matrix_world); Mi=np.linalg.inv(M); L=W@Mi[:3,:3].T+Mi[:3,3]
    body.data.vertices.foreach_set('co',L.ravel()); body.data.update()
def tex_pixels(path):
    img=bpy.data.images.load(path); w,h=img.size
    px=np.array(img.pixels[:]).reshape(h,w,4)[:,:,:3]; return px
def vert_colors(body,px):
    me=body.data; h,w,_=px.shape
    uv=np.zeros(len(me.loops)*2); me.uv_layers.active.data.foreach_get('uv',uv); uv=uv.reshape(-1,2)
    vi=np.zeros(len(me.loops),dtype=int); me.loops.foreach_get('vertex_index',vi)
    x=np.clip((uv[:,0]%1)*w,0,w-1).astype(int); y=np.clip((uv[:,1]%1)*h,0,h-1).astype(int)
    lc=px[y,x]; n=len(me.vertices)
    acc=np.zeros((n,3)); cnt=np.zeros(n); np.add.at(acc,vi,lc); np.add.at(cnt,vi,1)
    return acc/np.maximum(cnt,1)[:,None], lc, vi
def adjacency(body):
    import scipy.sparse as sp
    me=body.data; e=np.zeros(len(me.edges)*2,dtype=int); me.edges.foreach_get('vertices',e); e=e.reshape(-1,2)
    n=len(me.vertices); A=sp.coo_matrix((np.ones(len(e)*2),(np.r_[e[:,0],e[:,1]],np.r_[e[:,1],e[:,0]])),shape=(n,n)).tocsr()
    A.data[:]=1; return A
def hsv(c):
    mx=c.max(1); mn=c.min(1); s=np.where(mx>1e-4,(mx-mn)/np.maximum(mx,1e-4),0); return s,mx
def dilate(A,mask,k):
    m=mask.astype(float)
    for _ in range(k): m=np.maximum(m,(A@m>0).astype(float))
    return m>0
def taubin(W,A,region,iters=30,lam=0.5,mu=-0.53,weight=None):
    import scipy.sparse as sp
    deg=np.asarray(A.sum(1)).ravel(); Dinv=sp.diags(1/np.maximum(deg,1))
    P=Dinv@A; W=W.copy(); r=region.astype(float)[:,None]
    if weight is not None: r=r*weight[:,None]
    for _ in range(iters):
        for f in (lam,mu): W=W+f*r*(P@W-W)
    return W
def harmonic_fill(A,mask,values):
    import scipy.sparse as sp, scipy.sparse.linalg as la
    idx=np.where(mask)[0]; bnd=np.where(~mask)[0]
    deg=np.asarray(A.sum(1)).ravel(); L=sp.diags(deg)-A
    eps=1e-4; Lrr=(L[idx][:,idx]+eps*sp.identity(len(idx))).tocsc(); Lrb=L[idx][:,bnd]
    mean=values[bnd].mean(0)  # isolated islands with no skin neighbour fall back to mean skin
    out=values.copy(); rhs=-(Lrb@values[bnd])+eps*mean[None,:]
    solve=la.factorized(Lrr)
    rhs=np.asarray(rhs)
    for k in range(values.shape[1]): out[idx,k]=solve(rhs[:,k].ravel())
    return out
def close_mask(A,m,k=2):
    d=dilate(A,m,k); return ~dilate(A,~d,k)
def ring_distance(A,mask,maxk=12):
    # rings from boundary inward: 0 outside, 1..maxk inside
    d=np.zeros(len(mask)); cur=mask.copy()
    for k in range(1,maxk+1):
        d[cur]=k; cur=cur & ~dilate(A,~cur,1)
        if not cur.any(): break
    return d
def biharmonic_fill(W,A,region):
    """Replace positions inside region by the smoothest (bi-Laplacian) surface matching the 2 boundary rings."""
    import scipy.sparse as sp, scipy.sparse.linalg as la
    n=len(W); deg=np.asarray(A.sum(1)).ravel(); L=sp.diags(1/np.maximum(deg,1))@(sp.diags(deg)-A)
    L2=(L@L).tocsr(); idx=np.where(region)[0]; bnd=np.where(~region)[0]
    M=L2[idx][:,idx].tocsc()+1e-8*sp.identity(len(idx)); rhs=-(L2[idx][:,bnd]@W[bnd])
    solve=la.factorized(M.tocsc()); out=W.copy()
    for k in range(3): out[idx,k]=solve(np.asarray(rhs[:,k]).ravel())
    return out
def vertex_normals(body,W):
    me=body.data; n=np.zeros(len(me.vertices)*3); 
    tri=np.array([(p.vertices[0],p.vertices[i],p.vertices[i+1]) for p in me.polygons for i in range(1,len(p.vertices)-1)])
    a,b,c=W[tri[:,0]],W[tri[:,1]],W[tri[:,2]]; fn=np.cross(b-a,c-a)
    N=np.zeros_like(W)
    for k in range(3): np.add.at(N,tri[:,k],fn)
    return N/np.maximum(np.linalg.norm(N,axis=1),1e-9)[:,None]
def srgb2lin(c):
    c=np.clip(c,0,1); return np.where(c<=0.04045,c/12.92,((c+0.055)/1.055)**2.4)
def mesh_from_world(name,W,polys):
    me=bpy.data.meshes.new(name); me.from_pydata(W.tolist(),[],polys); o=bpy.data.objects.new(name,me)
    bpy.context.scene.collection.objects.link(o); return o
def activate(o):
    for x in bpy.context.view_layer.objects: x.select_set(False)
    bpy.context.view_layer.objects.active=o; o.select_set(True)
def tri_polys(me):
    return [p.vertices[:] for p in me.polygons]
def bone_world(arm,name,body):
    # the donor mesh's local space IS the armature's local space (Z-up, cm); work in body.matrix_world (x0.01)
    b=arm.data.bones[name]; return np.array(body.matrix_world@b.head_local)
def radial_bridge(W,p0,p1,side,band,refs,nbins=48,rmax=0.13):
    """Rebuild a limb's radius in the parameter band [t0,t1] (fraction of p0->p1) by fitting, per angle bin,
    a smooth quadratic r(t) through the reference ranges on both sides. Returns new W and the band weight."""
    u=p1-p0; L=np.linalg.norm(u); u=u/L
    ref=np.array([1.0,0,0]); e1=ref-u*ref.dot(u); e1/=np.linalg.norm(e1); e2=np.cross(u,e1)
    d=W-p0; t=d@u/L; radial=d-np.outer(d@u,u); r=np.linalg.norm(radial,axis=1)
    th=np.arctan2(radial@e2,radial@e1)
    sel=(np.sign(W[:,0])==side)&(r<rmax)&(t>min(refs[0][0],band[0])-0.02)&(t<max(refs[-1][1],band[1])+0.02)
    inref=np.zeros(len(W),bool)
    for a,b in refs: inref|=(t>=a)&(t<=b)
    bins=((th+np.pi)/(2*np.pi)*nbins).astype(int)%nbins
    coef=np.zeros((nbins,3))
    for k in range(nbins):
        m=sel&inref&((bins==k)|(bins==(k+1)%nbins)|(bins==(k-1)%nbins))
        if m.sum()<6: coef[k]=np.nan; continue
        coef[k]=np.polyfit(t[m],r[m],2)
    # fill/average bins circularly
    for k in range(nbins):
        if np.isnan(coef[k]).any(): coef[k]=np.nanmean(coef,0)
    c=coef
    for _ in range(3): c=(np.roll(c,1,0)+2*c+np.roll(c,-1,0))/4
    f=(th+np.pi)/(2*np.pi)*nbins-0.5; k0=np.floor(f).astype(int); a=(f-k0)[:,None]
    ci=(1-a)*c[k0%nbins]+a*c[(k0+1)%nbins]          # continuous in angle: no bin steps
    rfit=np.einsum('ij,ij->i',ci,np.c_[t**2,t,np.ones_like(t)])
    # weight: 1 inside band, smooth falloff 0.04 outside
    w=np.clip(np.minimum(t-band[0]+0.04,band[1]+0.04-t)/0.04,0,1); w=w*w*(3-2*w); w=np.where(sel,w,0)
    rn=r+(rfit-r)*w
    Wn=W+radial*((rn/np.maximum(r,1e-6)-1))[:,None]
    return Wn,w>0

def laplacian(W,A,region,iters=10,lam=0.5,weight=None):
    import scipy.sparse as sp
    deg=np.asarray(A.sum(1)).ravel(); P=sp.diags(1/np.maximum(deg,1))@A
    r=region.astype(float)[:,None]
    if weight is not None: r=r*weight[:,None]
    for _ in range(iters): W=W+lam*r*(P@W-W)
    return W
