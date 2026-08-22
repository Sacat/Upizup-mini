import bpy
import os
from collections import deque
from mathutils import Vector

FBX = r"E:\Unity\Up Iz Up Mini\Assets\UpIzUpMini\Art\Characters\Mainchar.fbx"
OUT = r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-105\SacatReference\Sacat-island-separation.png"
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=FBX, use_anim=False)
obj = next(o for o in bpy.context.scene.objects if o.type == 'MESH')
mesh = obj.data

vertex_faces = [[] for _ in mesh.vertices]
for face in mesh.polygons:
    for vertex in face.vertices:
        vertex_faces[vertex].append(face.index)
unseen = set(range(len(mesh.polygons)))
islands = []
while unseen:
    seed = unseen.pop(); queue = deque([seed]); component = [seed]
    while queue:
        face_index = queue.popleft()
        for vertex in mesh.polygons[face_index].vertices:
            for neighbor in vertex_faces[vertex]:
                if neighbor in unseen:
                    unseen.remove(neighbor); queue.append(neighbor); component.append(neighbor)
    islands.append(component)
islands.sort(key=len, reverse=True)

palette = [
    (0.04,0.36,0.80,1),(0.92,0.18,0.12,1),(0.10,0.68,0.30,1),(0.96,0.62,0.05,1),
    (0.55,0.16,0.72,1),(0.05,0.70,0.72,1),(0.95,0.30,0.65,1),(0.62,0.72,0.05,1),
    (0.35,0.18,0.08,1),(0.78,0.78,0.82,1),(0.95,0.82,0.24,1),(0.20,0.20,0.23,1)
]
material_offset = len(mesh.materials)
for index, color in enumerate(palette):
    mat = bpy.data.materials.new(f'Island_{index:02d}')
    mat.diffuse_color = color
    mesh.materials.append(mat)
for index, island in enumerate(islands):
    material = material_offset + (index if index < len(palette) else len(palette)-1)
    for face_index in island:
        mesh.polygons[face_index].material_index = material

points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
low = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
high = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
centre = (low + high) * 0.5; height = high.z-low.z

world=bpy.context.scene.world; world.use_nodes=True
world.node_tree.nodes['Background'].inputs['Color'].default_value=(0.025,0.03,0.04,1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value=.4
for name, offset, energy in [('Key',(3,-4,3),1000),('Fill',(-3,-2,2),600),('Rim',(1,3,3),800)]:
    data=bpy.data.lights.new(name,'AREA'); data.energy=energy; data.shape='DISK'; data.size=3
    light=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(light)
    light.location=centre+Vector(offset); light.rotation_euler=(centre-light.location).to_track_quat('-Z','Y').to_euler()

camdata=bpy.data.cameras.new('Camera'); cam=bpy.data.objects.new('Camera',camdata); bpy.context.collection.objects.link(cam)
bpy.context.scene.camera=cam; camdata.type='ORTHO'; camdata.ortho_scale=height*1.18
target=Vector((centre.x,centre.y,low.z+height*.52)); cam.location=target+Vector((.7,-.7,.03)).normalized()*height*3
cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
scene=bpy.context.scene; scene.render.engine='BLENDER_EEVEE'; scene.render.resolution_x=768; scene.render.resolution_y=1024
scene.render.resolution_percentage=100; scene.render.image_settings.file_format='PNG'; scene.render.filepath=OUT
bpy.ops.render.render(write_still=True)
print(f'SACAT_ISLAND_RENDER_PASS islands={len(islands)} out={OUT}')
