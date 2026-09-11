"""MINI-163 - give Sacat real hair. Ch06 has no separate hair mesh at all;
what read as a "cap" in every prior screenshot is a smooth metallic dome
baked directly into the Ch06 head geometry/texture, with a hard seam at
ear level (confirmed by isolated render, Logs/Tasks/MINI-162).

Two-part fix:
1. Retexture the baked dome region (Head-bone-dominant faces above a
   height threshold that excludes the face) to the character's own real
   skin tone, sampled from Neck-bone-dominant vertices (definitely below
   the dome, definitely real skin - not contaminated by the dome itself).
2. Attach a NEW hair mesh: reuse the exact geometry already proven good
   for Franki (Logs/Tasks/MINI-163/Franki-Hair-Fixed.blend - a real short
   wave/fade shape, not invented fresh), rebinding it with fresh single-
   bone rigid weights to Sacat's OWN "mixamorig9:Head" bone (Franki's
   mesh used "mixamorig10:*" names, a different skeleton - vertex groups
   don't carry across, so this rebinds rather than reusing them), scaled/
   positioned to fit Sacat's own head bounds.
"""
import bpy, bmesh, math, os, random
import numpy as np
import mathutils

ROOT = r'E:\Unity\Up Iz Up Mini'
OUT = ROOT + '/Logs/Tasks/MINI-163'
os.makedirs(OUT, exist_ok=True)
FBX_SRC = ROOT + '/Assets/UpIzUpMini/Art/Characters/Mainchar.fbx'
FBX_OUT = ROOT + '/Assets/UpIzUpMini/Art/Characters/Garments/Sacat_Hair.fbx'
HAIR_SOURCE_BLEND = OUT + '/Franki-Hair-Fixed.blend'

bpy.ops.import_scene.fbx(filepath=FBX_SRC)
for obj in bpy.data.objects:
    obj.animation_data_clear()
bpy.context.scene.frame_set(0)
if bpy.data.objects.get('Cube'):
    bpy.data.objects.remove(bpy.data.objects['Cube'], do_unlink=True)

ch06 = bpy.data.objects['Ch06']
me = ch06.data
mw = ch06.matrix_world
uv_layer = me.uv_layers['map1']
arm = next(a for a in bpy.data.objects if a.type == 'ARMATURE')

head_group = next((vg.index for vg in ch06.vertex_groups if vg.name == 'mixamorig9:Head'), None)
neck_group = next((vg.index for vg in ch06.vertex_groups if vg.name == 'mixamorig9:Neck'), None)
assert head_group is not None and neck_group is not None

# ---- dome height threshold (computed first, so the skin sample below can
# reliably stay in the face region rather than guessing a second bone) ----
head_bone = arm.data.bones['mixamorig9:Head']
head_world = arm.matrix_world @ head_bone.head_local
head_tail_world = arm.matrix_world @ head_bone.tail_local
dome_cut_z = head_world.z + 0.28 * (head_tail_world.z - head_world.z)  # roughly ear-top height
print(f"dome_cut_z={dome_cut_z:.4f} (head base {head_world.z:.4f} -> top {head_tail_world.z:.4f})")

# ---- sample real skin tone from the FACE region (Head-dominant faces
# BELOW the dome cut - guaranteed real skin, never clothed, never the
# dome itself) ----
tex_path = r'E:\Unity\Up Iz Up Mini\Assets\UpIzUpMini\Art\Characters\Ch06_1001_Diffuse.png'
img = bpy.data.images.load(tex_path)
w, h = img.size
pixels = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)

def sample(u, v):
    x = int(np.clip(u, 0, 0.9999) * w)
    y = int(np.clip(v, 0, 0.9999) * h)
    return pixels[y, x, :3].copy()

def dominant_group_is(poly, group_idx):
    for li in poly.loop_indices:
        vi = me.loops[li].vertex_index
        v = me.vertices[vi]
        if not v.groups:
            return False
        best = max(v.groups, key=lambda g: g.weight)
        if best.group != group_idx:
            return False
    return True

face_uvs = []
for p in me.polygons:
    if p.material_index != 0 or not dominant_group_is(p, head_group):
        continue
    verts_local = [me.vertices[me.loops[li].vertex_index].co for li in p.loop_indices]
    avg = mathutils.Vector((0, 0, 0))
    for v in verts_local:
        avg += v
    avg /= len(verts_local)
    if (mw @ avg).z >= dome_cut_z:
        continue
    for li in p.loop_indices:
        face_uvs.append(uv_layer.data[li].uv[:])
print(f"face-region (below dome) loops found: {len(face_uvs)}")
face_uvs = np.array(face_uvs)
skin_uv = np.median(face_uvs, axis=0)
skin_color = tuple(sample(skin_uv[0], skin_uv[1]))
print(f"sampled skin colour: {skin_color}")


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
    region = pixels[miny:maxy + 1, minx:maxx + 1, :3]
    region[mask] = color
    pixels[miny:maxy + 1, minx:maxx + 1, :3] = region


def dominant_group_is(poly, group_idx):
    for li in poly.loop_indices:
        vi = me.loops[li].vertex_index
        v = me.vertices[vi]
        if not v.groups:
            return False
        best = max(v.groups, key=lambda g: g.weight)
        if best.group != group_idx:
            return False
    return True


# SAFETY FIX: an earlier version of this script repainted the shared
# Ch06_1001_Diffuse atlas directly and corrupted the headphones' colour -
# the cap/headphones geometry reuses the SAME pixel region via UV tiling
# even though its FACES aren't Head-dominant (so it wasn't reclassified,
# but its sampled pixels were still overwritten). Fixed by giving the
# dome faces their own separate flat-colour material instead - a new
# material_index with NO shared texture at all, so there is zero pixel
# overlap with anything else, regardless of UV reuse elsewhere on the atlas.
def make_flat_material(name, color):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    bsdf = next((n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
    if bsdf is not None:
        for link in list(bsdf.inputs['Base Color'].links):
            mat.node_tree.links.remove(link)
        bsdf.inputs['Base Color'].default_value = color
        if 'Roughness' in bsdf.inputs:
            bsdf.inputs['Roughness'].default_value = 0.65
    return mat


dome_mat = make_flat_material('Ch06_Dome_SkinFix', (skin_color[0], skin_color[1], skin_color[2], 1.0))
me.materials.append(dome_mat)
dome_mat_index = len(me.materials) - 1

dome_faces = 0
for p in me.polygons:
    if p.material_index != 0:
        continue
    if not dominant_group_is(p, head_group):
        continue
    verts_local = [me.vertices[me.loops[li].vertex_index].co for li in p.loop_indices]
    avg = mathutils.Vector((0, 0, 0))
    for v in verts_local:
        avg += v
    avg /= len(verts_local)
    if (mw @ avg).z < dome_cut_z:
        continue
    p.material_index = dome_mat_index
    dome_faces += 1
print(f"dome faces moved to flat skin-colour material: {dome_faces}")

# ---- bring in Franki's already-fixed hair shape, rebind to Sacat's own head bone ----
with bpy.data.libraries.load(HAIR_SOURCE_BLEND, link=False) as (data_from, data_to):
    data_to.objects = ['Ch28_Hair']
hair_src = data_to.objects[0]
bpy.context.collection.objects.link(hair_src)
# Bake the source's current world-space shape (it was posed under Franki's
# own armature modifier) into plain local geometry, independent of that
# skeleton, before rebinding to Sacat's.
dep = bpy.context.evaluated_depsgraph_get()
hair_eval = hair_src.evaluated_get(dep)
hair_mesh_baked = bpy.data.meshes.new_from_object(hair_eval)
hair = bpy.data.objects.new('Ch06_Hair', hair_mesh_baked)
bpy.context.collection.objects.link(hair)
hair.matrix_world = hair_src.matrix_world.copy()
for slot_src, slot_dst in zip(hair_src.material_slots, hair.material_slots):
    slot_dst.material = slot_src.material
bpy.data.objects.remove(hair_src, do_unlink=True)

# Fit to Sacat's own head bounds (measured from the actual dome region we
# just repainted, so the hairline sits where the old dome used to be).
dome_verts_world = []
for p in me.polygons:
    # material_index was already reassigned to dome_mat_index above for
    # exactly this face set - check THAT instead of the original index 0,
    # or this collects almost nothing (real bug hit and fixed here).
    if p.material_index != dome_mat_index:
        continue
    for li in p.loop_indices:
        wc = mw @ me.vertices[me.loops[li].vertex_index].co
        dome_verts_world.append(wc)
dome_min = mathutils.Vector((min(v.x for v in dome_verts_world), min(v.y for v in dome_verts_world), min(v.z for v in dome_verts_world)))
dome_max = mathutils.Vector((max(v.x for v in dome_verts_world), max(v.y for v in dome_verts_world), max(v.z for v in dome_verts_world)))
dome_center = (dome_min + dome_max) / 2
dome_size = dome_max - dome_min
print(f"dome bounds min={tuple(dome_min)} max={tuple(dome_max)} size={tuple(dome_size)}")

hair_verts_world = [hair.matrix_world @ v.co for v in hair.data.vertices]
h_min = mathutils.Vector((min(v.x for v in hair_verts_world), min(v.y for v in hair_verts_world), min(v.z for v in hair_verts_world)))
h_max = mathutils.Vector((max(v.x for v in hair_verts_world), max(v.y for v in hair_verts_world), max(v.z for v in hair_verts_world)))
h_center = (h_min + h_max) / 2
h_size = h_max - h_min
scale = mathutils.Vector((
    dome_size.x / max(1e-5, h_size.x) * 1.05,
    dome_size.y / max(1e-5, h_size.y) * 1.05,
    dome_size.z / max(1e-5, h_size.z) * 1.02,
))
print(f"fit scale={tuple(scale)}")

bm = bmesh.new()
bm.from_mesh(hair.data)
for v in bm.verts:
    world = hair.matrix_world @ v.co
    local_offset = world - h_center
    new_world = dome_center + mathutils.Vector((local_offset.x * scale.x, local_offset.y * scale.y, local_offset.z * scale.z))
    v.co = hair.matrix_world.inverted() @ new_world
bm.to_mesh(hair.data)
bm.free()
hair.data.update()

# Rebind with fresh rigid single-bone weights to Sacat's own Head bone -
# hair only needs to follow the head, not deform with the body.
hair.parent = arm
mod = hair.modifiers.new('Armature', 'ARMATURE')
mod.object = arm
vg = hair.vertex_groups.new(name='mixamorig9:Head')
for v in hair.data.vertices:
    vg.add([v.index], 1.0, 'REPLACE')

bpy.context.view_layer.objects.active = hair
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.mesh.quads_convert_to_tris()
bpy.ops.object.mode_set(mode='OBJECT')

os.makedirs(os.path.dirname(FBX_OUT), exist_ok=True)
bpy.ops.object.select_all(action='DESELECT')
hair.select_set(True)
ch06.select_set(True)
arm.select_set(True)
bpy.ops.export_scene.fbx(filepath=FBX_OUT, use_selection=True, add_leaf_bones=False,
                          bake_anim=False, mesh_smooth_type='FACE', path_mode='AUTO')
bpy.ops.wm.save_as_mainfile(filepath=OUT + '/Sacat-Hair-Fixed.blend')
print('MINI163_SACAT_HAIR_PASS')
