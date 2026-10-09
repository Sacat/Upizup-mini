"""MINI-206 stage 2: garments for one character -> skinned FBX per garment + white AO tint texture + pose-test renders."""
import bpy,sys,os,math,json,numpy as np,importlib
argv=sys.argv[sys.argv.index('--')+1:]; FBX,OUT,CHAR=argv[:3]; os.makedirs(OUT,exist_ok=True)
sys.argv=['x','--',FBX,OUT,CHAR]; sys.path.insert(0,os.path.dirname(__file__))
import build_garments as bg; from garlib import skin_from_body,pose_bone_world,tris,activate
import rlib
arm,parts,bm,bvh,L=bg.setup()
G={}
for kind in ('tee','polo','jeans','trousers','shorts'):
    g=bg.top_garment(arm,parts,bm,bvh,L,kind) if kind in ('tee','polo') else bg.bottom_garment(arm,parts,bm,bvh,L,kind)
    g.name=f'{CHAR}_{kind}'; g.data.name=g.name
    for p in g.data.polygons: p.use_smooth=True
    G[kind]=g; print('BUILT',kind,tris(g))
# UVs + white AO texture per garment
sc=bpy.context.scene; sc.render.engine='CYCLES'; sc.cycles.device='CPU'; sc.cycles.samples=24
for kind,g in G.items():
    for o in G.values(): o.hide_render=(o is not g)
    activate(g); bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=0.004); bpy.ops.object.mode_set(mode='OBJECT')
    im=bpy.data.images.new(f'{g.name}_AO',1024,1024); im.generated_color=(1,1,1,1)
    mat=bpy.data.materials.new(f'{g.name}_mat'); mat.use_nodes=True; nt=mat.node_tree; tn=nt.nodes.new('ShaderNodeTexImage'); tn.image=im; nt.nodes.active=tn
    g.data.materials.clear(); g.data.materials.append(mat)
    sc.render.bake.margin=6; sc.render.bake.use_selected_to_active=False
    bpy.ops.object.bake(type='AO')
    px=np.array(im.pixels[:]).reshape(-1,4); ao=px[:,0]
    w=np.clip(0.30+0.70*ao,0,1)**0.8*0.96          # soft AO on white: tint-friendly
    px[:,0]=px[:,1]=px[:,2]=w; px[:,3]=1; im.pixels.foreach_set(px.astype(np.float32).ravel())
    im.filepath_raw=os.path.join(OUT,f'{g.name}_BaseColor.png'); im.file_format='PNG'; im.save()
    b=nt.nodes['Principled BSDF']; nt.links.new(tn.outputs[0],b.inputs['Base Color']); b.inputs['Roughness'].default_value=0.85
for o in G.values(): o.hide_render=False
# skin
for kind,g in G.items():
    skin_from_body(g,parts,arm,smooth_iters=int(os.environ.get('WSMOOTH','0')))
# export: armature + one garment each
def export(objs,path):
    for x in bpy.context.view_layer.objects: x.select_set(False)
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active=objs[0]
    bpy.ops.export_scene.fbx(filepath=path,use_selection=True,object_types={'ARMATURE','MESH'},apply_unit_scale=True,bake_space_transform=False,add_leaf_bones=False,
        use_armature_deform_only=False,use_mesh_modifiers=False,mesh_smooth_type='OFF',bake_anim=False,path_mode='STRIP')
rep={}
for kind,g in G.items():
    export([arm,g],os.path.join(OUT,f'{g.name}.fbx'))
    nw=sum(1 for v in g.data.vertices if len(v.groups)==0)
    rep[kind]={'tris':tris(g),'verts':len(g.data.vertices),'unweighted_verts':nw,'max_influences':max(len(v.groups) for v in g.data.vertices)}
json.dump(rep,open(os.path.join(OUT,f'{CHAR}_garments_report.json'),'w'),indent=1); print('REPORT',rep)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,f'{CHAR}_Garments.blend'),compress=True)
# ---------- pose tests ----------
skin=bpy.data.materials.new('skin'); skin.use_nodes=True; skin.node_tree.nodes['Principled BSDF'].inputs[0].default_value=(0.42,0.26,0.17,1)
for p in parts: p.data.materials.clear(); p.data.materials.append(skin)
TINT={'tee':(0.12,0.16,0.32),'polo':(0.85,0.85,0.82),'jeans':(0.10,0.16,0.30),'trousers':(0.18,0.17,0.15),'shorts':(0.35,0.42,0.55)}
for kind,g in G.items():
    n=g.data.materials[0].node_tree.nodes['Principled BSDF']; mix=g.data.materials[0].node_tree.nodes.new('ShaderNodeMix'); mix.data_type='RGBA'; mix.blend_type='MULTIPLY'; mix.inputs[0].default_value=1
    tex=[x for x in g.data.materials[0].node_tree.nodes if x.type=='TEX_IMAGE'][0]
    g.data.materials[0].node_tree.links.new(tex.outputs[0],mix.inputs[6]); mix.inputs[7].default_value=(*TINT[kind],1)
    g.data.materials[0].node_tree.links.new(mix.outputs[2],n.inputs['Base Color'])
def rest():
    for pb in arm.pose.bones: pb.matrix_basis.identity()
    bpy.context.view_layer.update()
P='CC_Base_'
def stride():
    pose_bone_world(arm,P+'L_Thigh',(1,0,0),-28); pose_bone_world(arm,P+'L_Calf',(1,0,0),18)
    pose_bone_world(arm,P+'R_Thigh',(1,0,0),18); pose_bone_world(arm,P+'R_Calf',(1,0,0),35)
    pose_bone_world(arm,P+'L_Upperarm',(0,1,0),70); pose_bone_world(arm,P+'R_Upperarm',(0,1,0),-70)
def pistol():
    pose_bone_world(arm,P+'L_Upperarm',(0,0,1),-80); pose_bone_world(arm,P+'R_Upperarm',(0,0,1),80)
    pose_bone_world(arm,P+'L_Upperarm',(0,1,0),12); pose_bone_world(arm,P+'R_Upperarm',(0,1,0),-12)
    pose_bone_world(arm,P+'L_Forearm',(0,0,1),-15); pose_bone_world(arm,P+'R_Forearm',(0,0,1),15)
def seated():
    for s in ('L','R'): pose_bone_world(arm,P+s+'_Thigh',(1,0,0),-85); pose_bone_world(arm,P+s+'_Calf',(1,0,0),85)
    pose_bone_world(arm,P+'L_Upperarm',(0,0,1),-55); pose_bone_world(arm,P+'R_Upperarm',(0,0,1),55)
    pose_bone_world(arm,P+'L_Upperarm',(0,1,0),35); pose_bone_world(arm,P+'R_Upperarm',(0,1,0),-35)
    pose_bone_world(arm,P+'L_Forearm',(0,0,1),-25); pose_bone_world(arm,P+'R_Forearm',(0,0,1),25)
cam=rlib.setup_render(360,560,12)
OUTFITS={'A_tee_jeans':('tee','jeans'),'B_polo_trousers':('polo','trousers'),'C_tee_shorts':('tee','shorts')}
for on,ks in OUTFITS.items():
    for k,g in G.items(): g.hide_render=k not in ks
    for pn,fn in (('rest',None),('stride',stride),('pistol',pistol),('seated',seated)):
        rest(); 
        if fn: fn()
        rlib.closeup(cam,os.path.join(OUT,f'{CHAR}_{on}_{pn}'),(0,0,0.95),1.15,views=(('front',0),('tq',35)) if pn!='rest' else (('front',0),('side',90),('back',180),('tq',35)))
rest()
