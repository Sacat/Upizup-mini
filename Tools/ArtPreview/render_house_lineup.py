"""MINI-182: fixed-camera LINEUP of several rows side by side (scale/style check). Run: blender -b -P render_house_lineup.py -- <out_dir> <name> <row_id> [<row_id> ...]"""
import sys, bpy
from pathlib import Path
from mathutils import Vector
sys.path.insert(0, str(Path(__file__).parent))
import house_system as hs
argv = sys.argv[sys.argv.index('--') + 1:]
out = Path(argv[0]); name = argv[1]; ids = argv[2:]
out.mkdir(parents=True, exist_ok=True)
hs.reset(); M = hs.make_materials()
report = []; gap = 8.0; x = 0.0; xs = []
for hid in ids:
    row = hs.load_row(hid); objs = hs.build_house(row, (x, 0), M)
    report.append(f"{hid}: {row['footprint']['w']}x{row['footprint']['d']} tris={hs.tri_count(objs)}"); xs.append(x); x += gap
total = x - gap; cx = total / 2
ground = hs._mat('Warm grass ground', (.28, .36, .19)); road = hs._mat('Unmarked asphalt', (.12, .15, .15))
hs.cube('Ground', (cx, 1, -.10), (total + 20, 14, .18), ground)
hs.cube('Sidewalk', (cx, -2.52, .012), (total + 20, 1.05, .12), M['concrete'], .015)
hs.cube('Road', (cx, -5.45, -.015), (total + 20, 4.7, .10), road)
sc = bpy.context.scene; sc.render.engine = 'CYCLES'; sc.cycles.samples = 24; sc.cycles.use_denoising = True
sc.view_settings.view_transform = 'AgX'; sc.view_settings.exposure = .6
sc.render.resolution_x = 1900; sc.render.resolution_y = 900
sc.world = bpy.data.worlds.new('w') if sc.world is None else sc.world
sc.world.use_nodes = True; sc.world.node_tree.nodes['Background'].inputs[0].default_value = (.65, .77, .88, 1); sc.world.node_tree.nodes['Background'].inputs[1].default_value = .5
bpy.ops.object.light_add(type='AREA', location=(cx - 9, -12, 18)); bpy.context.object.data.energy = 2600; bpy.context.object.data.shape = 'DISK'; bpy.context.object.data.size = 12
bpy.ops.object.light_add(type='SUN', location=(0, 0, 12)); sun = bpy.context.object; sun.rotation_euler = (.45, -.4, -.4); sun.data.energy = 1.5; sun.data.angle = .12
bpy.ops.object.camera_add(); cam = bpy.context.object; sc.camera = cam
views = {'front': ((cx, -40, 4.0), (cx, 1.8, 3.4), total + 8.5), '34': ((cx - 22, -26, 14), (cx, 1.8, 2.6), total + 9.0)}
for vn, (pos, look, scale) in views.items():
    cam.location = pos; cam.rotation_euler = (Vector(look) - Vector(pos)).to_track_quat('-Z', 'Y').to_euler()
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = scale
    sc.render.filepath = str(out / f'{name}_{vn}.png'); bpy.ops.render.render(write_still=True)
(out / f'{name}_report.txt').write_text('\n'.join(report)); print('MINI182_LINEUP_DONE', report)
