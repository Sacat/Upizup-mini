import bpy,sys,os,math
from mathutils import Vector
sys.path.insert(0,os.path.dirname(__file__)); import rlib
a=sys.argv[sys.argv.index('--')+1:]; src,out=a[0],a[1]; W=int(a[2]) if len(a)>2 else 900
bpy.ops.wm.open_mainfile(filepath=src)
for _o in bpy.data.objects:
    if 'LOD1' in _o.name: _o.hide_render=True
hide=a[3].split(',') if len(a)>3 and a[3] else []
for o in bpy.data.objects:
    if any(o.name.startswith(h) for h in hide): o.hide_render=True
cam=rlib.setup_render(W,int(W*0.62),12); sc=bpy.context.scene; cam.data.lens=50
tgt=Vector((0,0,0.62)); D=4.6
views={'left':(1,0,0.08),'right':(-1,0,0.08),'front':(0,-1,0.12),'back':(0,1,0.12),'tq_front':(0.75,-0.75,0.35),'tq_rear':(-0.75,0.75,0.35),'top':(0.001,0.0,1)}
sel=a[4].split(',') if len(a)>4 else list(views)
for n in sel:
    d=Vector(views[n]).normalized(); cam.location=tgt+d*D
    cam.rotation_euler=(tgt-cam.location).to_track_quat('-Z','Y').to_euler()
    sc.render.filepath=f'{out}_{n}.png'; bpy.ops.render.render(write_still=True)
