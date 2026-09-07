"""Isolated polo fitting prototype. Never writes source body or Unity Assets."""
import bpy, os, json, math
from mathutils import Vector
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'../..'))
OUT=os.path.join(ROOT,'Logs/Tasks/MINI-148')
os.makedirs(OUT,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=os.path.join(ROOT,'Docs/CharacterPipeline/MINI-105/AccuRigOutput/Sacat-ModularBase-Rigged-VisualProof.blend'))
body=next(o for o in bpy.context.scene.objects if o.type=='MESH' and o.find_armature())
dg=bpy.context.evaluated_depsgraph_get()
ev=body.evaluated_get(dg); me=ev.to_mesh()
points=[ev.matrix_world@v.co for v in me.vertices]
ev.to_mesh_clear()
lo=Vector(tuple(min(v[i] for v in points) for i in range(3)))
hi=Vector(tuple(max(v[i] for v in points) for i in range(3)))
height=hi.z-lo.z
slices=[]
for f in [.48,.52,.56,.60,.64,.68,.72,.76,.80,.84]:
    ps=[p for p in points if abs(p.z-(lo.z+height*f))<height*.008 and abs(p.x)<height*.16]
    slices.append({'height':f,'bounds':[[min(p[i] for p in ps),max(p[i] for p in ps)] for i in range(3)] if ps else []})
with open(os.path.join(OUT,'body-fit-measurements.json'),'w') as f:json.dump({'low':list(lo),'high':list(hi),'height':height,'slices':slices},f,indent=2)
print('MINI148_BODY_MEASURED',list(lo),list(hi),height)

# Prototype shell follows the measured modest body; source data remains untouched.
ev=body.evaluated_get(dg); src=ev.to_mesh()
selected=[]
for p in src.polygons:
    c=sum((points[i] for i in p.vertices),Vector())/len(p.vertices)
    if .975<c.z<1.59 and abs(c.x)<.41 and not(c.z>1.525 and abs(c.x)<.078):selected.append(p)
used=sorted({i for p in selected for i in p.vertices}); mapping={old:i for i,old in enumerate(used)}
verts=[]
for i in used:
    p=points[i].copy(); n=ev.matrix_world.to_3x3()@src.vertices[i].normal
    p+=n.normalized()*.015
    verts.append(p)
mesh=bpy.data.meshes.new('PoloShell');mesh.from_pydata(verts,[],[[mapping[i] for i in p.vertices] for p in selected]);mesh.update()
ev.to_mesh_clear()
shirt=bpy.data.objects.new('Lacos_Polo_FittingPrototype',mesh);bpy.context.collection.objects.link(shirt)
for g in body.vertex_groups:shirt.vertex_groups.new(name=g.name)
for old,new in mapping.items():
    for g in body.data.vertices[old].groups:shirt.vertex_groups[g.group].add([new],g.weight,'REPLACE')
bpy.ops.object.select_all(action='DESELECT');shirt.select_set(True);bpy.context.view_layer.objects.active=shirt
dec=shirt.modifiers.new('Prototype reduction','DECIMATE');dec.ratio=min(1,2400/max(1,sum(len(p.vertices)-2 for p in mesh.polygons)))
bpy.ops.object.modifier_apply(modifier=dec.name)
sm=shirt.modifiers.new('Relax cloth','SMOOTH');sm.factor=.65;sm.iterations=5;bpy.ops.object.modifier_apply(modifier=sm.name)
for p in shirt.data.polygons:p.use_smooth=True
def material(name,col):
    m=bpy.data.materials.new(name);m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*col,1);bs.inputs['Roughness'].default_value=.78
    tex=m.node_tree.nodes.new('ShaderNodeTexNoise');tex.inputs['Scale'].default_value=650
    bump=m.node_tree.nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.14;bump.inputs['Distance'].default_value=.0007
    m.node_tree.links.new(tex.outputs['Fac'],bump.inputs['Height']);m.node_tree.links.new(bump.outputs['Normal'],bs.inputs['Normal'])
    return m
navy=material('Polo Navy Pique',(.016,.035,.065));shirt.data.materials.append(navy)
solid=shirt.modifiers.new('Cloth edge thickness','SOLIDIFY');solid.thickness=.002;solid.offset=0
def patch(name,vs,fs,mat):
    m=bpy.data.meshes.new(name);m.from_pydata(vs,[],fs);m.update();o=bpy.data.objects.new(name,m);bpy.context.collection.objects.link(o);o.data.materials.append(mat)
    so=o.modifiers.new('Cloth thickness','SOLIDIFY');so.thickness=.002
    be=o.modifiers.new('Soft sewn edges','BEVEL');be.width=.0015;be.segments=2
    return o
# Folded collar and button placket are distinct cloth details, not painted vest.
for sign in [-1,1]:
    patch('FoldedCollar',[(sign*.018,-.097,1.574),(sign*.078,-.06,1.582),(sign*.139,-.104,1.533),(sign*.075,-.15,1.49)],[(0,1,2,3)],navy)
patch('ButtonPlacket',[(-.012,-.149,1.52),(.012,-.149,1.52),(.012,-.15,1.395),(-.012,-.15,1.395)],[(0,1,2,3)],navy)
buttonmat=material('Dark pearl buttons',(.24,.28,.30))
for z in [1.47,1.422]:
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=6,radius=1,location=(0,-.154,z));o=bpy.context.object;o.name='PoloButton';o.scale=(.004,.002,.004);o.data.materials.append(buttonmat)
textdata=bpy.data.curves.new('Lacos embroidery','FONT');textdata.body='LACOS';textdata.size=.019;textdata.extrude=.00015
label=bpy.data.objects.new('Lacos embroidery',textdata);bpy.context.collection.objects.link(label);label.location=(.073,-.151,1.385);label.rotation_euler=(math.pi/2,0,0)
label.data.materials.append(material('Ivory embroidery',(.8,.81,.78)))
# This shell was constructed in evaluated world space. Do not reapply the
# posed source rig during static fitting; export bind conversion is a later gate.
scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE';scene.render.resolution_x=800;scene.render.resolution_y=900;scene.render.resolution_percentage=100
camera=scene.camera;camera.data.type='ORTHO';camera.data.ortho_scale=1.28
target=Vector((0,0,1.31))
for name,offset in [('Front',(0,-4,.03)),('ThreeQuarter',(2,-4,.08)),('Back',(0,4,.03))]:
    camera.location=target+Vector(offset);camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=os.path.join(OUT,'Polo-'+name+'.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'Sacat-Polo-FittingPrototype.blend'))
with open(os.path.join(OUT,'polo-audit.json'),'w') as f:json.dump({'shellTriangles':sum(len(p.vertices)-2 for p in shirt.data.polygons),'status':'static fitting prototype only; motion and seam cleanup pending','gameChanged':False},f,indent=2)
print('MINI148_POLO_STATIC_PREVIEW_COMPLETE')
