import bpy
import json
import os

FBX = r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-105\AccuRigOutput\Sacat-ModularBase-Rigged-Unity.fbx"
OUT_DIR = r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-105\AccuRigOutput"
OUT_JSON = os.path.join(OUT_DIR, "AccuRig-Unity-Audit.json")
OUT_BLEND = os.path.join(OUT_DIR, "Sacat-ModularBase-Rigged-Unity.blend")

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX, use_anim=False)

armatures = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
if len(armatures) != 1:
    raise RuntimeError(f"Expected one armature, found {len(armatures)}")
if not meshes:
    raise RuntimeError("No skinned mesh found")

armature = armatures[0]
bone_names = [bone.name for bone in armature.data.bones]
deform_names = {bone.name for bone in armature.data.bones if bone.use_deform}

mesh_results = []
for mesh in meshes:
    group_index_to_name = {group.index: group.name for group in mesh.vertex_groups}
    unweighted = 0
    max_influences = 0
    over_four = 0
    min_total = None
    max_total = 0.0
    for vertex in mesh.data.vertices:
        weights = [
            entry.weight
            for entry in vertex.groups
            if group_index_to_name.get(entry.group) in deform_names and entry.weight > 0.000001
        ]
        total = sum(weights)
        count = len(weights)
        if total < 0.0001:
            unweighted += 1
        max_influences = max(max_influences, count)
        if count > 4:
            over_four += 1
        min_total = total if min_total is None else min(min_total, total)
        max_total = max(max_total, total)
    mesh_results.append({
        "name": mesh.name,
        "vertices": len(mesh.data.vertices),
        "triangles": sum(len(poly.vertices) - 2 for poly in mesh.data.polygons),
        "materials": [slot.material.name if slot.material else None for slot in mesh.material_slots],
        "vertex_groups": len(mesh.vertex_groups),
        "unweighted_vertices": unweighted,
        "max_influences": max_influences,
        "vertices_over_four_influences": over_four,
        "weight_total_min": min_total or 0.0,
        "weight_total_max": max_total,
        "armature_modifiers": [modifier.object.name for modifier in mesh.modifiers if modifier.type == "ARMATURE" and modifier.object],
    })

lower_names = [name.lower() for name in bone_names]
finger_tokens = ("thumb", "index", "middle", "ring", "pinky", "little")
finger_bones = [name for name in bone_names if any(token in name.lower() for token in finger_tokens)]

result = {
    "source_fbx": FBX,
    "armature": armature.name,
    "bone_count": len(bone_names),
    "deform_bone_count": len(deform_names),
    "bone_names": bone_names,
    "finger_bone_count": len(finger_bones),
    "finger_bones": finger_bones,
    "has_left_finger_chain": any("l_" in name.lower() or "left" in name.lower() for name in finger_bones),
    "has_right_finger_chain": any("r_" in name.lower() or "right" in name.lower() for name in finger_bones),
    "meshes": mesh_results,
}

with open(OUT_JSON, "w", encoding="utf-8") as handle:
    json.dump(result, handle, indent=2)

bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND)
print("ACCURIG_UNITY_AUDIT_PASS")
