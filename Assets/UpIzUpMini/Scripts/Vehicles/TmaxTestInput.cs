using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-065: a standalone keyboard adapter for the isolated
    /// TMAX_Physics_Test scene only - reads raw Input axes/keys and feeds
    /// them into TmaxBikeController.SetInput(throttle, steer, brake),
    /// exactly the decoupled boundary the controller was built for.
    ///
    /// Deliberately separate from the game's real input wiring (which
    /// doesn't exist yet for vehicles - PlayerController drives on-foot
    /// movement only) so this test rig can be deleted or replaced without
    /// touching TmaxBikeController itself. Also handles the "simple test
    /// recovery" requirement: R force-recovers regardless of tip state,
    /// and an on-screen readout shows speed/wheelie/tipped state so the
    /// bike can be evaluated without the Scene view's gizmos.
    ///
    /// Controls: W/S or Up/Down = throttle/reverse, A/D or Left/Right =
    /// steer, Space = brake, E = wheelie - HOLD it to lift the front,
    /// release to let it settle (see TmaxBikeController.SetWheelieHeld),
    /// R = recover.
    /// </summary>
    [RequireComponent(typeof(TmaxBikeController))]
    public class TmaxTestInput : MonoBehaviour
    {
        [SerializeField] private KeyCode recoverKey = KeyCode.R;
        [Tooltip("Redesigned per user feedback: \"when you hold down e it should go up more and then the taping would help balancing it in the air\" - E is now read continuously (GetKey), not just on the down-edge.")]
        [SerializeField] private KeyCode wheelieKey = KeyCode.E;
        private TmaxBikeController _bike;
        private BikeInteractable _ridden;

        private void Awake()
        {
            _bike = GetComponent<TmaxBikeController>();
            _ridden = GetComponent<BikeInteractable>();
        }

        private void Update()
        {
            TmaxTestOverlay.Poll();

            // MINI-066: once a rider is actually mounted, BikeInteractable
            // owns the controls (and uses the real game's key scheme). Both
            // components calling SetInput every frame would fight, with
            // whichever ran last winning - so this one stands aside.
            if (_ridden != null && _ridden.HasRider) return;

            float throttle = Input.GetAxis("Vertical");
            float steer = Input.GetAxis("Horizontal");
            float brake = Input.GetKey(KeyCode.Space) ? 1f : 0f;

            _bike.SetInput(throttle, steer, brake);

            // GetKey (held state), not GetKeyDown - holding the key lifts
            // the front continuously; releasing (even briefly, "tapping")
            // lets it settle back down before the next hold, which is the
            // actual balancing mechanic (see TmaxBikeController.ApplyWheelie).
            _bike.SetWheelieHeld(Input.GetKey(wheelieKey));

            if (Input.GetKeyDown(recoverKey) || (_bike.IsTipped() && Input.GetKeyDown(KeyCode.Return)))
            {
                _bike.Recover();
            }
        }

        private void OnGUI()
        {
            // Shared with the controls box so one H press hides both - see
            // TmaxTestOverlay for why the toggle is shared rather than local.
            if (!TmaxTestOverlay.Visible) return;

            var style = new GUIStyle(GUI.skin.box) { fontSize = 20, alignment = TextAnchor.UpperLeft };
            string status = $"TNAX PHYSICS TEST\n" +
                             $"Speed: {_bike.SpeedKmh:F1} km/h\n" +
                             $"Wheelie: {(_bike.IsWheelieing ? $"YES ({_bike.WheelieAngle:F0} deg)" : "no")} (hold R when ridden)\n" +
                             $"Tipped: {(_bike.IsTipped() ? "YES - press R to recover" : "no")}\n" +
                             // This rig's own keys only apply when NOBODY is
                             // riding - once mounted, BikeInteractable owns the
                             // controls and uses the real game scheme (R wheelie).
                             $"Unridden: W/S throttle, A/D steer, Space brake, E wheelie, R recover";
            GUI.Box(new Rect(10, 10, 460, 130), status, style);
        }
    }
}
