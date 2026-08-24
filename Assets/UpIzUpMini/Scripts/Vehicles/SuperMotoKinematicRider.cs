using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-119 follow-up, user: "the mounting shouldnt be from a Tpose
    /// it should be from the riding position that works with the bike"
    /// + the referenced RayznGames "Easy Bike System" tutorial/asset
    /// (already sitting in this project, unused, at
    /// Assets/RayznGames/BicycleSystem). Read that asset's own source
    /// directly (BikeIKTargets.cs) rather than guessing from the video:
    /// its whole technique is the Animator staying ON (a real pose, never
    /// disabled) with Unity's Animation Rigging package
    /// (com.unity.animation.rigging, already installed in this project)
    /// pulling hands/feet onto the bike's own anchor Transforms every
    /// frame via TwoBoneIKConstraint. No physics ragdoll for normal
    /// riding at all - that's exactly why SuperMotoRagdollRider produced
    /// a T-pose clash (it disabled the Animator and only posed 5 of ~15
    /// bones before physics took over; spine/elbows/knees were never
    /// posed). Ragdoll physics stays available (SacatRagdollBuilder,
    /// unused here) for an actual crash later, same as the vendor's own
    /// bike already does - just not for normal riding.
    ///
    /// Replaces SuperMotoRagdollRider as what SuperMotoStockInteractable
    /// drives. Same public surface (Configure/IsMounted/Dismount) so the
    /// interactable barely changed.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public class SuperMotoKinematicRider : MonoBehaviour
    {
        private GameObject _player;
        private Animator _animator;
        private Transform _seatAnchor, _rightHandTarget, _leftHandTarget, _rightFootTarget, _leftFootTarget;

        private RigBuilder _rigBuilder;
        private Transform _ikRightHand, _ikLeftHand, _ikRightFoot, _ikLeftFoot;
        private bool _built;

        public bool IsMounted => _built;

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

        private int _framesWaited;

        private void Update()
        {
            if (!_built)
            {
                if (_animator == null) return;

                // Same "wait for the real avatar" safety proven necessary
                // earlier this task - animator.avatar reads null when
                // built synchronously in the same frame as spawning.
                _framesWaited++;
                if (_animator.avatar == null)
                {
                    if (_framesWaited > 300)
                    {
                        Debug.LogError("MINI-119 KINEMATIC RIDER: animator.avatar never became non-null after 300 frames - giving up.");
                        _built = true;
                    }
                    return;
                }

                BuildRig();
                _built = true;
                return;
            }

            // Live every frame while mounted: same technique as the
            // RayznGames asset's own BikeIKTargets.cs - just move the IK
            // target Transforms onto the bike's own anchors, the
            // TwoBoneIKConstraints (built once in BuildRig) do the rest.
            if (_ikRightHand != null) _ikRightHand.SetPositionAndRotation(_rightHandTarget.position, _rightHandTarget.rotation);
            if (_ikLeftHand != null) _ikLeftHand.SetPositionAndRotation(_leftHandTarget.position, _leftHandTarget.rotation);
            if (_ikRightFoot != null) _ikRightFoot.SetPositionAndRotation(_rightFootTarget.position, _rightFootTarget.rotation);
            if (_ikLeftFoot != null) _ikLeftFoot.SetPositionAndRotation(_leftFootTarget.position, _leftFootTarget.rotation);
        }

        private void BuildRig()
        {
            var characterController = _player.GetComponent<CharacterController>();
            var playerController = _player.GetComponent<Character.PlayerController>();
            if (playerController != null) playerController.IsControlled = false;
            if (characterController != null) characterController.enabled = false;

            // Animator stays ON on purpose - unlike the old ragdoll
            // approach, there is no pose to build from scratch here. The
            // character keeps playing whatever pose its own controller
            // leaves it in (idle) and the IK constraints below pull hands/
            // feet onto the bike regardless of that base pose - no T-pose
            // is possible because the Animator never stops driving the
            // skeleton.

            var rigGo = new GameObject("SuperMotoIKRig");
            rigGo.transform.SetParent(_player.transform, false);
            var rig = rigGo.AddComponent<Rig>();
            rig.weight = 1f;

            _ikRightHand = BuildLimb(rigGo.transform, "RightArm", HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand);
            _ikLeftHand = BuildLimb(rigGo.transform, "LeftArm", HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand);
            _ikRightFoot = BuildLimb(rigGo.transform, "RightLeg", HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot);
            _ikLeftFoot = BuildLimb(rigGo.transform, "LeftLeg", HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot);

            _rigBuilder = _player.GetComponent<RigBuilder>();
            if (_rigBuilder == null) _rigBuilder = _player.AddComponent<RigBuilder>();
            _rigBuilder.layers.Add(new RigLayer(rig, true));
            _rigBuilder.Build();

            Debug.Log($"MINI-119 KINEMATIC RIDER: IK rig built after waiting {_framesWaited} frame(s) for animator.avatar.");
        }

        /// <summary>Builds one TwoBoneIKConstraint under rigParent for a
        /// root/mid/tip bone chain, and a free-standing world-space
        /// Transform as its target (moved every frame in Update to track
        /// the bike's own anchor) - returns that target Transform.</summary>
        private Transform BuildLimb(Transform rigParent, string label, HumanBodyBones root, HumanBodyBones mid, HumanBodyBones tip)
        {
            Transform rootT = _animator.GetBoneTransform(root);
            Transform midT = _animator.GetBoneTransform(mid);
            Transform tipT = _animator.GetBoneTransform(tip);
            if (rootT == null || midT == null || tipT == null)
            {
                Debug.LogError($"MINI-119 KINEMATIC RIDER: missing {label} bone(s) on Sacat's Animator - IK not built for this limb.");
                return null;
            }

            var targetGo = new GameObject($"IKTarget_{label}");
            targetGo.transform.SetParent(rigParent, false);
            targetGo.transform.SetPositionAndRotation(tipT.position, tipT.rotation);

            var constraintGo = new GameObject($"IK_{label}");
            constraintGo.transform.SetParent(rigParent, false);
            var constraint = constraintGo.AddComponent<TwoBoneIKConstraint>();
            constraint.data.root = rootT;
            constraint.data.mid = midT;
            constraint.data.tip = tipT;
            constraint.data.target = targetGo.transform;
            constraint.data.targetPositionWeight = 1f;
            constraint.data.targetRotationWeight = 1f;
            constraint.data.hintWeight = 0f; // no elbow/knee pole target yet - plain two-bone solve

            return targetGo.transform;
        }

        /// <summary>MINI-119 follow-up, same fall-through fix as before:
        /// fully reverses BuildRig so an inactive mounted character isn't
        /// left in a broken state if switched away from.</summary>
        public void Dismount(Vector3 exitPosition)
        {
            // MINI-119 follow-up fix, user: "the character cant mount on
            // the bike and the scale of the character is increasing...
            // doesnt get into the pose." Real cause: this used to
            // early-return here if the IK rig hadn't finished building
            // yet (BuildRig waits a few frames for animator.avatar) -
            // meaning a dismount pressed in that window left the player
            // still parented under the bike's seat with its
            // already-divided scale, while the OUTER
            // SuperMotoStockInteractable.Dismount() went ahead anyway and
            // handed control back. Every next F-press then called Mount()
            // again, re-dividing the ALREADY-divided scale by the bike's
            // scale a second time - compounding on every retry, hence
            // the character growing huge. The transform/scale/control
            // restore below must always run regardless of whether the
            // rig itself finished building - only the rig cleanup is
            // conditional.
            if (_rigBuilder != null) Object.DestroyImmediate(_rigBuilder);
            var rig = _player.transform.Find("SuperMotoIKRig");
            if (rig != null) Object.DestroyImmediate(rig.gameObject);
            _ikRightHand = _ikLeftHand = _ikRightFoot = _ikLeftFoot = null;

            _player.transform.SetParent(null, true);
            _player.transform.position = exitPosition;
            _player.transform.rotation = Quaternion.identity;
            _player.transform.localScale = Vector3.one;

            var characterController = _player.GetComponent<CharacterController>();
            if (characterController != null) characterController.enabled = true;

            _built = false;
            _framesWaited = 0;
        }
    }
}
