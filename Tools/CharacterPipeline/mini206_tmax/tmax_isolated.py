import bpy,sys,os
from mathutils import Vector
sys.path.insert(0,os.path.dirname(__file__)); import rlib
bpy.ops.wm.open_mainfile(filepath=sys.argv[sys.argv.index('--')+1]); out=sys.argv[-1]
names=['TMAX_Body','TMAX_FrontWheel','TMAX_RearWheel','TMAX_FrontForkAssembly','TMAX_Handlebar']
cam=rlib.setup_render(480,400,10); sc=bpy.context.scene; cam.data.lens=50
for n in names:
    for o in bpy.data.objects:
        if o.type=='MESH': o.hide_render=(o.name!=n)
    o=bpy.data.objects[n]; co=[o.matrix_world@v.co for v in o.data.vertices]
    mn=Vector([min(c[i] for c in co) for i in range(3)]); mx=Vector([max(c[i] for c in co) for i in range(3)]); c=(mn+mx)/2; s=(mx-mn).length
    p=c+Vector((0.8,-0.75,0.45)).normalized()*s*1.6; cam.location=p; cam.rotation_euler=(c-p).to_track_quat('-Z','Y').to_euler()
    sc.render.filepath=f'{out}_{n}.png'; bpy.ops.render.render(write_still=True)
