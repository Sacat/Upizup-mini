"""Head close-up of an exported bare body (plain FBX import): textured and clay; hair pieces near-black."""
import sys,os; sys.path.insert(0,os.path.dirname(__file__)); import bpy,rlib
fbx,tex,out=[os.path.abspath(p) for p in sys.argv[-3:]]
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=fbx)
img=bpy.data.images.load(tex); mat=bpy.data.materials.new('s'); mat.use_nodes=True; nt=mat.node_tree
t=nt.nodes.new('ShaderNodeTexImage'); t.image=img; nt.links.new(t.outputs[0],nt.nodes['Principled BSDF'].inputs[0])
hm=bpy.data.materials.new('h'); hm.use_nodes=True; hm.node_tree.nodes['Principled BSDF'].inputs[0].default_value=(0.02,0.02,0.022,1)
clay=bpy.data.materials.new('c'); clay.diffuse_color=(0.62,0.62,0.64,1)
M=[o for o in bpy.context.scene.objects if o.type=='MESH']
for o in M: o.data.materials.clear(); o.data.materials.append(hm if 'Hair' in o.name else mat)
cam=rlib.setup_render(360,420,16); rlib.closeup(cam,out+'_tex',(0,0,1.66),0.40,views=(('front',0),('side',90),('back',180),('tq',35)))
for o in M:
    if 'Hair' not in o.name: o.data.materials[0]=clay
rlib.closeup(cam,out+'_clay',(0,0,1.66),0.40,views=(('front',0),('side',90),('tq',35)))
