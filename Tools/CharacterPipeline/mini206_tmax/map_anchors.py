import numpy as np,json
d=np.load('tmax/glb2scan.npy',allow_pickle=True).item(); s,R,t=d['s'],d['R'],d['t']
YOFF=-0.0198   # axle midpoint in the scan metres frame
anch={  # Unity WORLD positions under the 1.15 root (from prefab_anchors.py)
 'Seat':(0,0.023,-0.2024),'PillionSeat':(0,0.023,-0.5828),'HandlebarLeft':(-0.322,1.0939,0.5184),'HandlebarRight':(0.322,1.0939,0.5184),
 'LeftFootTarget':(-0.207,0.3141,0.0575),'RightFootTarget':(0.207,0.3141,0.0575),'PillionFootLeft':(-0.276,0.384,-0.4452),'PillionFootRight':(0.276,0.384,-0.4452),
 'PillionGrabLeft':(-0.161,0.7182,-0.259),'PillionGrabRight':(0.161,0.7182,-0.259),'FrontAxle(FrontWheel)':(0.0197,0.3491,0.9425),'RearAxle(RearWheel)':(0.1825,0.3491,-0.8094),
 'SteeringPivot':(0,1.0939,0.5184),'COM':(0,0.3762,0),'FrontWheelCollider':(0,0.4641,0.9425),'RearWheelCollider':(0,0.4641,-0.8094)}
out={}
for k,w in anch.items():
    u=np.array(w)/1.15; b=np.array([u[0],u[2],u[1]]); m=s*R@b+t; m[1]-=YOFF
    out[k]={'old_root_local_unity':np.round(u,4).tolist(),'new_unity_local':np.round([m[0],m[2],m[1]],4).tolist()}
    print('%-24s old(root-local) %s -> new FBX unity-local (x right,y up,z fwd) %s'%(k,np.round(u,3),np.round([m[0],m[2],m[1]],3)))
json.dump(out,open('tmax/anchor_map.json','w'),indent=1)
