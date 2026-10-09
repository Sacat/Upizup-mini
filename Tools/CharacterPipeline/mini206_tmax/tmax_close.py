import bpy,sys,os
from mathutils import Vector
sys.path.insert(0,os.path.dirname(__file__)); import rlib
a=sys.argv[sys.argv.index('--')+1:]; src,out=a[0],a[1]
bpy.ops.wm.open_mainfile(filepath=src)
cam=rlib.setup_render(800,600,12); sc=bpy.context.scene; cam.data.lens=50
shots={ 'cockpit_rear':((0,0.9,1.75),(0,-0.25,1.0)), 'cockpit_side':((1.3,-0.2,1.3),(0,-0.25,1.0)),
        'fork_front':((0.35,-2.0,0.55),(0,-0.75,0.45)), 'fork_side':((1.2,-0.8,0.5),(0,-0.75,0.45)),'fork_under':((0.9,-0.9,0.05),(0,-0.7,0.5)),
        'rear_left':((1.2,0.7,0.4),(0,0.65,0.35)),'rear_right':((-1.2,0.7,0.4),(0,0.65,0.35)),'seat_top':((0.9,0.3,1.9),(0,0.25,0.8))}
for n,(p,t) in shots.items():
    cam.location=p; cam.rotation_euler=(Vector(t)-Vector(p)).to_track_quat('-Z','Y').to_euler()
    sc.render.filepath=f'{out}_{n}.png'; bpy.ops.render.render(write_still=True)
