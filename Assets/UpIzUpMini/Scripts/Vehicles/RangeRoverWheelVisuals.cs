using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// Keeps four visual Range Rover wheels locked to their matching physics
    /// WheelColliders. GetWorldPose supplies suspension, spin and steering, so
    /// the front pair steer while all four tyres rotate with the vehicle.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class RangeRoverWheelVisuals : MonoBehaviour
    {
        [SerializeField] private WheelCollider frontLeftCollider;
        [SerializeField] private WheelCollider frontRightCollider;
        [SerializeField] private WheelCollider rearLeftCollider;
        [SerializeField] private WheelCollider rearRightCollider;
        [SerializeField] private Transform frontLeftVisual;
        [SerializeField] private Transform frontRightVisual;
        [SerializeField] private Transform rearLeftVisual;
        [SerializeField] private Transform rearRightVisual;

        public WheelCollider FrontLeftCollider => frontLeftCollider;
        public WheelCollider FrontRightCollider => frontRightCollider;
        public WheelCollider RearLeftCollider => rearLeftCollider;
        public WheelCollider RearRightCollider => rearRightCollider;
        public Transform FrontLeftVisual => frontLeftVisual;
        public Transform FrontRightVisual => frontRightVisual;
        public Transform RearLeftVisual => rearLeftVisual;
        public Transform RearRightVisual => rearRightVisual;

        private void LateUpdate()
        {
            Sync(frontLeftCollider, frontLeftVisual);
            Sync(frontRightCollider, frontRightVisual);
            Sync(rearLeftCollider, rearLeftVisual);
            Sync(rearRightCollider, rearRightVisual);
        }

        private static void Sync(WheelCollider collider, Transform visual)
        {
            if (collider == null || visual == null) return;
            collider.GetWorldPose(out Vector3 position, out Quaternion rotation);
            visual.SetPositionAndRotation(position, rotation);
        }

#if UNITY_EDITOR
        public void Configure(
            WheelCollider frontLeft,
            WheelCollider frontRight,
            WheelCollider rearLeft,
            WheelCollider rearRight,
            Transform frontLeftMesh,
            Transform frontRightMesh,
            Transform rearLeftMesh,
            Transform rearRightMesh)
        {
            frontLeftCollider = frontLeft;
            frontRightCollider = frontRight;
            rearLeftCollider = rearLeft;
            rearRightCollider = rearRight;
            frontLeftVisual = frontLeftMesh;
            frontRightVisual = frontRightMesh;
            rearLeftVisual = rearLeftMesh;
            rearRightVisual = rearRightMesh;
        }
#endif
    }
}
