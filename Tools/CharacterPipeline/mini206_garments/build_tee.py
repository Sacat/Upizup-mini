import bpy,sys,os,math,numpy as np
sys.path.insert(0,os.path.dirname(__file__)); from garlib import *; import rlib
fbx,out=sys.argv[-2:]
arm,parts,bm,bvh=load_body(fbx)
sp2,_=bone(arm,'CC_Base_Spine02'); nk,_=bone(arm,'CC_Base_NeckTwist01')
uaL,_=bone(arm,'CC_Base_L_Upperarm'); faL,_=bone(arm,'CC_Base_L_Forearm')
HEM=float(os.environ.get('HEM','0.875'))
NECK_BACK=float(os.environ.get('NECK_BACK','1.525')); NECK_SIDE=float(os.environ.get('NECK_SIDE','1.565')); NECK_FRONT=float(os.environ.get('NECK_FRONT','1.462')); ARMPIT=uaL[2]-0.12; SLEEVE=float(os.environ.get('SLEEVE','0.42'))
# ---- torso hang tube
N=40; th=np.linspace(0,2*np.pi,N,endpoint=False); dirs=[np.array([math.cos(a),math.sin(a),0]) for a in th]
zs=np.linspace(HEM-0.02,ARMPIT,26); R=[]; C=[]
for z in zs:
    # section centre: midway between front and back surface on the spine line
    f=bvh.ray_cast(Vector((0,-0.5,z)),Vector((0,1,0)),1.0); b=bvh.ray_cast(Vector((0,0.5,z)),Vector((0,-1,0)),1.0)
    cy=(f[0].y+b[0].y)/2 if f[0] and b[0] else 0.0
    c=np.array([0,cy,z]); C.append(c)
    R.append(smooth_circ(fill_circ(body_radius(bvh,c,dirs,0.30)),2))
R=np.array(R)
HANG=float(os.environ.get('HANG','0.5'))
hang=np.array([HANG*R[i:].max(0)+(1-HANG)*R[i] for i in range(len(R))])   # partly hangs from the chest, partly follows the body
ease=np.interp(zs,[HEM,1.05,ARMPIT],[0.035,0.028,0.018])
rings=[C[i]+np.outer(hang[i]+ease[i],[1,1,1])*np.array(dirs) for i in range(len(zs))]
torso=loft('torso',rings)
# ---- sleeves
sl=[]
for s in (1,-1):
    p0=np.array([s*0.12,uaL[1],uaL[2]-0.01]); p1=np.array([s*(SLEEVE+0.03),faL[1],faL[2]-0.01])
    sl.append(tube_along(bvh,'sleeve',p0,p1,lambda t:0.018+0.012*t,14,N=28,maxr=0.12))
inf=inflated_body(bm,0.007)
shell=union_remesh([torso,inf]+sl,0.007,'tee')
# ---- cuts
nc=np.array([0,nk[1]+0.005,nk[2]])
def cut(p):
    x,y,z=p
    if z<HEM: return True
    if abs(x)>SLEEVE: return True
    # round crew neckline: a smooth height around the neck (back / sides / front), applied within 16 cm of the neck axis
    dx,dy=x,y-nc[1]; r=math.hypot(dx,dy*0.95)
    if r<0.16 and z>1.38:
        a=math.atan2(dy,abs(dx))            # +pi/2 = back, 0 = side, -pi/2 = front
        zb,zs,zf=NECK_BACK,NECK_SIDE,NECK_FRONT
        zN=zs+(zb-zs)*math.sin(a)**2 if a>0 else zs+(zf-zs)*math.sin(a)**2
        zN+=max(0.0,r-0.095)*float(os.environ.get('NECK_FLARE','2.0'))   # cut rises away from the neck: follows the shoulder slope, no tabs
        if z>zN: return True
    if z>1.40 and ((x/0.072)**2+((y-nc[1])/0.072)**2)<1: return True   # the neck itself
    if z>1.62: return True
    return False
delete_faces(shell,cut)
smooth_shell(shell,iters=12,factor=0.5)
tris=decimate_to(shell,int(os.environ.get('TEE_TRIS','3000')))
# ---- rib bands at the openings (real thickness where it shows)
loops=boundary_loops(shell); parts_out=[shell]
for L in loops:
    cz=L[:,2].mean(); cx=L[:,0].mean()
    if cz>1.38 and abs(cx)<0.1:     # neck: rib rises 1.6 cm up the neck, leaning in
        parts_out.append(band_along_loop('neckrib',L,lambda L:np.array([0,nc[1],L[:,2].mean()]),0.006,1.0,0.016,0.004,segments=56))
    elif abs(cx)>0.3:               # sleeve cuff: hemmed fold
        L=resample_loop(L,36)
        ax=np.array([np.sign(cx)*1.0,0,0]); c=L.mean(0)
        r0=L; toC=c-L; toC/=np.maximum(np.linalg.norm(toC,axis=1),1e-6)[:,None]
        parts_out.append(loft('cuff',[L-ax*0.022,L,L-toC*0.004,L-ax*0.022-toC*0.004,L-ax*0.022],closed_ends=False))
    else:                           # hem band
        L=resample_loop(L,72)
        c=L.mean(0); toC=c-L; toC[:,2]=0; toC/=np.maximum(np.linalg.norm(toC,axis=1),1e-6)[:,None]
        up=np.array([0,0,0.025])
        parts_out.append(loft('hem',[L,L+up,L+up-toC*0.004,L-toC*0.004,L],closed_ends=False))
for o in parts_out[1:]:
    o.select_set(True)
activate(shell)
for o in parts_out[1:]: o.select_set(True)
bpy.ops.object.join(); shell.name='Tee'
for p in shell.data.polygons: p.use_smooth=True
total=sum(len(p.vertices)-2 for p in shell.data.polygons)
print('TEE tris',total,'loops',len(loops),[ (round(L[:,0].mean(),2),round(L[:,2].mean(),2),len(L)) for L in loops])
mat=bpy.data.materials.new('cloth'); mat.use_nodes=True; mat.node_tree.nodes['Principled BSDF'].inputs[0].default_value=(0.12,0.16,0.32,1)
mat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=0.85; shell.data.materials.append(mat)
bpy.ops.wm.save_as_mainfile(filepath=out+'.blend')
cam=rlib.setup_render(420,560,16)
rlib.closeup(cam,out,(0,0,1.20),0.95,views=(('front',0),('side',90),('back',180),('tq',35)))
