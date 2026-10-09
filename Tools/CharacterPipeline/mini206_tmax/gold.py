import numpy as np, colorsys
import matplotlib; matplotlib.use('Agg'); import matplotlib.pyplot as plt
A=np.load('tmax/hp_pts.npy').astype(np.float64); C=np.load('tmax/vcol.npy')
r,g,b=C[:,0],C[:,1],C[:,2]
gold=(r>0.35)&(r>b*1.8)&(g>b*1.3)
red=(r>0.35)&(r>g*2.2)&(r>b*2.2)
white=(r>0.75)&(g>0.75)&(b>0.75)
print('gold',gold.sum(),'red',red.sum(),'white',white.sum())
fig,ax=plt.subplots(1,2,figsize=(20,9))
for k,(i,j) in enumerate(((1,2),(0,2))):
    ax[k].scatter(A[::20,i],A[::20,j],s=0.02,c='0.8')
    for m,cc in ((gold,'orange'),(red,'r'),(white,'b')): ax[k].scatter(A[m,i],A[m,j],s=0.2,c=cc)
    ax[k].set_aspect('equal'); ax[k].grid(True,alpha=.3)
plt.savefig('tmax/colors.png',dpi=55)
G=A[gold]; G=G[(G[:,2]>0.3)&(G[:,2]<0.8)&(G[:,1]<-0.4)]
for side in (1,-1):
    q=G[side*G[:,0]>0.03]; print('gold side',side,'n',len(q),'x',np.percentile(q[:,0],[5,50,95]).round(3),'z range',q[:,2].min().round(3),q[:,2].max().round(3))
    zs=np.arange(q[:,2].min()+0.01,q[:,2].max()-0.01,0.015); C2=[]
    for z in zs:
        s=q[np.abs(q[:,2]-z)<0.008]
        if len(s)>20: C2.append([(s[:,1].min()+s[:,1].max())/2,(s[:,0].min()+s[:,0].max())/2,z,s[:,1].max()-s[:,1].min()])
    C2=np.array(C2); print(np.round(C2,3)); k=np.polyfit(C2[:,2],C2[:,0],1); print(' rake deg',np.degrees(np.arctan(-k[0])).round(2),'y(z)=',k.round(4))
