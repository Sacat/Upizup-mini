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

            const float panelX = 460f;
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
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            // User: "for the wheelie it goes up too fast... it should be
            // a lot more gradual... i want sliders for the wheelie
            // assist." See SuperMotoWheelieAssist's own header for why
            // this replaces RB_Controller's raw instant torque entirely.
            GUILayout.Label("-- WHEELIE ASSIST (gradual, adapted from your original) --");
            if (_wheelieAssist != null)
            {
                Slider("Rise rate (deg/s)",
                    _wheelieAssist.riseRateDegPerSecond, 5f, 200f, v => _wheelieAssist.riseRateDegPerSecond = v,
                    "How fast a tap registers. Higher = a quick tap responds almost instantly.");
                Slider("Ramp ceiling (deg)",
                    _wheelieAssist.rampCeilingDeg, 5f, 120f, v => _wheelieAssist.rampCeilingDeg = v,
                    "The angle a full, sustained hold ramps toward.");
                Slider("Wheelie torque at full ramp",
                    _wheelieAssist.maxWheelieTorque, 20f, 800f, v => _wheelieAssist.maxWheelieTorque = v,
                    "How hard each press lifts. LOWER = gentler - tap/hold in bursts to build height instead of one press launching it.");
            }

            GUILayout.Space(10f);
            // User: "i want it to be like the original tmax controller
            // when we forced the wheelie to stay straight... apply logic
            // to it to transfer the idea to this system." Deterministic
            // roll=0 correction while wheelieing, backed up by the real
            // outrigger stabilizer (see VehicleSpawnController's own
            // comment on why that's back).
            GUILayout.Label("-- ROLL LOCK (forces the wheelie to stay straight) --");
            if (_wheelieAssist != null)
            {
                Slider("Deadzone (deg)",
                    _wheelieAssist.rollLockDeadzoneDeg, 0f, 20f, v => _wheelieAssist.rollLockDeadzoneDeg = v,
                    "Small leans below this are left alone (normal cornering). Above it, roll snaps to 0 while wheelieing.");
                Slider("Grace period (s)",
                    _wheelieAssist.rollLockGraceSeconds, 0f, 2f, v => _wheelieAssist.rollLockGraceSeconds = v,
                    "How long the correction keeps working after the wheelie visibly ends, so landing doesn't leave it leaned over.");
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
                    _autoLevel.autoLevelForce, 0f, 20f, v => _autoLevel.autoLevelForce = v,
                    "General self-righting strength while riding normally (not the wheelie roll-lock above).");
                Slider("Dot threshold",
                    _autoLevel.dotForAutoLevel, 0f, 1f, v => _autoLevel.dotForAutoLevel = v,
                    "How far it has to lean before self-righting kicks in. Lower = corrects sooner/more sensitively.");
            }

            GUILayout.Space(12f);
            if (GUILayout.Button("PRINT ALL VALUES to Console (survives Stop)"))
            {
                Debug.Log(
                    "MINI-119 STOCK DEMO BIKE TUNING VALUES\n" +
                    (_wheelieAssist != null ?
                        $"  SuperMotoWheelieAssist.riseRateDegPerSecond = {_wheelieAssist.riseRateDegPerSecond:F2}f;\n" +
                        $"  SuperMotoWheelieAssist.rampCeilingDeg = {_wheelieAssist.rampCeilingDeg:F2}f;\n" +
                        $"  SuperMotoWheelieAssist.maxWheelieTorque = {_wheelieAssist.maxWheelieTorque:F2}f;\n" +
                        $"  SuperMotoWheelieAssist.rollLockDeadzoneDeg = {_wheelieAssist.rollLockDeadzoneDeg:F2}f;\n" : "") +
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
