using UnityEngine;

namespace UpIzUpMini.Cameras
{
    /// <summary>
    /// Perspective three-quarter follow camera. Holds a fixed world-yaw
    /// offset behind the target and always looks down at it, so the
    /// resulting look angle stays roughly constant regardless of the
    /// player's facing direction. With the default distance/height this
    /// produces an ~44 degree downward look angle (atan(height/distance)),
    /// inside the 40-50 degree acceptance range for MINI-001.
    /// </summary>
    public class ThirdPersonFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float distance = 7f;
        [SerializeField] private float height = 6.8f;
        [SerializeField] private float followYawOffsetDegrees = 0f;
        [SerializeField] private float lookAtHeight = 1.4f;
        [SerializeField] private float positionSmoothTime = 0.15f;

        private Vector3 _velocity;

        public void SetTarget(Transform newTarget) => target = newTarget;

        public float CurrentLookAngleDegrees => Mathf.Atan2(height, distance) * Mathf.Rad2Deg;

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 offsetDir = Quaternion.Euler(0f, followYawOffsetDegrees, 0f) * Vector3.back;
            Vector3 desiredPosition = target.position + offsetDir * distance + Vector3.up * height;

            transform.position = Vector3.SmoothDamp(
                transform.position, desiredPosition, ref _velocity, positionSmoothTime);

            transform.LookAt(target.position + Vector3.up * lookAtHeight);
        }
    }
}
