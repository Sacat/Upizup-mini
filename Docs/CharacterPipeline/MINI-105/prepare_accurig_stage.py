import bpy
import json
import os
from mathutils import Vector

SOURCE = r"C:\Users\PCSS-PC\Downloads\Hi3D_Untitled_allparts_20260820_223739.glb"
OUT = r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-105\AccuRigStage"
TARGET_TRIANGLES = 100000
TARGET_HEIGHT_METRES = 1.85
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=SOURCE)
body = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH")
body.name = "Sacat_ModularBase_AccuRigStage"

# Normalize the candidate to Unity's one-unit-per-metre convention and ground it.
points = [body.matrix_world @ Vector(corner) for corner in body.bound_box]
source_height = max(point.z for point in points) - min(point.z for point in points)
scale = TARGET_HEIGHT_METRES / source_height
body.scale = tuple(value * scale for value in body.scale)
bpy.context.view_layer.objects.active = body
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
points = [body.matrix_world @ Vector(corner) for corner in body.bound_box]
body.location.z -= min(point.z for point in points)
bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)

before_vertices = len(body.data.vertices)
before_polygons = len(body.data.polygons)
ratio = min(1.0, TARGET_TRIANGLES / max(1, before_polygons))
modifier = body.modifiers.new("AccuRigStagingReduction", "DECIMATE")
modifier.decimate_type = "COLLAPSE"
modifier.ratio = ratio
modifier.use_collapse_triangulate = True
bpy.context.view_layer.objects.active = body
bpy.ops.object.modifier_apply(modifier=modifier.name)
for polygon in body.data.polygons:
    polygon.use_smooth = True

# AccuRIG accepts FBX/OBJ. Export both so its current build can use the safer option.
bpy.ops.object.select_all(action="DESELECT")
body.select_set(True)
bpy.context.view_layer.objects.active = body
fbx_path = os.path.join(OUT, "Sacat-ModularBase-AccuRigStage.fbx")
obj_path = os.path.join(OUT, "Sacat-ModularBase-AccuRigStage.obj")
fbx_exported = False
obj_exported = False
try:
    bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        use_selection=True,
        apply_unit_scale=True,
        bake_space_transform=False,
        add_leaf_bones=False,
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        bake_anim=False,
    )
    fbx_exported = True
except Exception as error:
    print(f"FBX_EXPORT_WARNING: {error}")
try:
    bpy.ops.wm.obj_export(
        filepath=obj_path,
        export_selected_objects=True,
        export_materials=True,
        export_uv=True,
        export_normals=True,
    )
    obj_exported = True
except Exception as error:
    print(f"OBJ_EXPORT_WARNING: {error}")

blend_path = os.path.join(OUT, "Sacat-ModularBase-AccuRigStage.blend")
bpy.ops.wm.save_as_mainfile(filepath=blend_path)

stats = {
    "source": SOURCE,
    "target_height_m": TARGET_HEIGHT_METRES,
    "before_vertices": before_vertices,
    "before_polygons": before_polygons,
    "after_vertices": len(body.data.vertices),
    "after_polygons": len(body.data.polygons),
    "fbx_exported": fbx_exported,
    "obj_exported": obj_exported,
    "purpose": "Full body and five-finger AccuRIG calibration proof; not the final mobile mesh.",
}
with open(os.path.join(OUT, "AccuRigStage-Inspection.json"), "w", encoding="utf-8") as handle:
    json.dump(stats, handle, indent=2)

if not (fbx_exported or obj_exported):
    raise RuntimeError("Neither an FBX nor OBJ staging file could be exported")
print("ACCURIG_STAGE_PASS")
