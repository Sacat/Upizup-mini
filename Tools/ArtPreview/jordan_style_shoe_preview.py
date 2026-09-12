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
def setup(campos, target, scale):
    sc = bpy.context.scene; sc.render.engine = 'CYCLES'; sc.cycles.samples = 64
    sc.cycles.use_denoising = True; sc.view_settings.exposure = .4
    sc.render.resolution_x = 1600; sc.render.resolution_y = 1200; sc.render.resolution_percentage = 100
    sc.world.use_nodes = True; sc.world.node_tree.nodes['Background'].inputs[0].default_value = (.8, .79, .77, 1)
    sc.world.node_tree.nodes['Background'].inputs[1].default_value = 1.0
    bpy.ops.object.light_add(type='AREA', location=(-.3, -.5, .45)); bpy.context.object.data.energy = 60; bpy.context.object.data.size = .5
    bpy.ops.object.light_add(type='SUN', location=(0, 0, .6)); sun = bpy.context.object; sun.rotation_euler = (.5, -.2, .5); sun.data.energy = 1.6; sun.data.angle = .1
    bpy.ops.object.camera_add(location=campos); cam = bpy.context.object; cam.rotation_euler = (Vector(target) - cam.location).to_track_quat('-Z', 'Y').to_euler()
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = scale; cam.data.clip_start = .001; cam.data.clip_end = 5
    sc.camera = cam
    sc.view_settings.view_transform = 'AgX'
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

    # No wing-accent panel this round - the previous attempt used an
    # arbitrary floating quad unrelated to the throat/panel geometry and
    # read as a random unattached triangle in the checklist comparison.
    # Removing it rather than leaving a known-bad element in.

    # ---- Tongue: a raised flap sitting INSIDE the same throat opening the
    # panel colouring now actually defines (aadeg < throat_half_angle),
    # not a guessed fixed-angle strip disconnected from it. ----
    tongue_vs, tongue_fs = [], []
    tongue_rows = [3, 4.5, 6, 7]
    for row in tongue_rows:
        t = row / 8
        half = math.radians(throat_half_angle(row) * .7)  # narrower than the full throat, sits inside it
        for side in (-1, 1):
            p = Upper(side * half, t); p.z += .008
            tongue_vs.append(tuple(p))
    for row in range(len(tongue_rows) - 1):
        k = row * 2
        tongue_fs.append((k, k + 1, k + 3, k + 2))
    mesh('Tongue', tongue_vs, tongue_fs, white)

    # Eyelets/laces deliberately REMOVED this round. Three straight attempts
    # (cube eyelets + rod cross, torus eyelets + rod cross, cube eyelets +
    # curve laces with corrected spacing) all produced a tangled, broken-
    # looking cluster rather than a clean lace line - a real, repeated
    # failure mode for procedural discrete-object detail at this scale
    # without interactive placement/preview, not something to keep
    # guessing coefficients at. Shipping the correct body/silhouette/
    # colour-blocking honestly rather than a known-broken detail on top of
    # it. See upizup-blender-modeling's "known failure patterns" for this
    # written up as a standing lesson.
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
# User: shoe was "0.1/100" partly because the camera stared straight down
# the shoe's own length axis (Y), producing a cone-on-end view instead of
# a recognizable side profile. The shoe's length runs along Y, width
# along X - so the camera needs its dominant offset along X (side-on),
# matching how the real reference photo is framed (a 3/4 side profile).
setup((0.55, -.14, .10), (0.13, 0, .045), .32)

# Sole+upper are now one welded continuous body (required for Subsurf to
# work across the colour seams), so the staged inspection is body-alone
# then body+details rather than the old separate sole/upper split.
stages = [
    ('Stage1-Body', ('upper',)),
    ('Stage2-Everything', ('upper', 'laces')),
]
for name, groups in stages:
    set_stage(*groups)
    render_to(name)
set_stage('sole', 'upper', 'laces')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'JordanStyleShoe.blend'))
render_to('JordanStyleShoe-Preview')
print('JORDAN_STYLE_SHOE_DONE ' + json.dumps(stats))
