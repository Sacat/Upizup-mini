"""MINI-182: build a SECOND, independent 8x8-cell palette texture (the shared one is full at 60/64 cells).
Run: python Tools/ArtPreview/palette_second.py coast  -> Logs/Tasks/MINI-182/Export/palette_coast.png + coast_cells.txt
Same cell layout/UV rule as the shared palette (cell = (7 - y//16)*8 + x//16, u=(cell%8+.5)/8, v=(cell//8+.5)/8)."""
import sys, json, zlib, struct
from pathlib import Path
OUT = Path(r'E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-182\Export')
name = sys.argv[1]
colors = json.loads(Path(__file__).with_name(name + '_colors.json').read_text())['colors']
assert len(colors) <= 64
def srgb(c): return round(255 * (12.92 * c if c <= .0031308 else 1.055 * c ** (1 / 2.4) - .055))
def chunk(t, d): return struct.pack('>I', len(d)) + t + d + struct.pack('>I', zlib.crc32(t + d) & 0xffffffff)
cells = {k: i for i, k in enumerate(colors)}; pal = [tuple(v) for v in colors.values()]
raw = bytearray()
for y in range(128):
    raw.append(0)
    for x in range(128):
        cell = (7 - y // 16) * 8 + x // 16; c = pal[cell] if cell < len(pal) else (.5, .5, .5)
        raw.extend([srgb(v) for v in c] + [255])
sig = bytes([137, 80, 78, 71, 13, 10, 26, 10])
(OUT / f'palette_{name}.png').write_bytes(sig + chunk(b'IHDR', struct.pack('>IIBBBBB', 128, 128, 8, 6, 0, 0, 0)) + chunk(b'sRGB', bytes([0])) + chunk(b'IDAT', zlib.compress(bytes(raw))) + chunk(b'IEND', b''))
(OUT / f'{name}_cells.txt').write_text('\n'.join(f'{k} {v}' for k, v in cells.items()))
print('PALETTE_SECOND_DONE', len(cells))
