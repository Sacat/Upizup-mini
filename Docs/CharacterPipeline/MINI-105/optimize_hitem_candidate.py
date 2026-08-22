import bpy
import json
import os
from mathutils import Vector

SOURCE = r"C:\Users\PCSS-PC\Downloads\Hi3D_Untitled_allparts_20260820_223739.glb"
OUT = r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-105\HitemCandidate01\Optimized"
TARGET_TRIANGLES = 25000
TARGET_HEIGHT_METRES = 1.85
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=SOURCE)
obj = next(o for o in bpy.context.scene.objects if o.type == "MESH")
obj.name = "Sacat_ModularBase_Optimized"

# Normalize to the project's one-unit-per-metre character standard.
points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
low_z = min(p.z for p in points)
high_z = max(p.z for p in points)
source_height = high_z - low_z
scale = TARGET_HEIGHT_METRES / source_height
obj.scale = tuple(v * scale for v in obj.scale)
bpy.context.view_layer.objects.active = obj
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
obj.location.z -= min(p.z for p in points)
bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)

before_vertices = len(obj.data.vertices)
before_polygons = len(obj.data.polygons)
ratio = min(1.0, TARGET_TRIANGLES / max(1, before_polygons))
modifier = obj.modifiers.new("MobileTriangleBudget", "DECIMATE")
modifier.decimate_type = "COLLAPSE"
modifier.ratio = ratio
modifier.use_collapse_triangulate = True
bpy.context.view_layer.objects.active = obj
bpy.ops.object.modifier_apply(modifier=modifier.name)

# Preserve normals after aggressive reduction.
for polygon in obj.data.polygons:
    polygon.use_smooth = True

stats = {
    "source": SOURCE,
    "target_height_m": TARGET_HEIGHT_METRES,
    "before_vertices": before_vertices,
    "before_polygons": before_polygons,
    "after_vertices": len(obj.data.vertices),
    "after_polygons": len(obj.data.polygons),
    "materials": [slot.material.name if slot.material else None for slot in obj.material_slots],
    "note": "Visual decimation proof only; final retopology and skinning are still required.",
}
with open(os.path.join(OUT, "Optimized-Inspection.json"), "w", encoding="utf-8") as handle:
    json.dump(stats, handle, indent=2)

# Neutral render stage.
points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
low = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
high = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
centre = (low + high) * 0.5
height = high.z - low.z
world = bpy.context.scene.world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.018, 0.022, 0.028, 1)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.35
bpy.ops.mesh.primitive_plane_add(size=6, location=(centre.x, centre.y, -0.004))
ground = bpy.context.object
ground_mat = bpy.data.materials.new("InspectionGround")
ground_mat.diffuse_color = (0.11, 0.12, 0.14, 1)
ground.data.materials.append(ground_mat)
for name, offset, energy in [("Key", (2.8, -3.5, 2.4), 1100), ("Fill", (-3, -1.2, 1.8), 650), ("Rim", (1, 3.2, 2.8), 850)]:
    data = bpy.data.lights.new(name, "AREA")
    data.energy = energy
    data.shape = "DISK"
    data.size = 3
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
target = Vector((centre.x, centre.y, height * 0.52))
distance = height * 3
for name, direction in {"front": Vector((0, -1, 0)), "three-quarter": Vector((0.7, -0.7, 0)), "side": Vector((1, 0, 0)), "back": Vector((0, 1, 0))}.items():
    cam.location = target + direction.normalized() * distance + Vector((0, 0, height * 0.02))
    cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = os.path.join(OUT, f"Optimized-{name}.png")
    bpy.ops.render.render(write_still=True)

bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "Sacat-ModularBase-Optimized.blend"))
bpy.ops.object.select_all(action="DESELECT")
obj.select_set(True)
bpy.context.view_layer.objects.active = obj
bpy.ops.export_scene.gltf(filepath=os.path.join(OUT, "Sacat-ModularBase-Optimized.glb"), export_format="GLB", use_selection=True)
print("HITEM_OPTIMIZATION_PROOF_PASS")
