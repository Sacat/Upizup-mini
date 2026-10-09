"""MINI-206 Stage 2: proper garments over the MINI-197 bare bodies.
Method (per garment): a closed SOLID = union of (a) the body inflated a few mm in the garment's region (shoulders/yoke
or hips/seat), (b) loose lofted tubes built from body cross-sections + ease (the torso hangs partly from the chest; legs go
straight from thigh to hem), via voxel remesh; then the solid's surface is cut at the openings (neckline, sleeves, hem,
waist, leg hems), smoothed so it drapes instead of copying muscles, decimated to budget, and real thickness is added where
it shows (rib/hem/fold bands). Usage: build_garments.py <body.fbx> <outdir> <character> [garment ...]"""
import bpy,sys,os,math,numpy as np
sys.path.insert(0,os.path.dirname(__file__)); from garlib import *
argv=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
fbx,outdir,char=argv[:3]; which=argv[3:] or ['tee','polo','jeans','trousers','shorts']
os.makedirs(outdir,exist_ok=True)
E=lambda k,d: float(os.environ.get(k,d))

def setup():
    arm,parts,bm,bvh=load_body(fbx)
    L={}
    for k,n in [('nk','CC_Base_NeckTwist01'),('ua','CC_Base_L_Upperarm'),('fa','CC_Base_L_Forearm'),('hip','CC_Base_Hip'),
                ('th','CC_Base_L_Thigh'),('ca','CC_Base_L_Calf'),('ft','CC_Base_L_Foot'),('wa','CC_Base_Waist')]:
        L[k]=bone(arm,n)[0]
    return arm,parts,bm,bvh,L

def section_center(bvh,z,x=0.0):
    f=bvh.ray_cast(Vector((x,-0.6,z)),Vector((0,1,0)),1.2); b=bvh.ray_cast(Vector((x,0.6,z)),Vector((0,-1,0)),1.2)
    return np.array([x,(f[0].y+b[0].y)/2 if f[0] and b[0] else 0.0,z])

def torso_tube(bvh,z0,z1,ease_pts,hang,name,maxr=0.30):
    N=40; th=np.linspace(0,2*np.pi,N,endpoint=False); dirs=np.array([[math.cos(a),math.sin(a),0] for a in th])
    zs=np.linspace(z0,z1,26); R=[];C=[]
    for z in zs:
        c=section_center(bvh,z); C.append(c); R.append(smooth_circ(fill_circ(body_radius(bvh,c,dirs,maxr)),2))
    R=np.array(R); hz=lambda z: hang*(1-np.clip((z-(z1-0.14))/0.14,0,1)**2)
    H=np.array([hz(zs[i])*R[i:].max(0)+(1-hz(zs[i]))*R[i] for i in range(len(R))])
    ease=np.interp(zs,[p[0] for p in ease_pts],[p[1] for p in ease_pts])
    return loft(name,[C[i]+np.outer(H[i]+ease[i],[1,1,1])*dirs for i in range(len(zs))])

def neck_cut_fn(nc,back,side,front,flare=2.0):
    def f(x,y,z):
        dx,dy=x,y-nc[1]; r=math.hypot(dx,dy*0.95)
        if r<0.16 and z>1.38:
            a=math.atan2(dy,abs(dx)); zN=side+(back-side)*math.sin(a)**2 if a>0 else side+(front-side)*math.sin(a)**2
            zN+=max(0.0,r-0.095)*flare
            if z>zN: return True
        if z>1.40 and ((x/0.072)**2+((y-nc[1])/0.072)**2)<1: return True
        return z>1.62
    return f

def top_garment(arm,parts,bm,bvh,L,kind):
    HEM=E('HEM','0.875'); SLEEVE=E('SLEEVE','0.42' if kind=='tee' else '0.44')
    nk,ua,fa=L['nk'],L['ua'],L['fa']; ARMPIT=ua[2]-0.12
    torso=torso_tube(bvh,HEM-0.02,ARMPIT,[(HEM,0.035),(1.05,0.028),(ARMPIT-0.06,0.016),(ARMPIT,0.008)],E('HANG','0.5'),'torso')
    sl=[]
    for s in (1,-1):
        p0=np.array([s*0.12,ua[1],ua[2]-0.01]); p1=np.array([s*(SLEEVE+0.03),fa[1],fa[2]-0.01])
        sl.append(tube_along(bvh,'sleeve',p0,p1,lambda t:0.018+0.012*t,14,N=28,maxr=0.12))
    inf=inflated_body(bm,0.007)
    shell=union_remesh([torso,inf]+sl,0.007,kind)
    nc=np.array([0,nk[1]+0.005,nk[2]])
    neck=neck_cut_fn(nc,E('NECK_BACK','1.525'),E('NECK_SIDE','1.565'),E('NECK_FRONT','1.462' if kind=='tee' else '1.475'))
    delete_faces(shell,lambda p: p[2]<HEM or abs(p[0])>SLEEVE or neck(*p))
    smooth_shell(shell,iters=16,factor=0.5)
    decimate_to(shell,int(E('SHELL_TRIS','3000' if kind=='tee' else '3300')))
    smooth_open_edges(shell,10)
    from mathutils.bvhtree import BVHTree
    sbvh=BVHTree.FromObject(shell,bpy.context.evaluated_depsgraph_get())
    loops=boundary_loops(shell); extra=[]
    for Lp in loops:
        cz=Lp[:,2].mean(); cx=Lp[:,0].mean()
        if cz>1.38 and abs(cx)<0.1:
            if kind=='tee':
                extra.append(band_along_loop('neckrib',Lp,lambda P:np.array([0,nc[1],P[:,2].mean()]),0.006,1.0,0.016,0.004,segments=56))
            else:
                extra+=polo_collar(Lp,nc,sbvh)
        elif abs(cx)>0.3:
            Lr=resample_loop(Lp,36); ax=np.array([np.sign(cx),0,0]); c=Lr.mean(0); toC=c-Lr; toC/=np.maximum(np.linalg.norm(toC,axis=1),1e-6)[:,None]
            w=0.022 if kind=='tee' else 0.028
            extra.append(loft('cuff',[Lr-ax*w,Lr,Lr-toC*0.004,Lr-ax*w-toC*0.004,Lr-ax*w],closed_ends=False))
        else:
            extra.append(fold_band(Lp,lambda P:P.mean(0)*np.array([1,1,0])+np.array([0,0,P[:,2].mean()]),np.array([0,0,0.025]),0.004,72,'hem'))
    if kind=='polo': extra+=polo_placket(bvh,nc)
    return join([shell]+extra,'Tee' if kind=='tee' else 'Polo')

def polo_collar(Lp,nc,sbvh):
    """polo collar: a 2.6 cm stand rising from the neckline, folded over into a leaf that lies ON the shirt (each leaf point is
    ray-projected onto the shell + 3 mm), with the classic pointed tips either side of the front placket gap. Solidified 3 mm."""
    Lr=resample_loop(Lp,72); c=np.array([0,nc[1],0])
    ang=np.arctan2(Lr[:,1]-c[1],Lr[:,0])                 # -pi/2 = front
    dfront=np.abs(np.angle(np.exp(1j*(ang+np.pi/2))))  # 0 at the front centre
    keep=dfront>0.17
    start=int(np.argmin(dfront)); order=[(start+i)%len(Lr) for i in range(len(Lr)) if keep[(start+i)%len(Lr)]]
    P=Lr[order]; df=dfront[order]
    out=P-c; out[:,2]=0; out/=np.maximum(np.linalg.norm(out,axis=1),1e-6)[:,None]
    up=np.array([0,0,1.0])
    stand=P+up*0.026+out*0.004
    fold=stand+out*0.006+up*0.002
    tipw=np.clip(1-(df-0.17)/0.6,0,1)**1.5             # 1 at the collar points, 0 round the back
    reach=0.032+0.030*tipw                               # leaf width: 3.2 cm at the back, 6.2 cm at the points
    rings=[P,stand,fold]
    def smooth_loop(A,it=3):
        for _ in range(it): A=np.vstack([A[:1],(A[:-2]+2*A[1:-1]+A[2:])/4,A[-1:]])
        return A
    for f in (0.5,1.0):
        R=[]
        for i in range(len(P)):
            g=fold[i]+out[i]*reach[i]*f-up*(0.020*f+0.015*tipw[i]*f)
            for _ in range(3):      # settle onto the shirt: nearest surface point + 3 mm along its outward normal
                loc,nrm,_,_=sbvh.find_nearest(Vector(tuple(g)))
                if loc is None: break
                nv=np.array(nrm); 
                if np.dot(nv,out[i])<0 and nv[2]<0.5: nv=-nv
                g=np.array(loc)+nv*0.003
            R.append(g)
        rings.append(smooth_loop(np.array(R)))
    V=np.vstack(rings); n=len(P); F=[]
    for j in range(len(rings)-1):
        for i in range(n-1):
            a=j*n+i; F.append((a,a+n,a+n+1,a+1))
    o=mesh_obj('collar',V,F)
    m=o.modifiers.new('s','SOLIDIFY'); m.thickness=0.003; m.offset=0
    sm=o.modifiers.new('sub','SUBSURF'); sm.levels=0; o.modifiers.remove(sm)
    activate(o); bpy.ops.object.modifier_apply(modifier='s')
    return [o]

def polo_placket(bvh,nc):
    """3-button placket: a raised strip down the centre front + 3 buttons."""
    out=[]; zs=np.linspace(1.47,1.33,8); pts=[]
    for z in zs:
        h=bvh.ray_cast(Vector((0,-0.6,z)),Vector((0,1,0)),1.2); pts.append(np.array([0,h[0].y-0.024,z]) if h[0] else np.array([0,-0.13,z]))
    pts=np.array(pts); w=0.015; d=0.004
    rings=[]
    for p in pts: rings.append(np.array([p+[-w,0,0],p+[w,0,0],p+[w,-d,0],p+[-w,-d,0]]))
    out.append(loft('placket',rings,closed_ends=True))
    for z in (1.445,1.405,1.365):
        p=pts[np.argmin(np.abs(pts[:,2]-z))]+np.array([0,-d,0])
        bpy.ops.mesh.primitive_cylinder_add(vertices=10,radius=0.0055,depth=0.003,location=tuple(p),rotation=(math.pi/2,0,0))
        out.append(bpy.context.object)
    # chest logo patch (left chest): a 3 cm square, 1 mm proud, its own material slot for the tint/logo
    h=bvh.ray_cast(Vector((0.085,-0.6,1.36)),Vector((0,1,0)),1.2)
    if h[0]:
        p=np.array(h[0])+np.array([0,-0.026,0])
        bpy.ops.mesh.primitive_plane_add(size=0.03,location=tuple(p),rotation=(math.pi/2,0,0)); lg=bpy.context.object; lg.name='logo'
        out.append(lg)
    return out

def bottom_garment(arm,parts,bm,bvh,L,kind):
    WAIST=E('WAIST','1.035'); th,ca,ft=L['th'],L['ca'],L['ft']
    if kind=='shorts': HEM=E('HEM_Z','0.56')
    elif kind=='trousers': HEM=E('HEM_Z','0.045')
    else: HEM=E('HEM_Z','0.040')
    ease_top={'jeans':0.012,'trousers':0.010,'shorts':0.016}[kind]
    hem_r={'jeans':0.072,'trousers':0.064,'shorts':0.105}[kind]       # straight-leg opening radius at the hem
    seat=body_region_solid(bm,0.80,WAIST+0.03,0.008,'seat')
    hips=torso_tube(bvh,0.84,WAIST+0.02,[(0.84,ease_top+0.004),(WAIST,ease_top)],0.0,'hips',maxr=0.28)
    legs=[]
    for s in (1,-1):
        top=np.array([s*abs(th[0])*1.05,th[1],0.90]); knee=np.array([s*abs(ca[0]),ca[1],ca[2]]); ank=np.array([s*abs(ft[0]),ft[1]+0.01,HEM-0.01])
        if kind=='shorts':
            knee2=top+(knee-top)*((top[2]-HEM+0.01)/(top[2]-knee[2]))
            legs.append(tube_along(bvh,'leg',top,knee2,lambda t:ease_top+0.02*t,12,N=32,maxr=0.16,min_r_fn=lambda t:0.08+(hem_r-0.08)*t))
        else:
            r_top=0.085
            legs.append(tube_along(bvh,'thigh',top,knee,lambda t:ease_top+0.006*t,12,N=32,maxr=0.16,min_r_fn=lambda t:r_top+(0.5*(r_top+hem_r)-r_top)*t,extend=(0.05,0.0)))
            legs.append(tube_along(bvh,'shin',knee,ank,lambda t:ease_top+0.01,14,N=32,maxr=0.14,min_r_fn=lambda t:0.5*(r_top+hem_r)+(hem_r-0.5*(r_top+hem_r))*t,extend=(0.04,0.02)))
    shell=union_remesh([seat,hips]+legs,0.007,kind)
    delete_faces(shell,lambda p: p[2]>WAIST or p[2]<HEM)
    smooth_shell(shell,iters=18,factor=0.5)
    decimate_to(shell,int(E('SHELL_TRIS',{'jeans':'4700','trousers':'4700','shorts':'3000'}[kind])))
    smooth_open_edges(shell,10)
    extra=[]
    for Lp in boundary_loops(shell):
        cz=Lp[:,2].mean()
        if cz>WAIST-0.05:      # waistband: 4 cm band, belt line
            extra.append(fold_band(Lp,lambda P:np.array([0,P[:,1].mean(),P[:,2].mean()]),np.array([0,0,-0.042]),0.005,72,'waistband'))
        else:                   # leg hem: turned-up fold
            extra.append(fold_band(Lp,lambda P:P.mean(0),np.array([0,0,0.03 if kind!='shorts' else 0.022]),0.004,40,'hemfold'))
    return join([shell]+extra,{'jeans':'Jeans','trousers':'Trousers','shorts':'Shorts'}[kind])

if __name__=='__main__':
    import json
    report={}
    for kind in which:
        arm,parts,bm,bvh,L=setup()
        g=top_garment(arm,parts,bm,bvh,L,kind) if kind in ('tee','polo') else bottom_garment(arm,parts,bm,bvh,L,kind)
        report[kind]={'tris':tris(g)}
        bpy.ops.wm.save_as_mainfile(filepath=os.path.join(outdir,f'{char}_{kind}_raw.blend'))
        print('GARMENT',char,kind,'tris',tris(g))
    json.dump(report,open(os.path.join(outdir,f'{char}_raw_report.json'),'w'),indent=1)
