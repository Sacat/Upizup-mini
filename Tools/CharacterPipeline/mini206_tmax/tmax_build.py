"""MINI-206 Stage 1 (lean): TMAX parts. Frame: Blender, metres, front -Y, up +Z, origin on the ground midway between the axles.
Moving parts are rebuilt/cut as closed solids; all static parts stay in TMAX_Body."""
import bpy,bmesh,math,numpy as np,time
from mathutils import Vector,Matrix
T0=time.time()
FA=np.array([0,-0.7875,0.3042]); RA=np.array([0,0.7875,0.2537])   # axles
RF,RR=0.3042,0.2537                                                # tyre radii (scan; see report)
RAKE=math.radians(25.0); AX=np.array([0,math.sin(RAKE),math.cos(RAKE)])   # steering axis direction (up, leaning back)
TRAIL=0.095; G0=np.array([0,FA[1]-TRAIL,0.0])                      # axis meets the ground 95 mm ahead of the contact patch
def axis_pt(z): return G0+AX*(z/AX[2])
HEAD=axis_pt(0.72)                                                 # head bearing (inside the fairing)
BARC=np.array([0,-0.13,1.09])                                      # handlebar clamp centre
bpy.ops.wm.open_mainfile(filepath='tmax/mid.blend')
mid=bpy.data.objects['TMAX_Mid']
def link(o): bpy.context.scene.collection.objects.link(o); return o
def mesh_from(name,verts,faces):
    me=bpy.data.meshes.new(name); me.from_pydata([tuple(map(float,v)) for v in verts],[],faces); me.update(); return link(bpy.data.objects.new(name,me))
def bm_obj(name,bm):
    me=bpy.data.meshes.new(name); bm.to_mesh(me); bm.free(); return link(bpy.data.objects.new(name,me))
def cyl_x(name,c,r,x0,x1,n=48):
    bm=bmesh.new(); bmesh.ops.create_cone(bm,cap_ends=True,segments=n,radius1=r,radius2=r,depth=x1-x0)
    bmesh.ops.rotate(bm,verts=bm.verts,cent=(0,0,0),matrix=Matrix.Rotation(math.pi/2,3,'Y'))
    bmesh.ops.translate(bm,verts=bm.verts,vec=Vector((float((x0+x1)/2),float(c[1]),float(c[2]))))
    return bm_obj(name,bm)
def box(name,c,size,rot=None):
    bm=bmesh.new(); bmesh.ops.create_cube(bm,size=1.0)
    bmesh.ops.scale(bm,verts=bm.verts,vec=Vector(tuple(map(float,size))))
    if rot is not None: bmesh.ops.rotate(bm,verts=bm.verts,cent=(0,0,0),matrix=rot)
    bmesh.ops.translate(bm,verts=bm.verts,vec=Vector(tuple(map(float,c)))); return bm_obj(name,bm)
def sector_x(name,c,r0,r1,a0,a1,x0,x1,n=40):
    """annular sector around an X axis (angles in the YZ plane, 0=+Y back, 90=up)."""
    V=[];F=[]; A=np.linspace(math.radians(a0),math.radians(a1),n)
    for x in (x0,x1):
        for r in (r0,r1):
            for a in A: V.append((x,c[1]+r*math.cos(a),c[2]+r*math.sin(a)))
    def idx(xi,ri,ai): return xi*2*n+ri*n+ai
    for i in range(n-1):
        for xi in (0,1): F.append((idx(xi,0,i),idx(xi,1,i),idx(xi,1,i+1),idx(xi,0,i+1)) if xi==1 else (idx(xi,0,i),idx(xi,0,i+1),idx(xi,1,i+1),idx(xi,1,i)))
        for ri in (0,1): F.append((idx(0,ri,i),idx(1,ri,i),idx(1,ri,i+1),idx(0,ri,i+1)) if ri==0 else (idx(0,ri,i),idx(0,ri,i+1),idx(1,ri,i+1),idx(1,ri,i)))
    for ai in (0,n-1):
        q=(idx(0,0,ai),idx(0,1,ai),idx(1,1,ai),idx(1,0,ai)); F.append(q if ai==0 else q[::-1])
    o=mesh_from(name,V,F); recalc(o); return o
def recalc(o):
    bm=bmesh.new(); bm.from_mesh(o.data); bmesh.ops.recalc_face_normals(bm,faces=bm.faces); bm.to_mesh(o.data); bm.free()
def boolean(target,cutter,op):
    m=target.modifiers.new('b','BOOLEAN'); m.operation=op; m.object=cutter; m.solver='EXACT'
    bpy.context.view_layer.objects.active=target; bpy.ops.object.modifier_apply(modifier='b')
def dup(o,name):
    c=o.copy(); c.data=o.data.copy(); c.name=name; return link(c)
def join(objs,name):
    for x in bpy.context.view_layer.objects: x.select_set(False)
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active=objs[0]; bpy.ops.object.join(); objs[0].name=name; objs[0].data.name=name; return objs[0]
def open_edges(o):
    me=o.data; lt=np.zeros(len(me.loops),np.int32); me.loops.foreach_get('edge_index',lt); c=np.bincount(lt,minlength=len(me.edges)); return int((c==1).sum()),int((c>2).sum())
# ---------------- cutters (what is removed from the body) ----------------
rot_leg=Matrix.Rotation(-RAKE,3,'X')     # local +Z -> along AX (top leans back to +Y)
cut=[]
cut.append(cyl_x('c_fw',FA,RF+0.012,-0.092,0.092))                     # front tyre+rim+discs
cut.append(cyl_x('c_rw',RA,RR+0.012,-0.088,0.088))                     # rear tyre+rim+disc
for s in (1,-1):                                                       # fork legs + calipers, up to the fairing underside
    mid_leg=axis_pt(0.47)+np.array([s*0.095,-0.035,0])
    cut.append(box(f'c_leg{s}',mid_leg+np.array([0,0,0]),(0.085,0.13,0.46),rot_leg))
fender_c=sector_x('c_fender',FA,RF+0.004,RF+0.085,8,178,-0.105,0.105)
cut.append(dup(fender_c,'c_fender_b'))
for s in (1,-1):
    cut.append(box(f'c_bar{s}',(s*0.29,-0.18,1.11),(0.26,0.48,0.16)))  # handlebar halves (|x| 0.16..0.42, y -0.42..0.06)
cut.append(box('c_barc',(0,-0.10,1.075),(0.32,0.12,0.06)))            # centre of the bar above the cover
# ---------------- body = mid - cutters ----------------
body=dup(mid,'TMAX_Body')
for c in cut:
    t=time.time(); n0=len(body.data.polygons); boolean(body,c,'DIFFERENCE'); print('  cut',c.name,'s',round(time.time()-t),'tris',n0,'->',len(body.data.polygons),'open/nonman',open_edges(body))
fender=dup(mid,'Fender'); t=time.time(); boolean(fender,fender_c,'INTERSECT'); print('fender boolean s',round(time.time()-t),open_edges(fender))
# keep only the biggest piece of the fender (sector also grabs bits of the fairing edge)
bm=bmesh.new(); bm.from_mesh(fender.data); bm.faces.ensure_lookup_table()
seen=set(); comps=[]
for f in bm.faces:
    if f in seen: continue
    st=[f]; seen.add(f); cp=[]
    while st:
        g=st.pop(); cp.append(g)
        for e in g.edges:
            for h in e.link_faces:
                if h not in seen: seen.add(h); st.append(h)
    comps.append(cp)
comps.sort(key=len,reverse=True); print('fender pieces',[len(c) for c in comps[:6]])
small=[f for c in comps[1:] if len(c)<0.2*len(comps[0]) for f in c]; bmesh.ops.delete(bm,geom=small,context='FACES'); bm.to_mesh(fender.data); bm.free()
[bpy.data.objects.remove(c) for c in cut]; bpy.data.objects.remove(fender_c); bpy.data.objects.remove(mid)
bpy.ops.wm.save_as_mainfile(filepath='tmax/build_a.blend')
print('total s',round(time.time()-T0))
