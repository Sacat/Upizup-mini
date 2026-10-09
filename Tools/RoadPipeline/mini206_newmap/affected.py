import sys,os,re,json,numpy as np
sys.path.insert(0,os.path.dirname(__file__)); from scene_lib import *; from geo_lib import *
from scipy.interpolate import LinearNDInterpolator
A=load(sys.argv[1]); B=load(sys.argv[2])
TW=A['ExpansionTerrain'][0]; d=B['ExpansionTerrain'][0][:,1]-TW[:,1]
f=LinearNDInterpolator(TW[:,[0,2]],d,fill_value=0)
sc=Scene(os.environ.get('SCENE',R+'/Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity'))
hits=[]
for pid,g in sc.gameobjects():
    c=sc.comps(g); n=g.get('m_Name','')
    if 'Transform' not in c or not ({'MeshRenderer','BoxCollider','CharacterController','MonoBehaviour'}&set(c)): continue
    tr=c['Transform'][0]
    t=sc.tt(tr)
    if 'm_Father' in t and t['m_Father']['m_FileID']==0 and t['m_Father']['m_PathID']:
        # only top-most interesting objects: parent has no renderer of same group -> keep everything but report roots of groups
        pass
    if re.match(r'(ExpansionRoad_|Road_|RoadJunctionPatch|JunctionPatch|ExpansionTerrain|ImportTrim_Road_way)',n): continue
    try: M=sc.world_matrix(tr)
    except Exception: continue
    x,y,z=M[:3,3]; dd=float(f(x,z))
    if abs(dd)>0.15 and sc.active_in_hierarchy(tr): hits.append((abs(dd),dd,sc.path(tr),round(x,1),round(z,1)))
hits.sort(reverse=True)
groups={}
for a,dd,p,x,z in hits: groups.setdefault('/'.join(p.split('/')[:3]),[]).append((round(dd,2),x,z))
print(len(hits),'objects whose pivot ground moves >0.15 m')
for k,v in sorted(groups.items(),key=lambda kv:-max(abs(t[0]) for t in kv[1]))[:40]: print(f'  {len(v):3d}  max {max(v,key=lambda t:abs(t[0]))[0]:+.2f}  {k}')
