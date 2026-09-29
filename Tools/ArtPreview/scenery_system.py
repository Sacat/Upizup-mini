"""MINI-182 stage 4: table-driven scenery (trees) generator for Blender 5.0.

Rows live in scenery_definitions.json (kind: palm | broadleaf | banana). Same conventions as house_system.py:
+Z up, plants stand at (x, y) on z=0, colours are named linear RGB entries in the JSON and appended to the shared
palette on export (append-only). Deterministic per (row, seed).
"""
import bpy, math, json, random
from mathutils import Vector
from pathlib import Path

DEFS = Path(__file__).with_name('scenery_definitions.json')


def load_defs():
    return json.loads(DEFS.read_text())


def load_row(rid):
    for r in load_defs()['scenery']:
        if r['id'] == rid:
            return r
    raise KeyError(rid)


def reset():
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
    for m in list(bpy.data.materials): bpy.data.materials.remove(m)


def _mat(name, c, rough=.75, metal=0):
    m = bpy.data.materials.new(name); m.diffuse_color = (*c, 1); m.use_nodes = True
    bs = m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*c, 1); bs.inputs['Roughness'].default_value = rough; bs.inputs['Metallic'].default_value = metal
    return m


def make_materials():
    return {name: _mat(name, c) for name, c in load_defs()['colors'].items()}


def mesh(name, vs, fs, mats, idx=None):
    me = bpy.data.meshes.new(name); me.from_pydata([tuple(v) for v in vs], [], fs); me.update()
    o = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(o)
    for m in mats: o.data.materials.append(m)
    if idx is not None:
        for p, i in zip(o.data.polygons, idx): p.material_index = i
    return o


def cyl_tube(name, pts, radii, n, mats, band=False):
    """Tapered tube along a polyline pts with per-point radius; optional alternating material bands (ring scars)."""
    vs = []; fs = []; idx = []
    for i, (c, r) in enumerate(zip(pts, radii)):
        for k in range(n):
            a = k * math.tau / n
            vs.append((c[0] + math.cos(a) * r, c[1] + math.sin(a) * r, c[2]))
    for i in range(len(pts) - 1):
        for k in range(n):
            a = i * n + k; b = i * n + (k + 1) % n; c2 = (i + 1) * n + (k + 1) % n; d = (i + 1) * n + k
            fs.append((a, b, c2, d)); idx.append((i % 2) if band and len(mats) > 1 else 0)
    top = len(vs); vs.append(tuple(pts[-1])); base = (len(pts) - 1) * n
    for k in range(n): fs.append((base + k, base + (k + 1) % n, top)); idx.append(0)
    return mesh(name, vs, fs, mats, idx)


def blob(name, centre, radii, mat, rng, jitter=.22, sub=1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub, radius=1, location=centre)
    o = bpy.context.object; o.name = name
    me = o.data
    for v in me.vertices:
        f = 1 + rng.uniform(-jitter, jitter)
        v.co = Vector((v.co.x * radii[0] * f, v.co.y * radii[1] * f, v.co.z * radii[2] * f))
    o.data.materials.append(mat)
    for p in me.polygons: p.use_smooth = False
    return o


def build_palm(row, seed, origin, M):
    rng = random.Random(seed); ox, oy = origin[0], origin[1]
    H = rng.uniform(*row['height']); lean = rng.uniform(*row['lean']); az = rng.uniform(0, math.tau)
    rings = 11; pts = []; radii = []
    for i in range(rings + 1):
        t = i / rings
        pts.append((ox + math.cos(az) * lean * t ** 1.7, oy + math.sin(az) * lean * t ** 1.7, H * t))
        radii.append(row['trunkRadius'] * (1 - .42 * t) + .14 * math.exp(-t * 14))
    cyl_tube('Palm trunk', pts, radii, 8, [M['trunk_palm'], M['trunk_palm_dark']], band=True)
    top = Vector(pts[-1])
    n = rng.randint(*row['fronds'])
    for k in range(n):
        a = k * math.tau / n + rng.uniform(-.25, .25)
        elev = math.radians(rng.uniform(15, 68) if k % 2 == 0 else rng.uniform(40, 78))
        L = rng.uniform(*row['frondLength']); dirh = Vector((math.cos(a), math.sin(a), 0)); perp = Vector((-math.sin(a), math.cos(a), 0))
        segs = 7; rib = []
        for s in range(segs + 1):
            t = s / segs
            rib.append(top + dirh * (L * math.cos(elev) * t) + Vector((0, 0, L * math.sin(elev) * t - 1.7 * L * .5 * t * t)))
        vs = list(rib); fs = []
        left = []; right = []
        for s in range(segs + 1):
            t = s / segs; ll = row['leafletLength'] * (math.sin(math.pi * min(1, t * 1.05 + .05)) ** .8) * (1.0 if s % 2 else .6)
            drop = Vector((0, 0, -.55 * ll)) + dirh * (.25 * ll)
            left.append(rib[s] + perp * ll + drop); right.append(rib[s] - perp * ll + drop)
        base = len(vs); vs.extend(left); vs.extend(right)
        for s in range(1, segs + 1):
            fs.append((s - 1, s, base + s)); fs.append((s - 1, base + s, base + s - 1))
            rb = base + segs + 1
            fs.append((s, s - 1, rb + s)); fs.append((s - 1, rb + s - 1, rb + s))
        mesh('Palm frond', vs, fs, [M['palm_leaf_a' if k % 2 else 'palm_leaf_b']])
    for j in range(rng.randint(2, 4)):
        a = rng.uniform(0, math.tau)
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=.2, location=top + Vector((math.cos(a) * .35, math.sin(a) * .35, -.5)))
        o = bpy.context.object; o.name = 'Coconut'; o.data.materials.append(M['coconut'])


def build_broadleaf(row, seed, origin, M):
    rng = random.Random(seed); ox, oy = origin[0], origin[1]
    trunkH = rng.uniform(*row['trunkHeight']); R = rng.uniform(*row['canopyRadius'])
    pts = [(ox + math.sin(i * .9) * .12, oy, trunkH * i / 5) for i in range(6)]
    cyl_tube('Trunk', pts, [row['trunkRadius'] * (1 - .35 * i / 5) + (.12 if i == 0 else 0) for i in range(6)], 7, [M['broad_trunk']])
    top = Vector(pts[-1])
    limbs = rng.randint(2, 3)
    for l in range(limbs):
        a = l * math.tau / limbs + rng.uniform(-.4, .4)
        end = top + Vector((math.cos(a) * R * .55, math.sin(a) * R * .55, R * .45))
        seg = [tuple(top.lerp(end, s / 3)) for s in range(4)]
        cyl_tube('Limb', seg, [row['trunkRadius'] * .62 * (1 - .5 * s / 3) for s in range(4)], 6, [M['broad_trunk']])
    canopy_c = top + Vector((0, 0, R * .85))
    for b in range(rng.randint(*row.get('blobs', [6, 8]))):
        a = rng.uniform(0, math.tau); d = rng.uniform(0, R * .62); h = rng.uniform(-.25, .7) * R
        r = rng.uniform(.42, .70) * R
        col = M['broad_leaf_dark'] if h < 0 else (M['broad_leaf_light'] if h > .35 * R else M['broad_leaf_mid'])
        blob('Canopy blob', canopy_c + Vector((math.cos(a) * d, math.sin(a) * d, h)), (r, r, r * .70), col, rng, jitter=.28)


def build_banana(row, seed, origin, M):
    rng = random.Random(seed); ox, oy = origin[0], origin[1]
    n = rng.randint(*row['plants'])
    for p in range(n):
        a = p * math.tau / n + rng.uniform(-.5, .5); off = Vector((math.cos(a), math.sin(a), 0)) * (0 if p == 0 else rng.uniform(.7, 1.1))
        H = rng.uniform(*row['height']) * (1 if p == 0 else .8); base = Vector((ox, oy, 0)) + off
        pts = [tuple(base + Vector((.05 * i, 0, H * i / 3))) for i in range(4)]
        cyl_tube('Pseudostem', pts, [.15 * (1 - .3 * i / 3) for i in range(4)], 8, [M['banana_stem']])
        crown = Vector(pts[-1])
        for j in range(rng.randint(5, 7)):
            aa = j * math.tau / 6 + rng.uniform(-.3, .3); dirh = Vector((math.cos(aa), math.sin(aa), 0)); side = Vector((-math.sin(aa), math.cos(aa), 0))
            L = rng.uniform(*row.get('leafLength', [1.5, 2.0])); vs = []; fs = []
            for s in range(7):
                t = s / 6; c = crown + dirh * (L * .95 * t) + Vector((0, 0, .45 * math.sin(math.pi * t * .9) - .55 * t * t))
                w = row.get('leafWidth', .30) * math.sin(math.pi * (t * .92 + .04)) ** .55
                vs.extend([c - side * w - Vector((0, 0, w * .3)), c, c + side * w - Vector((0, 0, w * .3))])
                if s:
                    q = (s - 1) * 3; fs.extend([(q, q + 1, q + 4, q + 3), (q + 1, q + 2, q + 5, q + 4)])
            mesh('Banana leaf', vs, fs, [M['banana_leaf_a' if j % 2 else 'banana_leaf_b']])


BUILDERS = {'palm': build_palm, 'broadleaf': build_broadleaf, 'banana': build_banana}


def build(row, seed, origin, M):
    start = set(bpy.data.objects)
    BUILDERS[row['kind']](row, seed, origin, M)
    return list(set(bpy.data.objects) - start)


def tri_count(objs):
    deps = bpy.context.evaluated_depsgraph_get(); n = 0
    for o in objs:
        if o.type != 'MESH': continue
        me = o.evaluated_get(deps).to_mesh(); me.calc_loop_triangles(); n += len(me.loop_triangles); o.evaluated_get(deps).to_mesh_clear()
    return n
