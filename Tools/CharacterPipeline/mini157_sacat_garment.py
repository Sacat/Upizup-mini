"""MINI-157 - Sacat (Mainchar.fbx / Ch06) shirt+pants pass.

Ch06 is ONE fused skin+clothing mesh (confirmed via Unity batch inspection,
Logs/MINI-157-*.log) - unlike Franki's Ch28_* which already has separate
already-skinned Hoody/Pants objects. There is no hidden second "bare skin"
layer under the clothing here, so the topology-cut method used for Franki
(MINI-154) would leave a real hole where fabric is removed instead of
revealing skin underneath.

Method used instead, entirely texture/UV-based, ZERO topology change (no
vertex/face deleted or added -> no risk of shoulder gaps, seams, or holes):

1. Classify every polygon of the Ch06_body submesh by world-space position
   (bone-derived height/width thresholds, same technique as MINI-154) into:
   shirt / pants / reveal-skin-sleeve / reveal-skin-neck / unchanged.
2. For "reveal-skin-*" faces (the forearm past the new short-sleeve cut,
   the neck band above the new collar line): rasterize their existing UV
   triangles directly onto a COPY of the diffuse texture with the
   character's own sampled skin tone. The geometry already has the right
   arm/neck shape (it's a real anatomical mesh, just currently textured as
   cloth there) - repainting those pixels makes it read as bare skin with
   no geometry edit at all.
3. For "shirt"/"pants" faces: same rasterization technique, painted with
   the chosen garment colour instead.
4. Export a new FBX referencing the repainted texture. Original
   Mainchar.fbx and its textures are untouched.

Run headless:
  blender.exe --background --python Tools/CharacterPipeline/mini157_sacat_garment.py
"""

import bpy, bmesh, math, os, mathutils
import numpy as np

SOURCE_FBX = r"E:\Unity\Up Iz Up Mini\Assets\UpIzUpMini\Art\Characters\Mainchar.fbx"
OUT_DIR = r"E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-157"
os.makedirs(OUT_DIR, exist_ok=True)

SHIRT_COLOR = (0.09, 0.16, 0.32)   # navy polo, approved concept direction
PANTS_COLOR = (0.24, 0.24, 0.26)   # charcoal trouser, approved concept direction

bpy.ops.import_scene.fbx(filepath=SOURCE_FBX)
for obj in bpy.data.objects:
    obj.animation_data_clear()
bpy.context.scene.frame_set(0)
bpy.context.view_layer.update()
if bpy.data.objects.get('Cube'):
    bpy.data.objects.remove(bpy.data.objects['Cube'], do_unlink=True)

arm = [o for o in bpy.data.objects if o.type == 'ARMATURE'][0]


def bone_world(name):
    b = arm.data.bones[name]
    return arm.matrix_world @ b.head_local, arm.matrix_world @ b.tail_local


left_arm_h, left_arm_t = bone_world("mixamorig9:LeftArm")
neck_h, neck_t = bone_world("mixamorig9:Neck")
hips_h, hips_t = bone_world("mixamorig9:Hips")
left_foot_h, _ = bone_world("mixamorig9:LeftFoot")

sleeve_cut_x = left_arm_h.x + 0.45 * (left_arm_t.x - left_arm_h.x)
collar_cut_z = neck_h.z + 0.82 * (neck_t.z - neck_h.z)
hip_z = hips_t.z - 0.02
ankle_z = left_foot_h.z + 0.10

print(f"thresholds: sleeve_cut_x={sleeve_cut_x:.4f} collar_cut_z={collar_cut_z:.4f} hip_z={hip_z:.4f} ankle_z={ankle_z:.4f}")

ch06 = bpy.data.objects['Ch06']
me = ch06.data
mw = ch06.matrix_world
uv_layer = me.uv_layers['map1']

# ---------------------------------------------------------------
# Sample the character's own skin tone from vertices dominantly
# weighted to the Head bone (guaranteed skin, no guessing).
# ---------------------------------------------------------------
head_group_index = None
for vg in ch06.vertex_groups:
    if vg.name == "mixamorig9:Head":
        head_group_index = vg.index
        break
assert head_group_index is not None, "no Head vertex group"

head_uvs = []
for p in me.polygons:
    if p.material_index != 0:
        continue
    for li in p.loop_indices:
        vi = me.loops[li].vertex_index
        v = me.vertices[vi]
        for g in v.groups:
            if g.group == head_group_index and g.weight > 0.6:
                head_uvs.append(uv_layer.data[li].uv[:])
                break
assert len(head_uvs) > 20, f"too few head-weighted UVs found ({len(head_uvs)})"
head_uvs = np.array(head_uvs)
skin_uv_center = head_uvs.mean(axis=0)
print(f"skin reference UV (from {len(head_uvs)} head-weighted loops): {tuple(skin_uv_center)}")

# ---------------------------------------------------------------
# Load the diffuse texture, sample skin colour, build classification.
# ---------------------------------------------------------------
tex_path = r"E:\Unity\Up Iz Up Mini\Assets\UpIzUpMini\Art\Characters\Ch06_1001_Diffuse.png"
img = bpy.data.images.load(tex_path)
w, h = img.size
pixels = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)

def sample(u, v):
    x = int(np.clip(u, 0, 0.9999) * w)
    y = int(np.clip(v, 0, 0.9999) * h)
    return pixels[y, x, :3].copy()

skin_color = sample(skin_uv_center[0], skin_uv_center[1])
print(f"sampled skin colour: {tuple(skin_color)}")

head_group_idx = head_group_index
hand_group_idx = {vg.index for vg in ch06.vertex_groups if "Hand" in vg.name}


def dominant_group_is(poly, group_indices):
    """True if EVERY vertex of this polygon has its highest-weight group
    inside group_indices - i.e. this face is really that body part, not
    just spatially nearby (guards the reveal_neck/_sleeve painting from
    ever touching real head/hand geometry regardless of position)."""
    for li in poly.loop_indices:
        vi = me.loops[li].vertex_index
        v = me.vertices[vi]
        if not v.groups:
            return False
        best = max(v.groups, key=lambda g: g.weight)
        if best.group not in group_indices:
            return False
    return True


def classify(poly, avg_pos):
    x, y, z = avg_pos  # local (matches world scale/orientation via mw already applied by caller)
    ax = abs(x)
    # Real head/hand geometry (by dominant bone weight, not position) is
    # NEVER touched, however this face would otherwise be classified -
    # this must be checked first, not just used to steer the reveal_*
    # branches, or a head/hand face that also satisfies a later branch
    # (e.g. "z > hip_z") falls through and gets painted anyway.
    if dominant_group_is(poly, {head_group_idx}):
        return "unchanged"
    if hand_group_idx and dominant_group_is(poly, hand_group_idx):
        return "unchanged"
    if z > collar_cut_z and ax < 0.14:
        return "reveal_neck"
    if ax > sleeve_cut_x and z > hip_z:
        return "reveal_sleeve"
    if z > hip_z:
        return "shirt"
    if z > ankle_z:
        return "pants"
    return "unchanged"

def rasterize_tri(uv0, uv1, uv2, color):
    pts = np.array([uv0, uv1, uv2])
    px = pts[:, 0] * w
    py = pts[:, 1] * h
    minx, maxx = max(int(np.floor(px.min())), 0), min(int(np.ceil(px.max())), w - 1)
    miny, maxy = max(int(np.floor(py.min())), 0), min(int(np.ceil(py.max())), h - 1)
    if maxx <= minx or maxy <= miny:
        return
    xs = np.arange(minx, maxx + 1)
    ys = np.arange(miny, maxy + 1)
    gx, gy = np.meshgrid(xs + 0.5, ys + 0.5)
    x0, y0 = px[0], py[0]
    x1, y1 = px[1], py[1]
    x2, y2 = px[2], py[2]
    denom = (y1 - y2) * (x0 - x2) + (x2 - x1) * (y0 - y2)
    if abs(denom) < 1e-9:
        return
    a = ((y1 - y2) * (gx - x2) + (x2 - x1) * (gy - y2)) / denom
    b = ((y2 - y0) * (gx - x2) + (x0 - x2) * (gy - y2)) / denom
    c = 1 - a - b
    mask = (a >= -0.01) & (b >= -0.01) & (c >= -0.01)
    if not mask.any():
        return
    yy = ys[np.any(mask, axis=1)]
    region = pixels[miny:maxy + 1, minx:maxx + 1, :3]
    region[mask] = color
    pixels[miny:maxy + 1, minx:maxx + 1, :3] = region

counts = {"shirt": 0, "pants": 0, "reveal_sleeve": 0, "reveal_neck": 0, "unchanged": 0}
color_map = {
    "shirt": SHIRT_COLOR,
    "pants": PANTS_COLOR,
    "reveal_sleeve": tuple(skin_color),
    "reveal_neck": tuple(skin_color),
}

for p in me.polygons:
    if p.material_index != 0:
        continue
    loop_idx = list(p.loop_indices)
    verts_local = [me.vertices[me.loops[li].vertex_index].co for li in loop_idx]
    avg = mathutils.Vector((0, 0, 0))
    for v in verts_local:
        avg += v
    avg /= len(verts_local)
    avg_world = mw @ avg
    label = classify(p, avg_world)
    counts[label] += 1
    if label == "unchanged":
        continue
    color = color_map[label]
    uvs = [uv_layer.data[li].uv[:] for li in loop_idx]
    # fan-triangulate (loops are almost certainly already triangles post-FBX-import)
    for i in range(1, len(uvs) - 1):
        rasterize_tri(uvs[0], uvs[i], uvs[i + 1], color)

print("classification counts:", counts)

img.pixels[:] = pixels.flatten()
out_tex_path = os.path.join(OUT_DIR, "Ch06_1001_Diffuse_Reshaped.png")
img.filepath_raw = out_tex_path
img.file_format = 'PNG'
img.save()
print("saved", out_tex_path)

# New material pointing at the repainted texture, keep the original
# normal/spec/gloss maps (only the diffuse/base colour needed to change).
orig_mat = ch06.material_slots[0].material
new_mat = orig_mat.copy()
new_mat.name = "Ch06_body_Reshaped"
for node in new_mat.node_tree.nodes:
    if node.type == 'TEX_IMAGE' and node.image is not None and node.image.filepath == tex_path:
        new_mat.node_tree.nodes.remove(node)
# Re-link a fresh image texture node for the base colour input.
bsdf = next((n for n in new_mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
tex_node = new_mat.node_tree.nodes.new('ShaderNodeTexImage')
tex_node.image = img
if bsdf is not None:
    new_mat.node_tree.links.new(tex_node.outputs['Color'], bsdf.inputs['Base Color'])
ch06.material_slots[0].material = new_mat

# ---------------------------------------------------------------
# Save reusable Blender source + export FBX (geometry untouched).
# ---------------------------------------------------------------
blend_path = os.path.join(OUT_DIR, "Sacat-Garment-Prototype.blend")
bpy.ops.wm.save_as_mainfile(filepath=blend_path)
print("saved", blend_path)

for o in list(bpy.data.objects):
    if o.type in ('CAMERA', 'LIGHT'):
        bpy.data.objects.remove(o, do_unlink=True)
out_fbx = r"E:\Unity\Up Iz Up Mini\Assets\UpIzUpMini\Art\Characters\Garments\Sacat_Ch06_Reshaped.fbx"
os.makedirs(os.path.dirname(out_fbx), exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=out_fbx, use_selection=True, add_leaf_bones=False,
                          bake_anim=False, mesh_smooth_type='FACE', path_mode='COPY',
                          embed_textures=True)
print("exported", out_fbx)

# ---------------------------------------------------------------
# Render front/back/three-quarter preview.
# ---------------------------------------------------------------
meshes = [o for o in bpy.data.objects if o.type == 'MESH']
gmin = mathutils.Vector((1e9, 1e9, 1e9))
gmax = mathutils.Vector((-1e9, -1e9, -1e9))
for o in meshes:
    mwv = o.matrix_world
    for v in o.data.vertices:
        wc = mwv @ v.co
        gmin = mathutils.Vector((min(gmin.x, wc.x), min(gmin.y, wc.y), min(gmin.z, wc.z)))
        gmax = mathutils.Vector((max(gmax.x, wc.x), max(gmax.y, wc.y), max(gmax.z, wc.z)))
center = (gmin + gmax) / 2
height = gmax.z - gmin.z

scene = bpy.context.scene
engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items]
scene.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in engines else 'BLENDER_EEVEE'
scene.render.resolution_x = 900
scene.render.resolution_y = 1400
world = bpy.data.worlds.new("W_sacat")
scene.world = world
world.use_nodes = True
bg = world.node_tree.nodes.get("Background")
if bg:
    bg.inputs[0].default_value = (0.82, 0.82, 0.85, 1)
    bg.inputs[1].default_value = 1.0


def look_at(obj, target):
    d = target - obj.location
    obj.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()


def make_cam(name, loc, target, ortho_scale):
    cam_data = bpy.data.cameras.new(name)
    cam_data.type = 'ORTHO'
    cam_data.ortho_scale = ortho_scale
    cam_obj = bpy.data.objects.new(name, cam_data)
    scene.collection.objects.link(cam_obj)
    cam_obj.location = loc
    look_at(cam_obj, target)
    return cam_obj


sun = bpy.data.lights.new("SunS", type='SUN'); sun.energy = 3.0
sun_obj = bpy.data.objects.new("SunS", sun); scene.collection.objects.link(sun_obj)
sun_obj.rotation_euler = (math.radians(55), 0, math.radians(35))
fill = bpy.data.lights.new("FillS", type='SUN'); fill.energy = 1.5
fill_obj = bpy.data.objects.new("FillS", fill); scene.collection.objects.link(fill_obj)
fill_obj.rotation_euler = (math.radians(60), 0, math.radians(-140))

target = mathutils.Vector((center.x, center.y, center.z))
dist = 3.0
views = {
    "Sacat-Reshaped-Front": mathutils.Vector((0, -dist, center.z)),
    "Sacat-Reshaped-Back": mathutils.Vector((0, dist, center.z)),
    "Sacat-Reshaped-ThreeQuarter": mathutils.Vector((dist * 0.85, -dist * 0.85, center.z + 0.15)),
}
for name, loc in views.items():
    cam = make_cam(f"Cam_{name}", loc, target, ortho_scale=height * 1.15)
    scene.camera = cam
    scene.render.filepath = os.path.join(OUT_DIR, f"{name}.png")
    bpy.ops.render.render(write_still=True)
    print("Rendered", scene.render.filepath)
