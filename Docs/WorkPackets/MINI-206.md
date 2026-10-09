# MINI-206 cloud lane log

## Stage 4 (done first: inputs for stages 1-3 were not on GitHub yet) - riding analysis
- Made: `Docs/WorkPackets/MINI-206-riding.md`, draft `Assets/UpIzUpMini/Scripts/Vehicles/Draft/TmaxSuperMotoVisualBinder.cs` (inside `#if MINI206_DRAFT`, never compiled).
- Main off-road glitch: `extraAirGravity` 2500 m/s² (about 255 g) after only 0.01 s airborne (TmaxBikeControllerCustom.cs line 888) adds about 50 m/s downward in one physics step whenever both wheels unload.
- Main "robotic" cause: kinematic yaw lock at strength 1 (line 668) plus yaw-spin damping with a 0 °/s threshold, and two world-up upright systems (lines 1197, 1508) that keep the body from ever rolling.
- Also: trike outrigger springs firing off-road (line 987), a 9 m/s² hill-climb push, a kinematic wheelie.
- Path A: 10 inspector values to try (table in the riding doc). Path B: TMAX visuals on the SuperMoto physics, as a duplicate of TMAX_560_SuperMoto.prefab with a 7-step list.
- Known limits: nothing run in Unity; the SuperMoto's exact roll mechanism (FreezeRotationZ together with roll torque) is unconfirmed.
