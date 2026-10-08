"""Re-import an exported bare-body FBX next to the donor and compare bone heads, bounds, weights and a bent-elbow/knee pose."""
import bpy,sys,os,numpy as np,json
sys.path.insert(0,os.path.dirname(__file__)); import bodylib as B
from scipy.spatial import cKDTree
donor_fbx,new_fbx=[os.path.abspath(p) for p in sys.argv[-2:]]
def load(p):
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=p,use_anim=False); bpy.context.view_layer.update()
    return [o for o in bpy.context.scene.objects if o.type=='ARMATURE'][0],[o for o in bpy.context.scene.objects if o.type=='MESH']
def posed(arm,meshes):
    arm.data.pose_position='POSE'
    for b in arm.pose.bones: b.rotation_mode='XYZ'; b.rotation_euler=(0,0,0); b.location=(0,0,0); b.scale=(1,1,1)
    for n in ('CC_Base_L_Forearm','CC_Base_L_Calf'): arm.pose.bones[n].rotation_euler=(np.radians(60),0,0)
    bpy.context.view_layer.update(); dg=bpy.context.evaluated_depsgraph_get(); out=[]
    for m in meshes:
        e=m.evaluated_get(dg); me=e.to_mesh(); out.append(np.array([e.matrix_world@v.co for v in me.vertices])); e.to_mesh_clear()
    return np.vstack(out)
res={}
arm,meshes=load(donor_fbx); bd={b.name:np.array(arm.matrix_world@b.head_local) for b in arm.data.bones}
Wd=np.vstack([B.world_co(m) for m in meshes]); Pd=posed(arm,meshes)
arm,meshes=load(new_fbx); bn={b.name:np.array(arm.matrix_world@b.head_local) for b in arm.data.bones}
res['bones']=len(bn); res['bones_missing']=sorted(set(bd)-set(bn))
res['bone_max_offset_mm']=round(float(max(np.linalg.norm(bd[k]-bn[k]) for k in bd if k in bn)*1000),3)
Wn=np.vstack([B.world_co(m) for m in meshes])
res['height_donor_new']=[round(float(Wd[:,2].max()-Wd[:,2].min()),4),round(float(Wn[:,2].max()-Wn[:,2].min()),4)]
res['rest_dist_to_donor_mm_p50_p99']=[round(float(x)*1000,1) for x in np.percentile(cKDTree(Wd).query(Wn)[0],[50,99])]
res['meshes']={m.name:{'tris':sum(len(p.vertices)-2 for p in m.data.polygons),
   'max_influences':max(len([g for g in v.groups if g.weight>1e-4]) for v in m.data.vertices),
   'unweighted':sum(1 for v in m.data.vertices if not any(g.weight>1e-4 for g in v.groups))} for m in meshes}
res['total_tris']=sum(v['tris'] for v in res['meshes'].values())
Pn=posed(arm,meshes); d=cKDTree(Pd).query(Pn)[0]
res['posed_dist_to_donor_mm_p50_p99']=[round(float(x)*1000,1) for x in np.percentile(d,[50,99])]
print('VERIFY '+json.dumps(res,indent=1))
