import bpy
import json
import os

BLEND = r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-105\RigProof\Sacat-ModularBase-RigProof.blend"
OUT = r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-105\RigProof\RigWeightAudit.json"
bpy.ops.wm.open_mainfile(filepath=BLEND)

body = bpy.data.objects.get("Sacat_ModularBase")
armature = bpy.data.objects.get("Sacat_CanonicalRig")
if body is None or armature is None:
    raise RuntimeError("Rig proof objects are missing")

deform_names = {bone.name for bone in armature.data.bones if bone.use_deform}
group_index_to_name = {group.index: group.name for group in body.vertex_groups}
unweighted = []
weight_totals = []
influence_counts = []
for vertex in body.data.vertices:
    weights = [g.weight for g in vertex.groups if group_index_to_name.get(g.group) in deform_names and g.weight > 0]
    total = sum(weights)
    weight_totals.append(total)
    influence_counts.append(len(weights))
    if total < 0.0001:
        unweighted.append({"index": vertex.index, "co": list(vertex.co)})

groups = {}
for group in body.vertex_groups:
    assigned = 0
    max_weight = 0.0
    for vertex in body.data.vertices:
        try:
            value = group.weight(vertex.index)
        except RuntimeError:
            continue
        if value > 0:
            assigned += 1
            max_weight = max(max_weight, value)
    groups[group.name] = {"vertices": assigned, "max_weight": max_weight}

result = {
    "vertices": len(body.data.vertices),
    "polygons": len(body.data.polygons),
    "deform_bones": len(deform_names),
    "vertex_groups": len(body.vertex_groups),
    "unweighted_count": len(unweighted),
    "unweighted_bounds": None,
    "max_influences": max(influence_counts) if influence_counts else 0,
    "over_four_influences": sum(1 for count in influence_counts if count > 4),
    "weight_total_min": min(weight_totals) if weight_totals else 0,
    "weight_total_max": max(weight_totals) if weight_totals else 0,
    "empty_deform_groups": sorted(name for name, data in groups.items() if name in deform_names and data["vertices"] == 0),
    "groups": groups,
}
if unweighted:
    coords = [entry["co"] for entry in unweighted]
    result["unweighted_bounds"] = {
        "min": [min(c[i] for c in coords) for i in range(3)],
        "max": [max(c[i] for c in coords) for i in range(3)],
    }

with open(OUT, "w", encoding="utf-8") as handle:
    json.dump(result, handle, indent=2)
print("RIG_WEIGHT_AUDIT_PASS")
