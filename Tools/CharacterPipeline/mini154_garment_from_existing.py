"""MINI-154 - reshape the EXISTING, already-skinned Ch28_Hoody / Ch28_Pants
meshes (Strong.fbx / Sacat) into a short-sleeve crew top and a straighter-leg
trouser, instead of building new isolated garment geometry from scratch.

Why this method: see Docs/WorkPackets/MINI-154.md. MINI-148 (scan/decimate)
and MINI-150 (isolated new shell) both failed visual review. This script
edits topology directly on a mesh that is already one continuous surface,
already skin-weighted to the character's proven-animating skeleton, and
already fitted to the body - no weight transfer, no new bind pose, no new
seam between disconnected pieces.

Axis note (verified empirically, see Logs/Tasks/MINI-154 investigation):
Ch28_Hoody/Ch28_Pants raw mesh-local axes are Y-up (local Y = height,
local Z = depth, local X = width). The object's matrix_world rotates this
into Blender's Z-up world space at 0.01 scale. Every cut in this script
is expressed directly in LOCAL mesh coordinates using that mapping, to
avoid the axis-mixup bug an earlier draft of this script had.

Run headless:
  blender.exe --background --python Tools/CharacterPipeline/mini154_garment_from_existing.py

Outputs (Logs/Tasks/MINI-154/):
  - Sacat-Garment-Prototype.blend (reusable source)
  - Reshaped-Front.png / Reshaped-Back.png / Reshaped-ThreeQuarter.png (static preview)

This script does NOT touch anything under Assets/ and does NOT integrate
into Unity. It is a preview step only, gated on user visual approval.
"""

import bpy, bmesh, math, os, mathutils

SOURCE_FBX = r"E:\Unity\Up Iz Up Mini\Assets\UpIzUpMini\Art\Characters\Strong.fbx"
OUT_DIR = r"E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-154"
os.makedirs(OUT_DIR, exist_ok=True)

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


left_arm_h = bone_world("mixamorig10:LeftArm")[0]
left_arm_tail = bone_world("mixamorig10:LeftArm")[1]  # elbow
neck_head, neck_tail = bone_world("mixamorig10:Neck")  # base of neck, top of neck (near chin)
left_leg_h = bone_world("mixamorig10:LeftLeg")[0]      # knee
left_foot_h = bone_world("mixamorig10:LeftFoot")[0]    # ankle

SCALE = 0.01  # object matrix_world uniform scale, confirmed empirically


def world_height_to_local_y(world_z):
    return world_z / SCALE


def world_width_to_local_x(world_x):
    return world_x / SCALE


# ---------------------------------------------------------------
# SHIRT: Ch28_Hoody -> short-sleeve crew top (remove hood + tall collar)
# ---------------------------------------------------------------
hoody = bpy.data.objects['Ch28_Hoody']

sleeve_t = 0.45  # 45% shoulder->elbow = classic short-sleeve length
sleeve_cut_x_world = left_arm_h.x + sleeve_t * (left_arm_tail.x - left_arm_h.x)
sleeve_cut_x_local = world_width_to_local_x(sleeve_cut_x_world)

# collar/hood cut in local HEIGHT (Y): a real crew neckline sits near the
# TOP of the neck (close to the jaw), not its base at the shoulder line -
# cutting at neck_head would remove the whole neck tube and gape the
# shoulders open. Use a point most of the way up the neck bone instead.
collar_cut_y_local = world_height_to_local_y(neck_head.z + 0.82 * (neck_tail.z - neck_head.z))

bm = bmesh.new()
bm.from_mesh(hoody.data)
bm.verts.ensure_lookup_table()


def bisect_and_delete_beyond(bm, plane_co, plane_no, keep_side_sign, axis_index):
    geom = list(bm.verts) + list(bm.edges) + list(bm.faces)
    bmesh.ops.bisect_plane(
        bm, geom=geom, plane_co=plane_co, plane_no=plane_no,
        clear_inner=False, clear_outer=False,
    )
    bm.verts.ensure_lookup_table()
    to_delete = [v for v in bm.verts if (v.co[axis_index] - plane_co[axis_index]) * keep_side_sign < -1e-6]
    bmesh.ops.delete(bm, geom=to_delete, context='VERTS')


# Left sleeve: local X axis, keep X <= cut
bisect_and_delete_beyond(bm, mathutils.Vector((sleeve_cut_x_local, 0, 0)), mathutils.Vector((1, 0, 0)), keep_side_sign=-1, axis_index=0)
# Right sleeve: mirror
bisect_and_delete_beyond(bm, mathutils.Vector((-sleeve_cut_x_local, 0, 0)), mathutils.Vector((1, 0, 0)), keep_side_sign=1, axis_index=0)
# Collar + hood: local Y axis (height), keep Y <= cut
bisect_and_delete_beyond(bm, mathutils.Vector((0, collar_cut_y_local, 0)), mathutils.Vector((0, 1, 0)), keep_side_sign=-1, axis_index=1)

bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
bm.to_mesh(hoody.data)
hoody.data.update()
bm.free()
print(f"SHIRT: sleeve_cut_x_world={sleeve_cut_x_world:.4f} collar_cut_y_local={collar_cut_y_local:.2f} "
      f"result verts={len(hoody.data.vertices)}")

# ---------------------------------------------------------------
# PANTS: Ch28_Pants -> relax the jogger ankle cuff toward a straight leg.
# Cross-section perpendicular to height(Y) is the local X-Z plane.
# ---------------------------------------------------------------
pants = bpy.data.objects['Ch28_Pants']

knee_y_local = world_height_to_local_y(left_leg_h.z)
ankle_y_local = world_height_to_local_y(left_foot_h.z)
cuff_band_top_y_local = world_height_to_local_y(left_foot_h.z + 0.16)  # relax the lowest ~16cm


def leg_radius_at(mesh, side_sign, y_local_target, tol):
    pts = []
    for v in mesh.vertices:
        if abs(v.co.y - y_local_target) < tol and (v.co.x * side_sign) > 0:
            pts.append(v.co)
    if not pts:
        return None, None
    cx = sum(p.x for p in pts) / len(pts)
    cz = sum(p.z for p in pts) / len(pts)
    r = sum(math.hypot(p.x - cx, p.z - cz) for p in pts) / len(pts)
    return (cx, cz), r


for side_sign, label in ((1, "left"), (-1, "right")):
    center, target_r = leg_radius_at(pants.data, side_sign, knee_y_local, tol=2.0)
    if center is None:
        print(f"PANTS: no knee-band verts found for {label} leg (y~{knee_y_local:.1f}), skipping")
        continue
    cx, cz = center
    count = 0
    for v in pants.data.vertices:
        if ankle_y_local - 1.0 <= v.co.y <= cuff_band_top_y_local and (v.co.x * side_sign) > 0:
            dx = v.co.x - cx
            dz = v.co.z - cz
            r = math.hypot(dx, dz)
            if r < 1e-6 or r >= target_r:
                continue
            scale = min(target_r / r, 1.6)  # cap the expansion to avoid blow-ups
            v.co.x = cx + dx * scale
            v.co.z = cz + dz * scale
            count += 1
    print(f"PANTS: relaxed {count} verts on {label} leg toward knee-band radius {target_r:.2f} (local units)")

pants.data.update()

# ---------------------------------------------------------------
# Save reusable Blender source
# ---------------------------------------------------------------
blend_path = os.path.join(OUT_DIR, "Sacat-Garment-Prototype.blend")
bpy.ops.wm.save_as_mainfile(filepath=blend_path)
print("Saved", blend_path)

# ---------------------------------------------------------------
# Render front/back/three-quarter preview (whole character, reshaped garments)
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
world = bpy.data.worlds.new("W2")
scene.world = world
world.use_nodes = True
bg = world.node_tree.nodes.get("Background")
if bg:
    bg.inputs[0].default_value = (0.82, 0.82, 0.85, 1)
    bg.inputs[1].default_value = 1.0


def look_at(obj, target):
    direction = target - obj.location
    obj.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()


def make_cam(name, loc, target, ortho_scale):
    cam_data = bpy.data.cameras.new(name)
    cam_data.type = 'ORTHO'
    cam_data.ortho_scale = ortho_scale
    cam_obj = bpy.data.objects.new(name, cam_data)
    scene.collection.objects.link(cam_obj)
    cam_obj.location = loc
    look_at(cam_obj, target)
    return cam_obj


sun = bpy.data.lights.new("Sun2", type='SUN'); sun.energy = 3.0
sun_obj = bpy.data.objects.new("Sun2", sun); scene.collection.objects.link(sun_obj)
sun_obj.rotation_euler = (math.radians(55), 0, math.radians(35))
fill = bpy.data.lights.new("Fill2", type='SUN'); fill.energy = 1.5
fill_obj = bpy.data.objects.new("Fill2", fill); scene.collection.objects.link(fill_obj)
fill_obj.rotation_euler = (math.radians(60), 0, math.radians(-140))

target = mathutils.Vector((center.x, center.y, center.z))
dist = 3.0
views = {
    "Reshaped-Front": mathutils.Vector((0, -dist, center.z)),
    "Reshaped-Back": mathutils.Vector((0, dist, center.z)),
    "Reshaped-ThreeQuarter": mathutils.Vector((dist * 0.85, -dist * 0.85, center.z + 0.15)),
}
for name, loc in views.items():
    cam = make_cam(f"Cam_{name}", loc, target, ortho_scale=height * 1.15)
    scene.camera = cam
    scene.render.filepath = os.path.join(OUT_DIR, f"{name}.png")
    bpy.ops.render.render(write_still=True)
    print("Rendered", scene.render.filepath)
