"""MINI-182 stage 4: fixed-camera scenery lineup with a reference house. blender -b -P render_scenery_lineup.py -- <out_dir> <name> <house_row> <scenery_row:seed> [...]"""
import sys, bpy
from pathlib import Path
from mathutils import Vector
sys.path.insert(0, str(Path(__file__).parent))
import scenery_system as ss
import house_system as hs
argv = sys.argv[sys.argv.index('--') + 1:]
out = Path(argv[0]); name = argv[1]; house_id = argv[2]; items = argv[3:]
out.mkdir(parents=True, exist_ok=True)
ss.reset(); M = ss.make_materials()
HM = hs.make_materials()
M.update({k: v for k, v in HM.items() if k not in M})
x = 0.0; gap = 9.0; report = []
hobjs = hs.build_house(hs.load_row(house_id), (x, 0), HM); report.append(f"{house_id}: tris={hs.tri_count(hobjs)}"); x += gap
for it in items:
    rid, seed = it.split(':'); row = ss.load_row(rid)
    objs = ss.build(row, int(seed), (x, 1.8), M); report.append(f"{rid}#{seed}: tris={ss.tri_count(objs)} (budget {row['budget']['trisLod0']})"); x += gap
total = x - gap; cx = total / 2
ground = hs._mat('Warm grass ground', (.28, .36, .19)); road = hs._mat('Unmarked asphalt', (.12, .15, .15))
hs.cube('Ground', (cx, 1, -.10), (total + 24, 16, .18), ground)
hs.cube('Road', (cx, -5.45, -.015), (total + 24, 4.7, .10), road)
sc = bpy.context.scene; sc.render.engine = 'CYCLES'; sc.cycles.samples = 24; sc.cycles.use_denoising = True
sc.view_settings.view_transform = 'AgX'; sc.view_settings.exposure = .6
sc.render.resolution_x = 1900; sc.render.resolution_y = 1000
sc.world = bpy.data.worlds.new('w') if sc.world is None else sc.world
sc.world.use_nodes = True; sc.world.node_tree.nodes['Background'].inputs[0].default_value = (.65, .77, .88, 1); sc.world.node_tree.nodes['Background'].inputs[1].default_value = .5
bpy.ops.object.light_add(type='AREA', location=(cx - 9, -12, 22)); bpy.context.object.data.energy = 3200; bpy.context.object.data.shape = 'DISK'; bpy.context.object.data.size = 14
bpy.ops.object.light_add(type='SUN', location=(0, 0, 12)); sun = bpy.context.object; sun.rotation_euler = (.45, -.4, -.4); sun.data.energy = 1.6; sun.data.angle = .12
bpy.ops.object.camera_add(); cam = bpy.context.object; sc.camera = cam
views = {'front': ((cx, -60, 6.0), (cx, 1.8, 5.2), total + 12), '34': ((cx - 26, -34, 16), (cx, 1.8, 4.5), total + 12)}
for vn, (pos, look, scale) in views.items():
    cam.location = pos; cam.rotation_euler = (Vector(look) - Vector(pos)).to_track_quat('-Z', 'Y').to_euler()
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = scale
    sc.render.filepath = str(out / f'{name}_{vn}.png'); bpy.ops.render.render(write_still=True)
(out / f'{name}_report.txt').write_text('\n'.join(report)); print('MINI182_SCENERY_DONE', report)
