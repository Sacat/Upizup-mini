import bpy
import json
import os

SOURCE = r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-105\AccuRigOutput\Sacat-ModularBase-Rigged-Unity.fbx"
OUTPUT = r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-106\BlenderOutput"
TARGETS = {
    "LOD0": 25000,
    "LOD1": 12000,
    "LOD2": 4500,
}


def triangle_count(mesh):
    return sum(max(0, len(poly.vertices) - 2) for poly in mesh.polygons)


def weight_audit(obj, deform_names):
    index_to_name = {group.index: group.name for group in obj.vertex_groups}
    unweighted = 0
    maximum = 0
    over_four = 0
    for vertex in obj.data.vertices:
        influences = [
            entry.weight for entry in vertex.groups
            if entry.weight > 0.000001 and index_to_name.get(entry.group) in deform_names
        ]
        maximum = max(maximum, len(influences))
        if len(influences) > 4:
            over_four += 1
        if sum(influences) < 0.0001:
            unweighted += 1
    return unweighted, maximum, over_four


def import_source():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=SOURCE, use_anim=False)
    armatures = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if len(armatures) != 1 or len(meshes) != 1:
        raise RuntimeError(f"Expected one armature/mesh, found {len(armatures)}/{len(meshes)}")
    return armatures[0], meshes[0]


def export_lod(label, target):
    armature, body = import_source()
    before = triangle_count(body.data)
    ratio = min(1.0, max(0.01, target / float(before)))

    armature_modifiers = [modifier for modifier in body.modifiers if modifier.type == "ARMATURE"]
    if len(armature_modifiers) != 1 or armature_modifiers[0].object != armature:
        raise RuntimeError("Expected exactly one armature modifier bound to the canonical armature")
    source_armature_modifier = armature_modifiers[0]
    preserve_volume = source_armature_modifier.use_deform_preserve_volume
    use_vertex_groups = source_armature_modifier.use_vertex_groups
    use_bone_envelopes = source_armature_modifier.use_bone_envelopes
    body.modifiers.remove(source_armature_modifier)

    # The AccuRIG export carries unused expression keys. MINI-105 deliberately
    # imports no blend shapes; remove them only from these derived LOD copies so
    # Blender can decimate while the approved neutral face remains unchanged.
    if body.data.shape_keys:
        body.shape_key_clear()

    bpy.context.view_layer.objects.active = body
    body.select_set(True)
    modifier = body.modifiers.new(name=f"Mobile_{label}", type="DECIMATE")
    modifier.decimate_type = "COLLAPSE"
    modifier.ratio = ratio
    modifier.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=modifier.name)

    # Make the mobile constraint explicit in the authoring file instead of
    # relying only on Unity to silently discard lower weights.
    bpy.ops.object.vertex_group_limit_total(group_select_mode="ALL", limit=4)
    bpy.ops.object.vertex_group_normalize_all(group_select_mode="ALL", lock_active=False)

    restored_armature = body.modifiers.new(name="Armature", type="ARMATURE")
    restored_armature.object = armature
    restored_armature.use_deform_preserve_volume = preserve_volume
    restored_armature.use_vertex_groups = use_vertex_groups
    restored_armature.use_bone_envelopes = use_bone_envelopes

    for polygon in body.data.polygons:
        polygon.use_smooth = True
    body.name = f"SacatModularBase_{label}"
    body.data.name = f"SacatModularBase_{label}_Mesh"
    armature.name = f"SacatModularBase_{label}_Armature"

    deform_names = {bone.name for bone in armature.data.bones if bone.use_deform}
    unweighted, max_influences, over_four = weight_audit(body, deform_names)
    after = triangle_count(body.data)

    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    body.select_set(True)
    bpy.context.view_layer.objects.active = armature
    output_path = os.path.join(OUTPUT, f"SacatModularBase_{label}.fbx")
    bpy.ops.export_scene.fbx(
        filepath=output_path,
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        apply_unit_scale=True,
        bake_space_transform=False,
        add_leaf_bones=False,
        use_armature_deform_only=False,
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        bake_anim=False,
        path_mode="STRIP",
    )
    return {
        "label": label,
        "target_triangles": target,
        "source_triangles": before,
        "triangles": after,
        "vertices": len(body.data.vertices),
        "ratio": ratio,
        "bone_count": len(armature.data.bones),
        "deform_bone_count": len(deform_names),
        "unweighted_vertices": unweighted,
        "max_source_influences": max_influences,
        "vertices_over_four_source_influences": over_four,
        "fbx": output_path,
    }


os.makedirs(OUTPUT, exist_ok=True)
results = [export_lod(label, target) for label, target in TARGETS.items()]
with open(os.path.join(OUTPUT, "Sacat-Mobile-LOD-Audit.json"), "w", encoding="utf-8") as handle:
    json.dump({"source": SOURCE, "lods": results}, handle, indent=2)
print("MINI106_BLENDER_LOD_PASS")
