"""Head turn/tilt check: CC_Base_Head +-35 deg yaw and 20 deg nod; textured neck close-up (plain FBX import)."""
import sys,os,math; sys.path.insert(0,os.path.dirname(__file__)); import bpy,rlib
fbx,tex,out=[os.path.abspath(p) for p in sys.argv[-3:]]
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=fbx)
arm=[o for o in bpy.context.scene.objects if o.type=='ARMATURE'][0]; arm.data.pose_position='POSE'
img=bpy.data.images.load(tex); mat=bpy.data.materials.new('s'); mat.use_nodes=True; nt=mat.node_tree
t=nt.nodes.new('ShaderNodeTexImage'); t.image=img; nt.links.new(t.outputs[0],nt.nodes['Principled BSDF'].inputs[0])
hm=bpy.data.materials.new('h'); hm.use_nodes=True; hm.node_tree.nodes['Principled BSDF'].inputs[0].default_value=(0.02,0.02,0.022,1)
for o in bpy.context.scene.objects:
    if o.type=='MESH': o.data.materials.clear(); o.data.materials.append(hm if 'Hair' in o.name else mat)
cam=rlib.setup_render(360,420,16); pb=arm.pose.bones
for b in pb: b.rotation_mode='XYZ'
for tag,rot in (('yawL',(0,35,0)),('yawR',(0,-35,0)),('nod',(20,0,0))):
    for b in pb: b.rotation_euler=(0,0,0)
    pb['CC_Base_Head'].rotation_euler=tuple(math.radians(a) for a in rot); pb['CC_Base_NeckTwist02'].rotation_euler=tuple(math.radians(a*0.4) for a in rot)
    bpy.context.view_layer.update(); rlib.closeup(cam,f'{out}_{tag}',(0,0,1.64),0.42,views=(('tq',30),))
