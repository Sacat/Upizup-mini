"""MINI-197 cloud lane: bare modular body from the MINI-105 AccuRig donor (vest + boxers sculpted in).

Stages are cumulative:
  legs  - boxers removed: hems/waistband relaxed, groin rebuilt as a smooth surface, skin filled from the thighs
  hands - hand/forearm/face skin tone evened out
  neck  - vest removed: neckline, straps and hem relaxed, clean neck/shoulder join, bare torso skin
Pipeline: donor -> voxel remesh (closed surface) -> region smoothing -> symmetric decimate (~20k tris)
          -> weights transferred from the AccuRig donor -> UV + 2K base colour baked from donor -> modular split -> FBX.
Usage: python build_bare_body.py <donor.fbx> <basecolor.png> <stage> <out_prefix> [skin_tint r,g,b]"""
import bpy,sys,os,numpy as np,math
sys.path.insert(0,os.path.dirname(__file__)); import rlib,bodylib as B
from scipy.spatial import cKDTree
argv=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[-4:]
fbx,tex,stage,out=argv[:4]; tex=os.path.abspath(tex); out=os.path.abspath(out); fbx=os.path.abspath(fbx)
TINT=np.array([float(t) for t in argv[4].split(',')]) if len(argv)>4 else None
NAME=os.environ.get('BODY_NAME','Sacat')
STAGES=['legs','hands','neck']; upto=STAGES.index(stage)
VOXEL=0.003; TARGET_TRIS=int(os.environ.get('BODY_TRIS','20000'))

# ---------- 1. donor: masks and skin colour (bake source) ----------
arm,donor=B.load(fbx); B.weld(donor)
px=B.tex_pixels(tex); col,_,_=B.vert_colors(donor,px); Wd=B.world_co(donor); Ad=B.adjacency(donor)
s,v=B.hsv(col); z=Wd[:,2]
boxer=B.close_mask(Ad,((v<0.22)&(s<0.45)&(z>0.70)&(z<1.10))|((v<0.30)&(z>0.78)&(z<1.08)&(np.abs(Wd[:,0])<0.20)),4)
vest=B.close_mask(Ad,(v>0.62)&(s<0.18)&(z>0.95)&(z<1.58),3)
fill=B.dilate(Ad,boxer,2)
if upto>=2: fill|=B.dilate(Ad,vest,2)
unknown=B.dilate(Ad,boxer|vest,4)|((s<0.22)&(z>0.6)&(z<1.6))      # sample skin only from clean skin
if upto>=1: unknown|=np.abs(Wd[:,0])>0.745
lin=B.srgb2lin(col)
skin=B.harmonic_fill(Ad,unknown,lin)
skin=np.where(fill[:,None],skin,lin)
if upto>=1:
    Nd=B.vertex_normals(donor,Wd); palm=np.clip(-Nd[:,2],0,1)*np.clip((np.abs(Wd[:,0])-0.75)/0.03,0,1)
    skin=skin*(1+0.22*palm)[:,None]
corr=np.ones((len(Wd),3))   # multiplicative large-scale tone correction (1 = unchanged)
import hands as H
HANDJ={}; thumbv=np.zeros(len(Wd),bool); cutv=np.zeros(len(Wd),bool)
if upto>=1:
    gn={g.index:g.name for g in donor.vertex_groups}
    domb=np.array([gn[max(vv.groups,key=lambda g:g.weight).group] if len(vv.groups) else '' for vv in donor.data.vertices])
    for side,sg in (('L',1),('R',-1)):
        HANDJ[side]=H.design(lambda n: B.bone_world(arm,n,donor),side)
        wrx=B.bone_world(arm,f'CC_Base_{side}_Hand',donor)[0]*sg
        th=np.array([d.startswith(f'CC_Base_{side}_Index') or d.startswith(f'CC_Base_{side}_Thumb') for d in domb])
        thumbv|=th
        cutv|=(~th)&(Wd[:,0]*sg>wrx+0.090)     # fused fingers beyond the knuckle line (thumb kept)
        print(side,'wrist x',round(wrx,3),'cut verts',int(((~th)&(Wd[:,0]*sg>wrx+0.090)).sum()))
    # hand colour: fill from the forearm tone; palms (facing -Z in T-pose) a little lighter, as on real darker skin
    handc=np.abs(Wd[:,0])>0.755
    fill|=handc
if upto>=1:
    # skin tone: arms read lighter than face/legs and the neck darker. Even out the LOW-frequency tone only,
    # keeping texture detail: corr = target / smoothed_skin_colour on skin vertices (hair/eyes/brows excluded).
    skinv=(s>0.25)&(v>0.25)&~B.dilate(Ad,boxer|vest,2)
    import scipy.sparse as sp
    deg=np.asarray(Ad.sum(1)).ravel(); P=sp.diags(1/np.maximum(deg,1))@Ad
    num=np.where(skinv[:,None],skin,0.0); den=skinv.astype(float)[:,None]
    for _ in range(int(os.environ.get('TONE_SMOOTH','400'))): num=P@num; den=P@den
    low=num/np.maximum(den,1e-6)
    target=skin[skinv].mean(0) if TINT is None else B.srgb2lin(TINT)
    k=float(os.environ.get('TONE_STRENGTH','0.85'))
    c=np.clip((target/np.maximum(low,1e-4))**k,0.6,1.6)
    hairish=(v<0.18)|(s<0.18)
    corr=np.where(hairish[:,None],1.0,c)
    for name,m in (('face',(np.abs(Wd[:,0])<0.05)&(Wd[:,2]>1.62)),('arm',np.abs(Wd[:,0])>0.3),('hand',np.abs(Wd[:,0])>0.74),('thigh',(Wd[:,2]>0.6)&(Wd[:,2]<0.75))):
        print('tone corr',name,corr[m&skinv].mean(0).round(3))
me=donor.data
skin=skin*np.where(fill[:,None],corr,1.0)
ca=me.color_attributes.new('tone_corr','FLOAT_COLOR','POINT'); ca.data.foreach_set('color',np.c_[corr,np.ones(len(Wd))].ravel())
a=me.color_attributes.new('skin_fill','FLOAT_COLOR','POINT'); a.data.foreach_set('color',np.c_[skin,np.ones(len(Wd))].ravel())
mk=me.attributes.new('fill_mask','FLOAT','POINT'); mk.data.foreach_set('value',fill.astype(float))
img=bpy.data.images.load(tex)
def skin_material(name,use_tex=True):
    mat=bpy.data.materials.new(name); mat.use_nodes=True; nt=mat.node_tree; bsdf=nt.nodes['Principled BSDF']
    bsdf.inputs['Roughness'].default_value=0.55
    t=nt.nodes.new('ShaderNodeTexImage'); t.image=img
    c=nt.nodes.new('ShaderNodeAttribute'); c.attribute_name='skin_fill'
    m=nt.nodes.new('ShaderNodeAttribute'); m.attribute_name='fill_mask'
    mix=nt.nodes.new('ShaderNodeMix'); mix.data_type='RGBA'
    tc=nt.nodes.new('ShaderNodeAttribute'); tc.attribute_name='tone_corr'
    mul=nt.nodes.new('ShaderNodeMix'); mul.data_type='RGBA'; mul.blend_type='MULTIPLY'; mul.inputs['Factor'].default_value=1.0
    nt.links.new(t.outputs[0],mul.inputs[6]); nt.links.new(tc.outputs[0],mul.inputs[7])
    nt.links.new(m.outputs['Fac'],mix.inputs['Factor']); nt.links.new(mul.outputs[2],mix.inputs[6]); nt.links.new(c.outputs[0],mix.inputs[7])
    em=nt.nodes.new('ShaderNodeEmission'); nt.links.new(mix.outputs[2],em.inputs[0])
    outn=nt.nodes['Material Output']; nt.links.new(em.outputs[0],outn.inputs[0])   # emission: bake source = pure albedo
    return mat
me.materials.clear(); me.materials.append(skin_material('DonorBake'))
# close holes in the donor (open fabric edges) so bake rays never miss
import bmesh
bm=bmesh.new(); bm.from_mesh(me)
nb=len([e for e in bm.edges if e.is_boundary])
bmesh.ops.holes_fill(bm,edges=[e for e in bm.edges if e.is_boundary],sides=0)
bm.to_mesh(me); bm.free(); print('donor boundary edges filled',nb)
if os.environ.get('DEBUG_DONOR'):
    cam=rlib.setup_render(400,400,8); rlib.closeup(cam,out+'_donorhip',(0,0,0.92),0.55,views=(('front',0),('back',180))); sys.exit(0)

# ---------- 2. closed surface ----------
polys=B.tri_polys(donor.data); Vsrc=Wd
if upto>=1:
    polys=[p for p in polys if not all(cutv[i] for i in p)]
    extra_v=[];extra_f=[]; base=len(Vsrc)
    for side,J in HANDJ.items():
        for f,(dy,dx,L,w,h,spread) in H.FINGERS.items():
            fv,ff=H.finger_mesh(J[f],w,h); extra_f+= [tuple(i+base+sum(len(e) for e in extra_v) for i in fc) for fc in ff]; extra_v.append(fv)
    Vsrc=np.vstack([Vsrc]+extra_v); polys=polys+extra_f
rm=B.mesh_from_world('rm',Vsrc,polys); rm.data.remesh_voxel_size=VOXEL
if upto>=1:
    import bmesh as _bm
    bm=_bm.new(); bm.from_mesh(rm.data); _bm.ops.holes_fill(bm,edges=[e for e in bm.edges if e.is_boundary],sides=0); bm.to_mesh(rm.data); bm.free()
B.activate(rm); bpy.ops.object.voxel_remesh()
# drop internal shells the remesh leaves inside old fabric folds (they poke through after smoothing)
import bmesh, scipy.sparse.csgraph as cg
nc,lab=cg.connected_components(B.adjacency(rm)); keep=np.bincount(lab).argmax()
bm=bmesh.new(); bm.from_mesh(rm.data); bm.verts.ensure_lookup_table()
bmesh.ops.delete(bm,geom=[v for v in bm.verts if lab[v.index]!=keep],context='VERTS'); bm.to_mesh(rm.data); bm.free()
print('remesh components',nc,'kept',np.bincount(lab).max())
W=B.world_co(rm); A=B.adjacency(rm)
tree=cKDTree(Wd); _,nn=tree.query(W)
rbox=B.close_mask(A,boxer[nn],2); rvest=B.close_mask(A,vest[nn],2)
x,y,z=W[:,0],W[:,1],W[:,2]
def shell_inset(mask,thick,band=5):
    # fabric shells sit 'thick' proud of the skin: push the whole shell in, blend only across the edge band outside it
    global W
    out=B.ring_distance(A,~mask,band)        # 1..band rings outside the mask (0 inside / far)
    w=np.where(mask,1.0,np.where(out>0,1-np.clip(out/band,0,1),0.0))
    # smooth the weight so the transition has no kink
    import scipy.sparse as sp
    deg=np.asarray(A.sum(1)).ravel(); P=sp.diags(1/np.maximum(deg,1))@A
    for _ in range(6): w=0.5*w+0.5*(P@w)
    W=W-B.vertex_normals(rm,W)*(thick*w)[:,None]
    return B.dilate(A,mask,band+3)
def relax(region,iters,ramp=6,inset=0.0,mu=-0.53):
    global W
    d=B.ring_distance(A,region,ramp); wgt=np.clip(d/ramp,0,1)
    if inset: W=W-B.vertex_normals(rm,W)*(inset*wgt)[:,None]
    # stable form: hard-boundary Taubin on the core, then a diffusion-only blend across the edge band
    W=B.taubin(W,A,region&(wgt>=1),iters=iters,mu=mu)
    band=B.dilate(A,region&(wgt<1),2)
    W=B.laplacian(W,A,band,iters=max(10,ramp*3),weight=np.clip(B.ring_distance(A,band,4)/4,0,1))
# ---------- 3. legs: boxers -> bare hips ----------
edge=shell_inset(B.dilate(A,rbox,int(os.environ.get('BOXER_GROW','4'))),float(os.environ.get('BOXER_T','0.005')))
relax(edge,120,ramp=4)
# (leg-opening step is handled by the per-thigh radial rebuild below)
groin=(np.abs(x)<0.07)&(z>0.80)&(z<0.975)&(y<0.0)&B.dilate(A,rbox,6)
W=B.biharmonic_fill(W,A,B.dilate(A,groin,2))
relax(B.dilate(A,groin,8),40,ramp=8)
# thighs: the boxer leg band squeezed the thigh (step in radius). Rebuild the upper-thigh radius per angle
# from clean thigh below and hip above, around each thigh bone.
for side,sd in (('L',1),('R',-1)):
    p0=B.bone_world(arm,f'CC_Base_{side}_Thigh',donor); p1=B.bone_world(arm,f'CC_Base_{side}_Calf',donor)
    W,bz=B.radial_bridge(W,p0,p1,sd,band=(0.22,0.40),refs=[(0.10,0.18),(0.44,0.56)])
    print(side,'thigh p0',p0.round(3),'band verts',bz.sum())
# low-cutoff Taubin over the whole boxer zone: removes 1-2 cm fabric features (hems, seams, waistband), keeps glute/thigh form
relax(B.dilate(A,rbox,3)&(z<1.0),int(os.environ.get('LOWPASS_IT','300')),ramp=12,mu=-0.505)
# ---------- 4. neck: vest -> bare torso, clean neck/shoulder join ----------
if upto>=2:
    edge=shell_inset(B.dilate(A,rvest,int(os.environ.get('VEST_GROW','4'))),float(os.environ.get('VEST_T','0.004')))
    relax(edge,200,ramp=4)
    # neck/shoulder join: gentle extra relax across the trapezius line so the strap ridge and neckline lip vanish
    join=(z>1.40)&(z<1.56)&(np.abs(x)<0.20)
    relax(join,60,ramp=10)
if upto>=1:
    # back of the hand: the donor dorsum is as thick as the old fused finger block; taper it down to finger height
    # over the last 3.5 cm before the knuckles so the fingers do not emerge under a ledge
    for side,sg in (('L',1),('R',-1)):
        wr=B.bone_world(arm,f'CC_Base_{side}_Hand',donor); X=W[:,0]*sg
        zc=wr[2]+H.MCP_DZ; ztop=zc+0.0095
        t=np.clip((X-(wr[0]*sg+0.065))/0.035,0,1); t=t*t*(3-2*t)
        m=(X>wr[0]*sg+0.06)&(X<wr[0]*sg+0.125)&(W[:,2]>ztop)&(np.abs(W[:,1]-wr[1])<0.07)
        W[m,2]=W[m,2]-(W[m,2]-ztop)[...]*t[m]*0.8
    # knuckle junction: blend the cut donor palm into the new finger roots
    for side,J in HANDJ.items():
        mc=np.array([J[f][0] for f in ('Index','Mid','Ring','Pinky')])
        dmin=np.min(np.linalg.norm(W[:,None,:]-mc[None,:,:],axis=2),axis=1)
        relax(dmin<0.032,80,ramp=5)
B.set_world_co(rm,W)
if os.environ.get('DEBUG_HI'):
    donor.hide_render=True; rm.data.materials.append(bpy.data.materials.new('c'))
    for f in rm.data.polygons: f.use_smooth=True
    cam=rlib.setup_render(400,400,8); rlib.closeup(cam,out+'_hi',(0,0,0.92),0.55,views=(('front',0),('side',90),('back',180))); sys.exit(0)

# ---------- 5. decimate (symmetric) ----------
B.activate(rm)
dec=rm.modifiers.new('dec','DECIMATE'); dec.ratio=TARGET_TRIS/ (2*len(rm.data.vertices)); dec.use_symmetry=True; dec.symmetry_axis='X'
bpy.ops.object.modifier_apply(modifier='dec')
bpy.ops.object.shade_smooth()
tris=sum(len(p.vertices)-2 for p in rm.data.polygons); print('LOD0 tris',tris,'verts',len(rm.data.vertices))

# ---------- 6. weights from the AccuRig donor ----------
for g in donor.vertex_groups: rm.vertex_groups.new(name=g.name)
dt=rm.modifiers.new('dt','DATA_TRANSFER'); dt.object=donor; dt.use_vert_data=True; dt.data_types_verts={'VGROUP_WEIGHTS'}
dt.vert_mapping='POLYINTERP_NEAREST'; dt.layers_vgroup_select_src='ALL'; dt.layers_vgroup_select_dst='NAME'
# donor is in armature space; transfer in world space
dt.use_object_transform=True
bpy.ops.object.modifier_apply(modifier='dt')
if upto>=1:
    P=B.world_co(rm)
    for side,sg in (('L',1),('R',-1)):
        wrx=B.bone_world(arm,f'CC_Base_{side}_Hand',donor)[0]*sg
        X=P[:,0]*sg; idx=np.where(X>wrx+0.005)[0]
        bl=np.clip((X[idx]-(wrx+0.005))/0.025,0,1); bl=bl*bl*(3-2*bl)   # blend donor wrist weights -> procedural hand weights
        Wh=H.hand_weights(P[idx],HANDJ[side],side,wrx)
        vs=rm.data.vertices; gl=list(rm.vertex_groups)
        for k,i in enumerate(idx):
            for ge in list(vs[int(i)].groups): gl[ge.group].add([int(i)],ge.weight*(1-bl[k]),'REPLACE')
        for name,w in Wh.items():
            g=rm.vertex_groups.get(name) or rm.vertex_groups.new(name=name)
            for k,i in enumerate(idx):
                if w[k]*bl[k]>1e-4: g.add([int(i)],float(w[k]*bl[k]),'ADD')
bpy.ops.object.vertex_group_limit_total(group_select_mode='ALL',limit=4)
bpy.ops.object.vertex_group_normalize_all(group_select_mode='ALL',lock_active=False)

# ---------- 7. UV + bake base colour ----------
bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(60),island_margin=0.004); bpy.ops.object.mode_set(mode='OBJECT')
RES=2048; bimg=bpy.data.images.new(f'{NAME}Bare_BaseColor',RES,RES); 
bmat=bpy.data.materials.new(f'{NAME}BareSkin'); bmat.use_nodes=True; nt=bmat.node_tree
tn=nt.nodes.new('ShaderNodeTexImage'); tn.image=bimg; nt.nodes.active=tn
nt.links.new(tn.outputs[0],nt.nodes['Principled BSDF'].inputs['Base Color']); nt.nodes['Principled BSDF'].inputs['Roughness'].default_value=0.55
rm.data.materials.clear(); rm.data.materials.append(bmat)
sc=bpy.context.scene; sc.render.engine='CYCLES'; sc.cycles.device='CPU'; sc.cycles.samples=1
donor.hide_render=False
for o in sc.objects: o.select_set(False)
donor.select_set(True); rm.select_set(True); bpy.context.view_layer.objects.active=rm
# donor is posed by its armature modifier; bake against its rest shape
for md in donor.modifiers: md.show_viewport=md.show_render=False
sc.render.bake.use_selected_to_active=True; sc.render.bake.margin=8
def bake(extr,ray):
    sc.render.bake.cage_extrusion=extr; sc.render.bake.max_ray_distance=ray
    bpy.ops.object.bake(type='EMIT'); return np.array(bimg.pixels[:]).reshape(RES,RES,4).copy()
p1=bake(0.02,0.05)
# smoothed regions can sit >2 cm inside the old fabric: rebake with longer rays only where the first bake missed (pure black)
from scipy.ndimage import binary_dilation
miss=binary_dilation(p1[:,:,:3].sum(2)<0.03,iterations=3)
if miss.any():
    p2=bake(0.06,0.12); p1[miss]=p2[miss]
print('bake misses refilled',int(miss.sum()))
bimg.pixels.foreach_set(p1.astype(np.float32).ravel())
bimg.filepath_raw=out+'_BaseColor.png'; bimg.file_format='PNG'; bimg.save()
donor.hide_render=True; donor.hide_viewport=True

# ---------- 8a. finger rig fix ----------
if upto>=1:
    import mathutils
    Mi=np.linalg.inv(np.array(donor.matrix_world))     # world -> armature/donor local (same space)
    toL=lambda p: mathutils.Vector((Mi[:3,:3]@p+Mi[:3,3]).tolist())
    B.activate(arm); bpy.ops.object.mode_set(mode='EDIT'); eb=arm.data.edit_bones
    for side,J in HANDJ.items():
        for dg,pts in J.items():
            for i in range(3):
                b=eb[f'CC_Base_{side}_{dg}{i+1}']; b.use_connect=False
            for i in range(3):
                b=eb[f'CC_Base_{side}_{dg}{i+1}']; b.head=toL(pts[i]); b.tail=toL(pts[i+1])
                b.align_roll(mathutils.Vector((0,0,1)))
    bpy.ops.object.mode_set(mode='OBJECT')
    print('finger bones moved')
# ---------- 8. rig + modular split ----------
rm.name=f'{NAME}Bare_Body'; rm.data.name=rm.name
# The MINI-105 import is inconsistent: the mesh stands upright but the armature OBJECT carries a +90 deg X
# rotation, so bones lie on their back relative to the mesh. Rest pose hides it; any bone rotation swings
# vertices around a pivot in the wrong space (posing explodes, donor included). Fix: mesh local space already
# equals armature local space (Z-up, cm), so drop the armature object's rotation and parent the mesh with an
# identity transform -> bones and skin share one upright frame.
import mathutils
Mi=np.linalg.inv(np.array(donor.matrix_world)); Wl=B.world_co(rm)@Mi[:3,:3].T+Mi[:3,3]
rm.data.vertices.foreach_set('co',Wl.ravel()); rm.data.update()
arm.matrix_world=mathutils.Matrix.Diagonal((0.01,0.01,0.01,1.0))
rm.parent=arm; rm.matrix_parent_inverse=mathutils.Matrix.Identity(4); rm.matrix_basis=mathutils.Matrix.Identity(4)
bpy.context.view_layer.update()
am=rm.modifiers.new('Armature','ARMATURE'); am.object=arm
# keep one smooth normal field across piece seams
rm.data.set_sharp_from_angle(angle=math.pi) if hasattr(rm.data,'set_sharp_from_angle') else None
PIECES={  # piece: bone-name prefixes (dominant weight decides)
 'Head':['Head','Facial','Jaw','Tongue','Teeth','Eye','UpperJaw','NeckTwist02'],
 'Torso':['Hip','Pelvis','Waist','Spine','NeckTwist01','Clavicle'],
 'Arms':['Upperarm','Forearm','Elbow'],
 'Hands':['Hand','Thumb','Index','Mid','Ring','Pinky'],
 'Legs':['Thigh','Calf','Knee'],
 'Feet':['Foot','Toe','BigToe','IndexToe','MidToe','RingToe','PinkyToe'],
}
def piece_of(bone):
    b=bone.replace('CC_Base_','').replace('L_','').replace('R_','')
    for p,keys in PIECES.items():
        for k in keys:
            if b.startswith(k): 
                if p=='Hands' and b.startswith('Mid') and 'Toe' in b: continue
                return p
    return 'Torso'
gname={g.index:g.name for g in rm.vertex_groups}
vp=[]
for vtx in rm.data.vertices:
    best=max(vtx.groups,key=lambda g:g.weight,default=None)
    vp.append(piece_of(gname[best.group]) if best else 'Torso')
vp=np.array(vp)
# freeze normals before splitting so seams stay invisible
B.activate(rm)
rm.data.update()
nrm=np.zeros(len(rm.data.loops)*3); rm.data.loops.foreach_get('normal',nrm) if hasattr(rm.data.loops[0],'normal') else None
rm.data.normals_split_custom_set(nrm.reshape(-1,3).tolist())
fp=np.array([vp[p.vertices[0]] for p in rm.data.polygons])  # face -> piece of its first vertex (majority below)
for i,p in enumerate(rm.data.polygons):
    vals,cnt=np.unique(vp[list(p.vertices)],return_counts=True); fp[i]=vals[cnt.argmax()]
counts={}
import bmesh
for name in PIECES:
    o=rm.copy(); o.data=rm.data.copy(); o.name=f'{NAME}Bare_{name}'; o.data.name=o.name; sc.collection.objects.link(o)
    bm=bmesh.new(); bm.from_mesh(o.data)
    bmesh.ops.delete(bm,geom=[f for f,pp in zip(bm.faces,fp) if pp!=name],context='FACES')
    bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
    bm.to_mesh(o.data); bm.free()
    counts[name]=sum(len(p.vertices)-2 for p in o.data.polygons)
print('pieces',counts)
sc.collection.objects.unlink(rm)
bpy.ops.wm.save_as_mainfile(filepath=out+'.blend',compress=True)
# ---------- 9. FBX ----------
for o in sc.objects: o.select_set(o.type in ('ARMATURE','MESH') and o.name!=donor.name and not o.hide_viewport)
donor.select_set(False)
bpy.ops.export_scene.fbx(filepath=out+'.fbx',use_selection=True,object_types={'ARMATURE','MESH'},
    apply_unit_scale=True,bake_space_transform=False,add_leaf_bones=False,use_armature_deform_only=False,
    use_mesh_modifiers=True,mesh_smooth_type='OFF',bake_anim=False,path_mode='STRIP')
# ---------- 10. renders ----------
for o in list(sc.objects):
    if o.type=='LIGHT' or o.type=='CAMERA': bpy.data.objects.remove(o)
arm.data.pose_position='REST'
cam=rlib.setup_render(400,720,24); rlib.turntable(cam,0.93,1.9,out)
sc.render.resolution_y=400; rlib.closeup(cam,out+'_hip',(0,0,0.92),0.55)
rlib.closeup(cam,out+'_shoulder',(0,0,1.40),0.55)
rlib.closeup(cam,out+'_hand',(0.80,0,1.47),0.30,views=(('front',0),('back',180)))
cam.location=(0.83,0.015,2.15); cam.rotation_euler=(0,0,0); sc.render.filepath=out+'_hand_top.png'; bpy.ops.render.render(write_still=True)
cam.location=(0.83,0.015,0.79); cam.rotation_euler=(math.pi,0,0); sc.render.filepath=out+'_hand_palm.png'; bpy.ops.render.render(write_still=True)
clay=bpy.data.materials.new('clay'); clay.diffuse_color=(0.6,0.6,0.62,1)
for o in sc.objects:
    if o.type=='MESH' and not o.hide_render: o.data.materials[0]=clay
rlib.closeup(cam,out+'_clayhip',(0,0,0.92),0.55); rlib.closeup(cam,out+'_clayshoulder',(0,0,1.40),0.55)
rlib.closeup(cam,out+'_clayhand',(0.80,0,1.47),0.30,views=(('front',0),('back',180)))
cam.location=(0.83,0.015,2.15); cam.rotation_euler=(0,0,0); sc.render.filepath=out+'_clayhand_top.png'; bpy.ops.render.render(write_still=True)
cam.location=(0.83,0.015,0.79); cam.rotation_euler=(math.pi,0,0); sc.render.filepath=out+'_clayhand_palm.png'; bpy.ops.render.render(write_still=True)
