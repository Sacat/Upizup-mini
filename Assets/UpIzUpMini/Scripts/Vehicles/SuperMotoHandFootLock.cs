using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-119 follow-up, user: hands/feet still weren't tracking the
    /// handlebar/pegs during a wheelie via VehicleRider's own
    /// OnAnimatorIK (Mecanim's built-in Humanoid IK) - the SECOND time
    /// this exact mechanism has silently under-applied for Sacat on this
    /// bike (the same symptom that first drove this whole SuperMoto
    /// effort away from Mecanim IK, long before the ragdoll/Animation-
    /// Rigging/VehicleRider detours). Two failures of the same
    /// mechanism on the same character is a real pattern, not bad luck.
    ///
    /// Fix, per the user's own confirmed direction: pin hands/feet with
    /// a continuous, direct bone override instead of Mecanim IK - the
    /// same technique the vendor's OWN bike rider already uses
    /// successfully (Gadd420.IK.cs). It runs every LateUpdate, directly
    /// overwriting bone Transform.position/rotation via root-space math,
    /// completely independent of the Animator/Avatar IK pipeline - it
    /// cannot silently under-apply the way OnAnimatorIK can.
    ///
    /// Deliberately layered ON TOP of VehicleRider rather than replacing
    /// it: VehicleRider still owns seating/mounting/the ride animation;
    /// this only takes over the four contact points. No conflict between
    /// the two - Unity's normal execution order runs the Animator's own
    /// IK pass (which is what VehicleRider's OnAnimatorIK hooks into)
    /// BEFORE other scripts' LateUpdate, so Gadd420.IK's LateUpdate
    /// write simply happens after and wins, overriding whatever
    /// (possibly under-applied) result Mecanim IK produced with a
    /// guaranteed-correct final position.
    /// </summary>
    public class SuperMotoHandFootLock : MonoBehaviour
    {
        private Gadd420.IK _rightHand, _leftHand, _rightFoot, _leftFoot;

        public void Attach(Animator animator, Transform rightHandTarget, Transform leftHandTarget, Transform rightFootTarget, Transform leftFootTarget)
        {
            // Animator.GetBoneTransform THROWS (InvalidOperationException:
            // "Avatar is null"), it doesn't just return null, when the
            // avatar isn't a bound Humanoid yet - guard rather than let a
            // caller's whole Mount() unwind from this.
            if (animator == null || !animator.isHuman) return;

            _rightHand = AddIK(animator.GetBoneTransform(HumanBodyBones.RightHand), rightHandTarget);
            _leftHand = AddIK(animator.GetBoneTransform(HumanBodyBones.LeftHand), leftHandTarget);
            _rightFoot = AddIK(animator.GetBoneTransform(HumanBodyBones.RightFoot), rightFootTarget);
            _leftFoot = AddIK(animator.GetBoneTransform(HumanBodyBones.LeftFoot), leftFootTarget);
        }

        private static Gadd420.IK AddIK(Transform bone, Transform target)
        {
            if (bone == null || target == null) return null;

            var ik = bone.gameObject.AddComponent<Gadd420.IK>();
            ik.chainLength = 2; // hand: hand->forearm->upperarm. foot: foot->lowerleg->upperleg.
            ik.target = target;
            ik.iterations = 10;
            return ik;
        }

        /// <summary>Removes every lock this attached - called on dismount
        /// so the character doesn't keep reaching for the handlebar/pegs
        /// while walking around afterward.</summary>
        public void Detach()
        {
            DestroyIfExists(ref _rightHand);
            DestroyIfExists(ref _leftHand);
            DestroyIfExists(ref _rightFoot);
            DestroyIfExists(ref _leftFoot);
        }

        private void DestroyIfExists(ref Gadd420.IK ik)
        {
            if (ik != null) Destroy(ik);
            ik = null;
        }

        private void OnDestroy() => Detach();
    }
}
