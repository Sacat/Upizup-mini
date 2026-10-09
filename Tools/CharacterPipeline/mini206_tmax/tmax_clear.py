import numpy as np
import matplotlib; matplotlib.use('Agg'); import matplotlib.pyplot as plt
A=np.load('tmax/profile_pts.npy').astype(np.float64); s=1.575/1.3515
A*=s
rear=np.array([-0.6927*s,0.2177*s]); front=np.array([0.6588*s,0.2610*s]); print('axles m',rear,front)
fig,ax=plt.subplots(1,2,figsize=(18,8))
for k,(c,lo,hi,spec) in enumerate([(rear,-1.25,-0.40,0.2865),(front,0.40,1.15,0.2745)]):
    m=(A[:,1]>lo)&(A[:,1]<hi)&(A[:,2]<0.75)
    for xw,col in ((0.06,'k'),(0.12,'orange')):
        mm=m&(np.abs(A[:,0])<xw)&(np.abs(A[:,0])>=(0 if xw==0.06 else 0.06)); ax[k].scatter(A[mm,1],A[mm,2],s=0.05,c=col)
    r=np.hypot(A[:,1]-c[0],A[:,2]-c[1])
    ax[k].add_patch(plt.Circle(c,c[1],fill=False,color='g',lw=1.5,label='scan r=axle h'))
    ax[k].add_patch(plt.Circle(c,spec,fill=False,color='r',lw=1.5,label=f'spec r {spec}'))
    ax[k].set_aspect('equal'); ax[k].grid(True,alpha=.3); ax[k].legend()
    for rr in (c[1]+0.01,spec+0.01,spec+0.03):
        hit=(r<rr)&(r>rr-0.02)&(np.abs(A[:,0])<0.1)&(A[:,2]>c[1])
        print('ring',round(rr,3),'points in band above axle',hit.sum())
plt.tight_layout(); plt.savefig('tmax/wheel_clear.png',dpi=65)
