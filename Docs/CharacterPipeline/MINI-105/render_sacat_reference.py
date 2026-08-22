import bpy
import math
import os
from mathutils import Vector

FBX = r"E:\Unity\Up Iz Up Mini\Assets\UpIzUpMini\Art\Characters\Mainchar.fbx"
OUT = r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-105\SacatReference"
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=FBX, use_anim=False)

meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
if not meshes:
    raise RuntimeError("Mainchar.fbx imported without meshes")

def bounds(objects):
    points = []
    deps = bpy.context.evaluated_depsgraph_get()
    for obj in objects:
        evaluated = obj.evaluated_get(deps)
        points.extend(obj.matrix_world @ Vector(corner) for corner in evaluated.bound_box)
    low = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    high = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return low, high

low, high = bounds(meshes)
centre = (low + high) * 0.5
height = high.z - low.z

world = bpy.context.scene.world
world.color = (0.035, 0.035, 0.045)
world.use_nodes = True
world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.025, 0.03, 0.04, 1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value = 0.32

ground_mat = bpy.data.materials.new('NeutralGround')
ground_mat.diffuse_color = (0.12, 0.13, 0.15, 1)
bpy.ops.mesh.primitive_plane_add(size=max(6.0, height * 3.0), location=(centre.x, centre.y, low.z - 0.005))
bpy.context.object.data.materials.append(ground_mat)

def area(name, location, energy, size):
    data = bpy.data.lights.new(name, 'AREA')
    data.energy = energy
    data.shape = 'DISK'
    data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    direction = centre - obj.location
    obj.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()

area('Key', centre + Vector((2.8, -3.5, height * 1.15)), 1050, 3.5)
area('Fill', centre + Vector((-3.0, -1.5, height * 0.8)), 650, 3.0)
area('Rim', centre + Vector((1.0, 3.0, height * 1.35)), 900, 2.5)

camera_data = bpy.data.cameras.new('SacatReferenceCamera')
camera = bpy.data.objects.new('SacatReferenceCamera', camera_data)
bpy.context.collection.objects.link(camera)
bpy.context.scene.camera = camera
camera.data.type = 'ORTHO'
camera.data.ortho_scale = height * 1.18

scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE'
scene.render.resolution_x = 768
scene.render.resolution_y = 1024
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.film_transparent = False
scene.render.image_settings.color_mode = 'RGBA'

target = Vector((centre.x, centre.y, low.z + height * 0.52))
distance = height * 3.0
views = {
    'front': Vector((0, -1, 0)),
    'three_quarter': Vector((0.70, -0.70, 0)),
    'side': Vector((1, 0, 0)),
    'back': Vector((0, 1, 0)),
}

for name, direction in views.items():
    camera.location = target + direction.normalized() * distance + Vector((0, 0, height * 0.02))
    camera.rotation_euler = (target - camera.location).to_track_quat('-Z', 'Y').to_euler()
    scene.render.filepath = os.path.join(OUT, f'Sacat-{name}.png')
    bpy.ops.render.render(write_still=True)

print(f"SACAT_REFERENCE_PASS meshes={len(meshes)} height={height:.4f} out={OUT}")
