import sys,os,numpy as np
sys.path.insert(0,os.path.dirname(__file__)); from geo_lib import *
G=load(sys.argv[1]); T=Sampler(*G['ExpansionTerrain'],cell=3.0)
roads={k:v for k,v in G.items() if k!='ExpansionTerrain'}
S={k:Sampler(*v,cell=2.0) for k,v in roads.items()}
print('1) HEIGHT ABOVE TERRAIN (road surface - terrain at the same XZ), sampled at triangle centroids')
for k,(W,F) in sorted(roads.items()):
    C=W[F].mean(1); d=[]
    for x,y,z in C:
        t=T.h(x,z)
        if t is not None: d.append(y-t)
    d=np.array(d)
    if len(d)==0: print(f'  {k:45s} no terrain under'); continue
    print(f'  {k:45s} n={len(d):4d} above: median {np.median(d):+.2f} max {d.max():+.2f} | buried(<-0.05) {np.mean(d<-0.05)*100:4.0f}% | >0.30m {np.mean(d>0.30)*100:4.0f}%')
print('2) BUMPINESS along drive line: centre samples every 0.5 m from vertex pairs; max step per 0.5 m and grade change')
for k,(W,F) in sorted(roads.items()):
    if len(W)<6 or len(W)%2: continue
    L=W[0::2]; Rr=W[1::2]; Cn=(L+Rr)/2
    seg=np.linalg.norm(np.diff(Cn[:,[0,2]],axis=0),axis=1)
    if seg.sum()<5: continue
    s=np.r_[0,np.cumsum(seg)]; ss=np.arange(0,s[-1],0.5); y=np.interp(ss,s,Cn[:,1])
    g=np.diff(y)/0.5; dg=np.abs(np.diff(g))
    cross=np.abs(L[:,1]-Rr[:,1])/np.maximum(np.linalg.norm((L-Rr)[:,[0,2]],axis=1),1e-3)
    print(f'  {k:45s} len {s[-1]:6.0f} m | max grade {np.abs(g).max()*100:5.1f}% | max grade change/0.5m {dg.max()*100:5.1f}% | crossfall max {cross.max()*100:5.1f}%')
print('3) JUNCTIONS: where two road surfaces cover the same XZ, height difference')
names=sorted(roads)
for i,a in enumerate(names):
    Wa,Fa=roads[a]; Ca=Wa[Fa].mean(1)
    for b in names[i+1:]:
        Wb=roads[b][0]
        if Wa[:,0].max()<Wb[:,0].min() or Wb[:,0].max()<Wa[:,0].min() or Wa[:,2].max()<Wb[:,2].min() or Wb[:,2].max()<Wa[:,2].min(): continue
        d=[]
        for x,y,z in Ca:
            h=S[b].h(x,z)
            if h is not None and abs(h-y)<3: d.append(h-y)
        if d:
            d=np.array(d); print(f'  {a:40s} x {b:40s} overlap tris {len(d):4d} height diff median {np.median(np.abs(d)):.3f} max {np.abs(d).max():.3f} m')
