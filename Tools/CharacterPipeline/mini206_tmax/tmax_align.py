import bpy,sys,numpy as np
from scipy.spatial import cKDTree
from scipy.optimize import minimize
bpy.ops.wm.open_mainfile(filepath=sys.argv[sys.argv.index('--')+1])
o=[x for x in bpy.data.objects if x.type=='MESH'][0]
co=np.empty(len(o.data.vertices)*3,np.float32); o.data.vertices.foreach_get('co',co); P=co.reshape(-1,3).astype(np.float64)
rng=np.random.default_rng(0); S=P[rng.choice(len(P),60000,replace=False)]
c=S.mean(0); X=S-c
w,v=np.linalg.eigh(np.cov(X.T)); print('PCA eig',w,'\n',v)
L=v[:,2]   # longest axis
# vertical guess = world Z made orthogonal to L
up=np.array([0,0,1.0]); up-=L*np.dot(up,L); up/=np.linalg.norm(up); side=np.cross(L,up)
R0=np.vstack([side,L,up])   # rows: new x (side), new y (length), new z (up)
def rot(a,b,g):
    ca,sa,cb,sb,cg,sg=np.cos(a),np.sin(a),np.cos(b),np.sin(b),np.cos(g),np.sin(g)
    Rx=np.array([[1,0,0],[0,ca,-sa],[0,sa,ca]]); Ry=np.array([[cb,0,sb],[0,1,0],[-sb,0,cb]]); Rz=np.array([[cg,-sg,0],[sg,cg,0],[0,0,1]])
    return Rz@Ry@Rx
Q=X@R0.T; T=cKDTree(Q[::3])
def cost(p):
    R=rot(0,p[0],p[1]); A=Q@R.T; A[:,0]-=p[2]
    M=A.copy(); M[:,0]*=-1
    d,_=cKDTree(A).query(M[::4]); return np.mean(np.minimum(d,0.03)**2)
best=None
for g0 in (-0.05,0,0.05):
    r=minimize(cost,[0,g0,0],method='Nelder-Mead',options={'xatol':1e-4,'fatol':1e-9,'maxiter':400})
    if best is None or r.fun<best.fun: best=r
print('sym fit roll,yaw,dx',best.x,'rms mm',np.sqrt(best.fun)*1000)
R=rot(0,best.x[0],best.x[1])@R0
A=(P-c)@R.T; A[:,0]-=best.x[2]
mn,mx=A.min(0),A.max(0); print('aligned bbox',mn,mx,'size',mx-mn)
np.save(sys.argv[-1],{'R':R,'c':c,'dx':best.x[2]},allow_pickle=True)
# low points along length: find ground contacts
yb=np.linspace(mn[1],mx[1],40); 
for i in range(39):
    m=(A[:,1]>=yb[i])&(A[:,1]<yb[i+1])
    if m.any(): print(round(yb[i],3),round(A[m,2].min(),3),round(A[m,2].max(),3))
