import bpy
import json
import os
from mathutils import Vector

BODY = r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-105\HitemCandidate01\Optimized\Sacat-ModularBase-Optimized.glb"
RIG_SOURCE = r"E:\Unity\Up Iz Up Mini\Assets\UpIzUpMini\Art\Characters\Mainchar.fbx"
OUT = r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-105\WeightTransferProof"
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=BODY)
body = next(o for o in bpy.context.scene.objects if o.type == "MESH")
body.name = "Sacat_ModularBase"
bpy.ops.import_scene.fbx(filepath=RIG_SOURCE, use_anim=False)
armature = next(o for o in bpy.context.scene.objects if o.type == "ARMATURE")
source = next(o for o in bpy.context.scene.objects if o.type == "MESH" and o != body)
armature.name = "Sacat_CanonicalRig"
source.name = "Sacat_WeightSource"

scale = 1.85 / 1.8121
for obj in (source, armature):
    obj.scale = (scale, scale, scale)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

# Create destination groups using the source's stable Mixamo/full-finger names.
for group in source.vertex_groups:
    if body.vertex_groups.get(group.name) is None:
        body.vertex_groups.new(name=group.name)

transfer = body.modifiers.new("SacatWeightTransfer", "DATA_TRANSFER")
transfer.object = source
transfer.use_vert_data = True
transfer.data_types_verts = {"VGROUP_WEIGHTS"}
transfer.vert_mapping = "POLYINTERP_NEAREST"
transfer.layers_vgroup_select_src = "ALL"
transfer.layers_vgroup_select_dst = "NAME"
bpy.context.view_layer.objects.active = body
bpy.ops.object.modifier_apply(modifier=transfer.name)

# Mobile skinning budget: normalize and keep the strongest four weights.
bpy.ops.object.select_all(action="DESELECT")
body.select_set(True)
bpy.context.view_layer.objects.active = body
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.mesh.select_all(action="SELECT")
bpy.ops.object.vertex_group_normalize_all(lock_active=False)
bpy.ops.object.vertex_group_limit_total(group_select_mode="ALL", limit=4)
bpy.ops.object.vertex_group_normalize_all(lock_active=False)
bpy.ops.object.mode_set(mode="OBJECT")

armature_modifier = body.modifiers.new("SacatCanonicalRig", "ARMATURE")
armature_modifier.object = armature
body.parent = armature
source.hide_render = True
source.hide_viewport = True

# Audit actual assigned weights.
index_to_name = {group.index: group.name for group in body.vertex_groups}
unweighted = []
max_influences = 0
for vertex in body.data.vertices:
    weights = [g.weight for g in vertex.groups if index_to_name.get(g.group) and g.weight > 0.0001]
    max_influences = max(max_influences, len(weights))
    if not weights:
        unweighted.append(vertex.index)

# Exaggerated deformation pose for visible inspection.
bpy.context.view_layer.objects.active = armature
bpy.ops.object.mode_set(mode="POSE")
pose_tests = {
    "mixamorig9:LeftForeArm": (0.0, 0.0, -1.05),
    "mixamorig9:RightForeArm": (0.0, 0.0, 1.05),
    "mixamorig9:LeftUpLeg": (0.45, 0.0, 0.0),
    "mixamorig9:LeftLeg": (-0.75, 0.0, 0.0),
    "mixamorig9:RightUpLeg": (-0.18, 0.0, 0.0),
    "mixamorig9:RightLeg": (-0.25, 0.0, 0.0),
}
for name, rotation in pose_tests.items():
    bone = armature.pose.bones.get(name)
    if bone:
        bone.rotation_mode = "XYZ"
        bone.rotation_euler = rotation
bpy.ops.object.mode_set(mode="OBJECT")
bpy.context.view_layer.update()

stats = {
    "vertices": len(body.data.vertices),
    "polygons": len(body.data.polygons),
    "bones": len(armature.data.bones),
    "groups": len(body.vertex_groups),
    "unweighted_vertices": len(unweighted),
    "max_influences": max_influences,
}
with open(os.path.join(OUT, "WeightTransferAudit.json"), "w", encoding="utf-8") as handle:
    json.dump(stats, handle, indent=2)

# Neutral studio render of the deformation proof.
points = [body.matrix_world @ Vector(corner) for corner in body.bound_box]
low = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
high = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
centre = (low + high) * 0.5
height = high.z - low.z
world = bpy.context.scene.world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.018, 0.022, 0.028, 1)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.35
bpy.ops.mesh.primitive_plane_add(size=6, location=(centre.x, centre.y, low.z - 0.004))
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
cam_data.ortho_scale = max(1.85, height) * 1.2
scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 768
scene.render.resolution_y = 1024
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
target = Vector((centre.x, centre.y, low.z + max(1.85, height) * 0.5))
distance = max(1.85, height) * 3
for name, direction in {"front": Vector((0, -1, 0)), "three-quarter": Vector((0.7, -0.7, 0)), "side": Vector((1, 0, 0)), "back": Vector((0, 1, 0))}.items():
    cam.location = target + direction.normalized() * distance + Vector((0, 0, 0.04))
    cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = os.path.join(OUT, f"WeightProof-{name}.png")
    bpy.ops.render.render(write_still=True)

bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "Sacat-ModularBase-WeightTransfer.blend"))
print(f"SACAT_WEIGHT_TRANSFER_PASS unweighted={len(unweighted)} max_influences={max_influences}")
