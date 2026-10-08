"""Exact horizontal cross-sections (edge/plane intersections) of hips and legs: per side outer/inner x, front/back y,
and the convex-hull perimeter of each thigh. Compare donor vs candidate."""
import bpy,sys,os,numpy as np
sys.path.insert(0,os.path.dirname(__file__)); import bodylib as B
from scipy.spatial import ConvexHull
def load(p):
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=os.path.abspath(p),use_anim=False); bpy.context.view_layer.update()
    segs=[]
    for o in bpy.context.scene.objects:
        if o.type!='MESH' or not o.data.vertices: continue
        W=B.world_co(o); e=np.zeros(len(o.data.edges)*2,int); o.data.edges.foreach_get('vertices',e); e=e.reshape(-1,2); segs.append((W[e[:,0]],W[e[:,1]]))
    return np.vstack([a for a,b in segs]),np.vstack([b for a,b in segs])
def section(P0,P1,z):
    a,b=P0[:,2]-z,P1[:,2]-z; m=(a*b)<0; t=a[m]/(a[m]-b[m]); return P0[m]+(P1[m]-P0[m])*t[:,None]
rows={}
for p in sys.argv[-2:]:
    P0,P1=load(p); name=os.path.basename(p); rows[name]={}
    for z in (0.98,0.94,0.90,0.86,0.82,0.78,0.74,0.70,0.62,0.52):
        s=section(P0,P1,z); s=s[np.abs(s[:,0])<0.30]
        out=[]
        for sg in (1,-1):
            q=s[s[:,0]*sg>0.004]
            per=ConvexHull(q[:,:2]).area if len(q)>5 else 0
            out.append((np.abs(q[:,0]).max(),np.abs(q[:,0]).min(),q[:,1].min(),q[:,1].max(),per))
        rows[name][z]=out
names=list(rows)
print('z     | side | outer x   inner x   front y   back y   thigh perim   (donor -> new, mm diff)')
for z in rows[names[0]]:
    for k,sd in enumerate('LR'):
        a=rows[names[0]][z][k]; b=rows[names[1]][z][k]
        print(f'{z:.2f} | {sd} | '+'  '.join(f'{a[i]:.3f}->{b[i]:.3f}({1000*(b[i]-a[i]):+.0f})' for i in range(5)))
