using System.Collections.Generic;
using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-065 round 7 (sliders), round 8 (weight bias), round 9
    /// (scrollable layout), round 10 (this pass - pinned live status,
    /// debug force override, tunable lift/recover authority, precise
    /// numeric entry). Test-scene-only (added to the TMAX_Physics_Test
    /// bike alongside TmaxTestInput - see Mini065TmaxPhysicsTest.
    /// BuildTestScene - never touches the real TMAX_560.prefab used in
    /// GrandBayProof) live tuning panel for the wheelie/stability/weight
    /// values.
    ///
    /// Direct response to the user's own diagnosis of this whole task's
    /// weak point: "i am not sure how your tests passes but your tests are
    /// flawed... put the wheelie controls as sliders into unity and i will
    /// adjust them to get the wheelie."
    ///
    /// Round 10 additions, each a direct response to a specific report:
    ///  - "the bike does not wheelie even if I play in all the sliders...
    ///    the wheelie target degrees numbers says it goes up at 80 but it
    ///    doesnt even lift the ground" - the pinned status box (always
    ///    visible, never requires scrolling) now shows the TARGET the
    ///    controller is chasing side by side with the REAL measured pitch
    ///    and a live ELIGIBLE yes/no with the actual reason, so "target
    ///    says 80" (a SETTING, always shows whatever it's set to) can never
    ///    again be mistaken for "the bike is actually at 80 degrees" (a
    ///    LIVE reading).
    ///  - "maybe we should have invisible holders to bring it up and hold
    ///    it upright that can be adjusted" - added the DEBUG FORCE section,
    ///    which drives TmaxBikeController.ApplyDebugForcedPitch: a direct
    ///    override that ignores every gate the normal wheelie has (speed
    ///    window, brake, ground, eligibility) and just forces the bike
    ///    toward a chosen angle at a chosen strength.
    ///  - "let the bike flip over if it has to i will fix it with the
    ///    slider" - the wheelie's upward lift authority (previously a
    ///    hardcoded, un-sliderable constant - very plausibly the actual
    ///    reason nothing visibly lifted no matter what else was tuned) is
    ///    now itself a slider (Lift authority, under WHEELIE), with no
    ///    ceiling silently capping it out of the user's reach.
    ///  - "can we make it more so i can be more specific with it" - every
    ///    slider now has an adjacent numeric field for exact value entry
    ///    (type a number, press Enter) instead of drag-only precision.
    ///  - "make sure i can see the sliders in the ui" - the panel opens
    ///    VISIBLE by default (T only hides it, never required to reveal
    ///    it), sits in front of everything else in the Game view, and the
    ///    most likely things the user actually needs (debug force, weight
    ///    bias, wheelie lift authority) are within the first screenful,
    ///    before any scrolling.
    ///
    /// Round 12 - "this is stressing me can we just have an invisible
    /// hydraulic to bring the bike up to make it appear like it is
    /// wheelieing. that sounds easy." Simplified the WHEELIE section from
    /// round 11's multi-part system (rear torque boost + separate lift/
    /// recover authority) down to ONE strength slider - "Wheelie strength
    /// (the hydraulic)" - now the first thing in the WHEELIE section, with
    /// DEBUG FORCE demoted to a last-resort section at the bottom since the
    /// normal wheelie now works the same simple way it does.
    ///
    /// Every slider here writes straight into the matching
    /// TmaxBikeController property, live, every frame - so a mid-air
    /// change takes effect immediately and can be felt on the very next
    /// wheelie. Values are NOT saved back into the prefab automatically;
    /// once a set of numbers feels right, report them back and they'll be
    /// baked into TmaxBikeController's own serialized defaults.
    /// </summary>
    [RequireComponent(typeof(TmaxBikeController))]
    public class TmaxWheelieTuner : MonoBehaviour
    {
        [SerializeField] private KeyCode toggleKey = KeyCode.T;
        private bool visible = true;
        private Vector2 scrollPos;
        private TmaxBikeController _bike;
        private readonly Dictionary<string, string> _editBuffers = new Dictionary<string, string>();

        private void Awake()
        {
            _bike = GetComponent<TmaxBikeController>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey)) visible = !visible;
        }

        private void OnGUI()
        {
            if (!visible)
            {
                // y=34, not 10: TmaxTestOverlay's own hidden-hint sits at
                // y=10 and the two were drawing on top of each other into an
                // unreadable smear (visible in the user's Play Mode
                // screenshots). Kept as separate stacked lines rather than
                // merged, since the two overlays toggle independently.
                GUI.Label(new Rect(10, 34, 340, 24), "[ T ] show wheelie tuner (weight/wheelie/stability sliders)");
                return;
            }

            const float panelX = 480f;
            const float panelW = 330f;
            // Taller than before so the rider-seating block (which is what is
            // actively being tuned) fits without scrolling - the user could
            // not reach the wheelie offset sliders at the old height.
            float panelH = Mathf.Min(Screen.height - 20f, 620f);

            // Shrink every label and button in the panel for this frame, then put


            // the skin back - the user asked for the UI smaller ("it doesnt have to


            // be so big"). Restored in a finally-style pair below so the rest of the


            // game.s IMGUI is not left with a tiny font.


            int prevLabel = GUI.skin.label.fontSize;


            int prevButton = GUI.skin.button.fontSize;


            GUI.skin.label.fontSize = 10;


            GUI.skin.button.fontSize = 10;



            GUILayout.BeginArea(new Rect(panelX, 10, panelW, panelH), GUI.skin.box);
            GUILayout.Label("WHEELIE TUNER (T to hide)");

            // Pinned status - deliberately OUTSIDE the scroll view, so it is
            // always on screen no matter where the user has scrolled to.
            // This is the direct fix for "target says 80 but it doesn't
            // lift" being a settings-vs-live-reading mix-up: TARGET and
            // REAL PITCH are shown side by side here, live, every frame.
            string eligibleReason;
            if (_bike.WheelieEligible) eligibleReason = "yes";
            else if (_bike.SpeedKmh < 1f) eligibleReason = "no - not moving";
            else eligibleReason = "no - check speed window / braking / grounded";

            var statusStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 10 };
            string status =
                // "R held", not "E" - the wheelie key moved to R when E became
                // look-right (see BikeInteractable). The stale label was
                // spotted while driving the editor directly and is exactly the
                // sort of thing that makes a readout untrustworthy.
                $"speed {_bike.SpeedKmh:F1} km/h   |   R held: {(_bike.IsWheelieHeld ? "YES" : "no")}   |   eligible: {eligibleReason}\n" +
                $"TARGET (setting the controller is chasing): {_bike.WheelieAngle:F1} deg\n" +
                $"REAL PITCH (actual physics): {_bike.CurrentPitchAngle:F1} deg   |   REAL ROLL: {_bike.CurrentRollAngle:F1} deg\n" +
                $"FRONT WHEEL: {(_bike.IsFrontWheelGrounded ? "on the ground" : "OFF THE GROUND - genuinely wheelieing")}";
            GUILayout.Box(status, statusStyle);

            scrollPos = GUILayout.BeginScrollView(scrollPos);

            // Round 12 - "this is stressing me can we just have an
            // invisible hydraulic to bring the bike up... that sounds
            // easy." Simplified from round 11's multi-slider system (rear
            // torque boost + separate lift/recover authority clamps) down
            // to ONE strength number that just holds the bike at its
            // target angle - this is now the FIRST, most important slider
            // in the whole panel, not buried under alternatives.
            GUILayout.Label("-- WHEELIE (the hydraulic) --");
            Slider("Wheelie strength (the hydraulic - raise this first if it won't lift)",
                _bike.WheelieHydraulicStrength, 1f, 400f, v => _bike.WheelieHydraulicStrength = v);
            // Range goes to a full 360 per "make it go all the way to 360"
            // and "dont put caps on the wheel height yet, i will put the
            // caps with the slider" - the wheelie is directly driven now,
            // so any angle here is reachable, not just physically plausible
            // ones. 90 = front straight up; 180 = fully inverted; 360 = a
            // complete loop.
            Slider("Max wheelie angle (deg) - a SETTING, not live progress",
                _bike.MaxWheelieAngle, 0f, 360f, v => _bike.MaxWheelieAngle = v);
            Slider("Rise rate (deg/s)",
                _bike.WheelieRiseRate, 5f, 750f, v => _bike.WheelieRiseRate = v);
            Slider("Min speed to start (km/h)",
                _bike.WheelieMinSpeedKmh, 0f, 200f, v => _bike.WheelieMinSpeedKmh = v);
            Slider("Max speed to start (km/h)",
                _bike.WheelieMaxSpeedKmh, 10f, 600f, v => _bike.WheelieMaxSpeedKmh = v);
            Slider("Air steer torque",
                _bike.WheelieAirSteerTorque, 0f, 200f, v => _bike.WheelieAirSteerTorque = v);
            Slider("Wheelie roll assist",
                _bike.WheelieRollAssist, 0f, 300f, v => _bike.WheelieRollAssist = v);

            // Rider seating keyframes - first, because this is what is
            // actively being dialled in. Only shown when someone is actually
            // mounted, since there is nothing to place otherwise.
            var rider = Object.FindFirstObjectByType<VehicleRider>();
            if (rider != null && rider.IsMounted)
            {
                // Every axis is a two-ended DirectionalSlider so it is obvious
                // each one moves the rider BOTH ways - the user asked
                // specifically for "up down left right and forward backward".
                GUILayout.Label($"== RIDER SEATING - blend {rider.PoseBlend:F2} (0=seated, 1=wheelie) ==");

                GUILayout.Label("-- SEATED pose (normal riding) --");
                DirectionalSlider("Back", "Fwd", rider.SeatedForward, -2.5f, 3.5f, v => rider.SeatedForward = v);
                DirectionalSlider("Down", "Up", rider.SeatedUp, -2f, 2.5f, v => rider.SeatedUp = v);
                DirectionalSlider("Left", "Right", rider.SeatedSide, -1.75f, 1.75f, v => rider.SeatedSide = v);
                Slider("  seated PITCH (deg)", rider.SeatedPitch, -225f, 225f, v => rider.SeatedPitch = v);

                GUILayout.Label("-- WHEELIE pose (front up) --");
                DirectionalSlider("Back", "Fwd", rider.WheelieForward, -2.5f, 8f, v => rider.WheelieForward = v);
                DirectionalSlider("Down", "Up", rider.WheelieUp, -2f, 3.5f, v => rider.WheelieUp = v);
                DirectionalSlider("Left", "Right", rider.WheelieSide, -1.75f, 1.75f, v => rider.WheelieSide = v);
                // How fast the rider shifts between the seated and wheelie

                // keyframes. Higher = slower, gentler weight shift; the user

                // reported "the character rise rate goes up too fast".

                // How fast the character ANGLES into the wheelie pose - the
                // crossfade into the clip itself. Distinct from the rise time
                // below (which moves his POSITION between keyframes) and from
                // the bike's own "Rise rate" under WHEELIE.
                var ra = rider.GetComponent<BikeRiderAnimation>();
                if (ra != null)
                {
                    // The one that actually decides how far back he ends up.
                    // The authored wheelie clip is bolt upright, so at 1.0 it
                    // leans him back ~61deg on its own no matter what the bike
                    // is doing; lower values hand that authority back to the
                    // bike's live angle. Start around 0.4 and creep up.
                    Slider("  WHEELIE pose blend (0=ride pose)", ra.WheelieClipWeight, 0f, 1f, v => ra.WheelieClipWeight = v);
                    Slider("  WHEELIE anim speed (s, higher=slower)", ra.WheelieBlendSeconds, 0.02f, 3f, v => ra.WheelieBlendSeconds = v);
                    Slider("  LEAN anim speed (s, higher=slower)", ra.LeanBlendSeconds, 0.02f, 3f, v => ra.LeanBlendSeconds = v);
                }
                // How far the rider tracks the bike's LIVE wheelie angle, so
                // he rises and drops in sync with the throttle rather than
                // just snapping between two end poses.
                var biR = GetComponent<BikeInteractable>();
                if (biR != null)
                    Slider("  CHARACTER follows wheelie (1=sync)", biR.WheelieFollowAmount, 0f, 2f, v => biR.WheelieFollowAmount = v);
                // 1 = the rider's ANGLE is a direct function of the bike's live
                // wheelie angle, so throttle changes move him in the same
                // instant. Below 1 he eases on a timer instead, which always
                // trails the bike.
                Slider("  ANGLE sync w/ bike (1=locked)", rider.WheelieAngleSync, 0f, 1f, v => rider.WheelieAngleSync = v);
                Slider("  CHARACTER rise time (s, higher=slower)", rider.PoseBlendSeconds, 0.02f, 3f, v => rider.PoseBlendSeconds = v);

                Slider("  wheelie PITCH (deg)", rider.WheeliePitch, -225f, 350f, v => rider.WheeliePitch = v);
                // Console output SURVIVES stopping Play Mode, unlike the live
                // slider values themselves - which are lost the moment Play
                // ends or a script recompiles. Learned the hard way: a set of
                // hand-tuned values was wiped by an unrelated recompile.
                // Press this before stopping and the numbers are recoverable
                // from the Console, ready to paste in as defaults.
                if (GUILayout.Button("PRINT these values to Console (survives Stop)"))
                {
                    Debug.Log(
                        "MINI-066 RIDER SEATING VALUES\n" +
                        $"  seatedOffset  = new Vector3({rider.SeatedSide:F3}f, {rider.SeatedUp:F3}f, {rider.SeatedForward:F3}f);\n" +
                        $"  seatedPitch   = {rider.SeatedPitch:F1}f;\n" +
                        $"  wheelieOffset = new Vector3({rider.WheelieSide:F3}f, {rider.WheelieUp:F3}f, {rider.WheelieForward:F3}f);\n" +
                        $"  wheeliePitch  = {rider.WheeliePitch:F1}f;\n" +
                        (ra != null
                            ? $"  wheelieClipWeight = {ra.WheelieClipWeight:F2}f;   // BikeRiderAnimation"
                            : ""));
                }

                GUILayout.Space(10f);
            }



            // Bike body lean. Was the one part of cornering with no slider at
            // all, and its speed gate was a hardcoded 60km/h that made the
            // bike read as staying straight at ordinary riding speed - so it
            // gets exposed here rather than needing another rebuild to try a
            // value.
            GUILayout.Label("-- BIKE LEAN (cornering) --");
            Slider("  max bike lean (deg)", _bike.MaxVisualLean, 0f, 275f, v => _bike.MaxVisualLean = v);
            Slider("  full lean AT speed (km/h)", _bike.FullLeanSpeedKmh, 3f, 400f, v => _bike.FullLeanSpeedKmh = v);
            Slider("  lean starts ABOVE (km/h)", _bike.MinLeanSpeedKmh, 0f, 150f, v => _bike.MinLeanSpeedKmh = v);
            var bi = GetComponent<BikeInteractable>();
            if (bi != null)
                Slider("  rider follows lean (1=locked)", bi.RiderLeanFollow, 0f, 6f, v => bi.RiderLeanFollow = v);

            if (bi != null)

                Slider("  CHARACTER extra lean (deg)", bi.RiderExtraLean, -125f, 125f, v => bi.RiderExtraLean = v);
            GUILayout.Space(8f);
            GUILayout.Label("-- WEIGHT BIAS (metres from centre) --");
            DirectionalSlider("Front", "Back",
                -_bike.CenterOfMassOffsetForward, -2.5f, 2.5f,
                v => _bike.CenterOfMassOffsetForward = -v);
            DirectionalSlider("Left", "Right",
                _bike.CenterOfMassOffsetRight, -1.5f, 1.5f,
                v => _bike.CenterOfMassOffsetRight = v);

            GUILayout.Space(10f);
            // MINI-119 follow-up, user: "sliders as not make it spin when
            // hitting ledge or bump or hill and then sliders for keep the
            // bike down like a gravity slider for when it leaves the
            // ground. the sliders should [be] wide so i can drastically
            // reduce the spinning and gravity." Ranges deliberately go
            // well past the shipped defaults on both ends - low enough on
            // yaw spin threshold/high enough on damping to all but kill
            // spin entirely, and 0 on air gravity to turn it off outright.
            GUILayout.Label("-- ANTI-SPIN (hedge/ledge/bump hits) --");
            Slider("Yaw spin threshold (deg/s) - LOWER = assist kicks in sooner",
                _bike.YawSpinThreshold, 0f, 800f, v => _bike.YawSpinThreshold = v);
            Slider("Yaw spin damping - HIGHER = kills excess spin faster",
                _bike.YawSpinDamping, 0f, 50f, v => _bike.YawSpinDamping = v);

            GUILayout.Space(8f);
            GUILayout.Label("-- HILL / LEDGE CLIMB ASSIST --");
            Slider("Hill climb assist strength",
                _bike.HillClimbAssist, 0f, 60f, v => _bike.HillClimbAssist = v);
            Slider("Full-strength slope angle (deg)",
                _bike.HillClimbMaxSlopeDeg, 1f, 89f, v => _bike.HillClimbMaxSlopeDeg = v);

            GUILayout.Space(8f);
            GUILayout.Label("-- AIR GRAVITY (pulls the bike back down once airborne) --");
            Slider("Extra air gravity (0 = off)",
                _bike.ExtraAirGravity, 0f, 500f, v => _bike.ExtraAirGravity = v);
            Slider("Airborne grace before it kicks in (s)",
                _bike.AirborneGraceSeconds, 0f, 2f, v => _bike.AirborneGraceSeconds = v);
            Slider("Ramp-up time to full strength (s)",
                _bike.AirGravityRampSeconds, 0f, 3f, v => _bike.AirGravityRampSeconds = v);

            GUILayout.Space(10f);
            GUILayout.Label("-- DEBUG FORCE (last-resort override, bypasses everything) --");
            Slider("Force pitch to (deg, 90=max representable)",
                _bike.DebugForcedPitchAngle, -450f, 450f, v => _bike.DebugForcedPitchAngle = v);
            Slider("Force strength (0 = off)",
                _bike.DebugForcedPitchStrength, 0f, 300f, v => _bike.DebugForcedPitchStrength = v);

            GUILayout.Space(10f);
            GUILayout.Label("-- DRIVE --");

            Slider("  top speed (km/h)", _bike.MaxSpeedKmh, 10f, 1000f, v => _bike.MaxSpeedKmh = v);

            Slider("  REVERSE speed (km/h)", _bike.MaxReverseSpeedKmh, 1f, 300f, v => _bike.MaxReverseSpeedKmh = v);

            Slider("  motor torque", _bike.MotorTorque, 25f, 4500f, v => _bike.MotorTorque = v);

            GUILayout.Space(8f);


            GUILayout.Label("-- STABILITY --");
            Slider("Upright assist",
                _bike.UprightAssist, 0f, 200f, v => _bike.UprightAssist = v);
            Slider("Upright low-speed boost",
                _bike.UprightAssistLowSpeedBoost, 0.5f, 30f, v => _bike.UprightAssistLowSpeedBoost = v);
            Slider("Base upright strength",
                _bike.UprightStrength, 0f, 150f, v => _bike.UprightStrength = v);
            Slider("Upright damping",
                _bike.UprightDamping, 0f, 75f, v => _bike.UprightDamping = v);
            Slider("Low-speed extra stability",
                _bike.LowSpeedExtraStability, 0f, 100f, v => _bike.LowSpeedExtraStability = v);

            GUILayout.Space(10f);
            GUILayout.EndScrollView();
            GUILayout.EndArea();


            GUI.skin.label.fontSize = prevLabel;

            GUI.skin.button.fontSize = prevButton;
        }

        /// <summary>Slider with an adjacent numeric field for exact-value
        /// entry (type a number, press Enter) - per the user's "can we
        /// make it more so i can be more specific with it". The text field
        /// only overwrites its own live value while NOT focused, so typing
        /// isn't fought/reset every frame by the physics value it's
        /// editing.</summary>
        private void Slider(
            string label, float value, float min, float max,
            System.Action<float> apply)
        {
            GUILayout.Label($"{label}: {value:F2}");
            GUILayout.BeginHorizontal();
            float next = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(190));
            if (!Mathf.Approximately(next, value))
            {
                apply(next);
                GUILayout.EndHorizontal();
                return;
            }
            ApplyNumericField(label, value, min, max, apply);
            GUILayout.EndHorizontal();
        }

        /// <summary>Same as Slider, but labelled with the two named
        /// directions at its own endpoints (e.g. "Front" ... "Back")
        /// instead of a single centred label - for the weight-bias sliders,
        /// so dragging toward a name means exactly what it says.</summary>
        private void DirectionalSlider(
            string negLabel, string posLabel, float value, float min, float max,
            System.Action<float> apply)
        {
            GUILayout.Label($"{negLabel,-8} {value:+0.00;-0.00;0.00}m {posLabel,8}");
            GUILayout.BeginHorizontal();
            float next = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(190));
            if (!Mathf.Approximately(next, value))
            {
                apply(next);
                GUILayout.EndHorizontal();
                return;
            }
            ApplyNumericField(negLabel + posLabel, value, min, max, apply);
            GUILayout.EndHorizontal();
        }

        private void ApplyNumericField(
            string fieldKey, float value, float min, float max, System.Action<float> apply)
        {
            string controlName = "tuner_" + fieldKey;
            bool focused = GUI.GetNameOfFocusedControl() == controlName;

            if (!focused || !_editBuffers.ContainsKey(fieldKey))
                _editBuffers[fieldKey] = value.ToString("F2");

            GUI.SetNextControlName(controlName);
            string text = GUILayout.TextField(_editBuffers[fieldKey], GUILayout.Width(52));
            _editBuffers[fieldKey] = text;

            bool commit =
                focused &&
                Event.current.isKey &&
                (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);

            if (commit && float.TryParse(text, out float parsed))
            {
                apply(Mathf.Clamp(parsed, min, max));
                GUI.FocusControl(null);
            }
        }
    }
}
