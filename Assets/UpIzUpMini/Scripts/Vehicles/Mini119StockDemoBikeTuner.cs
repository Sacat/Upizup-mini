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
        private SuperMotoTrikeStabilizer _trikeStab;
        private SuperMotoWheelieAssist _wheelieAssist;

        private void Awake()
        {
            _autoLevel = GetComponent<AutoLeveling>();
            _trikeStab = GetComponent<SuperMotoTrikeStabilizer>();
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
                Slider("Rise rate (deg/s) - LOWER = more gradual",
                    _wheelieAssist.riseRateDegPerSecond, 5f, 200f, v => _wheelieAssist.riseRateDegPerSecond = v);
                Slider("Ramp ceiling (deg)",
                    _wheelieAssist.rampCeilingDeg, 5f, 120f, v => _wheelieAssist.rampCeilingDeg = v);
                Slider("Wheelie torque at full ramp (stock RB_Controller field)",
                    _wheelieAssist.maxWheelieTorque, 50f, 2000f, v => _wheelieAssist.maxWheelieTorque = v);
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
                Slider("Auto-level force (gyro strength)",
                    _autoLevel.autoLevelForce, 0f, 20f, v => _autoLevel.autoLevelForce = v);
                Slider("Dot threshold before correcting (lower = corrects sooner)",
                    _autoLevel.dotForAutoLevel, 0f, 1f, v => _autoLevel.dotForAutoLevel = v);
            }

            GUILayout.Space(10f);
            // User: "there should be an assist like two invisible
            // colliders like for the side like in my original to keep it
            // up" - ported outrigger stabilizer, see
            // SuperMotoTrikeStabilizer's own header.
            GUILayout.Label("-- TRIKE STABILIZER (outriggers active on wheelie) --");
            if (_trikeStab != null)
            {
                Slider("Outrigger side offset (m)",
                    _trikeStab.outriggerSideOffset, 0.1f, 1f, v => _trikeStab.outriggerSideOffset = v);
                Slider("Debounce before non-wheelie air counts as 'up' (s)",
                    _trikeStab.debounceSeconds, 0.02f, 1f, v => _trikeStab.debounceSeconds = v);
                Slider("Spring rate",
                    _trikeStab.springRate, 500f, 15000f, v => _trikeStab.springRate = v);
                Slider("Damping",
                    _trikeStab.damping, 0f, 1500f, v => _trikeStab.damping = v);
            }

            GUILayout.Space(12f);
            if (GUILayout.Button("PRINT ALL VALUES to Console (survives Stop)"))
            {
                Debug.Log(
                    "MINI-119 STOCK DEMO BIKE TUNING VALUES\n" +
                    (_wheelieAssist != null ?
                        $"  SuperMotoWheelieAssist.riseRateDegPerSecond = {_wheelieAssist.riseRateDegPerSecond:F2}f;\n" +
                        $"  SuperMotoWheelieAssist.rampCeilingDeg = {_wheelieAssist.rampCeilingDeg:F2}f;\n" +
                        $"  SuperMotoWheelieAssist.maxWheelieTorque = {_wheelieAssist.maxWheelieTorque:F2}f;\n" : "") +
                    (_autoLevel != null ?
                        $"  AutoLeveling.autoLevelForce = {_autoLevel.autoLevelForce:F2}f;\n" +
                        $"  AutoLeveling.dotForAutoLevel = {_autoLevel.dotForAutoLevel:F3}f;\n" : "") +
                    (_trikeStab != null ?
                        $"  SuperMotoTrikeStabilizer.outriggerSideOffset = {_trikeStab.outriggerSideOffset:F3}f;\n" +
                        $"  SuperMotoTrikeStabilizer.debounceSeconds = {_trikeStab.debounceSeconds:F3}f;\n" +
                        $"  SuperMotoTrikeStabilizer.springRate = {_trikeStab.springRate:F2}f;\n" +
                        $"  SuperMotoTrikeStabilizer.damping = {_trikeStab.damping:F2}f;\n" : ""));
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            GUI.skin.label.fontSize = prevLabel;
            GUI.skin.button.fontSize = prevButton;
            GUI.skin.toggle.fontSize = prevToggle;
        }

        private void Slider(string label, float value, float min, float max, System.Action<float> apply)
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
