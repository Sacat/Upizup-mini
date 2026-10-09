import bpy,sys,numpy as np
import matplotlib; matplotlib.use('Agg'); import matplotlib.pyplot as plt
bpy.ops.wm.open_mainfile(filepath=sys.argv[sys.argv.index('--')+1])
o=[x for x in bpy.data.objects if x.type=='MESH'][0]
co=np.empty(len(o.data.vertices)*3,np.float32); o.data.vertices.foreach_get('co',co); P=co.reshape(-1,3).astype(np.float64)
d=np.load(sys.argv[-2],allow_pickle=True).item()
A=(P-d['c'])@d['R'].T; A[:,0]-=d['dx']; A[:,2]-=d['zoff']
np.save(sys.argv[-1].replace('.png','_pts.npy'),A.astype(np.float32))
fig,ax=plt.subplots(1,2,figsize=(22,9))
for k,(sel,t) in enumerate([(np.abs(A[:,0])<0.04,'centre slice |x|<4cm'),(np.ones(len(A),bool),'all (colour=x)')]):
    B=A[sel][::(1 if k==0 else 6)]
    ax[k].scatter(B[:,1],B[:,2],s=0.05,c=B[:,0],cmap='coolwarm'); ax[k].set_aspect('equal'); ax[k].set_title(t); ax[k].grid(True,alpha=.3)
    for w in d['wheels'].values():
        ax[k].add_patch(plt.Circle((w['y'],w['z']),w['r'],fill=False,color='g'))
plt.tight_layout(); plt.savefig(sys.argv[-1],dpi=70)
