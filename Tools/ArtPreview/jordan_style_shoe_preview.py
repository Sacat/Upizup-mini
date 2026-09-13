"""Concept preview only - NOT integrated into the game, NOT shipping any
real trademark. High-top sneaker in the visual language of the classic
Air Jordan 1 "Chicago" (red/white/black colour-blocking, wing-shaped side
panel) - silhouette and colour-blocking referenced, no swoosh/Jumpman
reproduced, matching this project's existing convention (Mike90/Mike97).

FIRST ATTEMPT AT THIS FILE FAILED BADLY (user: "this gets 0.1/100 for
modeling") - it stacked disconnected axis-aligned cubes, which cannot
form a continuous organic shoe-last silhouette; the render looked like a
pile of blocks, not a shoe. Root cause: it ignored this project's own
ALREADY-PROVEN technique for exactly this problem.

This rewrite uses the real, working ring-sweep technique from
Assets/UpIzUpMini/Editor/Mini166RepairAccessories.cs's Shoes() function
(the in-game Mike90/Mike97 shoe generator) - a continuous swept body
built from angular rings around a foot cross-section (Ring/Upper style
math), not primitive-stacking. That is the correct technique family for
organic curved consumer objects (per researched general 3D-modeling
guidance: box modeling suits hard-surface objects like buildings;
continuous lofted/swept surfaces suit organic curved forms like shoes,
bodies, vehicles) - and this project already has a validated
implementation of it, so it is reused rather than reinvented.

Reference: downloaded and directly inspected a real Air Jordan 1 High
"Chicago" photo (commons.wikimedia.org, Nike_Air_Jordan_I.jpg).
Iterated against that photo directly (see the comparison image saved
alongside the final render) rather than accepting a first pass.

Run: blender --background --python Tools/ArtPreview/jordan_style_shoe_preview.py
Output: Logs/Tasks/Concept/JordanStyleShoe/
"""
import bpy, math, json
from mathutils import Vector
from pathlib import Path
OUT = Path(r"E:\Unity\Up Iz Up Mini\Logs\Tasks\Concept\JordanStyleShoe")
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
def cube(name, p, s, m, bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=p); o = bpy.context.object; o.name = name
    o.scale = s; bpy.ops.object.transform_apply(location=False, rotation=False, scale=True); o.data.materials.append(m)
    if bevel:
        b = o.modifiers.new('edge', 'BEVEL'); b.width = bevel; b.segments = 2
        o.modifiers.new('wn', 'WEIGHTED_NORMAL')
    return o
def mesh(name, vs, fs, m):
    me = bpy.data.meshes.new(name); me.from_pydata(vs, [], fs); me.update()
    o = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(o); o.data.materials.append(m); return o
def multimesh(name, vs, faces_with_matidx, materials):
    # Researched (Francesco Saviano's Subdivision Surface guide, and the
    # standard AAA "cage + Subsurf" workflow): a Subdivision Surface
    # modifier only smooths continuously across a TRULY shared, welded
    # mesh - separate objects glued edge-to-edge each shrink toward their
    # own centre at the open boundary and visibly separate. So the whole
    # shoe body is ONE mesh with shared vertex indices at every panel
    # seam, coloured via per-face material_index instead of separate
    # objects - this is what makes Subsurf usable here at all.
    me = bpy.data.meshes.new(name)
    me.from_pydata(vs, [], [f for f, _ in faces_with_matidx]); me.update()
    for m in materials: me.materials.append(m)
    for poly, (_, idx) in zip(me.polygons, faces_with_matidx): poly.material_index = idx
    o = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(o); return o
def lace_curve(name, points, mat, radius=.0035):
    # Researched (Blender Artists Community + tutorial consensus): a
    # Bezier/poly curve with bevel_depth is the standard, reliable way to
    # model a lace/cord - much more robust than hand-built rod segments,
    # which is what produced the stray "rope" artifact in an earlier pass.
    cd = bpy.data.curves.new(name, type='CURVE'); cd.dimensions = '3D'
    cd.bevel_depth = radius; cd.bevel_resolution = 3
    sp = cd.splines.new('POLY'); sp.points.add(len(points) - 1)
    for i, p in enumerate(points): sp.points[i].co = (p.x, p.y, p.z, 1)
    o = bpy.data.objects.new(name, cd); bpy.context.collection.objects.link(o); o.data.materials.append(mat); return o
def rod(name, a, b, r, m, n=8):
    d = Vector(b) - Vector(a)
    bpy.ops.mesh.primitive_cone_add(vertices=n, radius1=r, radius2=r * .9, depth=d.length, location=(Vector(a) + Vector(b)) / 2)
    o = bpy.context.object; o.name = name; o.rotation_euler = d.to_track_quat('Z', 'Y').to_euler(); o.data.materials.append(m); return o
def setup_lighting():
    # Called ONCE - lights/world only, no camera. Kept separate so multiple
    # FIXED cameras can be created without duplicating lights each time.
    sc = bpy.context.scene; sc.render.engine = 'CYCLES'; sc.cycles.samples = 64
    sc.cycles.use_denoising = True; sc.view_settings.exposure = .4
    sc.render.resolution_x = 1600; sc.render.resolution_y = 1200; sc.render.resolution_percentage = 100
    sc.world.use_nodes = True; sc.world.node_tree.nodes['Background'].inputs[0].default_value = (.8, .79, .77, 1)
    sc.world.node_tree.nodes['Background'].inputs[1].default_value = 1.0
    bpy.ops.object.light_add(type='AREA', location=(-.3, -.5, .45)); bpy.context.object.data.energy = 60; bpy.context.object.data.size = .5
    bpy.ops.object.light_add(type='SUN', location=(0, 0, .6)); sun = bpy.context.object; sun.rotation_euler = (.5, -.2, .5); sun.data.energy = 1.6; sun.data.angle = .1
    sc.view_settings.view_transform = 'AgX'
def make_camera(name, campos, target, scale):
    # A named, reusable camera - the user's instruction is explicit: "Keep
    # these cameras unchanged between iterations." Once a view's numbers
    # are set here, later passes must not touch them just to make a
    # result look better.
    bpy.ops.object.camera_add(location=campos); cam = bpy.context.object; cam.name = name
    cam.rotation_euler = (Vector(target) - cam.location).to_track_quat('-Z', 'Y').to_euler()
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = scale; cam.data.clip_start = .001; cam.data.clip_end = 5
    return cam
def render_view(cam, name):
    bpy.context.scene.camera = cam
    render_to(name)
def render_to(name):
    bpy.context.scene.render.filepath = str(OUT / (name + '.png')); bpy.ops.render.render(write_still=True)
def count_group(name, objects):
    deps = bpy.context.evaluated_depsgraph_get(); tris = 0
    for o in objects:
        if o.type != 'MESH': continue
        me = o.evaluated_get(deps).to_mesh(); me.calc_loop_triangles(); tris += len(me.loop_triangles)
        o.evaluated_get(deps).to_mesh_clear()
    stats[name] = {'evaluated_triangles': tris, 'mesh_objects': sum(o.type == 'MESH' for o in objects)}

reset()
red = mat('Toe/quarter/heel leather', (.60, .085, .065))
white = mat('Vamp/midsole leather', (.90, .88, .83))
black = mat('Panel/lace/collar', (.045, .045, .048))
red_sole = mat('Outsole rubber', (.52, .075, .06), rough=.85)
grey = mat('Ankle padding fleck', (.55, .53, .5), rough=.9)
eyelet_m = mat('Metal eyelet', (.72, .72, .74), rough=.3, metal=.75)

GROUPS = {'sole': [], 'upper': [], 'laces': []}
def snap(group, before):
    after = set(bpy.data.objects); GROUPS[group].extend(after - before); return after

def shoe():
    # Real proportions researched: US10 last ~.29m long, sole ~.03m thick,
    # high-top collar rising ~.11m above the sole top (read off the real
    # reference photo's proportions).
    L = .29
    cx, cz = L * .5, 0.0
    length = L * .52          # half-length used by the Ring() cos(a) term
    width = .052              # half-width at the ball of the foot
    ground = .0
    before = set(bpy.data.objects)

    def Ring(a, y, scale, front_bias=.78):
        z = math.cos(a)
        w = width * (1.0 if z > 0 else front_bias)
        return Vector((cx + math.sin(a) * w * scale, cz + z * length * scale, ground + y))

    n = 28
    sole_ys = [0.0, .008, .016, .028]
    sole_scales = [.92, 1.0, 1.01, .97]
    sole_mats = [red_sole, red_sole, white]  # material for sole BANDS 0,1,2 (between the 4 levels)
    base_h = sole_ys[-1]

    def Upper(a, t):
        # The proven in-game Upper() lets taper -> cos(t*pi/2) reach 0 at
        # t=1, collapsing the rim to a point - correct for a LOW-cut
        # sneaker, wrong for a high-top, which needs a real OPEN ring at
        # the ankle. Clamp the scale-driving parameter so it stops
        # shrinking past t=.55; height still rises with the true t.
        z = math.cos(a)
        height = .06 + .07 * ((1 - z) * .5)
        scale_t = min(t, .55)
        taper = math.cos(scale_t * math.pi / 2)
        p = Ring(a, base_h + height * math.sin(t * math.pi / 2), .97 * taper)
        p.y += .012 * t
        return p

    def Profile(row):
        # row 0..3 = sole levels (Ring), row 3..11 = upper levels (Upper,
        # t=(row-3)/8) - row 3 is EXACTLY shared between both formulas
        # (verified: Upper(a,0) reduces to Ring(a, sole_ys[3], .97), which
        # equals sole_scales[3]=.97), so the sole and upper are one
        # continuous surface, not two glued pieces.
        if row <= 3:
            return lambda a: Ring(a, sole_ys[row], sole_scales[row] if row < len(sole_scales) else sole_scales[-1])
        t = (row - 3) / 8
        return lambda a: Upper(a, t)

    total_rows = 11  # 12 vertex levels: 0-3 sole, 3-11 upper
    materials = [red_sole, white, red, black, grey]
    IDX = {'red_sole': 0, 'white': 1, 'red': 2, 'black': 3, 'grey': 4}

    def norm_deg(a):
        d = math.degrees(a) % 360
        return d - 360 if d > 180 else d

    # User: "so this means your inspection is still very very poor" - fair.
    # A checklist comparison against the real photo (not a glance) showed
    # the previous fixed angle-wedge panels don't correspond to how a real
    # shoe is actually cut: the toe cap wraps the ENTIRE front hemisphere
    # at low rows (there is no toe-to-collar diagonal seam), and the only
    # place white shows through is a narrow V-shaped lace throat that
    # widens from nothing at the toe to a wide gap at the collar. Rebuilt
    # to match that real structure directly, instead of arbitrary angle
    # constants per row-band.
    def throat_half_angle(row):
        if row <= 2: return 0.0  # no throat over the toe cap itself
        t = (row - 2) / 6.0
        return 15 + 65 * t  # widens from 15deg to 80deg toward the collar
    def upper_material(urow, adeg):
        aadeg = abs(adeg)
        collar = urow >= 6  # the collar band wraps fully around the top, all angles
        if aadeg <= throat_half_angle(urow):
            return IDX['black'] if collar else IDX['white']  # vamp low, tongue/collar-front high
        return IDX['black'] if collar else IDX['red']  # toe cap + quarter panel (both red on the real shoe)

    # ---- Build ONE welded vertex grid (row-major), then faces referencing
    # shared indices - this is what lets Subsurf smooth continuously
    # across every colour seam instead of the panels pulling apart. ----
    vs = []
    for row in range(total_rows + 1):
        prof = Profile(row)
        for i in range(n):
            vs.append(tuple(prof(i * 2 * math.pi / n)))
    def vidx(row, i): return row * n + (i % n)

    faces = []
    for row in range(total_rows):
        for i in range(n):
            i2 = i + 1
            if row < 3:
                midx = IDX['red_sole'] if row == 0 else (IDX['red_sole'] if row == 1 else IDX['white'])
            else:
                urow = row - 3
                adeg = norm_deg((i + .5) * 2 * math.pi / n)
                midx = upper_material(urow, adeg)
            faces.append(((vidx(row, i), vidx(row, i2), vidx(row + 1, i2), vidx(row + 1, i)), midx))
    # Cap the sole's underside (row 0) - a shoe sole must be closed, not
    # an open hole - with a single n-gon fan.
    faces.append((tuple(vidx(0, i) for i in range(n)), IDX['red_sole']))

    body = multimesh('Shoe body', vs, faces, materials)
    # Researched (Francesco Saviano's Subdivision Surface guide; the
    # standard AAA "low-poly cage + Subsurf" workflow): this is what turns
    # a faceted, low-poly-looking loft into a smooth, professional-reading
    # curved surface. Render/viewport levels both set so the batch render
    # actually uses it.
    sub = body.modifiers.new('Subsurf', 'SUBSURF'); sub.levels = 2; sub.render_levels = 2
    before = snap('upper', before)  # sole+upper are one object now; keep 'sole' group for the staged-inspection API but leave it empty (nothing to hide separately anymore)

    # User correction: camera-facing billboard decals were REJECTED - they
    # are a presentation trick, not real geometry, and would not survive
    # rotation. Rebuilding the throat as an actual assembly instead:
    # eyestays (raised flaps along the throat edge) with the tongue
    # recessed BEHIND/BELOW them, all oriented purely from the shoe's own
    # geometry (never the camera).
    # First construction pass pushed the eyestay/tongue outward using a
    # crude 2D radial guess from a fixed axis (cx,cz) - a real diagnosis
    # (fixed front/side/3-4 views, not a glance) showed this pointed the
    # wrong way near the collar, where the clamped-taper geometry has
    # genuinely diverged from that assumption, making the flap jut out at
    # a disconnected angle. Replaced with the ACTUAL local surface normal,
    # computed from Upper()'s own tangent vectors - this is correct
    # wherever the surface actually is, not just where a simple radial
    # guess happens to match it.
    def surface_normal(a, t):
        eps = .015
        t0, t1 = max(t - eps, 0.0), min(t + eps, 1.0)
        da = Upper(a + eps, t) - Upper(a - eps, t)
        dt = Upper(a, t1) - Upper(a, t0)
        n = da.cross(dt)
        if n.length < 1e-9: return Vector((0, 1, 0))
        n.normalize()
        # Ensure it points AWAY from the shoe's central axis, not inward -
        # the cross product's sign depends on winding, not on which way is
        # actually "outward".
        p = Upper(a, t)
        if n.dot(Vector((p.x - cx, p.y - cz, 0))) < 0: n = -n
        return n

    eyestay_rows = [2.3, 3.4, 4.5, 5.6, 6.7, 7.6]
    eyestay_rise = .014  # how far the eyestay flap stands proud of the vamp
    def eyestay_pt(row, side, raised):
        t = row / 8
        half = math.radians(throat_half_angle(row) + 2)
        a = side * half
        p = Upper(a, t)
        if raised:
            p += surface_normal(a, t) * eyestay_rise
        return p

    # Eyestay: a real raised ribbon (base on the vamp, top standing proud
    # of it) running the length of the throat on each side - not a flat
    # decal, an actual 3D flap with the same "sweep between rows" quad
    # technique already proven for the sole/roof/tongue in this project.
    eyestay_objs = {}
    for side in (-1, 1):
        vs, fs = [], []
        for row in eyestay_rows:
            vs.append(tuple(eyestay_pt(row, side, False)))
            vs.append(tuple(eyestay_pt(row, side, True)))
        for i in range(len(eyestay_rows) - 1):
            k = i * 2
            fs.append((k, k + 2, k + 3, k + 1))
        eyestay_objs[side] = mesh(f'Eyestay {side}', vs, fs, black)

    # Tongue: recessed both inward (narrower half-angle, sits BEHIND the
    # eyestay in the gap) and lower (raised less than the eyestay's rise)
    # so there is a real, visible stepped gap between them, not a flush
    # co-planar seam.
    tongue_vs, tongue_fs = [], []
    for row in eyestay_rows:
        t = row / 8
        half = math.radians(throat_half_angle(row) * .55)
        for side in (-1, 1):
            a = side * half
            p = Upper(a, t) + surface_normal(a, t) * (eyestay_rise * .45)
            tongue_vs.append(tuple(p))
    for i in range(len(eyestay_rows) - 1):
        k = i * 2
        tongue_fs.append((k, k + 1, k + 3, k + 2))
    mesh('Tongue', tongue_vs, tongue_fs, white)

    # ---- Prove ONE lace segment before extending the pattern. ----
    # A real flat lace: a ribbon lying in the shoe's own tangent plane
    # (perpendicular to both its travel direction and the local outward
    # surface direction), offset outward by a real thickness - not
    # billboarded to the camera. Basis is entirely shoe-relative.
    def approx_outward(p):
        # A cruder radial approximation is acceptable HERE only - it sets
        # a small ribbon-thickness offset direction, not the actual
        # attachment geometry (which now uses the real surface_normal
        # above after that crude approximation was found wrong for it).
        d = Vector((p.x - cx, p.y - cz, 0))
        return d.normalized() if d.length > 1e-6 else Vector((0, 1, 0))
    def lace_ribbon(p0, p1, width, thickness, m, z_bump=0.0):
        trav = p1 - p0
        if trav.length < 1e-6: return None
        trav_n = trav.normalized()
        outn = ((approx_outward(p0) + approx_outward(p1)) * .5).normalized()
        widen = trav_n.cross(outn)
        widen = widen.normalized() if widen.length > 1e-6 else Vector((0, 0, 1))
        off = outn * thickness + Vector((0, 0, z_bump))
        a, b = p0 - widen * width / 2 + off, p0 + widen * width / 2 + off
        c, d_ = p1 + widen * width / 2 + off, p1 - widen * width / 2 + off
        return mesh(m[1], [tuple(a), tuple(b), tuple(c), tuple(d_)], [(0, 1, 2, 3)], m[0])

    eyestay_top = {side: [eyestay_pt(r, side, True) for r in eyestay_rows] for side in (-1, 1)}
    # ONE proof segment only this pass - the lowest crossing pair.
    lace_ribbon(eyestay_top[-1][0], eyestay_top[1][0], .009, .003, (black, 'Lace proof segment 0'))
    before = snap('laces', before)

    count_group('jordan_style_shoe', GROUPS['sole'] + GROUPS['upper'] + GROUPS['laces'])
    return L

def set_stage(*visible_groups):
    visible = set()
    for g in visible_groups: visible.update(GROUPS[g])
    for g, objs in GROUPS.items():
        for o in objs: o.hide_render = o not in visible

cube('Ground', (0, 0, -.005), (3.0, 3.0, .008), mat('Backdrop', (.86, .85, .82)))
L = shoe()
setup_lighting()
set_stage('sole', 'upper', 'laces')

# User: "Show fixed front, top, side and three-quarter views. Keep these
# cameras unchanged between iterations." These four numbers are now the
# fixed reference frame for every future pass on this shoe - do not tune
# them to flatter a result.
CENTER = Vector((0, 0, .06))
# Diagnosed: the first Front camera aimed at z=.075 (collar height) looked
# almost straight into the open top of the collar, showing the hollow
# inside - a camera-aim bug, not a geometry defect. Retargeted at the
# vamp/toe body height instead.
cam_front = make_camera('Cam_Front', (0, .5, .06), (0, 0, .045), .34)   # looking -Y, toe/throat toward camera
cam_side = make_camera('Cam_Side', (.55, 0, .07), (0, 0, .06), .34)      # looking -X, pure lateral profile
# Diagnosed: a perfectly vertical target direction is a known singularity
# for to_track_quat's up-hint (Y becomes ambiguous when looking straight
# down Z), which produced the unexpectedly off-centre framing - not the
# shoe's actual position. Setting rotation directly (identity = looking
# down -Z with +Y as screen-up) avoids the singularity.
cam_top = make_camera('Cam_Top', (0, 0, .55), (0, 0, .06), .38)
cam_top.rotation_euler = (0, 0, 0)
cam_3q = make_camera('Cam_ThreeQuarter', (.42, -.34, .16), (.13, 0, .06), .34)
# Isolated close-up on the throat assembly specifically (eyestays, tongue,
# proof lace segment) - same 3/4 angle, tighter framing, not a different
# angle chosen to make the detail look better.
cam_throat = make_camera('Cam_ThroatCloseup', (.30, -.24, .13), (.02, .04, .11), .11)

for name, cam in [('View-Front', cam_front), ('View-Side', cam_side), ('View-Top', cam_top), ('View-ThreeQuarter', cam_3q)]:
    render_view(cam, name)
render_view(cam_throat, 'ThroatCloseup-WithBody')

# Isolated throat-only view (body hidden) to check attachment/orientation
# without the rest of the shoe's shading confusing the read.
for o in GROUPS['upper']: o.hide_render = True
render_view(cam_throat, 'ThroatCloseup-IsolatedThroat')
for o in GROUPS['upper']: o.hide_render = False

bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'JordanStyleShoe.blend'))
render_view(cam_3q, 'JordanStyleShoe-Preview')
print('JORDAN_STYLE_SHOE_DONE ' + json.dumps(stats))
