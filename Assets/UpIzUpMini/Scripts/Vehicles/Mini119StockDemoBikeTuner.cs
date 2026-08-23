using System.Collections.Generic;
using Gadd420;
using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-119 follow-up, user: "this system is almost exactly what i
    /// want. My only issues it crashes too easy... it tends to lean on a
    /// side while riding sometimes so see if the existing controller has
    /// upright assist... also i like the wheelie but it needs upright
    /// assist as well and controls for it like sliders." Efficiency
    /// directive from the same message: "use what is there and works in
    /// the current system and we tweak it."
    ///
    /// The pack's own RB_Controller/AutoLeveling/CrashController already
    /// HAVE everything asked for - real upright assist (AutoLeveling.
    /// autoLevelForce/dotForAutoLevel/antiSpinTorque, already running
    /// every FixedUpdate) and a wheelie-specific safety system
    /// (AutoLeveling.safeWheelies/antiLoopStrength/maxWheelieAngle/
    /// maxAngleMultiplier) - nothing new was invented here, this just
    /// exposes those EXISTING public fields as live sliders, same GUI
    /// pattern as TmaxWheelieTuner (Slider/DirectionalSlider/
    /// ApplyNumericField, PRINT-to-console button that survives Stop).
    /// </summary>
    public class Mini119StockDemoBikeTuner : MonoBehaviour
    {
        [SerializeField] private KeyCode toggleKey = KeyCode.T;
        private bool _visible = true;
        private Vector2 _scrollPos;
        private readonly Dictionary<string, string> _editBuffers = new Dictionary<string, string>();

        private RB_Controller _rb;
        private AutoLeveling _autoLevel;
        private CrashController _crash;

        private void Awake()
        {
            _rb = GetComponent<RB_Controller>();
            _autoLevel = GetComponent<AutoLeveling>();
            _crash = GetComponent<CrashController>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey)) _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible)
            {
                GUI.Label(new Rect(10, 34, 380, 24), "[ T ] show stock bike tuner (crash/upright/wheelie sliders)");
                return;
            }

            const float panelX = 480f;
            const float panelW = 330f;
            float panelH = Mathf.Min(Screen.height - 20f, 560f);

            int prevLabel = GUI.skin.label.fontSize;
            int prevButton = GUI.skin.button.fontSize;
            GUI.skin.label.fontSize = 10;
            GUI.skin.button.fontSize = 10;

            GUILayout.BeginArea(new Rect(panelX, 10, panelW, panelH), GUI.skin.box);
            GUILayout.Label("STOCK BIKE TUNER (T to hide)");
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            // User: "it crashes too easy to put a slider for that."
            GUILayout.Label("-- CRASH SENSITIVITY --");
            if (_crash != null)
            {
                Slider("Deceleration needed to count as a crash (higher = harder to trigger)",
                    _crash.decelerationSpeedForCrash, 1f, 60f, v => _crash.decelerationSpeedForCrash = v);
            }

            GUILayout.Space(8f);
            // User: "it tends to lean on a side while riding sometimes so
            // see if the existing controller has upright assist." It does
            // - AutoLeveling, already running. Exposed here rather than
            // adding a second, competing correction system.
            GUILayout.Label("-- UPRIGHT ASSIST (leaning to a side - AutoLeveling, already built in) --");
            if (_autoLevel != null)
            {
                Slider("Auto-level force (gyro strength)",
                    _autoLevel.autoLevelForce, 0f, 20f, v => _autoLevel.autoLevelForce = v);
                Slider("Dot threshold before correcting (lower = corrects sooner)",
                    _autoLevel.dotForAutoLevel, 0f, 1f, v => _autoLevel.dotForAutoLevel = v);
                Slider("Anti-spin torque (yaw, when nearly sideways in the air)",
                    _autoLevel.antiSpinTorque, 0f, 40f, v => _autoLevel.antiSpinTorque = v);
            }

            GUILayout.Space(8f);
            // User: "i like the wheelie but it needs upright assist as
            // well and controls for it like sliders." AutoLeveling's own
            // safeWheelies system does exactly this - pulls the bike back
            // down once a wheelie exceeds a set angle, same file as above.
            GUILayout.Label("-- WHEELIE UPRIGHT ASSIST (AutoLeveling.safeWheelies) --");
            if (_autoLevel != null)
            {
                _autoLevel.safeWheelies = GUILayout.Toggle(_autoLevel.safeWheelies, "Safe wheelies enabled");
                Slider("Max wheelie angle before assist pulls it down (deg)",
                    _autoLevel.maxWheelieAngle, 5f, 120f, v => _autoLevel.maxWheelieAngle = v);
                Slider("Anti-loop strength (how hard it pulls back down)",
                    _autoLevel.antiLoopStrength, 0f, 60f, v => _autoLevel.antiLoopStrength = v);
                Slider("Max-angle multiplier (extra pull past the max angle)",
                    _autoLevel.maxAngleMultiplier, 1f, 5f, v => _autoLevel.maxAngleMultiplier = v);
            }

            GUILayout.Space(8f);
            GUILayout.Label("-- WHEELIE CONTROL (RB_Controller) --");
            if (_rb != null)
            {
                Slider("Wheelie torque (how hard the front lifts)",
                    _rb.wheelieTorque, 50f, 3000f, v => _rb.wheelieTorque = v);
                Slider("Stoppie torque (rear-up braking wheelie)",
                    _rb.stoppieTorque, 50f, 3000f, v => _rb.stoppieTorque = v);
                Slider("Back-flip torque (in-air rotation)",
                    _rb.backFlipTorque, 0f, 1500f, v => _rb.backFlipTorque = v);
            }

            GUILayout.Space(10f);
            if (GUILayout.Button("PRINT ALL VALUES to Console (survives Stop)"))
            {
                Debug.Log(
                    "MINI-119 STOCK DEMO BIKE TUNING VALUES\n" +
                    (_crash != null ? $"  CrashController.decelerationSpeedForCrash = {_crash.decelerationSpeedForCrash:F2}f;\n" : "") +
                    (_autoLevel != null ?
                        $"  AutoLeveling.autoLevelForce = {_autoLevel.autoLevelForce:F2}f;\n" +
                        $"  AutoLeveling.dotForAutoLevel = {_autoLevel.dotForAutoLevel:F3}f;\n" +
                        $"  AutoLeveling.antiSpinTorque = {_autoLevel.antiSpinTorque:F2}f;\n" +
                        $"  AutoLeveling.safeWheelies = {_autoLevel.safeWheelies};\n" +
                        $"  AutoLeveling.maxWheelieAngle = {_autoLevel.maxWheelieAngle:F2}f;\n" +
                        $"  AutoLeveling.antiLoopStrength = {_autoLevel.antiLoopStrength:F2}f;\n" +
                        $"  AutoLeveling.maxAngleMultiplier = {_autoLevel.maxAngleMultiplier:F2}f;\n" : "") +
                    (_rb != null ?
                        $"  RB_Controller.wheelieTorque = {_rb.wheelieTorque:F2}f;\n" +
                        $"  RB_Controller.stoppieTorque = {_rb.stoppieTorque:F2}f;\n" +
                        $"  RB_Controller.backFlipTorque = {_rb.backFlipTorque:F2}f;\n" : ""));
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            GUI.skin.label.fontSize = prevLabel;
            GUI.skin.button.fontSize = prevButton;
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
