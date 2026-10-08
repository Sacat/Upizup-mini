import bpy,sys,os,numpy as np
sys.path.insert(0,os.path.dirname(__file__)); import bodylib as B
from scipy.spatial import ConvexHull
def sect(objs,z):
    pts=[]
    for o in objs:
        dg=bpy.context.evaluated_depsgraph_get(); e=o.evaluated_get(dg); me=e.to_mesh()
        W=np.array([e.matrix_world@v.co for v in me.vertices]); E=np.array([ed.vertices[:] for ed in me.edges]); e.to_mesh_clear()
        a,b=W[E[:,0]],W[E[:,1]]; da,db=a[:,2]-z,b[:,2]-z; m=da*db<0; t=da[m]/(da[m]-db[m]); pts.append(a[m]+(b[m]-a[m])*t[:,None])
    p=np.vstack(pts); p=p[np.abs(p[:,0])<0.12]; c=p[:,:2].mean(0)
    return ConvexHull(p[:,:2]).area, c, p[:,0].max()-p[:,0].min(), p[:,1].max()-p[:,1].min()
for f in sys.argv[-2:]:
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=f,use_anim=False); bpy.context.view_layer.update()
    arm=[o for o in bpy.context.scene.objects if o.type=='ARMATURE'][0]; arm.data.pose_position='REST'; bpy.context.view_layer.update()
    body=[o for o in bpy.context.scene.objects if o.type=='MESH' and ('Body' in o.name or 'Head' in o.name or 'Torso' in o.name)]
    print('==',os.path.basename(f),[o.name for o in body])
    for b in arm.data.bones:
        if any(k in b.name for k in ('Head','Neck','Spine2','NeckTwist')) and 'Top' not in b.name: print('  bone',b.name,np.array(arm.matrix_world@b.head_local).round(3))
    for z in (1.40,1.44,1.46,1.48,1.50,1.52,1.54,1.58,1.62,1.66):
        try: per,c,w,d=sect(body,z); print(f'  z={z} perim={per:.3f} centre={c.round(3)} w={w:.3f} d={d:.3f}')
        except Exception as ex: print('  z',z,'fail',ex)
