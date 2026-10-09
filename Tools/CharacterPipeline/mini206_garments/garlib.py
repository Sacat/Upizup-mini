"""MINI-206 garment builder: loose garments over the MINI-197 bare bodies (Blender, Z up, -Y front, metres)."""
import bpy, bmesh, numpy as np, math
from mathutils import Vector
from mathutils.bvhtree import BVHTree

def load_body(fbx):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=fbx); bpy.context.view_layer.update()
    arm=[o for o in bpy.context.scene.objects if o.type=='ARMATURE'][0]
    parts=[o for o in bpy.context.scene.objects if o.type=='MESH' and 'Hair' not in o.name]
    bm=bmesh.new()
    for o in parts:
        tmp=bmesh.new(); tmp.from_mesh(o.data); bmesh.ops.transform(tmp,matrix=o.matrix_world,verts=tmp.verts)
        me=bpy.data.meshes.new('t'); tmp.to_mesh(me); tmp.free(); bm.from_mesh(me); bpy.data.meshes.remove(me)
    bmesh.ops.remove_doubles(bm,verts=bm.verts,dist=0.0015)
    bvh=BVHTree.FromBMesh(bm)
    return arm,parts,bm,bvh

def bone(arm,name):
    b=arm.data.bones[name]; return np.array(arm.matrix_world@b.head_local), np.array(arm.matrix_world@b.tail_local)

def mesh_obj(name,verts,faces):
    me=bpy.data.meshes.new(name); me.from_pydata([tuple(v) for v in verts],[],faces); me.update()
    o=bpy.data.objects.new(name,me); bpy.context.scene.collection.objects.link(o); return o

def body_radius(bvh,center,dirs,maxr):
    """distance from center to the body surface along each direction (first hit)."""
    out=np.zeros(len(dirs))
    for i,d in enumerate(dirs):
        hit=bvh.ray_cast(Vector(center),Vector(d),maxr)
        out[i]=hit[3] if hit[0] is not None else np.nan
    return out

def fill_circ(r):
    """fill NaN gaps around a ring by circular interpolation."""
    n=len(r); idx=np.where(~np.isnan(r))[0]
    if len(idx)==0: return r
    return np.interp(np.arange(n),np.r_[idx-n,idx,idx+n],np.r_[r[idx],r[idx],r[idx]])

def smooth_circ(r,k=2):
    for _ in range(k): r=(np.roll(r,1)+2*r+np.roll(r,-1))/4
    return r

def loft(name,rings,closed_ends=True):
    """rings: list of (N,3) arrays, same N. Returns a closed tube object."""
    N=len(rings[0]); V=np.vstack(rings); F=[]
    for j in range(len(rings)-1):
        for i in range(N):
            a=j*N+i; b=j*N+(i+1)%N; F.append((a,b,b+N,a+N))
    if closed_ends:
        c0=len(V); V=np.vstack([V,rings[0].mean(0),rings[-1].mean(0)])
        for i in range(N):
            F.append((c0,(i+1)%N,i)); F.append((c0+1,(len(rings)-1)*N+i,(len(rings)-1)*N+(i+1)%N))
    return mesh_obj(name,V,F)

def frame(axis):
    axis=axis/np.linalg.norm(axis); ref=np.array([0,0,1.0]) if abs(axis[2])<0.9 else np.array([0,-1.0,0])
    u=np.cross(axis,ref); u/=np.linalg.norm(u); v=np.cross(axis,u); return axis,u,v

def tube_along(bvh,name,p0,p1,ease_fn,n_rings,N=32,maxr=0.16,min_r_fn=None,extend=(0,0)):
    """rings around the segment p0->p1 at the body radius + ease(t)."""
    axis,u,v=frame(p1-p0); L=np.linalg.norm(p1-p0); rings=[]
    th=np.linspace(0,2*np.pi,N,endpoint=False); dirs=[math.cos(a)*u+math.sin(a)*v for a in th]
    for t in np.linspace(-extend[0],1+extend[1],n_rings):
        c=p0+(p1-p0)*t
        r=fill_circ(body_radius(bvh,c,dirs,maxr)); r=smooth_circ(r,2)
        r=r+ease_fn(t)
        if min_r_fn is not None: r=np.maximum(r,min_r_fn(t))
        rings.append(np.array([c+ri*d for ri,d in zip(r,dirs)]))
    return loft(name,rings)

def inflated_body(bm,offset,name='inflated'):
    b=bm.copy(); b.normal_update()
    for v in b.verts: v.co=v.co+v.normal*offset
    me=bpy.data.meshes.new(name); b.to_mesh(me); b.free()
    o=bpy.data.objects.new(name,me); bpy.context.scene.collection.objects.link(o); return o

def activate(o):
    for x in bpy.context.view_layer.objects: x.select_set(False)
    bpy.context.view_layer.objects.active=o; o.select_set(True)

def union_remesh(objs,voxel,name):
    """voxel-union of closed solids -> one closed surface."""
    bm=bmesh.new()
    for o in objs:
        tmp=bmesh.new(); tmp.from_mesh(o.data); bmesh.ops.transform(tmp,matrix=o.matrix_world,verts=tmp.verts)
        me=bpy.data.meshes.new('t'); tmp.to_mesh(me); tmp.free(); bm.from_mesh(me); bpy.data.meshes.remove(me)
    me=bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    o=bpy.data.objects.new(name,me); bpy.context.scene.collection.objects.link(o)
    for x in objs: bpy.data.objects.remove(x)
    o.data.remesh_voxel_size=voxel; activate(o); bpy.ops.object.voxel_remesh()
    return o

def delete_faces(o,pred):
    bm=bmesh.new(); bm.from_mesh(o.data)
    dead=[f for f in bm.faces if pred(np.array(f.calc_center_median()))]
    bmesh.ops.delete(bm,geom=dead,context='FACES')
    # keep the largest connected piece only
    bm.faces.ensure_lookup_table(); seen=set(); comps=[]
    for f in bm.faces:
        if f in seen: continue
        st=[f]; seen.add(f); comp=[]
        while st:
            g=st.pop(); comp.append(g)
            for e in g.edges:
                for h in e.link_faces:
                    if h not in seen: seen.add(h); st.append(h)
        comps.append(comp)
    comps.sort(key=len,reverse=True)
    for comp in comps[1:]: bmesh.ops.delete(bm,geom=comp,context='FACES')
    bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
    bm.to_mesh(o.data); bm.free(); o.data.update()

def boundary_loops(o):
    bm=bmesh.new(); bm.from_mesh(o.data); bm.verts.ensure_lookup_table()
    be=[e for e in bm.edges if e.is_boundary]; adj={}
    for e in be:
        for v in e.verts: adj.setdefault(v.index,[]).append(e.other_vert(v).index)
    used=set(); loops=[]
    for s in adj:
        if s in used: continue
        loop=[s]; used.add(s); prev=None; cur=s
        while True:
            nxt=[n for n in adj[cur] if n!=prev and n not in used]
            if not nxt: break
            prev,cur=cur,nxt[0]; loop.append(cur); used.add(cur)
        loops.append(np.array([tuple(bm.verts[i].co) for i in loop]))
    bm.free(); return loops

def smooth_shell(o,iters=10,factor=0.5,keep_boundary=True):
    activate(o); m=o.modifiers.new('sm','LAPLACIANSMOOTH') if False else o.modifiers.new('sm','SMOOTH')
    m.factor=factor; m.iterations=iters
    bpy.ops.object.modifier_apply(modifier='sm')

def decimate_to(o,tris):
    activate(o); n=sum(len(p.vertices)-2 for p in o.data.polygons)
    if n<=tris: return n
    # open edges (hems, necklines) and the ring next to them are locked: collapsing them tears slits into the garment
    bm=bmesh.new(); bm.from_mesh(o.data); lock=set()
    for e in bm.edges:
        if e.is_boundary:
            for v in e.verts:
                lock.add(v.index); lock.update(x.other_vert(v).index for x in v.link_edges)
    bm.free(); vg=o.vertex_groups.get('decimate_free') or o.vertex_groups.new(name='decimate_free')
    vg.add([i for i in range(len(o.data.vertices)) if i not in lock],1.0,'REPLACE')
    d=o.modifiers.new('d','DECIMATE'); d.ratio=tris/n; d.use_symmetry=True; d.symmetry_axis='X'
    d.vertex_group='decimate_free'; d.vertex_group_factor=1.0
    bpy.ops.object.modifier_apply(modifier='d'); o.vertex_groups.remove(o.vertex_groups['decimate_free']); return sum(len(p.vertices)-2 for p in o.data.polygons)

def band_along_loop(name,loop,center,inward,up,height,thick,segments=None):
    """rib band: a short closed strip that follows an opening edge loop. inward: metres toward 'center' (axis point
    per vertex), up: metres along +Z per unit height. Returns a solid (4 rings, closed)."""
    L=loop
    if segments: 
        # resample the loop evenly
        d=np.r_[0,np.cumsum(np.linalg.norm(np.diff(np.vstack([L,L[:1]]),axis=0),axis=1))]
        s=np.linspace(0,d[-1],segments,endpoint=False); L=np.array([[np.interp(x,d,np.r_[L[:,k],L[0,k]]) for k in range(3)] for x in s])
    c=center(L)
    toC=c-L; toC[:,2]=0; toC/=np.maximum(np.linalg.norm(toC,axis=1),1e-6)[:,None]
    r0=L-toC*0.0005; r1=L+toC*inward+np.array([0,0,up*height])
    o0=r0-toC*thick; o1=r1-toC*thick
    return loft(name,[r0,r1,o1,o0,r0],closed_ends=False)

def resample_loop(L,n):
    P=np.vstack([L,L[:1]]); d=np.r_[0,np.cumsum(np.linalg.norm(np.diff(P,axis=0),axis=1))]
    s=np.linspace(0,d[-1],n,endpoint=False)
    return np.array([[np.interp(x,d,P[:,k]) for k in range(3)] for x in s])

def orient_loop(L,axis):
    """consistent winding around 'axis' so lofted bands face outward."""
    c=L.mean(0); a=np.cross(L[0]-c,L[len(L)//4]-c)
    return L if np.dot(a,axis)>0 else L[::-1]

def body_region_solid(bm,zmin,zmax,offset,name):
    """inflated copy of the body between two heights, holes capped -> closed solid for voxel union."""
    b=bm.copy(); b.normal_update()
    for v in b.verts: v.co=v.co+v.normal*offset
    dead=[f for f in b.faces if not (zmin<=f.calc_center_median().z<=zmax)]
    bmesh.ops.delete(b,geom=dead,context='FACES')
    bmesh.ops.delete(b,geom=[v for v in b.verts if not v.link_faces],context='VERTS')
    bmesh.ops.holes_fill(b,edges=[e for e in b.edges if e.is_boundary],sides=0)
    me=bpy.data.meshes.new(name); b.to_mesh(me); b.free()
    o=bpy.data.objects.new(name,me); bpy.context.scene.collection.objects.link(o); return o

def join(objs,name):
    activate(objs[0])
    for o in objs[1:]: o.select_set(True)
    bpy.ops.object.join(); objs[0].name=name; objs[0].data.name=name
    for p in objs[0].data.polygons: p.use_smooth=True
    return objs[0]

def tris(o): return sum(len(p.vertices)-2 for p in o.data.polygons)

def fold_band(L,axis_center,height_vec,thick,n,name):
    """hem fold: a closed thin band hanging from an opening loop (turn-up / hem)."""
    L=resample_loop(L,n); c=axis_center(L); toC=c-L; toC/=np.maximum(np.linalg.norm(toC,axis=1),1e-6)[:,None]
    return loft(name,[L,L+height_vec,L+height_vec-toC*thick,L-toC*thick,L],closed_ends=False)

def pose_bone_world(arm,name,axis_world,deg):
    """rotate a pose bone about a world-space axis through its head (armature in rest -> pose)."""
    import mathutils
    pb=arm.pose.bones[name]; bpy.context.view_layer.update()
    Mw=arm.matrix_world; head_w=Mw@pb.head
    R=mathutils.Matrix.Rotation(math.radians(deg),4,Vector(axis_world))
    T=mathutils.Matrix.Translation(head_w); Ti=mathutils.Matrix.Translation(-head_w)
    world=Mw@pb.matrix; new=T@R@Ti@world
    pb.matrix=Mw.inverted()@new; bpy.context.view_layer.update()

def skin_from_body(garment,body_parts,arm,smooth_iters=2):
    """copy CC_Base weights from the nearest body surface, smooth, limit 4, normalise; parent to the armature."""
    src=[o.copy() for o in body_parts]
    for o in src: o.data=o.data.copy(); bpy.context.scene.collection.objects.link(o)
    for o in src:
        for m in list(o.modifiers): o.modifiers.remove(m)
    activate(src[0])
    for o in src[1:]: o.select_set(True)
    bpy.ops.object.join(); srcj=src[0]
    for g in srcj.vertex_groups: garment.vertex_groups.new(name=g.name)
    activate(garment)
    dt=garment.modifiers.new('dt','DATA_TRANSFER'); dt.object=srcj; dt.use_vert_data=True; dt.data_types_verts={'VGROUP_WEIGHTS'}
    dt.vert_mapping='POLYINTERP_NEAREST'; dt.layers_vgroup_select_src='ALL'; dt.layers_vgroup_select_dst='NAME'; dt.use_object_transform=True
    bpy.ops.object.modifier_apply(modifier='dt')
    bpy.ops.object.mode_set(mode='WEIGHT_PAINT')
    for _ in range(smooth_iters):
        bpy.ops.object.vertex_group_smooth(group_select_mode='ALL',factor=0.5,repeat=2)
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.vertex_group_limit_total(group_select_mode='ALL',limit=4)
    bpy.ops.object.vertex_group_normalize_all(group_select_mode='ALL',lock_active=False)
    bpy.data.objects.remove(srcj)
    # parent: keep world positions, mesh data in armature-object space
    import mathutils
    Mi=arm.matrix_world.inverted(); garment.data.transform(Mi@garment.matrix_world); garment.matrix_world=arm.matrix_world.copy()
    garment.parent=arm; garment.matrix_parent_inverse=mathutils.Matrix.Identity(4); garment.matrix_basis=mathutils.Matrix.Identity(4)
    am=garment.modifiers.new('Armature','ARMATURE'); am.object=arm

def smooth_open_edges(o,iters=8,factor=0.5,z_only=False):
    """relax each open edge loop along itself (removes the voxel stair-step on hems/necklines)."""
    bm=bmesh.new(); bm.from_mesh(o.data); bm.verts.ensure_lookup_table()
    nb={}
    for e in bm.edges:
        if e.is_boundary:
            a,b=e.verts; nb.setdefault(a.index,[]).append(b.index); nb.setdefault(b.index,[]).append(a.index)
    co=np.array([v.co[:] for v in bm.verts])
    for _ in range(iters):
        new=co.copy()
        for i,ns in nb.items():
            if len(ns)==2:
                m=(co[ns[0]]+co[ns[1]])/2
                if z_only: new[i,2]+=factor*(m[2]-co[i,2])
                else: new[i]+=factor*(m-co[i])
        co=new
    for i in nb: bm.verts[i].co=co[i]
    bm.to_mesh(o.data); bm.free(); o.data.update()
