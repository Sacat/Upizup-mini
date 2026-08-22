import bpy
import json
import os
from mathutils import Vector

SOURCE = r"C:\Users\PCSS-PC\Downloads\Hi3D_Untitled_allparts_20260820_223739.glb"
OUT = r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-105\HitemCandidate01"
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=SOURCE)

meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
armatures = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"]
stats = {
    "source": SOURCE,
    "source_bytes": os.path.getsize(SOURCE),
    "mesh_count": len(meshes),
    "armature_count": len(armatures),
    "meshes": [],
}
for obj in meshes:
    stats["meshes"].append({
        "name": obj.name,
        "vertices": len(obj.data.vertices),
        "polygons": len(obj.data.polygons),
        "materials": [slot.material.name if slot.material else None for slot in obj.material_slots],
        "shape_keys": list(obj.data.shape_keys.key_blocks.keys()) if obj.data.shape_keys else [],
        "vertex_groups": len(obj.vertex_groups),
    })

with open(os.path.join(OUT, "Candidate01-Inspection.json"), "w", encoding="utf-8") as f:
    json.dump(stats, f, indent=2)

points = [obj.matrix_world @ Vector(corner) for obj in meshes for corner in obj.bound_box]
low = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
high = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
centre = (low + high) * 0.5
height = high.z - low.z

world = bpy.context.scene.world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.018, 0.022, 0.028, 1)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.35

ground_mat = bpy.data.materials.new("InspectionGround")
ground_mat.diffuse_color = (0.11, 0.12, 0.14, 1)
bpy.ops.mesh.primitive_plane_add(size=max(6.0, height * 3.0), location=(centre.x, centre.y, low.z - 0.004))
bpy.context.object.data.materials.append(ground_mat)

for name, offset, energy in [
    ("Key", (2.8, -3.5, 2.4), 1100),
    ("Fill", (-3.0, -1.2, 1.8), 650),
    ("Rim", (1.0, 3.2, 2.8), 850),
]:
    data = bpy.data.lights.new(name, "AREA")
    data.energy = energy
    data.shape = "DISK"
    data.size = 3.0
    light = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(light)
    light.location = centre + Vector(offset)
    light.rotation_euler = (centre - light.location).to_track_quat("-Z", "Y").to_euler()

cam_data = bpy.data.cameras.new("InspectionCamera")
cam = bpy.data.objects.new("InspectionCamera", cam_data)
bpy.context.collection.objects.link(cam)
bpy.context.scene.camera = cam
cam_data.type = "ORTHO"
cam_data.ortho_scale = height * 1.15

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 768
scene.render.resolution_y = 1024
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
target = Vector((centre.x, centre.y, low.z + height * 0.52))
distance = height * 3.0
views = {
    "front": Vector((0, -1, 0)),
    "left": Vector((-1, 0, 0)),
    "right": Vector((1, 0, 0)),
    "back": Vector((0, 1, 0)),
    "three-quarter": Vector((0.7, -0.7, 0)),
}
for name, direction in views.items():
    cam.location = target + direction.normalized() * distance + Vector((0, 0, height * 0.02))
    cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = os.path.join(OUT, f"Candidate01-{name}.png")
    bpy.ops.render.render(write_still=True)

bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "Candidate01-Staging.blend"))
print("HITEM_CANDIDATE_INSPECTION_PASS")
