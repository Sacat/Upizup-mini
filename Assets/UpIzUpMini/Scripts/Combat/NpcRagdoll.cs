using UnityEngine;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.Combat
{
    /// <summary>
    /// MINI-119 follow-up, user: "i notice the supermoto original rig
    /// rag behaves something like this... can you make npcs like
    /// ragdoll behaviour... if i hit the npc they will behave like
    /// ragdoll." Same pattern as the vendor's own SuperMoto rider crash
    /// ragdoll (Gadd420.RagdollManager, read directly earlier this
    /// task): a real Unity ragdoll built once and left INERT (bones
    /// kinematic, colliders trigger) the whole time nothing's
    /// happening, then made dynamic with a one-time velocity match on
    /// impact for a realistic momentum-based tumble, then reversed back
    /// to inert once recovered - never destroyed/rebuilt each time,
    /// same object reused for the NPC's whole lifetime.
    ///
    /// Reuses SacatRagdollBuilder as-is - it's generic (any Humanoid
    /// Animator), not Sacat-specific, despite the name.
    /// </summary>
    public class NpcRagdoll : MonoBehaviour
    {
        private SacatRagdollBuilder.RagdollBone[] _bones;
        private Animator _animator;
        private CharacterController _controller;
        private Transform _hips;
        private bool _built;
        private bool _ragdolled;

        public bool IsRagdolled => _ragdolled;

        public bool IsSettled(float maximumSpeed = 0.7f)
        {
            if (!_ragdolled || _bones == null) return true;
            float limitSqr = maximumSpeed * maximumSpeed;
            foreach (var bone in _bones)
            {
                if (bone.rigidbody != null && bone.rigidbody.linearVelocity.sqrMagnitude > limitSqr)
                    return false;
            }
            return true;
        }

        private void EnsureBuilt()
        {
            if (_built) return;

            _animator = GetComponentInChildren<Animator>();
            _controller = GetComponent<CharacterController>();
            if (_animator == null || !_animator.isHuman) return;

            _bones = SacatRagdollBuilder.Build(_animator);
            _hips = _animator.GetBoneTransform(HumanBodyBones.Hips);
            _built = true;
        }

        /// <summary>Goes dynamic and gives every bone the same one-time
        /// velocity (the vendor's own technique for realistic momentum
        /// transfer, not an explosion of independent forces per bone).
        /// Safe to call on an NPC whose Animator isn't Humanoid or has
        /// no bones found - just does nothing.</summary>
        public void Ragdoll(Vector3 impactVelocity)
        {
            Ragdoll(impactVelocity, transform.position + Vector3.up);
        }

        public void Ragdoll(Vector3 impactVelocity, Vector3 impactPoint)
        {
            EnsureBuilt();
            if (!_built || _ragdolled) return;

            _ragdolled = true;
            if (_animator != null) _animator.enabled = false;
            if (_controller != null) _controller.enabled = false;

            foreach (var b in _bones)
            {
                if (b.rigidbody == null) continue;
                b.rigidbody.isKinematic = false;
                b.collider.isTrigger = false;
                b.rigidbody.linearVelocity = impactVelocity;
            }

            Rigidbody closest = null;
            float closestSqr = float.PositiveInfinity;
            foreach (var b in _bones)
            {
                if (b.rigidbody == null) continue;
                float sqr = (b.rigidbody.worldCenterOfMass - impactPoint).sqrMagnitude;
                if (sqr >= closestSqr) continue;
                closestSqr = sqr;
                closest = b.rigidbody;
            }
            if (closest != null)
                closest.AddForceAtPosition(Vector3.ClampMagnitude(impactVelocity * 0.22f, 2.4f),
                    impactPoint, ForceMode.VelocityChange);
        }

        /// <summary>Reverses Ragdoll() - bones go back to inert
        /// (kinematic/trigger), Animator and CharacterController come
        /// back on. Does NOT reset bone positions explicitly - the
        /// Animator re-poses everything naturally once it resumes
        /// driving them, same as the vendor's own reset.</summary>
        public void Recover()
        {
            Recover(false);
        }

        public void Recover(bool alignRootToRagdoll)
        {
            if (!_ragdolled) return;

            Vector3 ragdollPosition = _hips != null ? _hips.position : transform.position;
            Vector3 ragdollForward = _hips != null
                ? Vector3.ProjectOnPlane(_hips.forward, Vector3.up)
                : Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            _ragdolled = false;

            foreach (var b in _bones)
            {
                if (b.rigidbody == null) continue;
                b.rigidbody.isKinematic = true;
                b.collider.isTrigger = true;
                b.rigidbody.linearVelocity = Vector3.zero;
                b.rigidbody.angularVelocity = Vector3.zero;
            }

            if (alignRootToRagdoll)
            {
                if (Physics.Raycast(ragdollPosition + Vector3.up * 1.5f, Vector3.down,
                    out RaycastHit hit, 8f, ~0, QueryTriggerInteraction.Ignore))
                    ragdollPosition = hit.point;

                transform.position = ragdollPosition;
                if (ragdollForward.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.LookRotation(ragdollForward.normalized, Vector3.up);
            }

            if (_animator != null) _animator.enabled = true;
            if (_controller != null) _controller.enabled = true;
        }
    }
}
