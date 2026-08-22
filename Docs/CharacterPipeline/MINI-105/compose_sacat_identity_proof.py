import bpy
import bmesh
import os
from collections import deque
from mathutils import Vector

BODY = r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-105\HitemCandidate01\Optimized\Sacat-ModularBase-Optimized.glb"
SACAT = r"E:\Unity\Up Iz Up Mini\Assets\UpIzUpMini\Art\Characters\Mainchar.fbx"
OUT = r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-105\IdentityCompositeProof"
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=BODY)
body = next(o for o in bpy.context.scene.objects if o.type == "MESH")
body.name = "Sacat_ModularBody_Donor"

# Remove the generated identity above the neck while retaining its torso and neck base.
bm = bmesh.new()
bm.from_mesh(body.data)
bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z > 1.615], context="VERTS")
bm.to_mesh(body.data)
bm.free()
body.data.update()

bpy.ops.import_scene.fbx(filepath=SACAT, use_anim=False)
source = [o for o in bpy.context.scene.objects if o.type == "MESH" and o != body][0]
source.name = "Sacat_ExactHead_Source"

# Identify the FBX's disconnected parts. Keep the exact face/head, eyes and
# eyelashes; discard clothing, hands, footwear, cap and headphones.
mesh = source.data
vertex_faces = [[] for _ in mesh.vertices]
for face in mesh.polygons:
    for vertex in face.vertices:
        vertex_faces[vertex].append(face.index)
unseen = set(range(len(mesh.polygons)))
islands = []
while unseen:
    seed = unseen.pop()
    queue = deque([seed])
    component = [seed]
    while queue:
        face_index = queue.popleft()
        for vertex in mesh.polygons[face_index].vertices:
            for neighbor in vertex_faces[vertex]:
                if neighbor in unseen:
                    unseen.remove(neighbor)
                    queue.append(neighbor)
                    component.append(neighbor)
    islands.append(component)
islands.sort(key=len, reverse=True)
keep_islands = {1, 11, 12, *range(18, len(islands))}
remove_faces = {face for index, island in enumerate(islands) if index not in keep_islands for face in island}
remove_vertices = {vertex for face_index in remove_faces for vertex in mesh.polygons[face_index].vertices}
bm = bmesh.new()
bm.from_mesh(mesh)
bm.verts.ensure_lookup_table()
bmesh.ops.delete(bm, geom=[bm.verts[index] for index in remove_vertices], context="VERTS")
bm.to_mesh(mesh)
bm.free()
mesh.update()

# Match the approved canonical 1.85 m scale while keeping the exact head shape.
scale = 1.85 / 1.8121
source.scale = (scale, scale, scale)
bpy.context.view_layer.objects.active = source
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

# Reconstruct only the close-cropped hair/scalp area hidden by the source cap.
bpy.ops.mesh.primitive_uv_sphere_add(segments=48, ring_count=28, location=(0.001 * scale, -0.004 * scale, 1.768 * scale))
hair = bpy.context.object
hair.name = "Sacat_CloseCroppedHair_Proof"
hair.scale = (0.083 * scale, 0.096 * scale, 0.074 * scale)
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
hair_bm = bmesh.new()
hair_bm.from_mesh(hair.data)
bmesh.ops.delete(hair_bm, geom=[v for v in hair_bm.verts if v.co.z < -0.050 * scale], context="VERTS")
hair_bm.to_mesh(hair.data)
hair_bm.free()
hair.data.update()
hair_mat = bpy.data.materials.new("SacatHairProof")
hair_mat.use_nodes = True
hair_mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.004, 0.006, 0.005, 1)
hair_mat.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.83
hair.data.materials.append(hair_mat)

# Neutral proof render.
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
    scene.render.filepath = os.path.join(OUT, f"IdentityProof-{name}.png")
    bpy.ops.render.render(write_still=True)

bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "Sacat-IdentityComposite-Proof.blend"))
print("SACAT_IDENTITY_COMPOSITE_PROOF_PASS")
