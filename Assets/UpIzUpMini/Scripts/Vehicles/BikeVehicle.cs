using UnityEngine;
using UpIzUpMini.Character;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-033. Rideable motorcycle/scooter — the first vehicle system,
    /// built to be mesh-agnostic so a real TMAX model can be dropped in
    /// later with no rework (the visual is a clean scooter built from
    /// primitives by the scene builder; any bike mesh can replace the
    /// render child without touching this logic).
    ///
    /// Movement is camera-relative like the on-foot PlayerController, so
    /// riding feels consistent with walking. The bike is deliberately NOT
    /// too fast (top speed ~10 m/s) so it can wheelie — sustained at speed,
    /// the front lifts and it keeps driving on the rear.
    ///
    /// Controls (reusing existing keys where possible):
    ///   E   — mount / dismount (same key as interact, per "can use an
    ///         existing key already")
    ///   Space — WHEELIE while riding (Space is jump on foot; on the bike
    ///         it becomes the wheelie key)
    ///   WASD/arrows — steer + throttle
    ///   LeftShift — boost / faster (optional)
    ///
    /// On mount the active protagonist is parented onto the bike seat so
    /// they appear seated; the camera retargets to the bike. Dismount puts
    /// them back beside the bike and returns the camera to the character.
    /// </summary>
    public class BikeVehicle : MonoBehaviour
    {
        [Header("Speed / handling")]
        [SerializeField] private float maxSpeed = 10f;
        [SerializeField] private float acceleration = 14f;
        [SerializeField] private float brakeDecel = 18f;
        [SerializeField] private float turnSpeed = 3.2f;
        [SerializeField] private float wheelieSpeedThreshold = 2f; // min speed to wheelie

        [Header("Physics / height")]
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float groundSnap = 2f;    // how far below to snap to ground
        [SerializeField] private float riderHeight = 0.9f; // wheel radius / ride height

        [Header("Interaction")]
        [SerializeField] private float mountRange = 3f;

        [Header("Visuals / refs")]
        [SerializeField] private Transform riderMount;   // where the rider sits
        [SerializeField] private Transform frontWheel;  // visually spins / lifts
        [SerializeField] private Transform backWheel;

        private float _speed;
        private float _verticalVelocity;
        private bool _riderMounted;
        private Transform _rider;

        public bool IsIdle => !_riderMounted && _speed < 0.05f;
        public bool HasRider => _riderMounted;

        /// <summary>The Dismount offset, relative to the bike, when the rider
        /// gets off (updates as the bike faces).</summary>
        public Vector3 DismountPosition
            => transform.position - transform.forward * 1.6f + Vector3.up * 0.2f;

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            // Mount / dismount toggle.
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (_riderMounted) Dismount();
                else TryMountNearbyRider();
            }

            if (!_riderMounted) return;

            // Camera-relative steering like on foot.
            Transform cam = Camera.main != null ? Camera.main.transform : transform;
            Vector3 camForward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 dir = (camForward * v + camRight * h).normalized;

            // Turn toward input direction.
            if (dir.sqrMagnitude > 0.001f && _speed > 0.1f)
            {
                Quaternion target = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);
            }

            // Throttle / brake.
            float throttle = Input.GetAxisRaw("Vertical");
            bool boosting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            float targetSpeed = throttle > 0f
                ? Mathf.Lerp(0f, maxSpeed, Mathf.Clamp01(throttle * (boosting ? 1.0f : 0.7f)))
                : Mathf.Lerp(_speed, 0f, Mathf.Min(1f, brakeDecel * Time.deltaTime));

            if (throttle < 0f) targetSpeed = 0f; // reverse not needed; brake
            _speed = Mathf.Lerp(_speed, targetSpeed, Time.deltaTime * acceleration);

            // --- WHEELIE (Space) ---
            // Front wheel lifts when we're moving fast enough and Space held.
            bool wheelie = Input.GetKey(KeyCode.Space) && _speed >= wheelieSpeedThreshold;
            if (wheelie)
            {
                // Slight extra feel: reduce effective speed a touch, tilt nose up.
                RollBike(-1f);
            }
            else
            {
                RollBike(0f);
            }

            // Move forward along facing.
            Vector3 move = transform.forward * (_speed * Time.deltaTime);

            // Vertical (gravity + ground snap).
            _verticalVelocity += gravity * Time.deltaTime;
            move.y = _verticalVelocity * Time.deltaTime;

            // Ground snap: keep the bike at the ride height when close to a
            // surface below (so it follows hills without a heavy physics body).
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundSnap + 0.5f))
            {
                float groundY = hit.point.y;
                float targetY = groundY + riderHeight;
                transform.position = new Vector3(transform.position.x + move.x, targetY, transform.position.z);
                _verticalVelocity = 0f;
            }
            else
            {
                transform.position += move;
            }

            // Spin wheels visually.
            float spin = (_speed / maxSpeed) * 1200f * Time.deltaTime;
            if (frontWheel != null) frontWheel.Rotate(spin, 0f, 0f, Space.Self);
            if (backWheel != null) backWheel.Rotate(spin, 0f, 0f, Space.Self);
        }

        private void RollBike(float pitchAngle)
        {
            // Apply a visual (and small physical) pitch so the nose lifts.
            // We keep the bike upright but tilt the visual root slightly.
            var v = GetVisualRoot();
            if (v != null)
            {
                Quaternion current = v.localRotation;
                Quaternion target = Quaternion.Euler(pitchAngle * 12f, 0f, 0f);
                v.localRotation = Quaternion.Slerp(current, target, Time.deltaTime * 4f);
            }
        }

        private Transform GetVisualRoot()
        {
            // The first non-logic child is treated as the visual root.
            foreach (Transform child in transform)
            {
                if (child == riderMount || child == frontWheel || child == backWheel) continue;
                return child;
            }
            return null;
        }

        private void TryMountNearbyRider()
        {
            var switcher = CharacterSwitchManager.Instance;
            var active = switcher?.Active?.root;
            if (active == null) return;

            if (Vector3.Distance(transform.position, active.transform.position) > mountRange) return;

            // Mount: parent the active character to the seat.
            var player = active.GetComponent<PlayerController>();
            if (player != null) player.IsControlled = false;

            var cc = active.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            _rider = active.transform;
            _rider.SetParent(riderMount != null ? riderMount : transform, false);
            _rider.localPosition = Vector3.zero;
            _rider.localRotation = Quaternion.identity;

            // Freeze any follow/other movement on the rider.
            var follow = active.GetComponent<FollowController>();
            if (follow != null) follow.FollowingEnabled = false;

            _riderMounted = true;

            // Retarget the camera to the bike.
            var camera = UnityEngine.Object.FindFirstObjectByType<Cameras.ThirdPersonFollowCamera>();
            if (camera != null) camera.SetTarget(transform);

            // Try a seated full-body pose (safe no-op if no clip baked yet).
            var anim = active.GetComponent<HumanoidAnimationManager>();
            anim?.BeginSustainedAction("Ride");
        }

        public void Dismount()
        {
            if (!_riderMounted || _rider == null)
            {
                _riderMounted = false;
                return;
            }

            var rider = _rider;
            _rider = null;
            _riderMounted = false;

            // Restore the rider as an independent object.
            var switcher = CharacterSwitchManager.Instance;
            Vector3 pos = DismountPosition;

            rider.SetParent(null, true);
            rider.position = pos;

            var playerGo = rider.gameObject;
            var cc = playerGo.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = true;

            var player = playerGo.GetComponent<PlayerController>();
            if (player != null && switcher != null && switcher.Active != null
                && switcher.Active.root == rider) player.IsControlled = true;

            // Restore follow behaviour if this character was the inactive partner's
            // target (handled by CharacterSwitchManager on switch; here just re-enable).
            var follow = playerGo.GetComponent<FollowController>();
            if (follow != null && switcher != null && switcher.Active != null
                && switcher.Active.root != rider) follow.FollowingEnabled = true;

            // End the sustained seated pose.
            var anim = playerGo.GetComponent<HumanoidAnimationManager>();
            anim?.EndSustainedAction();

            // Return the camera to the character.
            var camera = UnityEngine.Object.FindFirstObjectByType<Cameras.ThirdPersonFollowCamera>();
            if (camera != null && switcher?.Active?.root != null) camera.SetTarget(switcher.Active.root.transform);
        }
    }
}
