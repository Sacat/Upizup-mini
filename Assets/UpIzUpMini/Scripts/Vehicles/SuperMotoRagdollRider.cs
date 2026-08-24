using Gadd420;
using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-119 follow-up, user: "well forget about sacat animation, use
    /// a ragdoll system for the bike... yow ragdoll ragdoll ragdoll dont
    /// change this be stricton implementing this on the bike." Full
    /// physics ragdoll riding, no Animator/IK involved at all: Sacat's
    /// Animator is disabled outright, his real ragdoll (SacatRagdollBuilder)
    /// is built and made dynamic immediately (not lazily on crash - it
    /// IS the riding mechanism now), and his hips/hands/feet are
    /// PHYSICALLY joined (ConfigurableJoint) to the bike's own existing
    /// anchor points (the same RightHandPos/LeftHandPos/RightFootPos/
    /// LeftFootPos/seat anchors already used elsewhere in this task) -
    /// real physics holds him there, not a per-frame position write, so
    /// this can never silently under-apply the way Animator-dependent IK
    /// did.
    ///
    /// The bike's own anchor points (RightHandPos etc.) have no
    /// Rigidbody of their own - they're plain kinematically-rotated
    /// Transforms (confirmed by reading the prefab directly, not
    /// assumed). A Joint needs a real Rigidbody on both ends, so each
    /// anchor gets one added here, set kinematic - a kinematic
    /// Rigidbody is a valid, correct Joint target and faithfully
    /// transmits whatever motion (e.g. the fork's own steering rotation)
    /// is applied to its Transform.
    ///
    /// Same "wait for the real avatar" safety already proven necessary
    /// for the vendor-IK attempt (animator.avatar read null when built
    /// synchronously in the same frame as spawning) - retries every
    /// Update() until ready rather than assuming.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public class SuperMotoRagdollRider : MonoBehaviour
    {
        [Tooltip("Position spring strength pulling hands/feet/hips toward their anchor - not a hard lock, so the ragdoll still reads as physically simulated rather than teleported.")]
        public float jointSpring = 8000f;
        public float jointDamper = 400f;
        [Tooltip("Hard cap on the correction force regardless of spring/distance - a defensive limit against exactly the kind of one-step velocity spike that threw the character off the map before the pose-first fix. Deliberately far below the spring's own theoretical max output.")]
        public float jointMaxForce = 6000f;

        private GameObject _player;
        private Animator _animator;
        private Transform _seatAnchor, _rightHandTarget, _leftHandTarget, _rightFootTarget, _leftFootTarget;
        private int _framesWaited;
        private bool _built;

        public void Configure(GameObject player, Transform seatAnchor,
            Transform rightHandTarget, Transform leftHandTarget, Transform rightFootTarget, Transform leftFootTarget)
        {
            _player = player;
            _animator = player.GetComponent<Animator>();
            _seatAnchor = seatAnchor;
            _rightHandTarget = rightHandTarget;
            _leftHandTarget = leftHandTarget;
            _rightFootTarget = rightFootTarget;
            _leftFootTarget = leftFootTarget;
        }

        private float _logTimer;

        private void Update()
        {
            if (!_built)
            {
                if (_animator == null) return;

                _framesWaited++;
                if (_animator.avatar == null)
                {
                    if (_framesWaited > 300)
                    {
                        Debug.LogError("MINI-119 RAGDOLL RIDER: animator.avatar never became non-null after 300 frames - giving up.");
                        _built = true; // stop retrying
                    }
                    return;
                }

                BuildAndPin();
                _built = true;
                return;
            }

            // MINI-119 follow-up, user: "mission failed fell off playable
            // area do some research first." A running position log makes
            // an explosion visible (or ruled out) in the real Player.log
            // from a headless build run, sidestepping the Editor-only
            // Animator/Avatar-binding gap entirely.
            _logTimer += Time.deltaTime;
            if (_logTimer >= 1f)
            {
                _logTimer = 0f;
                Debug.Log($"MINI-119 RAGDOLL RIDER: t={Time.time:F1}s playerPos={_player.transform.position}");
            }
        }

        private void BuildAndPin()
        {
            var characterController = _player.GetComponent<CharacterController>();
            var playerController = _player.GetComponent<Character.PlayerController>();
            if (playerController != null) playerController.IsControlled = false;
            if (characterController != null) characterController.enabled = false;

            Transform hips = _animator.GetBoneTransform(HumanBodyBones.Hips);
            Transform rightHand = _animator.GetBoneTransform(HumanBodyBones.RightHand);
            Transform leftHand = _animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform rightFoot = _animator.GetBoneTransform(HumanBodyBones.RightFoot);
            Transform leftFoot = _animator.GetBoneTransform(HumanBodyBones.LeftFoot);

            // MINI-119 follow-up fix, user report: "mission failed fell
            // off playable area." Real, worked-out cause: building the
            // ragdoll from whatever raw pose the bones happened to be in
            // (the Animator was already off, so likely a bind/T-pose)
            // then joining hand/foot bones straight to the bike anchors
            // with a stiff spring meant closing a potentially large gap
            // (tens of centimetres) in a single physics step - spring
            // force scales with distance, so a 8000 spring across even a
            // 0.3m gap on a ~2kg bone works out to roughly 20-30 m/s of
            // velocity change in ONE step. That is an explosion, not a
            // guess anymore.
            //
            // Fix: pose the limbs onto the real targets FIRST, using the
            // vendor's own kinematic IK.cs as a one-time posing tool (not
            // an ongoing system) - run its solve several times so it
            // converges, then remove it - THEN build the ragdoll from
            // that already-correct pose and pin it. The joint gap at the
            // moment physics takes over is now ~0, so the same spring
            // only ever has to resist small ongoing disturbances
            // (steering, bumps), never a full-distance snap.
            PoseOntoTargets(rightHand, _rightHandTarget);
            PoseOntoTargets(leftHand, _leftHandTarget);
            PoseOntoTargets(rightFoot, _rightFootTarget);
            PoseOntoTargets(leftFoot, _leftFootTarget);
            if (hips != null && _seatAnchor != null)
            {
                hips.position = _seatAnchor.position;
                hips.rotation = _seatAnchor.rotation;
            }

            _animator.enabled = false; // physics drives the bones now, not the Animator

            var bones = SacatRagdollBuilder.Build(_animator);
            foreach (var b in bones)
            {
                if (b.rigidbody != null) b.rigidbody.isKinematic = false;
                if (b.collider != null) b.collider.isTrigger = false;
            }

            PinToAnchor(hips, _seatAnchor);
            PinToAnchor(rightHand, _rightHandTarget);
            PinToAnchor(leftHand, _leftHandTarget);
            PinToAnchor(rightFoot, _rightFootTarget);
            PinToAnchor(leftFoot, _leftFootTarget);

            Debug.Log($"MINI-119 RAGDOLL RIDER: built and pinned after waiting {_framesWaited} frame(s) for animator.avatar.");
        }

        /// <summary>Temporarily attaches the vendor's own Gadd420.IK
        /// solver to reach from the current bone toward the target,
        /// runs its solve a few times so it actually converges, then
        /// removes it - a one-shot posing tool, not left running.</summary>
        private void PoseOntoTargets(Transform bone, Transform target)
        {
            if (bone == null || target == null) return;

            var ik = bone.gameObject.AddComponent<Gadd420.IK>();
            ik.chainLength = 2;
            ik.target = target;
            ik.iterations = 10;

            var lateUpdate = typeof(Gadd420.IK).GetMethod("LateUpdate",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            for (int i = 0; i < 5; i++) lateUpdate?.Invoke(ik, null);

            Object.DestroyImmediate(ik);
        }

        /// <summary>Ensures the anchor has a (kinematic) Rigidbody - it's
        /// a plain Transform on the vendor's own rig otherwise - then
        /// joints the given ragdoll bone to it with a strong position
        /// spring. Angular motion stays free so the limb can still hang/
        /// articulate naturally off that one pinned point, same as a
        /// real marionette.</summary>
        private void PinToAnchor(Transform bone, Transform anchor)
        {
            if (bone == null || anchor == null) return;

            var anchorRb = anchor.GetComponent<Rigidbody>();
            if (anchorRb == null)
            {
                anchorRb = anchor.gameObject.AddComponent<Rigidbody>();
                anchorRb.isKinematic = true;
            }

            var boneRb = bone.GetComponent<Rigidbody>();
            if (boneRb == null) return; // ragdoll build must have failed for this bone - nothing to joint

            var joint = bone.gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = anchorRb;
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = Vector3.zero;
            joint.connectedAnchor = Vector3.zero;

            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.yMotion = ConfigurableJointMotion.Limited;
            joint.zMotion = ConfigurableJointMotion.Limited;
            joint.linearLimit = new SoftJointLimit { limit = 0.02f };

            var drive = new JointDrive { positionSpring = jointSpring, positionDamper = jointDamper, maximumForce = jointMaxForce };
            joint.xDrive = drive;
            joint.yDrive = drive;
            joint.zDrive = drive;
            joint.targetPosition = Vector3.zero;

            joint.angularXMotion = ConfigurableJointMotion.Free;
            joint.angularYMotion = ConfigurableJointMotion.Free;
            joint.angularZMotion = ConfigurableJointMotion.Free;
        }
    }
}
