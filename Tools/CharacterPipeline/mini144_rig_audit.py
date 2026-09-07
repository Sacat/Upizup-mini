"""Read-only FBX audit. Writes evidence only under Logs/Tasks/MINI-144."""
import bpy
import json
import os
import math
from mathutils import Quaternion

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Logs', 'Tasks', 'MINI-144')
SOURCES = {
    'master': 'Docs/CharacterPipeline/MINI-105/AccuRigOutput/Sacat-ModularBase-Rigged-Unity.fbx',
    'LOD0': 'Assets/UpIzUpMini/Art/Characters/Modular/Sacat/Mobile/SacatModularBase_LOD0.fbx',
    'LOD1': 'Assets/UpIzUpMini/Art/Characters/Modular/Sacat/Mobile/SacatModularBase_LOD1.fbx',
    'LOD2': 'Assets/UpIzUpMini/Art/Characters/Modular/Sacat/Mobile/SacatModularBase_LOD2.fbx',
}

def extents(points):
    return [max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)]

def audit(label, relative):
    path = os.path.join(ROOT, relative)
    if not os.path.isfile(path):
        return {'path': relative, 'missing': True}
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=path, use_anim=False)
    arms = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE']
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    report = {'path': relative, 'armatures': [], 'meshes': []}
    for arm in arms:
        report['armatures'].append({'name': arm.name, 'bones': len(arm.data.bones),
            'scale': list(arm.scale), 'rotation': list(arm.rotation_euler)})
    for body in meshes:
        arm = body.find_armature()
        bones = set(arm.data.bones.keys()) if arm else set()
        groups = {g.index: g.name for g in body.vertex_groups}
        unknown = sorted(set(groups.values())-bones)
        unweighted = over4 = badsum = 0
        for v in body.data.vertices:
            weights = [g.weight for g in v.groups if groups[g.group] in bones and g.weight > 0.00001]
            unweighted += not weights
            over4 += len(weights) > 4
            badsum += abs(sum(weights)-1) > .005
        bpy.context.view_layer.update()
        baseline = body.evaluated_get(bpy.context.evaluated_depsgraph_get())
        base_mesh = baseline.to_mesh()
        rest = [baseline.matrix_world @ v.co for v in base_mesh.vertices]
        baseline.to_mesh_clear()
        details = {'name': body.name, 'vertices': len(rest),
            'triangles': sum(len(p.vertices)-2 for p in body.data.polygons),
            'dimensions': extents(rest), 'scale': list(body.scale),
            'unknownGroups': unknown, 'unweighted': unweighted, 'over4Weights': over4,
            'nonNormalized': badsum, 'materials': [m.name if m else None for m in body.data.materials],
            'poseTests': []}
        if arm:
            for keyword in ['L_Upperarm', 'L_Forearm', 'L_Thigh', 'L_Calf']:
                bone = next((b for b in arm.pose.bones if keyword.lower() in b.name.lower()), None)
                if bone is None:
                    continue
                original_basis = bone.matrix_basis.copy()
                original_mode = bone.rotation_mode
                bone.rotation_mode = 'QUATERNION'
                bone.rotation_quaternion = bone.rotation_quaternion @ Quaternion((1,0,0), math.radians(60))
                bpy.context.view_layer.update()
                dg = bpy.context.evaluated_depsgraph_get()
                evaluated = body.evaluated_get(dg)
                mesh = evaluated.to_mesh()
                moved = [evaluated.matrix_world @ v.co for v in mesh.vertices]
                details['poseTests'].append({'bone': bone.name, 'dimensions': extents(moved),
                    'maxDisplacement': max((a-b).length for a,b in zip(moved, rest))})
                evaluated.to_mesh_clear()
                bone.matrix_basis = original_basis
                bone.rotation_mode = original_mode
                bpy.context.view_layer.update()
            details['boneNames'] = list(arm.data.bones.keys())
        report['meshes'].append(details)
    return report

os.makedirs(OUT, exist_ok=True)
result = {label: audit(label, path) for label, path in SOURCES.items()}
with open(os.path.join(OUT, 'rig-audit.json'), 'w', encoding='utf-8') as f:
    json.dump(result, f, indent=2)
print('MINI144_RIG_AUDIT_COMPLETE')
