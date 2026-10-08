import bpy,sys,os,math,numpy as np
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=os.path.abspath(sys.argv[-1]),use_anim=False); bpy.context.view_layer.update()
arm=[o for o in bpy.context.scene.objects if o.type=='ARMATURE'][0]; arm.data.pose_position='POSE'; pb=arm.pose.bones
for b in pb: b.rotation_mode='XYZ'
def J(n): return np.array(arm.matrix_world@pb[n].head)
for ax in ('x','y','z'):
  for th,ca in ((-35,55),(-35,-55),(35,55),(35,-55)):
    for b in pb: b.rotation_euler=(0,0,0)
    v=[0,0,0]; v['xyz'.index(ax)]=math.radians(th); pb['CC_Base_L_Thigh'].rotation_euler=v
    v=[0,0,0]; v['xyz'.index(ax)]=math.radians(ca); pb['CC_Base_L_Calf'].rotation_euler=v
    bpy.context.view_layer.update()
    h,k,a=J('CC_Base_L_Thigh'),J('CC_Base_L_Calf'),J('CC_Base_L_Foot')
    # front is -Y. hip flexion: knee moves to -Y. correct knee bend: ankle behind the knee (+Y relative)
    print(f'axis {ax} thigh {th:+} calf {ca:+}: knee dy={1000*(k[1]-h[1]):+.0f}mm dx={1000*(k[0]-h[0]):+.0f} | ankle-knee dy={1000*(a[1]-k[1]):+.0f} dx={1000*(a[0]-k[0]):+.0f} dz={1000*(a[2]-k[2]):+.0f}')
