import bpy,sys,numpy as np
from mathutils import Matrix
bpy.ops.wm.open_mainfile(filepath='tmax/scan_raw_applied.blend')
o=[x for x in bpy.data.objects if x.type=='MESH'][0]; o.name='TMAX_Scan_HP'; o.data.name='TMAX_Scan_HP'
d=np.load('tmax/align2.npy',allow_pickle=True).item(); s=1.575/1.3515
# m = s*((P-c)@R.T - [dx,0,zoff]) ; final = diag(-1,-1,1) @ (m + [0,0.0198,0])
R,c,dx,zoff=d['R'],d['c'],d['dx'],d['zoff']
M=np.eye(4); M[:3,:3]=s*R; M[:3,3]=s*(-R@c-np.array([dx,0,zoff]))
F=np.diag([-1.0,-1.0,1.0,1.0]); T=np.eye(4); T[1,3]=0.0198
W=F@T@M
o.data.transform(Matrix(W.tolist())); o.data.update()
co=np.empty(len(o.data.vertices)*3,np.float32); o.data.vertices.foreach_get('co',co); co=co.reshape(-1,3)
print('FINAL bbox',co.min(0),co.max(0))
np.save('tmax/hp_pts.npy',co)
bpy.ops.wm.save_as_mainfile(filepath='tmax/scan_final.blend',compress=False)
