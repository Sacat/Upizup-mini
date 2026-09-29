"""MINI-182: fixed-camera turnaround render for one house row (+ optional comparison row). Run: blender -b -P render_house_turnaround.py -- <row_id> <out_dir> [compare_row_id]"""
import sys, bpy, math
from pathlib import Path
from mathutils import Vector
sys.path.insert(0, str(Path(__file__).parent))
import house_system as hs
argv = sys.argv[sys.argv.index('--') + 1:]
rid, out = argv[0], Path(argv[1]); cmp_id = argv[2] if len(argv) > 2 else None
out.mkdir(parents=True, exist_ok=True)
hs.reset(); M = hs.make_materials()
row = hs.load_row(rid)
sets = [(row, 0.0)]
if cmp_id: sets.append((hs.load_row(cmp_id), 9.0))
report = []
for r, x in sets:
    objs = hs.build_house(r, (x, 0), M)
    report.append(f"{r['id']}: triangles={hs.tri_count(objs)} objects={len(objs)} materials={len({m.name for o in objs for m in o.data.materials if o.type=='MESH'})} budgetLod0={r['budget']['trisLod0']}")
# context: ground, sidewalk, road (same as MINI-141 preview)
ground = hs._mat('Warm grass ground', (.28, .36, .19)); road = hs._mat('Unmarked asphalt', (.12, .15, .15))
hs.cube('Ground', (4.5, 1, -.10), (30, 14, .18), ground)
hs.cube('Sidewalk', (4.5, -2.52, .012), (30, 1.05, .12), M['concrete'], .015)
hs.cube('Road', (4.5, -5.45, -.015), (30, 4.7, .10), road)
sc = bpy.context.scene; sc.render.engine = 'CYCLES'; sc.cycles.samples = 24; sc.cycles.use_denoising = True
sc.view_settings.view_transform = 'AgX'; sc.view_settings.exposure = .6
sc.render.resolution_x = 1300; sc.render.resolution_y = 900
sc.world = bpy.data.worlds.new('w') if sc.world is None else sc.world
sc.world.use_nodes = True; sc.world.node_tree.nodes['Background'].inputs[0].default_value = (.65, .77, .88, 1); sc.world.node_tree.nodes['Background'].inputs[1].default_value = .5
bpy.ops.object.light_add(type='AREA', location=(-9, -12, 18)); bpy.context.object.data.energy = 2200; bpy.context.object.data.shape = 'DISK'; bpy.context.object.data.size = 10
bpy.ops.object.light_add(type='SUN', location=(0, 0, 12)); sun = bpy.context.object; sun.rotation_euler = (.45, -.4, -.4); sun.data.energy = 1.5; sun.data.angle = .12
bpy.ops.object.camera_add(); cam = bpy.context.object; sc.camera = cam
cx = 4.5 if cmp_id else 0.0
views = {'front': ((cx, -30, 4.2), (cx, 1.8, 3.6), 17 if cmp_id else 10.2), 'side': ((cx + 30, 1.8, 4.2), (cx, 1.8, 3.6), 10.2), 'top': ((cx, 1.8, 40), (cx, 1.8, 0), 14 if cmp_id else 9.0), '34': ((cx - 14, -17, 12), (cx, 1.8, 3.0), 16 if cmp_id else 10.5)}
suffix = '_pair' if cmp_id else ''
for name, (pos, look, scale) in views.items():
    if cmp_id and name not in ('front', '34'): continue
    cam.location = pos; cam.rotation_euler = (Vector(look) - Vector(pos)).to_track_quat('-Z', 'Y').to_euler()
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = scale
    sc.render.filepath = str(out / f'{rid}{suffix}_{name}.png'); bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(out / f'{rid}{suffix}.blend'))
(out / f'{rid}{suffix}_report.txt').write_text('\n'.join(report))
print('MINI182_RENDER_DONE', report)
