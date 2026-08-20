# TMAX asset pipeline (MINI-064)

Regenerates `Assets/UpIzUpMini/Vehicles/TMAX_560_clean.glb` from the pristine
source scan at `Assets/Tmax 560.glb`. **The source GLB is only ever read,
never written to.**

These live here rather than in scratch because the cleaned GLB is a build
artifact - without them there is no record of how it was produced, and the
corrections below are not obvious enough to rediscover.

## Why the source needs this much work

`Assets/Tmax 560.glb` is a photogrammetry-style scan, not an authored game
asset:

- one fused mesh, ~1,992,000 triangles, one material;
- **2,353 disconnected mesh islands** - so wheels cannot be split off as
  loose parts;
- yaw-rotated ~39 degrees within its own coordinate space;
- pitched nose-down ~6.7 degrees, so only the front tyre reaches the ground;
- pivot nowhere near the geometry.

Every one of those had to be corrected before the model could sit on a road.

## Scripts

Run with a headless Blender, e.g.:

```bash
"C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" --background --python tmax_clean_and_align.py
```

| Script | Purpose |
| --- | --- |
| `tmax_clean_and_align.py` | The real pipeline: decimate to ~15k tris, yaw-align via minimum-area-rectangle fit, orient nose to +Y, level pitch so both tyres touch the ground, scale to the real 2.195m length, move the pivot to ground-centre, export, and render three proof views. |
| `tmax_groundprobe.py` | Diagnostic. Reports which parts of the model actually touch the ground plane, plus an underside profile along the bike's length. This is what revealed the nose-down pitch. |
| `tmax_axlecheck.py` | Diagnostic. Draws rings at candidate axle positions and renders a side view, so wheel-collider placement can be checked visually against the real wheels. |

## Why Blender renders the proof images

Unity's headless renderer segfaults in this environment (documented in
`PROJECT-HANDOFF.md`), so Blender's Workbench engine is the only working way
to actually *look* at the model without opening the Editor by hand. Several
bugs in this task were invisible to structural validation and only showed up
in these renders.

## Built-in self-checks

The pipeline hard-fails rather than silently shipping a broken asset if:

- after yaw alignment the model is not clearly longer than it is wide;
- after levelling the two tyres differ in height by more than 3cm.

Both guards exist because earlier versions failed silently in exactly those
ways and the breakage only surfaced in the user's Play Mode session.

## Expected output

| Measure | Result | Real TMAX 560 |
| --- | --- | --- |
| Length | 2.195 m | 2.195 m |
| Width | 0.822 m | 0.780 m (plus mirrors) |
| Height | 1.487 m | ~1.42 m (this scan has a tall touring screen) |
| Wheelbase (measured downstream in Unity) | 1.523 m | 1.575 m |
| Wheel radius (measured) | 0.308 m | ~0.30 m |

Unity-side wheel placement is measured from the mesh at prefab-build time by
`Mini064TmaxAssetPrep.MeasureBike()`, not hardcoded - so re-running this
pipeline and rebuilding the prefab keeps the physics rig in sync with the art.
