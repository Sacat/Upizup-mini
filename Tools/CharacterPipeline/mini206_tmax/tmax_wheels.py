import bpy,sys,numpy as np
from scipy.spatial import ConvexHull
bpy.ops.wm.open_mainfile(filepath=sys.argv[sys.argv.index('--')+1])
o=[x for x in bpy.data.objects if x.type=='MESH'][0]
co=np.empty(len(o.data.vertices)*3,np.float32); o.data.vertices.foreach_get('co',co); P=co.reshape(-1,3).astype(np.float64)
d=np.load(sys.argv[-2],allow_pickle=True).item(); R,c,dx=d['R'],d['c'],d['dx']
A=(P-c)@R.T; A[:,0]-=dx
def rx(t): return np.array([[1,0,0],[0,np.cos(t),-np.sin(t)],[0,np.sin(t),np.cos(t)]])
def contacts(A):
    yb=np.linspace(A[:,1].min(),A[:,1].max(),80); lo=[]
    for i in range(79):
        m=(A[:,1]>=yb[i])&(A[:,1]<yb[i+1])&(np.abs(A[:,0])<0.1)
        lo.append(A[m,2].min() if m.any() else 9)
    lo=np.array(lo); yc=(yb[:-1]+yb[1:])/2; mid=len(lo)//2
    i1=np.argmin(lo[:mid]); i2=mid+np.argmin(lo[mid:]); return (yc[i1],lo[i1]),(yc[i2],lo[i2])
T=np.eye(3)
for it in range(4):
    (y1,z1),(y2,z2)=contacts(A); t=-np.arctan2(z2-z1,y2-y1); A=A@rx(t).T; T=rx(t)@T
    print('iter',it,'contacts',round(y1,3),round(z1,3),round(y2,3),round(z2,3),'rot deg',round(np.degrees(t),3))
(y1,z1),(y2,z2)=contacts(A); A[:,2]-=min(z1,z2)
def fit_circle(pts):
    x,y=pts[:,0],pts[:,1]; M=np.c_[2*x,2*y,np.ones(len(x))]; b=x**2+y**2; s=np.linalg.lstsq(M,b,rcond=None)[0]
    cx,cy=s[0],s[1]; r=np.sqrt(s[2]+cx**2+cy**2); return cx,cy,r
res={}
for name,yc in (('w1',y1),('w2',y2)):
    m=(np.abs(A[:,0])<0.07)&(np.abs(A[:,1]-yc)<0.40)&(A[:,2]<0.62)
    Q=A[m][:,1:]; h=ConvexHull(Q); H=Q[h.vertices]
    cx,cy,r=fit_circle(H[H[:,1]<0.25])
    for _ in range(3):
        dd=np.abs(np.hypot(H[:,0]-cx,H[:,1]-cy)-r); keep=(dd<0.015)&(H[:,1]<cy+0.05)
        cx,cy,r=fit_circle(H[keep])
    resid=np.abs(np.hypot(H[keep,0]-cx,H[keep,1]-cy)-r).mean()
    # tyre width: points within the tyre annulus
    ann=(np.abs(np.hypot(A[:,1]-cx,A[:,2]-cy)-(r-0.03))<0.03)&(np.abs(A[:,1]-cx)<r)
    wx=np.percentile(A[ann,0],[1,99])
    res[name]=dict(y=cx,z=cy,r=r,resid=resid,n=int(keep.sum()),width=wx.tolist())
    print(name,{k:(np.round(v,4) if not isinstance(v,int) else v) for k,v in res[name].items()})
wb=abs(res['w2']['y']-res['w1']['y']); print('wheelbase raw',wb,'scale to 1.575 m:',1.575/wb)
mn,mx=A.min(0),A.max(0); print('levelled bbox',mn,mx,'size',mx-mn,'scaled size',(mx-mn)*1.575/wb)
np.save(sys.argv[-1],{'R':T@R,'c':c,'dx':dx,'zoff':min(z1,z2),'wheels':res},allow_pickle=True)
