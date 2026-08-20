using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-066, TMAX_Physics_Test scene only. Seats the test rider on the
    /// bike the moment Play starts, so the riding animations can be judged
    /// immediately instead of having to walk over and press F first - and,
    /// more importantly, so this isolated scene can be used to test the rider
    /// at all without the full game's purchase/dealer flow.
    ///
    /// Not part of the real game: BikeInteractable's normal [F] prompt is how
    /// a player actually gets on. This just fires that same code path once at
    /// startup, so what is being tested is the real mounting logic rather
    /// than a special-case test path that could pass while the real one is
    /// broken.
    /// </summary>
    [RequireComponent(typeof(BikeInteractable))]
    public class BikeTestAutoMount : MonoBehaviour
    {
        [SerializeField] private GameObject rider;
        [Tooltip("Small delay so every Awake/Start has run (Animator ready, seats registered) before mounting.")]
        [SerializeField] private float delaySeconds = 0.25f;

        private BikeInteractable _bike;
        private float _t;
        private bool _done;

        private void Awake() => _bike = GetComponent<BikeInteractable>();

        private void Update()
        {
            TmaxTestOverlay.Poll();

            if (_done || rider == null || _bike == null) return;

            _t += Time.deltaTime;
            if (_t < delaySeconds) return;

            _done = true;
            _bike.Interact(rider);
        }

        private void OnGUI()
        {
            if (!TmaxTestOverlay.Visible)
            {
                TmaxTestOverlay.DrawHiddenHint();
                return;
            }

            var style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 15,
                alignment = TextAnchor.UpperLeft,
            };
            string help =
                "TNAX RIDER TEST\n" +
                "W / S      throttle / reverse\n" +
                "A / D      steer (rider leans)\n" +
                "R          wheelie (hold)\n" +
                "Q / E      look left / right\n" +
                "F          get off / on\n" +
                "Space      brake\n" +
                "T          wheelie tuner panel\n" +
                "RMB drag   orbit camera (stays where you put it)\n" +
                "Scroll     zoom in / out\n" +
                "C          reset camera to chase view\n" +
                "H          hide / show this + readout";
            GUI.Box(new Rect(10, 150, 320, 200), help, style);
        }
    }
}
