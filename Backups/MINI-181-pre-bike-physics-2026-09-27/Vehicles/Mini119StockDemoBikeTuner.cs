using System.Collections.Generic;
using Gadd420;
using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-119 follow-up. Originally exposed every candidate slider
    /// (crash sensitivity, AutoLeveling upright assist, raw RB_Controller
    /// wheelie torque) while figuring out what was actually needed. User's
    /// latest round: "remove the other sliders in the HUD and make the
    /// font larger so i can see the names. the slider sizes are good." -
    /// trimmed down to what's actually being worked on now (upright
    /// balance + the new gradual wheelie assist + the trike stabilizer),
    /// crash-sensitivity and the superseded raw wheelie-torque sliders
    /// removed (RB_Controller.wheelieTorque is zeroed by
    /// SuperMotoWheelieAssist itself now, see that class), font bumped up
    /// (was 10pt for both label and button - now 15pt label / 13pt
    /// button), slider width/panel widened to match.
    /// </summary>
    public class Mini119StockDemoBikeTuner : MonoBehaviour
    {
        [SerializeField] private KeyCode toggleKey = KeyCode.T;
        private bool _visible = true;
        private Vector2 _scrollPos;
        private readonly Dictionary<string, string> _editBuffers = new Dictionary<string, string>();

        private AutoLeveling _autoLevel;
        private SuperMotoWheelieAssist _wheelieAssist;

        private void Awake()
        {
            _autoLevel = GetComponent<AutoLeveling>();
            _wheelieAssist = GetComponent<SuperMotoWheelieAssist>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey)) _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible)
            {
                GUI.Label(new Rect(10, 34, 420, 26), "[ T ] show bike tuner (wheelie/upright sliders)");
                return;
            }

            // User: "put the HUD to the left so it doesnt obstruct my
            // view" - was centered-right (460), overlapping the riding
            // view straight ahead. Moved to the left edge.
            const float panelX = 10f;
            const float panelW = 400f;
            float panelH = Mathf.Min(Screen.height - 20f, 560f);

            int prevLabel = GUI.skin.label.fontSize;
            int prevButton = GUI.skin.button.fontSize;
            int prevToggle = GUI.skin.toggle.fontSize;
            // User: "make the font larger so i can see the names. the
            // slider sizes are good" - only the text grew, slider widths
            // below are unchanged.
            GUI.skin.label.fontSize = 15;
            GUI.skin.button.fontSize = 13;
            GUI.skin.toggle.fontSize = 15;

            GUILayout.BeginArea(new Rect(panelX, 10, panelW, panelH), GUI.skin.box);
            GUILayout.Label("BIKE TUNER (T to hide)");

            // MINI-119 follow-up, user: "it never worked even at high
            // speeds" / "your testing is the worst it never works." Live
            // truth, on screen, in the real running game - pinned OUTSIDE
            // the scroll view so it's always visible. Shows exactly which
            // condition is blocking a wheelie, and the REAL measured
            // front-wheel lift in metres (the actual physical definition
            // of a wheelie, which every earlier test in this task failed
            // to check).
            if (_wheelieAssist != null)
            {
                if (_statusStyle == null)
                    _statusStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 13, wordWrap = true };
                GUILayout.Box(_wheelieAssist.LiveWheelieStatus(), _statusStyle);
            }

            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            // User: "for the wheelie it goes up too fast... it should be
            // a lot more gradual... i want sliders for the wheelie
            // assist." See SuperMotoWheelieAssist's own header for why
            // this replaces RB_Controller's raw instant torque entirely.
            // User: "set the sliders to 5X positive and negitives" - every
            // range below widened to roughly 5x its previous span in both
            // directions, same convention as the earlier custom-bike
            // tuner's own "increase these sliders by 5" round.
            // User: "use the wheelie method of the original tmax." Same
            // kinematic mechanism as your original bike now - a rate-
            // limited target angle, direct-set each step with no roll
            // term at all, so there's no separate "roll lock" section any
            // more: the wheelie itself is mathematically incapable of
            // leaning while active.
            GUILayout.Label("-- WHEELIE (kinematic, same method as your original) --");
            if (_wheelieAssist != null)
            {
                Slider("Rise rate (deg/s)",
                    _wheelieAssist.riseRateDegPerSecond, 5f, 400f, v => _wheelieAssist.riseRateDegPerSecond = v,
                    "How fast the wheelie angle climbs while held, and falls back to 0 when released.");
                Slider("Max angle (deg)",
                    _wheelieAssist.rampCeilingDeg, 5f, 170f, v => _wheelieAssist.rampCeilingDeg = v,
                    "The angle a full, sustained hold reaches. 90 = straight up.");
                Slider("Min speed to start (km/h)",
                    _wheelieAssist.wheelieMinSpeedKmh, 0f, 100f, v => _wheelieAssist.wheelieMinSpeedKmh = v,
                    "Below this speed, starting a NEW wheelie is blocked (sustaining one already up isn't affected).");
                Slider("Max speed to start (km/h)",
                    _wheelieAssist.wheelieMaxSpeedKmh, 10f, 300f, v => _wheelieAssist.wheelieMaxSpeedKmh = v,
                    "Above this speed, starting a NEW wheelie is blocked.");
                Slider("Air steer (deg/s)",
                    _wheelieAssist.wheelieAirSteerTorque, 0f, 100f, v => _wheelieAssist.wheelieAirSteerTorque = v,
                    "How fast steering turns the bike while the front is up.");
            }

            GUILayout.Space(10f);
            // User: "i need something that can put up the bike to
            // straight equal on both side just like when i press F it
            // respawns straight." Same recipe as F itself, automated -
            // runs even if the bike is flagged crashed, unlike everything
            // above.
            GUILayout.Label("-- AUTO-RECOVER (like pressing F, automatic) --");
            if (_wheelieAssist != null)
            {
                Slider("Fallen-over limit (deg)",
                    _wheelieAssist.autoRecoverRollLimitDeg, 20f, 170f, v => _wheelieAssist.autoRecoverRollLimitDeg = v,
                    "Roll past this counts as properly fallen over, not just leaning.");
                Slider("Sustain time (s)",
                    _wheelieAssist.autoRecoverSustainSeconds, 0f, 3f, v => _wheelieAssist.autoRecoverSustainSeconds = v,
                    "How long it has to stay fallen over before auto-recovery snaps it upright.");
            }

            GUILayout.Space(10f);
            // User: "keeping the bike balanced and upright while riding" -
            // AutoLeveling, already built into the pack (autoLevelForce
            // etc.) - a real precedence bug in it (see AutoLeveling.cs's
            // own fix comment) was the actual cause of the permanent
            // side-lean, now fixed at the source; these sliders remain for
            // feel-tuning on top of that fix.
            GUILayout.Label("-- UPRIGHT BALANCE (AutoLeveling, bug-fixed) --");
            if (_autoLevel != null)
            {
                Slider("Auto-level force",
                    _autoLevel.autoLevelForce, 0f, 100f, v => _autoLevel.autoLevelForce = v,
                    "General self-righting strength while riding normally (not the wheelie roll-lock above).");
                Slider("Dot threshold",
                    _autoLevel.dotForAutoLevel, 0f, 1f, v => _autoLevel.dotForAutoLevel = v,
                    "How far it has to lean before self-righting kicks in. Lower = corrects sooner/more sensitively.");
            }

            GUILayout.Space(10f);
            // User: "i need more hill assist because it use to climb the
            // hill better than that."
            GUILayout.Label("-- DRIVE / HILL CLIMB (RB_Controller engine power) --");
            if (_wheelieAssist != null)
            {
                Slider("Low-gear torque",
                    _wheelieAssist.firstGearTorque, 50f, 3000f, v => _wheelieAssist.firstGearTorque = v,
                    "Starting/low-speed power. Raise if hills feel weak.");
                Slider("Top-gear torque",
                    _wheelieAssist.topGearTorque, 50f, 3000f, v => _wheelieAssist.topGearTorque = v,
                    "High-speed power.");
            }

            GUILayout.Space(10f);
            // User: "it crashes a bit too easy... not sure if you can
            // setup the slider for that." Also fixed the ragdoll-collider
            // timing issue at the source - see SuperMotoWheelieAssist.
            // Awake's own comment.
            GUILayout.Label("-- CRASH SENSITIVITY --");
            if (_wheelieAssist != null)
            {
                Slider("Deceleration needed to count as a crash",
                    _wheelieAssist.crashDecelerationThreshold, 1f, 200f, v => _wheelieAssist.crashDecelerationThreshold = v,
                    "HIGHER = harder to trigger by accident (a bump, hard brake, landing).");
            }

            GUILayout.Space(12f);
            if (GUILayout.Button("PRINT ALL VALUES to Console (survives Stop)"))
            {
                Debug.Log(
                    "MINI-119 STOCK DEMO BIKE TUNING VALUES\n" +
                    (_wheelieAssist != null ?
                        $"  SuperMotoWheelieAssist.riseRateDegPerSecond = {_wheelieAssist.riseRateDegPerSecond:F2}f;\n" +
                        $"  SuperMotoWheelieAssist.rampCeilingDeg = {_wheelieAssist.rampCeilingDeg:F2}f;\n" +
                        $"  SuperMotoWheelieAssist.wheelieMinSpeedKmh = {_wheelieAssist.wheelieMinSpeedKmh:F2}f;\n" +
                        $"  SuperMotoWheelieAssist.wheelieMaxSpeedKmh = {_wheelieAssist.wheelieMaxSpeedKmh:F2}f;\n" +
                        $"  SuperMotoWheelieAssist.wheelieAirSteerTorque = {_wheelieAssist.wheelieAirSteerTorque:F2}f;\n" +
                        $"  SuperMotoWheelieAssist.autoRecoverRollLimitDeg = {_wheelieAssist.autoRecoverRollLimitDeg:F2}f;\n" +
                        $"  SuperMotoWheelieAssist.autoRecoverSustainSeconds = {_wheelieAssist.autoRecoverSustainSeconds:F2}f;\n" +
                        $"  SuperMotoWheelieAssist.firstGearTorque = {_wheelieAssist.firstGearTorque:F2}f;\n" +
                        $"  SuperMotoWheelieAssist.topGearTorque = {_wheelieAssist.topGearTorque:F2}f;\n" +
                        $"  SuperMotoWheelieAssist.crashDecelerationThreshold = {_wheelieAssist.crashDecelerationThreshold:F2}f;\n" : "") +
                    (_autoLevel != null ?
                        $"  AutoLeveling.autoLevelForce = {_autoLevel.autoLevelForce:F2}f;\n" +
                        $"  AutoLeveling.dotForAutoLevel = {_autoLevel.dotForAutoLevel:F3}f;\n" : ""));
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            GUI.skin.label.fontSize = prevLabel;
            GUI.skin.button.fontSize = prevButton;
            GUI.skin.toggle.fontSize = prevToggle;
        }

        private GUIStyle _hintStyle;
        private GUIStyle _statusStyle;

        /// <summary>User: "next to the sliders if you make new ones based
        /// on what i tell you, put like brief instructions for the
        /// sliders." Optional one-line plain-language caption rendered
        /// under the slider itself - not a hover tooltip (this is an
        /// in-game OnGUI panel in a built player, nothing to hover), so it
        /// has to just be on-screen text.</summary>
        private void Slider(string label, float value, float min, float max, System.Action<float> apply, string hint = null)
        {
            GUILayout.Label($"{label}: {value:F2}");
            GUILayout.BeginHorizontal();
            float next = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(190));
            bool changed = !Mathf.Approximately(next, value);
            if (changed) apply(next);
            else ApplyNumericField(label, value, min, max, apply);
            GUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(hint))
            {
                if (_hintStyle == null)
                    _hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Italic, wordWrap = true };
                GUILayout.Label(hint, _hintStyle);
            }
        }

        private void ApplyNumericField(string fieldKey, float value, float min, float max, System.Action<float> apply)
        {
            string controlName = "stocktuner_" + fieldKey;
            bool focused = GUI.GetNameOfFocusedControl() == controlName;

            if (!focused || !_editBuffers.ContainsKey(fieldKey))
                _editBuffers[fieldKey] = value.ToString("F2");

            GUI.SetNextControlName(controlName);
            string text = GUILayout.TextField(_editBuffers[fieldKey], GUILayout.Width(60));
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
