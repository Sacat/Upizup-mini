import bpy
import math
import os
from mathutils import Vector

RIG_BLEND = r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-105\AccuRigOutput\Sacat-ModularBase-Rigged-Unity.blend"
SOURCE_BLEND = r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-105\AccuRigStage\Sacat-ModularBase-AccuRigStage.blend"
OUT = r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-105\AccuRigOutput\VisualProof"
OUT_BLEND = r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-105\AccuRigOutput\Sacat-ModularBase-Rigged-VisualProof.blend"
os.makedirs(OUT, exist_ok=True)

bpy.ops.wm.open_mainfile(filepath=RIG_BLEND)
meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
armatures = [obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE"]
if not meshes:
    raise RuntimeError("Rigged FBX audit blend has no mesh")
if len(armatures) != 1:
    raise RuntimeError(f"Expected one armature, found {len(armatures)}")

# AccuRIG's Unity FBX is Y-up. Preserve the FBX transforms and rotate the whole
# proof hierarchy into Blender's Z-up world without altering skinning.
axis_correction = bpy.data.objects.new("UnityToBlenderAxisProof", None)
bpy.context.collection.objects.link(axis_correction)
armatures[0].parent = axis_correction
axis_correction.rotation_euler.x = math.radians(90.0)
bpy.context.view_layer.update()

with bpy.data.libraries.load(SOURCE_BLEND, link=False) as (source, target):
    target.materials = [name for name in source.materials if name == "pbr_material"]
source_material = bpy.data.materials.get("pbr_material")
if source_material is None:
    raise RuntimeError("Could not append the approved Hitem material")

for image in bpy.data.images:
    if image.name == "Image_0" and image.size[0] > 2048:
        image.scale(2048, 2048)
        image.pack()

for mesh in meshes:
    mesh.data.materials.clear()
    mesh.data.materials.append(source_material)

def bounds(objects):
    points = []
    deps = bpy.context.evaluated_depsgraph_get()
    for obj in objects:
        evaluated = obj.evaluated_get(deps)
        points.extend(obj.matrix_world @ Vector(corner) for corner in evaluated.bound_box)
    low = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    high = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return low, high

low, high = bounds(meshes)
centre = (low + high) * 0.5
height = high.z - low.z

world = bpy.context.scene.world
if world is None:
    world = bpy.data.worlds.new("RigProofWorld")
    bpy.context.scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.012, 0.017, 0.028, 1)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.25

ground_mat = bpy.data.materials.new("ProofGround")
ground_mat.diffuse_color = (0.08, 0.095, 0.12, 1)
bpy.ops.mesh.primitive_plane_add(size=max(6.0, height * 3.0), location=(centre.x, centre.y, low.z - 0.005))
bpy.context.object.data.materials.append(ground_mat)

def area(name, location, energy, size, color):
    data = bpy.data.lights.new(name, "AREA")
    data.energy = energy
    data.shape = "DISK"
    data.size = size
    data.color = color
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    obj.rotation_euler = (centre - obj.location).to_track_quat("-Z", "Y").to_euler()

area("Key", centre + Vector((2.7, -3.2, height * 1.1)), 950, 3.2, (1.0, 0.86, 0.74))
area("Fill", centre + Vector((-2.8, -1.2, height * 0.75)), 600, 2.8, (0.55, 0.72, 1.0))
area("Rim", centre + Vector((1.0, 3.0, height * 1.3)), 850, 2.4, (0.65, 0.82, 1.0))

camera_data = bpy.data.cameras.new("RigProofCamera")
camera = bpy.data.objects.new("RigProofCamera", camera_data)
bpy.context.collection.objects.link(camera)
bpy.context.scene.camera = camera
camera.data.type = "ORTHO"
camera.data.ortho_scale = height * 1.18

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 576
scene.render.resolution_y = 768
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.image_settings.color_mode = "RGBA"
scene.render.film_transparent = False

target = Vector((centre.x, centre.y, low.z + height * 0.52))
distance = height * 3.0
views = {
    "front": Vector((0, -1, 0)),
    "three-quarter": Vector((0.70, -0.70, 0)),
    "back": Vector((0, 1, 0)),
}
for name, direction in views.items():
    camera.location = target + direction.normalized() * distance + Vector((0, 0, height * 0.02))
    camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = os.path.join(OUT, f"Sacat-Rigged-{name}.png")
    bpy.ops.render.render(write_still=True)

bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND)
print("ACCURIG_VISUAL_PROOF_PASS")
