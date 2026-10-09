import sys,os,json,numpy as np
sys.path.insert(0,os.path.dirname(__file__)); from geo_lib import *
G=load(sys.argv[1]); roads={k:v for k,v in G.items() if k!='ExpansionTerrain'}; S={k:Sampler(*v) for k,v in roads.items()}
pts=[]
for a,(Wa,Fa) in roads.items():
    for x,y,z in Wa[Fa].mean(1):
        for b in roads:
            if b==a: continue
            h=S[b].h(x,z)
            if h is not None and 0.25<abs(h-y)<3: pts.append((round(x/6)*6,round(z/6)*6))
pts=sorted(set(pts)); json.dump(pts,open(sys.argv[2],'w')); print(len(pts),'step cells')
