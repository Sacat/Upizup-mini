import numpy as np
A=np.load('tmax/hp_pts.npy').astype(np.float64); C=np.load('tmax/vcol.npy')
for name,yc,zc,R in (('front',-0.7875,0.3042,0.3042),('rear',0.7875,0.2537,0.2537)):
    d=np.hypot(A[:,1]-yc,A[:,2]-zc)
    # tread band: r within 2 cm of R; plane orientation by PCA of x vs (y-yc)
    m=(d>R-0.02)&(d<R+0.005)&(np.abs(A[:,0])<0.12)
    q=A[m]; slope=np.polyfit(q[:,1]-yc,q[:,0],1)[0]; print(name,'tyre plane yaw deg',np.degrees(np.arctan(slope)).round(2),'n',m.sum())
    # radial profile: max |x| (tyre half width) vs radius
    for r0 in np.arange(R-0.12,R+0.001,0.01):
        s=(d>r0-0.005)&(d<r0+0.005)&(np.abs(A[:,0])<0.16)
        ang=np.arctan2(A[:,2]-zc,A[:,1]-yc)
        s2=s&(ang<-0.3)&(ang>-2.8)   # lower half: away from fender/swingarm
        if s2.sum(): print('  r %.3f  x %.3f..%.3f  n %d  col %s'%(r0,np.percentile(A[s2,0],1),np.percentile(A[s2,0],99),s2.sum(),np.round(C[s2].mean(0),2)))
