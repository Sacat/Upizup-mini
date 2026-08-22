import bpy
import os
from mathutils import Vector

BODY = r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-105\HitemCandidate01\Optimized\Sacat-ModularBase-Optimized.glb"
RIG_SOURCE = r"E:\Unity\Up Iz Up Mini\Assets\UpIzUpMini\Art\Characters\Mainchar.fbx"
OUT = r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-105\RigProof"
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=BODY)
body = next(o for o in bpy.context.scene.objects if o.type == "MESH")
body.name = "Sacat_ModularBase"
bpy.ops.import_scene.fbx(filepath=RIG_SOURCE, use_anim=False)
armature = next(o for o in bpy.context.scene.objects if o.type == "ARMATURE")
armature.name = "Sacat_CanonicalRig"
old_meshes = [o for o in bpy.context.scene.objects if o.type == "MESH" and o != body]
for obj in old_meshes:
    bpy.data.objects.remove(obj, do_unlink=True)

# Match the normalized 1.85 m base while preserving hierarchy and bone names.
rig_scale = 1.85 / 1.8121
armature.scale = (rig_scale, rig_scale, rig_scale)
bpy.context.view_layer.objects.active = armature
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

# Automatic initial deformation weights. This is a proof; production weights
# still require shoulder, crotch, finger, knee and ankle corrections.
bpy.ops.object.select_all(action="DESELECT")
body.select_set(True)
armature.select_set(True)
bpy.context.view_layer.objects.active = armature
bpy.ops.object.parent_set(type="ARMATURE_AUTO")

# Lightweight deformation test pose.
bpy.context.view_layer.objects.active = armature
bpy.ops.object.mode_set(mode="POSE")
for pose_bone in armature.pose.bones:
    pose_bone.rotation_mode = "XYZ"
for name, rotation in {
    "mixamorig9:LeftForeArm": (0.0, -0.55, 0.0),
    "mixamorig9:RightForeArm": (0.0, 0.55, 0.0),
    "mixamorig9:LeftLeg": (0.35, 0.0, 0.0),
    "mixamorig9:RightLeg": (0.12, 0.0, 0.0),
}.items():
    bone = armature.pose.bones.get(name)
    if bone:
        bone.rotation_euler = rotation
bpy.ops.object.mode_set(mode="OBJECT")
bpy.context.view_layer.update()

meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
points = [o.matrix_world @ Vector(corner) for o in meshes for corner in o.bound_box]
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
cam_data.ortho_scale = height * 1.2
scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 768
scene.render.resolution_y = 1024
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
target = Vector((centre.x, centre.y, low.z + height * 0.52))
distance = height * 3
for name, direction in {"front": Vector((0, -1, 0)), "three-quarter": Vector((0.7, -0.7, 0)), "side": Vector((1, 0, 0))}.items():
    cam.location = target + direction.normalized() * distance + Vector((0, 0, height * 0.02))
    cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = os.path.join(OUT, f"RigProof-{name}.png")
    bpy.ops.render.render(write_still=True)

bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "Sacat-ModularBase-RigProof.blend"))
print(f"SACAT_RIG_PROOF_PASS bones={len(armature.data.bones)} groups={len(body.vertex_groups)}")
