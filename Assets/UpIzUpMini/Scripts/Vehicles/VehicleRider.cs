using UnityEngine;
using UpIzUpMini.Character;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-066. Puts a humanoid character onto a <see cref="VehicleSeat"/>
    /// and takes them off again, including the IK that pins hands to the
    /// handlebars and feet to the pegs.
    ///
    /// IK approach, and why it changed: the first version built an Animation
    /// Rigging rig (Rig + RigBuilder + TwoBoneIKConstraint) at runtime. In
    /// Play Mode the user reported "the hands come off the handlebar" - a
    /// runtime-constructed RigBuilder is fragile, because the Animator's
    /// playable graph has already been initialised by the time these
    /// components are added and the new rig layers do not reliably take
    /// effect. Replaced with Unity's built-in humanoid goal IK via
    /// OnAnimatorIK, which needs no graph rebuilding, runs every frame after
    /// the animation has been evaluated, and is exactly what AvatarIKGoal
    /// exists for. It requires the Animator layer to have IK Pass enabled -
    /// HumanoidAnimationLayerBuilder now sets that on the FullBodyOverride
    /// layer.
    ///
    /// The riding CLIP still supplies the body's overall attitude; IK only
    /// corrects the four contact points, which is the split the brief asked
    /// for ("Use IK/rig constraints for hands to handlebars, feet to foot
    /// positions... Do not author a unique rider animation system for every
    /// motorcycle").
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class VehicleRider : MonoBehaviour
    {
        [Tooltip("How quickly IK fades in/out when mounting and dismounting, so hands don't snap onto the bars.")]
        [SerializeField] private float ikBlendSeconds = 0.25f;

        [Tooltip("How strongly hands are pinned to the handlebars. 1 = fully.")]
        [Range(0f, 1f)][SerializeField] private float handIkWeight = 1f;
        [Tooltip("Feet are pinned more loosely than hands - the pegs are a rest, not a grip, and forcing them hard reads as stiff.")]
        [Range(0f, 1f)][SerializeField] private float footIkWeight = 0.75f;
        [Tooltip("Match the hand's ROTATION to the bar as well as its position, so the wrist isn't twisted oddly.")]
        [Range(0f, 1f)][SerializeField] private float handRotationWeight = 0.7f;

        // KEYFRAMED SEATING (user's own suggestion: "when the character sits
        // on the bike you key frame and then during the wheelie you can put
        // the character forward").
        //
        // Why this is the right fix rather than more rotation tweaking: the
        // purchased mocap clip carries its own root placement, authored
        // against whatever bike the mocap studio used - not this one. Parenting
        // the rider to the seat at zero offset therefore drops the body
        // wherever that studio put it, which on the TMAX lands him behind and
        // off the seat. No amount of rotating or IK-stretching fixes a
        // positional mismatch; the body has to be placed explicitly.
        //
        // So: two authored poses, blended by how deep the wheelie is. Seated
        // is where he belongs on the seat normally; wheelie is where he should
        // be once the bike stands up (further forward, up over the bars).
        // Both are plain offsets from the seat anchor, live-tunable in the
        // wheelie tuner so they can be dialled in by eye instead of guessed.
        [Header("Keyframed seating - SEATED")]
        [Tooltip("Where the rider sits relative to the seat anchor while riding normally. +Z is toward the bike's nose, +Y up.")]
        [SerializeField] private Vector3 seatedOffset = new Vector3(0.11f, -0.01f, 0.32f);
        [Tooltip("Extra pitch while seated. Negative leans the rider back, positive leans forward over the bars.")]
        [SerializeField] private float seatedPitch = 0f;

        [Header("Keyframed seating - WHEELIE")]
        [Tooltip("Where the rider should be at a FULL wheelie. Typically further forward (+Z) and up (+Y) so he stays over the bars instead of sliding off the back.")]
        [SerializeField] private Vector3 wheelieOffset = new Vector3(0f, -0.40f, 0.28f);
        [Tooltip("Extra pitch at a full wheelie. Positive leans him forward onto the bars, which is what a real rider does to counterbalance.")]
        [SerializeField] private float wheeliePitch = 22f;
        [Tooltip("1 = the rider's angle tracks the bike's LIVE wheelie angle exactly, so changing the throttle changes his angle in the same instant, up AND coming back down. 0 = he eases over poseBlendSeconds instead, which always lags the bike because it is time-based rather than angle-based.")]
        [Range(0f, 1f)][SerializeField] private float wheelieAngleSync = 1f;


        [Tooltip("Seconds to ease between the seated and wheelie keyframes, so the rider shifts his weight rather than snapping between poses.")]
        [SerializeField] private float poseBlendSeconds = 0.25f;

        [Header("Body lock (handlebar tracking)")]
        // Turned OFF after seeing it in Play Mode. The idea is sound - slide
        // the body so the animation's own grip lands on the real bars - but
        // in practice it dragged the whole torso toward the handlebars and
        // left the rider reclined off the seat, which is why the user said
        // they preferred the seating from BEFORE this was added. The hand IK
        // below already holds the grips on its own, so the body lock was
        // solving a problem that was already solved, at the cost of the pose.
        // Left in place (not deleted) because it is the right mechanism if a
        // future clip's hands sit genuinely far from these bars.
        [Tooltip("How strongly the whole body is slid so the animation's own grip lands on the real handlebars. 0 = off, hands are held by IK alone and the clip keeps its own seating (current setting). 1 = body fully locked to the bars.")]
        [Range(0f, 1f)][SerializeField] private float bodyLockWeight = 0f;
        [Tooltip("Safety cap in metres on that shift. A clip whose hands are wildly out of place should look wrong rather than silently teleport the rider.")]
        [SerializeField] private float maxBodyLockShift = 0.6f;

        private Animator _animator;
        private HumanoidAnimationManager _anim;
        private PlayerController _player;
        private CharacterController _characterController;

        private VehicleSeat _seat;
        private Transform _originalParent;
        private Vector3 _originalLocalScale = Vector3.one;
        private float _ikWeight;
        private float _ikTarget;

        /// <summary>Extra pitch/roll applied to the rider ON TOP of the
        /// seat's own orientation - pitch keeps them from being carried all
        /// the way back during a wheelie, roll trims down how far the lean
        /// clips throw them sideways. Both set by BikeInteractable.</summary>
        private float _extraPitch;
        private float _extraRoll;
        private float _poseBlend;
        private float _poseTarget;

        /// <summary>Multiplies the hand IK weight. Driven above 1 during a
        /// wheelie: the wheelie clip is the pack's "cheer01", which lifts an
        /// arm off the bars, so the hands need pulling back harder than the
        /// normal riding pose requires (user: "for the wheelie make his right
        /// hand go more on the handle bar").</summary>
        private float _handIkBoost = 1f;

        public bool IsMounted => _seat != null;
        public VehicleSeat CurrentSeat => _seat;

        // Live-tunable keyframes for TmaxWheelieTuner, so the seating can be
        // dialled in by eye during a real wheelie rather than guessed at and
        // rebuilt each time.
        public float SeatedForward { get => seatedOffset.z; set => seatedOffset.z = value; }
        public float SeatedUp { get => seatedOffset.y; set => seatedOffset.y = value; }
        public float SeatedSide { get => seatedOffset.x; set => seatedOffset.x = value; }
        public float SeatedPitch { get => seatedPitch; set => seatedPitch = value; }
        public float WheelieForward { get => wheelieOffset.z; set => wheelieOffset.z = value; }
        public float WheelieUp { get => wheelieOffset.y; set => wheelieOffset.y = value; }
        public float WheelieSide { get => wheelieOffset.x; set => wheelieOffset.x = value; }
        public float WheeliePitch { get => wheeliePitch; set => wheeliePitch = value; }
        /// <summary>Current 0-1 blend between the two keyframes - shown in the
        /// tuner's readout so it is obvious which pose is active.</summary>
        public float PoseBlendSeconds { get => poseBlendSeconds; set => poseBlendSeconds = value; }
        public float WheelieAngleSync { get => wheelieAngleSync; set => wheelieAngleSync = value; }
        public float PoseBlend => _poseBlend;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _anim = GetComponent<HumanoidAnimationManager>();
            _player = GetComponent<PlayerController>();
            _characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            float step = ikBlendSeconds > 0.001f ? Time.deltaTime / ikBlendSeconds : 1f;
            _ikWeight = Mathf.MoveTowards(_ikWeight, _ikTarget, step);

            if (!IsMounted) return;

            // Ease toward the target blend so the rider shifts his weight
            // rather than snapping between the seated and wheelie keyframes.
            // Two ways to reach the target, blended by wheelieAngleSync:
            //   eased  - a fixed-duration ease. Smooth, but TIME-based, so it
            //            always trails the bike and cannot follow the throttle
            //            changing the angle mid-wheelie.
            //   direct - take the live target as-is, so the rider.s angle is a
            //            function of the BIKE.s current angle and moves with it
            //            exactly, up and back down.
            float poseStep = poseBlendSeconds > 0.001f ? Time.deltaTime / poseBlendSeconds : 1f;
            float eased = Mathf.MoveTowards(_poseBlend, _poseTarget, poseStep);
            _poseBlend = Mathf.Lerp(eased, _poseTarget, wheelieAngleSync);

            ApplyKeyframedPose();
        }

        /// <summary>
        /// Places the rider from the two authored keyframes. This is a
        /// straight positional placement, deliberately: the mocap clip's own
        /// root was authored against a different bike, so the body has to be
        /// put where it belongs rather than nudged with rotations.
        /// </summary>
        private void ApplyKeyframedPose()
        {
            Vector3 pos = Vector3.Lerp(seatedOffset, wheelieOffset, _poseBlend);
            float pitch = Mathf.Lerp(seatedPitch, wheeliePitch, _poseBlend) + _extraPitch;

            transform.localPosition = pos;
            transform.localRotation = Quaternion.Euler(pitch, 0f, _extraRoll);
        }

        public bool Mount(VehicleSeat seat)
        {
            if (seat == null || IsMounted) return false;
            if (seat.IsOccupied && seat.Occupant != gameObject) return false;

            _seat = seat;
            seat.Occupant = gameObject;

            // The CharacterController must go off before reparenting: it
            // fights any externally-imposed transform, which would make the
            // rider drift off the seat while the bike moves.
            if (_player != null) _player.IsControlled = false;
            if (_characterController != null) _characterController.enabled = false;

            _originalParent = transform.parent;
            _originalLocalScale = transform.localScale;
            transform.SetParent(seat.SeatAnchor, worldPositionStays: false);

            // The bike is scaled up (see Mini064TmaxAssetPrep.BikeScale) and
            // the rider is now a child of it, so without this he would be
            // stretched by the same factor - a bigger bike AND a bigger
            // rider, which defeats the point. Divide out the parent's scale
            // so he keeps his true size on a larger bike.
            Vector3 parentScale = seat.SeatAnchor.lossyScale;
            transform.localScale = new Vector3(
                _originalLocalScale.x / Mathf.Max(0.0001f, parentScale.x),
                _originalLocalScale.y / Mathf.Max(0.0001f, parentScale.y),
                _originalLocalScale.z / Mathf.Max(0.0001f, parentScale.z));
            // Start fully in the seated keyframe - no wheelie blend yet.
            _poseBlend = 0f;
            _poseTarget = 0f;
            _extraPitch = 0f;
            _extraRoll = 0f;
            ApplyKeyframedPose();

            _ikTarget = 1f;

            if (_anim != null)
            {
                _anim.PlayAction(seat.MountActionId);
                _anim.BeginSustainedAction(seat.RidePoseActionId, startTimeSeconds: seat.RidePoseStartTimeSeconds);
            }

            return true;
        }

        public bool Dismount(Vector3 exitPosition)
        {
            if (!IsMounted) return false;

            var seat = _seat;
            _seat = null;
            seat.Occupant = null;

            _ikTarget = 0f;
            _extraPitch = 0f;
            if (_anim != null) _anim.EndSustainedAction();

            transform.SetParent(_originalParent, worldPositionStays: true);
            transform.position = exitPosition;
            transform.localRotation = Quaternion.identity;
            // Undo the scale compensation applied on mount.
            transform.localScale = _originalLocalScale;

            if (_characterController != null) _characterController.enabled = true;
            if (_player != null) _player.IsControlled = true;

            return true;
        }

        /// <summary>
        /// Leans the rider forward relative to the seat. Used during a
        /// wheelie: the rider is parented to the bike, so without this they
        /// are carried all the way back with it and end up lying almost
        /// horizontal (user: "the wheeling goes too much back"). A real rider
        /// leans FORWARD over the bars to counterbalance a wheelie, so this
        /// is also the physically right shape.
        /// </summary>
        /// <summary>
        /// Drives the keyframed seating. Pass how deep the wheelie is,
        /// 0 = fully seated, 1 = full wheelie; the rider eases between the
        /// two authored poses. Because the caller feeds this the bike's LIVE
        /// wheelie amount, the return journey needs no separate code - as the
        /// bike comes down the same blend simply runs backward, so the rider
        /// can never end up out of sync with the bike the way the earlier
        /// independent-rotation approach did.
        /// </summary>
        public void SetWheelieBlend(float wheelie01)
        {
            if (!IsMounted) return;
            _poseTarget = Mathf.Clamp01(wheelie01);
        }

        /// <summary>Kept for the older call site - additive pitch on top of
        /// the keyframed pose. Normally left at 0 now that the keyframes
        /// carry the lean themselves.</summary>
        public void SetExtraPitch(float degrees)
        {
            if (!IsMounted) return;
            _extraPitch = degrees;
        }

        /// <summary>Counter-rolls the rider, trimming how far the lean clips
        /// tip them sideways without needing to re-author the clips.</summary>
        public void SetExtraRoll(float degrees)
        {
            if (!IsMounted) return;
            _extraRoll = degrees;
        }

        /// <summary>Scales hand IK strength - see _handIkBoost.</summary>
        public void SetHandIkBoost(float boost)
        {
            _handIkBoost = Mathf.Max(0f, boost);
        }

        /// <summary>
        /// Unity's humanoid goal IK. Runs after the animation is evaluated,
        /// so it corrects the clip's hand/foot placement onto this specific
        /// bike's measured anchors rather than replacing the pose.
        /// </summary>
        private void OnAnimatorIK(int layerIndex)
        {
            if (_animator == null || _ikWeight <= 0.001f || _seat == null) return;

            // Lock the BODY to the handlebars first, then pin the hands.
            //
            // User: "isnt there a way to map the animation to the handle bar
            // and lock it there?" - and yes, this is the better way round.
            // Everything before this rotated the rider's body and hoped the
            // arms would follow, which meant hand-guessing a counter-lean
            // angle per pose and getting the sign wrong twice.
            //
            // Instead: measure where THIS animation frame actually puts the
            // hands, measure where the bars are, and slide the whole body by
            // the difference (Animator.bodyPosition is writable inside
            // OnAnimatorIK). The animation keeps its own posture and timing
            // completely intact - it is simply positioned so its grip lands
            // on the real grips. Any clip then works without per-clip tuning,
            // including the cheer/wheelie pose the user wants to keep, and it
            // stays locked for the whole wheelie because it is recomputed
            // every frame rather than being a fixed offset.
            LockBodyToHandlebars();

            float hand = Mathf.Clamp01(handIkWeight * _handIkBoost);
            ApplyGoal(AvatarIKGoal.LeftHand, _seat.LeftHandTarget, hand, handRotationWeight);
            ApplyGoal(AvatarIKGoal.RightHand, _seat.RightHandTarget, hand, handRotationWeight);
            ApplyGoal(AvatarIKGoal.LeftFoot, _seat.LeftFootTarget, footIkWeight, 0f);
            ApplyGoal(AvatarIKGoal.RightFoot, _seat.RightFootTarget, footIkWeight, 0f);
        }

        private void LockBodyToHandlebars()
        {
            if (bodyLockWeight <= 0.001f) return;

            Transform lt = _seat.LeftHandTarget;
            Transform rt = _seat.RightHandTarget;
            if (lt == null || rt == null) return;

            Transform lh = _animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform rh = _animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (lh == null || rh == null) return;

            Vector3 handMid = (lh.position + rh.position) * 0.5f;
            Vector3 barMid = (lt.position + rt.position) * 0.5f;
            Vector3 offset = barMid - handMid;

            // Cap it so a wildly mismatched clip cannot fling the rider
            // across the scene - past this something is wrong with the setup
            // and silently teleporting the body would hide it.
            if (offset.magnitude > maxBodyLockShift)
                offset = offset.normalized * maxBodyLockShift;

            _animator.bodyPosition += offset * (bodyLockWeight * _ikWeight);
        }

        private void ApplyGoal(AvatarIKGoal goal, Transform target, float weight, float rotWeight)
        {
            // A null target is a deliberate "this seat doesn't pin that limb"
            // - a pillion might hold on with hands but rest their feet freely.
            if (target == null) return;

            float w = weight * _ikWeight;
            _animator.SetIKPositionWeight(goal, w);
            _animator.SetIKPosition(goal, target.position);

            if (rotWeight > 0f)
            {
                _animator.SetIKRotationWeight(goal, rotWeight * _ikWeight);
                _animator.SetIKRotation(goal, target.rotation);
            }
        }
    }
}
