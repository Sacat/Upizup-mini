import sys,os,json,numpy as np
sys.path.insert(0,os.path.dirname(__file__)); from geo_lib import *
import matplotlib; matplotlib.use('Agg'); import matplotlib.pyplot as plt
from matplotlib.collections import PolyCollection
G=load(sys.argv[1]); mats={r['name']:os.path.basename(r['mats'][0]) for r in json.load(open(sys.argv[2])) if r.get('mats')}
col={'ExpansionAsphalt.mat':'#333','MainRoad.mat':'#1f77b4','SideRoad.mat':'#d62728','DirtTrack.mat':'#8c564b'}
fig,ax=plt.subplots(figsize=(16,9),dpi=110)
TW,TF=G['ExpansionTerrain']; ax.tricontourf(TW[:,0],TW[:,2],TF,TW[:,1],levels=20,cmap='Greens',alpha=.45)
for k,(W,F) in G.items():
    if k=='ExpansionTerrain': continue
    m=mats.get(k.replace('_Deck',''),'bridge'); c=col.get(m,'#999')
    ax.add_collection(PolyCollection(W[F][:,:,[0,2]],facecolor=c,edgecolor=c,lw=.2))
    ax.text(W[:,0].mean(),W[:,2].mean(),k.replace('ExpansionRoad_','E').replace('Road_','R_')[:22],fontsize=6)
for p in json.load(open(sys.argv[3])): ax.plot(p[0],p[1],'o',ms=14,mfc='none',mec='orange',mew=2)
ax.set_aspect('equal'); ax.set_title('Live roads: black=ExpansionAsphalt  blue=MainRoad  red=SideRoad(light grey)  brown=Dirt; orange=junction steps >0.25 m')
plt.savefig(sys.argv[4],bbox_inches='tight')
