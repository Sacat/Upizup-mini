import bpy,sys,os,math,numpy as np,json
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
sys.path.insert(0,os.path.dirname(__file__)); import rlib
a=sys.argv[sys.argv.index('--')+1:]; src,out=a[0],a[1]; steers=[float(x) for x in a[2].split(',')]; views=a[3].split(','); spin=float(a[4]) if len(a)>4 else 0
RAKE=math.radians(25); AX=Vector((0,math.sin(RAKE),math.cos(RAKE)))
bpy.ops.wm.open_mainfile(filepath=src)
for _o in bpy.data.objects:
    if 'LOD1' in _o.name: _o.hide_render=True
O=bpy.data.objects; body=O['TMAX_Body']; fk=O['TMAX_FrontForkAssembly']; fw=O['TMAX_FrontWheel']; rw=O['TMAX_RearWheel']; hb=O['TMAX_Handlebar']
dg=bpy.context.evaluated_depsgraph_get()
bvh=BVHTree.FromObject(body,dg)
def inside(p):
    n=0; o=Vector(p); d=Vector((0.0123,0.0071,1.0)).normalized()
    while True:
        h=bvh.ray_cast(o,d,5)
        if h[0] is None: break
        n+=1; o=h[0]+d*1e-4
    return n%2==1
base={o.name:o.matrix_world.copy() for o in (fk,fw,rw,hb)}
def rot_about(p,axis,deg): return Matrix.Translation(p)@Matrix.Rotation(math.radians(deg),4,axis)@Matrix.Translation(-p)
cam=rlib.setup_render(640,480,10); sc=bpy.context.scene; cam.data.lens=50
VIEWS={'tqf':((2.2,-2.6,1.5),(0,-0.5,0.6)),'top':((0.0,-0.55,3.2),(0,-0.55,0.6)),'front':((0,-3.6,0.9),(0,-0.6,0.7)),'cockpit':((0,1.2,1.9),(0,-0.2,1.0)),'left':((3.4,0,0.7),(0,0,0.62))}
rep={}
for st in steers:
    R=rot_about(fk.location.copy(),AX,st)
    fk.matrix_world=R@base[fk.name]; fw.matrix_world=R@base[fw.name]@Matrix.Rotation(math.radians(spin),4,'X')
    rw.matrix_world=base[rw.name]@Matrix.Rotation(math.radians(spin),4,'X')
    hb.matrix_world=rot_about(hb.location.copy(),AX,st)@base[hb.name]
    bpy.context.view_layer.update()
    res={}
    for o in (fk,fw,hb):
        V=[o.matrix_world@v.co for v in o.data.vertices][::3]
        res[o.name]=int(sum(inside(v) for v in V)); 
    rep[st]=res; print('STEER',st,res)
    for v in views:
        p,t=VIEWS[v]; cam.location=p; cam.rotation_euler=(Vector(t)-Vector(p)).to_track_quat('-Z','Y').to_euler()
        sc.render.filepath=f'{out}_s{int(st)}_{v}.png'; bpy.ops.render.render(write_still=True)
json.dump(rep,open(out+'_inside.json','w'))
