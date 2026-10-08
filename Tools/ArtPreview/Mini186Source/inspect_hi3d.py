"""Read-only FBX inspection for MINI-186. Run with Blender 5 in background mode."""

import bpy
import json
import sys
from mathutils import Vector
from pathlib import Path


root = Path(__file__).resolve().parent
source = root / "Hi3D_LalayTool_Modern_raw.fbx"
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(source), use_image_search=True)

meshes = []
corners = []
for obj in bpy.context.scene.objects:
    if obj.type != "MESH":
        continue
    mesh = obj.data
    triangles = sum(max(0, len(face.vertices) - 2) for face in mesh.polygons)
    corners.extend(obj.matrix_world @ Vector(corner) for corner in obj.bound_box)
    meshes.append({
        "name": obj.name,
        "vertices": len(mesh.vertices),
        "polygons": len(mesh.polygons),
        "triangles": triangles,
        "uv_layers": len(mesh.uv_layers),
        "materials": [mat.name if mat else None for mat in mesh.materials],
        "dimensions": list(obj.dimensions),
    })

mins = [min(point[i] for point in corners) for i in range(3)] if corners else [0] * 3
maxs = [max(point[i] for point in corners) for i in range(3)] if corners else [0] * 3
images = []
for img in bpy.data.images:
    images.append({
        "name": img.name,
        "size": list(img.size),
        "source": img.source,
        "file": bpy.path.abspath(img.filepath) if img.filepath else "",
        "packed": img.packed_file is not None,
    })
report = {
    "source": str(source),
    "fbx_bytes": source.stat().st_size,
    "bbox_min": mins,
    "bbox_max": maxs,
    "bbox_size": [maxs[i] - mins[i] for i in range(3)],
    "mesh_count": len(meshes),
    "vertices": sum(mesh["vertices"] for mesh in meshes),
    "triangles": sum(mesh["triangles"] for mesh in meshes),
    "meshes": meshes,
    "images": images,
}
output = root / "raw_inspection.json"
output.write_text(json.dumps(report, indent=2), encoding="utf-8")
print("MINI186_INSPECTION=" + json.dumps(report))
