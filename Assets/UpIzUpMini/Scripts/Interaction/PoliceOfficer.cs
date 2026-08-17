using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Combat;
using UpIzUpMini.Economy;

namespace UpIzUpMini.Interaction
{
    /// <summary>
    /// Police behaviour: paces a stretch of the Lalay road near the
    /// sellers, and switches to pursuing the player once heat is high.
    ///
    /// Movement goes through a CharacterController and steers around
    /// obstacles with a short forward whisker cast, because the previous
    /// straight-line patrol drove officers into houses whenever a building
    /// sat between them and their next waypoint.
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

        [Header("Obstacle avoidance")]
        [SerializeField] private float whiskerLength = 2.4f;
        [SerializeField] private float avoidTurnDegrees = 55f;
        [SerializeField] private LayerMask obstacleMask = ~0;

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
            bool wantsChase = player != null
                        && heat >= chaseHeatThreshold
                        && distToPlayer <= giveUpDistance;

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

                Vector3 flat = destination - transform.position;
                flat.y = 0f;
                if (flat.magnitude <= arriveDistance)
                {
                    _headingToB = !_headingToB;
                    _pauseTimer = pauseAtEndSeconds;
                    Animate(0f);
                    return;
                }

                speed = patrolSpeed;
            }

            Vector3 dir = destination - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) { Animate(0f); return; }
            dir.Normalize();

            dir = AvoidObstacles(dir);

            _controller.SimpleMove(dir * speed);
            Face(transform.position + dir);
            Animate(speed);
        }

        /// <summary>
        /// Steers around whatever is directly ahead by testing a short cast
        /// forward, then to each side, and taking the first clear heading.
        /// Without this officers walked into houses on the way to a
        /// waypoint.
        /// </summary>
        private Vector3 AvoidObstacles(Vector3 desired)
        {
            Vector3 origin = transform.position + Vector3.up * 1.0f;

            if (!Physics.Raycast(origin, desired, whiskerLength, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                return desired;
            }

            // Try progressively wider turns to each side.
            for (int step = 1; step <= 3; step++)
            {
                float angle = avoidTurnDegrees * step;

                Vector3 left = Quaternion.Euler(0f, -angle, 0f) * desired;
                if (!Physics.Raycast(origin, left, whiskerLength, obstacleMask, QueryTriggerInteraction.Ignore))
                {
                    return left;
                }

                Vector3 right = Quaternion.Euler(0f, angle, 0f) * desired;
                if (!Physics.Raycast(origin, right, whiskerLength, obstacleMask, QueryTriggerInteraction.Ignore))
                {
                    return right;
                }
            }

            // Boxed in - back off rather than grinding into the wall.
            return -desired;
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
