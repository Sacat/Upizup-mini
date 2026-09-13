"""Concept preview only - NOT integrated into the game, NOT shipping any
real trademark. Low-top running shoe in the visual language of the
classic Nike Air Max 90 - chunky two-tier sole with a visible heel wedge,
a lens-shaped encapsulated "air unit" window in the heel, a mesh toe with
a leather overlay cap, and a large curved midfoot side panel - silhouette
and construction referenced, NO swoosh, NO "AIR MAX" wordmark, NO Nike
branding reproduced. Matches this project's existing non-trademark
homage convention (Mike90/Mike97, the Jordan-style concept, the DA cap).

Reference research (per upizup-blender-modeling's "research real
reference before modeling anything"):
- WebSearch facts: visible heel Max Air unit, foam midsole, TPU overlay
  panels, waffle rubber outsole, textile mesh upper with leather/synthetic
  overlays, padded low-top collar (Foot Locker/Nike/RunRepeat listings).
- Actually inspected a real photo: downloaded and Read
  commons.wikimedia.org/Nike_Air_Max_90.jpg (a worn black pair, side
  profile) - confirmed landmarks used below: the midsole visibly steps up
  in height from toe to heel (a real heel-to-toe wedge, not a flat slab),
  a lens-shaped Air window sits on the lateral heel wall, the toe overlay
  wraps the mesh forefoot with a rounded edge, and a large curved overlay
  panel spans the midfoot above the wing/eyelet row.

Construction, reusing this project's own PROVEN techniques rather than
inventing new ones (per upizup-blender-modeling rule 4):
- Body: the same ring-sweep/welded-multi-material/Subsurf technique from
  Mini166RepairAccessories.cs's Shoes() and jordan_style_shoe_preview.py -
  proven correct for organic curved footwear.
- Eyestay/tongue/one-proof-lace-segment: the same real (non-billboard)
  surface_normal-attached construction validated on the Jordan-style
  concept after the billboard-lace rejection.
- Heel air window + side wing panel: the same flat-local-tangent-frame
  technique validated on the DA snapback cap's monogram (equal physical
  spacing in a plane built from the real surface normal, then projected
  onto the surface) - here used for a recessed lens and a large overlay
  panel instead of pixel letters.
- Fixed cameras, camera-Z-singularity check applied up front (see
  upizup-blender-modeling hard-lessons #9) before trusting any axis-
  aligned view.

Run: blender --background --python Tools/ArtPreview/air_max_style_shoe_preview.py
Output: Logs/Tasks/Concept/AirMaxStyleShoe/
"""
import bpy, math, json
from mathutils import Vector
from pathlib import Path
OUT = Path(r"E:\Unity\Up Iz Up Mini\Logs\Tasks\Concept\AirMaxStyleShoe")
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
def cube(name, p, s, m):
    bpy.ops.mesh.primitive_cube_add(size=1, location=p); o = bpy.context.object; o.name = name
    o.scale = s; bpy.ops.object.transform_apply(location=False, rotation=False, scale=True); o.data.materials.append(m)
    return o
def mesh(name, vs, fs, m):
    me = bpy.data.meshes.new(name); me.from_pydata(vs, [], fs); me.update()
    o = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(o); o.data.materials.append(m); return o
def multimesh(name, vs, faces_with_matidx, materials):
    me = bpy.data.meshes.new(name)
    me.from_pydata(vs, [], [f for f, _ in faces_with_matidx]); me.update()
    for m in materials: me.materials.append(m)
    for poly, (_, idx) in zip(me.polygons, faces_with_matidx): poly.material_index = idx
    o = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(o); return o
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
mesh_grey = mat('Mesh vamp', (.62, .62, .64), rough=.8)
overlay_navy = mat('Toe/heel overlay', (.09, .13, .24))
midsole_white = mat('Foam midsole', (.92, .91, .87), rough=.6)
outsole_black = mat('Waffle outsole', (.05, .05, .055), rough=.85)
window_ice = mat('Air unit window', (.75, .85, .90), rough=.15)
panel_red = mat('Side wing panel', (.55, .10, .09))
black_trim = mat('Collar/lace trim', (.045, .045, .05))
eyelet_m = mat('Metal eyelet', (.72, .72, .74), rough=.3, metal=.75)
lace_black = mat('Laces', (.05, .05, .05), rough=.9)

GROUPS = {'sole': [], 'upper': [], 'laces': []}
def snap(group, before):
    after = set(bpy.data.objects); GROUPS[group].extend(after - before); return after

def shoe():
    # Step 1 target: US10-ish last, L=.29 (matches the Jordan concept's
    # already-validated scale). Step 2 landmarks from the real photo: an
    # outsole/midsole that visibly steps UP in height from toe to heel (a
    # real running-shoe heel-to-toe drop/wedge, not a flat slab), a
    # low-top collar (much shorter than the Jordan high-top), a lens Air
    # window on the heel, a toe overlay cap over mesh, a big curved
    # midfoot side panel.
    L = .29
    cx, cz = L * .5, 0.0
    length = L * .52
    width = .050
    ground = 0.0
    before = set(bpy.data.objects)

    LEN_FRONT = length * 1.12
    LEN_BACK = length * 0.80
    # The toe now reaches further than the heel (asymmetric, real-shoe
    # proportions - see Ring() below), so the shape's actual centre drifts
    # away from cz=0. Recentre HERE, once, so every fixed camera (tuned
    # against cz=0) still frames the whole shoe instead of going off-axis.
    cz = (LEN_BACK - LEN_FRONT) / 2

    def z_of(a): return math.cos(a)  # z=+1 toe, z=-1 heel (same convention as the Jordan concept)

    # DIAGNOSED against a real photo (commons.wikimedia.org,
    # Nike_Air_Max_90_Infrared - the canonical side-profile shot): the
    # first blockout pass was a flat diagonal wedge because Ring() had no
    # toe-spring curve - a real running shoe's sole visibly lifts off the
    # ground at the front tip, it isn't a straight ramp. Adding a cubic
    # lift concentrated near z=1 (the toe) reproduces that curve; it's in
    # Ring() itself so the sole AND upper both inherit it together, which
    # is physically correct (the whole toe box tips up, not just the sole).
    TOE_SPRING = .013
    def toe_lift(z): return TOE_SPRING * max(0.0, z) ** 3

    # REBUILT after the user correctly called this a boat hull: a plain
    # sin/cos ellipse pinches to a sharp POINT at both a=0 (toe) and a=pi
    # (heel) - that IS a canoe bow/stern, not a shoe. A real toe is a
    # gently rounded taper; a real heel is a blunt, rounded-rectangle end,
    # not a point. Replaced with a superellipse (Lame curve) whose
    # sharpness exponent differs front vs back: p=2 reproduces a plain
    # ellipse (the old, wrong behaviour); p>2 makes the curve reach full
    # width quickly and STAY there (a flat-sided, rounded-corner shape)
    # instead of tapering all the way to zero. A mild exponent at the toe
    # keeps a real taper; a much higher exponent at the heel keeps it
    # genuinely blunt. Length reach is also now asymmetric (the toe
    # genuinely reaches further from centre than the heel on a real last),
    # not just a minor width front_bias as before. (LEN_FRONT/LEN_BACK
    # themselves are defined above, before cz, so the recentring above can
    # use them too.)
    def Ring(a, y, scale, front_bias=.90):
        z = z_of(a); s = math.sin(a)
        if z >= 0:
            p, len_reach, wscale = 2.3, LEN_FRONT, 1.0
        else:
            p, len_reach, wscale = 5.5, LEN_BACK, .94
        x = math.copysign(abs(s) ** (2.0 / p), s) * width * wscale
        yv = math.copysign(abs(z) ** (2.0 / p), z) * len_reach
        return Vector((cx + x * scale, cz + yv * scale, ground + y + toe_lift(z)))

    OUT_H = .011          # outsole thickness (thin, roughly constant - real AM90 outsole is thin under a tall midsole)
    TOE_MID_H = .017      # midsole height at the toe
    HEEL_MID_H = .040     # midsole height at the heel - the visible heel wedge/bump
    def midsole_h(z): return TOE_MID_H + (HEEL_MID_H - TOE_MID_H) * ((1 - z) * .5)
    # The reference's black rubber "cup" wrapping the lower midsole is NOT
    # a flat colour stripe - it visibly climbs higher up the midsole at
    # the heel than at the toe. Modelled as its own row whose HEIGHT is a
    # function of z, not a fixed row index.
    def wrap_frac(z): return .30 + .32 * ((1 - z) * .5)  # fraction of midsole height the black wrap covers
    def WrapTop(a):
        z = z_of(a)
        return Ring(a, OUT_H + wrap_frac(z) * midsole_h(z), 1.015 if z <= 0 else .995)

    n = 28

    def MidTop(a):
        z = z_of(a)
        return Ring(a, OUT_H + midsole_h(z), 1.03 if z <= 0 else .99)  # midsole "shelf" overhangs slightly, more at the heel

    # DIAGNOSED (this is what actually made the Side view still read as a
    # boat hull after the superellipse fix - that fix only changes the
    # TOP-DOWN plan view, which a Side camera can't even see since it
    # flattens X away entirely): the collar-height AMPLITUDE was one
    # smooth, monotonic function of z, so the whole top edge forms a
    # single continuous ramp from toe to heel - exactly a hull sheer line,
    # not a shoe. A real low-top's topline stays relatively FLAT across
    # the midfoot/arch and only rises distinctly at the heel counter (see
    # the Infrared reference: the black-panel top edge is nearly level
    # from behind the toe to the heel notch, then kicks up).
    def collar_amp(z):
        zz = -z  # zz=+1 at heel, -1 at toe
        heel_ramp = max(0.0, (zz - .1) / .9) ** 1.6  # near 0 across the whole front/mid, rises only near the heel
        return .020 + .020 * heel_ramp

    def Upper(a, t):
        # Low-top: much shorter total rise than the Jordan high-top, and
        # the collar genuinely tapers down toward the toe (t->0 gives the
        # midsole-top ring itself, t->1 gives the low collar rim).
        z = z_of(a)
        base = MidTop(a)
        rise = collar_amp(z) * math.sin(t * math.pi / 2)
        taper = 1.0 - .05 * t  # the collar narrows only slightly (low-top, not a high-top point)
        p = base + Vector((0, 0, rise))
        # narrow the ring slightly as it rises, about the shoe's own centre line
        p.x = cx + (p.x - cx) * taper
        return p

    total_upper_rows = 6
    def Profile(row):
        if row == 0: return lambda a: Ring(a, 0.0, .97)          # outsole bottom
        if row == 1: return lambda a: Ring(a, OUT_H, 1.0)         # outsole top / waffle line
        if row == 2: return lambda a: WrapTop(a)                  # top of the shaped black rubber wrap
        if row == 3: return lambda a: MidTop(a)                   # midsole top (welds into Upper(a,0))
        t = (row - 3) / total_upper_rows
        return lambda a: Upper(a, t)

    total_rows = 3 + total_upper_rows
    materials = [outsole_black, midsole_white, overlay_navy, mesh_grey, black_trim, panel_red]
    IDX = {'outsole': 0, 'midsole': 1, 'overlay': 2, 'mesh': 3, 'trim': 4, 'panel': 5}

    def norm_deg(a):
        d = math.degrees(a) % 360
        return d - 360 if d > 180 else d

    # Recognition map (step 2/3 of the mandatory sequence): toe overlay cap
    # wraps the front hemisphere at low rows (like the Jordan toe cap, same
    # real reason - there is no diagonal seam there on the reference), a
    # throat gap of mesh shows between the overlay's edges (narrow at the
    # toe, widening toward the collar - reused curve, not a fixed wedge),
    # a wide side-panel BAND sits over the mesh at the mid rows across the
    # whole midfoot circumference (the AM90's signature curved overlay,
    # modelled as a real coloured band here rather than a separate applied
    # patch - a further pass could bevel its edges proud of the mesh), and
    # the top row is the black collar trim.
    def throat_half_angle(row):
        if row <= 0: return 0.0
        t = row / (total_upper_rows - 1)
        return 12 + 55 * t
    def upper_material(urow, adeg):
        aadeg = abs(adeg)
        if urow >= total_upper_rows - 1:
            return IDX['trim']  # collar rim, all the way around
        if 2 <= urow <= 4:
            return IDX['panel']  # the big curved midfoot side panel band
        if aadeg <= throat_half_angle(urow):
            return IDX['mesh']  # vamp/tongue throat gap
        return IDX['overlay'] if urow <= 1 else IDX['mesh']

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
            if row == 0:
                midx = IDX['outsole']
            elif row == 1:
                midx = IDX['outsole']  # band between outsole-top and WrapTop: the shaped black rubber wrap
            elif row == 2:
                midx = IDX['midsole']  # band between WrapTop and MidTop: the white foam column above the wrap
            else:
                urow = row - 3
                adeg = norm_deg((i + .5) * 2 * math.pi / n)
                midx = upper_material(urow, adeg)
            faces.append(((vidx(row, i), vidx(row, i2), vidx(row + 1, i2), vidx(row + 1, i)), midx))
    faces.append((tuple(vidx(0, i) for i in range(n)), IDX['outsole']))  # closed sole underside

    body = multimesh('Shoe body', vs, faces, materials)
    sub = body.modifiers.new('Subsurf', 'SUBSURF'); sub.levels = 2; sub.render_levels = 2
    before = snap('upper', before)

    # ---- Real local surface normal (finite-difference tangents) - the
    # same technique validated on the Jordan eyestay and the DA cap's
    # attached hardware, used here for the eyestay/tongue AND the heel
    # window/side-panel raise, instead of a crude radial guess. ----
    def surface_normal(a, t):
        eps = .015
        t0, t1 = max(t - eps, 0.0), min(t + eps, 1.0)
        da = Upper(a + eps, t) - Upper(a - eps, t)
        dt = Upper(a, t1) - Upper(a, t0)
        n = da.cross(dt)
        if n.length < 1e-9: return Vector((0, 1, 0))
        n.normalize()
        p = Upper(a, t)
        if n.dot(Vector((p.x - cx, p.y - cz, 0))) < 0: n = -n
        return n

    # ---- Heel Air-unit window: a real recessed lens, not a flat sticker.
    # RE-DIAGNOSED against the Infrared reference photo (the earlier pass
    # put this on the Upper() surface, high on the ankle - wrong; the real
    # window sits LOW, on the midsole/wrap side wall, and is elongated
    # FORE-AFT (along the shoe's length), not a small round dot). Built on
    # a dedicated MidWall(a,frac) surface between the wrap and the midsole
    # top, using the same flat-local-tangent-frame technique validated on
    # the DA cap's monogram - except the tangent frame here is deliberately
    # aligned with the shoe's own length direction (world Y projected onto
    # the surface), not an arbitrary Z-up hint, and pushed INWARD (a
    # recess) instead of outward.
    def MidWall(a, frac):
        z = z_of(a)
        y0 = OUT_H + wrap_frac(z) * midsole_h(z)
        y1 = OUT_H + midsole_h(z)
        s0 = 1.015 if z <= 0 else .995
        s1 = 1.03 if z <= 0 else .99
        return Ring(a, y0 + (y1 - y0) * frac, s0 + (s1 - s0) * frac)

    def midwall_normal(a, frac):
        eps = .04
        da = MidWall(a + eps, frac) - MidWall(a - eps, frac)
        df = MidWall(a, min(frac + eps, 1)) - MidWall(a, max(frac - eps, 0))
        n = da.cross(df)
        if n.length < 1e-9: return Vector((0, 1, 0))
        n.normalize()
        p = MidWall(a, frac)
        if n.dot(Vector((p.x - cx, p.y - cz, 0))) < 0: n = -n
        return n

    heel_a, heel_frac = math.pi, .45  # a=pi -> z=-1 (heel), mid-height of the wrap/midsole band
    heel_center = MidWall(heel_a, heel_frac)
    heel_n = midwall_normal(heel_a, heel_frac)
    tangent_length = Vector((0, 1, 0)) - heel_n * heel_n.dot(Vector((0, 1, 0)))
    tangent_length = tangent_length.normalized() if tangent_length.length > 1e-6 else Vector((1, 0, 0))
    tangent_vert = heel_n.cross(tangent_length).normalized()
    RX, RY = .026, .009  # elongated fore-aft pill, matching the reference's window shape
    window_vs, window_fs = [], []
    WSEG = 16
    for i in range(WSEG):
        ang = 2 * math.pi * i / WSEG
        local = tangent_length * (RX * math.cos(ang)) + tangent_vert * (RY * math.sin(ang))
        window_vs.append(tuple(heel_center + local - heel_n * .0035))  # recessed inward
    window_vs.append(tuple(heel_center - heel_n * .005))  # centre point, recessed deeper (a real domed lens)
    center_i = len(window_vs) - 1
    for i in range(WSEG):
        window_fs.append((center_i, i, (i + 1) % WSEG))
    window_obj = mesh('Heel air window', window_vs, window_fs, window_ice)

    # ---- Eyestay + tongue + ONE proof lace segment: identical proven
    # technique to the Jordan concept (real 3D geometry along the real
    # surface normal, not a billboard), rebuilt for this shoe's lower,
    # shorter throat. ----
    eyestay_rows_t = [i / (total_upper_rows - 1) for i in range(1, total_upper_rows - 1)]
    eyestay_rise = .010
    def eyestay_pt(t, side, raised):
        half = math.radians(throat_half_angle(round(t * (total_upper_rows - 1))) + 2)
        a = side * half
        p = Upper(a, t)
        if raised:
            p += surface_normal(a, t) * eyestay_rise
        return p
    eyestay_objs = {}
    for side in (-1, 1):
        vs2, fs2 = [], []
        for t in eyestay_rows_t:
            vs2.append(tuple(eyestay_pt(t, side, False)))
            vs2.append(tuple(eyestay_pt(t, side, True)))
        for i in range(len(eyestay_rows_t) - 1):
            k = i * 2
            fs2.append((k, k + 2, k + 3, k + 1))
        eyestay_objs[side] = mesh(f'Eyestay {side}', vs2, fs2, black_trim)

    # DIAGNOSED (not a camera trick): viewed dead-on from the front, the
    # toe's own collar rim is genuinely lower than the heel's, so an
    # orthographic front projection shows the hollow interior in the gap
    # above the toe - the same known, unresolved issue found on the
    # Jordan-style concept. On a REAL shoe the tongue is what visually
    # closes that gap from the front - it stands up close to the collar
    # rim height, not stopping well short of it. Extending the tongue's
    # own row range higher (toward the actual rim) is a genuine
    # construction fix for that reason, not a presentation trick; the
    # eyestay/lace rows are left untouched since real eyestays don't rise
    # that high.
    tongue_rows_t = eyestay_rows_t + [.95]
    tongue_vs, tongue_fs = [], []
    for t in tongue_rows_t:
        half = math.radians(throat_half_angle(round(min(t, .99) * (total_upper_rows - 1))) * .6)
        for side in (-1, 1):
            a = side * half
            p = Upper(a, t) + surface_normal(a, t) * (eyestay_rise * .4)
            tongue_vs.append(tuple(p))
    for i in range(len(tongue_rows_t) - 1):
        k = i * 2
        tongue_fs.append((k, k + 1, k + 3, k + 2))
    mesh('Tongue', tongue_vs, tongue_fs, mesh_grey)

    def lace_ribbon(p0, p1, width, thickness, outward_hint, m, name):
        trav = p1 - p0
        if trav.length < 1e-6: return None
        trav_n = trav.normalized()
        widen = trav_n.cross(outward_hint)
        widen = widen.normalized() if widen.length > 1e-6 else Vector((0, 0, 1))
        off = outward_hint * thickness
        a, b = p0 - widen * width / 2 + off, p0 + widen * width / 2 + off
        c, d_ = p1 + widen * width / 2 + off, p1 - widen * width / 2 + off
        return mesh(name, [tuple(a), tuple(b), tuple(c), tuple(d_)], [(0, 1, 2, 3)], m)

    eyestay_top = {side: [eyestay_pt(t, side, True) for t in eyestay_rows_t] for side in (-1, 1)}
    mid_out = surface_normal(0, eyestay_rows_t[0])
    lace_ribbon(eyestay_top[-1][0], eyestay_top[1][0], .008, .0028, mid_out, lace_black, 'Lace proof segment 0')
    before = snap('laces', before)

    count_group('air_max_style_shoe', GROUPS['sole'] + GROUPS['upper'] + [window_obj] + GROUPS['laces'])
    return L, heel_center

def set_stage(*visible_groups):
    visible = set()
    for g in visible_groups: visible.update(GROUPS[g])
    for g, objs in GROUPS.items():
        for o in objs: o.hide_render = o not in visible

cube('Ground', (0, 0, -.005), (3.0, 3.0, .008), mat('Backdrop', (.86, .85, .82)))
L, heel_center = shoe()
setup_lighting()
set_stage('sole', 'upper', 'laces')

CX = L * .5  # DIAGNOSED: the shoe's own X-centre (cx inside shoe()) is L*.5, NOT 0 -
# every camera below was centred at world X=0, silently clipping the shoe
# out of frame on one side. All X positions/targets now use CX instead.
CENTER = Vector((CX, 0, .05))
# Camera-Z-singularity check (upizup-blender-modeling hard-lesson #9)
# applied BEFORE building these: Front looks along -Y (not parallel to
# world Z) and Top looks along -Z exactly (IS parallel - handled with an
# explicit rotation_euler below, not a computed quaternion), so both are
# safe from the degenerate-roll bug found on the Jordan and DA-cap scripts.
# Front target lowered from .035 to .022 (real fix, not a camera trick):
# .035 sat ABOVE the toe's own low collar-rim height, so the camera's
# centre ray passed over the toe into the open throat behind it.
cam_front = make_camera('Cam_Front', (CX, .5, .028), (CX, 0, .022), .30)
cam_side = make_camera('Cam_Side', (.55, 0, .05), (0, 0, .045), .30)
cam_top = make_camera('Cam_Top', (CX, 0, .5), (CX, 0, .045), .38)
cam_top.rotation_euler = (0, 0, 0)
cam_3q = make_camera('Cam_ThreeQuarter', (CX + .28, -.32, .14), (CX - .02, 0, .045), .32)
cam_heel = make_camera('Cam_HeelCloseup', (heel_center.x + .09, heel_center.y - .10, heel_center.z + .02), tuple(heel_center), .08)

for name, cam in [('View-Front', cam_front), ('View-Side', cam_side), ('View-Top', cam_top), ('View-ThreeQuarter', cam_3q)]:
    render_view(cam, name)
render_view(cam_heel, 'HeelWindowCloseup')

bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'AirMaxStyleShoe.blend'))
render_view(cam_3q, 'AirMaxStyleShoe-Preview')
print('AIR_MAX_STYLE_SHOE_DONE ' + json.dumps(stats))
