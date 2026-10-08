"""Exact torso sections (edge/plane cuts), arms excluded by |x|<0.24: width, front y, back y, hull perimeter. Donor vs candidate."""
import bpy,sys,os,numpy as np
sys.path.insert(0,os.path.dirname(__file__)); import bodylib as B
from scipy.spatial import ConvexHull
def load(p):
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=os.path.abspath(p),use_anim=False); bpy.context.view_layer.update()
    a=[];b=[]
    for o in bpy.context.scene.objects:
        if o.type!='MESH' or not o.data.vertices: continue
        W=B.world_co(o); e=np.zeros(len(o.data.edges)*2,int); o.data.edges.foreach_get('vertices',e); e=e.reshape(-1,2); a.append(W[e[:,0]]); b.append(W[e[:,1]])
    return np.vstack(a),np.vstack(b)
def section(P0,P1,z):
    a,b=P0[:,2]-z,P1[:,2]-z; m=(a*b)<0; t=a[m]/(a[m]-b[m]); return P0[m]+(P1[m]-P0[m])*t[:,None]
R={}
for p in sys.argv[-2:]:
    P0,P1=load(p); R[p]={}
    for z in (1.00,1.05,1.10,1.15,1.20,1.25,1.30,1.35,1.40,1.45,1.50,1.55):
        s=section(P0,P1,z); lim=0.24 if z<1.42 else 0.16; s=s[np.abs(s[:,0])<lim]
        R[p][z]=(s[:,0].max()-s[:,0].min(),s[:,1].min(),s[:,1].max(),ConvexHull(s[:,:2]).area)
a,b=list(R)
print('z    | width              front y              back y             perimeter   (donor->new, mm)')
for z in R[a]:
    x=R[a][z]; y=R[b][z]; print(f'{z:.2f} | '+'  '.join(f'{x[i]:.3f}->{y[i]:.3f}({1000*(y[i]-x[i]):+.0f})' for i in range(4)))
