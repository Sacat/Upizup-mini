"""Local upper-back projection; preserve repaired arms and original rig weights."""
import bpy,os,shutil,json,bmesh
ROOT=r'E:\Unity\Up Iz Up Mini'
OUT=ROOT+'/Logs/Tasks/MINI-162';os.makedirs(OUT,exist_ok=True)
FBX=ROOT+'/Assets/UpIzUpMini/Art/Characters/Garments/Franki_ArmsRestored.fbx'
backup=OUT+'/Franki_ArmsRestored-before-hood.fbx'
if not os.path.exists(backup):shutil.copy2(FBX,backup)
bpy.ops.wm.open_mainfile(filepath=ROOT+'/Logs/Tasks/MINI-161/Franki-Arms-Restored.blend')
o=bpy.data.objects['Ch28_Hoody'];inv=o.matrix_world.inverted();changed=0
arm_before=[(v.index,tuple(v.co)) for v in o.data.vertices if abs((o.matrix_world@v.co).x)>.25]
for v in o.data.vertices:
 p=o.matrix_world@v.co
 if p.z<1.29 or p.y<0 or abs(p.x)>.20:continue
 # Blend smoothly from broad upper back toward the narrower neck.
 t=max(0,min(1,(p.z-1.30)/.23));t=t*t*(3-2*t)
 cap=.116*(1-t)+.042*t
 side=max(0,min(1,(.20-abs(p.x))/.07));side=side*side*(3-2*side)
 if p.y>cap:
  p.y-=(p.y-cap)*side*.92;v.co=inv@p;changed+=1
assert all(tuple(o.data.vertices[i].co)==co for i,co in arm_before),'arm vertices changed'
bm=bmesh.new();bm.from_mesh(o.data)
region=[v for v in bm.verts if (o.matrix_world@v.co).z>1.30 and (o.matrix_world@v.co).y>0 and abs((o.matrix_world@v.co).x)<.18]
for _ in range(35):bmesh.ops.smooth_vert(bm,verts=region,factor=.45,use_axis_x=True,use_axis_y=True,use_axis_z=True)
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
arm=next(a for a in bpy.data.objects if a.type=='ARMATURE')
bpy.ops.object.select_all(action='DESELECT');o.select_set(True);arm.select_set(True)
bpy.ops.export_scene.fbx(filepath=FBX,use_selection=True,add_leaf_bones=False,bake_anim=False,mesh_smooth_type='FACE',path_mode='AUTO')
bpy.ops.wm.save_as_mainfile(filepath=OUT+'/Franki-Shirt-NoHood.blend')
with open(OUT+'/audit.json','w') as f:json.dump({'rearVerticesReshaped':changed,'armVerticesUnchanged':len(arm_before),'facesDeleted':0},f,indent=2)
print('MINI162_LOCAL_HOOD_PASS',changed)
