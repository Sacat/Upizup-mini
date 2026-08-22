import bpy
import bmesh
import os
from collections import deque
from mathutils import Vector

FBX = r"E:\Unity\Up Iz Up Mini\Assets\UpIzUpMini\Art\Characters\Mainchar.fbx"
REMOVE_CAP = os.environ.get("SACAT_REMOVE_CAP", "0") == "1"
ADD_HAIR = os.environ.get("SACAT_ADD_HAIR", "0") == "1"
QUICK_PREVIEW = os.environ.get("SACAT_QUICK_PREVIEW", "0") == "1"
OUT = (r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-105\SacatNoCapOrHeadphones"
       if REMOVE_CAP else
       r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-105\SacatNoHeadphones")
os.makedirs(OUT, exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=FBX, use_anim=False)
obj = next(o for o in bpy.context.scene.objects if o.type == 'MESH')
mesh = obj.data

vertex_faces=[[] for _ in mesh.vertices]
for face in mesh.polygons:
    for vertex in face.vertices: vertex_faces[vertex].append(face.index)
unseen=set(range(len(mesh.polygons))); islands=[]
while unseen:
    seed=unseen.pop(); queue=deque([seed]); component=[seed]
    while queue:
        face_index=queue.popleft()
        for vertex in mesh.polygons[face_index].vertices:
            for neighbor in vertex_faces[vertex]:
                if neighbor in unseen:
                    unseen.remove(neighbor); queue.append(neighbor); component.append(neighbor)
    islands.append(component)
islands.sort(key=len, reverse=True)

# Mainchar loose-island audit: 6/7 are the ear cups, 13/14 are the layered
# headband pieces, and 16/17 are the two small cup connectors. The cap is
# island 10 and is deliberately retained.
removal_islands = (6,7,10,13,14,15,16,17) if REMOVE_CAP else (6,7,13,14,16,17)
remove_faces={face for island_index in removal_islands for face in islands[island_index]}
remove_vertices={vertex for face_index in remove_faces for vertex in mesh.polygons[face_index].vertices}
bm=bmesh.new(); bm.from_mesh(mesh); bm.verts.ensure_lookup_table()
bmesh.ops.delete(bm, geom=[bm.verts[index] for index in remove_vertices], context='VERTS')
bm.to_mesh(mesh); bm.free(); mesh.update()

if REMOVE_CAP and ADD_HAIR:
    # The source deletes the scalp beneath the cap. Reconstruct a lightweight
    # close-cropped hair shell over the untouched face/head island.
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=20, location=(0.001, -0.016, 1.711))
    hair = bpy.context.object
    hair.name = 'Sacat_CloseCroppedHair_Preview'
    hair.scale = (0.086, 0.104, 0.116)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    hair_bm = bmesh.new(); hair_bm.from_mesh(hair.data)
    bmesh.ops.delete(hair_bm, geom=[v for v in hair_bm.verts if v.co.z < -0.030], context='VERTS')
    hair_bm.to_mesh(hair.data); hair_bm.free(); hair.data.update()
    hair_mat = bpy.data.materials.new('SacatHairPreview')
    hair_mat.use_nodes = True
    hair_mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (0.006, 0.008, 0.007, 1)
    hair_mat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = 0.82
    hair.data.materials.append(hair_mat)

meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
points=[o.matrix_world@Vector(corner) for o in meshes for corner in o.bound_box]
low=Vector((min(p.x for p in points),min(p.y for p in points),min(p.z for p in points)))
high=Vector((max(p.x for p in points),max(p.y for p in points),max(p.z for p in points)))
centre=(low+high)*.5; height=high.z-low.z
world=bpy.context.scene.world; world.use_nodes=True
world.node_tree.nodes['Background'].inputs['Color'].default_value=(.025,.03,.04,1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value=.32
groundmat=bpy.data.materials.new('NeutralGround'); groundmat.diffuse_color=(.12,.13,.15,1)
bpy.ops.mesh.primitive_plane_add(size=max(6,height*3),location=(centre.x,centre.y,low.z-.005)); bpy.context.object.data.materials.append(groundmat)
for name,offset,energy in [('Key',(2.8,-3.5,2.3),1050),('Fill',(-3,-1.5,1.8),650),('Rim',(1,3,2.5),900)]:
    data=bpy.data.lights.new(name,'AREA'); data.energy=energy; data.shape='DISK'; data.size=3
    light=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(light); light.location=centre+Vector(offset)
    light.rotation_euler=(centre-light.location).to_track_quat('-Z','Y').to_euler()
camdata=bpy.data.cameras.new('Camera'); cam=bpy.data.objects.new('Camera',camdata); bpy.context.collection.objects.link(cam)
bpy.context.scene.camera=cam; camdata.type='ORTHO'; camdata.ortho_scale=height*1.18
scene=bpy.context.scene; scene.render.engine='BLENDER_EEVEE'; scene.render.resolution_x=768; scene.render.resolution_y=1024; scene.render.resolution_percentage=100; scene.render.image_settings.file_format='PNG'
target=Vector((centre.x,centre.y,low.z+height*.52)); distance=height*3
prefix = 'Sacat-NoCapOrHeadphones' if REMOVE_CAP else 'Sacat-NoHeadphones'
views = {'front':Vector((0,-1,0)),'three_quarter':Vector((.7,-.7,0))}
if not QUICK_PREVIEW: views.update({'side':Vector((1,0,0)),'back':Vector((0,1,0))})
for name,direction in views.items():
    cam.location=target+direction.normalized()*distance+Vector((0,0,height*.02)); cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=os.path.join(OUT,f'{prefix}-{name}.png'); bpy.ops.render.render(write_still=True)
if not QUICK_PREVIEW:
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,f'{prefix}-Staging.blend'))
print(f'SACAT_ACCESSORY_REMOVAL_PREVIEW_PASS mode={prefix}')
