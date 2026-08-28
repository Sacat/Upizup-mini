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
        private bool _built;
        private bool _ragdolled;

        public bool IsRagdolled => _ragdolled;

        private void EnsureBuilt()
        {
            if (_built) return;

            _animator = GetComponentInChildren<Animator>();
            _controller = GetComponent<CharacterController>();
            if (_animator == null || !_animator.isHuman) return;

            _bones = SacatRagdollBuilder.Build(_animator);
            _built = true;
        }

        /// <summary>Goes dynamic and gives every bone the same one-time
        /// velocity (the vendor's own technique for realistic momentum
        /// transfer, not an explosion of independent forces per bone).
        /// Safe to call on an NPC whose Animator isn't Humanoid or has
        /// no bones found - just does nothing.</summary>
        public void Ragdoll(Vector3 impactVelocity)
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
        }

        /// <summary>Reverses Ragdoll() - bones go back to inert
        /// (kinematic/trigger), Animator and CharacterController come
        /// back on. Does NOT reset bone positions explicitly - the
        /// Animator re-poses everything naturally once it resumes
        /// driving them, same as the vendor's own reset.</summary>
        public void Recover()
        {
            if (!_ragdolled) return;
            _ragdolled = false;

            foreach (var b in _bones)
            {
                if (b.rigidbody == null) continue;
                b.rigidbody.isKinematic = true;
                b.collider.isTrigger = true;
                b.rigidbody.linearVelocity = Vector3.zero;
                b.rigidbody.angularVelocity = Vector3.zero;
            }

            if (_animator != null) _animator.enabled = true;
            if (_controller != null) _controller.enabled = true;
        }
    }
}
