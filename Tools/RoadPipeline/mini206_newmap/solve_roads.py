"""MINI-198 prototype: one smooth, shared height field for every live road (X/Z untouched).
Unknowns: welded vertex heights of every road mesh. Energy (sparse least squares):
  bi-Laplacian smoothness on each road's welded mesh graph (removes bumps/kinks, equalises crossfall)
  + data term toward  terrain+3 cm (roads hug the ground) or current height (locked: bridges' approaches, Lalay sidewalk roads)
  + hard-ish coupling: every road vertex lying on another road's surface = that surface's height (junctions share one height)
Then terrain vertices near roads are conformed just under the new road surface with a smooth blend."""
import sys,os,json,numpy as np, scipy.sparse as sp, scipy.sparse.linalg as la
sys.path.insert(0,os.path.dirname(__file__)); from geo_lib import *
G=load(sys.argv[1]); out=sys.argv[2]
TW,TF=G['ExpansionTerrain']; T=Sampler(TW,TF,cell=3.0)
FIXED={k for k in G if k.endswith('_Deck')}                  # bridge decks: untouched, roads couple to them
LOCKED={'ImportTrim_Road_way_22917921','Road_way_23042701','DogLifeJunction_MINI142','Road_Connector_00_way_387239000_way_23042701','JunctionPatch_LalayConnector_23042701'}  # Lalay sidewalks/kerbs follow these
roads=[k for k in G if k!='ExpansionTerrain' and k not in FIXED]
OFF=0.03
# weld each road mesh
nodes=[];node_of={};own=[]
for k in roads:
    W,F=G[k]; key=np.round(W[:,[0,2]]/0.01).astype(np.int64); _,inv=np.unique(key[:,0]*10**9+key[:,1],return_inverse=True)
    base=len(nodes); n=inv.max()+1; P=np.zeros((n,3)); P[inv]=W
    node_of[k]=base+inv; nodes.extend(P); own+= [k]*n
X=np.array(nodes); N=len(X); own=np.array(own)
rows=[];cols=[];vals=[];rhs=[];wts=[]; r=0
def eq(cs,vs,b,w):
    global r
    for c,v in zip(cs,vs): rows.append(r);cols.append(c);vals.append(v*w)
    rhs.append(b*w); r+=1
# adjacency per road
adj=[set() for _ in range(N)]
for k in roads:
    for a,b,c in node_of[k][G[k][1]]:
        for u,v in ((a,b),(b,c),(c,a)):
            if u!=v: adj[u].add(v); adj[v].add(u)
# smoothness: bi-Laplacian via L then penalize (L y) and (L^2 y): use umbrella L rows
Lr=[];Lc=[];Lv=[]
for i in range(N):
    nb=list(adj[i]);
    if not nb: continue
    Lr+=[i]*(len(nb)+1); Lc+=[i]+nb; Lv+=[1.0]+[-1.0/len(nb)]*len(nb)
L=sp.csr_matrix((Lv,(Lr,Lc)),shape=(N,N))
WS=float(os.environ.get('W_SMOOTH','300'))
# data term
ter=np.array([T.h(x,z) for x,y,z in X],dtype=object); ter=np.array([np.nan if t is None else t for t in ter],dtype=float)
# keep every road where it is, unless it is clearly buried (>5 cm under) or floating (>15 cm over) the ground
cur_off=X[:,1]-ter
bad=(~np.isnan(ter))&((cur_off<-0.05)|(cur_off>0.15))
target=np.where(bad,ter+OFF,X[:,1])
print('road nodes buried/floating -> ground target:',int(bad.sum()),'of',N)
locked=np.isin(own,list(LOCKED))
# roads that sit on a bridge approach: keep near current
from scipy.spatial import cKDTree as _KD
OBS=np.array(json.load(open(os.environ['OBSTACLES'])))[:,[0,2]] if os.environ.get('OBSTACLES') else np.zeros((0,2))
otree=_KD(OBS) if len(OBS) else None
near_obj=(otree.query(X[:,[0,2]])[0]<float(os.environ.get('OBS_ROAD_R','8'))) if otree is not None else np.zeros(N,bool)
target=np.where(near_obj,X[:,1],target)   # roads beside buildings/stalls keep their height
Wd=np.where(locked,30.0,np.where(near_obj&~bad,float(os.environ.get('W_NEAR','25')),np.where(near_obj,float(os.environ.get('W_NEAR_BAD','25')),float(os.environ.get('W_DATA','1.0')))))
print('road nodes held near buildings/props:',int(near_obj.sum()))
tgt=np.where(locked,X[:,1],target)
for i in range(N): eq([i],[1.0],tgt[i],Wd[i])
# coupling to other roads / decks
S={k:Sampler(*G[k],cell=2.0) for k in list(roads)+list(FIXED)}
WC=float(os.environ.get('W_COUPLE','60')); ncpl=0
for k in roads:
    Wk,Fk=G[k]
    for i in np.unique(node_of[k]):
        x,_,z=X[i]
        for o in list(roads)+list(FIXED):
            if o==k: continue
            Wo,Fo=G[o]
            if not (Wo[:,0].min()-1<x<Wo[:,0].max()+1 and Wo[:,2].min()-1<z<Wo[:,2].max()+1): continue
            # find triangle of o containing (x,z)
            So=S[o]; hit=None
            for fi in So.grid.get((int(np.floor(x/So.cell)),int(np.floor(z/So.cell))),[]):
                a,b,c=Wo[Fo[fi]]; dd=(b[2]-c[2])*(a[0]-c[0])+(c[0]-b[0])*(a[2]-c[2])
                if abs(dd)<1e-12: continue
                l1=((b[2]-c[2])*(x-c[0])+(c[0]-b[0])*(z-c[2]))/dd; l2=((c[2]-a[2])*(x-c[0])+(a[0]-c[0])*(z-c[2]))/dd; l3=1-l1-l2
                if min(l1,l2,l3)>=-1e-6: hit=(fi,(l1,l2,l3)); break
            if hit is None: continue
            fi,l=hit
            if o in FIXED: eq([i],[1.0],float(np.dot(l,Wo[Fo[fi]][:,1])),WC*2)
            else:
                ids=node_of[o][Fo[fi]]; eq([i]+list(ids),[1.0]+[-x_ for x_ in l],0.0,WC)
            ncpl+=1
# ribbons (2 verts per cross-section): level crossfall + arc-length second difference along the centre line
ribbon=set(); WX=float(os.environ.get('W_CROSS','80')); W2=float(os.environ.get('W_CURV','400'))
for k in roads:
    W,F=G[k]
    if len(W)%2 or len(F)!=len(W)-2 or 'Patch' in k or 'Junction' in k: continue
    ribbon.add(k); nL=node_of[k][0::2]; nR=node_of[k][1::2]; C=(W[0::2]+W[1::2])/2
    sl=np.linalg.norm(np.diff(C[:,[0,2]],axis=0),axis=1)
    for j in range(len(nL)): eq([nL[j],nR[j]],[1.0,-1.0],0.0,WX)
    for j in range(1,len(nL)-1):
        s0,s1=max(sl[j-1],.3),max(sl[j],.3); h=(s0+s1)/2
        cs=[nL[j-1],nR[j-1],nL[j],nR[j],nL[j+1],nR[j+1]]
        co=np.array([.5/s0,.5/s0,-.5/s0-.5/s1,-.5/s0-.5/s1,.5/s1,.5/s1])/h
        eq(cs,list(co),0.0,W2)
print('ribbons',len(ribbon),'of',len(roads))
nonrib=np.array([o not in ribbon for o in own])
A=sp.csr_matrix((vals,(rows,cols)),shape=(r,N)); b=np.array(rhs)
Dn=sp.diags(nonrib.astype(float))
M=sp.vstack([A,WS*(Dn@L@L),WS*0.3*(Dn@L)]); bb=np.r_[b,np.zeros(N),np.zeros(N)]
y=la.spsolve((M.T@M).tocsc(),M.T@bb)
print('nodes',N,'couplings',ncpl,'moved: median %.3f max %.3f m'%(np.median(np.abs(y-X[:,1])),np.abs(y-X[:,1]).max()))
newG={}
for k in G:
    W,F=G[k]
    if k in roads: W2=W.copy(); W2[:,1]=y[node_of[k]]; newG[k]=(W2,F)
    else: newG[k]=(W.copy(),F)
# terrain conform: terrain vertices under/near roads go to road-0.10 inside +1.5 m, blend out to 6 m
from scipy.spatial import cKDTree
DIRT=set(os.environ.get('DIRT','Road_user_highland_farm_spur').split(','))
RP=[];RH=[]
for k in roads:   # bridge decks excluded: never fill the ground under a bridge
    W,F=newG[k]; C=W[F].mean(1)
    for tri in W[F]:   # dense samples on each road triangle
        for u,v in ((1/3,1/3),(0.1,0.1),(0.8,0.1),(0.1,0.8),(0.5,0.25),(0.25,0.5),(0.25,0.25)):
            p=tri[0]*(1-u-v)+tri[1]*u+tri[2]*v; RP.append(p[[0,2]]); RH.append(p[1]+(0.04 if k in DIRT else 0.0))   # dirt: ground fitted 1 cm under the edges (soft blend)
RP=np.array(RP);RH=np.array(RH); tree=cKDTree(RP)
d,ii=tree.query(TW[:,[0,2]],k=4); dmin=d[:,0]; rh=(RH[ii]*(1/np.maximum(d,0.05))).sum(1)/(1/np.maximum(d,0.05)).sum(1)
IN=float(os.environ.get('T_IN','2.0')); BAND=float(os.environ.get('T_BAND','5.5'))
w=np.clip(1-(dmin-IN)/(BAND-IN),0,1); w=w*w*(3-2*w)
tw=TW.copy(); target_t=rh-float(os.environ.get('T_UNDER','0.05'))
# only fix what is wrong: cut terrain that pokes up into the road, raise terrain that leaves the road edge floating
# (more than 12 cm above the ground); everything else is left exactly as it is
FLOAT=float(os.environ.get('T_FLOAT','0.12'))
need=np.where(TW[:,1]>target_t, target_t, np.where(TW[:,1]<target_t-FLOAT, target_t-FLOAT, TW[:,1]))
tw[:,1]=np.where(dmin<=IN,need,TW[:,1]*(1-w)+need*w)
# bridges: keep the ground under and right beside every deck exactly as it is
DK=np.vstack([newG[k][0] for k in FIXED]); dkt=cKDTree(DK[:,[0,2]]); dd_,di_=dkt.query(TW[:,[0,2]])
gully=(dd_<8.0)&(TW[:,1]<DK[di_,1]-0.4)&(dmin>IN)   # ground well below a deck = the space under the bridge (not under a road)
tw[:,1]=np.where(gully,TW[:,1],tw[:,1])
if otree is not None:   # never move ground under/next to buildings, stalls, fences, NPCs
    od=otree.query(TW[:,[0,2]])[0]; R0=float(os.environ.get('OBS_GROUND_R','4.0'))
    keep=np.clip((od-R0)/2.0,0,1); keep=keep*keep*(3-2*keep)
    R1=float(os.environ.get('OBS_NOFILL_R','6.0'))
    nofill=od<R1                        # beside buildings/props/NPCs: never raise the ground (would bury their base)
    keep=np.where(dmin<=IN,1.0,keep)   # directly under a road footprint the ground may still be cut to meet the road
    delta=(tw[:,1]-TW[:,1])*keep
    delta=np.where(nofill&(delta>0),0.0,delta)
    tw[:,1]=TW[:,1]+delta
newG['ExpansionTerrain']=(tw,TF)
print('terrain verts moved',int((np.abs(tw[:,1]-TW[:,1])>0.005).sum()),'max |dy| %.2f'%np.abs(tw[:,1]-TW[:,1]).max())
np.savez_compressed(out,**{k:np.concatenate([v[0].ravel(),[-1e9],v[1].ravel().astype(float)]) for k,v in newG.items()})
