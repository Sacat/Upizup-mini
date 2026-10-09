import bpy,sys,numpy as np,time
t=time.time()
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=sys.argv[sys.argv.index('--')+1])
print('import s',round(time.time()-t))
for o in bpy.data.objects:
    print('OBJ',o.name,o.type,tuple(round(x,4) for x in o.location),tuple(round(x,3) for x in o.rotation_euler),tuple(round(x,4) for x in o.scale),o.parent.name if o.parent else None)
    if o.type=='MESH':
        me=o.data; n=len(me.vertices); co=np.empty(n*3,np.float32); me.vertices.foreach_get('co',co); co=co.reshape(-1,3)
        print(' verts',n,'polys',len(me.polygons),'uv',[u.name for u in me.uv_layers],'mats',[m.name for m in me.materials])
        print(' local bbox',co.min(0),co.max(0))
        M=np.array(o.matrix_world); w=co@M[:3,:3].T+M[:3,3]; print(' world bbox',w.min(0),w.max(0))
for im in bpy.data.images: print('IMG',im.name,im.size[:],im.filepath, im.packed_file is not None)
for m in bpy.data.materials:
    if m.use_nodes:
        for n in m.node_tree.nodes:
            if n.type=='TEX_IMAGE': print('TEXNODE',m.name,n.name,n.image.name if n.image else None,[l.to_socket.name+'@'+l.to_node.name for l in n.outputs[0].links])
bpy.ops.wm.save_as_mainfile(filepath=sys.argv[-1])
