using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-066. A stable follow point for the game camera while the player
    /// is riding.
    ///
    /// Tracks the bike's position and HEADING only - never its pitch or
    /// roll. That matters because the wheelie pitches the bike up to 90
    /// degrees and the rider goes with it; a camera following either of them
    /// directly would rotate onto its side mid-wheelie. Following a level
    /// anchor instead keeps the chase camera steady behind the bike no matter
    /// what the bike is doing, which is both what the user asked for ("the
    /// camera should be at the back of the bike in the real game") and the
    /// framing that shows the wheelie at its best.
    ///
    /// Height is lifted slightly off the bike's origin so the camera aims at
    /// the rider rather than the road surface.
    /// </summary>
    public class BikeCameraAnchor : MonoBehaviour
    {
        [Tooltip("How high above the bike's origin the camera aims - roughly rider chest height.")]
        [SerializeField] private float heightOffset = 0.9f;
        [Tooltip("How quickly the anchor's heading catches up to the bike's. Smoothed so sharp steering doesn't whip the camera around.")]
        [SerializeField] private float yawSmoothing = 6f;

        private Transform _bike;
        private float _yaw;

        public void Follow(Transform bike)
        {
            _bike = bike;
            if (bike != null)
            {
                _yaw = FlatYaw(bike);
                transform.position = bike.position + Vector3.up * heightOffset;
                transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            }
        }

        private static float FlatYaw(Transform t)
        {
            // Heading taken from the flattened forward vector, NOT
            // eulerAngles.y: once the bike pitches past vertical in a wheelie
            // its euler decomposition flips and the yaw reading jumps 180
            // degrees, which would spin the camera around behind the player.
            Vector3 flat = Vector3.ProjectOnPlane(t.forward, Vector3.up);
            if (flat.sqrMagnitude < 0.0001f)
                flat = Vector3.ProjectOnPlane(-t.up, Vector3.up); // near-vertical: heading lives in -up
            if (flat.sqrMagnitude < 0.0001f) return _staticFallbackYaw;
            _staticFallbackYaw = Quaternion.LookRotation(flat.normalized, Vector3.up).eulerAngles.y;
            return _staticFallbackYaw;
        }

        private static float _staticFallbackYaw;

        // Update, NOT LateUpdate. ThirdPersonFollowCamera reads this anchor in
        // its own LateUpdate, and Unity gives no ordering guarantee between two
        // LateUpdates - if the camera happened to run first it would follow
        // last frame.s anchor position, which shows up as camera judder even
        // once the Rigidbody itself is interpolated. Updating here puts this
        // reliably before every LateUpdate. Rigidbody interpolation has
        // already been applied to the bike.s transform by this point, so the
        // position read here is the smoothed one.
        private void Update()
        {
            if (_bike == null) return;

            transform.position = _bike.position + Vector3.up * heightOffset;
            _yaw = Mathf.LerpAngle(_yaw, FlatYaw(_bike), yawSmoothing * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        }
    }
}
