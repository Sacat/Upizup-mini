using UnityEngine;

namespace UpIzUpMini.Character
{
    /// <summary>
    /// Reusable anti-stuck steering for direct-steering characters
    /// (PatrolNPC waypoint walkers and FollowController companions) that
    /// move with CharacterController.SimpleMove toward a target. These have
    /// no NavMesh, so a building collider can block them and they'll push
    /// into the wall forever (the companion "can get stuck on/cut through
    /// obstacles" limitation noted in earlier MINI entries).
    ///
    /// The mechanism is exactly what the user asked for: if a moving
    /// character is NOT actually making progress (blocked by the same
    /// object) for too long, steer it to a perpendicular evasion direction
    /// so it walks AROUND the obstacle instead of grinding into it. Each new
    /// stuck episode alternates the evasion side (left / right), so if one
    /// side is also blocked it tries the other rather than oscillating in
    /// place.
    ///
    /// Usage: call <see cref="ShouldEvade"/> each frame before moving. When
    /// it returns true, call <see cref="GetEvasionDirection(Vector3)"/> for
    /// the movement vector (or override the target by resolving a detour
    /// waypoint/reposition). Clear is reset on real progress so an
    /// unobstructed walk never triggers it.
    /// </summary>
    public class AntiStuckSteering : MonoBehaviour
    {
        [Tooltip("How little movement counts as 'stuck'.")]
        [SerializeField] private float stuckThreshold = 0.05f;
        [Tooltip("How long (seconds) below the stuck threshold before evading.")]
        [SerializeField] private float stuckTimeout = 1.2f;
        [Tooltip("How far to step sideways per evasion impulse (metres/frame-ish, applied as a direction).")]
        [SerializeField] private float evadeDistance = 2.5f;
        [Tooltip("How long (seconds) to keep evading before re-evaluating.")]
        [SerializeField] private float evadeDuration = 1.0f;

        private Vector3 _lastPos;
        private float _stationaryFor;
        private bool _evading;
        private float _evadeTimer;
        private int _side = 1; // 1 = right, -1 = left, for alternating evasion

        /// <summary>True when the character should change direction this frame.</summary>
        public bool IsEvading => _evading;

        private void Awake()
        {
            _lastPos = transform.position;
        }

        /// <summary>
        /// Call every frame before moving. Pass the current intrinsic speed
        /// (0 when idle) so a stopped-at-waypoint character isn't treated as
        /// "stuck against a wall". <paramref name="dt"/> defaults to
        /// Time.deltaTime; it is a parameter so the logic is testable outside
        /// Play mode (where Time.deltaTime is 0).
        /// </summary>
        public void Tick(float speed, float dt = -1f)
        {
            if (dt < 0f) dt = Time.deltaTime;
            Vector3 pos = transform.position;
            float moved = (pos - _lastPos).magnitude;
            _lastPos = pos;

            if (speed <= 0.01f)
            {
                // Not trying to move (paused at a waypoint, idle) - not stuck.
                _stationaryFor = 0f;
                return;
            }

            if (moved < stuckThreshold)
            {
                _stationaryFor += dt;
                if (_stationaryFor >= stuckTimeout && !_evading)
                {
                    _evading = true;
                    _evadeTimer = evadeDuration;
                }
            }
            else
            {
                // Real progress - fully reset (and end any active evasion).
                _stationaryFor = 0f;
                _evading = false;
                _evadeTimer = 0f;
            }

            if (_evading)
            {
                _evadeTimer -= dt;
                if (_evadeTimer <= 0f)
                {
                    // Evasion window over; if still stuck, try the other side.
                    _evading = false;
                    _side = -_side;
                }
            }
        }

        /// <summary>
        /// Returns a direction vector (in the character's horizontal plane,
        /// perpendicular to its current facing) to step around the obstacle.
        /// Call only when <see cref="IsEvading"/> is true. Alternates side per
        /// episode so it doesn't grind against a wall facing either way.
        /// </summary>
        public Vector3 GetEvasionDirection(Vector3 currentFacing)
        {
            Vector3 flat = Vector3.ProjectOnPlane(currentFacing, Vector3.up).normalized;
            if (flat.sqrMagnitude < 0.0001f) flat = transform.forward;
            Vector3 perp = Vector3.Cross(flat, Vector3.up);
            return (perp * _side * evadeDistance); // evadeDistance scales the step length
        }

        /// <summary>Reset state (e.g. when teleported or respawned).</summary>
        public void Clear()
        {
            _evading = false;
            _evadeTimer = 0f;
            _stationaryFor = 0f;
            _lastPos = transform.position;
        }
    }
}
