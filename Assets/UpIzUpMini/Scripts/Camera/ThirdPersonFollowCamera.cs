using UnityEngine;
using UpIzUpMini.InputSystem;

namespace UpIzUpMini.Cameras
{
    /// <summary>
    /// Perspective three-quarter follow camera. Orbits around the target
    /// under mouse control on both axes (only while the cursor is locked -
    /// see PauseMenuController) and always looks at the target, so mouse
    /// movement changes the view angle directly rather than just spinning
    /// around a fixed pitch. Pitch is clamped so the player can look up and
    /// down without flipping over the target or going perfectly flat.
    /// </summary>
    public class ThirdPersonFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float orbitDistance = 9.6f;
        [SerializeField] private float followYawOffsetDegrees = 0f;
        [SerializeField] private float lookAtHeight = 1.4f;
        [SerializeField] private float positionSmoothTime = 0.15f;
        [SerializeField] private float mouseSensitivity = 140f;
        [SerializeField] private float verticalSensitivity = 100f;
        [SerializeField] private float minPitchDegrees = 20f;
        [SerializeField] private float maxPitchDegrees = 75f;
        [Header("Camera obstruction")]
        [SerializeField] private float obstructionRadius = 0.28f;
        [SerializeField] private float obstructionBuffer = 0.18f;
        [SerializeField] private LayerMask obstructionLayers = ~0;

        private Vector3 _velocity;
        [Tooltip("MINI-072: how fast the locked chase camera swings in behind the vehicle. Eased rather than snapped, or the view whips around on every steering input.")]
        [SerializeField] private float lockedYawFollowSpeed = 4.5f;
        [Tooltip("Pitch held while locked behind a vehicle.")]
        [SerializeField] private float lockedPitchDegrees = 12f;

        /// <summary>
        /// True while riding/driving: mouse orbit is ignored and the camera sits
        /// behind whatever it is following. Set by the vehicle interactables on
        /// mount and cleared on dismount.
        /// </summary>
        public bool OrbitLocked { get; set; }

        private float _yaw;

        // Field initializer (not Awake) so this reads correctly even
        // outside Play mode - Mini011SceneValidation checks this value
        // against the 40-50 degree acceptance range without entering
        // Play mode, where Awake() would never run.
        private float _pitch = 44f;

        public void SetTarget(Transform newTarget) => target = newTarget;

        public float CurrentLookAngleDegrees => _pitch;

        private void Awake()
        {
            _yaw = followYawOffsetDegrees;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Mouse-look on both axes: only while the cursor is locked
            // (i.e. not paused/in a menu), so opening the pause menu
            // doesn't spin/tilt the camera via residual mouse delta.
            if (OrbitLocked)
            {
                // MINI-072: while driving, the camera is a plain chase cam sat
                // behind the vehicle - no mouse orbit at all. The user asked for
                // "just the third person regular" on the bike, and panning was
                // actively broken there: the orbit yaw is absolute, so panning
                // away left the camera pointing off into the world while the
                // bike drove out of shot. Following the target's own heading is
                // the fix, not clamping the pan.
                _yaw = Mathf.LerpAngle(_yaw, target.eulerAngles.y + followYawOffsetDegrees,
                    1f - Mathf.Exp(-lockedYawFollowSpeed * Time.deltaTime));
                _pitch = Mathf.Lerp(_pitch, lockedPitchDegrees,
                    1f - Mathf.Exp(-lockedYawFollowSpeed * Time.deltaTime));
            }
            else if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 lookInput = GameInput.Look;
                _yaw += lookInput.x * mouseSensitivity * Time.deltaTime;
                _pitch -= lookInput.y * verticalSensitivity * Time.deltaTime;
                _pitch = Mathf.Clamp(_pitch, minPitchDegrees, maxPitchDegrees);
            }

            Quaternion orbitRot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 desiredPosition = target.position
                + orbitRot * new Vector3(0f, 0f, -orbitDistance)
                + Vector3.up * lookAtHeight;

            Vector3 lookPoint = target.position + Vector3.up * lookAtHeight;
            desiredPosition = ResolveObstruction(lookPoint, desiredPosition);

            transform.position = Vector3.SmoothDamp(
                transform.position, desiredPosition, ref _velocity, positionSmoothTime);

            transform.LookAt(lookPoint);
        }

        private Vector3 ResolveObstruction(Vector3 lookPoint, Vector3 desiredPosition)
        {
            Vector3 ray = desiredPosition - lookPoint;
            float distance = ray.magnitude;
            if (distance <= 0.01f) return desiredPosition;

            RaycastHit[] hits = Physics.SphereCastAll(
                lookPoint, obstructionRadius, ray / distance, distance,
                obstructionLayers, QueryTriggerInteraction.Ignore);

            float nearest = distance;
            bool blocked = false;
            for (int i = 0; i < hits.Length; i++)
            {
                Transform hitTransform = hits[i].transform;
                if (hitTransform == null || hitTransform == target || hitTransform.IsChildOf(target)) continue;
                if (hits[i].distance >= nearest) continue;
                nearest = hits[i].distance;
                blocked = true;
            }

            if (!blocked) return desiredPosition;
            float safeDistance = Mathf.Max(0.55f, nearest - obstructionBuffer);
            return lookPoint + ray.normalized * safeDistance;
        }
    }
}
