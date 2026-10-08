import bpy,sys,os,numpy as np
sys.path.insert(0,os.path.dirname(__file__)); import bodylib as B, rlib
arm,body=B.load(os.path.abspath('src/SacatRig.fbx')); B.weld(body)
px=B.tex_pixels(os.path.abspath('src/sacat_base.png')); col,_,_=B.vert_colors(body,px); W=B.world_co(body)
x,y,z=np.abs(W[:,0]),W[:,1],W[:,2]; s,v=B.hsv(col)
reg={'face(front)':(x<0.05)&(z>1.62)&(z<1.70)&(y<-0.06),'neck':(x<0.05)&(z>1.50)&(z<1.56),
 'upperarm':(x>0.25)&(x<0.42),'forearm':(x>0.50)&(x<0.66),'hand':(x>0.74),'thigh':(z>0.60)&(z<0.75),'shin':(z>0.15)&(z<0.40),'foot':(z<0.06)}
for k,m in reg.items():
    m=m&(s>0.2)
    c=col[m].mean(0); print(f'{k:12s} n={m.sum():5d} sRGB=({c[0]:.3f},{c[1]:.3f},{c[2]:.3f}) value={c.max():.3f}')
