"""MINI-157 - colour pass on Franki's already-reshaped garment (MINI-154's
Ch28_Hoody/Ch28_Pants, which are separate mesh objects each with their own
material slot already - no UV surgery needed here like Sacat, just clone
the shared material and set a flat colour per garment, the same pattern
CharacterEquipment.ApplyGarment already uses for tint-based clothing."""
import bpy, math, os

SRC = r"E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-154\Sacat-Garment-Prototype.blend"
OUT_DIR = r"E:\Unity\Up Iz Up Mini\Logs\Tasks\MINI-157"
os.makedirs(OUT_DIR, exist_ok=True)

SHIRT_COLOR = (0.09, 0.16, 0.32, 1.0)
PANTS_COLOR = (0.24, 0.24, 0.26, 1.0)

bpy.ops.wm.open_mainfile(filepath=SRC)

for obj_name, color in [("Ch28_Hoody", SHIRT_COLOR), ("Ch28_Pants", PANTS_COLOR)]:
    obj = bpy.data.objects[obj_name]
    orig_mat = obj.material_slots[0].material
    new_mat = orig_mat.copy()
    new_mat.name = f"{obj_name}_Reshaped_Colored"
    bsdf = next((n for n in new_mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
    assert bsdf is not None, f"{obj_name}: no Principled BSDF found in {new_mat.name}"
    # Unlink whatever currently feeds Base Color (texture or otherwise) and
    # set it to a flat colour directly - simple, guaranteed-visible tint,
    # same approach CharacterEquipment.ApplyGarment already uses elsewhere
    # in this project (material.color set directly). Pattern/shading detail
    # from the original texture is traded for reliability here; a
    # multiply-tint-over-texture pass is future polish, not this pass.
    base_color_input = bsdf.inputs['Base Color']
    for link in list(base_color_input.links):
        new_mat.node_tree.links.remove(link)
    base_color_input.default_value = color
    obj.material_slots[0].material = new_mat
    print(f"coloured {obj_name} -> {color}")

blend_path = os.path.join(OUT_DIR, "Franki-Garment-Prototype-Colored.blend")
bpy.ops.wm.save_as_mainfile(filepath=blend_path)
print("saved", blend_path)

for o in list(bpy.data.objects):
    if o.type in ('CAMERA', 'LIGHT'):
        bpy.data.objects.remove(o, do_unlink=True)
out_fbx = r"E:\Unity\Up Iz Up Mini\Assets\UpIzUpMini\Art\Characters\Garments\Franki_ReshapedGarments_Colored.fbx"
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=out_fbx, use_selection=True, add_leaf_bones=False,
                          bake_anim=False, mesh_smooth_type='FACE', path_mode='COPY',
                          embed_textures=True)
print("exported", out_fbx)

import mathutils
meshes = [o for o in bpy.data.objects if o.type == 'MESH']
gmin = mathutils.Vector((1e9, 1e9, 1e9)); gmax = mathutils.Vector((-1e9, -1e9, -1e9))
for o in meshes:
    mwv = o.matrix_world
    for v in o.data.vertices:
        wc = mwv @ v.co
        gmin = mathutils.Vector((min(gmin.x, wc.x), min(gmin.y, wc.y), min(gmin.z, wc.z)))
        gmax = mathutils.Vector((max(gmax.x, wc.x), max(gmax.y, wc.y), max(gmax.z, wc.z)))
center = (gmin + gmax) / 2
height = gmax.z - gmin.z

scene = bpy.context.scene
engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items]
scene.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in engines else 'BLENDER_EEVEE'
scene.render.resolution_x = 900
scene.render.resolution_y = 1400
world = bpy.data.worlds.new("W_franki_color")
scene.world = world
world.use_nodes = True
bg = world.node_tree.nodes.get("Background")
if bg:
    bg.inputs[0].default_value = (0.82, 0.82, 0.85, 1)
    bg.inputs[1].default_value = 1.0

def look_at(obj, target):
    d = target - obj.location
    obj.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()

sun = bpy.data.lights.new("SunFC", type='SUN'); sun.energy = 3.0
sun_obj = bpy.data.objects.new("SunFC", sun); scene.collection.objects.link(sun_obj)
sun_obj.rotation_euler = (math.radians(55), 0, math.radians(35))
fill = bpy.data.lights.new("FillFC", type='SUN'); fill.energy = 1.5
fill_obj = bpy.data.objects.new("FillFC", fill); scene.collection.objects.link(fill_obj)
fill_obj.rotation_euler = (math.radians(60), 0, math.radians(-140))

target = mathutils.Vector((center.x, center.y, center.z))
dist = 3.0
views = {
    "Franki-Colored-Front": mathutils.Vector((0, -dist, center.z)),
    "Franki-Colored-Back": mathutils.Vector((0, dist, center.z)),
}
for name, loc in views.items():
    cam_data = bpy.data.cameras.new(f"Cam_{name}")
    cam_data.type = 'ORTHO'
    cam_data.ortho_scale = height * 1.15
    cam_obj = bpy.data.objects.new(f"Cam_{name}", cam_data)
    scene.collection.objects.link(cam_obj)
    cam_obj.location = loc
    look_at(cam_obj, target)
    scene.camera = cam_obj
    scene.render.filepath = os.path.join(OUT_DIR, f"{name}.png")
    bpy.ops.render.render(write_still=True)
    print("Rendered", scene.render.filepath)
