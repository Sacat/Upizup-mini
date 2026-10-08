"""Build one measurable, textured game mesh from the Hi3D raw FBX."""

import bpy
import bmesh
import json
import sys
from pathlib import Path
from mathutils import Vector


root = Path(__file__).resolve().parent
source = root / "Hi3D_LalayTool_Modern_raw.fbx"
output = root / "LalayTool_3000tri.fbx"
texture_output = root / "LalayTool_1024.png"

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(source), use_image_search=True)
obj = next(o for o in bpy.context.scene.objects if o.type == "MESH")
bpy.context.view_layer.objects.active = obj
obj.select_set(True)
for other in bpy.context.selected_objects:
    if other != obj:
        other.select_set(False)

# Hi3D exports a near-centimetre bbox. Put real geometry into metres and apply
# the imported FBX transform, keeping the original cross-section proportions.
obj.matrix_world = obj.matrix_world @ __import__("mathutils").Matrix.Scale(19.0, 4)
bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

raw_triangles = len(obj.data.polygons)
target_triangles = 3000
modifier = obj.modifiers.new("GameTriangleBudget", "DECIMATE")
modifier.ratio = target_triangles / raw_triangles
bpy.ops.object.modifier_apply(modifier=modifier.name)

# The collapse pass may leave isolated sliver islands. Remove pieces with
# fewer than four vertices; the visual silhouette is the large component.
bm = bmesh.new()
bm.from_mesh(obj.data)
seen = set()
tiny = []
for vertex in bm.verts:
    if vertex in seen:
        continue
    stack = [vertex]
    seen.add(vertex)
    component = []
    while stack:
        current = stack.pop()
        component.append(current)
        for edge in current.link_edges:
            other = edge.other_vert(current)
            if other not in seen:
                seen.add(other)
                stack.append(other)
    if len(component) < 4:
        tiny.extend(component)
if tiny:
    bmesh.ops.delete(bm, geom=tiny, context="VERTS")
bm.to_mesh(obj.data)
bm.free()

image = next((img for img in bpy.data.images if img.size[0] == 8192), None)
if image is None:
    raise RuntimeError("Embedded Hi3D color texture missing")
image.scale(1024, 1024)
image.filepath_raw = str(texture_output)
image.file_format = "PNG"
image.save()
image.filepath = str(texture_output)

for mat in obj.data.materials:
    if mat and mat.use_nodes:
        for node in mat.node_tree.nodes:
            if node.type == "TEX_IMAGE":
                node.image = image

obj.name = "LalayTool_Modern"
obj.data.name = "LalayTool_Modern_Mesh"
obj.select_set(True)
bpy.ops.export_scene.fbx(
    filepath=str(output), use_selection=True, object_types={"MESH"},
    apply_unit_scale=True, add_leaf_bones=False, path_mode="RELATIVE",
    embed_textures=False,
)

report = {
    "source_triangles": raw_triangles,
    "game_triangles": len(obj.data.polygons),
    "game_vertices": len(obj.data.vertices),
    "target_triangles": target_triangles,
    "texture_size": list(image.size),
    "output_fbx_bytes": output.stat().st_size,
    "output_texture_bytes": texture_output.stat().st_size,
    "dimensions_metres": list(obj.dimensions),
}
(root / "game_mesh_report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
print("MINI186_GAME_MESH=" + json.dumps(report))
