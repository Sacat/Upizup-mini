"""MINI-182: append named colours to the shared palette WITHOUT Blender.
Run: python Tools/ArtPreview/palette_append.py ground   (reads Tools/ArtPreview/<name>_colors.json)
Starts from Logs/Tasks/MINI-182/Export/manifest.json (append-only), rewrites palette.png + manifest.json and writes
Export/<name>_cells.txt ("colour_name cell_index") for Unity editor tools. Cells 0..19 are asserted identical to MINI-142.
"""
import sys, json, zlib, struct
from pathlib import Path

ROOT = Path(r'E:\Unity\Up Iz Up Mini')
OLD = ROOT / 'Logs/Tasks/MINI-142/Export'
OUT = ROOT / 'Logs/Tasks/MINI-182/Export'
name = sys.argv[1]
colors = json.loads((Path(__file__).with_name(name + '_colors.json')).read_text())['colors']
man = json.loads((OUT / 'manifest.json').read_text())
palette = [tuple(c) for c in man['paletteLinearColors']]
if len(sys.argv) > 2: palette = palette[:int(sys.argv[2])]   # optional: drop this family's earlier UNSHIPPED cells and re-append
assert palette[:20] == [tuple(c) for c in json.loads((OLD / 'manifest.json').read_text())['paletteLinearColors']][:20]
before = len(palette); cells = {}
for cname, c in colors.items():
    t = tuple(round(float(v), 6) for v in c)
    if t not in palette: palette.append(t)
    cells[cname] = palette.index(t)
assert len(palette) <= 64, 'palette full'


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
man['paletteLinearColors'] = palette; man['appendedCells'] = len(palette) - 20
(OUT / 'manifest.json').write_text(json.dumps(man, indent=2))
(OUT / (name + '_cells.txt')).write_text('\n'.join(f'{k} {v}' for k, v in cells.items()))
print('PALETTE_APPEND_DONE', before, '->', len(palette), cells)
