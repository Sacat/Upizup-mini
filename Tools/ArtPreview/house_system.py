"""MINI-182 House Model System: table-driven house generator (Blender 5.0).

Usage inside Blender:
    import house_system as hs
    hs.reset(); mats = hs.make_materials(); objs = hs.build_house(hs.load_row('flat_concrete_2s'), (0,0), mats)

All sizes/colours come from house_definitions.json. Front faces -Y, +Z up. The part builders below
reproduce the approved MINI-141 one/two-storey houses exactly (regression rows) and add new families.
"""
import bpy, math, json
from mathutils import Vector
from pathlib import Path

DEFS = Path(__file__).with_name('house_definitions.json')


def load_defs():
    return json.loads(DEFS.read_text())


def load_row(hid):
    for r in load_defs()['houses']:
        if r['id'] == hid:
            return r
    raise KeyError(hid)


def reset():
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
    for m in list(bpy.data.materials): bpy.data.materials.remove(m)


def _mat(name, c, rough=.7, metal=0):
    m = bpy.data.materials.new(name); m.diffuse_color = (*c, 1); m.use_nodes = True
    bs = m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*c, 1); bs.inputs['Roughness'].default_value = rough
    bs.inputs['Metallic'].default_value = metal
    return m


def make_materials():
    """One Blender material per named colour (named exactly as in the JSON so the exporter palette is stable)."""
    out = {}
    for name, c in load_defs()['colors'].items():
        rough, metal = (.42, .55) if name in ('roof_aged_metal', 'roof_rust') else (.5, .25) if name == 'roof_red' else (.45, .5) if name == 'rail_dark' else (.27, 0) if name == 'glass' else (.7, 0)
        out[name] = _mat(name, c, rough, metal)
    return out


def cube(name, p, s, m, bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=p); o = bpy.context.object; o.name = name
    o.scale = s; bpy.ops.object.transform_apply(location=False, rotation=False, scale=True); o.data.materials.append(m)
    if bevel:
        b = o.modifiers.new('small real edge', 'BEVEL'); b.width = bevel; b.segments = 1
        o.modifiers.new('weighted normals', 'WEIGHTED_NORMAL')
    return o


def mesh(name, vs, fs, m):
    me = bpy.data.meshes.new(name); me.from_pydata(vs, [], fs); me.update()
    o = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(o); o.data.materials.append(m); return o


def rod(name, a, b, r, m, n=8):
    d = Vector(b) - Vector(a)
    bpy.ops.mesh.primitive_cone_add(vertices=n, radius1=r, radius2=r * .85, depth=d.length, location=(Vector(a) + Vector(b)) / 2)
    o = bpy.context.object; o.name = name; o.rotation_euler = d.to_track_quat('Z', 'Y').to_euler(); o.data.materials.append(m); return o


def ico(name, p, s, m, sub=1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub, radius=1, location=p)
    o = bpy.context.object; o.name = name; o.scale = s; o.data.materials.append(m)
    for poly in o.data.polygons: poly.use_smooth = True
    return o


# ---------------- part builders ----------------
def window(x, y, z, M):
    cube('Recess dark window', (x, y, z), (1.05, .06, 1.16), M['glass'])
    for xx in [x - .56, x + .56]: cube('Window side reveal', (xx, y - .04, z), (.085, .14, 1.3), M['limewash'], .015)
    for zz in [z - .65, z + .65]: cube('Window sill lintel', (x, y - .06, zz), (1.22, .20, .09), M['limewash'], .015)
    cube('Mullion', (x, y - .1, z), (.06, .04, 1.14), M['limewash'])
    for zz in [z - .26, z + .26]: cube('Glazing bar', (x, y - .11, zz), (1.04, .035, .04), M['limewash'])


def side_window(x, y, z, M, sign):
    """Window on a side wall (normal +/-X). x is the wall plane, y the position along the wall."""
    cube('Side recess window', (x, y, z), (.06, 1.05, 1.16), M['glass'])
    for yy in [y - .56, y + .56]: cube('Side reveal', (x + sign * .04, yy, z), (.14, .085, 1.3), M['limewash'], .015)
    for zz in [z - .65, z + .65]: cube('Side sill lintel', (x + sign * .06, y, zz), (.20, 1.22, .09), M['limewash'], .015)
    cube('Side mullion', (x + sign * .1, y, z), (.04, .06, 1.14), M['limewash'])


def shopfront(cx, fy, z, M, accent, width=3.6):
    """Ground-floor shop: dark recess, glass display with mullions and a stallboard, sloped awning with valance and brackets, sign band."""
    hw = width / 2
    cube('Shop recess', (cx, fy - .03, z + 1.2), (width + .2, .06, 2.3), M['ink'])
    cube('Shop glass', (cx, fy - .07, z + 1.32), (width - .1, .04, 1.72), M['glass'])
    for mx in (-hw * .5, 0.0, hw * .5): cube('Shop mullion', (cx + mx, fy - .10, z + 1.32), (.07, .05, 1.76), M['limewash'])
    cube('Shop transom', (cx, fy - .10, z + 2.20), (width, .06, .07), M['limewash'])
    cube('Shop stallboard', (cx, fy - .09, z + .27), (width + .18, .14, .54), M['concrete'], .015)
    cube('Shop lintel', (cx, fy - .07, z + 2.32), (width + .3, .16, .16), M['limewash'], .015)
    for sx in (-1, 1): cube('Shop jamb', (cx + sx * (hw + .1), fy - .07, z + 1.2), (.14, .14, 2.4), M['limewash'], .012)
    aw = cube('Shop awning', (cx, fy - .5, z + 2.42), (width + .5, 1.0, .06), M[accent], .01); aw.rotation_euler = (math.radians(20), 0, 0)
    cube('Awning valance', (cx, fy - .96, z + 2.13), (width + .5, .05, .18), M[accent], .008)
    for sx in (-1, 1): rod('Awning bracket', (cx + sx * (hw + .2), fy - .02, z + 2.5), (cx + sx * (hw + .2), fy - .93, z + 2.15), .022, M['rail_dark'], 5)
    cube('Sign band', (cx, fy - .05, z + 2.66), (width + .3, .06, .26), M['rail_dark'], .01)
    cube('Sign band trim', (cx, fy - .07, z + 2.66), (width + .1, .03, .05), M['limewash'])


def door(x, y, z, M):
    cube('Door recess', (x, y - .08, z + 1.04), (1.04, .08, 2.08), M['ink'])
    cube('Panelled timber door', (x, y - .13, z + 1.03), (.87, .075, 1.98), M['timber_green'], .015)
    for dz in [.65, 1.45]: cube('Door raised panel', (x, y - .18, z + dz), (.64, .025, .55), M['timber_green'], .016)
    ico('Door handle', (x + .30, y - .23, z + 1.02), (.025, .03, .025), M['limewash'])


def balcony(x, fy, w, z, M):
    """Cantilever slab + rail across the front at floor level z; fy = front wall plane Y."""
    cube('Balcony slab', (x, fy - .57, z - .03), (w + .16, 1.15, .16), M['concrete'], .025)
    for xx in [x - w / 2 + .16, x + w / 2 - .16]:
        cube('Balcony support', (xx, fy - .95, z - 1.35), (.17, .17, 2.65), M['limewash'], .015)
    rod('Balcony rail', (x - w / 2 + .1, fy - 1.13, z + .90), (x + w / 2 - .1, fy - 1.13, z + .90), .035, M['rail_dark'])
    n = 23
    for i in range(n):
        xx = x - w / 2 + .15 + i * (w - .3) / (n - 1)
        rod('Baluster', (xx, fy - 1.13, z + .07), (xx, fy - 1.13, z + .89), .018, M['rail_dark'], 6)


def roof_gable(x, y, w, d, base, row, M):
    r = row['roof']; ridge = base + r['rise']; roofmat = M[r['material']]
    hx = w / 2 + .24; hy = d / 2 + .22   # 3.04 / 2.57 for the approved 5.6 x 4.7 houses
    vs = [(x - hx, y - hy, base), (x + hx, y - hy, base), (x - hx, y, ridge), (x + hx, y, ridge), (x - hx, y + hy, base), (x + hx, y + hy, base)]
    mesh('Two pitched roof planes', vs, [(0, 1, 3, 2), (2, 3, 5, 4)], roofmat)
    if r.get('ribs'):
        for i in range(36):
            xx = x - hx + 2 * hx * i / 35
            rod('Metal roof rib', (xx, y - hy, base + .025), (xx, y, ridge + .025), .014, roofmat, 5)
            rod('Metal roof rib', (xx, y, ridge + .025), (xx, y + hy, base + .025), .014, roofmat, 5)
    for yy in [y - hy, y + hy]:
        cube('Roof fascia', (x, yy, base - .035), (2 * hx + .11, .11, .14), M['limewash'], .01)


def roof_gable_depth(x, y, w, d, base, row, M):
    """Gable roof whose ridge runs front-to-back (gable end faces the street), corrugated ribs, bargeboards."""
    r = row['roof']; ridge = base + r['rise']; roofmat = M[row['palette']['roof']]
    hx = w / 2 + r['overhang']; yf = y - d / 2 - r['frontOverhang']; yb = y + d / 2 + r.get('backOverhang', r['overhang'])
    mesh('Roof left plane', [(x - hx, yf, base), (x - hx, yb, base), (x, yb, ridge), (x, yf, ridge)], [(0, 1, 2, 3)], roofmat)
    mesh('Roof right plane', [(x + hx, yf, base), (x, yf, ridge), (x, yb, ridge), (x + hx, yb, base)], [(0, 1, 2, 3)], roofmat)
    if r.get('ribs'):
        n = int((yb - yf) / r['ribSpacing'])
        for i in range(n + 1):
            yy = yf + (yb - yf) * i / n
            rod('Corrugation rib', (x - hx, yy, base + .03), (x, yy, ridge + .03), .016, roofmat, 4)
            rod('Corrugation rib', (x, yy, ridge + .03), (x + hx, yy, base + .03), .016, roofmat, 4)
    rod('Ridge cap', (x, yf, ridge + .05), (x, yb, ridge + .05), .06, roofmat, 6)
    for yy in (yf, yb):   # bargeboards along the rake edges
        rod('Bargeboard', (x - hx, yy, base), (x, yy, ridge), .05, M['limewash'], 5)
        rod('Bargeboard', (x, yy, ridge), (x + hx, yy, base), .05, M['limewash'], 5)
    return ridge


def roof_hip(x, y, w, d, base, row, M):
    """Four-plane hip roof: 45 deg hips at the ends, ridge half-length = hx - hy. Metal ribs on the two long planes."""
    r = row['roof']; ov = r['overhang']; ridge = base + r['rise']; roofmat = M[r['material']]
    hx = w / 2 + ov; hy = d / 2 + ov; rl = max(.2, hx - hy)
    b = [(x - hx, y - hy, base), (x + hx, y - hy, base), (x + hx, y + hy, base), (x - hx, y + hy, base)]
    rf = [(x - rl, y, ridge), (x + rl, y, ridge)]
    mesh('Hip roof front', [b[0], b[1], rf[1], rf[0]], [(0, 1, 2, 3)], roofmat)
    mesh('Hip roof back', [b[3], b[2], rf[1], rf[0]], [(0, 3, 2, 1)], roofmat)
    mesh('Hip roof left', [b[0], b[3], rf[0]], [(0, 2, 1)], roofmat)
    mesh('Hip roof right', [b[1], b[2], rf[1]], [(0, 1, 2)], roofmat)
    if r.get('ribs'):
        rise = r['rise']
        for i in range(30):   # long planes: ribs run straight up the slope, ending on the ridge or the hip line
            xx = x - hx + 2 * hx * (i + .5) / 30; k = min(1.0, (hx - abs(xx - x)) / hy)
            for sg in (-1, 1):
                rod('Metal roof rib', (xx, y + sg * hy, base + .025), (xx, y + sg * hy * (1 - k), base + rise * k + .025), .014, roofmat, 5)
        for j in range(11):   # end planes: ribs run along X toward the ridge end
            yy = y - hy + 2 * hy * (j + .5) / 11; k = (hy - abs(yy - y)) / hy
            for sg in (-1, 1):
                rod('Metal roof rib', (x + sg * hx, yy, base + .025), (x + sg * (hx - hy * k), yy, base + rise * k + .025), .014, roofmat, 5)
    rod('Ridge cap', (x - rl, y, ridge + .04), (x + rl, y, ridge + .04), .05, roofmat, 6)
    cube('Eave fascia front', (x, y - hy, base - .035), (2 * hx + .11, .11, .14), M['limewash'], .01)
    cube('Eave fascia back', (x, y + hy, base - .035), (2 * hx + .11, .11, .14), M['limewash'], .01)
    for sx in (-1, 1): cube('Eave fascia side', (x + sx * hx, y, base - .035), (.11, 2 * hy - .11, .14), M['limewash'], .01)


def roof_flat(x, y, w, d, top, row, M):
    r = row['roof']; t = r['parapetThickness']; ph = r['parapet']; ov = r['slabOverhang']
    cube('Roof slab', (x, y, top + .07), (w + 2 * ov, d + 2 * ov, .14), M['concrete'], .02)
    z = top + .14 + ph / 2
    cube('Parapet front', (x, y - d / 2 - ov + t / 2, z), (w + 2 * ov, t, ph), M[row['palette']['_wall']], .012)
    cube('Parapet back', (x, y + d / 2 + ov - t / 2, z), (w + 2 * ov, t, ph), M[row['palette']['_wall']], .012)
    for sx in (-1, 1):
        cube('Parapet side', (x + sx * (w / 2 + ov - t / 2), y, z), (t, d + 2 * ov - 2 * t, ph), M[row['palette']['_wall']], .012)
    cz = top + .14 + ph + .03; W = w + 2 * ov + .08; D = d + 2 * ov + .08; ct = t + .08
    cube('Coping front', (x, y - D / 2 + ct / 2, cz), (W, ct, .06), M['limewash'], .01)
    cube('Coping back', (x, y + D / 2 - ct / 2, cz), (W, ct, .06), M['limewash'], .01)
    for sx in (-1, 1): cube('Coping side', (x + sx * (W / 2 - ct / 2), y, cz), (ct, D - 2 * ct, .06), M['limewash'], .01)
    # low scupper drain at one corner
    cube('Roof scupper', (x + w / 2 + ov, y - d / 2 + .4, top + .2), (.10, .18, .10), M['concrete'])


def build_house(row, origin, M, y0=1.8):
    """Build one row at origin=(x,_) (front faces -Y). Returns the list of created objects."""
    start = set(bpy.data.objects)
    x = origin[0]; y = y0
    w = row['footprint']['w']; d = row['footprint']['d']; levels = row['storeys']
    sh = row['storeyHeight']; h = levels * sh
    wallmat = M[row['palette']['wall']]
    row = dict(row); row['palette'] = dict(row['palette']); row['palette']['_wall'] = row['palette']['wall']
    cube('Foundation', (x, y, row['foundation'] / 2), (w + .14, d + .14, row['foundation']), M['concrete'], .025)
    cube('Plaster house shell', (x, y, h / 2 + .3), (w, d, h), wallmat, .035)
    fy0 = y - d / 2
    for level in range(levels):
        z = .3 + level * sh
        cube('Horizontal concrete course', (x, y, z + .10), (w + .07, d + .07, .13), M['limewash'], .015)
        for kind, off in row['facade']['storeyElements'][level]:
            if kind == 'window': window(x + off, y - d / 2 - .045, z + 1.50, M)
            elif kind == 'door': door(x + off, y - d / 2, z, M)
            elif kind == 'shopfront': shopfront(x + off, y - d / 2, z, M, row['palette'].get('accent', 'rail_dark'))
        if row['facade'].get('sideWindows'):
            for sx in (-1, 1):
                side_window(x + sx * (w / 2 + .045), y + .3, z + 1.50, M, sx)
        if level == 1 and row['facade'].get('balcony'):
            balcony(x, fy0, w, z, M)
    base = h + .30; rt = row['roof']['type']; fy = y - d / 2
    if rt == 'gable':
        roof_gable(x, y, w, d, base, row, M)
        ridge = base + row['roof']['rise']
        for xx in [x - w / 2, x + w / 2]:
            mesh('Plastered gable', [(xx, y - d / 2, base), (xx, y + d / 2, base), (xx, y, ridge - .10)], [(0, 1, 2)], wallmat)
        rod('Gutter downpipe', (x + w / 2 - .07, fy - .13, .4), (x + w / 2 - .07, fy - .13, base - .1), .035, M['limewash'])
    elif rt == 'gable_depth':
        ridge = roof_gable_depth(x, y, w, d, base, row, M)
        for yy in (y - d / 2, y + d / 2):
            mesh('Plastered gable end', [(x - w / 2, yy, base), (x + w / 2, yy, base), (x, yy, ridge)], [(0, 1, 2)] if yy < y else [(0, 2, 1)], wallmat)
        rod('Gutter downpipe', (x + w / 2 - .07, fy - .13, .4), (x + w / 2 - .07, fy - .13, base - .1), .035, M['limewash'])
    elif rt == 'hip':
        roof_hip(x, y, w, d, base, row, M)
        rod('Gutter downpipe', (x + w / 2 - .07, fy - .13, .4), (x + w / 2 - .07, fy - .13, base - .1), .035, M['limewash'])
    elif rt == 'flat':
        roof_flat(x, y, w, d, base, row, M)
        rod('Gutter downpipe', (x + w / 2 + .03, fy - .05, .4), (x + w / 2 + .03, fy - .05, base - .05), .04, M['limewash'])
    door_x = next((off for kind, off in row['facade']['storeyElements'][0] if kind == 'door'), 0.0)
    for step in range(3):
        cube('Entry step', (x + door_x, fy - .32 - step * .22, .24 - step * .075), (1.5, .34, .12), M['concrete'], .014)
    if row['facade'].get('canopy'):
        cm = M[row['palette']['roof']]; cxp = x + door_x
        cube('Door canopy', (cxp, fy - .55, 2.62), (1.9, 1.0, .07), cm, .015)
        for sx in (-.8, .8): cube('Canopy post', (cxp + sx, fy - .98, 1.32), (.08, .08, 2.6), M['limewash'], .01)
    if row['facade'].get('veranda'):
        roofmat = M[row['roof']['material']]
        cube('Veranda canopy', (x, fy - .58, 2.66), (5.75, 1.2, .12), roofmat, .02)
        for xx in [x - 2.55, x + 2.55]: cube('Veranda post', (xx, fy - 1.01, 1.46), (.13, .13, 2.42), M['limewash'], .012)
    return list(set(bpy.data.objects) - start)


def tri_count(objs):
    deps = bpy.context.evaluated_depsgraph_get(); n = 0
    for o in objs:
        if o.type != 'MESH': continue
        me = o.evaluated_get(deps).to_mesh(); me.calc_loop_triangles(); n += len(me.loop_triangles); o.evaluated_get(deps).to_mesh_clear()
    return n
