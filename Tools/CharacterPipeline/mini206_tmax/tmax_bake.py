import bpy,math,numpy as np,time,os
from mathutils import Vector,Matrix
RES=int(os.environ.get('RES','2048')); T=time.time()
bpy.ops.wm.open_mainfile(filepath='tmax/build_b.blend')
with bpy.data.libraries.load('tmax/scan_final.blend') as (src,dst): dst.objects=['TMAX_Scan_HP']
hp=dst.objects[0]; bpy.context.scene.collection.objects.link(hp)
O=bpy.data.objects; parts=[O[n] for n in ('TMAX_Body','TMAX_FrontWheel','TMAX_RearWheel','TMAX_FrontForkAssembly','TMAX_Handlebar')]
body=O['TMAX_Body']
# fender faces in the fork still carry the scan material -> plain black
pb=bpy.data.materials['p_black']
fk=O['TMAX_FrontForkAssembly']
for i,m in enumerate(fk.data.materials):
    if m and not m.name.startswith('p_'): fk.data.materials[i]=pb
# ---------- UVs: body keeps the scan's own atlas layout; moving parts get a small own atlas ----------
for o in parts[1:]:
    while len(o.data.uv_layers): o.data.uv_layers.remove(o.data.uv_layers[0])
    o.data.uv_layers.new(name='UVMap')
bu=body.data.uv_layers
while len(bu)>1: bu.remove(bu[-1])
bu[0].name='UVMap'; bu.active=bu[0]
# cut caps from the booleans have no UVs (all zero): point them at a dark body texel
SCAN=bpy.data.images['Image']; W=SCAN.size[0]; spx=np.array(SCAN.pixels[:],np.float32).reshape(W,W,4)
lum=spx[...,:3].mean(-1); ys,xs=np.where((lum>0.03)&(lum<0.06)); k=len(ys)//2; dark=((xs[k]+0.5)/W,(ys[k]+0.5)/W)
uvd=np.zeros(len(body.data.loops)*2,np.float32); bu[0].data.foreach_get('uv',uvd); uvd=uvd.reshape(-1,2); ncap=0
for poly in body.data.polygons:
    li=list(poly.loop_indices)
    if np.allclose(uvd[li],0,atol=1e-7): uvd[li]=dark; ncap+=1
bu[0].data.foreach_set('uv',uvd.ravel()); print('cap faces remapped',ncap)
for x in bpy.context.view_layer.objects: x.select_set(False)
for o in parts[1:]: o.select_set(True)
bpy.context.view_layer.objects.active=parts[1]
bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=0.01)
bpy.ops.uv.pack_islands(margin=0.01,rotate=True)
bpy.ops.object.mode_set(mode='OBJECT')
# ---------- images ----------
def img(name,col,res,non_color=False):
    im=bpy.data.images.new(name,res,res,alpha=False,float_buffer=False); im.generated_color=col
    if non_color: im.colorspace_settings.name='Non-Color'
    return im
PR=512
I_col=img('TMAX_Parts_BaseColor',(0.05,0.05,0.05,1),PR); I_met=img('TMAX_Parts_Metal',(0,0,0,1),PR,True); I_smo=img('TMAX_Parts_Smooth',(0.4,0.4,0.4,1),PR,True)
I_nrm=img('TMAX_Body_Normal',(0.5,0.5,1,1),RES,True)
# ---------- emission rigs: each material shows (a) base colour, (b) metallic, (c) smoothness as emission ----------
def rig(mat,mode):
    nt=mat.node_tree; b=next(n for n in nt.nodes if n.type=='BSDF_PRINCIPLED'); out=next(n for n in nt.nodes if n.type=='OUTPUT_MATERIAL')
    em=nt.nodes.get('bake_em') or nt.nodes.new('ShaderNodeEmission'); em.name='bake_em'
    for l in list(em.inputs['Color'].links): nt.links.remove(l)
    key={'col':'Base Color','met':'Metallic','smo':'Roughness'}[mode]
    sock=b.inputs[key]
    if sock.is_linked:
        src_s=sock.links[0].from_socket
        if mode=='smo':
            inv=nt.nodes.get('bake_inv') or nt.nodes.new('ShaderNodeMath'); inv.name='bake_inv'; inv.operation='SUBTRACT'; inv.inputs[0].default_value=1
            nt.links.new(src_s,inv.inputs[1]); nt.links.new(inv.outputs[0],em.inputs['Color'])
        else: nt.links.new(src_s,em.inputs['Color'])
    else:
        v=sock.default_value
        if mode=='col': em.inputs['Color'].default_value=v[:]
        else: x=(1-v) if mode=='smo' else v; em.inputs['Color'].default_value=(x,x,x,1)
    nt.links.new(em.outputs[0],out.inputs['Surface'])
def restore(mat):
    nt=mat.node_tree; b=next(n for n in nt.nodes if n.type=='BSDF_PRINCIPLED'); out=next(n for n in nt.nodes if n.type=='OUTPUT_MATERIAL'); nt.links.new(b.outputs[0],out.inputs['Surface'])
def target_node(o,im):
    for m in o.data.materials:
        nt=m.node_tree; n=nt.nodes.get('bake_target') or nt.nodes.new('ShaderNodeTexImage'); n.name='bake_target'; n.image=im
        for x in nt.nodes: x.select=False
        n.select=True; nt.nodes.active=n
sc=bpy.context.scene; sc.render.engine='CYCLES'; sc.cycles.device='CPU'; sc.cycles.samples=1
bk=sc.render.bake; bk.use_clear=False; bk.margin=4
def bake(o,im,typ,s2a):
    for x in bpy.context.view_layer.objects: x.select_set(False)
    target_node(o,im)
    if s2a: hp.select_set(True)
    o.select_set(True); bpy.context.view_layer.objects.active=o
    bk.use_selected_to_active=s2a; bk.cage_extrusion=0.012; bk.max_ray_distance=0.05
    bpy.ops.object.bake(type=typ,normal_space='TANGENT' if typ=='NORMAL' else 'TANGENT')
hpm=hp.data.materials[0]
for mode,im in (('col',I_col),('met',I_met),('smo',I_smo)):
    for o in parts[1:]:
        for m in o.data.materials: rig(m,mode)
    for o in parts[1:]: bake(o,im,'EMIT',False)
for o in parts[1:]:
    for m in o.data.materials: restore(m)
t=time.time(); bake(body,I_nrm,'NORMAL',True); print('normal',round(time.time()-t),'s')
def ms_image(name,met,smo,res):
    im=bpy.data.images.new(name,res,res,alpha=True); im.colorspace_settings.name='Non-Color'
    px=np.zeros((res,res,4),np.float32); px[...,0]=met; px[...,1]=met; px[...,2]=met; px[...,3]=smo; im.pixels.foreach_set(px.ravel()); return im
pm=np.array(I_met.pixels[:]).reshape(PR,PR,4)[...,0]; ps=np.array(I_smo.pixels[:]).reshape(PR,PR,4)[...,0]
P_ms=ms_image('TMAX_Parts_MetallicSmoothness',pm,ps,PR)
# body maps: the scan's own 4096 textures resampled to 2048 (same UV layout)
def scaled(img_name,new):
    im=bpy.data.images[img_name].copy(); im.name=new; im.scale(RES,RES); return im
B_col=scaled('Image','TMAX_Body_BaseColor')
bm_=scaled('Image.002','tmp_m'); br_=scaled('Image.003','tmp_r')
mm=np.array(bm_.pixels[:]).reshape(RES,RES,4)[...,0]; rr=np.array(br_.pixels[:]).reshape(RES,RES,4)[...,0]
B_ms=ms_image('TMAX_Body_MetallicSmoothness',mm,1-rr,RES)
os.makedirs('tmax/out',exist_ok=True)
outs=(B_col,I_nrm,B_ms,I_col,P_ms)
for im in outs:
    im.filepath_raw=os.path.abspath(f'tmax/out/{im.name}.png'); im.file_format='PNG'; im.save()
def mk(name,col,nrm,ms):
    mat=bpy.data.materials.new(name); mat.use_nodes=True; nt=mat.node_tree; b=nt.nodes['Principled BSDF']
    tc=nt.nodes.new('ShaderNodeTexImage'); tc.image=col; nt.links.new(tc.outputs[0],b.inputs['Base Color'])
    if nrm:
        tn=nt.nodes.new('ShaderNodeTexImage'); tn.image=nrm; nm=nt.nodes.new('ShaderNodeNormalMap'); nt.links.new(tn.outputs[0],nm.inputs['Color']); nt.links.new(nm.outputs[0],b.inputs['Normal'])
    tm=nt.nodes.new('ShaderNodeTexImage'); tm.image=ms; nt.links.new(tm.outputs[0],b.inputs['Metallic'])
    inv=nt.nodes.new('ShaderNodeMath'); inv.operation='SUBTRACT'; inv.inputs[0].default_value=1; nt.links.new(tm.outputs['Alpha'],inv.inputs[1]); nt.links.new(inv.outputs[0],b.inputs['Roughness'])
    return mat
MB=mk('TMAX_Body',B_col,I_nrm,B_ms); MP=mk('TMAX_Parts',I_col,None,P_ms)
body.data.materials.clear(); body.data.materials.append(MB)
for o in parts[1:]: o.data.materials.clear(); o.data.materials.append(MP)
for n in ('tmp_m','tmp_r'): bpy.data.images.remove(bpy.data.images[n])
bpy.data.objects.remove(hp)
bpy.ops.wm.save_as_mainfile(filepath='tmax/build_c.blend')
print('bake total s',round(time.time()-T))
