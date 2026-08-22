import bpy
import os
from mathutils import Vector

SOURCE = r"C:\Users\PCSS-PC\Downloads\Hi3D_Untitled_allparts_20260820_223739.glb"
OUT = r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-105\HandInspection"
os.makedirs(OUT, exist_ok=True)
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=SOURCE)
body = next(o for o in bpy.context.scene.objects if o.type == "MESH")

points = [body.matrix_world @ Vector(corner) for corner in body.bound_box]
height = max(p.z for p in points) - min(p.z for p in points)
scale = 1.85 / height
body.scale = (scale, scale, scale)
bpy.context.view_layer.objects.active = body
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
points = [body.matrix_world @ Vector(corner) for corner in body.bound_box]
minimum = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
maximum = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))

world = bpy.context.scene.world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.018, 0.022, 0.028, 1)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.4
for name, location, energy in [("Key", (1.2, -1.8, 2.2), 900), ("Fill", (-1.2, -1.2, 1.6), 500), ("Rim", (0, 1.5, 2.0), 700)]:
    data = bpy.data.lights.new(name, "AREA")
    data.energy = energy
    data.size = 1.2
    light = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(light)
    light.location = Vector(location)

cam_data = bpy.data.cameras.new("HandInspectionCamera")
cam = bpy.data.objects.new("HandInspectionCamera", cam_data)
bpy.context.collection.objects.link(cam)
bpy.context.scene.camera = cam
cam_data.type = "ORTHO"
cam_data.ortho_scale = 0.34
scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 1024
scene.render.resolution_y = 768
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"

hand_z = minimum.z + (maximum.z - minimum.z) * 0.735
centre_y = (minimum.y + maximum.y) * 0.5
for side, x in [("right", maximum.x - 0.075), ("left", minimum.x + 0.075)]:
    target = Vector((x, centre_y, hand_z))
    for view, direction in [("front", Vector((0, -1, 0))), ("palm-angle", Vector((0.25 if x > 0 else -0.25, -1, 0.15)) )]:
        cam.location = target + direction.normalized() * 1.4
        cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = os.path.join(OUT, f"Hand-{side}-{view}.png")
        bpy.ops.render.render(write_still=True)
print("HAND_INSPECTION_PASS")
