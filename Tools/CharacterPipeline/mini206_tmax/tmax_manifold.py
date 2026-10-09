import bpy,sys,bmesh,numpy as np
bpy.ops.wm.open_mainfile(filepath=sys.argv[sys.argv.index('--')+1])
o=[x for x in bpy.data.objects if x.type=='MESH'][0]; me=o.data
# edge-face counts via numpy
nE=len(me.edges); lt=np.zeros(len(me.loops),np.int32); me.loops.foreach_get('edge_index',lt)
cnt=np.bincount(lt,minlength=nE); print('edges',nE,'boundary',int((cnt==1).sum()),'nonmanifold(>2)',int((cnt>2).sum()))
