import sys,os,re,json,numpy as np
sys.path.insert(0,os.path.dirname(__file__)); from scene_lib import *
sc=Scene(os.environ.get('SCENE',R+'/Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity')); pts=[]
skip=re.compile(r'(ExpansionRoad_|Road_|RoadJunctionPatch|JunctionPatch|ExpansionTerrain|ImportTrim_Road|Copernicus|Grass|Tuft|Sea|Water|Ocean|Sky|Light|Camera|Deck|Sidewalk|Kerb|Frontage)',re.I)
for pid,g in sc.gameobjects():
    c=sc.comps(g); n=g.get('m_Name','')
    if 'Transform' not in c or skip.search(n): continue
    if not ({'MeshRenderer','BoxCollider','CharacterController','CapsuleCollider'}&set(c)): continue
    tr=c['Transform'][0]
    try:
        p=sc.path(tr)
        if skip.search(p) and not re.search(r'House|Home|Stall|Market|Church|Farm|Credit|Estate|NPC',p): continue
        if not sc.active_in_hierarchy(tr): continue
        M=sc.world_matrix(tr)
    except Exception: continue
    pts.append([float(M[0,3]),float(M[1,3]),float(M[2,3])])
json.dump(pts,open(sys.argv[1],'w')); print(len(pts),'obstacle pivots')
