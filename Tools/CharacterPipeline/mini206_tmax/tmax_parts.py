import bpy,bmesh,math,numpy as np,sys,os
from mathutils import Vector,Matrix
sys.path.insert(0,os.path.dirname(__file__))
exec(open(os.path.join(os.path.dirname(__file__),'tmax_build.py')).read().split("bpy.ops.wm.open_mainfile")[0])   # constants + helpers
exec("\n".join(l for l in open(os.path.join(os.path.dirname(__file__),'tmax_build.py')).read().split("\n") if l.startswith(('def ','    ')) and False))
bpy.ops.wm.open_mainfile(filepath='tmax/build_a.blend')
src=open(os.path.join(os.path.dirname(__file__),'tmax_build.py')).read()
helpers=src[src.index('def link'):src.index('# ---------------- cutters')]
exec(helpers)
body=bpy.data.objects['TMAX_Body']; fender=bpy.data.objects['Fender']
# ---------- 1. body: drop loose fragments left around the wheels/fork ----------
bm=bmesh.new(); bm.from_mesh(body.data); bm.faces.ensure_lookup_table(); seen=set(); comps=[]
for f in bm.faces:
    if f in seen: continue
    st=[f]; seen.add(f); cp=[]
    while st:
        g=st.pop(); cp.append(g)
        for e in g.edges:
            for h in e.link_faces:
                if h not in seen: seen.add(h); st.append(h)
    comps.append(cp)
comps.sort(key=len,reverse=True); N=len(comps[0]); dead=[]; kept=[]
for c in comps[1:]:
    ctr=np.mean([f.calc_center_median()[:] for f in c[:200]],axis=0)
    nearwheel=min(np.linalg.norm(ctr-FA),np.linalg.norm(ctr-RA))<0.45 and ctr[2]<0.7
    if len(c)<0.02*N and (nearwheel or len(c)<300): dead+=c
    else: kept.append((len(c),np.round(ctr,2).tolist()))
print('body pieces',len(comps),'main',N,'deleted faces',len(dead),'kept extra pieces',kept[:12])
bmesh.ops.delete(bm,geom=dead,context='FACES'); bm.to_mesh(body.data); bm.free()
# ---------- 2. procedural parts ----------
def lathe(name,profile,c,n=36,xdir=1):
    """profile: list of (x_offset, radius) around an X axis through c, closed ring sweep."""
    V=[];F=[]; m=len(profile)
    for i in range(n):
        a=2*math.pi*i/n
        for (x,r) in profile: V.append((c[0]+xdir*x,c[1]+r*math.cos(a),c[2]+r*math.sin(a)))
    for i in range(n):
        j=(i+1)%n
        for k in range(m): F.append((i*m+k,j*m+k,j*m+(k+1)%m,i*m+(k+1)%m))
    o=mesh_from(name,V,F); recalc(o); return o
def tyre(name,c,R,W,rim_r,n=36):
    h=W/2; prof=[(-h*0.62,rim_r),(-h*0.92,rim_r+0.25*(R-rim_r)),(-h,rim_r+0.55*(R-rim_r)),(-h*0.80,R-0.012),(-h*0.42,R-0.002),(0,R),
                 (h*0.42,R-0.002),(h*0.80,R-0.012),(h,rim_r+0.55*(R-rim_r)),(h*0.92,rim_r+0.25*(R-rim_r)),(h*0.62,rim_r)]
    prof=prof+[(h*0.62,rim_r-0.004),(-h*0.62,rim_r-0.004)]   # inner wall (closed section)
    return lathe(name,prof,c,n)
def ring_x(name,c,r0,r1,x0,x1,n=40):
    return lathe(name,[(x0,r0),(x0,r1),(x1,r1),(x1,r0)],c,n)
def spokes(name,c,r_in,r_out,xw,nsp=5,split=0.16,w=0.016):
    parts=[]
    for k in range(nsp):
        for sgn in (-1,1):
            a=2*math.pi*k/nsp+sgn*split/2
            mid=(r_in+r_out)/2; p=c+np.array([0,mid*math.cos(a),mid*math.sin(a)])
            rot=Matrix.Rotation(a-math.pi/2,3,'X')    # local Z -> radial direction
            parts.append(box(f'{name}{k}{sgn}',p,(xw,w,(r_out-r_in)*1.04),rot))
    return parts
def material(name,rgb,metal=0.0,rough=0.6):
    m=bpy.data.materials.get(name) or bpy.data.materials.new(name); m.use_nodes=True; b=m.node_tree.nodes['Principled BSDF']
    b.inputs['Base Color'].default_value=(*rgb,1); b.inputs['Metallic'].default_value=metal; b.inputs['Roughness'].default_value=rough; return m
M={'rubber':material('p_rubber',(0.025,0.025,0.027),0,0.85),'rim':material('p_rim',(0.05,0.05,0.055),0.7,0.35),'disc':material('p_disc',(0.55,0.55,0.56),1.0,0.3),
   'gold':material('p_gold',(0.78,0.55,0.16),1.0,0.3),'black':material('p_black',(0.03,0.03,0.035),0.2,0.45),'chrome':material('p_chrome',(0.7,0.7,0.72),1,0.15),
   'grip':material('p_grip',(0.04,0.04,0.04),0,0.9),'alu':material('p_alu',(0.32,0.32,0.34),0.9,0.35)}
def setmat(o,m):
    o.data.materials.clear(); o.data.materials.append(m); return o
def wheel(name,c,R,W,rim_r,discs,pulley=None):
    ps=[setmat(tyre(name+'_tyre',c,R,W,rim_r),M['rubber'])]
    ps.append(setmat(ring_x(name+'_rim',c,rim_r-0.022,rim_r-0.002,-W*0.31,W*0.31),M['rim']))
    ps+= [setmat(s,M['rim']) for s in spokes(name+'_sp',c,0.045,rim_r-0.018,0.022)]
    ps.append(setmat(ring_x(name+'_hub',c,0.012,0.05,-W*0.40,W*0.40,24),M['rim']))
    for (dx,dr) in discs:
        ps.append(setmat(ring_x(name+'_disc',c,dr-0.03,dr,dx-0.0025,dx+0.0025,32),M['disc']))
        ps.append(setmat(ring_x(name+'_carrier',c,0.045,dr-0.028,dx-0.002,dx+0.002,24),M['alu']))
    if pulley: ps.append(setmat(ring_x(name+'_pulley',c,0.05,pulley[1],pulley[0]-0.01,pulley[0]+0.01,32),M['black']))
    o=join(ps,name); return o
fw=wheel('TMAX_FrontWheel',FA,RF,0.128,0.192,[(0.072,0.134),(-0.072,0.134)])
rw=wheel('TMAX_RearWheel',RA,RR,0.150,0.168,[(-0.066,0.120)],pulley=(0.072,0.118))
# fork legs: black slider (axle lug .. 0.50) + gold tube (0.47 .. 0.80, into the fairing) + caliper + axle
fork=[]
def leg_seg(name,x,z0,z1,r,m,n=20):
    p0=axis_pt(z0)+np.array([x,-0.035,0]); p1=axis_pt(z1)+np.array([x,-0.035,0])
    L=float(np.linalg.norm(p1-p0)); bm=bmesh.new(); bmesh.ops.create_cone(bm,cap_ends=True,segments=n,radius1=r,radius2=r,depth=L)
    bmesh.ops.rotate(bm,verts=bm.verts,cent=(0,0,0),matrix=Matrix.Rotation(-RAKE,3,'X'))
    bmesh.ops.translate(bm,verts=bm.verts,vec=Vector(tuple(map(float,(p0+p1)/2)))); return setmat(bm_obj(name,bm),m)
for s in (1,-1):
    x=s*0.098
    fork.append(leg_seg(f'slider{s}',x,0.25,0.52,0.029,M['black']))
    fork.append(leg_seg(f'tube{s}',x,0.50,0.82,0.022,M['gold']))
    fork.append(setmat(box(f'lug{s}',FA+np.array([x,0.012,-0.005]),(0.05,0.075,0.07),Matrix.Rotation(-RAKE,3,'X')),M['black']))
    # radial caliper on the rear-upper edge of the disc
    a=math.radians(130); cp=FA+np.array([s*0.074,0.118*math.cos(a)*-1,0.118*math.sin(a)])
    fork.append(setmat(box(f'caliper{s}',cp,(0.042,0.05,0.11),Matrix.Rotation(-math.radians(40),3,'X')),M['gold' if False else 'black']))
fork.append(setmat(ring_x('axle',FA,0.004,0.014,-0.13,0.13,16),M['chrome']))
def decimate(o,target):
    n=len(o.data.polygons)
    if n<=target: return
    d=o.modifiers.new('d','DECIMATE'); d.ratio=target/n; bpy.context.view_layer.objects.active=o; bpy.ops.object.modifier_apply(modifier='d')
bpy.context.view_layer.objects.active=fender; bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.mesh.quads_convert_to_tris(); bpy.ops.object.mode_set(mode='OBJECT')
decimate(fender,int(__import__('os').environ.get('FENDER_TRIS','900')))
fender_lo=setmat(fender,M['black']) if False else fender
fk=join(fork+[fender_lo],'TMAX_FrontForkAssembly')
# handlebar: bar (slight sweep), grips, bar ends, switch pods, levers, reservoirs, clamp
hb=[]; BY,BZ=BARC[1],BARC[2]
def tube_pts(name,pts,r,m,n=14):
    objs=[]
    for i in range(len(pts)-1):
        a,b=np.array(pts[i]),np.array(pts[i+1]); d=b-a; L=float(np.linalg.norm(d))
        bm=bmesh.new(); bmesh.ops.create_cone(bm,cap_ends=True,segments=n,radius1=r,radius2=r,depth=L+r*0.6)
        q=Vector((0,0,1)).rotation_difference(Vector(tuple(map(float,d/L)))); bmesh.ops.rotate(bm,verts=bm.verts,cent=(0,0,0),matrix=q.to_matrix())
        bmesh.ops.translate(bm,verts=bm.verts,vec=Vector(tuple(map(float,(a+b)/2)))); objs.append(setmat(bm_obj(name,bm),m))
    return objs
bar=[(-0.36,BY+0.035,BZ),(-0.22,BY+0.012,BZ+0.004),(-0.08,BY,BZ+0.012),(0.08,BY,BZ+0.012),(0.22,BY+0.012,BZ+0.004),(0.36,BY+0.035,BZ)]
hb+=tube_pts('bar',bar,0.011,M['black'])
for s in (1,-1):
    g0=np.array([s*0.255,BY+0.019,BZ+0.002]); g1=np.array([s*0.365,BY+0.036,BZ])
    hb+=tube_pts(f'grip{s}',[g0,g1],0.017,M['grip'])
    hb+=tube_pts(f'end{s}',[g1,g1+np.array([s*0.022,0.004,0])],0.016,M['chrome'])
    hb.append(setmat(box(f'switch{s}',(s*0.225,BY+0.010,BZ+0.004),(0.050,0.058,0.050)),M['black']))
    hb.append(setmat(box(f'res{s}',(s*0.175,BY-0.006,BZ+0.040),(0.046,0.034,0.030)),M['black']))
    hb.append(setmat(box(f'lever{s}',(s*0.300,BY-0.040,BZ-0.006),(0.13,0.012,0.016),Matrix.Rotation(-s*math.radians(8),3,'Z')),M['alu']))
hb.append(setmat(box('clamp',(0,BY+0.004,BZ-0.012),(0.14,0.06,0.05)),M['black']))
hb.append(setmat(box('riser',(0,BY+0.004,BZ-0.06),(0.06,0.05,0.10)),M['black']))
hbar=join(hb,'TMAX_Handlebar')
# ---------- 3. pivots ----------
def set_origin(o,p):
    p=Vector(tuple(map(float,p))); o.data.transform(Matrix.Translation(-p+o.location)); o.location=p
decimate(body,int(__import__('os').environ.get('BODY_TRIS','14000')))
set_origin(fw,FA); set_origin(rw,RA); set_origin(fk,HEAD); set_origin(hbar,BARC); set_origin(body,(0,0,0))
for o in (fw,rw,fk,hbar,body):
    for p in o.data.polygons: p.use_smooth=True
print('HEAD',HEAD.round(4),'axis at the front axle height',axis_pt(FA[2]).round(4))
for o in (body,fw,rw,fk,hbar): print('PART',o.name,'tris',sum(len(p.vertices)-2 for p in o.data.polygons),'open/nonman',open_edges(o))
bpy.ops.wm.save_as_mainfile(filepath='tmax/build_b.blend')
