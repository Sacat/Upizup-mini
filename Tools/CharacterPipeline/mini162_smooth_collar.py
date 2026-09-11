"""MINI-162 follow-up: one more targeted smoothing pass on the flattened
Franki collar rim, picking up from Codex's Franki-Shirt-NoHood.blend
(the hood is already flattened there - this only cleans up the remaining
jagged/overlapping collar-top edge the user flagged as 'awkward, looks
like the hoodie shape'). Arms are untouched (verified by assertion, same
as the flatten script)."""
import bpy, os, shutil, bmesh, json
ROOT = r'E:\Unity\Up Iz Up Mini'
OUT = ROOT + '/Logs/Tasks/MINI-162'
os.makedirs(OUT, exist_ok=True)
FBX = ROOT + '/Assets/UpIzUpMini/Art/Characters/Garments/Franki_ArmsRestored.fbx'

bpy.ops.wm.open_mainfile(filepath=OUT + '/Franki-Shirt-NoHood.blend')
o = bpy.data.objects['Ch28_Hoody']
mw = o.matrix_world

arm_before = [(v.index, tuple(v.co)) for v in o.data.vertices if abs((mw @ v.co).x) > .25]

bm = bmesh.new()
bm.from_mesh(o.data)
bm.verts.ensure_lookup_table()

# Target just the collar rim band (near the neck opening, top of the
# torso) - a tighter, more repeated smooth than the broad flatten pass
# used, specifically to remove the remaining jagged/overlapping edge
# right at the neckline rather than the whole upper back again.
collar_verts = [v for v in bm.verts if (mw @ v.co).z > 1.45 and abs((mw @ v.co).x) < .16]
print(f"collar_verts count: {len(collar_verts)}")
for _ in range(60):
    bmesh.ops.smooth_vert(bm, verts=collar_verts, factor=.5,
                           use_axis_x=True, use_axis_y=True, use_axis_z=True)
bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
bm.to_mesh(o.data)
bm.free()

assert all(tuple(o.data.vertices[i].co) == co for i, co in arm_before), 'arm vertices changed'

# Triangulate before export (MINI-161 lesson: Unity discarded an
# untriangulated face on the first export attempt).
bpy.context.view_layer.objects.active = o
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.mesh.quads_convert_to_tris()
bpy.ops.object.mode_set(mode='OBJECT')

arm = next(a for a in bpy.data.objects if a.type == 'ARMATURE')
bpy.ops.object.select_all(action='DESELECT')
o.select_set(True); arm.select_set(True)
bpy.ops.export_scene.fbx(filepath=FBX, use_selection=True, add_leaf_bones=False,
                          bake_anim=False, mesh_smooth_type='FACE', path_mode='AUTO')
bpy.ops.wm.save_as_mainfile(filepath=OUT + '/Franki-Shirt-CollarSmoothed.blend')
with open(OUT + '/collar-audit.json', 'w') as f:
    json.dump({'collarVerticesSmoothed': len(collar_verts), 'armVerticesUnchanged': len(arm_before)}, f, indent=2)
print('MINI162_COLLAR_SMOOTH_PASS', len(collar_verts))
