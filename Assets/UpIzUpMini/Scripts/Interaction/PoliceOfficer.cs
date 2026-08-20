using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Combat;
using UpIzUpMini.Economy;
using UpIzUpMini.Navigation;

namespace UpIzUpMini.Interaction
{
    /// <summary>
    /// Police behaviour: paces a stretch of the Lalay road near the
    /// sellers, and switches to pursuing the player once heat is high.
    ///
    /// Movement goes through a CharacterController.
    ///
    /// MINI-052: steering now routes through NavPathSteerer (real NavMesh
    /// pathing around houses, with a recompute/nearby-point/rotate/relocate
    /// stuck-recovery ladder) instead of the previous short forward-whisker
    /// raycast, which only reacted to an obstacle directly ahead one probe
    /// at a time and had no actual recovery if an officer still got wedged.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PoliceOfficer : MonoBehaviour
    {
        [Header("Patrol")]
        [SerializeField] private Vector3 patrolA;
        [SerializeField] private Vector3 patrolB;
        [SerializeField] private float patrolSpeed = 2.0f;
        [SerializeField] private float arriveDistance = 1.2f;
        [SerializeField] private float pauseAtEndSeconds = 1.2f;

        [Header("Pursuit")]
        [SerializeField] private float chaseHeatThreshold = 45f;
        [SerializeField] private float chaseSpeed = 4.35f;
        [SerializeField] private float giveUpDistance = 45f;
        [SerializeField] private float stopDistance = 2.0f;

        [Header("Police stamina")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float chaseDrainPerSecond = 24f;
        [SerializeField] private float recoveryPerSecond = 28f;
        [SerializeField] private float minimumResumeStamina = 55f;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private float turnSpeed = 8f;
        [SerializeField] private NpcCombatHealth combatHealth;

        private CharacterController _controller;
        private bool _headingToB = true;
        private float _pauseTimer;
        private float _animBlend;
        private float _stamina;
        private bool _exhausted;
        private readonly NavPathSteerer _steerer = new NavPathSteerer();

        public bool IsChasing { get; private set; }
        public float Stamina => _stamina;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _stamina = maxStamina;
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (combatHealth == null) combatHealth = GetComponent<NpcCombatHealth>();
        }

        public void SetPatrol(Vector3 a, Vector3 b)
        {
            patrolA = a;
            patrolB = b;
        }

        private void Update()
        {
            // Lying down after a knockout - NpcCombatHealth is holding a
            // sustained full-body pose on this character. Don't fight it
            // by driving the base locomotion layer or calling SimpleMove
            // on a CharacterController it has already disabled; just wait
            // for it to get back up.
            if (combatHealth != null && combatHealth.IsDown)
            {
                IsChasing = false;
                return;
            }

            float heat = EconomyManager.Instance != null ? EconomyManager.Instance.Heat : 0f;
            var player = CharacterSwitchManager.Instance?.Active?.root;

            float distToPlayer = player != null
                ? Vector3.Distance(transform.position, player.transform.position)
                : float.MaxValue;

            // Chase while heat is up and the player is still in reach.
            // MINI-060: Gardey Zafeh's PoliceImmunity reading - "police
            // would not trouble you" - blocks a new chase outright for its
            // duration.
            bool wantsChase = player != null
                        && heat >= chaseHeatThreshold
                        && distToPlayer <= giveUpDistance
                        && !GardeyZafehBuffState.IsActive(GardeyZafehBuff.PoliceImmunity);

            if (_exhausted)
            {
                _stamina = Mathf.Min(maxStamina, _stamina + recoveryPerSecond * Time.deltaTime);
                if (_stamina >= minimumResumeStamina) _exhausted = false;
                else
                {
                    IsChasing = false;
                    Animate(0f);
                    return;
                }
            }

            IsChasing = wantsChase;

            Vector3? relocate = _steerer.ConsumeRelocation();
            if (relocate.HasValue) Relocate(relocate.Value);

            Vector3 destination;
            float speed;

            if (IsChasing)
            {
                _stamina = Mathf.Max(0f, _stamina - chaseDrainPerSecond * Time.deltaTime);
                if (_stamina <= 0f)
                {
                    _exhausted = true;
                    IsChasing = false;
                    Animate(0f);
                    return;
                }
                destination = player.transform.position;
                speed = chaseSpeed;

                if (distToPlayer <= stopDistance)
                {
                    // Close enough - hold position rather than shoving.
                    Face(player.transform.position);
                    Animate(0f);
                    return;
                }
            }
            else
            {
                _stamina = Mathf.Min(maxStamina, _stamina + recoveryPerSecond * 0.6f * Time.deltaTime);
                if (_pauseTimer > 0f)
                {
                    _pauseTimer -= Time.deltaTime;
                    Animate(0f);
                    return;
                }

                destination = _headingToB ? patrolB : patrolA;
                speed = patrolSpeed;
            }

            Vector3? dir = _steerer.Steer(transform.position, destination, speed, arriveDistance, out bool arrived);

            if (arrived)
            {
                if (!IsChasing)
                {
                    _headingToB = !_headingToB;
                    _pauseTimer = pauseAtEndSeconds;
                }
                Animate(0f);
                return;
            }

            if (!dir.HasValue) { Animate(0f); return; }

            _controller.SimpleMove(dir.Value * speed);
            Face(transform.position + dir.Value);
            Animate(speed);
        }

        /// <summary>CharacterController rejects direct transform writes
        /// while enabled, so it's briefly disabled to relocate.</summary>
        private void Relocate(Vector3 position)
        {
            _controller.enabled = false;
            transform.position = position;
            _controller.enabled = true;
        }

        private void Face(Vector3 worldPoint)
        {
            Vector3 look = worldPoint - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(look), turnSpeed * Time.deltaTime);
        }

        private void Animate(float speed)
        {
            if (animator == null) return;
            _animBlend = Mathf.Lerp(_animBlend, speed, 10f * Time.deltaTime);
            if (_animBlend < 0.01f) _animBlend = 0f;
            animator.SetFloat("Speed", _animBlend);
            animator.SetFloat("MotionSpeed", speed > 0.01f ? 1f : 0f);
            animator.SetBool("Grounded", true);
        }
    }
}
