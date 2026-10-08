"""Topology checks for the cleaned MINI-186 mesh."""

import bpy
import bmesh
import json
from pathlib import Path

root = Path(__file__).resolve().parent
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(root / "LalayTool_3000tri.fbx"))
obj = next(o for o in bpy.context.scene.objects if o.type == "MESH")
bm = bmesh.new()
bm.from_mesh(obj.data)
boundary = sum(1 for edge in bm.edges if edge.is_boundary)
nonmanifold = sum(1 for edge in bm.edges if not edge.is_manifold)
loose = sum(1 for vertex in bm.verts if not vertex.link_edges)
seen = set()
components = []
for vertex in bm.verts:
    if vertex in seen:
        continue
    stack = [vertex]
    seen.add(vertex)
    count = 0
    while stack:
        current = stack.pop()
        count += 1
        for edge in current.link_edges:
            other = edge.other_vert(current)
            if other not in seen:
                seen.add(other)
                stack.append(other)
    components.append(count)
report = {
    "vertices": len(bm.verts),
    "edges": len(bm.edges),
    "faces": len(bm.faces),
    "boundary_edges": boundary,
    "nonmanifold_edges": nonmanifold,
    "loose_vertices": loose,
    "connected_components": len(components),
    "largest_components_vertices": sorted(components, reverse=True)[:10],
}
(root / "topology_report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
print("MINI186_TOPOLOGY=" + json.dumps(report))
bm.free()
