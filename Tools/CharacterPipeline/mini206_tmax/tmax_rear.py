import numpy as np,sys
from scipy.spatial import ConvexHull
A=np.load('tmax/profile_pts.npy').astype(np.float64); d=np.load('tmax/align2.npy',allow_pickle=True).item()
def fit(pts):
    x,y=pts[:,0],pts[:,1]; M=np.c_[2*x,2*y,np.ones(len(x))]; s=np.linalg.lstsq(M,x**2+y**2,rcond=None)[0]; return s[0],s[1],np.sqrt(s[2]+s[0]**2+s[1]**2)
for name,(y0,y1) in (('rear',(-1.02,-0.42)),('front',(0.38,0.95))):
    for xw in (0.05,0.09):
        m=(np.abs(A[:,0])<xw)&(A[:,1]>y0)&(A[:,1]<y1)&(A[:,2]<0.24)
        Q=A[m][:,1:]; H=Q[ConvexHull(Q).vertices]
        cx,cy,r=fit(H)
        for _ in range(4):
            dd=np.abs(np.hypot(H[:,0]-cx,H[:,1]-cy)-r); k=dd<0.006; cx,cy,r=fit(H[k])
        print(name,'xw',xw,'centre y %.4f z %.4f r %.4f'%(cx,cy,r),'n',k.sum(),'resid mm %.2f'%(1000*np.abs(np.hypot(H[k,0]-cx,H[k,1]-cy)-r).mean()))
    # tyre width at the centre height
    m=(np.abs(A[:,1]-cx)<0.03)&(np.abs(A[:,2]-(cy-r+0.03))<0.03); print('  tyre width near the bottom',np.percentile(A[m,0],[0.5,99.5]))
    m=(np.abs(A[:,2]-cy)<0.015)&(np.abs(A[:,1]-(cx-r+0.01))<0.012); print('  width at the trailing/leading side',np.percentile(A[m,0],[0.5,99.5]))
