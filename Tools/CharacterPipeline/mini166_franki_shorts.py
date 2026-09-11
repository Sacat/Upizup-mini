"""MINI-166 Denim Shorts repair, Franki: same technique MINI-161 proved for
the arm gap, applied to the leg. Ch28_Pants_Reshaped_Colored is a FLAT
colour material (no texture, confirmed by direct inspection - base color
input has 0 links, default (0.24,0.24,0.26,1)) - simpler than the arm
repair, which had to rasterize onto a real texture. Here: bisect the
existing, already-correctly-skinned Ch28_Pants mesh at a knee-ish height,
classify faces below the cut, and give THEM a new flat skin-tone material
(sampled from Ch28_Body's own Head-bone-dominant texture region, same
proven method as MINI-161) instead of deleting geometry - the leg shape
underneath the pants fabric is already correct, it just needs to stop
looking like fabric below the hem.
"""
import bpy, bmesh, os
import numpy as np
import mathutils

ROOT = r'E:\Unity\Up Iz Up Mini'
OUT = ROOT + '/Logs/Tasks/MINI-166'
os.makedirs(OUT, exist_ok=True)
GARMENTS = ROOT + '/Assets/UpIzUpMini/Art/Characters/Garments'

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=GARMENTS + '/Franki_ReshapedGarments_Colored.fbx')
for o in bpy.data.objects:
    o.animation_data_clear()
bpy.context.scene.frame_set(0)
if bpy.data.objects.get('Cube'):
    bpy.data.objects.remove(bpy.data.objects['Cube'], do_unlink=True)

arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
pants = bpy.data.objects['Ch28_Pants']
body = bpy.data.objects['Ch28_Body']


def bone_world(name):
    b = arm.data.bones['mixamorig10:' + name]
    return arm.matrix_world @ b.head_local, arm.matrix_world @ b.tail_local


hip_head, hip_tail = bone_world('LeftUpLeg')   # hip -> knee
knee_head, knee_tail = bone_world('LeftLeg')   # knee -> ankle
hip_z = hip_head.z
knee_z = knee_head.z
# Shorts length: mid-thigh to just above the knee reads as denim shorts,
# not underwear or full trousers - 62% of the way from hip to knee.
cut_z = hip_z - 0.62 * (hip_z - knee_z)
print(f"hip_z={hip_z:.4f} knee_z={knee_z:.4f} cut_z={cut_z:.4f}")

# ---- sample Franki's real skin tone (identical method to MINI-161's proven arm repair) ----
img = bpy.data.images.load(ROOT + '/Assets/UpIzUpMini/Art/Characters/Ch28_1001_Diffuse.png')
w, h = img.size
pixels = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)
head_group = next(g.index for g in body.vertex_groups if g.name.endswith(':Head'))
uv = body.data.uv_layers.active.data
samples = []
for p in body.data.polygons:
    for li in p.loop_indices:
        v = body.data.vertices[body.data.loops[li].vertex_index]
        if any(g.group == head_group and g.weight > 0.6 for g in v.groups):
            u, t = uv[li].uv
            samples.append(pixels[min(h - 1, max(0, int(t * h))), min(w - 1, max(0, int(u * w))), :3])
assert len(samples) > 20
skin = np.median(np.array(samples), axis=0)
print(f"sampled skin colour: {tuple(skin)} from {len(samples)} texel samples")

# ---- bisect Ch28_Pants at the shorts-hem height, classify, recolour ----
bm = bmesh.new()
bm.from_mesh(pants.data)
mw = pants.matrix_world
mw_inv = mw.inverted()
# plane in pants-local space (bisect_plane operates in the object's own space)
local_point = mw_inv @ mathutils.Vector((0, 0, cut_z))
local_normal = mw_inv.to_3x3() @ mathutils.Vector((0, 0, 1))
bmesh.ops.bisect_plane(bm, geom=list(bm.verts) + list(bm.edges) + list(bm.faces),
                        plane_co=local_point, plane_no=local_normal, clear_inner=False, clear_outer=False)
bmesh.ops.triangulate(bm, faces=list(bm.faces))
bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
bm.to_mesh(pants.data)
bm.free()
pants.data.update()

skin_mat = bpy.data.materials.new('Franki_Shorts_LegSkin')
skin_mat.diffuse_color = (skin[0], skin[1], skin[2], 1.0)
bsdf = next(n for n in skin_mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
for link in list(bsdf.inputs['Base Color'].links):
    skin_mat.node_tree.links.remove(link)
bsdf.inputs['Base Color'].default_value = (skin[0], skin[1], skin[2], 1.0)
bsdf.inputs['Roughness'].default_value = 0.75
pants.data.materials.append(skin_mat)
skin_idx = len(pants.data.materials) - 1

me = pants.data
reveal_faces = 0
for p in me.polygons:
    c = mw @ p.center
    if c.z < cut_z:
        p.material_index = skin_idx
        reveal_faces += 1
print(f"leg-skin faces (below shorts hem): {reveal_faces} / {len(me.polygons)}")
assert reveal_faces > 20, "too few faces below the cut - check the cut height"

out = GARMENTS + '/Franki_Pants_Shorts_Denim.fbx'
bpy.ops.object.select_all(action='DESELECT')
pants.select_set(True)
arm.select_set(True)
bpy.ops.export_scene.fbx(filepath=out, use_selection=True, add_leaf_bones=False,
                          bake_anim=False, mesh_smooth_type='FACE', path_mode='AUTO')
bpy.ops.wm.save_as_mainfile(filepath=OUT + '/Franki-Shorts-Denim.blend')
print('MINI166_SHORTS_PASS')
