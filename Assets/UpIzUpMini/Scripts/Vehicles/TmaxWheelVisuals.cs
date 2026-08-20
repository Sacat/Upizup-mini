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
    /// Adapted from the user-supplied reference component. One TMAX-
    /// specific note: because the real wheel geometry on this bike is
    /// baked into one static scan mesh with no separable wheel mesh (see
    /// PROJECT-HANDOFF.md's MINI-064 investigation), the two "visual"
    /// transforms this drives are small stand-in wheel discs placed near
    /// the real wheel positions, not the real wheel mesh itself - a
    /// spinning approximation ("the illusion that they turn"), not a
    /// literal one.
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
    public class TmaxWheelVisuals : MonoBehaviour
    {
        [Header("Physics")]
        [SerializeField] private WheelCollider frontCollider;
        [SerializeField] private WheelCollider rearCollider;

        [Header("Visual")]
        [SerializeField] private Transform frontWheelVisual;
        [SerializeField] private Transform rearWheelVisual;

        private void LateUpdate()
        {
            UpdateWheel(frontCollider, frontWheelVisual);
            UpdateWheel(rearCollider, rearWheelVisual);
        }

        private static void UpdateWheel(
            WheelCollider collider,
            Transform visual)
        {
            if (collider == null || visual == null)
                return;

            collider.GetWorldPose(
                out Vector3 position,
                out Quaternion rotation
            );

            Vector3 p = visual.position;
            p.y = position.y;
            visual.position = p;
            visual.rotation = rotation;
        }
    }
}
