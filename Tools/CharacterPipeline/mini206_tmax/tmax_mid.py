import bpy,numpy as np,time
bpy.ops.wm.open_mainfile(filepath='tmax/scan_final.blend')
o=bpy.data.objects['TMAX_Scan_HP']; t=time.time()
d=o.modifiers.new('d','DECIMATE'); d.ratio=400000/len(o.data.polygons)
bpy.context.view_layer.objects.active=o; bpy.ops.object.modifier_apply(modifier='d'); o.name='TMAX_Mid'
me=o.data; lt=np.zeros(len(me.loops),np.int32); me.loops.foreach_get('edge_index',lt); cnt=np.bincount(lt,minlength=len(me.edges))
print('MID tris',len(me.polygons),'boundary',int((cnt==1).sum()),'nonmanifold',int((cnt>2).sum()),'s',round(time.time()-t))
bpy.ops.wm.save_as_mainfile(filepath='tmax/mid.blend')
