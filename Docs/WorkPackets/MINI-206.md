# MINI-206 cloud lane log

## Stage 4 (done first: inputs for stages 1-3 were not on GitHub yet) - riding analysis
- Made: `Docs/WorkPackets/MINI-206-riding.md`, draft `Assets/UpIzUpMini/Scripts/Vehicles/Draft/TmaxSuperMotoVisualBinder.cs` (inside `#if MINI206_DRAFT`, never compiled).
- Main off-road glitch: `extraAirGravity` 2500 m/s² (about 255 g) after only 0.01 s airborne (TmaxBikeControllerCustom.cs line 888) adds about 50 m/s downward in one physics step whenever both wheels unload.
- Main "robotic" cause: kinematic yaw lock at strength 1 (line 668) plus yaw-spin damping with a 0 °/s threshold, and two world-up upright systems (lines 1197, 1508) that keep the body from ever rolling.
- Also: trike outrigger springs firing off-road (line 987), a 9 m/s² hill-climb push, a kinematic wheelie.
- Path A: 10 inspector values to try (table in the riding doc). Path B: TMAX visuals on the SuperMoto physics, as a duplicate of TMAX_560_SuperMoto.prefab with a 7-step list.
- Known limits: nothing run in Unity; the SuperMoto's exact roll mechanism (FreezeRotationZ together with roll torque) is unconfirmed.

## Stage 1 note (2026-10-09): TMAX 560 rebuilt from the Hitem3D scan (lean version, per the owner: function over polish)
- Files: `Docs/CharacterPipeline/MINI-206/TMAX/` (`TMAX_560_Rebuilt.fbx`, `_LOD1.fbx`, `.blend`, 5 textures, `parts_report.json`, `legacy_anchor_map.json`, `renders/`); scripts in `Tools/CharacterPipeline/mini206_tmax/`.
- Scan aligned by PCA + mirror symmetry + tyre contacts; scaled so the wheelbase is the real 1.575 m (length 2.225 m, height 1.505 m). The in-game glb is the same scan decimated (ICP fit 3.6 mm median) and sat yawed ~7.5 deg, which is why the old rear wheel was 16 cm off-centre.
- Parts (all watertight, own pivots): `TMAX_FrontWheel` (axle, r 0.304 m) 2,464 tris, `TMAX_RearWheel` (r 0.254 m) 2,272, `TMAX_FrontForkAssembly` (fork legs + calipers + scan fender, local up = 25 deg steering axis) 1,380, `TMAX_Handlebar` (own pivot at the clamp, same axis direction) 564, `TMAX_Body` 14,208 (seat, screen, mirrors, lights, plate, exhaust, swingarm stay fused in the body). LOD0 20,888 / LOD1 10,450 tris.
- Wheels, fork and handlebar are rebuilt procedurally (round, spin cleanly); body keeps the scan's UV layout: 2048 base colour + metallic-smoothness (resampled from the 4096 scan maps) + 2048 normal baked from the 2M mesh. Moving parts share a 512 colour/metal atlas. Two materials (`TMAX_Body`, `TMAX_Parts`), not one.
- Empties: Seat_Driver, Seat_Pillion, FootPeg_L/R, PillionPeg_L/R, PillionHandle_L/R, FrontAxle, RearAxle, GripLeft/Right (under the handlebar). Unity positions in `parts_report.json`; old prefab anchors mapped into the new frame in `legacy_anchor_map.json`.
- Measured: steering +-30 deg renders show no fork/wheel clipping; 0-4 sampled bar vertices touch the cover at full lock. Wheel radii follow the scan (spec would be 0.2745/0.2865 m but the scan bodywork is shaped round its own wheels).
- KNOWN PROBLEMS: (1) small pieces of the scan's own switch blocks/levers remain in the body ahead of the new handlebar (visible from the rider's view at full lock); fix = extend the bar cutter to y -0.42 in `tmax_build.py` (one line, not run to save budget). (2) one touching-edge (bow-tie) on the hidden rear-wheel cut face of the body. (3) number plate text is mirrored in the scan texture. (4) screen is opaque (scan texture), no glass material. (5) swingarm is static in the body; the rear wheel moves alone with the suspension.
