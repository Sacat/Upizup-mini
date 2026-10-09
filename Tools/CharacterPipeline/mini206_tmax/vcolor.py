import bpy,numpy as np
bpy.ops.wm.open_mainfile(filepath='tmax/scan_final.blend')
o=bpy.data.objects['TMAX_Scan_HP']; me=o.data
img=[i for i in bpy.data.images if i.name=='Image'][0]; W,H=img.size
px=np.empty(W*H*4,np.float32); img.pixels.foreach_get(px); px=px.reshape(H,W,4)
np.save('tmax/basecolor.npy',(px[:,:,:3]*255).astype(np.uint8))
uv=np.empty(len(me.loops)*2,np.float32); me.uv_layers[0].data.foreach_get('uv',uv); uv=uv.reshape(-1,2)
vi=np.empty(len(me.loops),np.int32); me.loops.foreach_get('vertex_index',vi)
c=px[np.clip((uv[:,1]*H).astype(int),0,H-1),np.clip((uv[:,0]*W).astype(int),0,W-1),:3]
col=np.zeros((len(me.vertices),3)); n=np.zeros(len(me.vertices)); np.add.at(col,vi,c); np.add.at(n,vi,1); col/=n[:,None]
np.save('tmax/vcol.npy',col.astype(np.float32))
# also per loop uv for later
print('done',col.mean(0))
