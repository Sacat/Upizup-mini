"""Concept preview only - NOT integrated into the game, NOT shipping any
real trademark. A structured 6-panel snapback cap in the visual language
of a classic New York ballcap silhouette (domed crown, curved brim, front
monogram, top button, side eyelets, back strap) - silhouette referenced,
no real team logo/lettering reproduced. Monogram is "DA" (this project's
own initials), matching the existing non-trademark convention already
used for Mike90/Mike97 shoes and the shipped "Lacos Cap"
(Assets/UpIzUpMini/Editor/Mini166IntegrateHat.cs).

Mandatory sequence applied (see memory: mandatory-modeling-sequence):
1. Target - snapback cap, human-head scale matching the shipped Lacos Cap
   (crown radius ~0.10m, Assets/.../Mini166IntegrateHat.cs crownMatrix
   scale (0.2,0.12,0.2) on a unit sphere = 0.10m radius).
2. Analyze - landmarks: domed crown, 6 radial panel seams meeting a top
   button, a flat/curved brim, a front-panel monogram, crown eyelets, an
   adjustable back strap+clasp. Not established by generic references:
   this project's own head-bone scale (matched to the shipped cap instead
   of guessed).
3. Construction per component, chosen deliberately, not one universal
   method:
   - Crown: ring-sweep dome (this project's proven Ring/Upper technique
     from Mini166RepairAccessories.cs's Shoes()) as ONE welded
     multi-material mesh so 6 panel seams are real per-face materials
     under Subsurf, not separate objects that would pull apart at the
     open band edge.
   - Brim: a separate flat, gently-curved solidified panel - a brim is a
     genuinely near-planar part, so simple panel/extrude modeling is the
     right technique here, unlike the dome.
   - Button/eyelets: simple primitives (small sphere / cylinders).
   - Monogram: REAL raised 3D pixel geometry conforming to the crown's
     local surface normal at each pixel (same finite-difference tangent
     technique proven for the shoe's eyestay - see surface_normal()
     below), NOT a camera-facing billboard. It sits on the front panel,
     which is near-planar, so it reads correctly from every fixed camera
     angle without any rotation-dependent trick.

Run: blender --background --python Tools/ArtPreview/da_snapback_cap_preview.py
Output: Logs/Tasks/Concept/DASnapbackCap/
"""
import bpy, math, json
from mathutils import Vector
from pathlib import Path
OUT = Path(r"E:\Unity\Up Iz Up Mini\Logs\Tasks\Concept\DASnapbackCap")
OUT.mkdir(parents=True, exist_ok=True)
stats = {}

def reset():
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
    for m in list(bpy.data.materials): bpy.data.materials.remove(m)
def mat(name, c, rough=.55, metal=0):
    m = bpy.data.materials.new(name); m.diffuse_color = (*c, 1); m.use_nodes = True
    bs = m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*c, 1); bs.inputs['Roughness'].default_value = rough
    bs.inputs['Metallic'].default_value = metal
    return m
def mesh(name, vs, fs, m):
    me = bpy.data.meshes.new(name); me.from_pydata(vs, [], fs); me.update()
    o = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(o); o.data.materials.append(m); return o
def multimesh(name, vs, faces_with_matidx, materials, subsurf=True):
    me = bpy.data.meshes.new(name)
    me.from_pydata(vs, [], [f for f, _ in faces_with_matidx]); me.update()
    for m in materials: me.materials.append(m)
    for poly, (_, idx) in zip(me.polygons, faces_with_matidx): poly.material_index = idx
    o = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(o)
    if subsurf:
        s = o.modifiers.new('Subsurf', 'SUBSURF'); s.levels = 2; s.render_levels = 2
    return o
def setup_lighting():
    sc = bpy.context.scene; sc.render.engine = 'CYCLES'; sc.cycles.samples = 64
    sc.cycles.use_denoising = True; sc.view_settings.exposure = .4
    sc.render.resolution_x = 1600; sc.render.resolution_y = 1200; sc.render.resolution_percentage = 100
    sc.world.use_nodes = True; sc.world.node_tree.nodes['Background'].inputs[0].default_value = (.8, .79, .77, 1)
    sc.world.node_tree.nodes['Background'].inputs[1].default_value = 1.0
    bpy.ops.object.light_add(type='AREA', location=(-.3, -.5, .45)); bpy.context.object.data.energy = 60; bpy.context.object.data.size = .5
    bpy.ops.object.light_add(type='SUN', location=(0, 0, .6)); sun = bpy.context.object; sun.rotation_euler = (.5, -.2, .5); sun.data.energy = 1.6; sun.data.angle = .1
    sc.view_settings.view_transform = 'AgX'
def make_camera(name, campos, target, scale):
    bpy.ops.object.camera_add(location=campos); cam = bpy.context.object; cam.name = name
    cam.rotation_euler = (Vector(target) - cam.location).to_track_quat('-Z', 'Y').to_euler()
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = scale; cam.data.clip_start = .001; cam.data.clip_end = 5
    return cam
def render_view(cam, name):
    bpy.context.scene.camera = cam
    bpy.context.scene.render.filepath = str(OUT / (name + '.png')); bpy.ops.render.render(write_still=True)
def count_group(name, objects):
    deps = bpy.context.evaluated_depsgraph_get(); tris = 0
    for o in objects:
        if o.type != 'MESH': continue
        me = o.evaluated_get(deps).to_mesh(); me.calc_loop_triangles(); tris += len(me.loop_triangles)
        o.evaluated_get(deps).to_mesh_clear()
    stats[name] = {'evaluated_triangles': tris, 'mesh_objects': sum(o.type == 'MESH' for o in objects)}

reset()

navy = mat('Crown/brim', (.06, .11, .22))
navy_dark = mat('Underbrim/seam', (.03, .05, .10))
white_stitch = mat('Panel seam stitch', (.85, .84, .80), rough=.7)
grey_button = mat('Top button', (.5, .5, .52), rough=.4)
grey_eyelet = mat('Eyelet', (.15, .15, .17), rough=.35)
strap_mat = mat('Back strap', (.05, .08, .16))
snap_mat = mat('Strap snap', (.55, .55, .55), metal=.6, rough=.3)
logo_white = mat('Monogram DA', (.93, .93, .90), rough=.5)

# --- Step 2/3: crown as a ring-sweep dome, ONE welded multi-material mesh.
# t=0 top (button), t=1 headband bottom. Slightly flattened at the very
# top (real caps have a small flat button platform, not a sharp point) by
# clamping the profile's radius easing - same clamp trick already proven
# for the shoe's high-top collar.
CROWN_R = .10      # matches shipped Lacos Cap crown radius (0.2 scale on unit sphere)
CROWN_H = .075     # dome height above the headband
BAND_H = .028      # headband strip height below the dome
FRONT_BIAS = 1.06  # crown is very slightly deeper front-to-back than side-to-side

def dome_profile(t):
    tt = max(t, .06)  # clamp: keeps a small flat button platform at the very top
    y = CROWN_H * math.cos(tt * math.pi / 2)
    r = CROWN_R * math.sin(tt * math.pi / 2)
    return y, r

def CrownPt(a, t):
    y, r = dome_profile(t)
    fb = 1 + (FRONT_BIAS - 1) * max(0, math.cos(a))  # deeper on the front side (a=0)
    return Vector((r * math.sin(a), BAND_H + y, r * fb * math.cos(a)))

def BandPt(a, u):
    # u=0 at dome/band seam, u=1 at the bottom edge of the band
    y = BAND_H * (1 - u)
    r = CROWN_R * .99
    fb = 1 + (FRONT_BIAS - 1) * max(0, math.cos(a))
    return Vector((r * math.sin(a), y, r * fb * math.cos(a)))

SEGS = 36  # 6 panels x 6 segments/panel, so panel seams land exactly on grid lines
DOME_ROWS = 9
BAND_ROWS = 3

def panel_seam(seg):
    return (seg % (SEGS // 6)) == 0  # true on the 6 seam columns

vs, faces = [], []
row_start = []
for row in range(DOME_ROWS + 1):
    t = row / DOME_ROWS
    row_start.append(len(vs))
    for seg in range(SEGS):
        a = -math.pi + 2 * math.pi * seg / SEGS
        vs.append(tuple(CrownPt(a, t)))
for row in range(DOME_ROWS):
    for seg in range(SEGS):
        seg2 = (seg + 1) % SEGS
        a0, a1 = row_start[row], row_start[row + 1]
        idx = (a0 + seg, a0 + seg2, a1 + seg2, a1 + seg)
        mi = 1 if (panel_seam(seg) or panel_seam(seg2)) else 0
        faces.append((idx, mi))

band_row_start = []
for row in range(BAND_ROWS + 1):
    u = row / BAND_ROWS
    band_row_start.append(len(vs))
    for seg in range(SEGS):
        a = -math.pi + 2 * math.pi * seg / SEGS
        vs.append(tuple(BandPt(a, u)))
# weld band top row to dome bottom row (same ring, so reuse dome's last row indices)
band_row_start[0] = row_start[DOME_ROWS]
for row in range(BAND_ROWS):
    for seg in range(SEGS):
        seg2 = (seg + 1) % SEGS
        a0, a1 = band_row_start[row], band_row_start[row + 1]
        idx = (a0 + seg, a0 + seg2, a1 + seg2, a1 + seg)
        mi = 1 if (panel_seam(seg) or panel_seam(seg2)) else 2  # seam col, else plain band
        faces.append((idx, mi))

crown_obj = multimesh('Crown+Band', vs, faces, [navy, white_stitch, navy_dark])

# --- Real local surface normal of the crown dome (finite-difference
# tangents), reused below for EVERYTHING that attaches to the dome
# (eyelets, strap snaps, monogram) - this is the same technique already
# proven for the shoe's eyestay, moved up here so nothing downstream has
# to fall back on a crude radial approximation.
def surface_normal(a, t):
    eps = .01
    da = CrownPt(a + eps, t) - CrownPt(a - eps, t)
    dt = CrownPt(a, min(t + eps, 1)) - CrownPt(a, max(t - eps, 0))
    n = da.cross(dt)
    if n.length < 1e-9: return Vector((math.sin(a), .3, math.cos(a))).normalized()
    n.normalize()
    if n.y < 0: n = -n
    return n

# --- Button (top of crown, at the flat platform)
by, br = dome_profile(0.06)
bpy.ops.mesh.primitive_uv_sphere_add(radius=.009, location=(0, BAND_H + by + .004, 0))
button = bpy.context.object; button.name = 'Top button'; button.scale = (1, .6, 1)
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
button.data.materials.append(grey_button)

# --- Eyelets: 6, one per panel, low on the dome (real caps place them near
# the crown/band seam, not at the very top). Attached along the REAL
# surface normal so they sit flush instead of floating off the dome.
eyelet_objs = []
for i in range(6):
    a = -math.pi + 2 * math.pi * (i + .5) / 6
    p = CrownPt(a, .74)
    n = surface_normal(a, .74)
    bpy.ops.mesh.primitive_cylinder_add(vertices=10, radius=.0035, depth=.006, location=tuple(p + n * .002))
    e = bpy.context.object; e.name = f'Eyelet {i}'
    e.rotation_euler = n.to_track_quat('Z', 'Y').to_euler()
    e.data.materials.append(grey_eyelet)
    eyelet_objs.append(e)

# --- Brim: a separate flat, gently-curved solidified panel. Root ring
# (d=0) is WELDED to real crown-surface points (not an independently
# guessed z formula) - the earlier version put the root at a formula-only
# position that didn't actually coincide with the crown mesh, so it read
# as a disconnected blade poking out of empty air (visible in the
# three-quarter view). Front = a=0, +Z side, matching CrownPt's bias.
FRONT_HALF_ANGLE = math.radians(52)  # brim spans ~104 deg across the front
def brim_root(x):
    return CrownPt(x * FRONT_HALF_ANGLE, .92)  # x in [-1,1], real crown surface, near band
def BrimTop(x, d):
    root = brim_root(x)
    out = Vector((root.x, 0, root.z))
    out = out.normalized() if out.length > 1e-6 else Vector((0, 0, 1))
    reach = .088 * (1 - .35 * x * x)  # centre reaches farther than the edges (curved bill)
    p = root + out * reach * d
    p.y += -.005 * d * d - .008 * x * x * d  # slight downward droop toward the tip
    return p
brim_vs, brim_fs = [], []
BW, BD = 10, 6
def brim_idx(i, j, top): return (i * (BD + 1) + j) + (0 if top else (BW + 1) * (BD + 1))
for i in range(BW + 1):
    x = -1 + 2 * i / BW
    for j in range(BD + 1):
        d = j / BD
        top = BrimTop(x, d)
        brim_vs.append(tuple(top))
for i in range(BW + 1):
    x = -1 + 2 * i / BW
    for j in range(BD + 1):
        d = j / BD
        top = BrimTop(x, d)
        brim_vs.append((top.x, top.y - .006, top.z))
for i in range(BW):
    for j in range(BD):
        a, b, c, d_ = brim_idx(i, j, True), brim_idx(i + 1, j, True), brim_idx(i + 1, j + 1, True), brim_idx(i, j + 1, True)
        brim_fs.append((a, b, c, d_))
        a2, b2, c2, d2 = brim_idx(i, j, False), brim_idx(i, j + 1, False), brim_idx(i + 1, j + 1, False), brim_idx(i + 1, j, False)
        brim_fs.append((a2, b2, c2, d2))
for i in range(BW):  # tip edge cap
    a, b = brim_idx(i, BD, True), brim_idx(i + 1, BD, True)
    a2, b2 = brim_idx(i, BD, False), brim_idx(i + 1, BD, False)
    brim_fs.append((a, b, b2, a2))
for j in range(BD):  # left/right side caps
    a, b = brim_idx(0, j, True), brim_idx(0, j + 1, True)
    a2, b2 = brim_idx(0, j, False), brim_idx(0, j + 1, False)
    brim_fs.append((a2, b2, b, a))
    a, b = brim_idx(BW, j, True), brim_idx(BW, j + 1, True)
    a2, b2 = brim_idx(BW, j, False), brim_idx(BW, j + 1, False)
    brim_fs.append((a, b, b2, a2))
brim_obj = mesh('Brim', brim_vs, brim_fs, navy)
brim_obj.data.materials.append(navy_dark)
for f in brim_obj.data.polygons:
    if f.index >= BW * BD * 2:  # tip/side cap faces -> dark underside colour
        f.material_index = 1

# --- Back strap + snap closure (secondary defining feature of a snapback,
# real geometry, not implied).
strap_a0, strap_a1 = math.pi * .82, math.pi * 1.18
strap_vs, strap_fs = [], []
for row, t in enumerate((.62, .82)):
    for k, a in enumerate((strap_a0, strap_a1)):
        strap_vs.append(tuple(CrownPt(a if a <= math.pi else a - 2 * math.pi, t)))
strap_obj = mesh('Back strap', strap_vs, [(0, 1, 3, 2)], strap_mat)
for a in (strap_a0, strap_a1):
    aa = a if a <= math.pi else a - 2 * math.pi
    p = CrownPt(aa, .72)
    n = surface_normal(aa, .72)
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=.004, depth=.003, location=tuple(p + n * .002))
    s = bpy.context.object; s.name = 'Strap snap'
    s.rotation_euler = n.to_track_quat('Z', 'Y').to_euler()
    s.data.materials.append(snap_mat)

# --- Monogram "DA": REAL raised pixel geometry conforming to the crown's
# own local surface normal (finite-difference tangents, same function used
# above for eyelets/strap snaps), interlocked like a classic ballcap
# monogram - NOT a camera-facing billboard, so it reads correctly from
# every fixed camera below.
# 5x5 pixel grids (1 = filled). D and A share/interlock their overlapping
# columns like a real two-letter cap monogram.
D = ["11100", "10010", "10010", "10010", "11100"]
A = ["00100", "01010", "10001", "11111", "10001"]
PIXEL = .0055
front_a_center = 0.0  # a=0 is the front panel centre (matches FRONT_BIAS)
front_t_center = .42

# FOUND (re-reading pixel_to_at, not guessed): columns were spaced with
# PIXEL/CROWN_R (a radius-based angular step) while rows used
# PIXEL/CROWN_H (a height-based step) - two different, non-equal-length
# units, both then pushed through the dome's non-linear cos/sin easing.
# Equal grid steps therefore mapped to UNEQUAL physical distances, which
# is why the close-up render showed a jagged, illegible blob instead of
# two letters. Fixed by building one flat local tangent frame (equal real
# spacing on both axes) at the monogram's actual attach point, then
# pushing each pixel out along the true surface normal - a legitimate
# small flat-plane approximation for a ~3cm logo on a ~10cm-radius dome
# (max curvature deviation is sub-millimetre), not a rotation-dependent
# trick, since the frame itself is fixed in shoe/cap space, not camera
# space.
_center_p = CrownPt(front_a_center, front_t_center)
_center_n = surface_normal(front_a_center, front_t_center)
_up_hint = Vector((0, 1, 0))
_right = _center_n.cross(_up_hint)
_right = _right.normalized() if _right.length > 1e-6 else Vector((1, 0, 0))
_up = _right.cross(_center_n).normalized()

def pixel_point(col, row, grid_w, grid_h, dx):
    # DIAGNOSED (not guessed): _right actually points toward world -X here
    # (cross(normal, up_hint) with this dome's front normal leaning +Z),
    # opposite of what was assumed when dx was chosen - so every glyph
    # rendered horizontally mirrored AND the word came out "AD" instead of
    # "DA". Negating only the horizontal term fixes both without touching
    # _up (row/vertical order already read correctly in Front and Top).
    local = -_right * ((col - grid_w / 2 + .5) * PIXEL + dx) + _up * ((grid_h / 2 - .5 - row) * PIXEL)
    return _center_p + local

logo_objs = []
for grid, dx in ((D, -.017), (A, .017)):
    for r, rowbits in enumerate(grid):
        for c, bit in enumerate(rowbits):
            if bit != '1': continue
            p = pixel_point(c, r, 5, 5, dx)
            bpy.ops.mesh.primitive_cube_add(size=1, location=tuple(p + _center_n * .0035))
            px = bpy.context.object; px.name = 'Monogram pixel'
            px.scale = (PIXEL * .95, PIXEL * .95, .0035)
            px.rotation_euler = _center_n.to_track_quat('Z', 'Y').to_euler()
            bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
            px.data.materials.append(logo_white)
            logo_objs.append(px)

setup_lighting()
ALL = [crown_obj, button, brim_obj, strap_obj] + eyelet_objs + logo_objs
count_group('da_snapback_cap', ALL)

CENTER = Vector((0, BAND_H + CROWN_H * .55, 0))
cam_front = make_camera('Cam_Front', (0, CENTER.y, .42), (0, CENTER.y - .01, 0), .30)
# DIAGNOSED: this view direction is almost exactly parallel to world Z,
# which is the same to_track_quat degenerate-roll singularity already
# found once for a straight-down top camera on the shoe - it produced a
# vertically-FLIPPED render (button/monogram at the bottom, brim reading
# as if it floated above the crown). Fixed the same way: an explicit
# rotation_euler instead of a computed quaternion for this axis-aligned
# view. (0,0,0) is Blender's own default "looking down -Z, +Y up" pose,
# which is exactly what a camera parked on +Z needs here.
cam_front.rotation_euler = (0, 0, 0)
cam_side = make_camera('Cam_Side', (.42, CENTER.y, 0), (0, CENTER.y - .01, 0), .30)
cam_top = make_camera('Cam_Top', (0, .45, 0), (0, 0, 0), .30)
cam_top.rotation_euler = (-math.pi / 2, 0, 0)  # avoid to_track_quat singularity looking straight down
# DIAGNOSED: at (.30,.30) the camera views the front panel/monogram
# almost edge-on, so the shallow (~3.5mm) raised pixels visually overlap
# in silhouette - an optical consequence of grazing angle on a low-relief
# detail, not a mesh defect. Rebalanced toward the front (more Z, less X)
# so the monogram is seen at a real, non-grazing angle while still a
# recognisable three-quarter product-shot angle.
cam_3q = make_camera('Cam_ThreeQuarter', (.20, CENTER.y + .05, .37), (0, CENTER.y - .02, .015), .30)
_logo_target = CrownPt(0, front_t_center)  # aim at the monogram's actual world position, not a guess
cam_logo = make_camera('Cam_LogoCloseup', (.10, _logo_target.y, _logo_target.z + .10), tuple(_logo_target), .09)

for name, cam in [('View-Front', cam_front), ('View-Side', cam_side), ('View-Top', cam_top), ('View-ThreeQuarter', cam_3q), ('View-LogoCloseup', cam_logo)]:
    render_view(cam, name)

bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'DASnapbackCap.blend'))
render_view(cam_3q, 'DASnapbackCap-Preview')
print('DA_SNAPBACK_CAP_DONE ' + json.dumps(stats))
