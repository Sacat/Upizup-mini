"""MINI-164 - quick standalone render of the two new hair FBXs to eyeball
colour/shape before any Unity integration."""
import bpy, os, sys, math, mathutils

ROOT = r'E:\Unity\Up Iz Up Mini'
OUT = ROOT + '/Logs/Tasks/MINI-164'
os.makedirs(OUT, exist_ok=True)

fbx = sys.argv[-2]
tag = sys.argv[-1]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=fbx)
bpy.context.scene.frame_set(0)

scn = bpy.context.scene
scn.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE'
scn.render.resolution_x = 700
scn.render.resolution_y = 700
scn.render.film_transparent = False

# world grey
world = bpy.data.worlds.new('w'); scn.world = world
world.use_nodes = True
bg = world.node_tree.nodes['Background']
bg.inputs[0].default_value = (0.55, 0.55, 0.58, 1.0)
bg.inputs[1].default_value = 1.0

# find head bone / mesh bounds
meshes = [o for o in bpy.data.objects if o.type == 'MESH']
arm = next((o for o in bpy.data.objects if o.type == 'ARMATURE'), None)
head = None
if arm:
    for n in ('mixamorig9:Head', 'mixamorig10:Head', 'mixamorig:Head'):
        if n in arm.pose.bones:
            head = (arm.matrix_world @ arm.pose.bones[n].head)
            break
if head is None:
    zmax = max((o.matrix_world @ mathutils.Vector(c)).z for o in meshes for c in o.bound_box)
    head = mathutils.Vector((0, 0, zmax - 0.15))

def light(name, rot, energy):
    d = bpy.data.lights.new(name, 'SUN'); d.energy = energy
    o = bpy.data.objects.new(name, d); scn.collection.objects.link(o)
    o.rotation_euler = [math.radians(a) for a in rot]

light('key', (55, 0, -35), 4.0)
light('fill', (60, 0, 150), 2.0)

cam_d = bpy.data.cameras.new('c'); cam = bpy.data.objects.new('c', cam_d)
scn.collection.objects.link(cam); scn.camera = cam
cam_d.lens = 70

def shot(offset, name):
    focus = head + mathutils.Vector((0, 0, 0.02))
    cam.location = focus + offset
    dirv = (focus - cam.location).normalized()
    cam.rotation_euler = dirv.to_track_quat('-Z', 'Y').to_euler()
    scn.render.filepath = f'{OUT}/{name}.png'
    bpy.ops.render.render(write_still=True)

shot(mathutils.Vector((0, -0.55, 0.05)), f'{tag}-Front')
shot(mathutils.Vector((0.55, -0.05, 0.05)), f'{tag}-Side')
shot(mathutils.Vector((0.30, 0.45, 0.08)), f'{tag}-Back34')
print(f'MINI164_RENDER_{tag}_DONE')
