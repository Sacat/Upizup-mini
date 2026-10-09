"""Every junction / bridge join: where two road surfaces cover the same XZ, location + max height step before and after."""
import sys,os,numpy as np
sys.path.insert(0,os.path.dirname(__file__)); from geo_lib import *
A=load(sys.argv[1]); B=load(sys.argv[2]); out=sys.argv[3]
names=[k for k in A if k!='ExpansionTerrain']
SA={k:Sampler(*A[k],cell=2.0) for k in names}; SB={k:Sampler(*B[k],cell=2.0) for k in names}
rows=[]
for i,a in enumerate(names):
    Wa,Fa=A[a]; Ca=Wa[Fa].mean(1); Cb=B[a][0][Fa].mean(1)
    for b in names[i+1:]:
        pts=[];db=[];da=[]
        for ca,cb in zip(Ca,Cb):
            h0=SA[b].h(ca[0],ca[2]); h1=SB[b].h(cb[0],cb[2])
            if h0 is not None and abs(h0-ca[1])<3.0: pts.append(ca); db.append(abs(h0-ca[1])); da.append(abs(h1-cb[1]) if h1 is not None else np.nan)
        if pts:
            P=np.array(pts); kind='BRIDGE' if ('_Deck' in a or '_Deck' in b) else 'junction'
            rows.append((kind,a,b,P[:,0].mean(),P[:,2].mean(),max(db),np.nanmax(da) if not np.all(np.isnan(da)) else float('nan')))
rows.sort(key=lambda r:(r[0]!='BRIDGE',-r[5]))
with open(out,'w') as f:
    f.write('| type | surface A | surface B | x | z | step before (m) | step after (m) |\n|---|---|---|---|---|---|---|\n')
    for r in rows: f.write('| %s | %s | %s | %.1f | %.1f | %.3f | %.3f |\n'%r)
print(len(rows),'joins; worst after %.3f'%max(r[6] for r in rows if r[6]==r[6]))
