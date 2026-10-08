import bpy,sys,bmesh
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=sys.argv[-1])
o=[o for o in bpy.context.scene.objects if o.type=='MESH'][0]
me=o.data; print('verts',len(me.vertices),'faces',len(me.polygons),'tris',sum(len(p.vertices)-2 for p in me.polygons))
print('mw',o.matrix_world, 'parent',o.parent, o.parent.matrix_world if o.parent else '')
bm=bmesh.new(); bm.from_mesh(me)
seen=set(); comps=[]
for v in bm.verts:
    if v.index in seen: continue
    st=[v]; seen.add(v.index); n=0
    while st:
        x=st.pop(); n+=1
        for e in x.link_edges:
            w=e.other_vert(x)
            if w.index not in seen: seen.add(w.index); st.append(w)
    comps.append(n)
print('components',sorted(comps,reverse=True)[:10],len(comps))
print('groups',len(o.vertex_groups))
