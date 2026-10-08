import bpy,sys,os,numpy as np
sys.path.insert(0,os.path.dirname(__file__)); import bodylib as B, rlib
arm,body=B.load(os.path.abspath('src/SacatRig.fbx'))
for m in body.modifiers: m.show_render=False
mt=bpy.data.materials.new('c'); mt.use_nodes=True; mt.node_tree.nodes['Principled BSDF'].inputs['Alpha'].default_value=0.35; mt.blend_method='BLEND'
body.data.materials.clear(); body.data.materials.append(mt)
red=bpy.data.materials.new('r'); red.diffuse_color=(1,0,0,1); red.use_nodes=True; red.node_tree.nodes['Principled BSDF'].inputs[0].default_value=(1,0.05,0.05,1)
cols={'Thumb':(1,0.1,0.1,1),'Index':(1,0.6,0,1),'Mid':(0.1,0.7,0.1,1),'Ring':(0.1,0.3,1,1),'Pinky':(0.7,0.1,0.9,1)}
from mathutils import Vector
hand=Vector(B.bone_world(arm,'CC_Base_L_Hand',body))
for f,c in cols.items():
    mm=bpy.data.materials.new(f); mm.use_nodes=True; mm.node_tree.nodes['Principled BSDF'].inputs[0].default_value=c
    pts=[hand]+[Vector(B.bone_world(arm,f'CC_Base_L_{f}{i}',body)) for i in (1,2,3)]
    for a,b in zip(pts[:-1],pts[1:]):
        d=b-a; bpy.ops.mesh.primitive_cylinder_add(radius=0.0018,depth=d.length,location=(a+b)/2); o=bpy.context.object
        o.rotation_mode='QUATERNION'; o.rotation_quaternion=d.to_track_quat('Z','Y'); o.data.materials.append(mm)
    for p in pts[1:]:
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.0032,location=p); bpy.context.object.data.materials.append(mm)
p=B.bone_world(arm,'CC_Base_L_Hand',body); print('hand',p.round(3))
W=B.world_co(body); h=W[W[:,0]>0.74]; print('hand mesh x max',h[:,0].max().round(3),'y',h[:,1].min().round(3),h[:,1].max().round(3),'z',h[:,2].min().round(3),h[:,2].max().round(3))
cam=rlib.setup_render(600,400,12); sc=bpy.context.scene
for nm,loc,rot in (('top',(0.83,0.015,2.05),(0,0,0)),('palm',(0.83,0.015,0.87),(np.pi,0,0)),('front',(0.83,-0.58,1.46),(np.pi/2,0,0))):
    cam.location=loc; cam.rotation_euler=rot; cam.data.lens=85; sc.render.filepath=f'out/handbones_{nm}.png'; bpy.ops.render.render(write_still=True)
