import bpy,math,numpy as np,json,os
from mathutils import Vector,Matrix
bpy.ops.wm.open_mainfile(filepath='tmax/build_c.blend')
O=bpy.data.objects
for o in list(O):
    if o.type!='MESH': bpy.data.objects.remove(o)
RAKE=math.radians(25)
FA=(0,-0.7875,0.3042); RA=(0,0.7875,0.2537); BY,BZ=-0.13,1.09
def link(o): bpy.context.scene.collection.objects.link(o); return o
def empty(name,p,parent,size=0.04):
    e=link(bpy.data.objects.new(name,None)); e.empty_display_type='PLAIN_AXES'; e.empty_display_size=size
    e.matrix_world=Matrix.Translation(Vector(p)); 
    if parent: e.parent=parent; e.matrix_parent_inverse=parent.matrix_world.inverted()
    return e
def reframe(o,M):
    """keep the mesh where it is in the world, but give the object the world matrix M (pivot + orientation)."""
    W=o.matrix_world.copy(); o.data.transform(M.inverted()@W); o.matrix_world=M
root=link(bpy.data.objects.new('TMAX_560_Rebuilt',None)); root.empty_display_type='ARROWS'
parts={n:O[n] for n in ('TMAX_Body','TMAX_FrontWheel','TMAX_RearWheel','TMAX_FrontForkAssembly','TMAX_Handlebar')}
fk,hb=parts['TMAX_FrontForkAssembly'],parts['TMAX_Handlebar']
reframe(fk,Matrix.Translation(fk.location.copy())@Matrix.Rotation(-RAKE,4,'X'))   # local +Z (-> Unity +Y) = steering axis
reframe(hb,Matrix.Translation(hb.location.copy())@Matrix.Rotation(-RAKE,4,'X'))
for o in parts.values():
    o.parent=root; tm=o.modifiers.new('tri','TRIANGULATE'); tm.ngon_method='CLIP'; tm.quad_method='SHORTEST_DIAGONAL'; bpy.context.view_layer.objects.active=o; bpy.ops.object.modifier_apply(modifier='tri')
    import bmesh as _b; bm=_b.new(); bm.from_mesh(o.data); seen={}; dup=[]
    for f in bm.faces:
        k=tuple(sorted(v.index for v in f.verts))
        if k in seen: dup+= [f,seen[k]]
        else: seen[k]=f
    for e in bm.edges:
        if len(e.link_faces)>2: print('NONMANIFOLD edge',o.name,[tuple(round(c,3) for c in f.calc_center_median()) for f in e.link_faces],[len(f.verts) for f in e.link_faces])
    bad=[e for e in bm.edges if len(e.link_faces)>2]
    if bad:
        fs=list({f for e in bad for f in e.link_faces}); _b.ops.delete(bm,geom=fs,context='FACES')
        _b.ops.holes_fill(bm,edges=[e for e in bm.edges if e.is_boundary],sides=12); _b.ops.triangulate(bm,faces=bm.faces[:])
        print('repaired',o.name,'boundary left',sum(1 for e in bm.edges if e.is_boundary),'nonman left',sum(1 for e in bm.edges if len(e.link_faces)>2))
    if dup: _b.ops.delete(bm,geom=list(set(dup)),context='FACES_ONLY' if False else 'FACES'); print('removed duplicate faces',o.name,len(set(dup)))
    bm.to_mesh(o.data); bm.free()
A={'Seat_Driver':(0,0.40,0.770),'Seat_Pillion':(0,0.78,0.852),'FootPeg_L':(0.19,-0.12,0.47),'FootPeg_R':(-0.19,-0.12,0.47),
   'PillionPeg_L':(0.27,0.40,0.34),'PillionPeg_R':(-0.27,0.40,0.34),'PillionHandle_L':(0.17,0.80,0.82),'PillionHandle_R':(-0.17,0.80,0.82),
   'FrontAxle':FA,'RearAxle':RA}
for k,p in A.items(): empty(k,p,root)
empty('GripLeft',(0.31,BY+0.027,BZ+0.001),hb,0.03); empty('GripRight',(-0.31,BY+0.027,BZ+0.001),hb,0.03)
def tris(o): return sum(len(p.vertices)-2 for p in o.data.polygons)
def watertight(o):
    me=o.data; lt=np.zeros(len(me.loops),np.int32); me.loops.foreach_get('edge_index',lt); c=np.bincount(lt,minlength=len(me.edges)); return bool((c==1).sum()==0 and (c>2).sum()==0)
def bounds(o):
    co=np.array([o.matrix_world@v.co for v in o.data.vertices]); return [np.round(co.min(0),4).tolist(),np.round(co.max(0),4).tolist()]
def unity(p): return [round(-p[0],4),round(p[2],4),round(-p[1],4)]     # Blender (front -Y) -> Unity (x right, y up, z forward)
os.makedirs('tmax/out',exist_ok=True)
def export(path,objs):
    for x in bpy.context.view_layer.objects: x.select_set(False)
    for o in objs: o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=path,use_selection=True,object_types={'EMPTY','MESH'},apply_unit_scale=True,bake_space_transform=False,
        use_mesh_modifiers=True,mesh_smooth_type='FACE',use_tspace=True,bake_anim=False,path_mode='STRIP',add_leaf_bones=False)
allobjs=[root]+list(root.children_recursive)
export(os.path.abspath('tmax/out/TMAX_560_Rebuilt.fbx'),allobjs)
rep={'units':'metres','blender_frame':'front -Y, up +Z, bike left +X','unity_frame':'front +Z, up +Y, bike right +X (FBX default axis conversion)',
     'origin':'on the ground, midway between the axles','wheelbase_m':1.575,'scale_note':'scan scaled so the wheelbase is the real TMAX 560 value (1.575 m); overall length 2.225 m, height 1.505 m with the screen',
     'parts':{},'empties':{},'lod0_total_tris':0}
for n,o in parts.items():
    rep['parts'][n]={'tris':tris(o),'watertight':watertight(o),'pivot_blender':[round(x,4) for x in o.matrix_world.translation],'pivot_unity':unity(o.matrix_world.translation),
                     'bounds_blender':bounds(o),'material':o.data.materials[0].name}
    rep['lod0_total_tris']+=tris(o)
rep['parts']['TMAX_FrontForkAssembly']['local_up_is_steering_axis']='rake 25 deg (top leans back); in Unity rotate about local Y to steer'
rep['parts']['TMAX_Handlebar']['local_up_is_steering_axis']='own pivot at the bar clamp, axis parallel to the rake; steer by the same angle as the fork'
rep['parts']['TMAX_FrontWheel']['radius_m']=0.3042; rep['parts']['TMAX_RearWheel']['radius_m']=0.2537
for e in [x for x in bpy.data.objects if x.type=='EMPTY' and x!=root]:
    rep['empties'][e.name]={'blender':[round(x,4) for x in e.matrix_world.translation],'unity':unity(e.matrix_world.translation),'parent':e.parent.name}
# LOD1
lod=[]
L1={'TMAX_Body':7000,'TMAX_FrontWheel':1200,'TMAX_RearWheel':1100,'TMAX_FrontForkAssembly':700,'TMAX_Handlebar':450}
root1=link(bpy.data.objects.new('TMAX_560_Rebuilt_LOD1',None))
rep['lod1']={}
for n,o in parts.items():
    c=o.copy(); c.data=o.data.copy(); c.name=n+'_LOD1'; link(c); c.parent=None; c.matrix_world=o.matrix_world.copy()
    t=tris(c)
    if t>L1[n]:
        d=c.modifiers.new('d','DECIMATE'); d.ratio=L1[n]/t; bpy.context.view_layer.objects.active=c; bpy.ops.object.modifier_apply(modifier='d')
    M=c.matrix_world.copy(); c.parent=root1; c.matrix_world=M; lod.append(c); rep['lod1'][c.name]=tris(c)
rep['lod1_total_tris']=sum(rep['lod1'].values())
export(os.path.abspath('tmax/out/TMAX_560_Rebuilt_LOD1.fbx'),[root1]+lod)
json.dump(rep,open('tmax/out/parts_report.json','w'),indent=1)
bpy.ops.wm.save_as_mainfile(filepath=os.path.abspath('tmax/out/TMAX_560_Rebuilt.blend'),compress=True)
print(json.dumps({k:rep[k] for k in ('lod0_total_tris','lod1_total_tris')}), {n:(p['tris'],p['watertight']) for n,p in rep['parts'].items()})
