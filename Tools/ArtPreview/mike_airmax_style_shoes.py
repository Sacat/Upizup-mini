"""Concept preview only - NOT integrated into the game, NOT shipping any
real trademark. Own implementation, own station tables and proportions -
no geometry, code or logos copied from the Codex AirMax reference
package. Matches this project's non-trademark homage convention (no
swoosh, no "AIR MAX"/"NIKE" wordmark - stylized "Mike" naming like the
already-shipped Mike90/Mike97 shoe slot).

REAL ROOT-CAUSE FIX applied this pass, after two earlier failed attempts
(a Jordan-style high-top that worked reasonably, then an Air Max 90
low-top rated 1/100 as "a boat hull"): the earlier low-top used a plain
sin/cos ellipse cross-section swept along one axis, which pinches to a
sharp POINT at the very toe/heel parameter extreme and keeps an IDENTICAL
plan-view shape at every height - exactly a canoe hull, not a shoe last.
Reviewing a real, working reference implementation (a parallel Codex
delivery for the same MINI-166 request, at
C:\\Users\\PCSS-PC\\.codex\\visualizations\\...\\airmax-modeling\\build_airmax.py)
confirmed the actual fix: an explicit LONGITUDINAL STATION TABLE (measured
width and top-height at a series of x positions along the shoe, not a
formula), interpolated with a smooth spline, plus an elliptic end-cap
function so the toe/heel curve rounds off instead of pinching to zero.
This file reimplements that same class of technique with its own code,
station numbers and proportions - not copied geometry.

Three variants from one generator: MIKE90 (thin sole, one rear heel Air
window, toe overlay + mudguard panels), MIKE97 (thinner ripple-panelled
upper, two Air windows joined by a bridge, closer to full-length),
MIKE270 (the odd one out - a single, much taller heel Air unit that
dominates the whole rear third of the shoe, the closest real analogue's
defining feature).

Run: blender --background --python Tools/ArtPreview/mike_airmax_style_shoes.py -- --model 90
Output: Logs/Tasks/Concept/Mike<model>/
"""
import bpy, math, json, sys, argparse
from pathlib import Path
from mathutils import Vector

P = Path(r"E:\Unity\Up Iz Up Mini")
ap = argparse.ArgumentParser()
ap.add_argument('--model', choices=['90', '97', '270'], default='90')
args = ap.parse_args(sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else [])
MODEL = args.model
OUT = P / 'Logs' / 'Tasks' / 'Concept' / ('Mike' + MODEL)
OUT.mkdir(parents=True, exist_ok=True)
stats = {}

bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
for m in list(bpy.data.materials): bpy.data.materials.remove(m)
sc = bpy.context.scene
sc.unit_settings.system = 'METRIC'; sc.unit_settings.length_unit = 'MILLIMETERS'

def mat(name, c, rough=.55, metal=0, trans=0):
    m = bpy.data.materials.new(name); m.diffuse_color = (*c, 1); m.use_nodes = True
    bs = m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*c, 1); bs.inputs['Roughness'].default_value = rough
    bs.inputs['Metallic'].default_value = metal
    if trans: bs.inputs['Transmission Weight'].default_value = trans; bs.inputs['IOR'].default_value = 1.4
    return m

def mm(v): return tuple(c / 1000 for c in v)

def meshobj(name, vs, fs, m):
    me = bpy.data.meshes.new(name); me.from_pydata([mm(v) for v in vs], [], fs); me.update()
    o = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(o); me.materials.append(m)
    for f in me.polygons: f.use_smooth = True
    return o

def cube(name, center, dims, m, bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=mm(center)); o = bpy.context.object; o.name = name
    o.dimensions = mm(dims)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True); o.data.materials.append(m)
    if bevel:
        b = o.modifiers.new('bevel', 'BEVEL'); b.width = bevel / 1000; b.segments = 2
        bpy.context.view_layer.objects.active = o; bpy.ops.object.modifier_apply(modifier=b.name)
        for f in o.data.polygons: f.use_smooth = True
    return o

def ribbon(name, pts, w, th, m):
    vs = []
    for i, p in enumerate(pts):
        d = Vector(pts[min(i + 1, len(pts) - 1)]) - Vector(pts[max(0, i - 1)])
        d = d.normalized() if d.length > 1e-6 else Vector((1, 0, 0))
        across = Vector((0, 0, 1)).cross(d)
        across = (across.normalized() if across.length > .01 else Vector((0, 1, 0))) * w / 2
        for sg, h in [(-1, -th / 2), (1, -th / 2), (1, th / 2), (-1, th / 2)]:
            vs.append(tuple(Vector(p) + across * sg + Vector((0, 0, h))))
    fs = []
    for i in range(len(pts) - 1):
        for j in range(4): fs.append((i * 4 + j, i * 4 + (j + 1) % 4, (i + 1) * 4 + (j + 1) % 4, (i + 1) * 4 + j))
    return meshobj(name, vs, fs, m)

# ---- Step 1/2 (mandatory sequence): station tables traced against real
# proportions researched earlier this session (heel wedge taller than
# forefoot, thin sole per the user's own "too thick" complaint) - a
# smooth Catmull-Rom-style spline through named (x, value) control
# points, NOT a formula-driven ellipse. x runs from -145 (heel) to +145
# (toe), millimetres, matching a ~290mm (US10-ish) last.
def interp(x, pts):
    if x <= pts[0][0]: return pts[0][1]
    if x >= pts[-1][0]: return pts[-1][1]
    for i, ((x0, y0), (x1, y1)) in enumerate(zip(pts, pts[1:])):
        if x <= x1:
            t = (x - x0) / (x1 - x0)
            p0 = pts[max(0, i - 1)]; p2 = pts[min(len(pts) - 1, i + 2)]
            m0 = (y1 - p0[1]) / max(1e-6, x1 - p0[0]); m1 = (p2[1] - y0) / max(1e-6, p2[0] - x0)
            h00 = 2 * t**3 - 3 * t * t + 1; h10 = t**3 - 2 * t * t + t
            h01 = -2 * t**3 + 3 * t * t; h11 = t**3 - t * t
            return h00 * y0 + h10 * (x1 - x0) * m0 + h01 * y1 + h11 * (x1 - x0) * m1
    return pts[-1][1]

WIDTH = [(-145, 2), (-135, 22), (-118, 38), (-90, 41), (-55, 39), (-20, 43), (20, 49), (55, 52), (90, 48), (118, 38), (136, 22), (145, 2)]
TOP = [(-145, 100), (-120, 100), (-90, 95), (-55, 100), (-10, 90), (30, 75), (70, 55), (105, 42), (130, 30), (145, 20)]
if MODEL == '97': WIDTH = [(x, w * .95) for x, w in WIDTH]; TOP = [(x, z - 3) for x, z in TOP]

def width(x):
    # Elliptic end-cap past the last real station: rounds off instead of
    # pinching to a point - the actual fix for the earlier "boat hull".
    if x >= 136: return max(1.0, 22 * math.sqrt(max(0, 1 - ((x - 136) / 9) ** 2)))
    if x <= -135: return max(1.0, 22 * math.sqrt(max(0, 1 - ((x + 135) / 10) ** 2)))
    return interp(x, WIDTH)

def spring(x): return 9 * max(0, (x - 80) / 65) ** 2  # toe-spring: sole lifts near the toe only
SOLE_TOP = [(-145, 30), (-90, 29), (-30, 23), (40, 19), (90, 18), (145, 17)]
def sole_top(x): return interp(x, SOLE_TOP) + spring(x)
def base(x): return sole_top(x) - 1.2
def top(x): return max(base(x) + 6, interp(x, TOP))
def archmax(x): return interp(x, [(-145, 1.55), (-120, 1.15), (-90, .85), (-30, .85), (40, .88), (80, 1.15), (145, 1.55)])

def surf(x, ang, side, offset=0):
    w = max(1.0, width(x) - 1.5)
    y = side * (w * math.cos(ang) + offset * math.cos(ang))
    z = base(x) + (top(x) - base(x)) * math.sin(ang) + offset * math.sin(ang)
    return (x, y, z)

XS = [-145 + i * 290 / 96 for i in range(97)]
PANEL, MESH_UP, TRIM, SOLE_BLACK, LACE_MAT, AIR_MAT = None, None, None, None, None, None

def patch(name, x0, x1, lo, hi, m, side, offset, nu=60, nv=14):
    vs = []
    for i in range(nu + 1):
        x = x0 + (x1 - x0) * i / nu
        l = lo(x) if callable(lo) else lo; h = hi(x) if callable(hi) else hi
        for j in range(nv + 1): vs.append(surf(x, l + (h - l) * j / nv, side, offset))
    fs = []
    for i in range(nu):
        for j in range(nv):
            k = i * (nv + 1) + j; f = (k, k + nv + 1, k + nv + 2, k + 1)
            fs.append(f[::-1] if side == 1 else f)
    return meshobj(name, vs, fs, m)

def build():
    global PANEL, MESH_UP, TRIM, SOLE_BLACK, LACE_MAT, AIR_MAT
    accent = (.75, .12, .10) if MODEL != '270' else (.10, .12, .18)
    PANEL = mat('Overlay panel', (.30, .33, .35), .82)
    MESH_UP = mat('Woven mesh upper', (.60, .62, .63), .8)
    TRIM = mat('Accent trim', accent, .5)
    SOLE_WHITE = mat('Foam midsole', (.90, .89, .86), .6)
    SOLE_BLACK = mat('Rubber outsole', (.03, .035, .04), .8)
    LACE_MAT = mat('Laces', (.85, .84, .80), .85)
    AIR_MAT = mat('Air bladder', (.68, .78, .82), .12, trans=.8)
    support = mat('Air pillar', (.4, .46, .42), .45)

    # ---- Upper shell: one continuous mesh (proven Subsurf-safe technique)
    # for the mesh body, with SEPARATE welded panels layered on the exact
    # same surf() surface so nothing floats or intersects by accident.
    for side in (-1, 1):
        patch(f'Upper mesh {side}', -145, 145, 0, archmax, MESH_UP, side, 0, 96, 18)
        # Mudguard: a real overlay strip, own boundary curve, not a colour band.
        def mud(x): return interp(x, [(-145, .30), (-100, .28), (-40, .35), (30, .42), (90, .52), (145, .28)])
        patch(f'Mudguard {side}', -145, 145, .02, mud, SOLE_BLACK if MODEL != '270' else PANEL, side, .8, 70, 6)
        if MODEL == '90':
            patch(f'Toe overlay {side}', 60, 143, .5, lambda x: min(1.5, .7 + (x - 60) * .014), PANEL, side, .9, 34, 5)
            patch(f'Quarter overlay {side}', -138, 55, lambda x: mud(x) + .04, lambda x: min(archmax(x) - .08, mud(x) + .3), PANEL, side, .75, 55, 6)
        elif MODEL == '97':
            for band in range(3):
                def wave(x, b=band): return min(archmax(x) - .05, .3 + b * .18 + .07 * math.sin((x + 30) / 45))
                patch(f'Ripple {side} {band}', -145, 145, lambda x, b=band: max(.03, wave(x, b) - .03), lambda x, b=band: wave(x, b) + .03, PANEL, side, .95, 80, 3)
        else:  # 270: minimal panelling, let the huge heel Air unit be the whole story
            patch(f'Heel counter overlay {side}', -145, -60, lambda x: mud(x) + .03, archmax, PANEL, side, .85, 40, 6)

    # ---- Tongue: real arched patch overlapping the vamp under the laces,
    # not a flat decal - same technique already proven on the Jordan/DA cap.
    def tongue_w(x): return interp(x, [(-45, 17), (-20, 23), (15, 24), (45, 20), (65, 15)])
    def tongue_z(x): return top(x) + 2 + 6 * max(0, (-x - 25) / 20) ** 2
    vs, fs = [], []
    for i in range(36):
        x = -45 + 110 * i / 35
        for j in range(11):
            v = -1 + 2 * j / 10
            w = tongue_w(x)
            ratio = math.sqrt(max(.02, 1 - v * v))
            vs.append((x, v * w, base(x) + (top(x) - base(x)) * ratio + tongue_z(x) - top(x) + 2))
    for i in range(35):
        for j in range(10):
            k = i * 11 + j; fs.append((k, k + 11, k + 12, k + 1))
    meshobj('Tongue', vs, fs, MESH_UP)

    # ---- Eyestay + real crossed laces, all on the shoe's own surface.
    lace_xs = [-25, -8, 8, 26, 44] if MODEL != '270' else [-20, -2, 16, 34]
    for side in (-1, 1):
        for x in lace_xs:
            y = side * (tongue_w(x) + 2); z = base(x) + (top(x) - base(x)) + 2
            cube(f'Eyelet {side} {x}', (x, y, z), (7, 6, 2.5), TRIM, .8)
    for i in range(len(lace_xs) - 1):
        for side in (-1, 1):
            x0, x1 = lace_xs[i], lace_xs[i + 1]; pts = []
            for j in range(20):
                t = j / 19; x = x0 + (x1 - x0) * t
                y = (1 - t) * side * (tongue_w(x0) + 1.5) + t * (-side) * (tongue_w(x1) + 1.5)
                z = base(x) + (top(x) - base(x)) + 3 + 1.4 * math.sin(math.pi * t)
                pts.append((x, y, z))
            ribbon(f'Lace {i} {side}', pts, 2.6, .55, LACE_MAT)

    # ---- Sole: explicit closed perimeter loft (own two-sided technique),
    # thin per the user's own "too thick" complaint researched earlier.
    per = [(x, width(x)) for x in XS] + [(x, -width(x)) for x in reversed(XS)]
    def sole_mesh(name, bottom, upper, m):
        vs = []; n = len(per)
        for h in (bottom, upper):
            for x, y in per: vs.append((x, y, h(x)))
        fs = [tuple(reversed(range(n))), tuple(range(n, 2 * n))]
        fs += [(i, (i + 1) % n, (i + 1) % n + n, i + n) for i in range(n)]
        return meshobj(name, vs, fs, m)
    sole_mesh('Outsole', lambda x: spring(x), lambda x: 4 + spring(x), SOLE_BLACK)
    foam = sole_mesh('Midsole foam', lambda x: 3.5 + spring(x), sole_top, SOLE_WHITE)

    # ---- Real Air cavity: boolean-cut through the foam, not a decal -
    # the confirmed-working technique (same class of fix validated on the
    # reviewed Codex reference; own numbers/placement here).
    if MODEL == '90':
        windows = [(-95, 65, 16, 12)]
    elif MODEL == '97':
        windows = [(-120, 30, 14, 11), (10, 190, 12, 10)]
    else:  # 270: one dominant, much taller unit filling most of the heel's height
        windows = [(-105, 78, 20, 20)]
    for idx, (x, length, z, h) in enumerate(windows):
        cutter = cube('cutter', (x, 0, z), (length, 130, h), SOLE_BLACK, 3.5)
        bpy.context.view_layer.objects.active = foam
        bmod = foam.modifiers.new('air_cavity_' + str(idx), 'BOOLEAN'); bmod.operation = 'DIFFERENCE'; bmod.solver = 'EXACT'; bmod.object = cutter
        bpy.ops.object.modifier_apply(modifier=bmod.name)
        bpy.data.objects.remove(cutter, do_unlink=True)
        for side in (-1, 1):
            pts = [(x - length / 2 + 4 + (length - 8) * j / 40, side * (width(x - length / 2 + 4 + (length - 8) * j / 40) - 2.2), z) for j in range(41)]
            v, f = [], []
            for j, p in enumerate(pts):
                for k in range(10):
                    t = 2 * math.pi * k / 10
                    v.append((p[0], p[1] + math.cos(t) * 2.0, p[2] + math.sin(t) * (h / 2 - .8)))
            for j in range(40):
                for k in range(10): f.append((j * 10 + k, j * 10 + (k + 1) % 10, (j + 1) * 10 + (k + 1) % 10, (j + 1) * 10 + k))
            f += [tuple(reversed(range(10))), tuple(40 * 10 + k for k in range(10))]
            meshobj(f'Air bladder {idx} {side}', v, f, AIR_MAT)
            for j in range(1, max(2, round(length / 20))):
                xx = x - length / 2 + length * j / max(2, round(length / 20))
                cube('Air pillar', (xx, side * (width(xx) - 4.2), z), (2.6, 3.2, h - 3), support, 1)

    be = foam.modifiers.new('edge', 'BEVEL'); be.width = .0006; be.segments = 2
    foam.modifiers.new('wn', 'WEIGHTED_NORMAL')

    # ---- Waffle/tread nubs - discrete blocks, not a texture.
    for x in range(-130, 130, 14):
        for y in range(-38, 39, 12):
            if abs(y) < width(x) - 6:
                cube('Tread', (x, y, spring(x) - .3), (8, 8, .8), SOLE_BLACK, .5)

    # ---- Closed end caps at heel and toe - the earlier "boat hull" also
    # lacked this; a real last is closed solid at both tips, not an open
    # tube ring collapsing to a point.
    for xx, label in ((-145, 'Heel'), (145, 'Toe')):
        vv = [surf(xx, math.pi * k / 24, 1, .1) for k in range(25)]
        vv.append((xx, 0, base(xx)))
        ff = [(25, k, k + 1) for k in range(24)]
        meshobj(label + ' end cap', vv, ff, MESH_UP)

def setup_lighting():
    sc.render.engine = 'CYCLES'; sc.cycles.samples = 48; sc.cycles.use_denoising = True
    sc.render.resolution_x = 1500; sc.render.resolution_y = 1100; sc.render.resolution_percentage = 100
    sc.world.use_nodes = True
    sc.world.node_tree.nodes['Background'].inputs[0].default_value = (.82, .82, .85, 1)
    sc.world.node_tree.nodes['Background'].inputs[1].default_value = .5
    sc.view_settings.view_transform = 'AgX'
    for name, loc, power in [('Key', (110, -330, 480), 3.0), ('Fill', (-170, 280, 300), 1.4), ('Rim', (-340, -20, 300), 2.2)]:
        bpy.ops.object.light_add(type='AREA', location=mm(loc)); o = bpy.context.object; o.name = name
        o.data.energy = power; o.data.size = .45
        o.rotation_euler = (Vector((0, 0, .05)) - o.location).to_track_quat('-Z', 'Y').to_euler()
    cube('Ground', (0, 0, -3), (2500, 2500, 4), mat('Backdrop', (.86, .85, .82)))

def camera(name, loc, target, scale):
    bpy.ops.object.camera_add(location=mm(loc)); o = bpy.context.object; o.name = name
    o.rotation_euler = (Vector(mm(target)) - o.location).to_track_quat('-Z', 'Y').to_euler()
    o.data.type = 'ORTHO'; o.data.ortho_scale = scale / 1000; o.data.clip_start = .001; o.data.clip_end = 10
    return o

def render(cam, name):
    sc.camera = cam; sc.render.filepath = str(OUT / (name + '.png')); bpy.ops.render.render(write_still=True)

build()
setup_lighting()

# Camera-Z-singularity check applied up front (upizup-blender-modeling
# hard-lesson #9): Top looks straight down world Z - degenerate for
# to_track_quat - fixed with an explicit rotation_euler, not trusted math.
cam_side = camera('Side', (0, -700, 55), (0, 0, 50), 330)
cam_top = camera('Top', (0, 0, 700), (0, 0, 0), 330)
cam_top.rotation_euler = (0, 0, 0)
cam_front = camera('Front', (700, 0, 45), (0, 0, 40), 165)
cam_back = camera('Back', (-700, 0, 55), (0, 0, 50), 165)
cam_3q = camera('ThreeQuarter', (340, -540, 270), (0, 0, 50), 340)

for name, cam in [('View-Side', cam_side), ('View-Top', cam_top), ('View-Front', cam_front), ('View-Back', cam_back), ('View-ThreeQuarter', cam_3q)]:
    render(cam, name)

deps = bpy.context.evaluated_depsgraph_get(); tris = 0
for o in bpy.context.scene.objects:
    if o.type != 'MESH': continue
    ev = o.evaluated_get(deps); me = ev.to_mesh(); me.calc_loop_triangles(); tris += len(me.loop_triangles); ev.to_mesh_clear()
stats = {'model': MODEL, 'evaluated_triangles': tris, 'objects': len(bpy.context.scene.objects)}
bpy.ops.wm.save_as_mainfile(filepath=str(OUT / f'Mike{MODEL}.blend'))
render(cam_3q, f'Mike{MODEL}-Preview')
print('MIKE_AIRMAX_DONE ' + json.dumps(stats))
