using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-065: TMAX 560 arcade bike physics. Adapted from a user-supplied
    /// reference implementation (same architecture, tuning fields, and
    /// method shapes) rather than written from scratch or ported from the
    /// downloaded MIT-licensed `UPIZUP_Motorcycle_Controller` reference,
    /// which was reviewed and NOT adopted: it drives a single front-wheel
    /// WheelCollider with `Input.GetKey` calls baked directly into
    /// `FixedUpdate` (couples input to physics, which this project
    /// explicitly avoids - see <see cref="SetInput"/>), and rotates the
    /// bike's own Rigidbody transform directly to "tilt" it -
    /// physically forcing the lean angle the way the user's own
    /// architecture note warns against ("do not force the Rigidbody to
    /// physically lean 35 degrees just to make the motorcycle look like
    /// it is leaning 35 degrees - that introduces most of the balancing/
    /// wobbling problems"). Its braking/neutral-drag concept was read for
    /// reference only; no code from it is copied here.
    ///
    /// Architecture: the Rigidbody root stays close to upright at all
    /// times (small real roll only, from <see cref="ApplyStability"/>'s
    /// correction torque); the motorcycle "look" of leaning happens
    /// entirely on a separate <see cref="visualLeanRoot"/> transform that
    /// carries no physics of its own. This is deliberately cheaper to
    /// tune than simulating real self-balancing, per the brief.
    ///
    /// Input is fully decoupled from physics via <see cref="SetInput"/> -
    /// nothing in this class reads Input.* directly, so the same
    /// controller can be driven by keyboard, a gamepad, mobile buttons, or
    /// AI without any change here.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class TmaxBikeController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WheelCollider frontWheel;
        [SerializeField] private WheelCollider rearWheel;
        // Round 13 - "make it two invisible wheels at the back and one
        // invisible wheel at the front so it will appear as a bike but it
        // will be a trike only when you press E to wheelie." Real physical
        // anti-roll outriggers, but NOT real WheelCollider components (a
        // first attempt used real WheelColliders and a drop test proved
        // that measurably corrupted the vehicle's baseline physics just by
        // being enabled, regardless of position - see ApplyTrikeStabilizers'
        // own comment for the full story). These are plain, physics-inert
        // Transform anchors instead - ApplyTrikeStabilizers raycasts down
        // from each one and applies a real spring force at the hit point
        // when active, so the anti-roll effect is still genuine physics
        // (Rigidbody.AddForceAtPosition, real lever arm), it just never
        // touches the Rigidbody's own collider/mass setup.
        [SerializeField] private Transform rearStabLeftAnchor;
        [SerializeField] private Transform rearStabRightAnchor;
        [SerializeField] private Transform centerOfMass;
        [SerializeField] private Transform visualLeanRoot;

        [Header("Drive")]
        [SerializeField] private float maxSpeedKmh = 150f;
        // MINI-118: scaled up by the same ~2.18x ratio as BikeMassKg
        // (220->480). Unlike the wheelie/lean systems (kinematic/
        // ForceMode.Acceleration, both mass-independent), motor and brake
        // torque are real WheelCollider forces - acceleration and stopping
        // distance ARE mass-dependent, so these need to scale with the
        // heavier bike or it would feel sluggish/under-braked as an
        // unintended side effect of the mass change, not "the same
        // wheeling and leaning" the user asked to preserve.
        // MINI-119, user: "the bike is still terrible... i cant climb
        // simple hill with the bike, should have enough power to climb
        // hill and ledges... this is a powerful bike." MINI-118 scaled
        // motorTorque by the SAME ratio as the mass increase (700/320 ~=
        // 480/220), which only preserved the bike's PRE-existing power-to-
        // weight ratio - it never actually made hill-climbing stronger, so
        // the complaint that hills still feel weak is real and expected.
        // Raised further here, past parity, plus see hillClimbAssist below
        // for a slope-specific boost on top of this.
        [SerializeField] private float motorTorque = 950f;
        [SerializeField] private float brakeTorque = 1200f;
        [Tooltip("Top speed in REVERSE. Reverse previously shared the forward limit entirely, so backing up accelerated to the same 150km/h the bike does going forward.")]
        [SerializeField] private float maxReverseSpeedKmh = 18f;

        [Header("Steering")]
        [SerializeField] private float maxSteerAngle = 28f;
        [SerializeField] private float highSpeedSteerAngle = 7f;
        [SerializeField] private float steerResponse = 5f;
        [SerializeField] private float yawAssist = 2.0f;
        [Header("Visual Lean")]
        [Tooltip("Increased per the user's explicit request that the lean while riding should be a little more - purely cosmetic (VisualLeanRoot only), never touches the physics root.")]
        // MINI-079: was 42.36, capped to match the rider's own 15deg roll
        // clamp so bike body and riders share one lean envelope - see the
        // explicit write in Mini065TmaxPhysicsTest.WireController for why
        // this default alone would not have been enough on an existing prefab.
        [SerializeField] private float maxVisualLean = 15f;
        [SerializeField] private float leanResponse = 6f;
        [SerializeField] private float minLeanSpeedKmh = 2.1f;
        [Tooltip("Speed at which the bike reaches its FULL visual lean. Was hardcoded to 60km/h, so ordinary riding only ever showed a fraction of the lean and the bike read as staying straight through corners.")]
        [SerializeField] private float fullLeanSpeedKmh = 30f;   // user-tuned

        [Header("Stability")]
        [SerializeField] private float uprightStrength = 11f;
        [SerializeField] private float uprightDamping = 3.5f;
        [SerializeField] private float lowSpeedExtraStability = 7f;

        // The user's "put stands to stabilize the bike when it drops". A
        // real pair of stand colliders would only catch the bike after it
        // had already toppled onto them; this instead removes roll
        // directly, so it never topples in the first place.
        //
        // Bug history, both directions - kept here since both mistakes are
        // easy to repeat by "fixing" one at the expense of the other:
        //  1) With this assist always fully active, holding E to wheelie
        //     made the bike "lean more to the left" (user report) - a real
        //     physics test showed roll climbing in lockstep with wheelie
        //     pitch, reaching ~47 degrees of unwanted roll at a 38-degree
        //     wheelie. Cause: Quaternion.LookRotation(forward, worldUp) is
        //     only well-behaved for small pitch: as the wheelie's own
        //     intentional pitch grows large, LookRotation's "closest
        //     orthogonal up to worldUp" solution stops matching the bike's
        //     actual attitude, and MoveRotation-ing toward that mismatched
        //     target is what shows up as phantom roll.
        //  2) Removing this assist entirely (the first attempted fix) was
        //     WRONG - it broke general driving stability outright, not
        //     just the wheelie case: a real test with no wheelie involved
        //     at all, just plain full-throttle driving, showed lean
        //     exploding past 100 degrees within one second. ApplyStability
        //     alone is not sufficient once the bike is actually being
        //     driven (torque, wheel spin, ground friction) - it only
        //     looked sufficient because the earlier "Phase 1" test that
        //     seemed to prove it was a static, zero-throttle drop, not a
        //     driving scenario.
        // The fix that actually works: keep this assist for normal driving
        // (where it demonstrably works, per Phase 2's 0.47-degree max
        // lean), but fade its strength toward zero as the wheelie's own
        // pitch target grows, so it steps out of the way of ApplyWheelie's
        // own (already correct) pitch control instead of fighting it.
        [Header("Upright Assist (anti-topple 'stand')")]
        [Tooltip("Removes roll so the bike cannot fall on its side during normal driving. 0 disables it and the bike is free to topple.")]
        [SerializeField] private float uprightAssist = 16f;
        [Tooltip("Below this speed the assist is strongest, so the bike stays up at a standstill.")]
        [SerializeField] private float uprightAssistLowSpeedBoost = 2f;

        [Header("Recovery")]
        [SerializeField] private float tippedDotThreshold = 0.35f;

        // MINI-065 (folded forward from the roadmap's own MINI-067, per
        // the user's explicit repeated request while this task was in
        // progress: "make it easy to wheelie", "the wheelie should be
        // made easy and look good", "i want to be able to wheelie easy in
        // this version as well"). Rider pose reaction is NOT included -
        // there is still no rider (that's MINI-066), so this is the
        // mechanical/visual half only.
        //
        // Redesign, round 2 (user report on the first explicit-button
        // version: "the wheelie isnt working now when i press E. it seems
        // like it wants to go up but it gigs leans more to the left...
        // when you hold down e it should go up more and then the taping
        // would help balancing it in the air"). The previous version used
        // a tap-adds/passively-decays "sustain" value, which the user
        // found unresponsive. This is a simpler, more standard hold-to-
        // lift scheme instead: holding the button (SetWheelieHeld(true))
        // drives the target pitch straight toward maxWheelieAngle at
        // wheelieRiseRate degrees/second; releasing it drives the target
        // back to 0 at the same rate. Balance comes for free from this -
        // a player who eases off (a quick release-and-reapply, i.e. "the
        // tapping") lets the front settle a little before lifting again,
        // which is exactly how real wheelie control feels, without a
        // separate sustain/decay system to fight against the player's own
        // input.
        [Header("Wheelie")]
        [SerializeField] private bool enableWheelie = true;
        // MINI-077: raised 1 -> 8 km/h per the user's later explicit request
        // ("the min speed of wheelie should 8km"). Overrides the earlier 1
        // km/h floor set for a different reason (pressing E doing nothing at
        // a near-stop) - 8 still comfortably clears "doing nothing near a
        // dead stop" while reading as a deliberate rolling start.
        [SerializeField] private float wheelieMinSpeedKmh = 8f;
        [SerializeField] private float wheelieMaxSpeedKmh = 70f;
        [Tooltip("Degrees of nose-up pitch the front reaches while E is held. Now DIRECTLY DRIVEN (see ApplyWheelie) so any value works, including past 90 and all the way to a full 360 loop, per the user's \"make it go all the way to 360\". Slider goes to 360; 90 = front pointing straight up.")]
        [SerializeField] private float maxWheelieAngle = 90f;
        [Tooltip("Degrees/second the target pitch rises while held, and falls while released - keeps the lift progressive and the recovery smooth rather than snapping (\"no instant 90-degree rotation\"). Raised 30->45->60 across two rounds of the user's \"make it go up faster\".")]
        // MINI-118: nudged 60->72 to compensate for a real, measured side
        // effect of the heavier bike - real pitch dropped from a baseline
        // 85.9deg to 77.4deg (still above the drop test's 76.6deg minimum,
        // but with far less margin than before), most likely because the
        // heavier bike's real WheelCollider traction curve isn't perfectly
        // linear even with motorTorque scaled to match, shifting how much
        // of the drop test's fixed duration is spent inside the wheelie's
        // speed-eligible window. Re-verified with the same drop test after
        // this change, not assumed fixed.
        [SerializeField] private float wheelieRiseRate = 72f;
        [Tooltip("Round 12 - the user's own \"invisible hydraulic\" ask, simplified from round 11's separate rear-torque-boost/lift-clamp/recover-clamp sliders down to ONE number. Internally still gentle while climbing and much stronger while correcting an overshoot (a first attempt at a plain symmetric version measurably destabilised the bike - see ApplyWheelie's own comment) - that asymmetry is real, tested safety margin, not exposed complexity. Raising this raises both how hard it lifts AND its own recovery ceiling together, so there's no hidden ceiling the slider can't reach past.")]
        [SerializeField] private float wheelieHydraulicStrength = 9f;
        [Tooltip("Steering is scaled down by this much (0=no steering, 1=unchanged) at the peak of a wheelie.")]
        [SerializeField] private float wheelieSteerMultiplier = 0.35f;
        [Tooltip("Extra REAL motor torque added to the rear wheel while the wheelie button is held, on top of normal throttle - per the user's \"add more torque to help this on the rear wheels\".")]
        // MINI-118: scaled with motorTorque/BikeMassKg - this is real drive
        // torque (forward creep during a wheelie), not the pitch itself
        // (which stays kinematic/mass-independent, see ApplyWheelie).
        [SerializeField] private float wheelieRearTorqueBoost = 1960f;

        // User report: "it goes left sometimes... I think it needs something
        // to stabilize it in the air like hold it straight and only if I
        // press left or right it will tilt." Two separate things were
        // needed here, not one:
        //  1) "Hold it straight" with no input - this is ApplyStability's
        //     job (see its own comment for the wheelieRollAssist addition
        //     that gives it enough authority to do that at a fast, steep
        //     wheelie specifically).
        //  2) "Only if I press left or right it will tilt" - a genuinely
        //     missing capability, not a bug: ApplySteering's yaw assist is
        //     gated on `frontWheel.isGrounded`, which is false throughout a
        //     real wheelie (that IS the point of a wheelie), so steerInput
        //     currently does nothing at all while airborne. ApplyWheelieAir-
        //     Control below is that missing capability - a direct,
        //     player-driven torque, active only while actually steering.
        //     Deliberately does NOT also try to "lock" a heading when the
        //     player releases the stick - an earlier version of this method
        //     did that via a world-Y-axis correction, and it made the roll
        //     bug measurably WORSE (a real physics test showed peak roll
        //     jump from ~58 to worse), because at a steep pitch the bike's
        //     own forward axis is close to world-up, so "yaw" and "roll"
        //     stop being separate rotations - torque meant as pure yaw
        //     leaks straight into roll. Leaving "hold straight" entirely to
        //     ApplyStability's roll-specific (not yaw-based) correction
        //     avoids that trap.
        [Header("Wheelie Air Control")]
        [Tooltip("Direct player-driven yaw/tilt torque while airborne during a wheelie - steering has no other way to act on the bike once the front leaves the ground.")]
        [SerializeField] private float wheelieAirSteerTorque = 14f;

        [Tooltip("Extra roll-correction strength added to ApplyUprightAssist's Slerp, scaled by how deep into the wheelie the bike currently is - see that method's own comment for why it lives there and not in ApplyStability's torque.")]
        [SerializeField] private float wheelieRollAssist = 18f;

        // User report: "the bike does not wheelie even if I play in all the
        // sliders... the wheelie target degrees numbers says it goes up at
        // 80 but it doesnt even lift the ground, maybe we should have
        // invisible holders to bring it up and hold it upright that can be
        // adjusted." Two different things are true here at once: (1) the
        // "target" reading the user is looking at may well be the MAX
        // WHEELIE ANGLE *setting* slider (which always reads whatever it's
        // set to, 80, regardless of whether a wheelie is actually
        // happening) rather than the live progress readout - the tuner's
        // pinned status box (see TmaxWheelieTuner) now makes that
        // distinction impossible to miss. (2) Independent of that, the
        // user's own ask for "invisible holders... that can be adjusted"
        // is a genuinely useful, separate diagnostic tool: a direct,
        // unconditional override that forces the bike's pitch toward an
        // exact chosen angle with an exact chosen strength, completely
        // bypassing the normal eligibility window (speed/brake/ground),
        // the asymmetric lift/recovery clamp, and every other gate in
        // ApplyWheelie. This exists to answer one question cleanly: "CAN
        // the physics rig be moved to any given angle at all" - if this
        // can lift it and the normal wheelie control can't, the bug is in
        // ApplyWheelie's gating/authority, not in the rig itself.
        [Header("Debug Force (bypasses everything - a literal invisible hand)")]
        [Tooltip("Degrees of pitch to force the bike toward. Note: pitch is measured via asin(forward.y), which mathematically cannot exceed +-90 (a unit vector's elevation angle has no meaning past vertical) - a 'greater than 90' wheelie isn't representable by this metric at all, it would be a different manoeuvre (a full loop) with different physics. 90 is already the practical maximum to test with.")]
        [SerializeField] private float debugForcedPitchAngle = 0f;
        [Tooltip("0 = off (debug force does nothing). Above 0, this ignores speed/brake/ground/eligibility entirely and just holds the bike at the angle above - crank it up to confirm the rig itself can be moved to any angle, independent of whether the normal wheelie mechanic's own gating/authority is the problem.")]
        [SerializeField] private float debugForcedPitchStrength = 0f;

        // Round 11 added tunable wheelieLiftClamp/wheelieRecoverClamp here
        // (replacing a hardcoded, un-sliderable ceiling) - round 12 removed
        // them again, not reverting the fix but folding it into the new
        // single wheelieHydraulicStrength actuator instead (see that
        // field's own comment): one number, symmetric, no separate lift-vs-
        // recover split to reason about. The underlying fix (no more hidden,
        // un-sliderable ceiling on wheelie authority) is preserved - it's
        // just one slider now instead of two.

        // User request: "can you implement sliders for the weight of the
        // bike as well like back, front, left and right." Real motorcycle
        // wheelieing is heavily about weight bias (a rider shifting their
        // own weight back makes the front come up easier; forward makes it
        // harder) - offsetting the Rigidbody's own centre of mass is the
        // physically correct way to model that, not a fake multiplier on
        // some other system. Implemented as two offsets from the measured
        // baseline COM (see Awake/ApplyCenterOfMass) rather than four
        // separate fields, since "back" and "front" (and "left"/"right")
        // are the two ends of the same physical axis, not independent
        // quantities - the tuner exposes each as a single slider whose
        // negative/positive ends are labelled with the two directions the
        // user asked for.
        [Header("Weight Bias (centre of mass offset)")]
        [Tooltip("Metres to shift the centre of mass along the bike's own length, in the same +Z/forward direction as the bike's own nose. Positive = toward the front wheel (harder to wheelie, more front grip); negative = toward the rear wheel (easier to wheelie, more rear grip/wheelspin) - matches a real rider shifting their weight back to pop the front up.")]
        [SerializeField] private float comOffsetForward = 0f;
        [Tooltip("Metres to shift the centre of mass sideways. Negative = left, positive = right - affects how the bike balances/tips during cornering and mid-wheelie.")]
        [SerializeField] private float comOffsetRight = 0f;

        /// <summary>The COM transform's own measured local position,
        /// captured once in Awake before any slider offset is applied -
        /// comOffsetForward/comOffsetRight are added on top of this, not
        /// replacing it, so "0" on both sliders always means "exactly the
        /// originally measured centre of mass".</summary>
        private Vector3 baseCenterOfMassLocal;

        private Rigidbody rb;

        private float throttleInput;
        private float steerInput;
        private float brakeInput;

        private float currentSteer;
        private float currentVisualLean;
        private float currentWheelieTarget;
        private bool wheelieHeld;
        private bool wheelieYawLatched;
        private float lockedWheelieYaw;

        /// <summary>True while ApplyWheelie is directly driving the bike's
        /// pose (round 14). The roll/upright stabilisers MUST stand down
        /// while this is true - they exist to hold the bike level, so
        /// leaving them running during a deliberate 90-degree pitch means
        /// they spend every step fighting the very thing the player asked
        /// for, which is a large part of why torque-based lifting never
        /// worked in earlier rounds.</summary>
        private bool WheelieForcingPose => currentWheelieTarget > 0.01f;

        public float SpeedKmh =>
            rb == null ? 0f : rb.linearVelocity.magnitude * 3.6f;

        /// <summary>Current progressive wheelie pitch TARGET in degrees
        /// (0 = front down) - a SETTING/setpoint the pitch controller is
        /// chasing, not a measurement. Exposed for a future rider-pose hook
        /// (MINI-066/067) and for HUD/debug display.</summary>
        public float WheelieAngle => currentWheelieTarget;

        // Bug fix (user report: "the wheelie degrees reading it very false
        // it should start reading when the front tire is off the ground").
        // This used to be currentWheelieTarget > 1f - the CONTROLLER'S
        // SETPOINT, which climbs the instant the button is held and
        // eligible, regardless of whether the bike has actually left the
        // ground. Changed to the real, physical thing the user asked for:
        // whether the front WheelCollider itself is not touching the
        // ground right now. IsWheelieing now means "is genuinely
        // wheelieing", not "does the controller currently want to".
        public bool IsWheelieing => frontWheel != null && !frontWheel.isGrounded;

        /// <summary>Whether the front wheel is currently touching the
        /// ground - the real physical signal IsWheelieing is now built on,
        /// exposed directly for the tuner's status readout.</summary>
        public bool IsFrontWheelGrounded => frontWheel == null || frontWheel.isGrounded;

        // Live-tunable properties for TmaxWheelieTuner (test scene only -
        // see that script's own doc comment). User report: "i am not sure
        // how your tests passes but your tests are flawed... maybe you can
        // put the wheelie controls as sliders into unity and i will adjust
        // them to get the wheelie." Real headless Physics.Simulate() tests
        // drive the bike with exact synthetic inputs and can't reproduce
        // real keyboard feel/timing - these properties let the actual
        // values be dragged live in Play Mode instead of guessed at and
        // re-verified only by a script.
        public float MaxWheelieAngle { get => maxWheelieAngle; set => maxWheelieAngle = value; }
        public float WheelieRiseRate { get => wheelieRiseRate; set => wheelieRiseRate = value; }
        public float WheelieHydraulicStrength { get => wheelieHydraulicStrength; set => wheelieHydraulicStrength = value; }
        public float WheelieMinSpeedKmh { get => wheelieMinSpeedKmh; set => wheelieMinSpeedKmh = value; }
        public float WheelieMaxSpeedKmh { get => wheelieMaxSpeedKmh; set => wheelieMaxSpeedKmh = value; }
        public float WheelieAirSteerTorque { get => wheelieAirSteerTorque; set => wheelieAirSteerTorque = value; }
        public float WheelieRollAssist { get => wheelieRollAssist; set => wheelieRollAssist = value; }
        public float UprightAssist { get => uprightAssist; set => uprightAssist = value; }
        public float UprightAssistLowSpeedBoost { get => uprightAssistLowSpeedBoost; set => uprightAssistLowSpeedBoost = value; }
        public float UprightStrength { get => uprightStrength; set => uprightStrength = value; }
        public float UprightDamping { get => uprightDamping; set => uprightDamping = value; }
        public float LowSpeedExtraStability { get => lowSpeedExtraStability; set => lowSpeedExtraStability = value; }
        public float CenterOfMassOffsetForward { get => comOffsetForward; set => comOffsetForward = value; }
        public float CenterOfMassOffsetRight { get => comOffsetRight; set => comOffsetRight = value; }
        public float DebugForcedPitchAngle { get => debugForcedPitchAngle; set => debugForcedPitchAngle = value; }
        public float DebugForcedPitchStrength { get => debugForcedPitchStrength; set => debugForcedPitchStrength = value; }

        // MINI-119 follow-up, user: "sliders as not make it spin when
        // hitting ledge or bump or hill and then sliders for keep the
        // bike down like a gravity slider for when it leaves the ground."
        // Same live-tunable pattern as every property above.
        public float YawSpinThreshold { get => yawSpinThreshold; set => yawSpinThreshold = value; }
        public float YawSpinDamping { get => yawSpinDamping; set => yawSpinDamping = value; }
        public float CollisionYawSpinCap { get => collisionYawSpinCap; set => collisionYawSpinCap = value; }
        public float RampAssistStrength { get => rampAssistStrength; set => rampAssistStrength = value; }
        public float LedgeMaxHeight { get => ledgeMaxHeight; set => ledgeMaxHeight = value; }
        public float LedgeReactionStrength { get => ledgeReactionStrength; set => ledgeReactionStrength = value; }
        public float YawLockStrength { get => yawLockStrength; set => yawLockStrength = value; }
        public float YawLockTurnRate { get => yawLockTurnRate; set => yawLockTurnRate = value; }
        public float HillClimbAssist { get => hillClimbAssist; set => hillClimbAssist = value; }
        public float HillClimbMaxSlopeDeg { get => hillClimbMaxSlopeDeg; set => hillClimbMaxSlopeDeg = value; }
        public float ExtraAirGravity { get => extraAirGravity; set => extraAirGravity = value; }
        public float AirborneGraceSeconds { get => airborneGraceSeconds; set => airborneGraceSeconds = value; }
        public float AirGravityRampSeconds { get => airGravityRampSeconds; set => airGravityRampSeconds = value; }

        /// <summary>True right now if the normal (non-debug) wheelie could
        /// engage - i.e. every gate in ApplyWheelie's own "eligible" check
        /// is satisfied. Exposed for the tuner's pinned status box so the
        /// user can see directly WHY a wheelie isn't starting (too slow,
        /// too fast, braking, front/rear off the ground) rather than
        /// guessing from behaviour alone.</summary>
        public bool WheelieEligible =>
            enableWheelie
            && brakeInput < 0.1f
            && SpeedKmh >= wheelieMinSpeedKmh
            && SpeedKmh <= wheelieMaxSpeedKmh
            && rearWheel != null && rearWheel.isGrounded;

        /// <summary>True while the wheelie button is currently held (per
        /// SetWheelieHeld) - exposed so the tuner can show whether the game
        /// even registered the button press, separate from whether the
        /// wheelie was ELIGIBLE to act on it.</summary>
        public bool IsWheelieHeld => wheelieHeld;

        /// <summary>Current real physics pitch (world-frame, degrees) -
        /// exposed for the tuner's live readout. Distinct from WheelieAngle
        /// (the TARGET the pitch controller is chasing) so the two can be
        /// compared on-screen, which is exactly what caught the "target
        /// reaches 80 but real pitch lags/overshoots" class of question.</summary>
        public float CurrentPitchAngle =>
            transform == null ? 0f : Mathf.Asin(Mathf.Clamp(transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;

        /// <summary>Current real roll (degrees off upright, world frame) -
        /// exposed for the tuner's live readout, same measurement
        /// Mini065TmaxDropTest uses.</summary>
        /// <summary>The bike BODY.s current cosmetic lean in degrees
        /// (VisualLeanRoot.s Z rotation). Exposed so a mounted rider can roll
        /// WITH the bike: the rider sits on Seat, which is a SIBLING of
        /// VisualLeanRoot rather than a child, so without this the body
        /// leans into a corner while the rider stays bolt upright and their
        /// legs and hands visibly separate from the bike.</summary>
        public float MaxSpeedKmh { get => maxSpeedKmh; set => maxSpeedKmh = value; }
        public float MaxReverseSpeedKmh { get => maxReverseSpeedKmh; set => maxReverseSpeedKmh = value; }
        public float MotorTorque { get => motorTorque; set => motorTorque = value; }
        public float MaxVisualLean { get => maxVisualLean; set => maxVisualLean = value; }
        public float FullLeanSpeedKmh { get => fullLeanSpeedKmh; set => fullLeanSpeedKmh = value; }
        public float MinLeanSpeedKmh { get => minLeanSpeedKmh; set => minLeanSpeedKmh = value; }

        public float CurrentVisualLean => currentVisualLean;

        public float CurrentRollAngle
        {
            get
            {
                if (transform == null) return 0f;
                Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
                if (flatForward.sqrMagnitude < 0.0001f) flatForward = transform.forward;
                flatForward.Normalize();
                return Vector3.SignedAngle(transform.up, Vector3.up, flatForward);
            }
        }

        [Header("Trike Stabilizers (round 13)")]
        [Tooltip("How far above/below each anchor's own height to search for ground - real suspension travel for the virtual outrigger spring.")]
        [SerializeField] private float stabilizerRayRange = 0.15f;
        [Tooltip("Spring stiffness (N/m) for the virtual outrigger - a real Rigidbody.AddForceAtPosition spring, not an abstract torque. Roughly matched to the real rear WheelCollider's own spring rate.")]
        [SerializeField] private float stabilizerSpringRate = 6000f;
        [Tooltip("Damping for the virtual outrigger spring, proportional to how fast the anchor point itself is moving vertically.")]
        [SerializeField] private float stabilizerDamping = 400f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();

            baseCenterOfMassLocal =
                centerOfMass != null
                    ? transform.InverseTransformPoint(centerOfMass.position)
                    : rb.centerOfMass;

            ApplyCenterOfMass();
        }

        /// <summary>Re-applies the measured base centre of mass plus the
        /// live weight-bias sliders. Called every FixedUpdate (not just
        /// once) so a slider dragged mid-Play-Mode takes effect on the very
        /// next physics step, the same way every other tuner value does.</summary>
        private void ApplyCenterOfMass()
        {
            if (rb == null) return;

            Vector3 local = baseCenterOfMassLocal;
            local.x += comOffsetRight;
            local.z += comOffsetForward;
            rb.centerOfMass = local;
        }

        public void SetInput(float throttle, float steer, float brake)
        {
            throttleInput = Mathf.Clamp(throttle, -1f, 1f);
            steerInput = Mathf.Clamp(steer, -1f, 1f);
            brakeInput = Mathf.Clamp01(brake);
        }

        /// <summary>
        /// The explicit wheelie button (E in the test rig - see
        /// TmaxTestInput), reporting whether it is currently HELD (not a
        /// one-shot tap edge - see the Wheelie header comment for why this
        /// changed from the previous tap-based design). Deliberately a
        /// distinct method rather than a fourth SetInput parameter, so
        /// callers that only care about drive/steer/brake (AI, a simpler
        /// mobile control scheme) don't need to know the wheelie button
        /// exists at all - same decoupling reasoning as SetInput itself.
        /// </summary>
        public void SetWheelieHeld(bool held) => wheelieHeld = held;

        private void FixedUpdate()
        {
            ApplyTrikeStabilizers();
            ApplyExtraAirGravity();
            ApplyCenterOfMass();
            ApplyDrive();
            ApplySteering();
            ApplyStability();
            ApplyYawSpinAssist();
            ApplyYawLock();
            ApplyWheelie();
            ApplyWheelieAirControl();
            ApplyUprightAssist();
            ApplyDebugForcedPitch();
        }

        [Header("Yaw Spin Assist (anti wild-spin on lateral impacts)")]
        [Tooltip("MINI-119, user: \"i hit small hedge the bike spins like crazy... there should be an assist... make it lose control slightly but not all that spin, it should just be slightly.\" A hedge/kerb clipped at an angle dumps a large yaw impulse into the Rigidbody that nothing here previously opposed (ApplyStability only ever corrects ROLL, never yaw). This damps yaw angular velocity, but only the part ABOVE yawSpinThreshold, and only removes a FRACTION of that excess per second - ordinary steering-induced yaw (turning corners) sits far below the threshold and is completely untouched, and even a genuine hedge hit still spins, just far less wildly, matching \"lose control slightly, not all that spin.\"")]
        // MINI-119 follow-up round 5, user-tuned final value ("save all
        // these settings") - read directly off the live tuner panel.
        [SerializeField] private float yawSpinThreshold = 0f;
        // MINI-119 follow-up, user: "sliders as not make it spin when
        // hitting ledge or bump or hill... the sliders should [be] wide
        // so i can drastically reduce the spinning." Un-capped from the
        // original Range(0,1) - that "1" was a fraction-per-second in
        // name only; because it is multiplied by Time.fixedDeltaTime
        // (~0.02s) before being applied (see ApplyYawSpinAssist), a value
        // of 1 only ever removed ~2% of the excess spin per physics step,
        // nowhere near "drastic". The field is now an uncapped rate: the
        // per-step removal is still Clamp01'd internally so it can never
        // remove MORE than 100% of the excess in one step (no overshoot,
        // no reversal), but a high value (see TmaxWheelieTuner's slider,
        // which now goes to 50) reaches that 100%-per-step ceiling and
        // reads as the excess spin vanishing almost the instant it starts.
        // MINI-119 follow-up round 5, user-tuned final value.
        [SerializeField] private float yawSpinDamping = 250f;

        private void ApplyYawSpinAssist()
        {
            if (rb == null || WheelieForcingPose) return; // wheelie already zeroes angularVelocity itself each step

            float yawRateDeg = Vector3.Dot(rb.angularVelocity, Vector3.up) * Mathf.Rad2Deg;
            float absYaw = Mathf.Abs(yawRateDeg);
            if (absYaw <= yawSpinThreshold) return;

            float excess = absYaw - yawSpinThreshold;
            float removed = excess * Mathf.Clamp01(yawSpinDamping * Time.fixedDeltaTime);
            float newYawRateDeg = Mathf.Sign(yawRateDeg) * (absYaw - removed);

            Vector3 av = rb.angularVelocity;
            av -= Vector3.up * Vector3.Dot(av, Vector3.up); // strip the old yaw component only
            av += Vector3.up * (newYawRateDeg * Mathf.Deg2Rad);
            rb.angularVelocity = av;
        }

        // MINI-119 follow-up round 4, user (after round 3's collision-
        // event-based fix still failed): "the bike keeps turning when i
        // hit a ledge... keep the bike straight when you hit the ledge no
        // turning." Root problem with every earlier round: they all
        // reacted to a COLLISION EVENT (OnCollisionEnter/Stay) or damped
        // ANGULAR VELOCITY after the fact - but a short/low ledge can be
        // caught entirely by a WheelCollider's own suspension/friction
        // model, which never raises a chassis collision event at all, and
        // PhysX can also resolve a sharp/thin contact with a direct
        // one-step POSITION/ROTATION correction that doesn't show up as
        // "high angular velocity" for these methods to damp in the first
        // place. This is a fundamentally different, much blunter fix that
        // sidesteps all of that: yaw stops being physics-derived at all
        // and becomes DIRECTLY, KINEMATICALLY driven from the player's own
        // steering input alone (MoveRotation every step) - the exact same
        // proven technique ApplyWheelie already uses for pitch (see that
        // method's own "round 14" history: torque-based persuasion never
        // worked reliably; direct kinematic control did). Nothing else -
        // not a curb, not wheel friction, not a chassis collision - can
        // turn the bike anymore once this is active; only steerInput can.
        [Header("Yaw Lock (guarantees the bike only turns from your own steering)")]
        [Tooltip("0 = normal physics-driven yaw, fully exposed to being spun by collisions/wheel friction (the original problem). 1 = yaw is ENTIRELY kinematic - heading changes ONLY from your own steering input at yawLockTurnRate, and nothing else can turn it at all, period. Defaults to 1 this round given the explicit \"no turning, no matter what\" ask - drop it toward 0 if you want some real reaction back.")]
        [SerializeField, Range(0f, 1f)] private float yawLockStrength = 1f;
        [Tooltip("Degrees/second the locked heading turns at full steering input and typical riding speed - tune to match how sharply the bike should turn under normal steering.")]
        [SerializeField] private float yawLockTurnRate = 110f;

        private float _lockedYawDeg;
        private bool _yawLockInitialized;

        private void ApplyYawLock()
        {
            if (rb == null || yawLockStrength <= 0f || WheelieForcingPose)
            {
                _yawLockInitialized = false; // re-sync to the real heading next time this turns back on
                return;
            }

            if (!_yawLockInitialized)
            {
                _lockedYawDeg = transform.eulerAngles.y;
                _yawLockInitialized = true;
            }

            // Only the player's own steering advances the locked heading -
            // speed-scaled so it can't spin on the spot at a standstill,
            // same shape as ApplySteering's own yaw assist.
            float speed01 = Mathf.Clamp01(Mathf.Abs(SpeedKmh) / 15f);
            _lockedYawDeg += steerInput * yawLockTurnRate * speed01 * Time.fixedDeltaTime;

            Vector3 euler = transform.eulerAngles;
            Quaternion target = Quaternion.Euler(euler.x, _lockedYawDeg, euler.z);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, target, yawLockStrength));

            // Strip whatever physics-integrated yaw velocity remains,
            // proportionally, so nothing lingers to fight next step or
            // show up the instant this is turned back down.
            Vector3 av = rb.angularVelocity;
            av -= Vector3.up * (Vector3.Dot(av, Vector3.up) * yawLockStrength);
            rb.angularVelocity = av;
        }

        // MINI-119 follow-up round 3, user: "i tested it now but its
        // still doing the same thing, I want this to be so strict i dont
        // want the bike fliping spinning or doing anything but moving
        // straight... please fix this dont make it react to the sidewalk
        // or ledges... then you can create a slider for me to make it
        // react if i want." Rounds 1/2 (yawSpinThreshold/Damping,
        // collisionYawSpinCap) only ever damped/capped the RATE of spin,
        // gradually or on-contact - never strong enough, per the user's
        // own report. This round is a different, much blunter
        // instrument, matching the explicit ask: for a collision
        // classified as ledge-like (every contact point sits at or below
        // ledgeMaxHeight above the bike's own ground level - the same
        // "is this a kerb/sidewalk, not a wall/car/NPC" test used below
        // for ramp assist), angular velocity is scaled straight to
        // ledgeReactionStrength * (whatever it was) - AT THE DEFAULT 0,
        // that is a hard, total zero, every single physics step the
        // contact persists, not a gradual settle - and the rotation
        // itself is actively re-levelled back toward flat/forward at the
        // same rate, so an already-started roll/pitch/yaw from this same
        // contact gets pulled back out, not just prevented from growing
        // further. A real wall/vehicle/NPC collision (every contact point
        // ABOVE ledgeMaxHeight) is deliberately left alone - this is
        // specifically the "sidewalk/ledge" case, not a general crash
        // suppressor.
        [Header("Ledge / Sidewalk Reaction (default: none at all)")]
        [Tooltip("How high above the bike's own current ground level a collision contact point can be and still count as a low kerb/ledge/sidewalk lip, rather than a real solid obstacle (wall, vehicle, NPC) that should still behave normally.")]
        [SerializeField] private float ledgeMaxHeight = 0.6f;
        [Tooltip("MINI-119 follow-up round 3, user: \"i dont want the bike fliping spinning or doing anything but moving straight... dont make it react to the sidewalk or ledges... then you can create a slider for me to make it react if i want.\" 0 (the default) = zero rotational reaction to a ledge-classified collision at all, every step, actively pulled back level too - not just capped. 1 = full, unsuppressed physical reaction, same as if this whole system didn't exist. Values in between let a proportional amount through.")]
        [SerializeField, Range(0f, 1f)] private float ledgeReactionStrength = 0f;

        [Header("Ramp Assist (rides up over ledges instead of catching)")]
        [Tooltip("MINI-119 follow-up round 2, user: \"can you maybe put ramp assistant so when the bike is about to hit a sharp colider it would be like going on the smooth ramp.\" On a ledge-classified collision (see ledgeMaxHeight above) whose contact normal reads as a roughly-vertical face (a kerb/ledge lip specifically, not a flat top surface), nudges the bike up and slightly forward at that real contact point - a genuine AddForceAtPosition, same 'real force at a real contact point' principle as the trike stabilizer springs - so it rides up and over instead of catching on the corner. 0 = off. Independent of ledgeReactionStrength above - this is a positive assist, not a reaction.")]
        [SerializeField] private float rampAssistStrength = 0f;

        [Header("Collision Impact Control (real obstacles - walls/vehicles/NPCs)")]
        [Tooltip("Hard cap (deg/s) on yaw spin the instant the bike's own body collides with something taller than ledgeMaxHeight (a genuine solid obstacle, not a kerb/sidewalk - those are handled entirely by Ledge/Sidewalk Reaction above). Unlike Yaw Spin Threshold/Damping further up (a gradual per-second damping of excess), this clamps immediately and unconditionally on contact.")]
        [SerializeField] private float collisionYawSpinCap = 40f;

        private void OnCollisionEnter(Collision collision) => HandleChassisCollision(collision);
        private void OnCollisionStay(Collision collision) => HandleChassisCollision(collision);

        private void HandleChassisCollision(Collision collision)
        {
            if (rb == null || WheelieForcingPose) return; // stand down during a deliberate forced pose, same as every other stabiliser

            float groundY = transform.position.y - 0.3f; // rough wheel-contact reference, same one ramp assist already used
            bool isLedge = true;
            foreach (var c in collision.contacts)
            {
                if (c.point.y - groundY > ledgeMaxHeight) { isLedge = false; break; }
            }

            if (isLedge)
            {
                // Total (at default) rotational suppression - scale
                // whatever angular velocity exists straight down, every
                // step, rather than only capping its growth.
                rb.angularVelocity *= ledgeReactionStrength;

                // Also actively pull any roll/pitch/yaw ALREADY picked up
                // from this same contact back toward level/forward - a
                // cap alone only stops it getting WORSE, it doesn't undo
                // what already happened this step.
                if (ledgeReactionStrength < 0.999f)
                {
                    Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
                    if (flatForward.sqrMagnitude > 0.0001f)
                    {
                        Quaternion levelled = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
                        rb.MoveRotation(Quaternion.Slerp(rb.rotation, levelled, 1f - ledgeReactionStrength));
                    }
                }

                ApplyRampAssist(collision);
                return;
            }

            // A real solid obstacle (wall/vehicle/NPC) - keep the softer,
            // general-purpose cap from round 2 rather than the ledge
            // system's much harsher total suppression.
            float yawRateDeg = Vector3.Dot(rb.angularVelocity, Vector3.up) * Mathf.Rad2Deg;
            float capped = Mathf.Clamp(yawRateDeg, -collisionYawSpinCap, collisionYawSpinCap);
            if (!Mathf.Approximately(capped, yawRateDeg))
            {
                Vector3 av = rb.angularVelocity;
                av -= Vector3.up * Vector3.Dot(av, Vector3.up);
                av += Vector3.up * (capped * Mathf.Deg2Rad);
                rb.angularVelocity = av;
            }
        }

        private void ApplyRampAssist(Collision collision)
        {
            if (rampAssistStrength <= 0f) return;
            foreach (var contact in collision.contacts)
            {
                // Skip a flat top surface (normal near straight up) -
                // only a roughly-vertical-faced lip counts as a
                // rampable edge.
                float normalUpDot = Vector3.Dot(contact.normal, Vector3.up);
                if (Mathf.Abs(normalUpDot) > 0.5f) continue;

                Vector3 nudge = (Vector3.up + transform.forward * 0.5f).normalized * rampAssistStrength;
                rb.AddForceAtPosition(nudge, contact.point, ForceMode.Acceleration);
            }
        }

        // MINI-118, user: "could there be a gravity added so when the bike
        // reaches a certain height when it goes up from the ground if what
        // you did doesnt work" - a real, standard technique (extra
        // downward force once genuinely airborne, common in arcade vehicle
        // physics) added as a second, independent layer alongside the mass
        // increase, rather than waiting to find out the mass change alone
        // wasn't enough. Gated on BOTH wheels being ungrounded for a
        // sustained period (the same debounce idea as ApplyTrikeStabilizers'
        // own _frontUngroundedSeconds, so an ordinary single-frame bump
        // never triggers it) AND never during an actual wheelie (whose own
        // kinematic rear-pivot rotation keeps the rear wheel grounded, so
        // "both wheels off the ground" should not normally coincide with a
        // real wheelie anyway - WheelieForcingPose is still checked
        // explicitly as a second, belt-and-braces guard). Scales up with
        // how long the bike has actually been airborne, so a brief hop off
        // a bump barely feels it while a genuine launch gets pulled back
        // down hard.
        [Tooltip("MINI-118: extra downward acceleration (on top of normal gravity) once the bike has been genuinely airborne (both wheels off the ground) for longer than airborneGraceSeconds - pulls an unwanted launch back down faster without affecting an intentional wheelie.")]
        // MINI-119 follow-up round 5, user-tuned final values ("save all
        // these settings") - read directly off the live tuner panel:
        // pushed to the 2500 ceiling, with almost no grace period and no
        // ramp-up, i.e. "come down hard, almost immediately."
        [SerializeField] private float extraAirGravity = 2500f;
        [Tooltip("How long both wheels must stay ungrounded before extra air gravity kicks in - long enough that a normal bump/bike-hop never triggers it.")]
        [SerializeField] private float airborneGraceSeconds = 0.01f;
        [Tooltip("Seconds of sustained air time for extraAirGravity to ramp up to its full strength, rather than snapping on.")]
        [SerializeField] private float airGravityRampSeconds = 0f;

        private float _bothWheelsAirborneSeconds;

        private void ApplyExtraAirGravity()
        {
            bool bothAirborne = frontWheel != null && rearWheel != null
                && !frontWheel.isGrounded && !rearWheel.isGrounded;

            if (!bothAirborne || WheelieForcingPose)
            {
                _bothWheelsAirborneSeconds = 0f;
                return;
            }

            _bothWheelsAirborneSeconds += Time.fixedDeltaTime;
            float pastGrace = _bothWheelsAirborneSeconds - airborneGraceSeconds;
            if (pastGrace <= 0f) return;

            float rampT = airGravityRampSeconds > 0.001f ? Mathf.Clamp01(pastGrace / airGravityRampSeconds) : 1f;
            rb.AddForce(Vector3.down * (extraAirGravity * rampT), ForceMode.Acceleration);
        }

        private void Update()
        {
            UpdateVisualLean();
        }

        /// <summary>
        /// Round 13 - "make it two invisible wheels at the back and one
        /// invisible wheel at the front so it will appear as a bike but it
        /// will be a trike only when you press E to wheelie." Then: "once
        /// the bike is wheelieing as well the trike collider should still
        /// stay on while the bike is up." Active whenever ANY of the
        /// following is true: the player wants a wheelie and the
        /// eligibility window is open (so support is already there BEFORE
        /// roll has a chance to start, not catching up after), the wheelie
        /// target is still above zero (covers the climb and the release
        /// ramp-down), OR the front wheel is genuinely off the ground right
        /// now (covers "the bike is up" - the real physical state, not just
        /// the controller's own target, per the user's explicit follow-up).
        /// Inactive the rest of the time, so it cannot affect normal
        /// riding/cornering at all outside a wheelie.
        ///
        /// Implementation (see rearStabLeftAnchor's own comment for why this
        /// is a raycast + AddForceAtPosition spring rather than real
        /// WheelCollider components): for each anchor, cast a short ray
        /// straight down; if it finds ground within stabilizerRayRange,
        /// apply a real spring+damper force at the hit point, same shape as
        /// a normal suspension (force = penetration * spring - velocity *
        /// damping). A real Rigidbody.AddForceAtPosition force at an actual
        /// ground contact point produces a genuine, correctly-levered
        /// anti-roll torque through real physics - the same "real force at
        /// a real contact point beats an abstract torque" principle the
        /// round-11 wheelie research established - without adding any
        /// Collider component to the Rigidbody at all.
        /// </summary>
        // MINI-080: "the bike still jumps too much from riding and hitting
        // ramps and objects" - the third OR clause below (front wheel
        // ungrounded) used to fire on ANY single-frame loss of front-wheel
        // contact, including a normal bump/ramp hop with no wheelie intent
        // at all. That momentarily activated this ADDITIONAL 6000 N/m rear
        // spring on TOP of the normal WheelCollider suspension - stacking two
        // springs at exactly the moment a bump already unsettles the bike,
        // which is a real, understated contributor to "way too high" bounce.
        // Debounced: the front wheel now has to stay ungrounded for a
        // SUSTAINED period before this clause activates, which a genuine
        // wheelie clears easily and a quick bump hop does not.
        private float _frontUngroundedSeconds;

        private void ApplyTrikeStabilizers()
        {
            if (frontWheel != null && !frontWheel.isGrounded)
                _frontUngroundedSeconds += Time.fixedDeltaTime;
            else
                _frontUngroundedSeconds = 0f;

            bool active =
                (wheelieHeld && WheelieEligible)
                || currentWheelieTarget > 1f
                || _frontUngroundedSeconds > 0.12f;

            if (!active) return;

            ApplyStabilizerSpring(rearStabLeftAnchor);
            ApplyStabilizerSpring(rearStabRightAnchor);
        }

        private void ApplyStabilizerSpring(Transform anchor)
        {
            if (anchor == null || rb == null) return;

            Vector3 origin = anchor.position + Vector3.up * stabilizerRayRange;
            float maxDist = stabilizerRayRange * 2f;

            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxDist))
                return;

            float penetration = maxDist - hit.distance;
            float pointVelocityY = rb.GetPointVelocity(hit.point).y;

            float force = (penetration * stabilizerSpringRate) - (pointVelocityY * stabilizerDamping);
            force = Mathf.Max(0f, force); // a spring only ever pushes, never pulls

            rb.AddForceAtPosition(Vector3.up * force, hit.point, ForceMode.Force);
        }

        [Header("Hill Climb Assist")]
        [Tooltip("MINI-119, user: \"should have enough power to climb hill and ledges because this is a powerful bike.\" Plain WheelCollider motorTorque alone was still losing too much speed on an incline once the bike got heavier (MINI-118). This adds extra forward push while grounded, throttling on, and climbing - proportional to how steep the ground ahead actually is (0 on flat ground, full at hillClimbMaxSlopeDeg), so ordinary flat-ground driving is completely unaffected. ForceMode.Acceleration - mass-independent by design (see this file's own header note on that), so it reads as a consistent power boost regardless of any future mass tuning.")]
        [SerializeField] private float hillClimbAssist = 9f;
        [Tooltip("Slope angle (degrees off horizontal) at which hillClimbAssist reaches full strength.")]
        [SerializeField] private float hillClimbMaxSlopeDeg = 35f;

        private void ApplyDrive()
        {
            if (frontWheel == null || rearWheel == null)
                return;

            float forwardSpeed =
                Vector3.Dot(rb.linearVelocity, transform.forward) * 3.6f;

            // Forward and reverse are limited separately - sharing one cap meant
            // reverse would wind up to the full forward top speed, which is
            // neither realistic nor controllable.
            bool belowMaxSpeed = throttleInput >= 0f
                ? forwardSpeed < maxSpeedKmh
                : forwardSpeed > -maxReverseSpeedKmh;

            // Rear-wheel drive (a scooter/maxi-scooter is rear-driven,
            // unlike the downloaded reference's single front-driven
            // wheel), front wheel free-rolling except when braking.
            rearWheel.motorTorque =
                belowMaxSpeed ? throttleInput * motorTorque : 0f;

            float braking = brakeInput * brakeTorque;

            // More braking at front, less at rear - matches real
            // motorcycle weight transfer under braking.
            frontWheel.brakeTorque = braking * 0.65f;
            rearWheel.brakeTorque = braking * 0.35f;

            ApplyHillClimbAssist(belowMaxSpeed);
        }

        /// <summary>
        /// Extra forward acceleration on an incline, on top of the normal
        /// motorTorque above - see the Hill Climb Assist header comment.
        /// Reads the rear wheel's own ground-contact normal (the real
        /// slope under the bike right now, not the world terrain in the
        /// abstract), so it naturally also fires on a ledge/kerb lip, not
        /// only an open hillside.
        /// </summary>
        private void ApplyHillClimbAssist(bool belowMaxSpeed)
        {
            if (hillClimbAssist <= 0f || throttleInput <= 0.01f || !belowMaxSpeed) return;
            if (!rearWheel.isGrounded || !rearWheel.GetGroundHit(out WheelHit hit)) return;

            float slopeDeg = Vector3.Angle(hit.normal, Vector3.up);
            if (slopeDeg < 1f) return; // flat ground - no assist, no change to normal feel

            float slope01 = Mathf.Clamp01(slopeDeg / Mathf.Max(1f, hillClimbMaxSlopeDeg));
            rb.AddForce(transform.forward * (hillClimbAssist * slope01 * throttleInput), ForceMode.Acceleration);
        }

        private void ApplySteering()
        {
            float speed01 =
                Mathf.Clamp01(SpeedKmh / maxSpeedKmh);

            float allowedSteer =
                Mathf.Lerp(
                    maxSteerAngle,
                    highSpeedSteerAngle,
                    speed01
                );

            // "Steering remains possible but reduced" while wheelieing.
            if (enableWheelie && maxWheelieAngle > 0.01f)
            {
                float wheelie01 = Mathf.Clamp01(currentWheelieTarget / maxWheelieAngle);
                allowedSteer *= Mathf.Lerp(1f, wheelieSteerMultiplier, wheelie01);
            }

            float targetSteer =
                steerInput * allowedSteer;

            currentSteer =
                Mathf.Lerp(
                    currentSteer,
                    targetSteer,
                    steerResponse * Time.fixedDeltaTime
                );

            frontWheel.steerAngle = currentSteer;

            // Small arcade yaw assistance.
            if (SpeedKmh > 5f && frontWheel.isGrounded)
            {
                float assist =
                    steerInput *
                    yawAssist *
                    Mathf.Clamp01(SpeedKmh / 30f);

                rb.AddTorque(
                    Vector3.up * assist,
                    ForceMode.Acceleration
                );
            }
        }

        private void ApplyStability()
        {
            if (!frontWheel.isGrounded && !rearWheel.isGrounded)
                return;

            // Stand down while the wheelie is directly driving the pose -
            // see WheelieForcingPose. This stabiliser's whole job is to
            // remove tilt, so running it during an intentional wheelie
            // means fighting the player's own input.
            if (WheelieForcingPose) return;

            // Bug fix (user report: holding E to wheelie made the bike
            // "lean more to the left" - confirmed with a real physics test
            // showing genuine roll growing right alongside wheelie pitch).
            // Root cause: this used the bike's raw transform.forward as
            // BOTH the roll-error measurement axis and the torque axis.
            // That is only a pure roll axis while the bike is level - once
            // pitch is significant (exactly what a wheelie is), forward
            // itself is tilted up, and correcting "roll around a tilted
            // axis" is not the same as correcting roll in the world frame;
            // the difference leaks into yaw, which is what actually
            // produced the visible sideways lean. Flattening forward onto
            // the horizontal plane first gives a roll axis that stays pure
            // regardless of how much pitch is happening, so this can run
            // unconditionally through a wheelie without fighting it.
            Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.0001f) flatForward = transform.forward;
            flatForward.Normalize();

            float rollError =
                Vector3.SignedAngle(
                    transform.up,
                    Vector3.up,
                    flatForward
                );

            float rollVelocity =
                Vector3.Dot(rb.angularVelocity, flatForward);

            float stability = uprightStrength;

            if (SpeedKmh < 12f)
                stability += lowSpeedExtraStability;

            // Tried adding extra gain here for the steeper/faster wheelie
            // (see git history / PROJECT-HANDOFF for the full account) -
            // a real physics test proved that was WRONG: pushing this
            // torque-based PD controller's gain up further made the roll
            // measurably worse (93.6deg peak, up from 58.7deg, and briefly
            // flipped the bike outright) rather than better. This is a
            // classic PD-instability symptom, not a tuning nudge away from
            // working - AddTorque integrates velocity every step, so too
            // much proportional gain relative to damping overshoots and
            // the overshoot compounds. The actual fix for the steep-wheelie
            // case lives in ApplyUprightAssist instead, which corrects roll
            // via a direct MoveRotation/Slerp toward an exact zero-roll
            // orientation for the current forward - bounded to [0,1] per
            // step, so it geometrically cannot overshoot/oscillate the way
            // this torque-based approach can. Leave this method's gain at
            // its plain driving-tuned value; do not repeat this experiment.
            float correction =
                (-rollError * stability) -
                (rollVelocity * uprightDamping);

            rb.AddTorque(
                flatForward * correction,
                ForceMode.Acceleration
            );
        }

        /// <summary>
        /// Pitch (nose up/down) control. Normally holds the bike level
        /// (target 0) the same way ApplyStability holds roll level; while
        /// the wheelie condition is met, the target progressively ramps
        /// up toward maxWheelieAngle instead, and ramps back down the
        /// moment throttle drops or the window is left - this is both the
        /// baseline pitch stabiliser AND the wheelie mechanic, since a
        /// wheelie is just a deliberately large, deliberately allowed
        /// pitch error.
        /// </summary>
        private void ApplyWheelie()
        {
            if (frontWheel == null || rearWheel == null) return;

            // Bug fix (user report: bike spinning wildly in mid-air on
            // Play). This torque used to apply unconditionally, including
            // while fully airborne with nothing else opposing it -
            // ApplyStability already skips its own roll correction while
            // both wheels are off the ground; this now matches that same
            // gate, so a genuinely airborne bike just tumbles under plain
            // Rigidbody physics instead of accumulating unopposed pitch
            // torque every FixedUpdate.
            // Round 14 bug fix, caught by reading the drop test's own trace:
            // during a real (now genuinely high) wheelie BOTH wheels leave
            // the ground, so this early return fired and froze
            // currentWheelieTarget completely - the release ramp measured
            // 0.14 deg/step for its first 60 steps instead of the 1.2
            // deg/step wheelieRiseRate asks for, i.e. the bike hung in the
            // air instead of coming down. While the wheelie is directly
            // driving the pose we are deliberately holding the bike up, so
            // this must keep running.
            if (!frontWheel.isGrounded && !rearWheel.isGrounded && !WheelieForcingPose) return;

            // Bug fix (user report: the wheelie "goes to the right a little
            // when it goes up" - the bike should go up straight). Same class
            // of bug already fixed once in ApplyStability: this used to
            // measure and apply pitch torque around the bike's raw
            // transform.right, which is only a pure, roll-free lateral axis
            // while the bike is level. The moment ApplyStability's own
            // correction (or just normal riding) leaves even a small amount
            // of residual roll on the Rigidbody, a tilted transform.right
            // has a vertical component, so "pure pitch" torque applied
            // around it also injects a bit of yaw/roll - which reads
            // exactly as "drifting to the side while lifting". Building a
            // right axis from the flattened forward instead (perpendicular
            // to world up) keeps it a pure horizontal lateral axis
            // regardless of the bike's current roll, so the wheelie's pitch
            // torque can no longer leak sideways.
            Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.0001f) flatForward = transform.forward;
            flatForward.Normalize();
            Vector3 flatRight = Vector3.Cross(Vector3.up, flatForward);

            float pitchAngle =
                Mathf.Asin(Mathf.Clamp(transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
            float pitchVelocity =
                Vector3.Dot(rb.angularVelocity, flatRight);

            // Explicit-button, hold-to-lift design (see the Wheelie header
            // comment for why). Eligibility no longer looks at
            // throttleInput at all - only the button and a sane speed/
            // brake/ground window - so holding full throttle alone can
            // never trigger it by accident.
            // Round 14 fix: STARTING a wheelie needs rear grip and a sane
            // speed window; SUSTAINING one must not. The drop test's trace
            // caught this - at a high angle the rear wheel chatters on and
            // off the ground, so a single eligibility test flickered
            // true/false every step and MoveTowards alternated climb/fall,
            // freezing real pitch at 66.7 degrees instead of continuing to
            // the requested angle. Once the bike is genuinely up, holding
            // the button is the only thing that should keep it there.
            bool canStart =
                enableWheelie
                && brakeInput < 0.1f
                && SpeedKmh >= wheelieMinSpeedKmh
                && SpeedKmh <= wheelieMaxSpeedKmh
                && rearWheel.isGrounded;

            bool canSustain = enableWheelie && brakeInput < 0.1f;

            bool eligible = WheelieForcingPose ? canSustain : canStart;

            // Hold-to-lift: while the button is held AND the window is
            // open, the target climbs straight to maxWheelieAngle; the
            // MoveTowards below (wheelieRiseRate) is what makes that
            // progressive rather than instant. Releasing - or leaving the
            // window at all (braking, off the ground, too slow/fast) -
            // sends the target straight back to 0 at the same rate, which
            // is what gives a quick release-and-reapply its "balancing"
            // feel: the front settles a little before climbing again,
            // rather than snapping.
            bool wantsWheelie = eligible && wheelieHeld;
            float targetPitch = wantsWheelie ? maxWheelieAngle : 0f;

            currentWheelieTarget =
                Mathf.MoveTowards(currentWheelieTarget, targetPitch, wheelieRiseRate * Time.fixedDeltaTime);

            // Extra REAL rear-wheel drive torque while lifting, per "add
            // more torque to help this on the rear wheels".
            if (wantsWheelie)
                rearWheel.motorTorque += wheelieRearTorqueBoost;

            // ROUND 14 - THE ACTUAL FIX. Every previous round tried to
            // PERSUADE the bike to pitch up with torque (AddTorque, PD
            // gains, clamps, rear-wheel traction). A corrected drop test
            // finally measured the REAL Rigidbody pitch instead of the
            // controller's own setpoint and proved all of it barely worked:
            // real pitch reached 7.2 degrees while the setpoint claimed 80,
            // and the front wheel was never off the ground for even one
            // physics step out of 100 - exactly the user's report ("it
            // doesnt life the ground much like maybe 1 or 2 degrees up").
            // Torque was always being swamped by 220kg of bike, the
            // WheelCollider suspension pushing back, and this class's own
            // stability systems.
            //
            // So this no longer asks. It DIRECTLY DRIVES the rotation, per
            // the user's explicit instruction: "make it abitrarily life the
            // ground... do not make it fall or move to any side, keep it
            // straight in the air", "make it go all the way to 360".
            //
            // Two properties this gets for free, which no amount of torque
            // tuning ever achieved:
            //  1) The rotation is built as yaw * pitch ONLY - there is no
            //     roll term anywhere in it - so the bike is mathematically
            //     incapable of leaning left or right while lifting. That is
            //     the "keep it straight in the air"/trike behaviour.
            //  2) It rotates about the REAR CONTACT PATCH, not the centre
            //     of mass, so the front genuinely rises off the ground
            //     (rotating about the COM would sink the rear into the road
            //     as much as it raised the front). This is what actually
            //     makes the front wheel - collider AND visible model, since
            //     both are children of this Rigidbody - leave the ground.
            //
            // Angle is tracked as a plain accumulating number, NOT measured
            // back via asin(forward.y), so it is not limited to 90 degrees
            // and can be driven all the way round to 360 as requested. No
            // cap is imposed here - maxWheelieAngle's slider is the only
            // limit, per "dont put caps on the wheel height yet, i will put
            // the caps with the slider".
            if (currentWheelieTarget <= 0.01f)
            {
                wheelieYawLatched = false;
                return;
            }

            // Latch heading when the lift starts; steering can still turn it
            // in the air, but nothing else may drift it sideways.
            if (!wheelieYawLatched)
            {
                lockedWheelieYaw = transform.eulerAngles.y;
                wheelieYawLatched = true;
            }
            lockedWheelieYaw += steerInput * wheelieAirSteerTorque * Time.fixedDeltaTime;

            // Rear contact patch, in world space.
            Vector3 pivot = rearWheel.transform.position - Vector3.up * rearWheel.radius;

            Quaternion newRot =
                Quaternion.Euler(0f, lockedWheelieYaw, 0f) *
                Quaternion.Euler(-currentWheelieTarget, 0f, 0f);

            Quaternion delta = newRot * Quaternion.Inverse(rb.rotation);
            Vector3 newPos = pivot + delta * (rb.position - pivot);

            rb.MoveRotation(newRot);
            rb.MovePosition(newPos);
            // No residual spin - otherwise the moment the forced pose ends
            // the bike would fling itself sideways with whatever angular
            // momentum had accumulated underneath it.
            rb.angularVelocity = Vector3.zero;
        }

        /// <summary>
        /// User's own explicit request: "invisible holders to bring it up
        /// and hold it upright that can be adjusted." A direct, always-on
        /// (once strength > 0) pitch override that ignores every gate in
        /// ApplyWheelie above - speed window, brake, ground, eligibility,
        /// the lift/recover clamps, all of it. Exists to answer one
        /// question cleanly: can the physics rig itself reach a given
        /// angle at all, independent of whether the normal wheelie
        /// mechanic's own gating is the actual problem. Off by default
        /// (debugForcedPitchStrength = 0f does nothing).
        /// </summary>
        private void ApplyDebugForcedPitch()
        {
            if (debugForcedPitchStrength <= 0f) return;
            if (rb == null) return;

            Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.0001f) flatForward = transform.forward;
            flatForward.Normalize();
            Vector3 flatRight = Vector3.Cross(Vector3.up, flatForward);

            float pitchAngle =
                Mathf.Asin(Mathf.Clamp(transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
            float pitchVelocity =
                Vector3.Dot(rb.angularVelocity, flatRight);

            float error = debugForcedPitchAngle - pitchAngle;
            float correction =
                (error * debugForcedPitchStrength) -
                (pitchVelocity * debugForcedPitchStrength * 0.3f);

            rb.AddTorque(
                flatRight * -correction,
                ForceMode.Acceleration
            );
        }

        /// <summary>
        /// Steering while airborne during a wheelie (see the Wheelie Air
        /// Control header comment for why this exists as its own method
        /// instead of relying on ApplySteering's normal yaw assist, and why
        /// it deliberately does nothing at all when there's no input).
        /// </summary>
        private void ApplyWheelieAirControl()
        {
            // Round 14: while the wheelie is directly driving the pose,
            // steering is handled inside ApplyWheelie by turning the latched
            // yaw instead - applying a yaw TORQUE here as well would just be
            // overwritten by the forced rotation, and any part of it that
            // did survive would show up as exactly the sideways drift the
            // user reported.
            if (WheelieForcingPose) return;
            if (currentWheelieTarget <= 3f) return;

            // Deadzone raised from 0.05 to 0.25 (user report: real keyboard
            // play showed the wheelie "going left sometimes" with no
            // deliberate steering input). Unity's default Input Manager
            // smooths keyboard axes with "gravity" - Input.GetAxis doesn't
            // snap to 0 the instant a key is released, it decays over a
            // few tenths of a second. A synthetic test that sets steerInput
            // exactly (0 or 1) never sees that residue; real play does. A
            // wider deadzone means only a clearly intentional press acts on
            // the bike while airborne, not leftover decay from the last
            // turn before the wheelie started.
            if (Mathf.Abs(steerInput) < 0.25f) return;

            rb.AddTorque(
                Vector3.up * steerInput * wheelieAirSteerTorque,
                ForceMode.Acceleration
            );
        }

        /// <summary>
        /// Keeps the bike from ever falling on its side during normal
        /// driving - the "stand" the user asked for. Rebuilds the rotation
        /// from the current forward direction with world up, which zeroes
        /// roll while leaving yaw and pitch alone, and eases toward it so
        /// it reads as the bike righting itself rather than snapping.
        ///
        /// Kept unconditional (no wheelie-pitch fade) after real testing
        /// showed that fading or removing it broke general driving
        /// stability outright (a plain full-throttle test with no wheelie
        /// involved at all tumbled past 100 degrees of lean within one
        /// second without it) - this really is load-bearing for normal
        /// driving, not just a "nice to have". The wheelie-specific "leans
        /// left" bug (roll growing alongside wheelie pitch) turned out to
        /// live in ApplyStability instead - see that method's own comment.
        ///
        /// Steep/fast wheelie follow-up (user report at the raised 80-
        /// degree, 60deg/s cap: "it goes left sometimes... hold it
        /// straight"): LookRotation(fwd, worldUp) computes the EXACT zero-
        /// roll orientation for the bike's current forward, at any pitch,
        /// and MoveRotation/Slerp toward it is bounded to [0,1] per step -
        /// it geometrically cannot overshoot or oscillate. That makes it
        /// the right place to add extra correction strength for a deep
        /// wheelie, unlike ApplyStability's torque-based approach (tried
        /// first; a real test proved boosting torque gain there overshoots
        /// and made the roll worse, see that method's comment). Extra
        /// strength here is additive and scales with how deep into the
        /// wheelie the bike is, so ordinary near-level driving/cornering is
        /// completely unaffected.
        /// </summary>
        private void ApplyUprightAssist()
        {
            if (uprightAssist <= 0f) return;

            // Stand down during a directly-driven wheelie (see
            // WheelieForcingPose). This one is the strongest attitude
            // constraint in the class - a MoveRotation/Slerp straight
            // toward a levelled pose every step - so leaving it on during
            // an intentional 90-degree pitch would simply cancel the
            // wheelie, which is very likely why the front never lifted in
            // every torque-based round before this one.
            if (WheelieForcingPose) return;

            Vector3 fwd = transform.forward;
            // Degenerate when pointing straight up/down (mid-wheelie); fall
            // back to a horizontal reference so LookRotation stays stable.
            if (Mathf.Abs(Vector3.Dot(fwd, Vector3.up)) > 0.99f)
                fwd = Vector3.ProjectOnPlane(transform.up, Vector3.up).normalized;
            if (fwd.sqrMagnitude < 0.0001f) return;

            Quaternion levelled = Quaternion.LookRotation(fwd, Vector3.up);

            float strength = uprightAssist;
            if (SpeedKmh < 12f) strength *= uprightAssistLowSpeedBoost;

            float wheelie01 =
                maxWheelieAngle > 0.01f
                    ? Mathf.Clamp01(currentWheelieTarget / maxWheelieAngle)
                    : 0f;
            strength += wheelie01 * wheelieRollAssist;

            rb.MoveRotation(Quaternion.Slerp(
                rb.rotation, levelled, Mathf.Clamp01(strength * Time.fixedDeltaTime)));

            // Kill residual roll spin so the correction cannot be fought by
            // leftover angular momentum from a knock or a landing.
            Vector3 av = rb.angularVelocity;
            av -= transform.forward * Vector3.Dot(av, transform.forward);
            rb.angularVelocity = av;
        }

        private void UpdateVisualLean()
        {
            if (visualLeanRoot == null)
                return;

            // Full lean at fullLeanSpeedKmh, not a hardcoded 60. At 60 the bike
            // only reached a third of its lean at ordinary riding speed, which
            // is why the user reported it looking straight through corners
            // even with a non-zero maxVisualLean.
            float speedFactor =
                Mathf.InverseLerp(
                    minLeanSpeedKmh,
                    fullLeanSpeedKmh,
                    SpeedKmh
                );

            // Bug fix (user report/screenshot: the rear wheel visibly comes
            // out of alignment with the body while wheelieing). Root cause:
            // this cosmetic cornering lean rotates VisualLeanRoot - the SAME
            // transform both wheel discs and the body mesh live under -
            // around its own local Z axis, with no awareness of the
            // wheelie's own (much larger, physics-real) pitch already
            // present on the Rigidbody root above it. Composing a local Z
            // "lean" rotation on top of a large real-world pitch is not the
            // same operation as a small cosmetic lean on top of a level
            // bike - the two rotations don't commute, and the result visibly
            // skews the whole visual body away from where the physics
            // (WheelColliders, parented separately under the un-leaned
            // Physics group) actually is. A wheelie doesn't need cosmetic
            // cornering lean anyway - fading it out as the wheelie deepens
            // removes the skew entirely without touching ordinary
            // cornering-while-riding at all.
            float wheelie01 =
                maxWheelieAngle > 0.01f
                    ? Mathf.Clamp01(currentWheelieTarget / maxWheelieAngle)
                    : 0f;

            float targetLean =
                -steerInput *
                maxVisualLean *
                speedFactor *
                (1f - wheelie01);

            currentVisualLean =
                Mathf.Lerp(
                    currentVisualLean,
                    targetLean,
                    leanResponse * Time.deltaTime
                );

            visualLeanRoot.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    currentVisualLean
                );
        }

        public bool IsTipped()
        {
            return Vector3.Dot(
                transform.up,
                Vector3.up
            ) < tippedDotThreshold;
        }

        public void Recover()
        {
            Vector3 euler = transform.eulerAngles;

            transform.rotation =
                Quaternion.Euler(
                    0f,
                    euler.y,
                    0f
                );

            transform.position +=
                Vector3.up * 0.35f;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            currentWheelieTarget = 0f;
            wheelieHeld = false;
        }
    }
}
