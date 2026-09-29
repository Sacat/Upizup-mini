"""MINI-182 stage 4: additive JSON export for table-driven scenery. Run:
  blender -b -P Tools/ArtPreview/scenery_export.py -- coconut_palm
Starts from the latest MINI-182 palette (which already includes the shipped MINI-142 palette and the house colours)
and only APPENDS new colours. Writes Logs/Tasks/MINI-182/Export/Scenery*.json + palette.png + manifest.json.
Unity coordinates X,Z,Y from Blender (reflection) with reversed triangle winding, same as house_export.py.
"""
import sys, json, zlib, struct
from pathlib import Path
import bpy
from mathutils import Vector
sys.path.insert(0, str(Path(__file__).parent))
import scenery_system as ss

ROOT = Path(r'E:\Unity\Up Iz Up Mini')
OLD = ROOT / 'Logs/Tasks/MINI-142/Export'
OUT = ROOT / 'Logs/Tasks/MINI-182/Export'; OUT.mkdir(parents=True, exist_ok=True)
BASE = OUT / 'manifest.json' if (OUT / 'manifest.json').exists() else OLD / 'manifest.json'
palette = [tuple(c) for c in json.loads(BASE.read_text())['paletteLinearColors']]
assert palette[:20] == [tuple(c) for c in json.loads((OLD / 'manifest.json').read_text())['paletteLinearColors']][:20], 'shipped palette cells changed'
n_existing = 20
n_before = len(palette)


def color_index(material):
    c = tuple(round(float(v), 6) for v in material.diffuse_color[:3]) if material else (.5, .5, .5)
    if c not in palette: palette.append(c)
    return palette.index(c)


def part(name, objects, origin, decimate=False):
    vs = []; normals = []; tris = []; uv = []
    for obj in sorted(objects, key=lambda o: o.name):
        if obj.type != 'MESH': continue
        if decimate:
            mod = obj.modifiers.new('MINI182 distant simplification', 'DECIMATE'); mod.ratio = .5
        deps = bpy.context.evaluated_depsgraph_get(); ev = obj.evaluated_get(deps); me = ev.to_mesh(); me.calc_loop_triangles()
        nm = obj.matrix_world.to_3x3().inverted().transposed()
        for tri in me.loop_triangles:
            material = me.materials[tri.material_index] if me.materials else None
            cell = color_index(material); u = ((cell % 8) + .5) / 8; v = ((cell // 8) + .5) / 8
            base = len(vs)
            for li in tri.loops:
                loop = me.loops[li]; p = obj.matrix_world @ me.vertices[loop.vertex_index].co - Vector(origin)
                n = (nm @ me.corner_normals[li].vector).normalized()
                vs.append(dict(x=round(p.x, 6), y=round(p.z, 6), z=round(p.y, 6)))
                normals.append(dict(x=round(n.x, 6), y=round(n.z, 6), z=round(n.y, 6))); uv.append(dict(x=u, y=v))
            tris.extend([base, base + 2, base + 1])
        ev.to_mesh_clear()
        if decimate: obj.modifiers.remove(mod)
    return dict(name=name, vertices=vs, normals=normals, triangles=tris, uv=uv)


summary = {}


def export_row(row, seed, asset_name):
    ss.reset(); M = ss.make_materials()
    objs = ss.build(row, seed, (0, 1.8), M)
    origin = (0, 1.8, 0)
    body = part('Body', objs, origin)
    lod1 = dict(name='LOD1', parts=[part('Body', objs, origin, True)])
    pts = body['vertices']
    bounds = {k: dict(min=min(v[k] for v in pts), max=max(v[k] for v in pts)) for k in ('x', 'y', 'z')}
    crown = max(bounds['x']['max'], -bounds['x']['min'], bounds['z']['max'], -bounds['z']['min'])
    data = dict(name=asset_name, parts=[body], lods=[lod1], bounds=bounds,
                dimensions={k: round(b['max'] - b['min'], 6) for k, b in bounds.items()}, palette='palette.png',
                family=row['kind'], bodyHeight=bounds['y']['max'], footprintW=round(crown * 2, 3), footprintD=round(crown * 2, 3))
    (OUT / (asset_name + '.json')).write_text(json.dumps(data, separators=(',', ':')))
    summary[asset_name] = dict(row=row['id'], seed=seed, triangles=len(body['triangles']) // 3,
                               lod1Triangles=len(lod1['parts'][0]['triangles']) // 3, dimensions=data['dimensions'])


argv = sys.argv[sys.argv.index('--') + 1:]
row = ss.load_row(argv[0])
camel = ''.join(w.capitalize() for w in row['id'].split('_'))
for seed in row['seeds']:
    export_row(row, seed, 'Scenery' + camel + '_' + str(seed))


def srgb(c): return round(255 * (12.92 * c if c <= .0031308 else 1.055 * c ** (1 / 2.4) - .055))
def chunk(t, d): return struct.pack('>I', len(d)) + t + d + struct.pack('>I', zlib.crc32(t + d) & 0xffffffff)


raw = bytearray()
for y in range(128):
    raw.append(0)
    for x in range(128):
        cell = (7 - y // 16) * 8 + x // 16; c = palette[cell] if cell < len(palette) else (.5, .5, .5)
        raw.extend([srgb(v) for v in c] + [255])
sig = bytes([137, 80, 78, 71, 13, 10, 26, 10])
(OUT / 'palette.png').write_bytes(sig + chunk(b'IHDR', struct.pack('>IIBBBBB', 128, 128, 8, 6, 0, 0, 0)) + chunk(b'sRGB', bytes([0])) + chunk(b'IDAT', zlib.compress(bytes(raw))) + chunk(b'IEND', b''))
(OUT / 'manifest.json').write_text(json.dumps(dict(assets=summary, paletteLinearColors=palette, existingCells=n_existing, appendedCells=len(palette) - n_existing,
    notes=['Append-only palette across houses and scenery', 'Unity X,Z,Y reflected from Blender; reversed winding']), indent=2))
print('MINI182_SCENERY_EXPORT_DONE', json.dumps(summary), 'palette', n_before, '->', len(palette), flush=True)
