using UnityEngine;

namespace UpIzUpMini.Character
{
    /// <summary>
    /// MINI-045. A lightweight dangle/pendulum for a bone-anchored
    /// accessory - the gold chain, specifically. Bone attachment itself
    /// (Animator.GetBoneTransform) already existed from MINI-022's
    /// CharacterEquipment; this is the "physics" half of the user's
    /// "attach chain/accessories to bones with suitable IK/physics" ask.
    ///
    /// Not IK: true inverse kinematics solves for a bone chain reaching a
    /// target (a hand gripping a weapon, a foot planting on uneven
    /// ground). A passive necklace has nothing to reach for - what reads
    /// as "physical" here is a damped spring that lags the accessory
    /// behind the anchor bone's own movement, which is the standard cheap
    /// technique for this kind of jiggle and the right tool for this job.
    ///
    /// Deliberately velocity-driven rather than acceleration-driven (a
    /// real pendulum responds to acceleration) - it's a simplification
    /// that is far cheaper to reason about and tune, and still reads as
    /// "swings when you move, settles when you stop," which is the whole
    /// visual goal.
    /// </summary>
    public class AccessorySwing : MonoBehaviour
    {
        [SerializeField] private float stiffness = 90f;
        [SerializeField] private float damping = 9f;
        [SerializeField] private float driveFactor = 0.3f;
        [SerializeField] private float maxSwingDistance = 0.045f;

        private Transform _anchor;
        private Vector3 _localOffset;
        private Vector3 _swingOffset;
        private Vector3 _swingVelocity;
        private Vector3 _lastRestWorldPos;
        private bool _initialized;
        private Quaternion _rotationOffset = Quaternion.identity;

        /// <summary>
        /// Call once right after parenting/positioning the accessory.
        /// <paramref name="localOffset"/> is the rest position already set
        /// on the transform (e.g. by CharacterEquipment.PositionOnBone) -
        /// captured here rather than re-derived, so this component doesn't
        /// need to know the per-item offset table.
        /// </summary>
        public void Initialize(Transform anchor, Vector3 localOffset)
            => Initialize(anchor, localOffset, null);

        /// <summary>
        /// MINI-067 overload. <paramref name="worldRotation"/> is the
        /// orientation the accessory should be HOLDING at rest, in world
        /// space - normally the character root's rotation.
        ///
        /// Needed because this rig's bone rest orientations are not
        /// world-aligned (the same trap already documented on Boss C's
        /// necklace, where trusting the neck bone's local axes hung the
        /// chain down by the hip). Copying the bone's rotation outright was
        /// fine for a symmetric ring of spheres and is visibly wrong for a
        /// real modelled chain. The delta is captured once here and
        /// re-applied every frame, so the accessory still follows the bone
        /// exactly - it just does so from the correct starting orientation.
        ///
        /// Passing null keeps the original behaviour (offset = identity).
        /// </summary>
        public void Initialize(Transform anchor, Vector3 localOffset, Quaternion? worldRotation)
        {
            _anchor = anchor;
            _localOffset = localOffset;
            _rotationOffset = anchor != null && worldRotation.HasValue
                ? Quaternion.Inverse(anchor.rotation) * worldRotation.Value
                : Quaternion.identity;
            _lastRestWorldPos = _anchor != null ? _anchor.TransformPoint(localOffset) : transform.position;
            _swingOffset = Vector3.zero;
            _swingVelocity = Vector3.zero;
            _initialized = true;
        }

        private void LateUpdate()
        {
            if (!_initialized || _anchor == null) return;
            if (Time.deltaTime <= 0f) return;

            Vector3 restWorldPos = _anchor.TransformPoint(_localOffset);
            Simulate(Time.deltaTime, restWorldPos);
            transform.position = restWorldPos + _swingOffset;
            transform.rotation = _anchor.rotation * _rotationOffset;
        }

        /// <summary>
        /// The pure spring step, split out from LateUpdate so it can be
        /// driven with an explicit dt and verified against known inputs -
        /// Time.deltaTime is 0 outside Play mode (no frames are actually
        /// ticking), so a test harness calling LateUpdate directly could
        /// never exercise this integration at all otherwise.
        /// </summary>
        public void Simulate(float dt, Vector3 restWorldPos)
        {
            // How fast the rest position itself is moving is the "drive" -
            // the faster the bone moves, the harder the chain gets tugged
            // away from where it's trying to settle.
            Vector3 anchorVelocity = (restWorldPos - _lastRestWorldPos) / dt;
            _lastRestWorldPos = restWorldPos;

            // Semi-implicit (symplectic) Euler: update velocity first, then
            // integrate position from the updated velocity - meaningfully
            // more stable than full explicit Euler for a spring at these
            // stiffness values, without needing a fixed-timestep solver.
            Vector3 force = -_swingOffset * stiffness - _swingVelocity * damping - anchorVelocity * driveFactor;
            _swingVelocity += force * dt;
            _swingOffset += _swingVelocity * dt;

            if (_swingOffset.magnitude > maxSwingDistance)
            {
                _swingOffset = _swingOffset.normalized * maxSwingDistance;
            }
        }

        public Vector3 SwingOffset => _swingOffset;
    }
}
