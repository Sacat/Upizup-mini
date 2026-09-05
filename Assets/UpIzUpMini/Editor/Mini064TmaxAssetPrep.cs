using UnityEditor;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-064: assembles the reusable TMAX_560 prefab from the cleaned
    /// mesh (see Mini064TmaxInspect/the Blender cleanup pass recorded in
    /// PROJECT-HANDOFF.md - the original "Assets/Tmax 560.glb" is a
    /// ~2M-triangle, 2353-fragment photogrammetry-style scan with no
    /// separate wheel geometry and no clean hierarchy, so it was decimated
    /// to 15k tris and real-world scaled via Blender rather than hand-
    /// authored here; that original file is left completely untouched).
    ///
    /// Hierarchy (per the user's specified architecture):
    /// <code>
    /// TMAX_560
    /// ├── Physics
    /// │   ├── FrontWheelCollider
    /// │   └── RearWheelCollider
    /// ├── COM
    /// ├── VisualLeanRoot
    /// │   ├── Body                 (the cleaned scan mesh - static, "stiff" wheels included)
    /// │   ├── FrontSteering
    /// │   │    └── FrontWheel      (a small spinning disc overlay - see class doc below)
    /// │   └── RearWheel            (same)
    /// ├── Seat
    /// ├── HandlebarLeft / HandlebarRight
    /// ├── LeftFootTarget / RightFootTarget
    /// └── CameraTarget
    /// </code>
    ///
    /// The root Rigidbody stays close to upright (physics root); the
    /// motorcycle "look" of leaning happens entirely on VisualLeanRoot, per
    /// the user's explicit architecture note - never force the physics
    /// body itself to the full visual lean angle.
    ///
    /// Wheel visuals: because the source scan has no separable wheel mesh
    /// (2353 disconnected fragments, not two clean loose parts - see the
    /// PROJECT-HANDOFF.md investigation note), the real wheels baked into
    /// the Body mesh stay visually static ("stiff", which the user
    /// explicitly accepted for this version - "leave them so, we will
    /// upgrade it in a future version"). To still give "the illusion that
    /// they turn" (also explicitly requested), a thin dark cylinder is
    /// added at each wheel-hub position and spun by TmaxWheelVisuals via
    /// WheelCollider.GetWorldPose - a cheap rotating overlay roughly
    /// coincident with the real (static) wheel, not a mesh edit.
    /// </summary>
    public static class Mini064TmaxAssetPrep
    {
        private const string CleanedMeshPath = "Assets/UpIzUpMini/Vehicles/TMAX_560_clean.glb";
        private const string PrefabPath = "Assets/UpIzUpMini/Vehicles/TMAX_560.prefab";

        // Estimated from the real TMAX 560's spec (Docs/MASTER-DEVELOPMENT-BRIEF.md
        // MINI-064: ~2.195m length, ~1.575m wheelbase) since the source
        // scan has no identifiable wheel geometry to measure directly -
        // front/rear hub Z is placed symmetrically about the bike's
        // centre, (length - wheelbase) / 2 in from each end.
        // Fallbacks only. Everything below is now MEASURED from the actual
        // cleaned mesh at build time (see MeasureBike) rather than assumed -
        // the previous assumed-constants version put the wheels in the wrong
        // place relative to the visible model, which is part of why the bike
        // looked wrong in the user's Play Mode test.
        private const float FallbackWheelbaseM = 1.575f;
        private const float FallbackWheelRadiusM = 0.30f;

        // Was 280 (real ~220kg wet weight + ~60kg of "headroom for a future
        // rider"). User's explicit instruction while chasing the wheelie
        // lift issue: "do it without the character's weight in mind so when
        // the character gets on the bike his weight will become zero" - the
        // bike alone, with no rider aboard yet (MINI-066 hasn't built one),
        // should weigh what a real riderless TMAX actually weighs, not a
        // pre-inflated guess at a future rider's mass baked permanently
        // into the chassis. That extra ~60kg was real, unaccounted weight
        // working directly against every wheelie attempt (a heavier bike
        // needs more torque to lift, per the real wheelie physics - see
        // wheelieRearTorqueBoost's own comment in TmaxBikeControllerCustom). When
        // MINI-066 adds a rider, their mass should be ADDED at mount time
        // (rb.mass += riderMassKg or similar), not folded back into this
        // constant.
        // MINI-118, user: "make the bike heavier a lot heavier but still
        // keep the same wheeling and leaning... anytime i hit a slight
        // bump... the bike bumps too high or too much." Was 220f (real
        // TMAX wet weight). Increased ~2.18x: the spring rate/damper below
        // are BOTH derived from this constant, so a heavier bike gets a
        // proportionally stiffer/more damped suspension automatically -
        // same sag depth, same settle behaviour - while a fixed-size bump
        // impulse now produces a proportionally smaller velocity change
        // (Δv = impulse / mass), which is the actual physics of "harder to
        // launch." The wheelie mechanic is fully kinematic (MoveRotation,
        // not torque) and the lean/stability corrections use
        // ForceMode.Acceleration (mass-independent by Unity's own
        // definition) - neither can be affected by this change, so
        // "keep the same wheeling and leaning" holds without touching them.
        private const float BikeMassKg = 480f;
        // MINI-077, then MINI-080 (user: "still... lower this a lot"): a real
        // IMPACT complaint, distinct from an earlier fix that only tuned
        // settling (small-amplitude behaviour) at rest. Pushed further both
        // rounds - less travel stores less spring energy on a hard hit to
        // begin with (0.25 -> 0.18 -> 0.14), and more damping past critical
        // bleeds off whatever energy IS stored rather than returning it as a
        // launch (1.3 -> 2.2 -> 3.2). See also ApplyTrikeStabilizers' own
        // debounce fix in TmaxBikeControllerCustom - a SECOND spring was stacking
        // on top of this one during exactly this scenario (front wheel
        // briefly airborne off a bump), which is at least as much of the
        // "way too high" complaint as this suspension tuning is.
        // MINI-119 follow-up, user: "it still glitches bad on the same
        // rough terrain and the sidewalks... i think i need motocross
        // bike like scripts." The 0.17/3.2 pair above was earlier tuned
        // AWAY from absorption specifically to stop a launch/bounce
        // problem (see the history above this line) - but a heavily
        // overdamped (3.2x critical), short-travel suspension barely
        // compresses at all on a sharp hit, so most of a sidewalk/ledge
        // impact transmits straight into the chassis instead of being
        // absorbed - a real, physically-grounded cause of "glitchy",
        // distinct from every rotation/collision fix already in place.
        // Safe to soften again now: ApplyLaunchCap and extraAirGravity
        // (both added since that history) are independent, dedicated
        // systems for "don't bounce too high" - the suspension no longer
        // has to fight that battle alone, so it can afford to actually
        // absorb a hit again. More travel (room to compress before
        // bottoming out) and closer to critical damping (still no
        // oscillation, but far less rigid) - a real step toward the
        // longer-travel, more-compliant feel of an off-road suspension,
        // without changing the visual model's wheel-arch geometry enough
        // to look wrong on this scooter body.
        private const float SuspensionTravelM = 0.20f;
        private const float SuspensionTargetFraction = 0.5f;
        private const float SuspensionDampingRatio = 1.6f;

        // Purely cosmetic - see CreateWheelDisc's comment. 1.0 = exact measured
        // radius, no padding.
        private const float WheelDiscVisualPadding = 1.08f;

        // Half-thickness of the black stand-in wheel disc, in metres (Unity's
        // primitive cylinder is 2 units tall, so the rendered disc ends up
        // twice this wide). Narrowed 0.09 -> 0.02 (0.18m -> 0.04m wide) per
        // the user: "make the black wheel coliders plenty more narrow for now
        // and later versions we will fix it" - these discs are only a stand-in
        // for the real wheel geometry the scan mesh doesn't separate out (see
        // the class doc's wheel-visuals note), so a thin disc reads far less
        // wrong against the real tyre than a fat one. Purely visual: it does
        // not touch the physics WheelCollider's radius or width at all.
        private const float WheelDiscHalfThickness = 0.02f;

        // Uniform scale applied to the whole bike (user: "the bike is too
        // small it has to be a little bigger in scale"). Applied to the ROOT,
        // so the visual mesh, both WheelColliders, the body collider and every
        // measured marker all scale together and stay consistent - scaling
        // only the mesh would leave the physics wheels sitting inside a
        // larger-looking bike.
        //
        // Note the rider must NOT inherit this: he is parented to the Seat,
        // so a scaled bike would stretch him too. VehicleRider.Mount
        // compensates by restoring his original world scale after parenting.
        public const float BikeScale = 1.15f;

        // MINI-066 rider placement, both derived from a real Blender
        // measurement of the mocap clip rather than eyeballed - see the Seat
        // marker's own comment for the full reasoning.
        //   height: the seated clip already carries its hips 0.846m above its
        //   own root, so the anchor sits near ground level and lets the clip
        //   supply the seat height.
        //   pitch:  the stock riding pose is a -59.8deg racing crouch; leaning
        //   the rider back softens it, per "make the rider bend less in
        //   conjunction with the bike".
        private const float RiderSeatHeight = 0.02f;
        // Raised 18 -> 30 after the user's second Play Mode look ("the
        // bending is a problem it bends too much"). The clip's own -59.8deg
        // crouch minus 30 lands around -30deg, an upright cruiser posture
        // rather than a racing tuck - which suits a maxi-scooter anyway. The
        // hands are held on the bars by IK independently of this, so leaning
        // the body back no longer pulls them off (see VehicleRider).
        private const float RiderSeatPitchBack = -30f;

        // Metres the handlebar IK targets sit below the measured bar height,
        // so the rider's arms hang a little more relaxed rather than reaching
        // up. Deliberately small - this is a posture nudge, not a repositioning
        // of the bars themselves (the bars are where the mesh says they are).
        private const float HandlebarDropForArms = 0.06f;

        // Sideways (+X = right) nudge applied to the REAR BLACK WHEEL DISC
        // (the visible stand-in), on top of its measured position. Visual
        // only - the rear WheelCollider stays centred at X=0 so the physics
        // remains symmetric.
        private const float RearWheelDiscOffsetX = 0.05f;


        /// <summary>
        /// How high a WheelCollider's transform must sit above the ground for
        /// the wheel to rest exactly on it. A WheelCollider's transform marks
        /// the TOP of suspension travel, and the wheel centre hangs below it
        /// by (suspensionDistance * targetPosition) at rest - so placing the
        /// collider at merely `radius` (which is what the earlier version did)
        /// spawns the bike with that much suspension already crushed, which
        /// is precisely what catapulted it into the air on Play.
        /// </summary>
        private static float WheelColliderRestHeight(float radius)
            => radius + SuspensionTravelM * SuspensionTargetFraction;

        [MenuItem("Up Iz Up Mini/MINI-064/Build TMAX Prefab")]
        public static void BuildPrefab()
        {
            var meshAsset = AssetDatabase.LoadAssetAtPath<GameObject>(CleanedMeshPath);
            if (meshAsset == null)
            {
                Debug.LogError($"MINI-064 BUILD FAIL: cleaned mesh not found at {CleanedMeshPath}. Run the Blender cleanup pass first (see PROJECT-HANDOFF.md).");
                return;
            }

            var root = new GameObject("TMAX_560");
            var rb = root.AddComponent<Rigidbody>();
            rb.mass = BikeMassKg; // real TMAX wet weight ~219-221kg + headroom for a future rider, per the brief.
            rb.linearDamping = 0.05f;
            rb.angularDamping = 2.5f;
            // MINI-119 follow-up, user: "get the best motocross code
            // from online." Declined per policy (no using another
            // game's/asset's code), but researched what actually
            // differs under the hood for a stable off-road-feeling
            // WheelCollider rig - Unity's own docs/community call out
            // the DEFAULT solver iteration count (6) as a documented,
            // common cause of exactly this class of jitter on complex
            // suspension setups, and recommend a targeted per-Rigidbody
            // override rather than a global project-wide change. Never
            // set before this.
            rb.solverIterations = 16;
            rb.solverVelocityIterations = 12;
            // Smoothness fix (user: "the bike riding in the game now looks
            // like its glitching... not smooth"). Physics runs at a fixed
            // 50Hz while rendering runs at whatever the display does, so
            // WITHOUT interpolation the bike's visible transform only moves
            // on physics steps and visibly stutters between them. A chase
            // camera following it amplifies that badly, and the rider -
            // parented to the bike - inherits the same judder. Interpolate
            // smooths the transform between physics steps.
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            // A 220kg body moving at 60km/h covers ~0.33m per physics step,
            // which discrete collision can tunnel straight through. Continuous
            // detection against static geometry avoids dropping through the
            // road or clipping walls at speed.
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            // MINI-119 follow-up, user: "when you ride fast and you hit
            // the ledge the bike front or back would go up fast then come
            // back down quickly." Researched Unity's own documented
            // rollover-prevention practice (Rigidbody.maxAngularVelocity
            // defaults to 7 rad/s - ~401deg/s - which is high enough to
            // let a single hard hit spin the bike through most of a full
            // rotation in a fraction of a second). Capped well below that;
            // still far faster than any legitimate steering/wheelie
            // rotation rate ever asks for, per the drop test.
            rb.maxAngularVelocity = 5f;

            // --- VisualLeanRoot (the only thing that visually leans). ---
            var visualLean = new GameObject("VisualLeanRoot");
            visualLean.transform.SetParent(root.transform, false);

            var bodyInstance = (GameObject)PrefabUtility.InstantiatePrefab(meshAsset, visualLean.transform);
            bodyInstance.name = "Body";
            bodyInstance.transform.localPosition = Vector3.zero;
            bodyInstance.transform.localRotation = Quaternion.identity;

            // Measure the real model before placing anything onto it.
            var m = MeasureBike(bodyInstance);

            // glTF is right-handed/-Z-forward and Unity is left-handed/+Z-
            // forward, so importers flip Z - which means the model's nose can
            // legitimately land on either Z depending on the exporter. Rather
            // than assume, MeasureBike detects which half is taller (a maxi-
            // scooter's screen/bars end) and we spin the Body 180 degrees if
            // the nose came in backwards, then re-measure.
            // Bug fix: the build-log summary used to report `!m.FrontIsPlusZ` using
            // the FINAL (post-flip) measurement, which reads true again immediately
            // after a real flip - so it always printed "frontWasFlipped=False" even
            // when a flip genuinely happened. Captured explicitly here instead.
            bool wasFlipped = !m.FrontIsPlusZ;

            if (wasFlipped)
            {
                bodyInstance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                m = MeasureBike(bodyInstance);
            }

            float wheelRadius = m.WheelRadius;
            float frontZ = m.FrontAxleZ;
            float rearZ = m.RearAxleZ;

            // --- Physics (WheelColliders - never parented under VisualLeanRoot). ---
            var physics = new GameObject("Physics");
            physics.transform.SetParent(root.transform, false);

            float wcRestY = WheelColliderRestHeight(wheelRadius);
            var frontWc = CreateWheelCollider(physics.transform, "FrontWheelCollider",
                new Vector3(0f, wcRestY, frontZ), wheelRadius);
            var rearWc = CreateWheelCollider(physics.transform, "RearWheelCollider",
                new Vector3(0f, wcRestY, rearZ), wheelRadius);

            // Round 13 - "this is doing the same thing, the hydraulic lift
            // is lifting it but the bike keeps moving right when
            // wheelieing... make it two invisible wheels at the back and
            // one invisible wheel at the front so it will appear as a bike
            // but it will be a trike only when you press E to wheelie."
            // Real physical anti-roll support - two passive "outrigger"
            // anchor points either side of the rear axle, same height/Z as
            // the real rear wheel. No visible wheel disc (they are
            // genuinely invisible, not just cosmetically hidden) and no
            // wheel disc at all.
            //
            // First attempt used real extra WheelCollider components,
            // parked well clear of the ground and moved down only while
            // wheelieing. A real drop test proved that was WRONG regardless
            // of how far they were parked (tried both 3m and 0.5m clear -
            // identical failure either way): merely having two additional
            // ENABLED WheelColliders attached to the Rigidbody - even ones
            // never touching anything - measurably corrupted the vehicle's
            // baseline drop/settle physics (Phase 1 stopped settling
            // cleanly, resting ~0.045m lower than before with a permanent
            // small residual lean). Since the failure was identical however
            // far away they were parked, it isn't a suspension/contact
            // effect - it's WheelCollider's own contribution to the
            // Rigidbody's physics (most likely its automatic inertia
            // tensor calculation) simply by being an enabled component,
            // independent of ground contact.
            //
            // Fixed by not using real Colliders for this at all: these are
            // plain, physics-inert Transform markers (no Collider
            // component whatsoever) - TmaxBikeControllerCustom.ApplyTrikeStabilizers
            // raycasts down from each one only while a wheelie is happening
            // and applies a real spring force (Rigidbody.AddForceAtPosition)
            // if the raycast finds ground close enough, the same "real
            // force at a real contact point" approach the round-11 wheelie
            // research validated - so the anti-roll effect is still genuine
            // physics, it just never touches the Rigidbody's own collider/
            // mass setup at all.
            // Deliberately WIDE - "make the coliders wide enough so it will
            // wheelie striaght". The bike itself is only ~0.82m across, so
            // 0.9m per side gives a ~1.8m track, genuinely trike-like
            // rather than a token offset: the wider the outrigger, the more
            // leverage it has against any roll, for the same spring force.
            float stabilizerOffsetX = Mathf.Max(0.9f, m.Width * 1.1f);
            var rearStabLeft = new GameObject("RearStabilizerLeftAnchor");
            rearStabLeft.transform.SetParent(physics.transform, false);
            rearStabLeft.transform.localPosition = new Vector3(-stabilizerOffsetX, wcRestY, rearZ);
            var rearStabRight = new GameObject("RearStabilizerRightAnchor");
            rearStabRight.transform.SetParent(physics.transform, false);
            rearStabRight.transform.localPosition = new Vector3(stabilizerOffsetX, wcRestY, rearZ);

            // --- COM marker. ---
            var com = new GameObject("COM");
            com.transform.SetParent(root.transform, false);
            // "Slightly below the natural calculated center" per the user's
            // guidance - placed relative to the measured body height rather
            // than a fixed guess, and biased low for low-speed stability.
            com.transform.localPosition = new Vector3(0f, Mathf.Max(0.25f, m.Height * 0.22f), 0f);

            var frontSteering = new GameObject("FrontSteering");
            frontSteering.transform.SetParent(visualLean.transform, false);
            frontSteering.transform.localPosition = new Vector3(0f, wheelRadius, frontZ);

            // Front disc built INVISIBLE too, same as the rear - the user
            // spotted it in a Play Mode screenshot ("i can see the front
            // wheel so make it invisible as well"). Both are physics/measure
            // stand-ins for wheels the scan mesh doesn't separate out, so
            // neither should ever be rendered over the real model.
            var frontWheelVisual = CreateWheelDisc("FrontWheel", frontSteering.transform, wheelRadius, visible: false);
            // Bug fix (user report: rear disc doesn't align with the model
            // from behind). The physics WheelColliders above stay centred at
            // X=0 for clean, symmetric physics, but the visual disc's own X
            // is set here to the REAL measured wheel position (m.FrontAxleX/
            // RearAxleX) - TmaxWheelVisuals no longer overwrites X/Z at
            // runtime (see its own updated comment), so this authored offset
            // is what the player actually sees.
            frontWheelVisual.localPosition = new Vector3(m.FrontAxleX, 0f, 0f);
            var rearWheelGo = new GameObject("RearWheel");
            rearWheelGo.transform.SetParent(visualLean.transform, false);
            rearWheelGo.transform.localPosition = new Vector3(m.RearAxleX + RearWheelDiscOffsetX, wheelRadius, rearZ);
            // Rear disc built INVISIBLE on the user's instruction ("make this
            // same black backwheel colider that you made invisible"). It still
            // exists as a transform so the spin sync and the drop test's
            // clearance measurement keep working unchanged.
            var rearWheelVisual = CreateWheelDisc(null, rearWheelGo.transform, wheelRadius, reuseParent: true, visible: false);

            // --- Body collision: a simple primitive sized from the measured
            // model, not the 15k-tri scan mesh, per the brief's own "simple
            // collision representation". Kept clear of the ground so it can
            // never fight the WheelColliders for ground contact. ---
            var bodyCollider = root.AddComponent<BoxCollider>();
            float colliderBottom = wheelRadius * 0.9f;
            float colliderTop = Mathf.Max(colliderBottom + 0.2f, m.Height * 0.75f);
            bodyCollider.center = new Vector3(0f, (colliderBottom + colliderTop) * 0.5f, 0f);
            bodyCollider.size = new Vector3(
                Mathf.Max(0.3f, m.Width * 0.6f),
                colliderTop - colliderBottom,
                Mathf.Max(0.8f, m.Length * 0.8f));

            // --- Rider/IK anchor points (MINI-066+ will use these; no
            // rider/IK is wired up in this pass). Proportional to the
            // measured model rather than fixed guesses, so they stay put if
            // the mesh is ever re-cleaned at a different scale.
            // Rider seat anchor. Y is deliberately near GROUND level, not at
            // the visible seat height, and that is not a mistake:
            // Mini064 measured the mocap seated clip in Blender and its own
            // root already carries the hips 0.846m up (feet hang to 0.422m).
            // The rider's transform is placed at this anchor, so putting the
            // anchor at the visible seat height (the old m.Height*0.50 =
            // ~0.74m) stacked those two together and left the rider floating
            // ~1.6m up - exactly what the user's screenshots showed. At
            // ground level the clip's own 0.846m hip height lands the rider
            // on a seat ~0.85m up, which is about right for a maxi-scooter.
            //
            // The X rotation leans the whole rider BACK, because the stock
            // riding pose measures -59.8deg from vertical (a hard racing
            // crouch) and the user reported "the bending is too hard make the
            // rider bend less in conjunction with the bike". Rotating the
            // anchor is preferable to swapping in the more upright
            // MOTOIdle01 clip, since that one is the stopped/mount pose and
            // is needed for its own state.
            var seatMarker = CreateMarker(root.transform, "Seat",
                new Vector3(0f, RiderSeatHeight, rearZ * 0.25f));
            seatMarker.localRotation = Quaternion.Euler(RiderSeatPitchBack, 0f, 0f);
            // Grip height lowered by HandlebarDropForArms - the user asked for
            // the arms to hang slightly lower during normal riding ("just
            // make his arms drop slightly slightly"). Moving the IK TARGET is
            // the right lever: the hands stay pinned exactly as firmly, they
            // are simply pinned a little lower, so the elbows relax instead of
            // the grip loosening.
            float barY = m.Height * 0.68f - HandlebarDropForArms;
            CreateMarker(root.transform, "HandlebarLeft", new Vector3(-0.28f, barY, frontZ * 0.55f));
            CreateMarker(root.transform, "HandlebarRight", new Vector3(0.28f, barY, frontZ * 0.55f));
            CreateMarker(root.transform, "LeftFootTarget", new Vector3(-0.18f, wheelRadius * 0.9f, 0.05f));
            CreateMarker(root.transform, "RightFootTarget", new Vector3(0.18f, wheelRadius * 0.9f, 0.05f));
            CreateMarker(root.transform, "CameraTarget", new Vector3(0f, m.Height * 0.75f, rearZ - 0.4f));

            // MINI-066 pillion (second rider) anchors. A TMAX is a maxi-
            // scooter with a genuinely usable pillion seat, so "the other guy
            // hopping on behind him" is a real seat further back and slightly
            // higher, not the driver position reused. The passenger holds the
            // grab rails either side rather than handlebars they can't reach -
            // which is also why VehicleSeat keeps hand targets per-seat
            // instead of assuming everyone grips the bars.
            // Same ground-level convention and back-lean as the driver seat -
            // the pillion uses the same clip family, so it has the same
            // built-in hip height and would float identically if anchored at
            // the visible seat height.
            var pillionMarker = CreateMarker(root.transform, "PillionSeat",
                new Vector3(0f, RiderSeatHeight, rearZ * 0.72f));
            // MINI-128: preserve the approved height/position but match the
            // proven SuperMoto pillion's neutral orientation. The old -30deg
            // anchor compounded with the passenger pose and rotated him.
            pillionMarker.localRotation = Quaternion.identity;
            // MINI-077: "his hand should be on the body of the main
            // character" - moved from the grab rails out at the pillion's
            // OWN position (rearZ*0.95) to the DRIVER's waist instead. The
            // driver's own hip sits at Seat's rearZ*0.25 (see above); a real
            // pillion passenger reaches forward and wraps their hands around
            // the rider's waist from behind, not straight down at their own
            // hip, so the target sits near the driver's seat depth rather
            // than the pillion's. Height is above hip/below the handlebar
            // grip height (barY) - roughly torso/waist level on a seated rider.
            CreateMarker(root.transform, "PillionGrabLeft", new Vector3(-0.14f, m.Height * 0.42f, rearZ * 0.32f));
            CreateMarker(root.transform, "PillionGrabRight", new Vector3(0.14f, m.Height * 0.42f, rearZ * 0.32f));
            CreateMarker(root.transform, "PillionFootLeft", new Vector3(-0.24f, wheelRadius * 1.1f, rearZ * 0.55f));
            CreateMarker(root.transform, "PillionFootRight", new Vector3(0.24f, wheelRadius * 1.1f, rearZ * 0.55f));

            string summary = $"MINI-064 BUILD OK: TMAX_560 prefab saved to {PrefabPath}. " +
                $"MEASURED from mesh: size={m.Width:F3}w x {m.Length:F3}l x {m.Height:F3}h, " +
                $"frontAxleZ={frontZ:F3}, rearAxleZ={rearZ:F3}, wheelbase={(frontZ - rearZ):F3}m, wheelRadius={wheelRadius:F3}m, frontAxleX={m.FrontAxleX:F3}, rearAxleX={m.RearAxleX:F3}, " +
                $"frontWasFlipped={wasFlipped}. FrontWheel visual anchor={frontWheelVisual.name}, RearWheel visual anchor={rearWheelVisual.name}.";

            System.IO.Directory.CreateDirectory("Assets/UpIzUpMini/Vehicles");
            // Scale last, once everything has been placed from the real
            // measurements - so all the measuring above stays in true metres
            // and only the finished rig is resized.
            root.transform.localScale = Vector3.one * BikeScale;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
            Object.DestroyImmediate(root);

            if (!success)
            {
                Debug.LogError("MINI-064 BUILD FAIL: SaveAsPrefabAsset reported failure.");
                return;
            }

            Debug.Log(summary);
        }

        /// <summary>What MeasureBike() reads off the real cleaned mesh, in
        /// the prefab root's local space (metres).</summary>
        private struct BikeMeasurements
        {
            public float Width, Length, Height;
            public float FrontAxleZ, RearAxleZ, WheelRadius;
            public float FrontAxleX, RearAxleX;
            public bool FrontIsPlusZ;
        }

        /// <summary>
        /// Measures the actual cleaned mesh instead of trusting assumed
        /// constants. Added after the user's Play Mode tests showed the bike
        /// visibly wrong: the earlier version hardcoded a spec wheelbase and
        /// wheel radius and simply hoped they matched wherever the scan's
        /// wheels really were. Everything here is derived from vertex data,
        /// so the physics rig lines up with whatever the mesh actually is.
        /// </summary>
        private static BikeMeasurements MeasureBike(GameObject body)
        {
            var result = new BikeMeasurements
            {
                FrontAxleZ = FallbackWheelbaseM / 2f,
                RearAxleZ = -FallbackWheelbaseM / 2f,
                WheelRadius = FallbackWheelRadiusM,
                FrontAxleX = 0f,
                RearAxleX = 0f,
                FrontIsPlusZ = true,
            };

            var mf = body.GetComponentInChildren<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                Debug.LogWarning("MeasureBike: no mesh found - falling back to spec constants.");
                return result;
            }

            // Vertices into the prefab root's space (Body may be nested/rotated).
            var verts = mf.sharedMesh.vertices;
            var toRoot = body.transform.parent.parent != null
                ? body.transform.parent.parent.worldToLocalMatrix * mf.transform.localToWorldMatrix
                : mf.transform.localToWorldMatrix;

            var pts = new Vector3[verts.Length];
            for (int i = 0; i < verts.Length; i++) pts[i] = toRoot.MultiplyPoint3x4(verts[i]);

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (var p in pts)
            {
                if (p.x < minX) minX = p.x; if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y; if (p.y > maxY) maxY = p.y;
                if (p.z < minZ) minZ = p.z; if (p.z > maxZ) maxZ = p.z;
            }
            result.Width = maxX - minX;
            result.Height = maxY - minY;
            result.Length = maxZ - minZ;

            // Front detection: a maxi-scooter's screen/bars end is markedly
            // taller than its tail.
            float midZ = (minZ + maxZ) * 0.5f;
            float plusPeak = float.MinValue, minusPeak = float.MinValue;
            foreach (var p in pts)
            {
                if (p.z > midZ) { if (p.y > plusPeak) plusPeak = p.y; }
                else { if (p.y > minusPeak) minusPeak = p.y; }
            }
            result.FrontIsPlusZ = plusPeak >= minusPeak;

            // Wheel contact patches: only tyres sit near the centre plane at
            // low height, so exclude side stand / footboards / fairing with an
            // |x| filter, then work each half independently (the two tyres do
            // not bottom out at exactly the same height in this scan).
            float xFilter = Mathf.Max(0.12f, result.Width * 0.15f);
            bool MeasureHalf(bool plusHalf, out float contactZ, out float lowestY)
            {
                contactZ = 0f; lowestY = 0f;
                float lo = float.MaxValue;
                foreach (var p in pts)
                {
                    if (Mathf.Abs(p.x) > xFilter) continue;
                    if ((p.z > midZ) != plusHalf) continue;
                    if (p.y < lo) lo = p.y;
                }
                if (lo == float.MaxValue) return false;

                float sum = 0f; int count = 0;
                foreach (var p in pts)
                {
                    if (Mathf.Abs(p.x) > xFilter) continue;
                    if ((p.z > midZ) != plusHalf) continue;
                    if (p.y > lo + 0.05f) continue;
                    sum += p.z; count++;
                }
                if (count == 0) return false;
                contactZ = sum / count;
                lowestY = lo;
                return true;
            }

            // Front axle from the contact patch - reliable, because the front
            // tyre genuinely touches the ground plane in this scan.
            bool haveFront = MeasureHalf(true, out float fz, out float frontLowY);
            if (haveFront) result.FrontAxleZ = fz;

            // Wheel radius from the front tyre, fitted from a narrow band of probe
            // heights and averaged rather than one single sample. Debugged against
            // this exact mesh (see PROJECT-HANDOFF.md): below ~0.08m the probe
            // catches the front disc brake/hub/spoke clutter visible through the
            // scan and the fit collapses to nonsense (r dropped as low as 0.05m);
            // above ~0.13m it drifts onto the fender. 0.09-0.12m is the clean
            // band that actually traces the tyre's own outer curve. At height h
            // above the contact patch a circle of radius R has half-chord
            // w = sqrt(R^2-(R-h)^2), so R = (w^2 + h^2) / 2h.
            float lowY = frontLowY;
            var radii = new System.Collections.Generic.List<float>();
            foreach (float probeH in new[] { 0.09f, 0.10f, 0.11f, 0.12f })
            {
                float wMin = float.MaxValue, wMax = float.MinValue;
                foreach (var p in pts)
                {
                    if (Mathf.Abs(p.x) > xFilter) continue;
                    if (p.z <= midZ) continue;
                    if (Mathf.Abs(p.y - (lowY + probeH)) > 0.012f) continue;
                    if (p.z < wMin) wMin = p.z;
                    if (p.z > wMax) wMax = p.z;
                }
                if (wMax <= wMin) continue;
                float halfChord = (wMax - wMin) * 0.5f;
                float rr = (halfChord * halfChord + probeH * probeH) / (2f * probeH);
                radii.Add(rr);
            }
            if (radii.Count >= 2)
            {
                float r = 0f;
                foreach (var v in radii) r += v;
                r /= radii.Count;
                if (r > 0.2f && r < 0.45f) result.WheelRadius = r;
                else Debug.LogWarning($"MeasureBike: fitted wheel radius {r:F3}m (from {radii.Count} probes) is implausible - using the {FallbackWheelRadiusM}m fallback.");
            }
            else
            {
                Debug.LogWarning($"MeasureBike: only {radii.Count} usable radius probes - using the {FallbackWheelRadiusM}m fallback.");
            }
            // Rear axle, from its own contact patch. This is reliable now that
            // the Blender pass levels the scan's ~6.7 degree nose-down pitch -
            // previously only the front tyre reached the ground and the rear
            // hung ~0.18m in the air, which is why an awkward "measure the
            // tyre's rear edge and step forward one radius" workaround was
            // needed here at all. With both tyres actually on the ground the
            // direct measurement is both simpler and far more accurate, so the
            // edge method is gone.
            if (MeasureHalf(false, out float rz, out _))
            {
                float wb = result.FrontAxleZ - rz;
                // A real TMAX 560's wheelbase is 1.575m; accept a measurement
                // in a sane band around that, else anchor to the spec figure
                // off the (trustworthy) measured front axle.
                if (wb > 1.35f && wb < 1.80f)
                {
                    result.RearAxleZ = rz;
                }
                else
                {
                    result.RearAxleZ = result.FrontAxleZ - FallbackWheelbaseM;
                    Debug.LogWarning($"MeasureBike: measured wheelbase {wb:F3}m is out of range - anchoring the rear axle to the measured front axle using the {FallbackWheelbaseM}m spec wheelbase instead.");
                }
            }
            else
            {
                result.RearAxleZ = result.FrontAxleZ - FallbackWheelbaseM;
            }

            // Wheel X-centre (left-right), independently for each wheel -
            // added after the user reported the rear wheel visibly off-
            // centre from behind. The whole mesh is symmetric about X=0 by
            // construction (MeasureBike's own bounding-box centring), but
            // that does NOT mean each wheel individually sits on X=0 - this
            // scan's rear wheel genuinely measures ~0.11m off centreline
            // (confirmed independently in Blender), which is why a hardcoded
            // X=0 for both WheelColliders put the rear disc visibly beside
            // the real wheel rather than on it. Sampled from the tyre's rim
            // band (a fixed height range above the ground, avoiding both the
            // hub/spoke clutter right at the axle and any fender/bodywork
            // clutter further up) rather than the whole wheel silhouette.
            float WheelXCenter(bool plusHalf)
            {
                float rimLo = minY + 0.06f;
                float rimHi = minY + 0.22f;
                float xMin = float.MaxValue, xMax = float.MinValue;
                foreach (var p in pts)
                {
                    if ((p.z > midZ) != plusHalf) continue;
                    if (p.y < rimLo || p.y > rimHi) continue;
                    if (p.x < xMin) xMin = p.x;
                    if (p.x > xMax) xMax = p.x;
                }
                return xMin <= xMax ? (xMin + xMax) * 0.5f : 0f;
            }


            result.FrontAxleX = WheelXCenter(true);
            result.RearAxleX = WheelXCenter(false);
            float xBound = Mathf.Max(0.05f, result.Width * 0.5f);
            if (Mathf.Abs(result.FrontAxleX) > xBound)
            {
                Debug.LogWarning($"MeasureBike: front wheel X-centre {result.FrontAxleX:F3}m is implausible (beyond half the bike's own width) - using 0.");
                result.FrontAxleX = 0f;
            }
            if (Mathf.Abs(result.RearAxleX) > xBound)
            {
                Debug.LogWarning($"MeasureBike: rear wheel X-centre {result.RearAxleX:F3}m is implausible (beyond half the bike's own width) - using 0.");
                result.RearAxleX = 0f;
            }

            return result;
        }

        private static WheelCollider CreateWheelCollider(Transform parent, string name, Vector3 localPos, float radius)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var wc = go.AddComponent<WheelCollider>();
            wc.radius = radius;
            wc.mass = 12f;
            // Bug fix (user report: "spinning in the air like crazy" on
            // Play): 0.12m was too little suspension travel combined with
            // the test scene's own placement error (see BuildTestScene) -
            // the wheels never reached the ground at all, so the bike
            // free-fell with no wheel contact and no roll-stability
            // correction (ApplyStability only runs once grounded) while
            // ApplyWheelie's pitch correction kept firing regardless -
            // together, an ungrounded body accumulating unopposed torque.
            // 0.25m gives real forgiveness against small placement/terrain
            // sampling error (relevant again once this is used on
            // GrandBayProof's real terrain, not just this flat test road).
            wc.suspensionDistance = SuspensionTravelM;
            // Bug fix (user report: "it bounces like 10 feet high twice and
            // then it falls on the side"). The spring was 2.5x too stiff for
            // the load it carries. Deriving it from the actual mass instead
            // of guessing: for the suspension to rest exactly at
            // targetPosition under the bike's own weight,
            //     load * g = spring * targetPosition * suspensionDistance
            // so spring = load*g / (targetPosition * suspensionDistance).
            // At 280kg over two corners that is ~11000 N/m, not 28000 - the
            // old value pushed up far harder than gravity pulled down, so
            // the bike launched itself. Damper is set past critical
            // (2*sqrt(k*m)) so the remaining travel settles rather than
            // oscillates.
            float loadPerWheelN = (BikeMassKg / 2f) * 9.81f;
            float springRate = loadPerWheelN / (SuspensionTargetFraction * SuspensionTravelM);
            float criticalDamper = 2f * Mathf.Sqrt(springRate * (BikeMassKg / 2f));

            var spring = wc.suspensionSpring;
            spring.spring = springRate;
            spring.damper = criticalDamper * SuspensionDampingRatio;
            spring.targetPosition = SuspensionTargetFraction;
            wc.suspensionSpring = spring;
            // MINI-077: the suspension SPRING damping above controls how the
            // wheel's vertical travel settles; this is the wheel's own
            // rotational damping, left at Unity's low default (0.25) until
            // now - raised so a hard hit's leftover spin energy bleeds off
            // too, rather than fighting the suspension on the next bounce.
            wc.wheelDampingRate = 1.0f;
            // MINI-119 follow-up, user: "it still glitches bad on the
            // same rough terrain and the sidewalks." Researched a
            // specific, well-documented WheelCollider instability class:
            // forceAppPointDistance defaults to 0, and Unity's own
            // scripting reference/community explicitly call that out as
            // a real cause of jitter/instability on rough terrain - the
            // suspension force applies at the collider's own local
            // origin instead of below the Rigidbody's real centre of
            // mass, which is especially bad for a narrow, tall vehicle
            // like a motorcycle. Never set before this. 0.3m below the
            // wheel's rest position is the commonly-documented starting
            // point for a normal vehicle's mass distribution.
            wc.forceAppPointDistance = 0.3f;
            // MINI-119 follow-up: only stiffness was ever set here - the
            // actual curve SHAPE (extremumSlip/Value, asymptoteSlip/Value)
            // was left at Unity's generic car defaults, tuned for a
            // 4-wheeled car's much wider, lower-slip-sensitivity contact
            // patch, not a motorcycle's narrow one hitting rough terrain.
            // Widened both curves so grip degrades more gradually past
            // the peak instead of dropping off sharply - a bump momentarily
            // spiking slip now loses grip progressively rather than
            // snapping loose all at once.
            var fwdFriction = wc.forwardFriction;
            fwdFriction.extremumSlip = 0.35f;
            fwdFriction.extremumValue = 1f;
            fwdFriction.asymptoteSlip = 1.0f;
            fwdFriction.asymptoteValue = 0.6f;
            fwdFriction.stiffness = 1.6f;
            wc.forwardFriction = fwdFriction;
            var sideFriction = wc.sidewaysFriction;
            sideFriction.extremumSlip = 0.3f;
            sideFriction.extremumValue = 1f;
            sideFriction.asymptoteSlip = 0.65f;
            sideFriction.asymptoteValue = 0.65f;
            sideFriction.stiffness = 2.2f;
            wc.sidewaysFriction = sideFriction;
            return wc;
        }

        /// <summary>
        /// A thin dark cylinder standing in for a visible, spinning wheel -
        /// see the class doc's "Wheel visuals" note for why this exists
        /// instead of a real separated wheel mesh.
        /// </summary>
        private static Transform CreateWheelDisc(string name, Transform parent, float radius, bool reuseParent = false, bool visible = true)
        {
            Transform holder;
            if (reuseParent)
            {
                holder = parent;
            }
            else
            {
                var holderGo = new GameObject(name);
                holderGo.transform.SetParent(parent, false);
                holder = holderGo.transform;
            }

            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "WheelDiscVisual";
            Object.DestroyImmediate(disc.GetComponent<Collider>());
            disc.transform.SetParent(holder, false);
            // Unity's primitive cylinder is Y-up, height 2, radius 0.5 at
            // scale 1 - rotate so its axis runs left-right (the wheel's
            // spin axis) and scale to the real wheel radius/thickness. A small
            // visual-only pad (this does NOT touch the physics WheelCollider's
            // own radius) covers the last centimetre of any measurement drift
            // so the overlay reads as sitting exactly on the real tyre rather
            // than looking a hair out of sync with it (user report).
            disc.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            float visualRadius = radius * WheelDiscVisualPadding;
            disc.transform.localScale = new Vector3(visualRadius * 2f, WheelDiscHalfThickness, visualRadius * 2f);

            var discRenderer = disc.GetComponent<Renderer>();
            discRenderer.sharedMaterial = GetWheelDiscMaterial();
            // Hidden discs keep their transform (TmaxWheelVisuals still syncs
            // height/spin onto it harmlessly, and the drop test still measures
            // its ground clearance) - only the renderer is switched off, so
            // nothing that references it has to change.
            discRenderer.enabled = visible;

            return holder;
        }

        /// <summary>
        /// Bug fix (user report: solid magenta blobs on the bike in Play
        /// Mode - Unity's "missing/null shader" colour). Two compounding
        /// mistakes in the original code: (1) `new Material(Shader.Find
        /// ("Standard"))` had no fallback/null-check, unlike every other
        /// material this whole project creates (see Mini011PhaseBSetup.
        /// GetOrCreateMaterial's `Shader.Find("Standard") ?? Shader.Find
        /// ("Diffuse")`); (2) more importantly, that Material was a
        /// throwaway in-memory instance, never saved as its own asset -
        /// `Mini065TmaxPhysicsTest.WireController` reloads this prefab via
        /// `PrefabUtility.LoadPrefabContents` and re-saves it right after
        /// `BuildPrefab` runs, and that round-trip does not reliably carry
        /// a non-asset Material reference through, which is exactly why
        /// the automated shader check added after this bug started
        /// reporting a **null material reference**, not a null shader -
        /// the reference itself was getting dropped. Fixed by making this
        /// a real persisted .mat asset, the same `GetOrCreateMaterial`
        /// pattern already proven everywhere else in this project.
        /// </summary>
        private static Material GetWheelDiscMaterial()
        {
            const string path = "Assets/UpIzUpMini/Vehicles/TmaxWheelDisc.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
            if (shader == null)
            {
                Debug.LogError("Mini064TmaxAssetPrep: no usable shader found (tried Standard and Diffuse) for the wheel disc material - it will render as the missing-shader colour.");
            }
            var mat = new Material(shader) { color = new Color(0.05f, 0.05f, 0.05f) };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static Transform CreateMarker(Transform parent, string name, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go.transform;
        }
    }
}
