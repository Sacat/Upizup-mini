import bpy
import json
import math
import os
from mathutils import Vector

BODY = r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-105\HitemCandidate01\Optimized\Sacat-ModularBase-Optimized.glb"
RIG_SOURCE = r"E:\Unity\Up Iz Up Mini\Assets\UpIzUpMini\Art\Characters\Mainchar.fbx"
OUT = r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-105\EnvelopeRigProof"
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=BODY)
body = next(o for o in bpy.context.scene.objects if o.type == "MESH")
body.name = "Sacat_ModularBase"
bpy.ops.import_scene.fbx(filepath=RIG_SOURCE, use_anim=False)
armature = next(o for o in bpy.context.scene.objects if o.type == "ARMATURE")
armature.name = "Sacat_CanonicalRig"
for old in [o for o in bpy.context.scene.objects if o.type == "MESH" and o != body]:
    bpy.data.objects.remove(old, do_unlink=True)

scale = 1.85 / 1.8121
armature.scale = (scale, scale, scale)
bpy.context.view_layer.objects.active = armature
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

deform_bones = [bone for bone in armature.data.bones if bone.use_deform and not bone.name.endswith("_End")]
groups = {bone.name: body.vertex_groups.new(name=bone.name) for bone in deform_bones}

def point_segment_distance(point, start, end):
    segment = end - start
    length_sq = segment.length_squared
    if length_sq < 1e-10:
        return (point - start).length
    t = max(0.0, min(1.0, (point - start).dot(segment) / length_sq))
    return (point - (start + segment * t)).length

bone_segments = [(bone.name, Vector(bone.head_local), Vector(bone.tail_local)) for bone in deform_bones]
for vertex in body.data.vertices:
    point = Vector(vertex.co)
    distances = sorted(
        ((point_segment_distance(point, head, tail), name) for name, head, tail in bone_segments),
        key=lambda item: item[0],
    )[:4]
    # Strong inverse-distance falloff keeps limbs separated while still blending joints.
    raw = [(1.0 / ((distance + 0.018) ** 3), name) for distance, name in distances]
    total = sum(value for value, _ in raw)
    for value, name in raw:
        groups[name].add([vertex.index], value / total, "REPLACE")

modifier = body.modifiers.new("SacatCanonicalRig", "ARMATURE")
modifier.object = armature
body.parent = armature

# Exaggerated but bounded diagnostic pose.
bpy.context.view_layer.objects.active = armature
bpy.ops.object.mode_set(mode="POSE")
tests = {
    "mixamorig9:LeftForeArm": (0.0, 0.0, -0.72),
    "mixamorig9:RightForeArm": (0.0, 0.0, 0.72),
    "mixamorig9:LeftUpLeg": (0.20, 0.0, 0.0),
    "mixamorig9:LeftLeg": (-0.38, 0.0, 0.0),
}
for name, rotation in tests.items():
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
    "weighted_groups": len(body.vertex_groups),
    "max_influences": 4,
    "method": "four-nearest bone segments with inverse-cube falloff",
}
with open(os.path.join(OUT, "EnvelopeWeightAudit.json"), "w", encoding="utf-8") as handle:
    json.dump(stats, handle, indent=2)

depsgraph = bpy.context.evaluated_depsgraph_get()
evaluated = body.evaluated_get(depsgraph)
evaluated_points = [evaluated.matrix_world @ vertex.co for vertex in evaluated.data.vertices]
low = Vector((min(p.x for p in evaluated_points), min(p.y for p in evaluated_points), min(p.z for p in evaluated_points)))
high = Vector((max(p.x for p in evaluated_points), max(p.y for p in evaluated_points), max(p.z for p in evaluated_points)))
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
cam_data.ortho_scale = max(1.85, height) * 1.18
scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 768
scene.render.resolution_y = 1024
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
target = centre
distance = max(1.85, height) * 3
for name, direction in {"front": Vector((0, -1, 0)), "three-quarter": Vector((0.7, -0.7, 0)), "side": Vector((1, 0, 0)), "back": Vector((0, 1, 0))}.items():
    cam.location = target + direction.normalized() * distance + Vector((0, 0, 0.03))
    cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = os.path.join(OUT, f"EnvelopeProof-{name}.png")
    bpy.ops.render.render(write_still=True)

bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "Sacat-ModularBase-EnvelopeRig.blend"))
print("SACAT_ENVELOPE_RIG_PASS")
