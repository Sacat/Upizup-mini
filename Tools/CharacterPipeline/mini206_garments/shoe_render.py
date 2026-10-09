import bpy,sys,os,numpy as np
sys.path.insert(0,'scripts'); import scene_lib as sl, rlib
from garlib import load_body
A='/home/user/Upizup-mini/Assets/UpIzUpMini/Art/Characters/Garments/Outfits166/'
R='/home/user/Upizup-mini/Docs/CharacterPipeline/MINI-197/BareBody/'
char,fbx,shoe=sys.argv[sys.argv.index('--')+1:]
arm,parts,bm,bvh=load_body(R+fbx)
red=bpy.data.materials.new('red'); red.use_nodes=True; red.node_tree.nodes['Principled BSDF'].inputs[0].default_value=(1,0,0,1)
for p in parts:
    p.data.materials.clear(); p.data.materials.append(red); p.hide_render='Feet' not in p.name and 'Legs' not in p.name
V,F=sl.load_yaml_mesh(A+f'{char}_{shoe}.asset'); Vb=np.c_[-V[:,0],-V[:,2],V[:,1]]
me=bpy.data.meshes.new('shoe'); me.from_pydata(Vb.tolist(),[],F.tolist()); o=bpy.data.objects.new('shoe',me); bpy.context.scene.collection.objects.link(o)
w=bpy.data.materials.new('w'); w.use_nodes=True; w.node_tree.nodes['Principled BSDF'].inputs[0].default_value=(0.9,0.9,0.9,1); me.materials.append(w)
cam=rlib.setup_render(360,300,8); rlib.closeup(cam,f'g2/shoe_{char}_{shoe}',(0.11,0,0.08),0.36,views=(('front',0),('side',90),('back',180),('tq',35)))
