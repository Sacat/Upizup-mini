using UnityEngine;
using UpIzUpMini.Combat;

namespace UpIzUpMini.Character
{
    /// <summary>
    /// MINI-058: assignment states for a recruited Not Ah Word member, per
    /// the roadmap brief's list (minus "later errands", which the brief
    /// itself defers).
    /// </summary>
    public enum GangAssignment { Follow, GuardPlantation, StayAtHome, Unavailable }

    /// <summary>
    /// Movement/state driver for one recruited gang member. Deliberately
    /// modelled on FollowController (same animator-blend pattern) rather
    /// than sharing a base class with it - the companion boy
    /// (FollowController) always has exactly one behaviour (follow the
    /// active boy); a gang member has four, including two fixed-
    /// destination ones FollowController never needed. Forking here keeps
    /// FollowController's proven, already content-critical logic untouched.
    ///
    /// Reverted from MINI-052's NavPathSteerer-based steering back to
    /// simple direct steering, same as FollowController's own MINI-060-
    /// followup revert - "characters not following me properly, they are
    /// glitching especially by a hill i prefer the old follow system."
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class GangMemberController : MonoBehaviour
    {
        [SerializeField] private float followDistance = 4f;
        [SerializeField] private float arriveDistance = 1.5f;
        [SerializeField] private float moveSpeed = 2.0f;
        [SerializeField] private float turnSpeed = 8f;
        [SerializeField] private Animator animator;
        [SerializeField] private string speedParam = "Speed";
        [SerializeField] private Vector3 guardPosition;
        [SerializeField] private Vector3 homePosition;

        private CharacterController _controller;
        private float _animSpeedBlend;
        private NpcCombatHealth _combatHealth;
        private float _sideSign;
        private float _blockedFor;

        public string MemberName = "Recruit";
        public GangAssignment Assignment { get; private set; } = GangAssignment.Follow;
        public Transform FollowTarget { get; set; }

        /// <summary>True once recruited and active in the world - the
        /// recruiter NPC uses this to find the next free pool slot.</summary>
        public bool IsRecruited { get; private set; }

        // MINI-059: lets PlantationTheftController ask "is anyone on
        // GuardPlantation duty right now" without every recruit needing to
        // register itself with that specific system - same pattern as
        // InteractableBase.All.
        public static readonly System.Collections.Generic.List<GangMemberController> All
            = new System.Collections.Generic.List<GangMemberController>();

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            _combatHealth = GetComponent<NpcCombatHealth>();
            _sideSign = (GetInstanceID() & 1) == 0 ? 1f : -1f;
        }

        /// <summary>
        /// MINI-073: true while this member is riding as a passenger. Freezes
        /// their own AI movement/animation entirely - otherwise they would try
        /// to walk toward FollowTarget while parented inside a moving vehicle,
        /// fighting the vehicle's own transform every frame the same way a live
        /// CharacterController does (the exact problem BikeInteractable and
        /// CarInteractable both disable the rider's CharacterController for).
        /// </summary>
        public bool IsRiding { get; set; }

        public void SetGuardPosition(Vector3 pos) => guardPosition = pos;
        public void SetHomePosition(Vector3 pos) => homePosition = pos;

        public void Recruit(Transform followTarget)
        {
            IsRecruited = true;
            FollowTarget = followTarget;
            Assignment = GangAssignment.Follow;
        }

        /// <summary>Cycles Follow -> GuardPlantation -> StayAtHome -> Follow.
        /// Refuses while Unavailable - see GangMemberInteractable.</summary>
        /// <summary>
        /// MINI-069: forces this member back onto Follow. Used by the phone
        /// call, which means "regroup on me" - without it a member on guard or
        /// home duty would be teleported to the player and then immediately
        /// walk back to his post, making the call look broken.
        /// </summary>
        public void SetAssignmentFollow()
        {
            if (Assignment == GangAssignment.Unavailable) return;   // someone who is down stays down
            Assignment = GangAssignment.Follow;
        }

        public void CycleAssignment()
        {
            Assignment = Assignment switch
            {
                GangAssignment.Follow => GangAssignment.GuardPlantation,
                GangAssignment.GuardPlantation => GangAssignment.StayAtHome,
                _ => GangAssignment.Follow
            };
        }

        private void Update()
        {
            if (!IsRecruited || IsRiding) return;

            // Real combat integration, not a cosmetic flag: a member
            // actually knocked down in a fight (NpcCombatHealth, MINI-038)
            // becomes Unavailable on their own, and the interactable
            // refuses to reassign them until they recover - "injured" is
            // something that can really happen, not a manually-picked menu
            // option pretending to be one.
            if (_combatHealth != null && _combatHealth.IsDown)
            {
                if (Assignment != GangAssignment.Unavailable) Assignment = GangAssignment.Unavailable;
                Animate(0f);
                return;
            }
            if (Assignment == GangAssignment.Unavailable && (_combatHealth == null || !_combatHealth.IsDown))
            {
                // Recovered - hand back to Follow rather than leaving them
                // stuck Unavailable forever with nothing to clear it.
                Assignment = GangAssignment.Follow;
            }

            Vector3? target = Assignment switch
            {
                GangAssignment.Follow => FollowTarget != null ? FollowTarget.position : (Vector3?)null,
                GangAssignment.GuardPlantation => guardPosition,
                GangAssignment.StayAtHome => homePosition,
                _ => null
            };

            if (target == null) { Animate(0f); return; }

            float arrive = Assignment == GangAssignment.Follow ? followDistance : arriveDistance;

            Vector3 toTarget = target.Value - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            if (dist > arrive)
            {
                Vector3 dir = toTarget.normalized;
                if (!LocalSteeringSafety.TryDirection(transform, _controller, dir, FollowTarget, _sideSign, out dir))
                {
                    Animate(0f);
                    _blockedFor += Time.deltaTime;
                    if (_blockedFor >= 1.2f)
                    {
                        _sideSign *= -1f;
                        _blockedFor = 0f;
                    }
                    return;
                }
                _blockedFor = 0f;
                float speed = Assignment == GangAssignment.Follow && dist > arrive * 3f ? 5.335f : moveSpeed;
                _controller.SimpleMove(dir * speed);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
                Animate(speed);
            }
            else
            {
                Animate(0f);
            }
        }

        private void Animate(float speed)
        {
            if (animator == null) return;
            _animSpeedBlend = Mathf.Lerp(_animSpeedBlend, speed, 10f * Time.deltaTime);
            if (_animSpeedBlend < 0.01f) _animSpeedBlend = 0f;
            animator.SetFloat(speedParam, _animSpeedBlend);
            animator.SetFloat("MotionSpeed", speed > 0.01f ? 1f : 0f);
            animator.SetBool("Grounded", true);
        }
    }
}
