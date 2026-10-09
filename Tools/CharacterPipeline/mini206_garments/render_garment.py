import bpy,sys,os,math
sys.path.insert(0,os.path.dirname(__file__)); import rlib
argv=sys.argv[sys.argv.index('--')+1:]
blend,out,cz,size=argv[0],argv[1],float(argv[2]),float(argv[3]); col=tuple(float(c) for c in (argv[4] if len(argv)>4 else '0.12,0.16,0.32').split(','))
bpy.ops.wm.open_mainfile(filepath=blend)
for o in list(bpy.data.objects):
    if o.type in ('LIGHT','CAMERA'): bpy.data.objects.remove(o)
skin=bpy.data.materials.new('skinr'); skin.use_nodes=True; skin.node_tree.nodes['Principled BSDF'].inputs[0].default_value=(0.42,0.26,0.17,1)
cl=bpy.data.materials.new('clothr'); cl.use_nodes=True; b=cl.node_tree.nodes['Principled BSDF']; b.inputs[0].default_value=col+(1,); b.inputs['Roughness'].default_value=0.85
for o in bpy.data.objects:
    if o.type!='MESH': continue
    o.hide_render=False
    if o.name.startswith(('SacatBare','FrankiBare','Body','CC_')) or 'Bare' in o.name:
        o.data.materials.clear(); o.data.materials.append(skin)
    else:
        o.data.materials.clear(); o.data.materials.append(cl)
        for p in o.data.polygons: p.use_smooth=True
cam=rlib.setup_render(420,600,16)
rlib.closeup(cam,out,(0,0,cz),size)
