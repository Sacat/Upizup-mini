import sys,os,re,json,numpy as np
sys.path.insert(0,os.path.dirname(__file__)); from scene_lib import *
sc=Scene(os.environ.get('SCENE',R+'/Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity'))
rows=[]
for pid,g in sc.gameobjects():
    n=g.get('m_Name','')
    if not re.search(r'road|junction|apron|connector|roundabout|asphalt|bridge|ground|terrain',n,re.I): continue
    c=sc.comps(g)
    if 'MeshFilter' not in c: continue
    tr=c['Transform'][0] if 'Transform' in c else None
    info={'name':n,'path':sc.path(tr),'active':sc.active_in_hierarchy(tr),'tr':tr}
    if 'MeshRenderer' in c:
        mr=sc.tt(c['MeshRenderer'][0]); info['ren']=mr['m_Enabled']
        info['mats']=[ (sc.tt(m['m_PathID'])['m_Name'] if m['m_FileID']==0 else sc.ext(m)[0]) for m in mr['m_Materials']]
    mf=sc.tt(c['MeshFilter'][0]); info['mesh']=sc.ext(mf['m_Mesh'])
    info['col']='MeshCollider' in c
    rows.append(info)
json.dump(rows,open(sys.argv[1],'w'),indent=1,default=str)
from collections import Counter
act=[r for r in rows if r['active'] and r.get('ren',True)]
print(len(rows),'candidates,',len(act),'active')
print(Counter(r['name'].split('_')[0] for r in act).most_common(30))
print(Counter(tuple(os.path.basename(m) for m in r.get('mats',[])) for r in act).most_common(30))
