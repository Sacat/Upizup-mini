"""Write the MINI-198 apply file for Unity: per road object new world Y per vertex (+ original world XZ for checking),
triangles to drop where a higher-priority road already covers the same surface, terrain Y, objects to ground-follow."""
import sys,os,json,re,numpy as np
sys.path.insert(0,os.path.dirname(__file__)); from geo_lib import *; from scene_lib import *
A=load(sys.argv[1]); B=load(sys.argv[2]); roads_meta={r['name']:r for r in json.load(open(sys.argv[3]))}; out=sys.argv[4]
LOCKED=['ImportTrim_Road_way_22917921','Road_way_23042701','DogLifeJunction_MINI142','Road_Connector_00_way_387239000_way_23042701','JunctionPatch_LalayConnector_23042701']
names=[k for k in B if k!='ExpansionTerrain' and not k.endswith('_Deck')]
def length(k): W=B[k][0]; return np.ptp(W[:,0])+np.ptp(W[:,2])
order=[k for k in LOCKED if k in names]+sorted([k for k in names if 'Patch' in k or 'Junction' in k and k not in LOCKED],key=length,reverse=True)
order=list(dict.fromkeys(order)); order+=sorted([k for k in names if k not in order],key=length,reverse=True)
S={}; recs=[]
for k in order:
    W,F=B[k]; drop=[]
    for fi,tri in enumerate(W[F]):
        cov=0
        for u,v in ((1/3,1/3),(0.05,0.05),(0.9,0.05),(0.05,0.9)):
            p=tri[0]*(1-u-v)+tri[1]*u+tri[2]*v
            for o,so in S.items():
                h=so.h(p[0],p[2])
                if h is not None and abs(h-p[1])<0.06: cov+=1; break
        if cov==4: drop.append(fi)
    S[k]=Sampler(W,F,cell=2.0)
    m=roads_meta[k]; W0=A[k][0]
    recs.append({'name':k,'path':m['path'],'mesh':m['mesh'],'priority':order.index(k),
      'x':np.round(W0[:,0],4).tolist(),'z':np.round(W0[:,2],4).tolist(),'y0':np.round(W0[:,1],4).tolist(),'y':np.round(W[:,1],4).tolist(),
      'dropTriangles':drop if len(drop)<len(F) else [],'disable':len(drop)==len(F),'triangleCount':len(F)})
    print(f'{k:45s} moved max {np.abs(W[:,1]-W0[:,1]).max():.2f} m, drop {len(drop)}/{len(F)} covered triangles')
TW0=A['ExpansionTerrain'][0]; TW=B['ExpansionTerrain'][0]
terrain={'name':'ExpansionTerrain','path':roads_meta['ExpansionTerrain']['path'],'mesh':roads_meta['ExpansionTerrain']['mesh'],
  'x':np.round(TW0[:,0],3).tolist(),'z':np.round(TW0[:,2],3).tolist(),'y0':np.round(TW0[:,1],4).tolist(),'y':np.round(TW[:,1],4).tolist()}
# ground-follow list: transforms whose pivot ground changes > 3 cm, excluding roads/terrain; houses are never moved (reported only)
from scipy.interpolate import LinearNDInterpolator
f=LinearNDInterpolator(TW0[:,[0,2]],TW[:,1]-TW0[:,1],fill_value=0)
sc=Scene(R+'/Assets/UpIzUpMini/Scenes/GrandBayProof.unity'); follow=[];report=[]
CONTAINER=re.compile(r'(ApprovedWorld|Massing|Homes$|Districts|Lots|Boundaries|Expansion$|Bridges|Sidewalks|Frontages|Vegetation|Grass|Crops|Props|NPCs$|World$|Environment)')
skip=re.compile(r'(ExpansionRoad_|Road_|RoadJunctionPatch|JunctionPatch|ExpansionTerrain|ImportTrim_|Copernicus|DogLifeJunction|Sea|Water|Ocean|Sun|Light|Camera|Canvas|EventSystem|Manager|System|Spawner)')
def chain(tr):
    out=[]; pid=tr
    while pid:
        t=sc.tt(pid)
        if 'm_GameObject' not in t: break
        out.append(pid); pid=t['m_Father']['m_PathID'] if t['m_Father']['m_FileID']==0 else 0
    return out[::-1]   # root ... self
units={}
for pid,g in sc.gameobjects():
    c=sc.comps(g)
    if 'Transform' not in c or not ({'MeshRenderer','BoxCollider','CharacterController','CapsuleCollider','MeshCollider'}&set(c)): continue
    tr=c['Transform'][0]
    try:
        if not sc.active_in_hierarchy(tr): continue
        ch=chain(tr)
        unit=None
        for p in ch:   # shallowest ancestor that is not a big container
            nm=sc.tt(sc.tt(p)['m_GameObject']['m_PathID']).get('m_Name','')
            if skip.match(nm): unit=None; break
            if CONTAINER.search(nm) and p!=ch[-1]: continue
            unit=p; break
        if unit is not None: units[unit]=1
    except Exception: continue
for u in units:
    M=sc.world_matrix(u); dy=float(f(M[0,3],M[2,3]))
    if abs(dy)<0.03: continue
    p=sc.path(u)
    if re.search(r'Bridge|Church',p) or abs(dy)>0.5: report.append({'path':p,'dy':round(dy,3)})
    else: follow.append({'path':p,'dy':round(dy,3),'x':round(float(M[0,3]),3),'z':round(float(M[2,3]),3)})
json.dump({'task':'MINI-198','scene':'Assets/UpIzUpMini/Scenes/GrandBayProof.unity','material':'Assets/UpIzUpMini/Maps/Regions/dm-dom-grand-bay-expansion-v1/Generated/ExpansionAsphalt.mat',
  'roads':recs,'terrain':terrain,'groundFollow':follow,'notMovedReport':report},open(out,'w'))
print('ground-follow objects',len(follow),'reported-only structures',len(report)); print('apply file MB',os.path.getsize(out)/1e6)
