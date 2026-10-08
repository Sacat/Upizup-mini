"""MINI-197: give Franki his own face on the shared bare body.

Franki's original model (Assets/UpIzUpMini/Art/Characters/Strong.fbx, Mixamo Ch28) supplies the head from the
jaw/upper neck up (its body mesh stops under the hoodie at ~1.52 m). It is moved so its Head joint lands on the
bare rig's CC_Base_Head joint (+0.099 m: Franki's old body was shorter); at the join (~1.635 m on the bare body)
the two necks match (perimeter 0.49 vs 0.50 m). The head geometry goes through the same voxel remesh as the body
(one closed surface, invisible neck seam) and its Ch28 texture is baked into the shared atlas.
Hair: the approved MINI-164 black hair (Garments/Franki_Hair.fbx), moved by the same offset, rigid on CC_Base_Head."""
import bpy, numpy as np, os
def _import(path):
    before=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=path,use_anim=False); bpy.context.view_layer.update()
    new=[o for o in bpy.data.objects if o not in before]
    for o in new:
        if o.type=='ARMATURE': o.data.pose_position='REST'
    bpy.context.view_layer.update(); return new
def _eval_world(o):
    dg=bpy.context.evaluated_depsgraph_get(); e=o.evaluated_get(dg); me=e.to_mesh()
    W=np.array([e.matrix_world@v.co for v in me.vertices]); polys=[p.vertices[:] for p in me.polygons]
    uv=np.array([d.uv[:] for d in me.uv_layers.active.data]) if me.uv_layers else None
    e.to_mesh_clear(); return W,polys,uv
def load(strong_fbx,hair_fbx,target_head):
    new=_import(strong_fbx)
    farm=[o for o in new if o.type=='ARMATURE'][0]
    hb=[b for b in farm.data.bones if b.name.endswith(':Head')][0]
    delta=np.asarray(target_head)-np.array(farm.matrix_world@hb.head_local); delta[0]=0.0
    body=[o for o in new if o.type=='MESH' and o.name.startswith('Ch28_Body')][0]
    W,polys,uv=_eval_world(body)
    keep=[i for i,p in enumerate(polys) if all(abs(W[v,0])<0.16 and W[v,2]>1.45 for v in p)]
    used=sorted({v for i in keep for v in polys[i]}); remap={v:k for k,v in enumerate(used)}
    me=bpy.data.meshes.new('FrankiHeadSrc'); me.from_pydata((W[used]+delta).tolist(),[],[[remap[v] for v in polys[i]] for i in keep])
    # per-corner UVs in polygon order
    loops=[]; li=0; start=[]
    for p in polys: start.append(li); li+=len(p)
    me.uv_layers.new(name='UV'); uvd=me.uv_layers[0].data; k=0
    for i in keep:
        for j in range(len(polys[i])): uvd[k].uv=uv[start[i]+j]; k+=1
    head=bpy.data.objects.new('FrankiHeadSrc',me); bpy.context.scene.collection.objects.link(head)
    # load the Ch28 base colour explicitly (the FBX material may bind several images; pick by file name)
    img=bpy.data.images.load(os.path.join(os.path.dirname(strong_fbx),'Ch28_1001_Diffuse.png'),check_existing=True)
    img.colorspace_settings.name='sRGB'
    for o in new: bpy.data.objects.remove(o,do_unlink=True)
    # hair
    newh=_import(hair_fbx); hm=[o for o in newh if o.type=='MESH'][0]
    HW,hp,huv=_eval_world(hm)
    hme=bpy.data.meshes.new('FrankiHairSrc'); hme.from_pydata((HW+delta).tolist(),[],hp)
    if huv is not None:
        hme.uv_layers.new(name='UV'); hme.uv_layers[0].data.foreach_set('uv',huv.ravel())
    hair=bpy.data.objects.new('FrankiHairSrc',hme); bpy.context.scene.collection.objects.link(hair)
    hcol=(0.020,0.020,0.022,1.0)
    for s in hm.material_slots:
        if s.material and s.material.use_nodes:
            b=s.material.node_tree.nodes.get('Principled BSDF')
            if b: hcol=tuple(b.inputs['Base Color'].default_value)
    for o in newh: bpy.data.objects.remove(o,do_unlink=True)
    print('franki head tex',img.filepath,img.size[:],'franki head verts',len(used),'delta',delta.round(3),'hair verts',len(HW),'hair colour',np.round(hcol,3))
    return head,img,hair,hcol,delta

def section_profile(W,polys,z,nang=72,center=None):
    """Radius per angle (around the section centroid) of a mesh cut at height z."""
    E=set()
    for p in polys:
        for a,b in zip(p,p[1:]+p[:1]): E.add((min(a,b),max(a,b)))
    E=np.array(list(E)); a,b=W[E[:,0]],W[E[:,1]]; da,db=a[:,2]-z,b[:,2]-z; m=da*db<0
    t=da[m]/(da[m]-db[m]); P=(a[m]+(b[m]-a[m])*t[:,None])[:,:2]; P=P[np.abs(P[:,0])<0.13]
    c=P.mean(0) if center is None else center
    d=P-c; ang=np.arctan2(d[:,1],d[:,0]); r=np.linalg.norm(d,axis=1)
    bins=((ang+np.pi)/(2*np.pi)*nang).astype(int)%nang; R=np.full(nang,np.nan)
    for k in range(nang):
        mm=bins==k
        if mm.any(): R[k]=r[mm].max()
    # fill empty bins circularly, then light smoothing
    idx=np.where(~np.isnan(R))[0]
    R=np.interp(np.arange(nang),np.r_[idx-nang,idx,idx+nang],np.r_[R[idx],R[idx],R[idx]])
    R=(np.roll(R,1)+2*R+np.roll(R,-1))/4
    return c,R
def neck_bridge(Wa,pa,za,Wb,pb,zb,step=0.003,overlap=0.010,nang=72):
    """Closed-enough tube from the donor neck section at za up to the Franki head section at zb.
    Radius and centre blend with a smoothstep, so the under-chin slopes naturally into the jaw."""
    ca,Ra=section_profile(Wa,pa,za,nang); cb,Rb=section_profile(Wb,pb,zb,nang)
    zs=np.arange(za-overlap,zb+overlap+1e-9,step); V=[];F=[]
    th=-np.pi+(np.arange(nang)+0.5)*2*np.pi/nang
    for z in zs:
        s=np.clip((z-za)/(zb-za),0,1); s=s*s*(3-2*s); c=ca+(cb-ca)*s; R=Ra+(Rb-Ra)*s
        for k in range(nang): V.append((c[0]+R[k]*np.cos(th[k]),c[1]+R[k]*np.sin(th[k]),z))
    for i in range(len(zs)-1):
        for k in range(nang):
            a=i*nang+k; b=i*nang+(k+1)%nang; F.append((a,b,b+nang,a+nang))
    return np.array(V),F

TILT=0.36   # Franki's neck stub is cut on a slant: z rises 0.36 m per m toward the back (+Y)
def shear(W,k=TILT):
    S=W.copy(); S[:,2]=W[:,2]-k*W[:,1]; return S
def unshear(W,k=TILT):
    S=W.copy(); S[:,2]=W[:,2]+k*W[:,1]; return S
def neck_bridge_tilted(Wa,pa,za,Wb,pb,zb,k=TILT,**kw):
    """neck_bridge in sheared space (z' = z - k*y), so both cuts follow the slanted neck-stub edge."""
    V,F=neck_bridge(shear(Wa,k),pa,za,shear(Wb,k),pb,zb,**kw); return unshear(V,k),F

def hair_cap(head_obj_W,head_polys,hair_W,A_fn,max_off=0.0065,cover=0.010,min_z=None):
    """Mobile hair: a solid cap grown from the scalp of the decimated head instead of ~9.5k tris of alpha cards.
    Scalp faces = faces whose vertices all lie within `cover` of the card hair (so the coverage and hairline follow
    the approved MINI-164 hair). Each vertex moves out along its normal by an offset that is ~0 on the cap border
    (no visible edge on the skin) and reaches max_off a few rings in (hair volume at the crown)."""
    from scipy.spatial import cKDTree
    d,_=cKDTree(hair_W).query(head_obj_W); near=d<cover
    if min_z is not None: near&=head_obj_W[:,2]>min_z
    faces=[p for p in head_polys if all(near[i] for i in p)]
    used=sorted({i for p in faces for i in p}); rm_={v:k for k,v in enumerate(used)}
    V=head_obj_W[used].copy(); F=[[rm_[i] for i in p] for p in faces]
    # vertex normals + ring distance from the cap border
    N=np.zeros_like(V)
    for f in F:
        for k in range(1,len(f)-1):
            n=np.cross(V[f[k]]-V[f[0]],V[f[k+1]]-V[f[0]]); N[f[0]]+=n; N[f[k]]+=n; N[f[k+1]]+=n
    N/=np.maximum(np.linalg.norm(N,axis=1),1e-9)[:,None]
    from collections import defaultdict
    ec=defaultdict(int); nb=defaultdict(set)
    for f in F:
        for a,b in zip(f,f[1:]+f[:1]): ec[(min(a,b),max(a,b))]+=1; nb[a].add(b); nb[b].add(a)
    ring=np.full(len(V),99); cur={v for (a,b),c in ec.items() if c==1 for v in (a,b)}; r=0
    while cur:
        for v in cur: ring[v]=min(ring[v],r)
        nxt={w for v in cur for w in nb[v] if ring[w]==99}; cur=nxt; r+=1
    t=np.clip(ring/3.0,0,1); t=t*t*(3-2*t)
    V=V+N*(0.0004+max_off*t)[:,None]
    return V,F
