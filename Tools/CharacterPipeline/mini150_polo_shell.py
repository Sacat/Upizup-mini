"""Continuous polo blockout; source and gameplay assets are read-only."""
import bpy,bmesh,math,os,json
from mathutils import Vector
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'../..'))
OUT=os.path.join(ROOT,'Logs/Tasks/MINI-150');os.makedirs(OUT,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=os.path.join(ROOT,'Docs/CharacterPipeline/MINI-105/AccuRigOutput/Sacat-ModularBase-Rigged-VisualProof.blend'))
body=next(o for o in bpy.context.scene.objects if o.type=='MESH' and o.find_armature())
def mat(name,col):
 m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*col,1);p.inputs['Roughness'].default_value=.85
 return m
navy=mat('Navy polo',(.006,.014,.035));white=mat('Embroidery',(.75,.75,.71))
parts=[]
def rings(name,rows,axis):
 vs=[];fs=[];n=48
 for a,r1,r2 in rows:
  for i in range(n):
   t=i*2*math.pi/n
   vs.append((r1*math.cos(t),r2*math.sin(t)+.012,a) if axis=='Z' else (a,r1*math.cos(t)+.035,1.49+r2*math.sin(t)))
 for j in range(len(rows)-1):
  for i in range(n):
   k=j*n+i;l=j*n+(i+1)%n;fs.append((k,l,l+n,k+n))
 fs.extend([tuple(reversed(range(n))),tuple(range((len(rows)-1)*n,len(rows)*n))])
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update()
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
 o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);parts.append(o);return o
shirt=rings('Polo shell',[(.965,.193,.151),(1.03,.190,.151),(1.14,.174,.15),(1.26,.18,.154),(1.38,.215,.16),(1.47,.25,.152),(1.53,.235,.123),(1.585,.078,.077)],'Z')
for s in [-1,1]:rings('Short sleeve',[(s*.16,.13,.115),(s*.29,.12,.108),(s*.405,.10,.094)],'X')
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=shirt;bpy.ops.object.join()
rm=shirt.modifiers.new('Union continuous shoulder seams','REMESH');rm.mode='VOXEL';rm.voxel_size=.008;rm.use_smooth_shade=True;bpy.ops.object.modifier_apply(modifier=rm.name)
sm=shirt.modifiers.new('Cloth smoothing','SMOOTH');sm.factor=.65;sm.iterations=7;bpy.ops.object.modifier_apply(modifier=sm.name)
# Open genuine neck, hem and sleeve cuffs after merging.
bm=bmesh.new();bm.from_mesh(shirt.data)
bad=[f for f in bm.faces if f.calc_center_median().z<.978 or f.calc_center_median().z>1.571 or abs(f.calc_center_median().x)>.394]
bmesh.ops.delete(bm,geom=bad,context='FACES');bm.to_mesh(shirt.data);bm.free()
de=shirt.modifiers.new('Preview topology budget','DECIMATE');de.ratio=min(1,2200/max(1,sum(len(p.vertices)-2 for p in shirt.data.polygons)));bpy.ops.object.modifier_apply(modifier=de.name)
shirt.data.materials.append(navy)
for p in shirt.data.polygons:p.use_smooth=True
so=shirt.modifiers.new('Sewn cloth thickness','SOLIDIFY');so.thickness=.002
# Mask only the covered part on an evaluated disposable copy, never the source.
ev=body.evaluated_get(bpy.context.evaluated_depsgraph_get());me=bpy.data.meshes.new_from_object(ev)
copy=bpy.data.objects.new('Body with covered region hidden - preview only',me);bpy.context.collection.objects.link(copy);copy.matrix_world=body.matrix_world.copy();body.hide_render=True
bm=bmesh.new();bm.from_mesh(me)
covered=[]
for f in bm.faces:
 c=copy.matrix_world@f.calc_center_median()
 if .99<c.z<1.56 and abs(c.x)<.373:covered.append(f)
bmesh.ops.delete(bm,geom=covered,context='FACES');bm.to_mesh(me);bm.free()
def panel(name,vs):
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],[tuple(range(len(vs)))]);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);o.data.materials.append(navy)
 so=o.modifiers.new('Fold thickness','SOLIDIFY');so.thickness=.003
 be=o.modifiers.new('Soft fold','BEVEL');be.width=.003;be.segments=3
 return o
for s in [-1,1]:panel('Polo folded collar',[(s*.012,-.07,1.573),(s*.078,-.045,1.58),(s*.123,-.112,1.535),(s*.055,-.151,1.495)])
panel('Button placket',[(-.012,-.153,1.53),(.012,-.153,1.53),(.012,-.154,1.397),(-.012,-.154,1.397)])
for z in [1.475,1.425]:
 bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=6,radius=.0035,location=(0,-.157,z));bpy.context.object.scale.y=.4;bpy.context.object.data.materials.append(white)
tx=bpy.data.curves.new('Lacos','FONT');tx.body='LACOS';tx.size=.018;tx.extrude=.0001
o=bpy.data.objects.new('Lacos',tx);bpy.context.collection.objects.link(o);o.location=(.07,-.153,1.385);o.rotation_euler=(math.pi/2,0,0);o.data.materials.append(white)
scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE';scene.render.resolution_x=800;scene.render.resolution_y=900;scene.render.resolution_percentage=100
cam=scene.camera;cam.data.type='ORTHO';cam.data.ortho_scale=1.28;target=Vector((0,0,1.31))
for name,offset in [('Front',(0,-4,.03)),('Back',(0,4,.03)),('ThreeQuarter',(2,-4,.05))]:
 cam.location=target+Vector(offset);cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=os.path.join(OUT,'Polo-'+name+'.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'Polo-Continuous-Prototype.blend'))
with open(os.path.join(OUT,'audit.json'),'w') as f:json.dump({'shellTriangles':sum(len(p.vertices)-2 for p in shirt.data.polygons),'rigged':False,'gameChanged':False,'coveredFacesHidden':len(covered)},f,indent=2)
print('MINI150_STATIC_PROTOTYPE_COMPLETE')
