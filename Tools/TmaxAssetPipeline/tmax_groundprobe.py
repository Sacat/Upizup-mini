import bpy, json, os

CLEAN = r"E:\Unity\Up Iz Up Mini\Assets\UpIzUpMini\Vehicles\TMAX_560_clean.glb"
OUT = r"C:\Users\PCSS-PC\AppData\Local\Temp\claude\E--Unity-Up-Iz-Up-Mini\a683e6f2-f1e1-422d-9273-a9d5eacc6a9a\scratchpad\tmax"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=CLEAN)
obj = next(o for o in bpy.context.scene.objects if o.type == 'MESH')
m = obj.matrix_world
co = [m @ v.co for v in obj.data.vertices]

r = {}
# Which parts of the bike actually touch the ground plane?
for thresh in (0.02, 0.05, 0.10, 0.20):
    low = [c for c in co if c.z < thresh]
    if not low:
        continue
    r[f"touching_z_lt_{thresh}"] = {
        "count": len(low),
        "y_min": round(min(c.y for c in low), 3),
        "y_max": round(max(c.y for c in low), 3),
        "x_min": round(min(c.x for c in low), 3),
        "x_max": round(max(c.x for c in low), 3),
    }

# Histogram of the lowest z found in each slice along the bike's length,
# which shows the true underside profile (and therefore where each tyre is).
ys = [c.y for c in co]
lo_y, hi_y = min(ys), max(ys)
nb = 22
prof = []
for i in range(nb):
    a = lo_y + (hi_y - lo_y) * i / nb
    b = lo_y + (hi_y - lo_y) * (i + 1) / nb
    sl = [c for c in co if a <= c.y < b]
    if sl:
        prof.append({
            "y_mid": round((a + b) / 2, 3),
            "min_z": round(min(c.z for c in sl), 3),
            "min_z_centre": round(min((c.z for c in sl if abs(c.x) < 0.13), default=-1), 3),
        })
r["underside_profile"] = prof

with open(os.path.join(OUT, "groundprobe.json"), "w") as f:
    json.dump(r, f, indent=2)
print("PROBE_DONE")
print(json.dumps(r, indent=2))
