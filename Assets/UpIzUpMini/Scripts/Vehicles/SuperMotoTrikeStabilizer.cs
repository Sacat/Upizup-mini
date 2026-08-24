using Gadd420;
using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-119 follow-up, user: "i prefer my wheelie system with the
    /// collider system activated on wheelie." Direct port of
    /// TmaxBikeControllerCustom's own trike-stabilizer (two invisible
    /// rear outrigger points, raycast + spring/damper, active only during
    /// a wheelie) onto the stock Motorbike Physics Tool bike - same
    /// design, same math, adapted to read the stock asset's own
    /// Input_Manager.WheelieInput and RB_Controller.wheelColliders
    /// instead of our custom controller's own fields. A real Rigidbody.
    /// AddForceAtPosition spring at an actual ground contact point, NOT a
    /// real Collider/WheelCollider component - the custom controller's
    /// own header explains why: an earlier attempt with real WheelColliders
    /// there failed a drop test.
    ///
    /// Anchors are auto-placed relative to the rear wheel (wheelColliders[0]
    /// per this asset's own convention - see TmaxBikeController facade's
    /// IsFrontWheelGrounded comment) since, unlike the custom bike, this
    /// rig has no manually-authored anchor transforms yet. Offsets are
    /// exposed as tunable fields (and mirrored in Mini119StockDemoBikeTuner)
    /// rather than hardcoded, since the right width for THIS bike's mesh
    /// hasn't been hand-tuned the way the custom bike's was.
    /// </summary>
    [RequireComponent(typeof(RB_Controller), typeof(Rigidbody))]
    public class SuperMotoTrikeStabilizer : MonoBehaviour
    {
        [Tooltip("How far to each side of the rear wheel the outrigger points sit (m).")]
        public float outriggerSideOffset = 0.35f;
        [Tooltip("How long the debounce is before a sustained (non-wheelie) front-wheel-off-ground counts as 'up' too - same purpose as the custom bike's own trikeStabilizerDebounceSeconds.")]
        public float debounceSeconds = 0.22f;
        public float rayRange = 0.15f;
        public float springRate = 6000f;
        public float damping = 400f;

        private RB_Controller _rb;
        private Rigidbody _body;
        private Input_Manager _input;
        private Transform _leftAnchor;
        private Transform _rightAnchor;
        private float _frontUngroundedSeconds;

        private void Awake()
        {
            _rb = GetComponent<RB_Controller>();
            _body = GetComponent<Rigidbody>();
            _input = GetComponent<Input_Manager>();

            _leftAnchor = new GameObject("TrikeStabAnchor_Left").transform;
            _rightAnchor = new GameObject("TrikeStabAnchor_Right").transform;
            _leftAnchor.SetParent(transform, false);
            _rightAnchor.SetParent(transform, false);
            PlaceAnchors();
        }

        private void PlaceAnchors()
        {
            if (_rb == null || _rb.wheelColliders == null || _rb.wheelColliders.Length < 1 || _rb.wheelColliders[0] == null)
                return;

            var rearWheel = _rb.wheelColliders[0];
            Vector3 rearBottomWorld = rearWheel.transform.position - Vector3.up * rearWheel.radius;
            Vector3 rearBottomLocal = transform.InverseTransformPoint(rearBottomWorld);

            _leftAnchor.localPosition = rearBottomLocal + Vector3.left * outriggerSideOffset;
            _rightAnchor.localPosition = rearBottomLocal + Vector3.right * outriggerSideOffset;
        }

        private void FixedUpdate()
        {
            if (_rb == null || _body == null || _input == null) return;
            // MINI-119 follow-up fix, user (screenshot of the bike lying
            // fully on its side): "implement the other colliders like a
            // trike when the wheelie button is pressed... this worked
            // before so it can work again." The `if (_rb.isCrashed)
            // return;` that used to be here was switching this whole
            // system off at exactly the moment it was needed most -
            // removed, same reasoning as SuperMotoWheelieAssist's own
            // auto-recover fix.

            bool frontGrounded = _rb.wheelColliders != null && _rb.wheelColliders.Length > 1
                && _rb.wheelColliders[1] != null && _rb.wheelColliders[1].isGrounded;

            if (!frontGrounded) _frontUngroundedSeconds += Time.fixedDeltaTime;
            else _frontUngroundedSeconds = 0f;

            bool active =
                Mathf.Abs(_input.WheelieInput) > 0.01f
                || _frontUngroundedSeconds > debounceSeconds;

            if (!active) return;

            ApplyStabilizerSpring(_leftAnchor);
            ApplyStabilizerSpring(_rightAnchor);
        }

        private void ApplyStabilizerSpring(Transform anchor)
        {
            if (anchor == null || _body == null) return;

            Vector3 origin = anchor.position + Vector3.up * rayRange;
            float maxDist = rayRange * 2f;

            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxDist))
                return;

            float penetration = maxDist - hit.distance;
            float pointVelocityY = _body.GetPointVelocity(hit.point).y;

            float force = (penetration * springRate) - (pointVelocityY * damping);
            force = Mathf.Max(0f, force); // a spring only ever pushes, never pulls

            _body.AddForceAtPosition(Vector3.up * force, hit.point, ForceMode.Force);
        }
    }
}
