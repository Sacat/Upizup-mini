import bpy,sys,os,math,numpy as np
sys.path.insert(0,os.path.dirname(__file__)); import rlib
a=sys.argv[sys.argv.index('--')+1:]; src,out=a[0],a[1]
bpy.ops.wm.open_mainfile(filepath=src)
o=[x for x in bpy.data.objects if x.type=='MESH'][0]
if o.parent:
    M=o.matrix_world.copy(); o.parent=None; o.matrix_world=M
    bpy.data.objects.remove(bpy.data.objects['world'])
    bpy.context.view_layer.objects.active=o; o.select_set(True)
    o.scale=tuple(s*100 for s in o.scale); o.location=(0,0,0)
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    bpy.ops.wm.save_as_mainfile(filepath=src.replace('.blend','_applied.blend'))
co=np.empty(len(o.data.vertices)*3,np.float32); o.data.vertices.foreach_get('co',co); co=co.reshape(-1,3)
mn,mx=co.min(0),co.max(0); c=(mn+mx)/2; print('BBOX',mn,mx,'size',mx-mn)
cam=rlib.setup_render(640,480,8)
sc=bpy.context.scene; cam.data.type='ORTHO'; cam.data.ortho_scale=float(max(mx-mn))*1.15
views={'px':(1,0,0),'nx':(-1,0,0),'py':(0,1,0),'ny':(0,-1,0),'pz':(0,0,1),'nz':(0,0,-1)}
for n,d in views.items():
    d=np.array(d,float); p=c+d*5
    cam.location=tuple(p)
    from mathutils import Vector
    cam.rotation_euler=(Vector(tuple(-d))).to_track_quat('-Z','Y' if abs(d[2])<0.9 else 'Y').to_euler()
    sc.render.filepath=f'{out}_{n}.png'; bpy.ops.render.render(write_still=True)
