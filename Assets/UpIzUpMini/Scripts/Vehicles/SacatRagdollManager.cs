using Gadd420;
using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-119 follow-up, user: "i want to mimic the original ragdoll
    /// to my character. also how the ragdoll parts was mapped to the
    /// bike." Direct port of the vendor's own Gadd420.RagdollManager
    /// logic (same states, same order, same crash-velocity-inherit
    /// trick), adapted to Sacat's real Unity ragdoll (SacatRagdollBuilder)
    /// and to VehicleRider's native Humanoid IK instead of the vendor's
    /// own IK.cs (disabling the whole component has the same effect as
    /// the vendor setting ikScript.hasCrashed = true - it stops
    /// OnAnimatorIK from correcting hands/feet, freeing the limbs).
    ///
    /// MINI-119 follow-up fix, user (with the body-lock change in the
    /// same build): "the hands come off the handlebar, the character
    /// looks stiff... torso doesnt turn with the handle bar. no ragdoll
    /// stuff happening here." Real regression, not a fluke: the FIRST
    /// version built the whole ragdoll (Rigidbody + Collider +
    /// CharacterJoint on every humanoid bone) immediately at mount time,
    /// while riding normally. Adding physics components directly onto
    /// bones the Animator is actively driving is a known Unity trap -
    /// even kinematic ones can make Unity treat that hierarchy as
    /// physically simulated and stop writing normal animation/IK poses
    /// to it, which matches "stiff" and "torso doesn't turn" exactly.
    /// Fixed by building the ragdoll LAZILY, only at the actual instant
    /// of a real crash - Sacat's bones carry zero extra physics
    /// components at all while riding normally, so there is nothing to
    /// interfere with the Animator/IK the rest of the time.
    /// </summary>
    public class SacatRagdollManager : MonoBehaviour
    {
        [HideInInspector] public bool resetRider;

        private Animator _animator;
        private SacatRagdollBuilder.RagdollBone[] _bones; // null until the first real crash
        private VehicleRider _rider;
        private RB_Controller _rbScript;
        private Rigidbody _bikeRb;
        private bool _velocitySet;

        public void Configure(Animator animator, VehicleRider rider, RB_Controller rbScript, Rigidbody bikeRb)
        {
            _animator = animator;
            _rider = rider;
            _rbScript = rbScript;
            _bikeRb = bikeRb;
        }

        private void Update()
        {
            if (_rbScript == null) return;

            if (_rbScript.isCrashed)
            {
                if (_bones == null)
                {
                    // Build now, once, only because a real crash is
                    // actually happening - never while riding normally.
                    _bones = SacatRagdollBuilder.Build(_animator);
                }

                // Stop the native IK from fighting the ragdoll - same role
                // as the vendor's own ikScript.hasCrashed = true.
                if (_rider != null) _rider.enabled = false;

                foreach (var b in _bones)
                {
                    if (b.collider != null) b.collider.isTrigger = false;
                }
                foreach (var b in _bones)
                {
                    if (b.rigidbody == null) continue;
                    b.rigidbody.isKinematic = false;
                    if (!_velocitySet && _bikeRb != null)
                        b.rigidbody.linearVelocity = _bikeRb.linearVelocity;
                    _velocitySet = true;
                }
            }

            if (resetRider)
            {
                _velocitySet = false;
                if (_rider != null) _rider.enabled = true;

                if (_bones != null)
                {
                    foreach (var b in _bones)
                    {
                        if (b.collider != null) b.collider.isTrigger = true;
                    }
                    foreach (var b in _bones)
                    {
                        if (b.rigidbody != null) b.rigidbody.isKinematic = true;
                    }
                }
                resetRider = false;
            }
        }
    }
}
