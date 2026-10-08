"""Body deformation check on a plain FBX import: elbows 90, left knee 70 / hip 40, right shoulder raised. Clay + textured."""
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
S=float(os.environ.get('POSE_SIGN','1'))
def R(n,x=0,y=0,z=0): pb[n].rotation_euler=(math.radians(x),math.radians(y),math.radians(z))
R('CC_Base_L_Forearm',z=S*90); R('CC_Base_R_Forearm',z=-S*90)
R('CC_Base_L_Thigh',x=-S*40); R('CC_Base_L_Calf',x=S*70)
R('CC_Base_R_Upperarm',z=S*-45)
bpy.context.view_layer.update()
cam=rlib.setup_render(420,720,16)
for m in meshes: m.data.materials.clear(); m.data.materials.append(mat)
rlib.turntable(cam,0.93,1.95,out+'_tex',views=(('front',0),('side',90),('back',180),('tq',35)))
for m in meshes: m.data.materials[0]=clay
rlib.turntable(cam,0.93,1.95,out+'_clay',views=(('front',0),('tq',35)))
