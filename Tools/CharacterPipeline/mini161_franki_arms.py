"""Retain source sleeve surface and weights; repaint instead of deleting arms."""
import bpy,bmesh,os,ast,json
import numpy as np
ROOT=r'E:\Unity\Up Iz Up Mini'
OUT=ROOT+'/Logs/Tasks/MINI-161';os.makedirs(OUT,exist_ok=True)
ART=ROOT+'/Assets/UpIzUpMini/Art/Characters/Garments'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=ROOT+'/Assets/UpIzUpMini/Art/Characters/Strong.fbx')
for o in bpy.data.objects:o.animation_data_clear()
bpy.context.scene.frame_set(0);bpy.context.view_layer.update()
arm=next(o for o in bpy.data.objects if o.type=='ARMATURE')
def bone(n):
 b=arm.data.bones['mixamorig10:'+n];return arm.matrix_world@b.head_local,arm.matrix_world@b.tail_local
a,b=bone('LeftArm');cut=a.x+.45*(b.x-a.x)
a,b=bone('Neck');neck=a.z+.82*(b.z-a.z)
body=bpy.data.objects['Ch28_Body'];shirt=bpy.data.objects['Ch28_Hoody']
img=bpy.data.images.load(ROOT+'/Assets/UpIzUpMini/Art/Characters/Ch28_1001_Diffuse.png')
w,h=img.size;pixels=np.array(img.pixels[:],dtype=np.float32).reshape(h,w,4)
head=next(g.index for g in body.vertex_groups if g.name.endswith(':Head'))
uv=body.data.uv_layers.active.data;samples=[]
for p in body.data.polygons:
 for li in p.loop_indices:
  v=body.data.vertices[body.data.loops[li].vertex_index]
  if any(g.group==head and g.weight>.6 for g in v.groups):
   u,t=uv[li].uv;samples.append(pixels[min(h-1,max(0,int(t*h))),min(w-1,max(0,int(u*w))),:3])
assert len(samples)>20
skin=np.median(np.array(samples),axis=0)
tree=ast.parse(open(ROOT+'/Tools/CharacterPipeline/mini157_sacat_garment.py',encoding='utf-8-sig').read())
fn=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=='rasterize_tri')
exec(compile(ast.Module(body=[fn],type_ignores=[]),'<MINI157 rasterizer>','exec'))
bm=bmesh.new();bm.from_mesh(shirt.data)
for point,normal in [((cut/.01,0,0),(1,0,0)),((-cut/.01,0,0),(1,0,0)),((0,neck/.01,0),(0,1,0))]:
 bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),plane_co=point,plane_no=normal,clear_inner=False,clear_outer=False)
bmesh.ops.triangulate(bm,faces=list(bm.faces))
bm.to_mesh(shirt.data);bm.free();shirt.data.update()
me=shirt.data;uv=me.uv_layers.active.data;counts={'skin':0,'shirt':0}
for p in me.polygons:
 c=shirt.matrix_world@p.center
 reveal=abs(c.x)>cut or c.z>neck
 color=skin if reveal else (.09,.16,.32);counts['skin' if reveal else 'shirt']+=1
 uvs=[uv[i].uv[:] for i in p.loop_indices]
 for i in range(1,len(uvs)-1):rasterize_tri(uvs[0],uvs[i],uvs[i+1],color)
img.pixels[:]=pixels.flatten();img.filepath_raw=ART+'/Franki_ArmsRestored.png';img.file_format='PNG';img.save()
mat=bpy.data.materials.new('Franki_ArmsRestored');mat.use_nodes=True
bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.85
tx=mat.node_tree.nodes.new('ShaderNodeTexImage');tx.image=img;mat.node_tree.links.new(tx.outputs['Color'],bs.inputs['Base Color'])
shirt.data.materials.clear();shirt.data.materials.append(mat)
for p in me.polygons:p.material_index=0
bpy.ops.object.select_all(action='DESELECT');shirt.select_set(True);arm.select_set(True)
bpy.ops.export_scene.fbx(filepath=ART+'/Franki_ArmsRestored.fbx',use_selection=True,add_leaf_bones=False,bake_anim=False,mesh_smooth_type='FACE',path_mode='AUTO')
bpy.ops.wm.save_as_mainfile(filepath=OUT+'/Franki-Arms-Restored.blend')
with open(OUT+'/source-audit.json','w') as f:json.dump({'skin':skin.tolist(),'faces':counts,'sleeveCut':cut,'neckCut':neck,'vertices':len(me.vertices),'deletedFaces':0},f,indent=2)
print('MINI161_SOURCE_PASS',counts,skin)
