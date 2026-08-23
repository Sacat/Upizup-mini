using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-119 follow-up, user: "i didnt see the screen read out if E was
    /// held or not."
    ///
    /// The earlier live readout was rendered inside Mini119StockDemoBikeTuner's
    /// panel, which is toggleable (T), scrollable, and shares screen space
    /// with a dozen sliders - easy to miss entirely, which is exactly what
    /// happened. This is a dedicated, standalone, ALWAYS-VISIBLE overlay
    /// with its own large text, drawn at the top-centre of the screen with
    /// no toggle and no dependency on the tuner existing at all.
    ///
    /// Its whole purpose is to end the guess-build-report loop: it shows,
    /// in the real running game, whether the E key is even registering and
    /// exactly which condition is blocking a wheelie - the one piece of
    /// information that batch tests in this task provably could not
    /// establish (they set the input field directly and always passed,
    /// while the real game never wheelied).
    /// </summary>
    [DefaultExecutionOrder(2000)]
    public class SuperMotoWheelieHud : MonoBehaviour
    {
        [SerializeField] private KeyCode toggleKey = KeyCode.H;
        private bool _visible = true;
        private SuperMotoWheelieAssist _assist;
        private GUIStyle _style;

        private void Awake() => _assist = GetComponent<SuperMotoWheelieAssist>();

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey)) _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible || _assist == null) return;

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.UpperLeft,
                    fontSize = 16,
                    wordWrap = true,
                    richText = false
                };
                _style.normal.textColor = Color.white;
            }

            // Top-centre, wide, unmissable. Deliberately NOT inside any
            // scroll view or collapsible panel.
            float w = Mathf.Min(760f, Screen.width - 40f);
            float x = (Screen.width - w) * 0.5f;
            GUI.Box(new Rect(x, 8f, w, 110f),
                "WHEELIE DIAGNOSTIC  (H to hide)\n" + _assist.LiveWheelieStatus(), _style);
        }
    }
}
