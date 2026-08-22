import bpy
import json
from mathutils import Vector

FILES = {
    "source": r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-105\AccuRigOutput\Sacat-ModularBase-Rigged-Unity.fbx",
    "lod0": r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-106\BlenderOutput\SacatModularBase_LOD0.fbx",
}
OUT = r"E:\Unity\Up Iz Up Mini\Docs\CharacterPipeline\MINI-106\BlenderOutput\LOD-Orientation-Audit.json"


def values(v):
    return [round(float(item), 6) for item in v]


def audit(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=path, use_anim=False)
    mesh = next(obj for obj in bpy.context.scene.objects if obj.type == "MESH")
    armature = next(obj for obj in bpy.context.scene.objects if obj.type == "ARMATURE")
    corners = [mesh.matrix_world @ Vector(corner) for corner in mesh.bound_box]
    minimum = Vector((min(p.x for p in corners), min(p.y for p in corners), min(p.z for p in corners)))
    maximum = Vector((max(p.x for p in corners), max(p.y for p in corners), max(p.z for p in corners)))
    return {
        "mesh": mesh.name,
        "mesh_location": values(mesh.location),
        "mesh_rotation_euler": values(mesh.rotation_euler),
        "mesh_scale": values(mesh.scale),
        "mesh_world_bounds_min": values(minimum),
        "mesh_world_bounds_max": values(maximum),
        "mesh_world_bounds_size": values(maximum - minimum),
        "armature": armature.name,
        "armature_location": values(armature.location),
        "armature_rotation_euler": values(armature.rotation_euler),
        "armature_scale": values(armature.scale),
        "root_bones": [bone.name for bone in armature.data.bones if bone.parent is None],
        "bone_count": len(armature.data.bones),
        "mesh_parent": mesh.parent.name if mesh.parent else None,
        "armature_modifier": [mod.object.name for mod in mesh.modifiers if mod.type == "ARMATURE" and mod.object],
    }


with open(OUT, "w", encoding="utf-8") as handle:
    json.dump({key: audit(path) for key, path in FILES.items()}, handle, indent=2)
print("MINI106_ORIENTATION_AUDIT_PASS")
