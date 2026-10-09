#if MINI206_DRAFT
// =====================================================================================================================
// MINI-206 DRAFT - written in the cloud design lane, NEVER COMPILED OR RUN. Enable with the scripting define MINI206_DRAFT.
// =====================================================================================================================
using UnityEngine;

namespace UpIzUpMini.Vehicles.Draft
{
    /// <summary>
    /// Puts the MINI-206 rebuilt TMAX (Docs/CharacterPipeline/MINI-206/TMAX/TMAX_560_Rebuilt.fbx) on the CURRENT TMAX_560 prefab
    /// (TmaxBikeControllerCustom) and makes the steering read properly:
    ///  - wheels: suspension height + spin + steer from the WheelColliders (same proven method as TmaxWheelVisuals: authored
    ///    rest X/Z, collider Y, banked with the body lean);
    ///  - fork assembly (fork legs, calipers, fender) and handlebar turn about their OWN local Y, which the FBX already aligns
    ///    with the 25 deg raked steering axis, by the front collider steer angle (optionally exaggerated at low speed, where a
    ///    real rider turns the bars far more than the physics steer angle);
    ///  - the riders' grip IK targets (HandlebarLeft/Right) follow the FBX GripLeft/GripRight empties, so the hands turn with the bars.
    /// Setup on a COPY of TMAX_560.prefab: drop the FBX under VisualLeanRoot (origin on the ground between the tyres, +Z forward),
    /// add this component to the prefab root, assign the fields, list the old visuals in hideRenderers (the glb "Body", the
    /// SuperMotoBlackWheel overlays, SuperMotoForkHandlebarFit) and disable TmaxWheelVisuals. Nothing in the physics changes.
    /// </summary>
    [DefaultExecutionOrder(10001)]
    public class TmaxRebuiltVisuals : MonoBehaviour
    {
        public WheelCollider frontCollider, rearCollider;
        public Transform frontWheel, rearWheel, forkAssembly, handlebar, gripLeft, gripRight;
        [Tooltip("Rider IK targets on the prefab (HandlebarLeft / HandlebarRight).")]
        public Transform handTargetLeft, handTargetRight;
        public Renderer[] hideRenderers;
        [Tooltip("Visual steering gain at walking pace (the bars turn more than the physics steer angle when slow), blended to 1 by fullGainSpeedKmh.")]
        public float lowSpeedSteerGain = 1.8f;
        public float fullGainSpeedKmh = 25f;
        [Tooltip("Visual bar lock, degrees.")]
        public float maxVisualSteer = 32f;
        [Tooltip("Smoothing of the bar angle (s): stops keyboard steering from snapping the bars.")]
        public float steerSmoothing = 0.06f;

        Vector3 _frontRest, _rearRest; Quaternion _forkBase, _barBase, _handLRot, _handRRot; Rigidbody _rb; float _steer, _steerVel;

        void Awake()
        {
            _rb = GetComponentInParent<Rigidbody>();
            if (frontWheel) _frontRest = frontWheel.localPosition;
            if (rearWheel) _rearRest = rearWheel.localPosition;
            if (forkAssembly) _forkBase = forkAssembly.localRotation;
            if (handlebar) _barBase = handlebar.localRotation;
            if (handTargetLeft && gripLeft) _handLRot = Quaternion.Inverse(gripLeft.rotation) * handTargetLeft.rotation;
            if (handTargetRight && gripRight) _handRRot = Quaternion.Inverse(gripRight.rotation) * handTargetRight.rotation;
            if (hideRenderers != null) foreach (var r in hideRenderers) if (r) r.enabled = false;
        }

        void LateUpdate()
        {
            Transform leanRoot = frontWheel ? frontWheel.parent : null;
            Quaternion bank = leanRoot && leanRoot.parent ? leanRoot.rotation * Quaternion.Inverse(leanRoot.parent.rotation) : Quaternion.identity;
            Wheel(frontCollider, frontWheel, _frontRest, bank);
            Wheel(rearCollider, rearWheel, _rearRest, bank);
            if (frontCollider == null) return;
            float kmh = _rb ? _rb.velocity.magnitude * 3.6f : 0f;
            float gain = Mathf.Lerp(lowSpeedSteerGain, 1f, Mathf.Clamp01(kmh / Mathf.Max(1f, fullGainSpeedKmh)));
            float target = Mathf.Clamp(frontCollider.steerAngle * gain, -maxVisualSteer, maxVisualSteer);
            _steer = Mathf.SmoothDamp(_steer, target, ref _steerVel, steerSmoothing);
            if (forkAssembly) forkAssembly.localRotation = _forkBase * Quaternion.Euler(0f, _steer, 0f);
            if (handlebar) handlebar.localRotation = _barBase * Quaternion.Euler(0f, _steer, 0f);
            if (handTargetLeft && gripLeft) handTargetLeft.SetPositionAndRotation(gripLeft.position, gripLeft.rotation * _handLRot);
            if (handTargetRight && gripRight) handTargetRight.SetPositionAndRotation(gripRight.position, gripRight.rotation * _handRRot);
        }

        static void Wheel(WheelCollider c, Transform v, Vector3 rest, Quaternion bank)
        {
            if (c == null || v == null) return;
            c.GetWorldPose(out Vector3 pos, out Quaternion rot);
            Vector3 p = v.parent ? v.parent.TransformPoint(rest) : v.position; p.y = pos.y;
            v.SetPositionAndRotation(p, bank * rot);
        }
    }
}
#endif
