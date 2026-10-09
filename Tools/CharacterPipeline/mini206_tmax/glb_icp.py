import numpy as np
from scipy.spatial import cKDTree
s=1.575/1.3515
A=np.load('tmax/profile_pts.npy').astype(np.float64)*s     # scan aligned frame, metres: x side, y fwd, z up, ground 0
G=np.load('tmax/glb_pts.npy').astype(np.float64)
rng=np.random.default_rng(1); As=A[rng.choice(len(A),150000,replace=False)]; T=cKDTree(As)
def umeyama(X,Y):
    mx,my=X.mean(0),Y.mean(0); Xc,Yc=X-mx,Y-my; U,D,Vt=np.linalg.svd(Yc.T@Xc/len(X)); S=np.eye(3)
    if np.linalg.det(U)*np.linalg.det(Vt)<0: S[2,2]=-1
    R=U@S@Vt; sc=np.trace(np.diag(D)@S)/Xc.var(0).sum(); t=my-sc*R@mx; return sc,R,t
best=None
for flip in (1,-1):
    Rf=np.diag([flip,flip,1.0])   # try both front directions
    X=G@Rf.T; sc=1.0; R=np.eye(3); t=As.mean(0)-X.mean(0)
    for it in range(40):
        Y=sc*X@R.T+t; d,i=T.query(Y); k=d<np.percentile(d,90)
        sc,R,t=umeyama(X[k],As[i[k]])
    Y=sc*X@R.T+t; d,_=T.query(Y); print('flip',flip,'scale',round(sc,4),'median mm',round(np.median(d)*1000,2),'p90 mm',round(np.percentile(d,90)*1000,2),'rot deg',np.round(np.degrees(np.arccos(np.clip((np.trace(R)-1)/2,-1,1))),3))
    if best is None or np.median(d)<best[0]: best=(np.median(d),sc,R@Rf,t)
_,sc,R,t=best; np.save('tmax/glb2scan.npy',{'s':sc,'R':R,'t':t},allow_pickle=True)
print('glb->scan(metres): scale',sc,'\nR',np.round(R,4),'\nt',np.round(t,4))
