import numpy as np
A=np.load('tmax/hp_pts.npy').astype(np.float64)
def top(x,y,r=0.015,zmax=2):
    q=A[(np.abs(A[:,0]-x)<r)&(np.abs(A[:,1]-y)<r)&(A[:,2]<zmax)]; return q[:,2].max() if len(q) else None
print('seat/top profile x=0:'); print([(round(y,2),round(top(0,y),3)) for y in np.arange(-0.3,1.0,0.05)])
print('floorboard x=+-0.2:'); print([(round(y,2),round(top(0.2,y,zmax=0.6),3),round(top(-0.2,y,zmax=0.6),3)) for y in np.arange(-0.4,0.5,0.05)])
print('side x=0.17 z<0.9 (grab rails?)');print([(round(y,2),round(top(0.17,y,zmax=1.0),3)) for y in np.arange(0.2,1.0,0.05)])
