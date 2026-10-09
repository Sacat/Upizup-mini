import bpy,sys,os,json
from mathutils import Vector
sys.path.insert(0,os.path.dirname(__file__)); import rlib
a=sys.argv[sys.argv.index('--')+1:]; src,out,spec=a[0],a[1],json.loads(a[2])
bpy.ops.wm.open_mainfile(filepath=src)
hide=spec.get('hide',[])
for o in bpy.data.objects:
    if o.type=='MESH' and any(o.name.startswith(h) for h in hide): o.hide_render=True
cam=rlib.setup_render(spec.get('w',800),spec.get('h',600),spec.get('samples',12)); sc=bpy.context.scene; cam.data.lens=spec.get('lens',50)
if spec.get('ortho'): cam.data.type='ORTHO'; cam.data.ortho_scale=spec['ortho']
for n,(p,t) in spec['shots'].items():
    cam.location=p; cam.rotation_euler=(Vector(t)-Vector(p)).to_track_quat('-Z','Y').to_euler()
    sc.render.filepath=f'{out}_{n}.png'; bpy.ops.render.render(write_still=True)
