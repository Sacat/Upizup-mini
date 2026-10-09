"""MINI-206 puffed mariner chain: builds the instanced link and drapes it on ANY body (+ optional garment) pair.
Usage: blender python chain_fit.py -- <body.fbx> <out_dir> <name> [garment.fbx ...]
Link: puffed oval (mariner/"Gucci" style) 9 mm wide x 13.5 mm long, 3.4 mm puffed wire, centre bar; links alternate 90 deg.
Path: back/side arc rests on the trapezius around the neck base (found by ray-casting down), the front hangs as a catenary
between the collarbones and is pushed onto the outer surface (shirt if worn, else skin) so it lies ON it, never in it."""
import bpy,bmesh,sys,os,math,json,numpy as np
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
argv=sys.argv[sys.argv.index('--')+1:]; BODY,OUT,NAME=argv[:3]; GARMENTS=argv[3:]
DROP=float(os.environ.get('CHAIN_DROP','0.27'))   # lowest front point below the neck base (m)
LW,LL,WIRE=0.009,0.0135,0.0034; PITCH=LL-WIRE*1.15  # link width, length, wire thickness; pitch = interlinked spacing
os.makedirs(OUT,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=BODY)
for g in GARMENTS: bpy.ops.import_scene.fbx(filepath=g)
arms=[o for o in bpy.context.scene.objects if o.type=='ARMATURE']; arm=arms[0]
for a in arms[1:]:
    for o in list(a.children): 
        M=o.matrix_world.copy(); o.parent=arm; o.matrix_world=M
        for m in o.modifiers:
            if m.type=='ARMATURE': m.object=arm
    bpy.data.objects.remove(a)
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH' and 'Hair' not in o.name]
dg=bpy.context.evaluated_depsgraph_get()
bm=bmesh.new()
for o in meshes:
    me=o.evaluated_get(dg).to_mesh(); t=bmesh.new(); t.from_mesh(me); bmesh.ops.transform(t,matrix=o.matrix_world,verts=t.verts)
    m2=bpy.data.meshes.new('t'); t.to_mesh(m2); t.free(); bm.from_mesh(m2); bpy.data.meshes.remove(m2); o.evaluated_get(dg).to_mesh_clear()
bvh=BVHTree.FromBMesh(bm)
def bonepos(n): return np.array(arm.matrix_world@arm.pose.bones[n].head)
NK=bonepos('CC_Base_NeckTwist01'); SP=bonepos('CC_Base_Spine02')
R=0.5*WIRE+0.0028      # centre-line clearance above the surface
def down(x,y,z0=2.2):
    h=bvh.ray_cast(Vector((x,y,z0)),Vector((0,0,-1)),3); return np.array(h[0]) if h[0] else None
def from_front(x,z):
    h=bvh.ray_cast(Vector((x,-0.8,z)),Vector((0,1,0)),2); return (np.array(h[0]),np.array(h[1])) if h[0] else (None,None)
# 1) back + side arc: ellipse around the neck base, resting on the trapezius/collar (ray down from above)
pts=[]
for a in np.linspace(math.radians(-35),math.radians(215),36):     # 90 = straight back (+Y), sweeps over both shoulders to the front-sides
    x=0.098*math.cos(a); y=NK[1]+0.074*math.sin(a)
    p=down(x,y)
    # rays from above can land on the neck/head: accept only hits below the neck base + 4 cm
    z0=NK[2]+0.04
    while p is not None and p[2]>z0: p=down(x,y,p[2]-0.005)
    if p is not None and p[2]>NK[2]-0.06: pts.append(p+np.array([0,0,R]))
arc=np.array(pts)
# 2) front: catenary between the two front ends of the arc, sagging DROP below the neck base, pushed onto the outer surface
L0,R0=arc[0],arc[-1]
xs=np.linspace(L0[0],R0[0],60); zc=NK[2]-DROP
span=abs(R0[0]-L0[0])/2; sag=min(L0[2],R0[2])-zc
c=0.07
for _ in range(60): c=span/ math.acosh(1+sag/c) if 1+sag/c>1 else c   # solve cosh catenary constant
front=[]
for x in xs:
    z=zc+c*(math.cosh(x/c)-1); p,nrm=from_front(x,z)
    if p is not None: front.append(p+nrm*R)
# blend the joins (the arc ends and the catenary starts meet smoothly)
path=np.vstack([front[::-1] if np.linalg.norm(front[0]-arc[-1])<np.linalg.norm(front[-1]-arc[-1]) else front, arc])
path=np.vstack([arc,front if np.linalg.norm(front[0]-arc[-1])<np.linalg.norm(front[-1]-arc[-1]) else front[::-1]])
for _ in range(6): path=np.vstack([path[:1]*0+path[-1:]*0+path[:1],path[1:-1]]) if False else np.array([(path[i-1]+2*path[i]+path[(i+1)%len(path)])/4 for i in range(len(path))])
# re-push smoothed points out of the surface
def push_out(p):
    loc,n,_,d=bvh.find_nearest(Vector(tuple(p)))
    if loc is None: return p
    v=np.array(p)-np.array(loc); nn=np.array(n)
    if np.dot(v,nn)<R: return np.array(loc)+nn*R
    return p
path=np.array([push_out(p) for p in path])
# 3) resample at the link pitch along the closed loop
seg=np.linalg.norm(np.diff(np.vstack([path,path[:1]]),axis=0),axis=1); s=np.r_[0,np.cumsum(seg)]; total=s[-1]
n=int(total/PITCH); n-=n%2; st=np.linspace(0,total,n,endpoint=False)
P=np.array([[np.interp(t,s,np.r_[path[:,k],path[0,k]]) for k in range(3)] for t in st])
for _ in range(3): P=np.array([push_out(p) for p in P])
# 4) link mesh (one, instanced)
def link_mesh():
    bm=bmesh.new(); U,V=8,4; a,b=LL/2-WIRE/2,LW/2-WIRE/2; r=WIRE/2
    verts=[]
    for i in range(U):
        t=2*math.pi*i/U; c=Vector((a*math.cos(t),b*math.sin(t),0)); tg=Vector((-a*math.sin(t),b*math.cos(t),0)).normalized(); nr=Vector((tg.y,-tg.x,0))
        puff=1.0+0.18*abs(math.cos(t))      # puffed: fatter at the ends
        ring=[bm.verts.new(c+(nr*math.cos(2*math.pi*j/V)*r*puff+Vector((0,0,1))*math.sin(2*math.pi*j/V)*r*puff)) for j in range(V)]
        verts.append(ring)
    for i in range(U):
        for j in range(V): bm.faces.new((verts[i][j],verts[(i+1)%U][j],verts[(i+1)%U][(j+1)%V],verts[i][(j+1)%V]))
    bmesh.ops.create_cube(bm,size=1.0,matrix=Matrix.Diagonal((WIRE*0.7,LW-WIRE*1.1,WIRE*0.7,1)))   # centre bar
    me=bpy.data.meshes.new('MarinerLink'); bm.to_mesh(me); bm.free(); return me
lm=link_mesh()
out=bmesh.new(); tmp=bmesh.new(); tmp.from_mesh(lm)
for i,p in enumerate(P):
    tg=P[(i+1)%n]-P[i-1]; tg/=np.linalg.norm(tg)
    loc,nrm,_,_=bvh.find_nearest(Vector(tuple(p))); nn=np.array(nrm); nn-=tg*np.dot(nn,tg); nn/=np.linalg.norm(nn)
    side=np.cross(nn,tg)
    if i%2: nn,side=side,-nn            # alternate links 90 deg
    M=Matrix(((tg[0],side[0],nn[0],p[0]),(tg[1],side[1],nn[1],p[1]),(tg[2],side[2],nn[2],p[2]),(0,0,0,1)))
    # links lying flat get a little lift so the puff never touches the surface
    if i%2==0: M=Matrix.Translation(Vector(tuple(nrm))*WIRE*0.2)@M
    t2=tmp.copy(); bmesh.ops.transform(t2,matrix=M,verts=t2.verts); m3=bpy.data.meshes.new('t'); t2.to_mesh(m3); t2.free(); out.from_mesh(m3); bpy.data.meshes.remove(m3)
me=bpy.data.meshes.new(f'Chain_{NAME}'); out.to_mesh(me); out.free()
ch=bpy.data.objects.new(f'Chain_{NAME}',me); bpy.context.scene.collection.objects.link(ch)
gold=bpy.data.materials.new('ChainGold'); gold.use_nodes=True; b=gold.node_tree.nodes['Principled BSDF']; b.inputs[0].default_value=(1.0,0.76,0.33,1); b.inputs['Metallic'].default_value=1; b.inputs['Roughness'].default_value=0.22
me.materials.append(gold)
for pl in me.polygons: pl.use_smooth=True
# skin: rigid on the chest bone, the back/top links follow the neck a little
vg_c=ch.vertex_groups.new(name='CC_Base_Spine02'); vg_n=ch.vertex_groups.new(name='CC_Base_NeckTwist01')
for v in me.vertices:
    w=float(np.clip((v.co.z-(NK[2]-0.02))/0.05,0,1))*0.6
    vg_c.add([v.index],1-w,'REPLACE')
    if w>0: vg_n.add([v.index],w,'REPLACE')
ch.parent=arm; ch.matrix_parent_inverse=arm.matrix_world.inverted(); am=ch.modifiers.new('Armature','ARMATURE'); am.object=arm
# measurements: clearance from the surface (should be >= 0), drop, length
d=[bvh.find_nearest(Vector(tuple(p)))[3] for p in P]
rep={'links':int(n),'tris':sum(len(p.vertices)-2 for p in me.polygons),'loop_length_m':round(total,3),'loop_length_in':round(total/0.0254,1),
     'lowest_point_below_neck_base_m':round(float(NK[2]-P[:,2].min()),3),'centreline_min_clearance_mm':round(1000*(min(d)-WIRE/2),2),'garments':[os.path.basename(g) for g in GARMENTS]}
json.dump(rep,open(os.path.join(OUT,f'Chain_{NAME}.json'),'w'),indent=1); print('CHAIN',NAME,rep)
for x in bpy.context.view_layer.objects: x.select_set(False)
arm.select_set(True); ch.select_set(True); bpy.context.view_layer.objects.active=arm
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,f'Chain_{NAME}.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=False,mesh_smooth_type='OFF',path_mode='STRIP',use_mesh_modifiers=False)
# render
sys.path.insert(0,os.path.dirname(__file__)); import rlib
for o in bpy.data.objects:
    if o.type=='MESH' and o is not ch:
        mt=bpy.data.materials.new('m'); mt.use_nodes=True; mt.node_tree.nodes['Principled BSDF'].inputs[0].default_value=(0.42,0.26,0.17,1) if 'Bare' in o.name else (0.12,0.16,0.32,1)
        o.data.materials.clear(); o.data.materials.append(mt)
cam=rlib.setup_render(420,420,16); rlib.closeup(cam,os.path.join(OUT,f'Chain_{NAME}'),(0,0,NK[2]-0.12),0.42,views=(('front',0),('side',90),('back',180),('tq',35)))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,f'Chain_{NAME}.blend'),compress=True)
