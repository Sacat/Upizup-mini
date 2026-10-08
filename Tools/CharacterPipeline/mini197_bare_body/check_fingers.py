import bpy,sys,os,numpy as np
sys.path.insert(0,os.path.dirname(__file__)); import bodylib as B, hands as H
for f in (sys.argv[-2],sys.argv[-1]):
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=os.path.abspath(f),use_anim=False); bpy.context.view_layer.update()
    arm=[o for o in bpy.context.scene.objects if o.type=='ARMATURE'][0]; body=[o for o in bpy.context.scene.objects if o.type=='MESH'][0]
    print(f, 'body mw scale', np.round(np.array(body.matrix_world)[0,0],4), 'parent',body.parent.name if body.parent else None)
    for n in ('CC_Base_L_Hand','CC_Base_L_Mid1','CC_Base_L_Mid2','CC_Base_L_Mid3','CC_Base_L_Thumb1','CC_Base_L_Forearm'):
        b=arm.data.bones[n]; print('  ',n,'head',B.bone_world(arm,n,body).round(4),'tail',np.array(body.matrix_world@b.tail_local).round(4), 'parent',b.parent.name if b.parent else None)
    if 'legs' not in f:
        J=H.design(lambda n:B.bone_world(arm,n,body),'L'); print('  design mid',[p.round(4) for p in J['Mid']])
