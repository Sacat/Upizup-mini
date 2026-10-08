import bpy
from pathlib import Path

root = Path(__file__).resolve().parent
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(root / "LalayTool_3000tri.fbx"))
mesh = next(o for o in bpy.context.scene.objects if o.type == "MESH").data
for name, test in [("negative_z", lambda z: z < -0.06), ("positive_z", lambda z: z > 0.06)]:
    points = [v.co for v in mesh.vertices if test(v.co.z)]
    print(f"MINI186_DIRECTION {name} count={len(points)} y_min={min(p.y for p in points):.5f} y_max={max(p.y for p in points):.5f} y_mean={sum(p.y for p in points)/len(points):.5f}")
