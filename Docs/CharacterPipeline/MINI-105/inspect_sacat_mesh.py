import bpy
from collections import Counter, deque

FBX = r"E:\Unity\Up Iz Up Mini\Assets\UpIzUpMini\Art\Characters\Mainchar.fbx"
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=FBX, use_anim=False)

for obj in [o for o in bpy.context.scene.objects if o.type == 'MESH']:
    mesh = obj.data
    print(f"MESH {obj.name} vertices={len(mesh.vertices)} polygons={len(mesh.polygons)} materials={len(mesh.materials)} groups={len(obj.vertex_groups)}")
    for index, material in enumerate(mesh.materials):
        faces = sum(1 for polygon in mesh.polygons if polygon.material_index == index)
        print(f"  MATERIAL {index}: {material.name if material else '<none>'} faces={faces}")
    print("  VERTEX_GROUPS " + ", ".join(group.name for group in obj.vertex_groups))

    # Connected polygon islands reveal whether head/clothes/body are merely joined
    # into one object or actually welded into one continuous surface.
    vertex_faces = [[] for _ in mesh.vertices]
    for polygon in mesh.polygons:
        for vertex in polygon.vertices:
            vertex_faces[vertex].append(polygon.index)
    unseen = set(range(len(mesh.polygons)))
    islands = []
    while unseen:
        seed = unseen.pop()
        queue = deque([seed])
        component = [seed]
        while queue:
            face_index = queue.popleft()
            for vertex in mesh.polygons[face_index].vertices:
                for neighbor in vertex_faces[vertex]:
                    if neighbor in unseen:
                        unseen.remove(neighbor)
                        queue.append(neighbor)
                        component.append(neighbor)
        islands.append(component)
    islands.sort(key=len, reverse=True)
    for i, island in enumerate(islands[:30]):
        materials = Counter(mesh.polygons[index].material_index for index in island)
        verts = {vertex for index in island for vertex in mesh.polygons[index].vertices}
        zs = [obj.matrix_world @ mesh.vertices[v].co for v in verts]
        print(f"  ISLAND {i}: faces={len(island)} verts={len(verts)} x={min(p.x for p in zs):.4f}..{max(p.x for p in zs):.4f} y={min(p.y for p in zs):.4f}..{max(p.y for p in zs):.4f} z={min(p.z for p in zs):.4f}..{max(p.z for p in zs):.4f} materials={dict(materials)}")

print("SACAT_MESH_INSPECTION_PASS")
