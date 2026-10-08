"""MINI-197 cloud lane: bare modular body from the MINI-105 AccuRig donor (vest + boxers sculpted in).

Stages are cumulative:
  legs  - boxers removed: hems/waistband relaxed, groin rebuilt as a smooth surface, skin filled from the thighs
  hands - hand/forearm/face skin tone evened out
  neck  - vest removed: neckline, straps and hem relaxed, clean neck/shoulder join, bare torso skin
Pipeline: donor -> voxel remesh (closed surface) -> region smoothing -> symmetric decimate (~20k tris)
          -> weights transferred from the AccuRig donor -> UV + 2K base colour baked from donor -> modular split -> FBX.
Usage: python build_bare_body.py <donor.fbx> <basecolor.png> <stage> <out_prefix> [skin_tint r,g,b]"""
import bpy,sys,os,numpy as np,math
sys.path.insert(0,os.path.dirname(__file__)); import rlib,bodylib as B
from scipy.spatial import cKDTree
argv=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[-4:]
fbx,tex,stage,out=argv[:4]; tex=os.path.abspath(tex); out=os.path.abspath(out); fbx=os.path.abspath(fbx)
TINT=np.array([float(t) for t in argv[4].split(',')]) if len(argv)>4 else None
NAME=os.environ.get('BODY_NAME','Sacat')
STAGES=['legs','hands','neck']; upto=STAGES.index(stage)
VOXEL=0.003; TARGET_TRIS=int(os.environ.get('BODY_TRIS','20000'))

# ---------- 1. donor: masks and skin colour (bake source) ----------
arm,donor=B.load(fbx); B.weld(donor)
FHEAD=os.environ.get('FRANKI_HEAD'); fhead=None
# heights are in the slanted frame z' = z - 0.36*y that follows Franki's neck-stub edge (front 1.60 m, back 1.64 m)
NECK_A=float(os.environ.get('NECK_A','1.585'))   # donor neck kept below z'=NECK_A (under Sacat's jaw)
NECK_B=float(os.environ.get('NECK_B','1.628'))   # Franki head kept above z'=NECK_B (just above its stub edge)
HEAD_CUT=NECK_B
if FHEAD:
    import franki_head as FH
    fhead,fimg,fhair,fhaircol,fdelta=FH.load(FHEAD,os.environ['FRANKI_HAIR'],B.bone_world(arm,'CC_Base_Head',donor))
    fm=bpy.data.materials.new('FrankiHeadBake'); fm.use_nodes=True; nt_=fm.node_tree
    ti=nt_.nodes.new('ShaderNodeTexImage'); ti.image=fimg; em_=nt_.nodes.new('ShaderNodeEmission')
    nt_.links.new(ti.outputs[0],em_.inputs[0]); nt_.links.new(em_.outputs[0],nt_.nodes['Material Output'].inputs[0])
    fhead.data.materials.append(fm); fhead.hide_render=True
px=B.tex_pixels(tex); col,_,_=B.vert_colors(donor,px); Wd=B.world_co(donor); Ad=B.adjacency(donor)
s,v=B.hsv(col); z=Wd[:,2]
boxer=B.close_mask(Ad,((v<0.22)&(s<0.45)&(z>0.70)&(z<1.10))|((v<0.30)&(z>0.78)&(z<1.08)&(np.abs(Wd[:,0])<0.20)),4)
vest=B.close_mask(Ad,(v>0.62)&(s<0.18)&(z>0.95)&(z<1.58),3)
fill=B.dilate(Ad,boxer,3)
if upto>=2: fill|=B.dilate(Ad,vest,4)
unknown=B.dilate(Ad,boxer|vest,4)|((s<0.22)&(z>0.6)&(z<1.6))      # sample skin only from clean skin
if fhead is not None:
    # Sacat's low nape hairline / stubble would bake onto Franki's neck: refill the upper donor neck from the shoulders
    neckfill=(FH.shear(Wd)[:,2]>NECK_A-0.05)&(np.abs(Wd[:,0])<0.13)&(Wd[:,2]>1.45)
    fill|=neckfill; unknown|=neckfill|(FH.shear(Wd)[:,2]>NECK_A-0.05)&(np.abs(Wd[:,0])<0.16)&(Wd[:,2]>1.45)
if upto>=1: unknown|=np.abs(Wd[:,0])>0.745
lin=B.srgb2lin(col)
skin=B.harmonic_fill(Ad,unknown,lin)
skin=np.where(fill[:,None],skin,lin)
if upto>=1:
    Nd=B.vertex_normals(donor,Wd); palm=np.clip(-Nd[:,2],0,1)*np.clip((np.abs(Wd[:,0])-0.75)/0.03,0,1)
    skin=skin*(1+0.22*palm)[:,None]
corr=np.ones((len(Wd),3))   # multiplicative large-scale tone correction (1 = unchanged)
import hands as H
HANDJ={}; thumbv=np.zeros(len(Wd),bool); cutv=np.zeros(len(Wd),bool)
if upto>=1:
    gn={g.index:g.name for g in donor.vertex_groups}
    domb=np.array([gn[max(vv.groups,key=lambda g:g.weight).group] if len(vv.groups) else '' for vv in donor.data.vertices])
    for side,sg in (('L',1),('R',-1)):
        HANDJ[side]=H.design(lambda n: B.bone_world(arm,n,donor),side)
        wrx=B.bone_world(arm,f'CC_Base_{side}_Hand',donor)[0]*sg
        th=np.array([d.startswith(f'CC_Base_{side}_Index') or d.startswith(f'CC_Base_{side}_Thumb') for d in domb])
        thumbv|=th
        cutv|=(~th)&(Wd[:,0]*sg>wrx+0.090)     # fused fingers beyond the knuckle line (thumb kept)
        print(side,'wrist x',round(wrx,3),'cut verts',int(((~th)&(Wd[:,0]*sg>wrx+0.090)).sum()))
    # hand colour: fill from the forearm tone; palms (facing -Z in T-pose) a little lighter, as on real darker skin
    handc=np.abs(Wd[:,0])>0.755
    fill|=handc
if upto>=1:
    # skin tone: arms read lighter than face/legs and the neck darker. Even out the LOW-frequency tone only,
    # keeping texture detail: corr = target / smoothed_skin_colour on skin vertices (hair/eyes/brows excluded).
    skinv=(s>0.25)&(v>0.25)&~B.dilate(Ad,boxer|vest,2)
    import scipy.sparse as sp
    deg=np.asarray(Ad.sum(1)).ravel(); P=sp.diags(1/np.maximum(deg,1))@Ad
    num=np.where(skinv[:,None],skin,0.0); den=skinv.astype(float)[:,None]
    for _ in range(int(os.environ.get('TONE_SMOOTH','400'))): num=P@num; den=P@den
    low=num/np.maximum(den,1e-6)
    target=skin[skinv].mean(0) if TINT is None else B.srgb2lin(TINT)
    k=float(os.environ.get('TONE_STRENGTH','0.85'))
    c=np.clip((target/np.maximum(low,1e-4))**k,0.6,1.6)
    # only the scalp hair keeps its colour; dark facial features (lids, brows, nostrils) take the same smooth
    # correction as the skin around them, otherwise they show as angular dark patches after the tone shift
    hairish=((v<0.18)|(s<0.18))&((Wd[:,2]>1.715)|((Wd[:,2]>1.60)&(Wd[:,1]>0.035)))
    corr=np.where(hairish[:,None],1.0,c)
    for name,m in (('face',(np.abs(Wd[:,0])<0.05)&(Wd[:,2]>1.62)),('arm',np.abs(Wd[:,0])>0.3),('hand',np.abs(Wd[:,0])>0.74),('thigh',(Wd[:,2]>0.6)&(Wd[:,2]<0.75))):
        print('tone corr',name,corr[m&skinv].mean(0).round(3))
me=donor.data
skin=skin*np.where(fill[:,None],corr,1.0)
AREOLA=[]
if upto>=1:
    # filled clothing areas: use the exact equalised target tone (the rest of the skin is pulled to it too), so the
    # old vest/boxer outline does not show as a paler 'bodysuit'. Fine variation is added in the shader.
    bodyfill=fill&~(np.abs(Wd[:,0])>0.745)
    # start from the neighbouring skin at the edge (harmonic fill) and ease to the body tone ~5 cm inside,
    # so the old fabric outline does not read as a seam
    dfill=B.ring_distance(Ad,bodyfill,8); w=np.clip((dfill-1)/7,0,1); w=(w*w*(3-2*w))[:,None]
    skin=np.where(bodyfill[:,None],skin*(1-w)+target[None,:]*float(os.environ.get('FILL_GAIN','0.97'))*w,skin)
if upto>=2:
    # areolae on the pectorals: most forward chest point per side between z 1.26 and 1.36
    for sg in (1,-1):
        # male nipple spacing ~19 cm on a 1.85 m body (about 4th intercostal space, ~1.31 m here)
        m=(np.abs(Wd[:,0]*sg-0.095)<0.012)&(Wd[:,2]>1.29)&(Wd[:,2]<1.33)
        i=np.where(m)[0][np.argmin(Wd[m,1])]; AREOLA.append(Wd[i]); print('areola',Wd[i].round(3))
ca=me.color_attributes.new('tone_corr','FLOAT_COLOR','POINT'); ca.data.foreach_set('color',np.c_[corr,np.ones(len(Wd))].ravel())
a=me.color_attributes.new('skin_fill','FLOAT_COLOR','POINT'); a.data.foreach_set('color',np.c_[skin,np.ones(len(Wd))].ravel())
soft=np.clip(B.ring_distance(Ad,fill,3)/3,0,1)         # feathered edge: 1/3, 2/3, 1 over the first rings inside
mk=me.attributes.new('fill_mask','FLOAT','POINT'); mk.data.foreach_set('value',soft.astype(float))
img=bpy.data.images.load(tex)
def skin_material(name,use_tex=True):
    mat=bpy.data.materials.new(name); mat.use_nodes=True; nt=mat.node_tree; bsdf=nt.nodes['Principled BSDF']
    bsdf.inputs['Roughness'].default_value=0.55
    t=nt.nodes.new('ShaderNodeTexImage'); t.image=img
    c=nt.nodes.new('ShaderNodeAttribute'); c.attribute_name='skin_fill'
    m=nt.nodes.new('ShaderNodeAttribute'); m.attribute_name='fill_mask'
    mix=nt.nodes.new('ShaderNodeMix'); mix.data_type='RGBA'
    tc=nt.nodes.new('ShaderNodeAttribute'); tc.attribute_name='tone_corr'
    mul=nt.nodes.new('ShaderNodeMix'); mul.data_type='RGBA'; mul.blend_type='MULTIPLY'; mul.inputs['Factor'].default_value=1.0
    nt.links.new(t.outputs[0],mul.inputs[6]); nt.links.new(tc.outputs[0],mul.inputs[7])
    nt.links.new(m.outputs['Fac'],mix.inputs['Factor']); nt.links.new(mul.outputs[2],mix.inputs[6]); nt.links.new(c.outputs[0],mix.inputs[7])
    src=mix.outputs[2]
    if upto>=1:
        tcn=nt.nodes.new('ShaderNodeTexCoord')
        def noise(scale,lo,hi):
            n=nt.nodes.new('ShaderNodeTexNoise'); n.inputs['Scale'].default_value=scale; n.inputs['Detail'].default_value=6
            nt.links.new(tcn.outputs['Object'],n.inputs['Vector'])
            r=nt.nodes.new('ShaderNodeMapRange'); r.inputs['To Min'].default_value=lo; r.inputs['To Max'].default_value=hi
            nt.links.new(n.outputs['Fac'],r.inputs['Value']); return r.outputs['Result']
        def mul(a,b):
            mm=nt.nodes.new('ShaderNodeMix'); mm.data_type='RGBA'; mm.blend_type='MULTIPLY'; mm.inputs['Factor'].default_value=1.0
            nt.links.new(a,mm.inputs[6]); nt.links.new(b,mm.inputs[7]) if not isinstance(b,float) else None; return mm.outputs[2]
        var=mul(noise(0.35,0.93,1.07),noise(6.0,0.96,1.04))             # ~3 cm mottling x ~1.5 mm grain
        fv=nt.nodes.new('ShaderNodeMix'); fv.data_type='RGBA'                # only inside the filled regions
        nt.links.new(m.outputs['Fac'],fv.inputs['Factor']); fv.inputs[6].default_value=(1,1,1,1); nt.links.new(var,fv.inputs[7])
        src=mul(src,fv.outputs[2])
        Mi=np.linalg.inv(np.array(donor.matrix_world))
        for P in AREOLA:
            pl=(Mi[:3,:3]@P+Mi[:3,3])
            vm=nt.nodes.new('ShaderNodeVectorMath'); vm.operation='DISTANCE'; vm.inputs[1].default_value=tuple(pl)
            nt.links.new(tcn.outputs['Object'],vm.inputs[0])
            for rad,dark in ((1.35,0.74),(0.45,0.80)):                       # areola 13.5 mm, nipple 4.5 mm (cm units)
                r=nt.nodes.new('ShaderNodeMapRange'); r.inputs['From Min'].default_value=rad*0.75; r.inputs['From Max'].default_value=rad
                r.inputs['To Min'].default_value=dark; r.inputs['To Max'].default_value=1.0
                nt.links.new(vm.outputs['Value'],r.inputs['Value']); src=mul(src,r.outputs['Result'])
    em=nt.nodes.new('ShaderNodeEmission'); nt.links.new(src,em.inputs[0])
    outn=nt.nodes['Material Output']; nt.links.new(em.outputs[0],outn.inputs[0])   # emission: bake source = pure albedo
    return mat
me.materials.clear(); me.materials.append(skin_material('DonorBake'))
# close holes in the donor (open fabric edges) so bake rays never miss
import bmesh
bm=bmesh.new(); bm.from_mesh(me)
nb=len([e for e in bm.edges if e.is_boundary])
# not on the head: eye/mouth openings capped with UV-less n-gons bake as dark angular smudges around the eyes
bmesh.ops.holes_fill(bm,edges=[e for e in bm.edges if e.is_boundary and min(v.co[2] for v in e.verts)*donor.matrix_world[2][2]<1.52],sides=0)
bm.to_mesh(me); bm.free(); print('donor boundary edges filled (below the neck)',nb)
if os.environ.get('DEBUG_DONOR'):
    cam=rlib.setup_render(400,400,8); rlib.closeup(cam,out+'_donorhip',(0,0,0.92),0.55,views=(('front',0),('back',180))); sys.exit(0)

# ---------- 2. closed surface ----------
polys=B.tri_polys(donor.data); Vsrc=Wd
if fhead is not None:
    FW=B.world_co(fhead); fpolys=[p.vertices[:] for p in fhead.data.polygons]
    BV,BF=FH.neck_bridge_tilted(Wd,B.tri_polys(donor.data),NECK_A,FW,fpolys,NECK_B)
    Zd=FH.shear(Wd)[:,2]; Zf=FH.shear(FW)[:,2]
    polys=[p for p in polys if not any(Zd[i]>NECK_A+0.006 for i in p)]
    base=len(Vsrc); polys=polys+[tuple(i+base for i in p) for p in fpolys if not any(Zf[i]<NECK_B-0.004 for i in p)]
    Vsrc=np.vstack([Vsrc,FW]); base=len(Vsrc); polys=polys+[tuple(i+base for i in f) for f in BF]; Vsrc=np.vstack([Vsrc,BV])
    print('neck bridge',NECK_A,'->',NECK_B,'rings',len(BV)//72)
    # bake source for the bridge: the donor's neck skin tone just below the cut (no texture exists there)
    nb=B.mesh_from_world('NeckBridgeBake',BV,BF); nm=bpy.data.materials.new('NeckBridgeBake'); nm.use_nodes=True
    neckc=(np.abs(FH.shear(Wd)[:,2]-(NECK_A-0.01))<0.01)&(np.abs(Wd[:,0])<0.08)
    nc=skin[neckc].mean(0)*corr[neckc].mean(0) if 'skin' in dir() else np.array([0.2,0.1,0.06])
    # Franki's stub bottom is painted dark (old hoodie-collar shading): fade his texture into the neck tone over 3.5 cm
    zf=FH.shear(B.world_co(fhead))[:,2]; nmx=np.clip((NECK_B+0.035-zf)/0.035,0,1); nmx=nmx*nmx*(3-2*nmx)
    at_=fhead.data.attributes.new('neckmix','FLOAT','POINT'); at_.data.foreach_set('value',nmx)
    fnt=fhead.data.materials[0].node_tree; emn=[n for n in fnt.nodes if n.type=='EMISSION'][0]; tin=[n for n in fnt.nodes if n.type=='TEX_IMAGE'][0]
    a2=fnt.nodes.new('ShaderNodeAttribute'); a2.attribute_name='neckmix'; mx2=fnt.nodes.new('ShaderNodeMix'); mx2.data_type='RGBA'
    fnt.links.new(a2.outputs['Fac'],mx2.inputs['Factor']); fnt.links.new(tin.outputs[0],mx2.inputs[6]); mx2.inputs[7].default_value=(*nc,1.0)
    fnt.links.new(mx2.outputs[2],emn.inputs[0])
    e_=nm.node_tree.nodes.new('ShaderNodeEmission'); e_.inputs[0].default_value=(*nc,1.0)
    nm.node_tree.links.new(e_.outputs[0],nm.node_tree.nodes['Material Output'].inputs[0]); nb.data.materials.append(nm); nb.hide_render=True
if upto>=1:
    polys=[p for p in polys if not all(i<len(cutv) and cutv[i] for i in p)]
    extra_v=[];extra_f=[]; base=len(Vsrc)
    for side,J in HANDJ.items():
        for f,(dy,dx,L,w,h,spread) in H.FINGERS.items():
            fv,ff=H.finger_mesh(J[f],w,h); extra_f+= [tuple(i+base+sum(len(e) for e in extra_v) for i in fc) for fc in ff]; extra_v.append(fv)
    Vsrc=np.vstack([Vsrc]+extra_v); polys=polys+extra_f
if os.environ.get('DEBUG_SRC') and fhead is not None:
    cols=[(0.8,0.5,0.4,1),(0.3,0.5,0.9,1),(0.3,0.8,0.3,1)]
    nD=len(Wd); nF=len(FW)
    parts=[[p for p in polys if max(p)<nD],[tuple(i-nD for i in p) for p in polys if nD<=min(p) and max(p)<nD+nF],[tuple(i-nD-nF for i in p) for p in polys if min(p)>=nD+nF and max(p)<nD+nF+len(BV)]]
    for k,(VV,PP) in enumerate(((Wd,parts[0]),(FW,parts[1]),(BV,parts[2]))):
        o=B.mesh_from_world(f'dbg{k}',VV,PP); mt=bpy.data.materials.new(f'd{k}'); mt.diffuse_color=cols[k]; o.data.materials.append(mt)
    donor.hide_render=True; fhead.hide_render=True; nb.hide_render=True
    cam=rlib.setup_render(400,400,8); rlib.closeup(cam,out+'_src',(0,0,1.60),0.32,views=(('front',0),('side',90),('back',180))); sys.exit(0)
rm=B.mesh_from_world('rm',Vsrc,polys); rm.data.remesh_voxel_size=VOXEL
if upto>=1 or fhead is not None:
    import bmesh as _bm
    bm=_bm.new(); bm.from_mesh(rm.data); nb0=sum(e.is_boundary for e in bm.edges)
    if fhead is not None:   # neck openings (donor cut, bridge ends, Franki stub): fan caps, holes_fill fails on these
        ncap=B.fan_cap_loops(bm,lambda e: all(1.50<v.co[2]<1.72 and abs(v.co[0])<0.16 for v in e.verts))
        print('neck loops fan-capped',ncap)
    _bm.ops.holes_fill(bm,edges=[e for e in bm.edges if e.is_boundary],sides=0)
    print('remesh source boundary edges',nb0,'->',sum(e.is_boundary for e in bm.edges),'non-manifold',sum(len(e.link_faces)>2 for e in bm.edges))
    if os.environ.get('DEBUG_OPEN'):
        mids=np.array([[(e.verts[0].co[k]+e.verts[1].co[k])/2 for k in range(3)] for e in bm.edges if e.is_boundary])
        for lo,hi in ((0,0.5),(0.5,1.0),(1.0,1.4),(1.4,1.55),(1.55,1.62),(1.62,1.7),(1.7,1.9)):
            m=(mids[:,2]>=lo)&(mids[:,2]<hi); print(f'  open edges z[{lo},{hi}) n={m.sum()}', (mids[m].min(0).round(3),mids[m].max(0).round(3)) if m.any() else '')
    bm.to_mesh(rm.data); bm.free()
B.activate(rm); bpy.ops.object.voxel_remesh()
# drop internal shells the remesh leaves inside old fabric folds (they poke through after smoothing)
import bmesh, scipy.sparse.csgraph as cg
nc,lab=cg.connected_components(B.adjacency(rm)); keep=np.bincount(lab).argmax()
bm=bmesh.new(); bm.from_mesh(rm.data); bm.verts.ensure_lookup_table()
bmesh.ops.delete(bm,geom=[v for v in bm.verts if lab[v.index]!=keep],context='VERTS'); bm.to_mesh(rm.data); bm.free()
print('remesh components',nc,'kept',np.bincount(lab).max())
if os.environ.get('DEBUG_RM'):
    donor.hide_render=True
    if fhead is not None: fhead.hide_render=True; nb.hide_render=True; fhair.hide_render=True
    rm.data.materials.append(bpy.data.materials.new('c'))
    cam=rlib.setup_render(400,400,8); rlib.closeup(cam,out+'_rm',(0,0,1.60),0.36,views=(('front',0),('side',90),('back',180)))
    rlib.closeup(cam,out+'_rmfull',(0,0,0.93),2.0,views=(('front',0),)); sys.exit(0)
W=B.world_co(rm); A=B.adjacency(rm)
tree=cKDTree(Wd); _,nn=tree.query(W)
rbox=B.close_mask(A,boxer[nn],2); rvest=B.close_mask(A,vest[nn],2)
x,y,z=W[:,0],W[:,1],W[:,2]
def shell_inset(mask,thick,band=5):
    # fabric shells sit 'thick' proud of the skin: push the whole shell in, blend only across the edge band outside it
    global W
    out=B.ring_distance(A,~mask,band)        # 1..band rings outside the mask (0 inside / far)
    w=np.where(mask,1.0,np.where(out>0,1-np.clip(out/band,0,1),0.0))
    # smooth the weight so the transition has no kink
    import scipy.sparse as sp
    deg=np.asarray(A.sum(1)).ravel(); P=sp.diags(1/np.maximum(deg,1))@A
    for _ in range(6): w=0.5*w+0.5*(P@w)
    W=W-B.vertex_normals(rm,W)*(thick*w)[:,None]
    return B.dilate(A,mask,band+3)
def relax(region,iters,ramp=6,inset=0.0,mu=-0.53):
    global W
    d=B.ring_distance(A,region,ramp); wgt=np.clip(d/ramp,0,1)
    if inset: W=W-B.vertex_normals(rm,W)*(inset*wgt)[:,None]
    # stable form: hard-boundary Taubin on the core, then a diffusion-only blend across the edge band
    W=B.taubin(W,A,region&(wgt>=1),iters=iters,mu=mu)
    band=B.dilate(A,region&(wgt<1),2)
    W=B.laplacian(W,A,band,iters=max(10,ramp*3),weight=np.clip(B.ring_distance(A,band,4)/4,0,1))
# ---------- 3. legs: boxers -> bare hips ----------
# Conservative: keep the donor's hip/thigh/glute volume (tight boxers ~= body). Only:
#  (1) remove the fabric thickness (1.5 mm) with a soft edge,
#  (2) rebuild a geometric band +-HEM_R around each leg opening as the smoothest surface between the real
#      thigh below and the real hip above (biharmonic; boundary = untouched surface on both sides -> no shrink),
#  (3) flatten the groin bulge, (4) light wrinkle smoothing.
edge=shell_inset(B.dilate(A,rbox,int(os.environ.get('BOXER_GROW','4'))),float(os.environ.get('BOXER_T','0.0015')))
hemv=rbox&B.dilate(A,~rbox,1)&(z<0.90)                       # leg-opening boundary vertices
HEM_R=float(os.environ.get('HEM_R','0.022'))
hem=np.zeros(len(W),bool)
for sg in (1,-1):
    hv=W[hemv&(np.sign(x)==sg)]
    if len(hv)==0: continue
    # asymmetric: the detected hem line follows the fold underside; the visible lip sits 2-3 cm ABOVE it
    dd,ii=cKDTree(hv).query(W); dz=W[:,2]-hv[ii,2]
    hem|=(dd<float(os.environ.get('HEM_UP','0.045')))&(dz>-HEM_R)&(dz<float(os.environ.get('HEM_UP','0.045')))&(np.sign(x)==sg)
W0=W.copy(); W=B.biharmonic_fill(W,A,hem); print('hem band verts',int(hem.sum()),'max move mm',round(1000*np.linalg.norm(W-W0,axis=1).max(),1))
if os.environ.get('DEBUG_BAND'):
    B.set_world_co(rm,W0); cattr=rm.data.color_attributes.new('h','FLOAT_COLOR','POINT')
    cc=np.tile([0.7,0.7,0.7,1.0],(len(W),1)); cc[rbox]=[0.85,0.45,0.45,1]; cc[hem]=[0.3,0.4,0.95,1]; cc[hemv]=[1,1,0,1]; cattr.data.foreach_set('color',cc.ravel())
    mt=bpy.data.materials.new('h'); mt.use_nodes=True; at=mt.node_tree.nodes.new('ShaderNodeAttribute'); at.attribute_name='h'
    mt.node_tree.links.new(at.outputs[0],mt.node_tree.nodes['Principled BSDF'].inputs[0]); rm.data.materials.append(mt); donor.hide_render=True
    for f in rm.data.polygons: f.use_smooth=True
    cam=rlib.setup_render(400,400,8); rlib.closeup(cam,out+'_band',(0,0,0.88),0.5,views=(('front',0),('side',90),('back',180))); sys.exit(0)
groin=(np.abs(x)<0.07)&(z>0.80)&(z<0.975)&(y<0.0)&B.dilate(A,rbox,6)
W=B.biharmonic_fill(W,A,B.dilate(A,groin,2))
relax(B.dilate(A,groin,8),40,ramp=8)
relax(B.dilate(A,rbox,3)&(z<1.0),int(os.environ.get('LOWPASS_IT','40')),ramp=12,mu=-0.505)
# ---------- 4. neck: vest -> bare torso, clean neck/shoulder join ----------
if upto>=2:
    # Same conservative recipe as the legs: the vest is tight, so keep the torso volume and only
    #  (1) remove 1.5 mm of fabric, (2) rebuild a band around each vest edge (bottom hem over the waistband,
    #  neckline, armholes) by a biharmonic fill anchored to the real surface on both sides,
    #  (3) low-pass the ribbing, (4) a light pass across the neck/shoulder join.
    edge=shell_inset(B.dilate(A,rvest,int(os.environ.get('VEST_GROW','3'))),float(os.environ.get('VEST_T','0.0015')))
    vb=rvest&B.dilate(A,~rvest,1)
    hemb=vb&(z<1.10); neckb=vb&(z>1.38)&(np.abs(x)<0.13); armb=vb&~hemb&~neckb
    band=np.zeros(len(W),bool)
    for m,r_ in ((hemb,float(os.environ.get('VEST_HEM_R','0.032'))),(neckb,0.016),(armb,0.018)):
        if m.any(): dd,_=cKDTree(W[m]).query(W); band|=dd<r_
    band&=(np.abs(x)<0.32)&(z>0.90)
    W0=W.copy(); W=B.biharmonic_fill(W,A,band)
    print('vest band verts',int(band.sum()),'max move mm',round(1000*np.linalg.norm(W-W0,axis=1).max(),1))
    relax(B.dilate(A,rvest,2)&~band,int(os.environ.get('VEST_LOWPASS','300')),ramp=8,mu=-0.505)
    # small slits / label bumps left from the vest (high local Laplacian on the torso)
    import scipy.sparse as sp
    deg=np.asarray(A.sum(1)).ravel(); Pm=sp.diags(1/np.maximum(deg,1))@A
    lap=np.linalg.norm(Pm@W-W,axis=1); spike=(lap>0.0010)&(z>0.95)&(z<1.62)&(np.abs(x)<0.26)
    print('torso spikes',int(spike.sum()))
    if spike.any(): relax(B.dilate(A,spike,4),40,ramp=3)
    join=(z>1.40)&(z<1.58)&(np.abs(x)<0.20)
    relax(join,30,ramp=10)
if upto>=1:
    # back of the hand: the donor dorsum is as thick as the old fused finger block; taper it down to finger height
    # over the last 3.5 cm before the knuckles so the fingers do not emerge under a ledge
    for side,sg in (('L',1),('R',-1)):
        wr=B.bone_world(arm,f'CC_Base_{side}_Hand',donor); X=W[:,0]*sg
        zc=wr[2]+H.MCP_DZ; ztop=zc+0.0095
        t=np.clip((X-(wr[0]*sg+0.065))/0.035,0,1); t=t*t*(3-2*t)
        m=(X>wr[0]*sg+0.06)&(X<wr[0]*sg+0.125)&(W[:,2]>ztop)&(np.abs(W[:,1]-wr[1])<0.07)
        W[m,2]=W[m,2]-(W[m,2]-ztop)[...]*t[m]*0.8
    # knuckle junction: blend the cut donor palm into the new finger roots
    for side,J in HANDJ.items():
        mc=np.array([J[f][0] for f in ('Index','Mid','Ring','Pinky')])
        dmin=np.min(np.linalg.norm(W[:,None,:]-mc[None,:,:],axis=2),axis=1)
        relax(dmin<0.032,80,ramp=5)
if fhead is None:
    # the donor face (AI scan) has flat eyelid 'shelves' that shade as dark wedges at the eye corners;
    # soften only the eye sockets (texture keeps the eye detail)
    for eb_,sg_ in (('CC_Base_L_Eye',1),('CC_Base_R_Eye',-1)):
        ec=B.bone_world(arm,eb_,donor)+np.array([sg_*0.012,0.0,0.002])
        de=np.linalg.norm((W-ec)/np.array([0.038,0.045,0.020]),axis=1); eye=(de<1.0)&(W[:,0]*sg_>0.008)
        W=B.biharmonic_fill(W,A,eye); relax(B.dilate(A,eye,3),15,ramp=3)
        print('eye socket rebuilt',eb_,int(eye.sum()))
B.set_world_co(rm,W)
if os.environ.get('DEBUG_HI'):
    donor.hide_render=True; rm.data.materials.append(bpy.data.materials.new('c'))
    for f in rm.data.polygons: f.use_smooth=True
    cam=rlib.setup_render(400,400,8); rlib.closeup(cam,out+'_hi',(0,0,0.92),0.55,views=(('front',0),('side',90),('back',180))); sys.exit(0)

# ---------- 5. decimate (symmetric) ----------
B.activate(rm)
print('pre-decimate verts',len(rm.data.vertices),'faces',len(rm.data.polygons))
# keep the face denser (eyes, nose, lips bake badly on large triangles): vertex-group weighted collapse.
# Weight 0 near the face front = protected, 1 elsewhere; iterate the ratio until the total hits the target.
Wr=B.world_co(rm); hb=B.bone_world(arm,'CC_Base_Head',donor)
# features oval: eyes ~1.72 m, mouth ~1.66 m, on the front of the face (y < head joint - 6 cm)
fc=hb+np.array([0,-0.10,0.055]); dd_=np.linalg.norm((Wr-fc)/np.array([0.055,0.05,0.065]),axis=1)
face=np.clip((1.3-dd_)/0.3,0,1)*(Wr[:,1]<hb[1]-0.06)
def tris_of(o): return sum(len(p.vertices)-2 for p in o.data.polygons)
# Face detail: eyes/nose/mouth bake badly on large triangles. Decimate evenly, then subdivide only the features
# oval once and snap the new vertices onto the full-detail surface (BVH nearest), so sockets and nostrils keep shape.
from mathutils.bvhtree import BVHTree
import bmesh as _bmF
hi=_bmF.new(); hi.from_mesh(rm.data); hi_bvh=BVHTree.FromBMesh(hi)
FACE_SUB=int(os.environ.get('FACE_SUB','1'))
body_target=TARGET_TRIS-int(os.environ.get('FACE_EXTRA','2400'))*FACE_SUB
src=tris_of(rm); ratio=body_target/src
for it in range(5):
    tmp=rm.copy(); tmp.data=rm.data.copy(); bpy.context.scene.collection.objects.link(tmp); B.activate(tmp)
    d_=tmp.modifiers.new('dec','DECIMATE'); d_.ratio=ratio; d_.use_symmetry=True; d_.symmetry_axis='X'
    bpy.ops.object.modifier_apply(modifier='dec'); got=tris_of(tmp)
    if abs(got-body_target)<0.01*body_target or it==4: break
    ratio*=body_target/got; bpy.data.objects.remove(tmp,do_unlink=True)
bpy.data.objects.remove(rm,do_unlink=True); rm=tmp; rm.name='rm'; B.activate(rm)
if FACE_SUB:
    bm=_bmF.new(); bm.from_mesh(rm.data); bm.verts.ensure_lookup_table()
    Wv=np.array([v.co[:] for v in bm.verts]); dv=np.linalg.norm((Wv-fc)/np.array([0.055,0.05,0.065]),axis=1)
    infa=(dv<1.15)&(Wv[:,1]<hb[1]-0.05)
    edges=[e for e in bm.edges if infa[e.verts[0].index] or infa[e.verts[1].index]]
    res=_bmF.ops.subdivide_edges(bm,edges=edges,cuts=1,use_grid_fill=True)
    _bmF.ops.triangulate(bm,faces=bm.faces)
    newv=[g for g in res['geom_inner'] if isinstance(g,_bmF.types.BMVert)]
    moved=0
    for v in bm.verts:
        if v.index>=len(Wv) or infa[v.index] if v.index<len(Wv) else True:
            loc,nrm,idx,dist=hi_bvh.find_nearest(v.co)
            if loc is not None and dist<0.01: v.co=loc; moved+=1
    bm.to_mesh(rm.data); bm.free()
    print('face oval subdivided: snapped verts',moved)
print('decimated: total',tris_of(rm))
B.activate(rm)
bpy.ops.object.shade_smooth()
tris=sum(len(p.vertices)-2 for p in rm.data.polygons); print('LOD0 tris',tris,'verts',len(rm.data.vertices))

# ---------- 6. weights from the AccuRig donor ----------
for g in donor.vertex_groups: rm.vertex_groups.new(name=g.name)
dt=rm.modifiers.new('dt','DATA_TRANSFER'); dt.object=donor; dt.use_vert_data=True; dt.data_types_verts={'VGROUP_WEIGHTS'}
dt.vert_mapping='POLYINTERP_NEAREST'; dt.layers_vgroup_select_src='ALL'; dt.layers_vgroup_select_dst='NAME'
# donor is in armature space; transfer in world space
dt.use_object_transform=True
bpy.ops.object.modifier_apply(modifier='dt')
if upto>=1:
    P=B.world_co(rm)
    for side,sg in (('L',1),('R',-1)):
        wrx=B.bone_world(arm,f'CC_Base_{side}_Hand',donor)[0]*sg
        X=P[:,0]*sg; idx=np.where(X>wrx+0.005)[0]
        bl=np.clip((X[idx]-(wrx+0.005))/0.025,0,1); bl=bl*bl*(3-2*bl)   # blend donor wrist weights -> procedural hand weights
        Wh=H.hand_weights(P[idx],HANDJ[side],side,wrx)
        vs=rm.data.vertices; gl=list(rm.vertex_groups)
        for k,i in enumerate(idx):
            for ge in list(vs[int(i)].groups): gl[ge.group].add([int(i)],ge.weight*(1-bl[k]),'REPLACE')
        for name,w in Wh.items():
            g=rm.vertex_groups.get(name) or rm.vertex_groups.new(name=name)
            for k,i in enumerate(idx):
                if w[k]*bl[k]>1e-4: g.add([int(i)],float(w[k]*bl[k]),'ADD')
if fhead is not None:
    P=B.world_co(rm); Pz=FH.shear(P)[:,2]; idx=np.where((Pz>NECK_A)&(np.abs(P[:,0])<0.2))[0]
    wh=np.clip((Pz[idx]-NECK_A)/(NECK_B+0.015-NECK_A),0,1); wh=wh*wh*(3-2*wh)
    vs=rm.data.vertices; gl=list(rm.vertex_groups); gH=rm.vertex_groups.get('CC_Base_Head')
    for k,i in enumerate(idx):
        for ge in list(vs[int(i)].groups): gl[ge.group].add([int(i)],ge.weight*(1-wh[k]),'REPLACE')
        if wh[k]>1e-4: gH.add([int(i)],float(wh[k]),'ADD')
bpy.ops.object.vertex_group_limit_total(group_select_mode='ALL',limit=4)
bpy.ops.object.vertex_group_normalize_all(group_select_mode='ALL',lock_active=False)

# ---------- 7. UV + bake base colour ----------
bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(60),island_margin=0.004); bpy.ops.object.mode_set(mode='OBJECT')
RES=2048; bimg=bpy.data.images.new(f'{NAME}Bare_BaseColor',RES,RES); 
bmat=bpy.data.materials.new(f'{NAME}BareSkin'); bmat.use_nodes=True; nt=bmat.node_tree
tn=nt.nodes.new('ShaderNodeTexImage'); tn.image=bimg; nt.nodes.active=tn
nt.links.new(tn.outputs[0],nt.nodes['Principled BSDF'].inputs['Base Color']); nt.nodes['Principled BSDF'].inputs['Roughness'].default_value=0.55
rm.data.materials.clear(); rm.data.materials.append(bmat)
sc=bpy.context.scene; sc.render.engine='CYCLES'; sc.cycles.device='CPU'; sc.cycles.samples=1
donor.hide_render=False
if fhead is not None:
    import bmesh as _bm2
    bm=_bm2.new(); bm.from_mesh(donor.data); bm.verts.ensure_lookup_table()
    Zd_=FH.shear(Wd)[:,2]
    _bm2.ops.delete(bm,geom=[f for f in bm.faces if any(v.index<len(Wd) and Zd_[v.index]>NECK_A+0.004 and abs(Wd[v.index,0])<0.2 for v in f.verts)],context='FACES')
    bm.to_mesh(donor.data); bm.free(); fhead.hide_render=False
for o in sc.objects: o.select_set(False)
if fhead is not None:
    fhead.select_set(True); nb.hide_render=False; nb.select_set(True)
donor.select_set(True); rm.select_set(True); bpy.context.view_layer.objects.active=rm
# donor is posed by its armature modifier; bake against its rest shape
for md in donor.modifiers: md.show_viewport=md.show_render=False
sc.render.bake.use_selected_to_active=True; sc.render.bake.margin=8
def bake(extr,ray):
    sc.render.bake.cage_extrusion=extr; sc.render.bake.max_ray_distance=ray
    bpy.ops.object.bake(type='EMIT'); return np.array(bimg.pixels[:]).reshape(RES,RES,4).copy()
p1=bake(0.02,0.05)
# smoothed regions can sit >2 cm inside the old fabric: rebake with longer rays only where the first bake missed (pure black)
from scipy.ndimage import binary_dilation
miss=binary_dilation(p1[:,:,:3].sum(2)<0.03,iterations=3)
if miss.any():
    p2=bake(0.06,0.12); p1[miss]=p2[miss]
print('bake misses refilled',int(miss.sum()))
bimg.pixels.foreach_set(p1.astype(np.float32).ravel())
bimg.filepath_raw=out+'_BaseColor.png'; bimg.file_format='PNG'; bimg.save()
donor.hide_render=True; donor.hide_viewport=True

# ---------- 8a. finger rig fix ----------
if upto>=1:
    import mathutils
    Mi=np.linalg.inv(np.array(donor.matrix_world))     # world -> armature/donor local (same space)
    toL=lambda p: mathutils.Vector((Mi[:3,:3]@p+Mi[:3,3]).tolist())
    B.activate(arm); bpy.ops.object.mode_set(mode='EDIT'); eb=arm.data.edit_bones
    for side,J in HANDJ.items():
        for dg,pts in J.items():
            for i in range(3):
                b=eb[f'CC_Base_{side}_{dg}{i+1}']; b.use_connect=False
            for i in range(3):
                b=eb[f'CC_Base_{side}_{dg}{i+1}']; b.head=toL(pts[i]); b.tail=toL(pts[i+1])
                b.align_roll(mathutils.Vector((0,0,1)))
    bpy.ops.object.mode_set(mode='OBJECT')
    print('finger bones moved')
# ---------- 8. rig + modular split ----------
rm.name=f'{NAME}Bare_Body'; rm.data.name=rm.name
if fhead is not None and os.environ.get('HAIR_MODE','cap')=='cap':
    # mobile hair: replace the 9.5k-tri alpha cards with a solid cap grown from the scalp (same coverage)
    HWc=B.world_co(fhair); RW=B.world_co(rm)
    cv,cf=FH.hair_cap(RW,[p.vertices[:] for p in rm.data.polygons],HWc,None,
                      max_off=float(os.environ.get('HAIR_OFF','0.0065')),cover=float(os.environ.get('HAIR_COVER','0.012')))
    old=fhair.data; fhair.data=bpy.data.meshes.new('FrankiHairCap'); fhair.data.from_pydata(cv.tolist(),[],cf)
    bpy.data.meshes.remove(old)
    # smoother hairline + rounder volume: one Catmull-Clark level, then trim back to the mobile budget
    B.activate(fhair); sd=fhair.modifiers.new('sub','SUBSURF'); sd.levels=1; sd.render_levels=1; bpy.ops.object.modifier_apply(modifier='sub')
    HT=int(os.environ.get('HAIR_TRIS','1200')); d2=fhair.modifiers.new('dec','DECIMATE'); d2.ratio=min(1.0,HT/(2.0*len(fhair.data.polygons))); d2.use_symmetry=True; d2.symmetry_axis='X'
    bpy.ops.object.modifier_apply(modifier='dec')
    print('hair cap tris',sum(len(p.vertices)-2 for p in fhair.data.polygons))
# The MINI-105 import is inconsistent: the mesh stands upright but the armature OBJECT carries a +90 deg X
# rotation, so bones lie on their back relative to the mesh. Rest pose hides it; any bone rotation swings
# vertices around a pivot in the wrong space (posing explodes, donor included). Fix: mesh local space already
# equals armature local space (Z-up, cm), so drop the armature object's rotation and parent the mesh with an
# identity transform -> bones and skin share one upright frame.
import mathutils
Mi=np.linalg.inv(np.array(donor.matrix_world)); Wl=B.world_co(rm)@Mi[:3,:3].T+Mi[:3,3]
rm.data.vertices.foreach_set('co',Wl.ravel()); rm.data.update()
arm.matrix_world=mathutils.Matrix.Diagonal((0.01,0.01,0.01,1.0))
rm.parent=arm; rm.matrix_parent_inverse=mathutils.Matrix.Identity(4); rm.matrix_basis=mathutils.Matrix.Identity(4)
bpy.context.view_layer.update()
am=rm.modifiers.new('Armature','ARMATURE'); am.object=arm
# keep one smooth normal field across piece seams
rm.data.set_sharp_from_angle(angle=math.pi) if hasattr(rm.data,'set_sharp_from_angle') else None
PIECES={  # piece: bone-name prefixes (dominant weight decides)
 'Head':['Head','Facial','Jaw','Tongue','Teeth','Eye','UpperJaw','NeckTwist02'],
 'Torso':['Hip','Pelvis','Waist','Spine','NeckTwist01','Clavicle'],
 'Arms':['Upperarm','Forearm','Elbow'],
 'Hands':['Hand','Thumb','Index','Mid','Ring','Pinky'],
 'Legs':['Thigh','Calf','Knee'],
 'Feet':['Foot','Toe','BigToe','IndexToe','MidToe','RingToe','PinkyToe'],
}
def piece_of(bone):
    b=bone.replace('CC_Base_','').replace('L_','').replace('R_','')
    for p,keys in PIECES.items():
        for k in keys:
            if b.startswith(k): 
                if p=='Hands' and b.startswith('Mid') and 'Toe' in b: continue
                return p
    return 'Torso'
gname={g.index:g.name for g in rm.vertex_groups}
vp=[]
for vtx in rm.data.vertices:
    best=max(vtx.groups,key=lambda g:g.weight,default=None)
    vp.append(piece_of(gname[best.group]) if best else 'Torso')
vp=np.array(vp)
# freeze normals before splitting so seams stay invisible
B.activate(rm)
rm.data.update()
nrm=np.zeros(len(rm.data.loops)*3); rm.data.loops.foreach_get('normal',nrm) if hasattr(rm.data.loops[0],'normal') else None
rm.data.normals_split_custom_set(nrm.reshape(-1,3).tolist())
fp=np.array([vp[p.vertices[0]] for p in rm.data.polygons])  # face -> piece of its first vertex (majority below)
for i,p in enumerate(rm.data.polygons):
    vals,cnt=np.unique(vp[list(p.vertices)],return_counts=True); fp[i]=vals[cnt.argmax()]
counts={}
import bmesh
for name in PIECES:
    o=rm.copy(); o.data=rm.data.copy(); o.name=f'{NAME}Bare_{name}'; o.data.name=o.name; sc.collection.objects.link(o)
    bm=bmesh.new(); bm.from_mesh(o.data)
    bmesh.ops.delete(bm,geom=[f for f,pp in zip(bm.faces,fp) if pp!=name],context='FACES')
    bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
    bm.to_mesh(o.data); bm.free()
    counts[name]=sum(len(p.vertices)-2 for p in o.data.polygons)
if fhead is not None:
    fhead.hide_render=True; fhead.hide_viewport=True; nb.hide_render=True; nb.hide_viewport=True
    Mi=np.linalg.inv(np.array(donor.matrix_world)); HWl=B.world_co(fhair)@Mi[:3,:3].T+Mi[:3,3]
    fhair.data.vertices.foreach_set('co',HWl.ravel()); fhair.data.update()
    fhair.name=f'{NAME}Bare_Hair'; fhair.data.name=fhair.name
    fhair.parent=arm; fhair.matrix_parent_inverse=mathutils.Matrix.Identity(4); fhair.matrix_basis=mathutils.Matrix.Identity(4)
    g=fhair.vertex_groups.new(name='CC_Base_Head'); g.add(list(range(len(fhair.data.vertices))),1.0,'REPLACE')
    hmd=fhair.modifiers.new('Armature','ARMATURE'); hmd.object=arm
    hm=bpy.data.materials.new(f'{NAME}Hair'); hm.use_nodes=True; hm.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=fhaircol
    hm.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=0.7
    fhair.data.materials.clear(); fhair.data.materials.append(hm)
    for f in fhair.data.polygons: f.use_smooth=True
    counts['Hair']=sum(len(p.vertices)-2 for p in fhair.data.polygons)
print('pieces',counts)
sc.collection.objects.unlink(rm)
bpy.ops.wm.save_as_mainfile(filepath=out+'.blend',compress=True)
# ---------- 9. FBX ----------
for o in sc.objects: o.select_set(o.type in ('ARMATURE','MESH') and o.name!=donor.name and not o.hide_viewport)
donor.select_set(False)
bpy.ops.export_scene.fbx(filepath=out+'.fbx',use_selection=True,object_types={'ARMATURE','MESH'},
    apply_unit_scale=True,bake_space_transform=False,add_leaf_bones=False,use_armature_deform_only=False,
    use_mesh_modifiers=True,mesh_smooth_type='OFF',bake_anim=False,path_mode='STRIP')
# ---------- 10. renders ----------
for o in list(sc.objects):
    if o.type=='LIGHT' or o.type=='CAMERA': bpy.data.objects.remove(o)
arm.data.pose_position='REST'
cam=rlib.setup_render(400,720,24); rlib.turntable(cam,0.93,1.9,out)
sc.render.resolution_y=400; rlib.closeup(cam,out+'_hip',(0,0,0.92),0.55)
rlib.closeup(cam,out+'_shoulder',(0,0,1.40),0.55)
rlib.closeup(cam,out+'_hand',(0.80,0,1.47),0.30,views=(('front',0),('back',180)))
cam.location=(0.83,0.015,2.15); cam.rotation_euler=(0,0,0); sc.render.filepath=out+'_hand_top.png'; bpy.ops.render.render(write_still=True)
cam.location=(0.83,0.015,0.79); cam.rotation_euler=(math.pi,0,0); sc.render.filepath=out+'_hand_palm.png'; bpy.ops.render.render(write_still=True)
clay=bpy.data.materials.new('clay'); clay.diffuse_color=(0.6,0.6,0.62,1)
for o in sc.objects:
    if o.type=='MESH' and not o.hide_render: o.data.materials[0]=clay
rlib.closeup(cam,out+'_clayhip',(0,0,0.92),0.55); rlib.closeup(cam,out+'_clayshoulder',(0,0,1.40),0.55)
rlib.closeup(cam,out+'_clayhand',(0.80,0,1.47),0.30,views=(('front',0),('back',180)))
cam.location=(0.83,0.015,2.15); cam.rotation_euler=(0,0,0); sc.render.filepath=out+'_clayhand_top.png'; bpy.ops.render.render(write_still=True)
cam.location=(0.83,0.015,0.79); cam.rotation_euler=(math.pi,0,0); sc.render.filepath=out+'_clayhand_palm.png'; bpy.ops.render.render(write_still=True)
