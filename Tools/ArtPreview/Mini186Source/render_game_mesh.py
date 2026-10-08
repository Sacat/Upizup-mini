"""Render the measured game mesh for visual quality review."""

import bpy
from pathlib import Path
from mathutils import Vector


root = Path(__file__).resolve().parent
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(root / "LalayTool_3000tri.fbx"), use_image_search=True)
obj = next(o for o in bpy.context.scene.objects if o.type == "MESH")
corners = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
centre = sum(corners, Vector()) / len(corners)
span = max((max(c[i] for c in corners) - min(c[i] for c in corners)) for i in range(3))

scene = bpy.context.scene
scene.render.engine = "CYCLES"
scene.cycles.samples = 24
scene.render.resolution_x = 1000
scene.render.resolution_y = 800
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
scene.world.color = (0.16, 0.17, 0.19)

cam = bpy.data.cameras.new("Review Camera")
cam.type = "ORTHO"
cam.ortho_scale = span * 1.35
cam_obj = bpy.data.objects.new("Review Camera", cam)
scene.collection.objects.link(cam_obj)
scene.camera = cam_obj

for name, offset, power, size in [
    ("Key", Vector((0.3, -0.3, 0.4)), 180, 0.4),
    ("Fill", Vector((-0.3, 0.2, 0.2)), 110, 0.5),
]:
    light = bpy.data.lights.new(name, "AREA")
    light.energy = power
    light.shape = "DISK"
    light.size = size
    lamp = bpy.data.objects.new(name, light)
    scene.collection.objects.link(lamp)
    lamp.location = centre + offset
    lamp.rotation_euler = (centre - lamp.location).to_track_quat("-Z", "Y").to_euler()

for name, direction in [
    ("side", Vector((1, 0.12, 0.05))),
    ("reverse", Vector((-1, -0.12, 0.05))),
    ("three_quarter", Vector((0.7, -0.5, 0.35))),
]:
    cam_obj.location = centre + direction.normalized() * span * 3.0
    cam_obj.rotation_euler = (centre - cam_obj.location).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = str(root / ("LalayTool_" + name + ".png"))
    bpy.ops.render.render(write_still=True)
