import bpy, math, os

CLEAN = r"E:\Unity\Up Iz Up Mini\Assets\UpIzUpMini\Vehicles\TMAX_560_clean.glb"
OUT = r"C:\Users\PCSS-PC\AppData\Local\Temp\claude\E--Unity-Up-Iz-Up-Mini\a683e6f2-f1e1-422d-9273-a9d5eacc6a9a\scratchpad\tmax"

R = 0.308
FRONT_Y = 0.820
CANDIDATES = [
    ("final_rear", -0.704),
]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=CLEAN)

def ring(name, y, radius, colorless=True):
    # A thin torus standing in the bike's centre plane, marking where a
    # wheel of this radius centred at (0, y, radius) would actually sit.
    bpy.ops.mesh.primitive_torus_add(
        major_radius=radius, minor_radius=0.012,
        location=(0.0, y, radius),
        rotation=(0.0, math.radians(90), 0.0),
    )
    bpy.context.active_object.name = name

ring("FRONT_AXLE", FRONT_Y, R)
for name, y in CANDIDATES:
    ring(name, y, R)

scene = bpy.context.scene
scene.render.engine = 'BLENDER_WORKBENCH'
scene.render.resolution_x = 1100
scene.render.resolution_y = 620
scene.display.shading.show_xray = True

cam_data = bpy.data.cameras.new("Cam")
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 2.5
cam = bpy.data.objects.new("Cam", cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (-6.0, 0.0, 0.75)
cam.rotation_euler = (math.radians(90), 0.0, math.radians(-90))

scene.render.filepath = os.path.join(OUT, "axle_check.png")
bpy.ops.render.render(write_still=True)
print("AXLECHECK_DONE")
