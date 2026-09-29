"""MINI-182: additive JSON export for table-driven houses. Run:
  blender -b -P Tools/ArtPreview/house_export.py -- flat_concrete_2s
Reads the EXISTING palette from Logs/Tasks/MINI-142/Export/manifest.json and only APPENDS new colours, so every
existing house/crop keeps its palette cells. Writes to Logs/Tasks/MINI-182/Export (MINI-142 export is never modified).
Unity coordinates X,Z,Y from Blender (reflection) with reversed triangle winding, same as mini142_export.py.
"""
import sys, json, zlib, struct, copy
from pathlib import Path
import bpy
from mathutils import Vector
sys.path.insert(0, str(Path(__file__).parent))
import house_system as hs

ROOT = Path(r'E:\Unity\Up Iz Up Mini')
OLD = ROOT / 'Logs/Tasks/MINI-142/Export'
OUT = ROOT / 'Logs/Tasks/MINI-182/Export'; OUT.mkdir(parents=True, exist_ok=True)
# Append-only across runs: start from the latest MINI-182 palette if one exists, else the shipped MINI-142 palette.
BASE = OUT / 'manifest.json' if (OUT / 'manifest.json').exists() else OLD / 'manifest.json'
palette = [tuple(c) for c in json.loads(BASE.read_text())['paletteLinearColors']]
assert palette[:20] == [tuple(c) for c in json.loads((OLD / 'manifest.json').read_text())['paletteLinearColors']][:20], 'shipped palette cells changed'
n_existing = 20


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


def export_row(row, asset_name):
    hs.reset(); M = hs.make_materials()
    objs = hs.build_house(row, (0, 0), M)
    origin = (0, 1.8, 0)
    body = part('Body', objs, origin)
    lod1 = dict(name='LOD1', parts=[part('Body', objs, origin, True)])
    pts = body['vertices']
    bounds = {k: dict(min=min(v[k] for v in pts), max=max(v[k] for v in pts)) for k in ('x', 'y', 'z')}
    data = dict(name=asset_name, parts=[body], lods=[lod1], bounds=bounds, dimensions={k: round(b['max'] - b['min'], 6) for k, b in bounds.items()}, palette='palette.png', footprintW=row['footprint']['w'], footprintD=row['footprint']['d'], bodyHeight=row['storeys'] * row['storeyHeight'], family=row['family'])
    (OUT / (asset_name + '.json')).write_text(json.dumps(data, separators=(',', ':')))
    summary[asset_name] = dict(row=row['id'], wall=row['palette']['wall'], triangles=len(body['triangles']) // 3, lod1Triangles=len(lod1['parts'][0]['triangles']) // 3, dimensions=data['dimensions'], bounds=bounds)


argv = sys.argv[sys.argv.index('--') + 1:]
row = hs.load_row(argv[0])
suffix = ''.join(w.capitalize() for w in row['id'].split('_'))
variants = row.get('variants') or [dict(name=''.join(w.capitalize() for w in wall.replace('wall_', '').split('_')), wall=wall, roof=row['palette']['roof']) for wall in row.get('wallChoices', [row['palette']['wall']])]
for v in variants:
    r = copy.deepcopy(row); r['palette']['wall'] = v['wall']; r['palette']['roof'] = v['roof']
    export_row(r, 'House' + suffix + '_' + v['name'])

def srgb(c): return round(255 * (12.92 * c if c <= .0031308 else 1.055 * c ** (1 / 2.4) - .055))
def chunk(t, d): return struct.pack('>I', len(d)) + t + d + struct.pack('>I', zlib.crc32(t + d) & 0xffffffff)
raw = bytearray()
for y in range(128):
    raw.append(0)
    for x in range(128):
        cell = (7 - y // 16) * 8 + x // 16; c = palette[cell] if cell < len(palette) else (.5, .5, .5)
        raw.extend([srgb(v) for v in c] + [255])
(OUT / 'palette.png').write_bytes(b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', 128, 128, 8, 6, 0, 0, 0)) + chunk(b'sRGB', b'\x00') + chunk(b'IDAT', zlib.compress(raw)) + chunk(b'IEND', b''))
(OUT / 'manifest.json').write_text(json.dumps(dict(assets=summary, paletteLinearColors=palette, existingCells=n_existing, appendedCells=len(palette) - n_existing,
    notes=['Only appended palette cells; cells 0..existingCells-1 identical to MINI-142', 'Unity X,Z,Y reflected from Blender; reversed winding', 'Front faces Unity -Z before the 180 deg root rotation used by MINI-142 fitting']), indent=2))
print('MINI182_EXPORT_DONE', json.dumps(summary), 'palette', n_existing, '->', len(palette), flush=True)
