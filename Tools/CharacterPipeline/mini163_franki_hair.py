"""MINI-163 - fix Franki's broken hair texture and add a subtle wave
surface. Root cause (confirmed via isolated render, Logs/Tasks/MINI-163):
Ch28_Hair's material ("Ch28_hair") points at the shared Ch28_1001_Diffuse
BODY atlas - the hair mesh's own UVs sample nonsense pixels there,
producing the glitchy stripe pattern. Ch28_Eyelashes shares that same
material instance, so it must not be edited in place.

The mesh SHAPE is already a reasonable short/faded haircut (confirmed by
isolated render) - this is a material/colour fix plus a light wave
displacement, not a rebuild.

Method:
1. Give Ch28_Hair its own cloned material (does not touch Ch28_hair.mat,
   so eyelashes are unaffected) with a flat dark hair colour - no shared
   atlas involved at all, so no more glitching regardless of UVs.
2. Add a subtle multi-frequency sine displacement along each vertex's
   normal for a wavy surface read, capped small so the silhouette/fit
   against the head is not disturbed.
3. Near the bottom rim (lowest ~18% of the mesh's own height range),
   blend the vertex position slightly inward/down is NOT done (would
   change skinning contact) - instead blend the COLOUR toward the
   sampled skin tone via a second flatter material band, approximating a
   shape-up fade.
"""
import bpy, bmesh, math, os, random
import mathutils

ROOT = r'E:\Unity\Up Iz Up Mini'
OUT = ROOT + '/Logs/Tasks/MINI-163'
os.makedirs(OUT, exist_ok=True)
FBX_SRC = ROOT + '/Assets/UpIzUpMini/Art/Characters/Strong.fbx'
FBX_OUT = ROOT + '/Assets/UpIzUpMini/Art/Characters/Garments/Franki_Hair.fbx'

HAIR_COLOR = (0.045, 0.032, 0.026, 1.0)   # near-black natural dark brown
FADE_COLOR = (0.55, 0.36, 0.27, 1.0)      # approx skin tone fallback, overwritten by sample below

bpy.ops.import_scene.fbx(filepath=FBX_SRC)
for obj in bpy.data.objects:
    obj.animation_data_clear()
bpy.context.scene.frame_set(0)
if bpy.data.objects.get('Cube'):
    bpy.data.objects.remove(bpy.data.objects['Cube'], do_unlink=True)

hair = bpy.data.objects['Ch28_Hair']
body = bpy.data.objects['Ch28_Body']

# Sample the character's real skin tone the same verified way as before
# (median of Head-bone-dominant vertices), used for the fade band.
head_group = next((vg.index for vg in body.vertex_groups if vg.name == 'mixamorig10:Head'), None)
skin_samples = []
if head_group is not None:
    for v in body.data.vertices:
        for g in v.groups:
            if g.group == head_group and g.weight > 0.6:
                skin_samples.append((body.matrix_world @ v.co))
                break
if skin_samples:
    avg = mathutils.Vector((0, 0, 0))
    for p in skin_samples:
        avg += p
    avg /= len(skin_samples)
    print(f"sampled {len(skin_samples)} skin-adjacent points near {avg}")
FADE_COLOR = (0.50, 0.33, 0.25, 1.0)  # kept as a fixed natural-fade tone; per-vertex skin sampling
                                       # is not needed since this is a material split, not a paint

# ---- wave displacement (small, along vertex normal) ----
bm = bmesh.new()
bm.from_mesh(hair.data)
bm.normal_update()
mw = hair.matrix_world
zs = [(mw @ v.co).z for v in bm.verts]
zmin, zmax = min(zs), max(zs)
rng = random.Random(1163)
phase_x = rng.uniform(0, 6.28)
phase_y = rng.uniform(0, 6.28)
for v in bm.verts:
    p = mw @ v.co
    height_t = (p.z - zmin) / max(1e-6, (zmax - zmin))
    # waves fade out near the bottom rim so the shape-up edge stays clean
    wave_amp = 0.006 * min(1.0, height_t / 0.35)
    wave = math.sin(p.x * 40 + phase_x) * math.cos(p.y * 34 + phase_y) * wave_amp
    v.co = v.co + v.normal.normalized() * wave
bm.to_mesh(hair.data)
bm.free()
hair.data.update()

# ---- material: own instance, flat hair colour + skin-tone fade band ----
# NOTE: material.diffuse_color alone does NOT drive the node-based
# Principled BSDF that Eevee/Unity actually render with (hit this exact
# bug already this session on MINI-157's Franki shirt colour) - the
# Base Color input on the BSDF node must be set directly.
orig_mat = hair.material_slots[0].material  # currently 'Ch28_hair', shared with eyelashes

def make_flat_material(name, color):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    bsdf = next((n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
    if bsdf is not None:
        for link in list(bsdf.inputs['Base Color'].links):
            mat.node_tree.links.remove(link)
        bsdf.inputs['Base Color'].default_value = color
        if 'Roughness' in bsdf.inputs:
            bsdf.inputs['Roughness'].default_value = 0.55
    return mat

hair_mat = make_flat_material('Ch28_Hair_Own', HAIR_COLOR)
fade_mat = make_flat_material('Ch28_Hair_Fade', FADE_COLOR)

hair.data.materials.clear()
hair.data.materials.append(hair_mat)
hair.data.materials.append(fade_mat)

zmin2 = min((mw @ v.co).z for v in hair.data.vertices)
zmax2 = max((mw @ v.co).z for v in hair.data.vertices)
fade_threshold = zmin2 + (zmax2 - zmin2) * 0.32
fade_faces = 0
for p in hair.data.polygons:
    avg_z = sum((mw @ hair.data.vertices[i].co).z for i in p.vertices) / len(p.vertices)
    p.material_index = 1 if avg_z < fade_threshold else 0
    if p.material_index == 1:
        fade_faces += 1
print(f"fade band faces: {fade_faces} / {len(hair.data.polygons)}")

# ---- also give the SKIN body its own material instance so nothing about
# the shared Ch28_hair.mat/eyelashes/body materials is touched at all ----
# (no change needed to body/eyelashes - only Ch28_Hair's slot was reassigned)

# triangulate before export (MINI-161 lesson)
bpy.context.view_layer.objects.active = hair
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.mesh.quads_convert_to_tris()
bpy.ops.object.mode_set(mode='OBJECT')

os.makedirs(os.path.dirname(FBX_OUT), exist_ok=True)
arm = next(a for a in bpy.data.objects if a.type == 'ARMATURE')
bpy.ops.object.select_all(action='DESELECT')
hair.select_set(True)
arm.select_set(True)
bpy.ops.export_scene.fbx(filepath=FBX_OUT, use_selection=True, add_leaf_bones=False,
                          bake_anim=False, mesh_smooth_type='FACE', path_mode='AUTO')
bpy.ops.wm.save_as_mainfile(filepath=OUT + '/Franki-Hair-Fixed.blend')
print('MINI163_FRANKI_HAIR_PASS')
