import bpy
import json
import math
import os
from mathutils import Matrix, Vector

SRC = r"E:\Unity\Up Iz Up Mini\Assets\Tmax 560.glb"          # pristine original, never written to
DST = r"E:\Unity\Up Iz Up Mini\Assets\UpIzUpMini\Vehicles\TMAX_560_clean.glb"
OUT = r"C:\Users\PCSS-PC\AppData\Local\Temp\claude\E--Unity-Up-Iz-Up-Mini\a683e6f2-f1e1-422d-9273-a9d5eacc6a9a\scratchpad\tmax"

REAL_LENGTH_M = 2.195
TARGET_TRIS = 15000

report = {"stages": []}

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=SRC)
obj = next(o for o in bpy.context.scene.objects if o.type == 'MESH')
bpy.context.view_layer.objects.active = obj
obj.select_set(True)

report["orig_polys"] = len(obj.data.polygons)

mod = obj.modifiers.new(name="Decimate", type='DECIMATE')
mod.ratio = TARGET_TRIS / max(1, len(obj.data.polygons))
bpy.ops.object.modifier_apply(modifier=mod.name)
report["decimated_polys"] = len(obj.data.polygons)

mesh = obj.data

# Bake whatever transform the import gave us straight into the mesh data,
# then work purely on mesh data from here. Using mesh.transform() instead of
# bpy.ops.object.transform_apply() is deliberate: the operator silently
# used a stale object matrix in headless Blender (an earlier attempt
# reported a 31 degree yaw correction that provably never applied - the
# rendered top view came back byte-identical), whereas mesh.transform() is
# a direct, deterministic data operation with no operator/context or
# depsgraph-timing dependency.
mesh.transform(obj.matrix_world)
obj.matrix_world = Matrix.Identity(4)

def bbox():
    xs = [v.co.x for v in mesh.vertices]
    ys = [v.co.y for v in mesh.vertices]
    zs = [v.co.z for v in mesh.vertices]
    return (min(xs), min(ys), min(zs)), (max(xs), max(ys), max(zs))

def log(stage):
    lo, hi = bbox()
    report["stages"].append({
        "stage": stage,
        "min": [round(v, 4) for v in lo],
        "max": [round(v, 4) for v in hi],
        "size": [round(hi[i]-lo[i], 4) for i in range(3)],
    })

log("after_import_and_decimate")

# ---- 1) Find the true horizontal orientation (minimum-area rectangle) ----
# The scan is yaw-rotated, so its axis-aligned bbox is inflated and cannot
# be used to measure real length. Brute-force search for the rotation that
# minimises the horizontal bounding-rectangle AREA - the classic
# minimum-area-rectangle fit. Chosen over PCA because PCA is weighted by
# vertex density, and a decimated photogrammetry scan has wildly uneven
# density (dense around detailed panels, sparse on flat bodywork), which
# skews the result; min-area-rect depends only on silhouette.
pts = [(v.co.x, v.co.y) for v in mesh.vertices]
best = None
steps = 720  # 0.25 degree resolution over 180 degrees
for i in range(steps):
    a = math.pi * i / steps
    ca, sa = math.cos(a), math.sin(a)
    xs = [p[0]*ca + p[1]*sa for p in pts]
    ys = [-p[0]*sa + p[1]*ca for p in pts]
    w = max(xs) - min(xs)
    h = max(ys) - min(ys)
    area = w * h
    if best is None or area < best[0]:
        best = (area, a, w, h)

area, angle, w, h = best
# The search loop above evaluates coordinates as
#   X = x*cos(a) + y*sin(a);  Y = -x*sin(a) + y*cos(a)
# which is a rotation of the cloud by -a, so the matrix that actually
# reproduces the measured rectangle is Rz(-a), not Rz(+a). Applying the
# wrong sign here is what left the bike's long axis on X on the previous
# run and then corrupted the scale step downstream.
rot = -angle
# Orient so the LONGER side of that rectangle ends up along +Y.
if w > h:
    rot += math.pi / 2
report["min_area_rect_angle_deg"] = round(math.degrees(angle), 3)
report["applied_rotation_deg"] = round(math.degrees(rot), 3)
report["min_area_rect_w_h"] = [round(w, 4), round(h, 4)]

mesh.transform(Matrix.Rotation(rot, 4, 'Z'))
log("after_yaw_align")

# Self-check: after aligning, a motorcycle MUST be clearly longer (Y) than
# it is wide (X). Asserting this catches a silently-wrong rotation instead
# of letting it corrupt the scale step and only surface as a broken-looking
# bike in the user's Play Mode session, which is exactly how the previous
# two attempts failed.
_lo, _hi = bbox()
_w = _hi[0] - _lo[0]
_l = _hi[1] - _lo[1]
report["align_check_width"] = round(_w, 4)
report["align_check_length"] = round(_l, 4)
if _l <= _w * 1.4:
    raise RuntimeError(
        f"Alignment failed: after yaw-align the model is {_l:.3f}m long vs "
        f"{_w:.3f}m wide - a motorcycle must be far longer than it is wide, "
        f"so the rotation is wrong.")

# ---- 2) Front/back: a maxi-scooter's front (screen + bars) is taller ----
ys = [v.co.y for v in mesh.vertices]
mid = (min(ys) + max(ys)) / 2
plus_peak = max((v.co.z for v in mesh.vertices if v.co.y > mid), default=0.0)
minus_peak = max((v.co.z for v in mesh.vertices if v.co.y <= mid), default=0.0)
report["plus_y_peak"] = round(plus_peak, 4)
report["minus_y_peak"] = round(minus_peak, 4)
if minus_peak > plus_peak:
    mesh.transform(Matrix.Rotation(math.pi, 4, 'Z'))
    report["flipped_180"] = True
else:
    report["flipped_180"] = False
log("after_front_orientation")

# ---- 3) Level pitch so BOTH tyres reach the ground ----
# The previous attempt looked for ground contacts inside a fixed 0.12m band
# above the model's single lowest point. That silently found nothing in the
# rear half - this scan is pitched nose-down, so only the front tyre reaches
# z=0 while the rear tyre floats ~0.18m up, far outside that band - and the
# `if fr and re:` guard quietly skipped the correction entirely. The bike
# therefore shipped tilted, with its rear wheel hanging in the air.
# Binning the underside profile along the length finds each tyre's own
# lowest point independently, with no assumption that they start level.
ys2 = [v.co.y for v in mesh.vertices]
lo_y, hi_y = min(ys2), max(ys2)
NB = 24
bin_min = [None] * NB
for v in mesh.vertices:
    idx = int((v.co.y - lo_y) / (hi_y - lo_y) * NB)
    if idx >= NB: idx = NB - 1
    if bin_min[idx] is None or v.co.z < bin_min[idx]:
        bin_min[idx] = v.co.z

def bin_y(i):
    return lo_y + (hi_y - lo_y) * (i + 0.5) / NB

half = NB // 2
front_bins = [(bin_min[i], i) for i in range(half, NB) if bin_min[i] is not None]
rear_bins = [(bin_min[i], i) for i in range(0, half) if bin_min[i] is not None]

if front_bins and rear_bins:
    fz, fi = min(front_bins)
    rz, ri = min(rear_bins)
    fy, ry = bin_y(fi), bin_y(ri)
    report["front_tyre_low"] = [round(fy, 4), round(fz, 4)]
    report["rear_tyre_low"] = [round(ry, 4), round(rz, 4)]
    # Rotating by theta about X maps z -> y*sin(theta) + z*cos(theta); solving
    # z_front' == z_rear' gives tan(theta) = (z_rear - z_front)/(y_front - y_rear).
    pitch = math.atan2(rz - fz, fy - ry)
    report["detected_pitch_deg"] = round(math.degrees(pitch), 3)
    if abs(pitch) > math.radians(0.3):
        mesh.transform(Matrix.Rotation(pitch, 4, 'X'))
        report["applied_pitch_fix"] = True
    else:
        report["applied_pitch_fix"] = False
log("after_pitch_level")

# Self-check: after levelling, both tyres must sit within a couple of cm of
# the same height, or the bike will visibly stand on one wheel.
ys3 = [v.co.y for v in mesh.vertices]
lo_y2, hi_y2 = min(ys3), max(ys3)
bm = [None] * NB
for v in mesh.vertices:
    idx = int((v.co.y - lo_y2) / (hi_y2 - lo_y2) * NB)
    if idx >= NB: idx = NB - 1
    if bm[idx] is None or v.co.z < bm[idx]:
        bm[idx] = v.co.z
f2 = min(z for z in bm[half:] if z is not None)
r2 = min(z for z in bm[:half] if z is not None)
report["post_level_front_low"] = round(f2, 4)
report["post_level_rear_low"] = round(r2, 4)
if abs(f2 - r2) > 0.03:
    raise RuntimeError(
        f"Levelling failed: front tyre bottom {f2:.3f} vs rear {r2:.3f} - "
        f"the bike would stand on one wheel.")

# ---- 4) Scale by the TRUE, now axis-aligned length ----
ys = [v.co.y for v in mesh.vertices]
true_len = max(ys) - min(ys)
report["true_length_before_scale"] = round(true_len, 4)
s = REAL_LENGTH_M / true_len if true_len > 1e-6 else 1.0
report["scale_factor"] = round(s, 5)
mesh.transform(Matrix.Scale(s, 4))
log("after_scale")

# ---- 5) Pivot at ground-level centre, geometry moved onto the origin ----
lo, hi = bbox()
cx = (lo[0] + hi[0]) / 2
cy = (lo[1] + hi[1]) / 2
mesh.transform(Matrix.Translation(Vector((-cx, -cy, -lo[2]))))
obj.location = (0.0, 0.0, 0.0)
obj.rotation_euler = (0.0, 0.0, 0.0)
obj.scale = (1.0, 1.0, 1.0)
log("after_recenter")

# ---- 6) Measure the real wheel contact points for the physics rig ----
lo, hi = bbox()
zs = [v.co.z for v in mesh.vertices]
band = [v.co for v in mesh.vertices if v.co.z < min(zs) + 0.09]
mid3 = (lo[1] + hi[1]) / 2
fr = [c.y for c in band if c.y > mid3]
re = [c.y for c in band if c.y <= mid3]
if fr and re:
    fcy = sum(fr)/len(fr)
    rcy = sum(re)/len(re)
    report["front_contact_y"] = round(fcy, 4)
    report["rear_contact_y"] = round(rcy, 4)
    report["measured_wheelbase"] = round(fcy - rcy, 4)

report["final_size"] = [round(hi[i]-lo[i], 4) for i in range(3)]

mesh.update()
os.makedirs(os.path.dirname(DST), exist_ok=True)
bpy.ops.export_scene.gltf(filepath=DST, export_format='GLB', use_selection=False)

# ---- 7) Render proof views ----
scene = bpy.context.scene
scene.render.engine = 'BLENDER_WORKBENCH'
scene.render.resolution_x = 640
scene.render.resolution_y = 640
cy2 = (lo[1] + hi[1]) / 2
cz2 = (lo[2] + hi[2]) / 2
span = max(report["final_size"]) * 1.35
cam_data = bpy.data.cameras.new("Cam")
cam_data.type = 'ORTHO'
cam_data.ortho_scale = span
cam = bpy.data.objects.new("Cam", cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
d = span * 2

def shoot(name, loc, rot_deg):
    cam.location = loc
    cam.rotation_euler = [math.radians(a) for a in rot_deg]
    scene.render.filepath = os.path.join(OUT, name)
    bpy.ops.render.render(write_still=True)

shoot("fix2_front.png", (0.0, cy2 - d, cz2), (90, 0, 0))
shoot("fix2_side.png",  (-d, cy2, cz2), (90, 0, -90))
shoot("fix2_top.png",   (0.0, cy2, cz2 + d), (0, 0, 0))

with open(os.path.join(OUT, "realign2_report.json"), "w") as f:
    json.dump(report, f, indent=2)

print("REALIGN2_DONE")
print(json.dumps(report, indent=2))
