"""Manifest-driven Blender LOD builder for Up Iz Up Mini.

Run through Invoke-CharacterPipeline.ps1. Outputs are derived files; the master FBX is read-only.
"""
import bpy
import json
import os
import sys


def project_path(value):
    return value if os.path.isabs(value) else os.path.abspath(value)


args = sys.argv[sys.argv.index("--") + 1:]
if not args:
    raise RuntimeError("Character manifest path is required after --")
manifest_path = project_path(args[0])
with open(manifest_path, encoding="utf-8") as handle:
    manifest = json.load(handle)

source = project_path(manifest["source"]["masterFbx"])
character = manifest["displayName"].replace(" ", "") + "ModularBase"
output = project_path(os.path.join("Docs", "CharacterPipeline", "Generated", manifest["characterId"]))
targets = manifest["mobile"]["lodTriangleTargets"]


def triangles(mesh):
    return sum(max(0, len(poly.vertices) - 2) for poly in mesh.polygons)


def build(index, target):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=source, use_anim=False)
    arms = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"]
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    if len(arms) != 1 or len(meshes) != 1:
        raise RuntimeError(f"Expected one armature and one mesh, got {len(arms)}/{len(meshes)}")
    arm, body = arms[0], meshes[0]
    before = triangles(body.data)
    arm_mods = [m for m in body.modifiers if m.type == "ARMATURE"]
    if len(arm_mods) != 1:
        raise RuntimeError("Expected exactly one Armature modifier")
    old = arm_mods[0]
    preserve = old.use_deform_preserve_volume
    body.modifiers.remove(old)
    if body.data.shape_keys:
        body.shape_key_clear()
    bpy.context.view_layer.objects.active = body
    body.select_set(True)
    decimate = body.modifiers.new(name=f"Mobile_LOD{index}", type="DECIMATE")
    decimate.decimate_type = "COLLAPSE"
    decimate.ratio = min(1.0, max(0.01, target / float(before)))
    decimate.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=decimate.name)
    bpy.ops.object.vertex_group_limit_total(group_select_mode="ALL", limit=manifest["rig"]["maxWeightsPerVertex"])
    bpy.ops.object.vertex_group_normalize_all(group_select_mode="ALL", lock_active=False)
    restored = body.modifiers.new(name="Armature", type="ARMATURE")
    restored.object = arm
    restored.use_deform_preserve_volume = preserve
    for polygon in body.data.polygons:
        polygon.use_smooth = True
    label = f"LOD{index}"
    body.name = f"{character}_{label}"
    arm.name = f"{character}_{label}_Armature"
    bpy.ops.object.select_all(action="DESELECT")
    arm.select_set(True)
    body.select_set(True)
    bpy.context.view_layer.objects.active = arm
    path = os.path.join(output, f"{character}_{label}.fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"ARMATURE", "MESH"},
        apply_unit_scale=True, bake_space_transform=False, add_leaf_bones=False,
        use_armature_deform_only=False, use_mesh_modifiers=True, mesh_smooth_type="FACE",
        bake_anim=False, path_mode="STRIP")
    return {"lod": label, "target": target, "sourceTriangles": before,
            "triangles": triangles(body.data), "vertices": len(body.data.vertices), "fbx": path}


os.makedirs(output, exist_ok=True)
audit = {"manifest": manifest_path, "source": source, "lods": [build(i, target) for i, target in enumerate(targets)]}
with open(os.path.join(output, "LOD-Audit.json"), "w", encoding="utf-8") as handle:
    json.dump(audit, handle, indent=2)
print("UPIZUP_CHARACTER_LOD_PASS")
