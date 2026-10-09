"""MINI-206: footprints (2 m grid over each mesh's world XZ bounds) of structures the road fix must never move or bury:
school/classroom/field/pitch, bay & sea walls, roundabout island + beach, apartments, houses, credit union, church, market.
Merged with obstacles.json (object pivots). Usage: protect.py <obstacles.json> <out.json> <names_out.json>"""
import sys,os,re,json,numpy as np
sys.path.insert(0,os.path.dirname(__file__)); from scene_lib import *
sc=Scene(os.environ.get('SCENE',R+'/Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity'))
PAT=re.compile(os.environ.get('PROTECT',r'School|Classroom|Field|Pitch|Wall|Roundabout|Apartment|House|Home|CreditUnion|Church|Market|Stall|Fence|Gate'),re.I)
pts=json.load(open(sys.argv[1])); names={}
for pid,g in sc.gameobjects():
    c=sc.comps(g); n=g.get('m_Name','')
    if 'MeshFilter' not in c or 'Transform' not in c: continue
    tr=c['Transform'][0]
    try: p=sc.path(tr)
    except Exception: continue
    if not PAT.search(p) or re.search(r'(^|/)(ExpansionRoad_|Road_|ImportTrim_Road|Junction)',p): continue
    try:
        if not sc.active_in_hierarchy(tr): continue
        mf=sc.tt(c['MeshFilter'][0]); V,F=load_mesh(sc,sc.ext(mf['m_Mesh'])); M=sc.world_matrix(tr)
    except Exception: continue
    W=V@M[:3,:3].T+M[:3,3]
    if len(W)==0: continue
    lo,hi=W.min(0),W.max(0)
    if (hi[0]-lo[0])*(hi[2]-lo[2])>40000: continue      # skip giant ground-like meshes
    xs=np.arange(lo[0],hi[0]+2,2.0); zs=np.arange(lo[2],hi[2]+2,2.0)
    for x in xs:
        for z in zs: pts.append([float(x),float(W[:,1].min()),float(z)])
    key=p.split('/')[0]+'/'+n; names[key]=names.get(key,0)+1
json.dump(pts,open(sys.argv[2],'w')); json.dump(names,open(sys.argv[3],'w'),indent=1)
print('protected points',len(pts),'objects',len(names)); print(sorted(names)[:60])
# objects whose mesh cannot be read offline: protect a disc around the transform (radius from env, default 12 m)
for nm in os.environ.get('PROTECT_DISC','GenevaRoundaboutIsland').split(','):
    for pid,g in sc.gameobjects():
        if g.get('m_Name','')!=nm: continue
        M=sc.world_matrix(sc.comps(g)['Transform'][0]); cx,cy,cz=M[0,3],M[1,3],M[2,3]; r=float(os.environ.get('DISC_R','12'))
        for x in np.arange(-r,r+1,2.0):
            for z in np.arange(-r,r+1,2.0):
                if x*x+z*z<=r*r: pts.append([float(cx+x),float(cy),float(cz+z)])
        names[nm+' (disc r=%.0f m at %.1f,%.1f)'%(r,cx,cz)]=1
json.dump(pts,open(sys.argv[2],'w')); json.dump(names,open(sys.argv[3],'w'),indent=1); print('with discs',len(pts),[k for k in names if 'disc' in k])
