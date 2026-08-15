using UnityEngine;

namespace UpIzUpMini.Cameras
{
    /// <summary>
    /// Perspective three-quarter follow camera. Orbits around the target
    /// horizontally under mouse control (only while the cursor is locked -
    /// see PauseMenuController) and always looks down at the target, so the
    /// downward look angle stays roughly constant regardless of orbit yaw
    /// or the player's facing direction. With the default distance/height
    /// this produces an ~44 degree downward look angle
    /// (atan(height/distance)), inside the 40-50 degree acceptance range.
    /// </summary>
    public class ThirdPersonFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float distance = 7f;
        [SerializeField] private float height = 6.8f;
        [SerializeField] private float followYawOffsetDegrees = 0f;
        [SerializeField] private float lookAtHeight = 1.4f;
        [SerializeField] private float positionSmoothTime = 0.15f;
        [SerializeField] private float mouseSensitivity = 140f;

        private Vector3 _velocity;
        private float _yaw;

        public void SetTarget(Transform newTarget) => target = newTarget;

        public float CurrentLookAngleDegrees => Mathf.Atan2(height, distance) * Mathf.Rad2Deg;

        private void Awake()
        {
            _yaw = followYawOffsetDegrees;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Mouse-look: only orbit while the cursor is locked (i.e. not
            // paused/in a menu - see PauseMenuController), so opening the
            // pause menu doesn't spin the camera via residual mouse delta.
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                _yaw += Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            }

            Vector3 offsetDir = Quaternion.Euler(0f, _yaw, 0f) * Vector3.back;
            Vector3 desiredPosition = target.position + offsetDir * distance + Vector3.up * height;

            transform.position = Vector3.SmoothDamp(
                transform.position, desiredPosition, ref _velocity, positionSmoothTime);

            transform.LookAt(target.position + Vector3.up * lookAtHeight);
        }
    }
}
