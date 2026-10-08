"""Terrain poking through roads: sample each road triangle (dense) and compare terrain height there."""
import sys,os,numpy as np
sys.path.insert(0,os.path.dirname(__file__)); from geo_lib import *
G=load(sys.argv[1]); T=Sampler(*G['ExpansionTerrain'],cell=3.0); worst=[]
tot=0;bad=0
for k,(W,F) in G.items():
    if k=='ExpansionTerrain' or k.endswith('_Deck'): continue
    for tri in W[F]:
        for u,v in ((0.02,0.02),(0.96,0.02),(0.02,0.96),(0.49,0.02),(0.02,0.49),(0.49,0.49),(1/3,1/3)):
            p=tri[0]*(1-u-v)+tri[1]*u+tri[2]*v; t=T.h(p[0],p[2]); tot+=1
            if t is not None and t>p[1]+0.005: bad+=1; worst.append((t-p[1],k,round(p[0],1),round(p[2],1)))
worst.sort(reverse=True); print(f'terrain above road at {bad}/{tot} samples ({bad/tot*100:.2f}%); worst', worst[:6])
from collections import Counter; print(Counter(w[1] for w in worst).most_common(8))
