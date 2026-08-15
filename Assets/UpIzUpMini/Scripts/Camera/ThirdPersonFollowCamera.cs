using UnityEngine;

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

        private Vector3 _velocity;
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
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                _yaw += Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
                _pitch -= Input.GetAxis("Mouse Y") * verticalSensitivity * Time.deltaTime;
                _pitch = Mathf.Clamp(_pitch, minPitchDegrees, maxPitchDegrees);
            }

            Quaternion orbitRot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 desiredPosition = target.position
                + orbitRot * new Vector3(0f, 0f, -orbitDistance)
                + Vector3.up * lookAtHeight;

            transform.position = Vector3.SmoothDamp(
                transform.position, desiredPosition, ref _velocity, positionSmoothTime);

            transform.LookAt(target.position + Vector3.up * lookAtHeight);
        }
    }
}
