import numpy as np
A=np.load('tmax/hp_pts.npy').astype(np.float64)
m=(A[:,1]>-0.40)&(A[:,1]<0.10)&(A[:,2]>0.85)&(A[:,2]<1.25)
B=A[m]
print('x bins: z range / y range of points (box y -0.40..0.10, z 0.85..1.25)')
for x0 in np.arange(-0.46,0.46,0.04):
    q=B[(B[:,0]>=x0)&(B[:,0]<x0+0.04)]
    if len(q)<50: print(round(x0,2),'-'); continue
    print('%5.2f n%6d z %.3f..%.3f  y %.3f..%.3f  zp5 %.3f'%(x0,len(q),q[:,2].min(),q[:,2].max(),q[:,1].min(),q[:,1].max(),np.percentile(q[:,2],5)))
# grip tube: outer region |x|>0.30
for s in (1,-1):
    q=A[(s*A[:,0]>0.30)&(A[:,2]>0.95)&(A[:,2]<1.2)&(A[:,1]>-0.3)&(A[:,1]<0.1)]
    print('outer',s,'x max',np.abs(q[:,0]).max().round(3),'centre y,z of |x|>0.35:',np.median(q[s*q[:,0]>0.35][:,1:],axis=0).round(3))
