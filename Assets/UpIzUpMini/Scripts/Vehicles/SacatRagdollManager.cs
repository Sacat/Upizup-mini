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
    /// </summary>
    public class SacatRagdollManager : MonoBehaviour
    {
        [HideInInspector] public bool resetRider;

        private SacatRagdollBuilder.RagdollBone[] _bones;
        private VehicleRider _rider;
        private RB_Controller _rbScript;
        private Rigidbody _bikeRb;
        private bool _velocitySet;

        public void Configure(SacatRagdollBuilder.RagdollBone[] bones, VehicleRider rider, RB_Controller rbScript, Rigidbody bikeRb)
        {
            _bones = bones;
            _rider = rider;
            _rbScript = rbScript;
            _bikeRb = bikeRb;

            // Match the vendor's own Start(): inert (kinematic + trigger)
            // until a real crash.
            foreach (var b in _bones)
            {
                if (b.rigidbody != null) b.rigidbody.isKinematic = true;
                if (b.collider != null) b.collider.isTrigger = true;
            }
        }

        private void Update()
        {
            if (_bones == null || _rbScript == null) return;

            if (_rbScript.isCrashed)
            {
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

                foreach (var b in _bones)
                {
                    if (b.collider != null) b.collider.isTrigger = true;
                }
                foreach (var b in _bones)
                {
                    if (b.rigidbody != null) b.rigidbody.isKinematic = true;
                }
                resetRider = false;
            }
        }
    }
}
