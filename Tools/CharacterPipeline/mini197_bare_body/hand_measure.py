import bpy,sys,os,numpy as np
sys.path.insert(0,os.path.dirname(__file__)); import bodylib as B
arm,body=B.load(os.path.abspath('src/SacatRig.fbx')); B.weld(body)
W=B.world_co(body); me=body.data
gn={g.index:g.name for g in body.vertex_groups}
dom=np.array([gn[max(v.groups,key=lambda g:g.weight).group] if len(v.groups) else '' for v in me.vertices])
for side,sg in (('L',1),('R',-1)):
    X=W[:,0]*sg; hand=B.bone_world(arm,f'CC_Base_{side}_Hand',body)
    th=np.array([d.startswith(f'CC_Base_{side}_Index') or d.startswith(f'CC_Base_{side}_Thumb') for d in dom])
    fi=np.array([any(d.startswith(f'CC_Base_{side}_{k}') for k in ('Mid','Ring','Pinky')) for d in dom])
    print(side,'hand joint',hand.round(3),'thumb verts',th.sum(),'finger verts',fi.sum())
    for xs in np.arange(0.74,0.95,0.02):
        m=(X>xs*1)&(X<xs+0.01)
        mt=m&th; mo=m&~th
        if mo.sum()==0: continue
        print(f'  x={xs:.2f} nonthumb y[{W[mo,1].min():.3f},{W[mo,1].max():.3f}] z[{W[mo,2].min():.3f},{W[mo,2].max():.3f}]  thumb n={mt.sum()}'+(f' y[{W[mt,1].min():.3f},{W[mt,1].max():.3f}] z[{W[mt,2].min():.3f},{W[mt,2].max():.3f}]' if mt.sum() else ''))
    # finger principal axis
    f=W[(X>0.85)&~th]; c=f.mean(0); u=np.linalg.svd(f-c)[2][0]; print('  finger axis',u.round(3),'center',c.round(3))
    t=W[th&(X>0.76)]; ct=t.mean(0); ut=np.linalg.svd(t-ct)[2][0]; print('  thumb axis',ut.round(3),'center',ct.round(3),'thumb tip',t[np.argmax((t-ct)@ut*np.sign(ut[0]*sg))].round(3))
