"""Pose test on the exported FBX: fist and pistol grip on both hands, rendered textured from the back and palm side."""
import bpy,sys,os,math,numpy as np
sys.path.insert(0,os.path.dirname(__file__)); import rlib
fbx,tex,out=[os.path.abspath(p) for p in sys.argv[-3:]]
if fbx.endswith('.blend'):
    bpy.ops.wm.open_mainfile(filepath=fbx)
    for o in list(bpy.context.scene.objects):
        if o.type in ('LIGHT','CAMERA') or (o.type=='MESH' and o.hide_render): bpy.data.objects.remove(o)
else:
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=fbx,use_anim=False)
bpy.context.view_layer.update()
arm=[o for o in bpy.context.scene.objects if o.type=='ARMATURE'][0]
img=bpy.data.images.load(tex); mat=bpy.data.materials.new('s'); mat.use_nodes=True; nt=mat.node_tree
t=nt.nodes.new('ShaderNodeTexImage'); t.image=img; nt.links.new(t.outputs[0],nt.nodes['Principled BSDF'].inputs[0])
for o in bpy.context.scene.objects:
    if o.type=='MESH': o.data.materials.clear(); o.data.materials.append(mat)
arm.data.pose_position='POSE'
def pose(curl,thumb,index_straight=False,sign=1):
    for b in arm.pose.bones: b.rotation_mode='XYZ'; b.rotation_euler=(0,0,0)
    for side in 'LR':
        for f in ('Index','Mid','Ring','Pinky'):
            for i in (1,2,3):
                a=0 if (index_straight and f=='Index') else curl[i-1]
                arm.pose.bones[f'CC_Base_{side}_{f}{i}'].rotation_euler=(math.radians(a*sign),0,0)
        for i,a in zip((1,2,3),thumb): arm.pose.bones[f'CC_Base_{side}_Thumb{i}'].rotation_euler=(math.radians(a*sign),0,0)
    bpy.context.view_layer.update()
import numpy as np
def spread():
    dg=bpy.context.evaluated_depsgraph_get(); mx=0
    for o in bpy.context.scene.objects:
        if o.type!='MESH' or 'Hands' not in o.name: continue
        e=o.evaluated_get(dg); me=e.to_mesh(); co=np.array([e.matrix_world@v.co for v in me.vertices]); e.to_mesh_clear()
        print('  ',o.name,'bbox',co.min(0).round(3),co.max(0).round(3))
cam=rlib.setup_render(420,360,16); sc=bpy.context.scene; cam.data.lens=85
sign=float(os.environ.get('CURL_SIGN','1'))
for name,args in (('rest',dict(curl=(0,0,0),thumb=(0,0,0))),('fist',dict(curl=(75,90,60),thumb=(10,25,30))),('pistol',dict(curl=(75,90,60),thumb=(0,0,0),index_straight=True))):
    pose(sign=sign,**args); print(name); spread()
    for side,sg in (('L',1),('R',-1)):
        for vn,loc,rot in (('front',(0.84*sg,-0.55,1.45),(math.pi/2,0,0)),('palm',(0.84*sg,0.015,0.85),(math.pi,0,0))):
            cam.location=loc; cam.rotation_euler=rot; sc.render.filepath=f'{out}_{name}_{side}_{vn}.png'; bpy.ops.render.render(write_still=True)
