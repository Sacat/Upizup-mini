using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-065. Syncs the visible wheel transforms to their physics
    /// WheelColliders every frame via WheelCollider.GetWorldPose - the
    /// same approach Unity's own WheelCollider documentation uses to
    /// synchronise a physics wheel with its rendered model, so no wheel-
    /// spin/steer math needed to be invented here.
    ///
    /// MINI-124 upgrades the children of the two driven transforms from
    /// thin placeholder discs to fitted SuperMoto wheel meshes. Physics
    /// remains unchanged: these are visual-only children and the original
    /// WheelColliders still own suspension, steering and contact.
    ///
    /// Bug fix (user report: the rear disc doesn't align with the real
    /// mesh's rear wheel from behind). The real cause: the physics
    /// WheelColliders are deliberately kept centred at X=0 (a genuinely
    /// off-centre rear WheelCollider would apply drive/brake force off the
    /// vehicle's centreline and induce a persistent yaw pull - bad
    /// physics), but the scan's real rear wheel geometry measures ~0.11m
    /// off the bike's own centreline (confirmed independently via Blender -
    /// an artefact of the source scan, not something to "fix" by moving
    /// the mesh). Overwriting the visual's full world position from the
    /// (centred) collider pose therefore put the disc exactly where the
    /// real wheel ISN'T. Now only the collider's HEIGHT (suspension
    /// compression) and ROTATION (spin/steer) are copied - X/Z are left
    /// alone, so each disc keeps its own authored offset (set once in
    /// Mini064TmaxAssetPrep.BuildPrefab from the real measured wheel
    /// position) and can sit exactly on the real, slightly-asymmetric
    /// mesh while the physics stays clean and symmetric. As a side effect the
    /// disc's POSITION now banks naturally with VisualLeanRoot's cosmetic
    /// lean (X/Z come from wherever it already sits in that hierarchy,
    /// not a world-space overwrite) - though its ROTATION still does not
    /// (still fully overwritten from the collider's own spin/steer pose,
    /// same known, accepted limitation as before).
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public class TmaxWheelVisuals : MonoBehaviour
    {
        [Header("Physics")]
        [SerializeField] private WheelCollider frontCollider;
        [SerializeField] private WheelCollider rearCollider;

        [Header("Visual")]
        [SerializeField] private Transform frontWheelVisual;
        [SerializeField] private Transform rearWheelVisual;
        [Tooltip("Visual-only fork/handlebar pivot. It follows the front WheelCollider steer angle but never spins with the wheel.")]
        [SerializeField] private Transform steeringPivot;
        [Tooltip("Existing rider IK targets. They remain at their established root paths and are moved from the steering pivot's pose at runtime.")]
        [SerializeField] private Transform leftGripTarget;
        [SerializeField] private Transform rightGripTarget;

        private Transform bikeLeanRoot;
        private Transform FindLeanRoot()
        {
            var controller = GetComponentInParent<TmaxBikeControllerCustom>();
            return controller != null ? controller.VisualLeanRoot : null;
        }

        private Quaternion steeringBaseLocalRotation = Quaternion.identity;
        private Vector3 leftGripPivotOffset;
        private Vector3 rightGripPivotOffset;
        private Quaternion leftGripPivotRotation = Quaternion.identity;
        private Quaternion rightGripPivotRotation = Quaternion.identity;
        private Vector3 frontWheelRestLocalPosition;
        private Vector3 rearWheelRestLocalPosition;
        private bool wheelRestPositionsCached;

        private void Awake()
        {
            CacheWheelRestPositions();
            CacheSteeringBaseRotation();
        }

        private void OnEnable()
        {
            CacheSteeringBaseRotation();
        }

        private void LateUpdate()
        {
            CacheWheelRestPositions();
            // MINI-193: bank the wheel discs with the body lean/pitch instead of keeping them vertical
            Quaternion bank = Quaternion.identity;
            var leanRoot = bikeLeanRoot != null ? bikeLeanRoot : (bikeLeanRoot = FindLeanRoot());
            if (leanRoot != null && leanRoot.parent != null)
                bank = leanRoot.rotation * Quaternion.Inverse(leanRoot.parent.rotation);
            UpdateWheel(frontCollider, frontWheelVisual, frontWheelRestLocalPosition, bank);
            UpdateWheel(rearCollider, rearWheelVisual, rearWheelRestLocalPosition, bank);
            UpdateSteeringAssembly();
        }

        private void CacheWheelRestPositions()
        {
            if (wheelRestPositionsCached)
                return;

            if (frontWheelVisual != null)
                frontWheelRestLocalPosition = frontWheelVisual.localPosition;
            if (rearWheelVisual != null)
                rearWheelRestLocalPosition = rearWheelVisual.localPosition;

            wheelRestPositionsCached = frontWheelVisual != null && rearWheelVisual != null;
        }

        private void CacheSteeringBaseRotation()
        {
            if (steeringPivot != null)
            {
                steeringBaseLocalRotation = steeringPivot.localRotation;
                CacheGrip(leftGripTarget, out leftGripPivotOffset, out leftGripPivotRotation);
                CacheGrip(rightGripTarget, out rightGripPivotOffset, out rightGripPivotRotation);
            }
        }

        private void UpdateSteeringAssembly()
        {
            if (frontCollider == null || steeringPivot == null)
                return;

            steeringPivot.localRotation = steeringBaseLocalRotation
                * Quaternion.Euler(0f, frontCollider.steerAngle, 0f);
            ApplyGrip(leftGripTarget, leftGripPivotOffset, leftGripPivotRotation);
            ApplyGrip(rightGripTarget, rightGripPivotOffset, rightGripPivotRotation);
        }

        private void CacheGrip(Transform grip, out Vector3 offset, out Quaternion rotation)
        {
            if (grip == null || steeringPivot == null)
            {
                offset = Vector3.zero;
                rotation = Quaternion.identity;
                return;
            }

            offset = steeringPivot.InverseTransformPoint(grip.position);
            rotation = Quaternion.Inverse(steeringPivot.rotation) * grip.rotation;
        }

        private void ApplyGrip(Transform grip, Vector3 offset, Quaternion rotation)
        {
            if (grip == null || steeringPivot == null)
                return;

            grip.SetPositionAndRotation(
                steeringPivot.TransformPoint(offset),
                steeringPivot.rotation * rotation);
        }

#if UNITY_EDITOR
        public void ConfigureVisuals(
            WheelCollider front,
            WheelCollider rear,
            Transform frontVisual,
            Transform rearVisual,
            Transform forkAndHandlebarPivot,
            Transform leftGrip,
            Transform rightGrip)
        {
            frontCollider = front;
            rearCollider = rear;
            frontWheelVisual = frontVisual;
            rearWheelVisual = rearVisual;
            steeringPivot = forkAndHandlebarPivot;
            leftGripTarget = leftGrip;
            rightGripTarget = rightGrip;
            wheelRestPositionsCached = false;
            CacheWheelRestPositions();
            CacheSteeringBaseRotation();
        }
#endif

        private static void UpdateWheel(
            WheelCollider collider,
            Transform visual,
            Vector3 restLocalPosition,
            Quaternion bank)
        {
            if (collider == null || visual == null)
                return;

            collider.GetWorldPose(
                out Vector3 position,
                out Quaternion rotation
            );

            // Rebuild the visual pose from an immutable authored rest point
            // every frame. The previous implementation read visual.position
            // back as its next baseline; any animation/order offset could
            // therefore become permanent and slowly walk a wheel off its hub.
            // Parent.TransformPoint also keeps the scan's approved asymmetric
            // X/Z placement and cosmetic bike lean, while WheelCollider owns
            // suspension height, spin and steering.
            Vector3 lockedPosition = visual.parent != null
                ? visual.parent.TransformPoint(restLocalPosition)
                : visual.position;
            lockedPosition.y = position.y;
            visual.SetPositionAndRotation(lockedPosition, bank * rotation);
        }
    }
}
