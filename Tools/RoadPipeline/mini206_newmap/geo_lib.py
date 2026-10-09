import numpy as np
def load(npz):
    d=np.load(npz); out={}
    for k in d.files:
        a=d[k]; i=int(np.where(a==-1e9)[0][0]); out[k]=(a[:i].reshape(-1,3),a[i+1:].astype(int).reshape(-1,3))
    return out
class Sampler:
    """Exact top-surface height of a triangle mesh at XZ points (Unity Y-up)."""
    def __init__(self,W,F,cell=2.0):
        self.W=W;self.F=F;self.cell=cell;self.grid={}
        P=W[:,[0,2]]
        for fi,(a,b,c) in enumerate(F):
            lo=np.floor(np.minimum(np.minimum(P[a],P[b]),P[c])/cell).astype(int); hi=np.floor(np.maximum(np.maximum(P[a],P[b]),P[c])/cell).astype(int)
            for i in range(lo[0],hi[0]+1):
                for j in range(lo[1],hi[1]+1): self.grid.setdefault((i,j),[]).append(fi)
    def h(self,x,z):
        best=None
        for fi in self.grid.get((int(np.floor(x/self.cell)),int(np.floor(z/self.cell))),[]):
            a,b,c=self.W[self.F[fi]]
            d=(b[2]-c[2])*(a[0]-c[0])+(c[0]-b[0])*(a[2]-c[2])
            if abs(d)<1e-12: continue
            l1=((b[2]-c[2])*(x-c[0])+(c[0]-b[0])*(z-c[2]))/d; l2=((c[2]-a[2])*(x-c[0])+(a[0]-c[0])*(z-c[2]))/d; l3=1-l1-l2
            if min(l1,l2,l3)>=-1e-6:
                y=l1*a[1]+l2*b[1]+l3*c[1]
                if best is None or y>best: best=y
        return best
