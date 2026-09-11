"""MINI-166 Shirt slot, round 1: build a real, distinguishable polo collar +
placket + buttons as new geometry, rigid-weighted to the Neck bone, then:

  Franki: joined onto a duplicate of the already-fitted Ch28_Hoody mesh
          (Franki_ArmsRestored.fbx) -> two real shirt meshes:
          - Franki_Shirt_Tee_Mike.fbx   (existing reshaped crew top, as-is)
          - Franki_Shirt_Polo_Lacos.fbx (same top + new collar/placket/buttons)

  Sacat:  Ch06 is one fused body+clothing mesh (MINI-154 finding) - cannot
          safely delete/replace its torso without risking a hole in a region
          with no underlying bare-skin geometry. Do NOT touch Ch06 this
          round. Instead export the collar/placket/buttons as a STANDALONE
          overlay object, rigid-weighted to mixamorig9:Neck, sized to
          Sacat's own neck - toggled on (polo) / off (tee, = current base
          look, unchanged) by OutfitWardrobe. This is a real, working, lower
          -risk slice for this session, not the final ideal (a true separate
          fitted torso mesh for Sacat is future work - documented as an open
          item, not silently substituted).

Boundary-loop check on Ch28_Hoody found ZERO open boundary edges - the
MINI-154/162 neckline flatten fully capped the opening (closed mesh). So the
collar is NOT extruded from an existing neckline hole; it is new geometry
built as a ring around the measured neck position/radius and merged in.
"""
import bpy, bmesh, math, os, sys
import mathutils

ROOT = r'E:\Unity\Up Iz Up Mini'
OUT = ROOT + '/Logs/Tasks/MINI-166'
os.makedirs(OUT, exist_ok=True)
GARMENTS = ROOT + '/Assets/UpIzUpMini/Art/Characters/Garments'


def make_flat_material(name, color, rough=0.6):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    bsdf = next((n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
    if bsdf is not None:
        for link in list(bsdf.inputs['Base Color'].links):
            mat.node_tree.links.remove(link)
        bsdf.inputs['Base Color'].default_value = color
        if 'Roughness' in bsdf.inputs:
            bsdf.inputs['Roughness'].default_value = rough
        if 'Metallic' in bsdf.inputs:
            bsdf.inputs['Metallic'].default_value = 0.0
    return mat


def build_collar_placket(neck_world, bone_radius_world, up_axis_scale, forward_sign=1.0):
    """Build collar ring + placket + 3 buttons around a neck position, in
    WORLD space (metres). Returns a new mesh object, not yet parented/skinned."""
    bm = bmesh.new()
    segs = 20
    collar_r_in = bone_radius_world * 1.12
    collar_r_out = bone_radius_world * 1.32
    collar_h = 0.028 * up_axis_scale
    base_z = neck_world.z - 0.01 * up_axis_scale
    ring_in_lo, ring_in_hi, ring_out_lo, ring_out_hi = [], [], [], []
    for i in range(segs):
        a = 2 * math.pi * i / segs
        dx, dy = math.cos(a), math.sin(a)
        ring_in_lo.append(bm.verts.new((neck_world.x + dx * collar_r_in, neck_world.y + dy * collar_r_in, base_z)))
        ring_in_hi.append(bm.verts.new((neck_world.x + dx * collar_r_in, neck_world.y + dy * collar_r_in, base_z + collar_h)))
        ring_out_lo.append(bm.verts.new((neck_world.x + dx * collar_r_out, neck_world.y + dy * collar_r_out, base_z)))
        ring_out_hi.append(bm.verts.new((neck_world.x + dx * collar_r_out, neck_world.y + dy * collar_r_out, base_z + collar_h * 0.75)))
    for i in range(segs):
        j = (i + 1) % segs
        bm.faces.new((ring_in_lo[i], ring_in_lo[j], ring_out_lo[j], ring_out_lo[i]))   # bottom skirt
        bm.faces.new((ring_out_hi[i], ring_out_hi[j], ring_in_hi[j], ring_in_hi[i]))   # top
        bm.faces.new((ring_out_lo[i], ring_out_lo[j], ring_out_hi[j], ring_out_hi[i])) # outer wall
        bm.faces.new((ring_in_hi[i], ring_in_hi[j], ring_in_lo[j], ring_in_lo[i]))     # inner wall

    # placket: thin strip down the front centre, plus 3 buttons
    front_dir = mathutils.Vector((0, -forward_sign, 0))
    front = neck_world + front_dir * collar_r_out * 0.95
    plack_w = bone_radius_world * 0.34
    plack_len = 0.16 * up_axis_scale
    plack_thick = 0.006 * up_axis_scale
    top_z = base_z
    bot_z = base_z - plack_len
    right = mathutils.Vector((1, 0, 0)) * plack_w * 0.5
    fwd_off = front_dir * plack_thick
    p_tl = bm.verts.new(tuple(front - right + mathutils.Vector((0, 0, top_z - front.z))))
    p_tr = bm.verts.new(tuple(front + right + mathutils.Vector((0, 0, top_z - front.z))))
    p_bl = bm.verts.new(tuple(front - right + mathutils.Vector((0, 0, bot_z - front.z))))
    p_br = bm.verts.new(tuple(front + right + mathutils.Vector((0, 0, bot_z - front.z))))
    p_tl2 = bm.verts.new(tuple(mathutils.Vector(p_tl.co) + fwd_off))
    p_tr2 = bm.verts.new(tuple(mathutils.Vector(p_tr.co) + fwd_off))
    p_bl2 = bm.verts.new(tuple(mathutils.Vector(p_bl.co) + fwd_off))
    p_br2 = bm.verts.new(tuple(mathutils.Vector(p_br.co) + fwd_off))
    bm.faces.new((p_tl2, p_tr2, p_br2, p_bl2))
    bm.faces.new((p_tl, p_bl, p_br, p_tr))
    bm.faces.new((p_tl, p_tl2, p_bl2, p_bl))
    bm.faces.new((p_tr, p_tr2, p_br2, p_br))
    bm.faces.new((p_tl, p_tr, p_tr2, p_tl2))
    bm.faces.new((p_bl, p_bl2, p_br2, p_br))

    for t in (0.15, 0.45, 0.75):
        cz = top_z + (bot_z - top_z) * t
        centre = mathutils.Vector((front.x, front.y, cz)) + fwd_off * 1.1
        bmesh.ops.create_uvsphere(bm, u_segments=8, v_segments=6, radius=plack_w * 0.22, matrix=mathutils.Matrix.Translation(centre))

    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new('CollarPlacket')
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new('CollarPlacket', me)
    bpy.context.collection.objects.link(obj)
    return obj


def build_franki():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=GARMENTS + '/Franki_ArmsRestored.fbx')
    for o in bpy.data.objects:
        o.animation_data_clear()
    if bpy.data.objects.get('Cube'):
        bpy.data.objects.remove(bpy.data.objects['Cube'], do_unlink=True)
    if bpy.data.objects.get('Camera'):
        bpy.data.objects.remove(bpy.data.objects['Camera'], do_unlink=True)
    if bpy.data.objects.get('Light'):
        bpy.data.objects.remove(bpy.data.objects['Light'], do_unlink=True)
    bpy.context.scene.frame_set(0)

    hoody = bpy.data.objects['Ch28_Hoody']
    arm = next(a for a in bpy.data.objects if a.type == 'ARMATURE')
    neck_bone = arm.data.bones['mixamorig10:Neck']
    neck_world = arm.matrix_world @ neck_bone.head_local
    mw = hoody.matrix_world
    # The mesh is a continuously tapering closed dome (MINI-162's flattened
    # hood cap), not a flat-ringed neckline - measured (not guessed): radius
    # is ~0.267 at the neck bone height itself (shoulder/chest, too wide for
    # a collar) and shrinks toward ~0.02 near the very top of the dome. The
    # actual neckline-sized band (where a collar realistically sits) is a
    # narrow height window just above the neck bone.
    collar_z = neck_world.z + 0.035
    near = [(mw @ v.co) for v in hoody.data.vertices if abs((mw @ v.co).z - collar_z) < 0.01]
    radius = 0.08
    if near:
        radius = sum(math.hypot(v.x - neck_world.x, v.y - neck_world.y) for v in near) / len(near)
    neck_world = mathutils.Vector((neck_world.x, neck_world.y, collar_z))
    print(f"Franki collar_z={collar_z:.4f} radius={radius:.4f} sample_n={len(near)}")

    # BUG FOUND BY RENDER (first attempt): wiping the mesh's materials to one
    # flat colour destroyed the MINI-157/161 "reveal skin via texture paint"
    # trick already baked into Franki_ArmsRestored's own texture (the
    # short-sleeve look is NOT separate geometry - it's the same long-sleeve
    # mesh with the forearm region repainted to skin tone). Overwriting that
    # material made the whole arm read as one long blue sleeve. Fix: do NOT
    # touch the original material/texture at all for the tee piece - export
    # it completely unmodified. tintSlots therefore stays empty for tee/polo
    # base (colour selection is a known limitation of this first pass, not
    # silently hidden - see MINI-166.md).
    tee_out = GARMENTS + '/Franki_Shirt_Tee_Mike.fbx'
    bpy.ops.object.select_all(action='DESELECT')
    hoody.select_set(True); arm.select_set(True)
    bpy.ops.export_scene.fbx(filepath=tee_out, use_selection=True, add_leaf_bones=False, bake_anim=False, mesh_smooth_type='FACE', path_mode='AUTO')
    print('wrote', tee_out)

    # ---- build polo = duplicate tee mesh + collar/placket, rigid-weighted to Neck ----
    polo_hoody = hoody.copy(); polo_hoody.data = hoody.data.copy(); polo_hoody.name = 'Ch28_Hoody_Polo'
    bpy.context.collection.objects.link(polo_hoody)
    collar = build_collar_placket(neck_world, radius, up_axis_scale=1.0, forward_sign=1.0)
    polo_mat = make_flat_material('Franki_Polo_Lacos', (0.08, 0.16, 0.30, 1.0))
    collar.data.materials.append(polo_mat)

    # join() preserves each object's own material slots (hoody's original,
    # untouched fabric/skin-reveal material stays on the body faces; the
    # collar's new polo_mat lands in its own appended slot on the collar
    # faces only) - do NOT clear/reassign materials after this, that was
    # the bug that wiped the skin-reveal paint the first time.
    bpy.ops.object.select_all(action='DESELECT')
    polo_hoody.select_set(True); collar.select_set(True)
    bpy.context.view_layer.objects.active = polo_hoody
    bpy.ops.object.join()

    # rigid-weight every vertex not already in a vertex group to Neck (the
    # new collar/placket/button verts have none yet - the base mesh's own
    # verts already carry their proven MINI-161 weights, untouched)
    vg = polo_hoody.vertex_groups.get('mixamorig10:Neck')
    if vg is None:
        vg = polo_hoody.vertex_groups.new(name='mixamorig10:Neck')
    for v in polo_hoody.data.vertices:
        if not v.groups:
            vg.add([v.index], 1.0, 'REPLACE')

    bpy.context.view_layer.objects.active = polo_hoody
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.quads_convert_to_tris()
    bpy.ops.object.mode_set(mode='OBJECT')

    polo_out = GARMENTS + '/Franki_Shirt_Polo_Lacos.fbx'
    bpy.ops.object.select_all(action='DESELECT')
    polo_hoody.select_set(True); arm.select_set(True)
    bpy.ops.export_scene.fbx(filepath=polo_out, use_selection=True, add_leaf_bones=False, bake_anim=False, mesh_smooth_type='FACE', path_mode='AUTO')
    print('wrote', polo_out)
    bpy.ops.wm.save_as_mainfile(filepath=OUT + '/Franki-Shirt-Slot.blend')


def build_sacat_overlay():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=ROOT + '/Assets/UpIzUpMini/Art/Characters/Mainchar.fbx')
    for o in bpy.data.objects:
        o.animation_data_clear()
    if bpy.data.objects.get('Cube'):
        bpy.data.objects.remove(bpy.data.objects['Cube'], do_unlink=True)
    bpy.context.scene.frame_set(0)

    ch06 = bpy.data.objects['Ch06']
    arm = next(a for a in bpy.data.objects if a.type == 'ARMATURE')
    neck_bone = arm.data.bones['mixamorig9:Neck']
    neck_world = arm.matrix_world @ neck_bone.head_local
    mw = ch06.matrix_world
    near = [(mw @ v.co) for v in ch06.data.vertices if abs((mw @ v.co).z - neck_world.z) < 0.03]
    radius = 0.06
    if near:
        radius = sum(math.hypot(v.x - neck_world.x, v.y - neck_world.y) for v in near) / len(near)
    print(f"Sacat neck_world={tuple(neck_world)} radius={radius:.4f}")

    collar = build_collar_placket(neck_world, radius, up_axis_scale=1.0, forward_sign=1.0)
    collar.name = 'Sacat_ShirtOverlay_Polo'
    polo_mat = make_flat_material('Sacat_Polo_Lacos', (0.08, 0.16, 0.30, 1.0))
    collar.data.materials.append(polo_mat)
    for p in collar.data.polygons:
        p.material_index = 0

    vg = collar.vertex_groups.new(name='mixamorig9:Neck')
    for v in collar.data.vertices:
        vg.add([v.index], 1.0, 'REPLACE')
    # BUG FOUND (all vertices collapsed to ~(0,0,0.01) after Unity import,
    # confirmed on the as-imported mesh, not a Unity-side bug at all):
    # `collar.parent = arm` sets a parent WITHOUT Blender auto-computing
    # matrix_parent_inverse (that only happens via the UI "Parent" operator),
    # so the FBX exporter had to compensate by baking arm's own transform
    # into the exported vertex data - collapsing the absolute-world-authored
    # coordinates this script builds. The Armature modifier alone (no scene
    # parent needed) is sufficient for deformation - Franki's collar never
    # needed parenting either, it was joined directly into an already-correct
    # mesh object.
    mod = collar.modifiers.new('Armature', 'ARMATURE')
    mod.object = arm

    bpy.context.view_layer.objects.active = collar
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.quads_convert_to_tris()
    bpy.ops.object.mode_set(mode='OBJECT')

    out = GARMENTS + '/Sacat_Shirt_Polo_Overlay.fbx'
    bpy.ops.object.select_all(action='DESELECT')
    collar.select_set(True); arm.select_set(True)
    bpy.ops.export_scene.fbx(filepath=out, use_selection=True, add_leaf_bones=False, bake_anim=False, mesh_smooth_type='FACE', path_mode='AUTO')
    print('wrote', out)
    bpy.ops.wm.save_as_mainfile(filepath=OUT + '/Sacat-Shirt-Overlay.blend')


build_franki()
build_sacat_overlay()
print('MINI166_SHIRT_SLOT_PASS')
