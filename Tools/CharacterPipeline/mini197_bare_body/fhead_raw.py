import sys,os,math; sys.path.insert(0,os.path.dirname(__file__)); import bpy,rlib,numpy as np, franki_head as FH
bpy.ops.wm.read_factory_settings(use_empty=True)
head,img,hair,hc,d=FH.load(sys.argv[-3],sys.argv[-2],np.array([0,0.015,1.641]))
bpy.data.objects.remove(hair)
c=bpy.data.materials.new('c'); c.diffuse_color=(0.62,0.62,0.64,1); head.data.materials.append(c)
for f in head.data.polygons: f.use_smooth=True
W=np.array([v.co for v in head.data.vertices]); print('z min',W[:,2].min().round(3))
for z in np.arange(1.58,1.70,0.01):
    m=np.abs(W[:,2]-z)<0.005; 
    if m.any(): print(f' z={z:.2f} y[{W[m,1].min():.3f},{W[m,1].max():.3f}] x[{W[m,0].min():.3f},{W[m,0].max():.3f}] n={m.sum()}')
cam=rlib.setup_render(360,420,12); out=sys.argv[-1]
rlib.closeup(cam,out,(0,0,1.66),0.36,views=(('front',0),('side',90),('back',180)))
cam.location=(0,-0.2,1.25); cam.rotation_euler=(math.radians(25),0,0); bpy.context.scene.render.filepath=out+'_below.png'; bpy.ops.render.render(write_still=True)
