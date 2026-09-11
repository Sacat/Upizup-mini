"""MINI-164 - give Sacat BLACK hair, the low-risk way.

Sacat has no separate hair mesh - a smooth glossy dome is baked into the
Ch06 head geometry (MINI-162/163 finding). The MINI-163 attempt to
transplant Franki's hair mesh onto his head via a bounding-box remap was
distorted and rejected.

This pass does NOT transplant geometry. It just retextures the existing
baked dome faces to a MATTE BLACK flat material so the dome reads as a
short black haircut instead of a shiny skin-coloured dome. Uses a separate
material index with no shared-atlas pixels (MINI-163 lesson: painting the
shared Ch06_1001_Diffuse atlas corrupted the headphones via UV tiling).

Classification is copied verbatim from mini163_sacat_hair.py (proven): the
face set is Ch06's material-0, Head-bone-dominant polygons whose centroid
is above an ear-top height threshold.
"""
import bpy, os, mathutils

ROOT = r'E:\Unity\Up Iz Up Mini'
OUT = ROOT + '/Logs/Tasks/MINI-164'
os.makedirs(OUT, exist_ok=True)
FBX_SRC = ROOT + '/Assets/UpIzUpMini/Art/Characters/Mainchar.fbx'
FBX_OUT = ROOT + '/Assets/UpIzUpMini/Art/Characters/Garments/Sacat_Hair.fbx'

HAIR_COLOR = (0.020, 0.020, 0.022, 1.0)   # near-black matte

bpy.ops.import_scene.fbx(filepath=FBX_SRC)
for obj in bpy.data.objects:
    obj.animation_data_clear()
bpy.context.scene.frame_set(0)
if bpy.data.objects.get('Cube'):
    bpy.data.objects.remove(bpy.data.objects['Cube'], do_unlink=True)

ch06 = bpy.data.objects['Ch06']
me = ch06.data
mw = ch06.matrix_world
arm = next(a for a in bpy.data.objects if a.type == 'ARMATURE')

head_group = next((vg.index for vg in ch06.vertex_groups if vg.name == 'mixamorig9:Head'), None)
assert head_group is not None

head_bone = arm.data.bones['mixamorig9:Head']
head_world = arm.matrix_world @ head_bone.head_local
head_tail_world = arm.matrix_world @ head_bone.tail_local
dome_cut_z = head_world.z + 0.28 * (head_tail_world.z - head_world.z)   # ear-top height
print(f"dome_cut_z={dome_cut_z:.4f} (head base {head_world.z:.4f} -> top {head_tail_world.z:.4f})")


def dominant_group_is(poly, group_idx):
    for li in poly.loop_indices:
        v = me.vertices[me.loops[li].vertex_index]
        if not v.groups:
            return False
        if max(v.groups, key=lambda g: g.weight).group != group_idx:
            return False
    return True


def make_flat_material(name, color):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    bsdf = next((n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
    if bsdf is not None:
        for link in list(bsdf.inputs['Base Color'].links):
            mat.node_tree.links.remove(link)
        bsdf.inputs['Base Color'].default_value = color
        if 'Roughness' in bsdf.inputs:
            bsdf.inputs['Roughness'].default_value = 0.78   # matte, kill the plastic sheen
        if 'Specular IOR Level' in bsdf.inputs:
            bsdf.inputs['Specular IOR Level'].default_value = 0.18
        if 'Metallic' in bsdf.inputs:
            bsdf.inputs['Metallic'].default_value = 0.0
    return mat


dome_mat = make_flat_material('Ch06_Hair_Black', HAIR_COLOR)
me.materials.append(dome_mat)
dome_mat_index = len(me.materials) - 1

dome_faces = 0
for p in me.polygons:
    if p.material_index != 0 or not dominant_group_is(p, head_group):
        continue
    avg = mathutils.Vector((0, 0, 0))
    for li in p.loop_indices:
        avg += me.vertices[me.loops[li].vertex_index].co
    avg /= len(p.loop_indices)
    if (mw @ avg).z < dome_cut_z:
        continue
    p.material_index = dome_mat_index
    dome_faces += 1
print(f"dome faces -> matte black material: {dome_faces}")
assert dome_faces > 50, "too few dome faces classified - check thresholds"

bpy.context.view_layer.objects.active = ch06
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.mesh.quads_convert_to_tris()
bpy.ops.object.mode_set(mode='OBJECT')

os.makedirs(os.path.dirname(FBX_OUT), exist_ok=True)
bpy.ops.object.select_all(action='DESELECT')
ch06.select_set(True)
arm.select_set(True)
bpy.ops.export_scene.fbx(filepath=FBX_OUT, use_selection=True, add_leaf_bones=False,
                         bake_anim=False, mesh_smooth_type='FACE', path_mode='AUTO')
bpy.ops.wm.save_as_mainfile(filepath=OUT + '/Sacat-Hair-Black.blend')
print('MINI164_SACAT_HAIR_PASS')
