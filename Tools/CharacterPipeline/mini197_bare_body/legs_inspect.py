"""Leg inspection on a plain FBX import: rest + a natural walk/knee-bend pose, close framing, textured + clay."""
import bpy,sys,os,math
sys.path.insert(0,os.path.dirname(__file__)); import rlib
fbx,tex,out=[os.path.abspath(p) for p in sys.argv[-3:]]
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=fbx,use_anim=False); bpy.context.view_layer.update()
arm=[o for o in bpy.context.scene.objects if o.type=='ARMATURE'][0]; arm.data.pose_position='POSE'
img=bpy.data.images.load(tex); mat=bpy.data.materials.new('s'); mat.use_nodes=True; nt=mat.node_tree
t=nt.nodes.new('ShaderNodeTexImage'); t.image=img; nt.links.new(t.outputs[0],nt.nodes['Principled BSDF'].inputs[0])
clay=bpy.data.materials.new('c'); clay.diffuse_color=(0.62,0.62,0.64,1)
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
pb=arm.pose.bones
for b in pb: b.rotation_mode='XYZ'
print('L_Thigh axes (world): x',(arm.matrix_world.to_3x3()@pb['CC_Base_L_Thigh'].bone.matrix_local.to_3x3().col[0]).normalized(),
      'y',(arm.matrix_world.to_3x3()@pb['CC_Base_L_Thigh'].bone.matrix_local.to_3x3().col[1]).normalized())
cam=rlib.setup_render(420,560,16)
def shoot(tag):
    for m in meshes: m.data.materials.clear(); m.data.materials.append(mat)
    rlib.closeup(cam,f'{out}_{tag}_tex',(0,0,0.55),1.25,views=(('front',0),('side',90),('back',180),('tq',35)))
    for m in meshes: m.data.materials[0]=clay
    rlib.closeup(cam,f'{out}_{tag}_clay',(0,0,0.55),1.25,views=(('front',0),('side',90),('back',180)))
shoot('rest')
# natural stride: thigh local X points to world -X, so a NEGATIVE angle flexes the hip forward
pb['CC_Base_L_Thigh'].rotation_euler=(math.radians(-35),0,0); pb['CC_Base_L_Calf'].rotation_euler=(math.radians(55),0,0)
pb['CC_Base_R_Thigh'].rotation_euler=(math.radians(15),0,0); pb['CC_Base_R_Calf'].rotation_euler=(math.radians(15),0,0)
bpy.context.view_layer.update(); shoot('bend')
