import sys,os,re,json,numpy as np
sys.path.insert(0,os.path.dirname(__file__)); from scene_lib import *
sc=Scene(os.environ.get('SCENE',R+'/Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity'))
items={}
for pid,g in sc.gameobjects():
    n=g.get('m_Name','');c=sc.comps(g)
    if 'MeshFilter' not in c or 'Transform' not in c: continue
    tr=c['Transform'][0]
    if not sc.active_in_hierarchy(tr): continue
    if re.match(r'(ExpansionRoad_|Road_|RoadJunctionPatch_|JunctionPatch_|DogLifeJunction|ImportTrim_Road|ExpansionTerrain$|Driveable_Deck)',n):
        mf=sc.tt(c['MeshFilter'][0]); ref=sc.ext(mf['m_Mesh'])
        V,F=load_mesh(sc,ref); M=sc.world_matrix(tr)
        W=V@M[:3,:3].T+M[:3,3]
        key=n if n!='Driveable_Deck' else sc.path(tr).split('/')[-2]+'_Deck'
        items[key]=(W,F)
        print(f'{key[:45]:45s} v={len(W):6d} f={len(F):6d} x[{W[:,0].min():.0f},{W[:,0].max():.0f}] y[{W[:,1].min():.2f},{W[:,1].max():.2f}] z[{W[:,2].min():.0f},{W[:,2].max():.0f}]')
np.savez_compressed(sys.argv[1],**{k:np.concatenate([v[0].ravel(),[-1e9],v[1].ravel().astype(float)]) for k,v in items.items()})
