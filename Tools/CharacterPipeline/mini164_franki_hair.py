"""MINI-164 - make Franki's hair solid BLACK and clean.

MINI-163 gave Ch28_Hair its own material but (a) split off a pale skin-tone
FADE_COLOR band across the lower scalp that reads as white/grey in game, and
(b) applied a sine wave displacement that shattered the crown into noisy
shards. User: "no one hair should be white. black hair".

This pass:
1. One flat near-black material for the WHOLE hair mesh - no fade band.
2. No wave displacement. Instead a couple of gentle Laplacian smoothing
   passes to settle any spiky crown verts the broken texture used to hide.
3. Re-export over Franki_Hair.fbx (same path MINI-163 integration reads).
"""
import bpy, bmesh, os

ROOT = r'E:\Unity\Up Iz Up Mini'
OUT = ROOT + '/Logs/Tasks/MINI-164'
os.makedirs(OUT, exist_ok=True)
FBX_SRC = ROOT + '/Assets/UpIzUpMini/Art/Characters/Strong.fbx'
FBX_OUT = ROOT + '/Assets/UpIzUpMini/Art/Characters/Garments/Franki_Hair.fbx'

HAIR_COLOR = (0.020, 0.020, 0.022, 1.0)   # near-black, very slight cool tint

bpy.ops.import_scene.fbx(filepath=FBX_SRC)
for obj in bpy.data.objects:
    obj.animation_data_clear()
bpy.context.scene.frame_set(0)
if bpy.data.objects.get('Cube'):
    bpy.data.objects.remove(bpy.data.objects['Cube'], do_unlink=True)

hair = bpy.data.objects['Ch28_Hair']

# ---- gentle smoothing of the mesh (no displacement) ----
bm = bmesh.new()
bm.from_mesh(hair.data)
for _ in range(2):
    new_co = {}
    for v in bm.verts:
        if not v.link_edges:
            continue
        nb = [e.other_vert(v).co for e in v.link_edges]
        avg = sum(nb, type(v.co)((0, 0, 0))) / len(nb)
        new_co[v] = v.co.lerp(avg, 0.5)
    for v, co in new_co.items():
        v.co = co
bm.to_mesh(hair.data)
bm.free()
hair.data.update()

# ---- single flat black material for the entire mesh ----
# material.diffuse_color alone does NOT drive the Principled BSDF that Unity
# renders with (MINI-157/163 lesson) - set Base Color on the node directly.
def make_flat_material(name, color, rough=0.62):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    bsdf = next((n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
    if bsdf is not None:
        for link in list(bsdf.inputs['Base Color'].links):
            mat.node_tree.links.remove(link)
        bsdf.inputs['Base Color'].default_value = color
        if 'Roughness' in bsdf.inputs:
            bsdf.inputs['Roughness'].default_value = rough
        if 'Specular IOR Level' in bsdf.inputs:
            bsdf.inputs['Specular IOR Level'].default_value = 0.25
        if 'Metallic' in bsdf.inputs:
            bsdf.inputs['Metallic'].default_value = 0.0
    return mat

hair_mat = make_flat_material('Ch28_Hair_Black', HAIR_COLOR)
hair.data.materials.clear()
hair.data.materials.append(hair_mat)
for p in hair.data.polygons:
    p.material_index = 0

# triangulate before export (MINI-161 lesson)
bpy.context.view_layer.objects.active = hair
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.mesh.quads_convert_to_tris()
bpy.ops.object.mode_set(mode='OBJECT')

arm = next(a for a in bpy.data.objects if a.type == 'ARMATURE')
os.makedirs(os.path.dirname(FBX_OUT), exist_ok=True)
bpy.ops.object.select_all(action='DESELECT')
hair.select_set(True)
arm.select_set(True)
bpy.ops.export_scene.fbx(filepath=FBX_OUT, use_selection=True, add_leaf_bones=False,
                         bake_anim=False, mesh_smooth_type='FACE', path_mode='AUTO')
bpy.ops.wm.save_as_mainfile(filepath=OUT + '/Franki-Hair-Black.blend')
print('MINI164_FRANKI_HAIR_PASS')
