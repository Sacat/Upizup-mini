using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Combat;
using UpIzUpMini.Economy;
using UpIzUpMini.Navigation;

namespace UpIzUpMini.Interaction
{
    public enum PoliceMovementState { Patrol, Chase, Search, Recover, Down }

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
        [SerializeField] private float searchSeconds = 4.5f;
        [SerializeField] private float searchSpeed = 2.6f;

        [Header("Police melee")]
        [SerializeField] private float strikeRange = 1.65f;
        [SerializeField] private float strikeDamage = 12f;
        [SerializeField] private float strikeCooldown = 1.1f;
        [SerializeField] private float strikeWindupSeconds = 0.22f;
        [SerializeField] private float strikeActiveSeconds = 0.12f;
        [SerializeField] private float strikeRecoverySeconds = 0.45f;
        [SerializeField] private float strikeRadius = 0.4f;
        [SerializeField] private float strikeArcDegrees = 85f;

        [Header("Police stamina")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float chaseDrainPerSecond = 24f;
        [SerializeField] private float recoveryPerSecond = 28f;
        [SerializeField] private float minimumResumeStamina = 55f;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private float turnSpeed = 8f;
        [SerializeField] private NpcCombatHealth combatHealth;
        [SerializeField] private HumanoidAnimationManager animationManager;

        private CharacterController _controller;
        private bool _headingToB = true;
        private float _pauseTimer;
        private float _animBlend;
        private float _stamina;
        private bool _exhausted;
        private float _searchUntil;
        private Vector3 _lastKnownPlayerPosition;
        private float _sideSign;
        private float _nextStrike;
        private CharacterVitals _pendingVictim;
        private readonly MeleeSwingTimeline _strike = new MeleeSwingTimeline();
        private readonly NavPathSteerer _steerer = new NavPathSteerer();
        private readonly NpcObstacleJumpMotor _jumpMotor = new NpcObstacleJumpMotor();

        public bool IsChasing { get; private set; }
        public float Stamina => _stamina;
        public PoliceMovementState CurrentState { get; private set; } = PoliceMovementState.Patrol;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _stamina = maxStamina;
            _sideSign = (GetInstanceID() & 1) == 0 ? 1f : -1f;
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (combatHealth == null) combatHealth = GetComponent<NpcCombatHealth>();
            if (animationManager == null) animationManager = GetComponent<HumanoidAnimationManager>();
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
                CurrentState = PoliceMovementState.Down;
                CancelStrike();
                Animate(0f);
                return;
            }

            float heat = EconomyManager.Instance != null ? EconomyManager.Instance.Heat : 0f;
            var player = CharacterSwitchManager.Instance?.Active?.root;

            float distToPlayer = player != null
                ? Vector3.Distance(transform.position, player.transform.position)
                : float.MaxValue;

            // A chase begins only when the officer can actually see the
            // wanted player. Losing sight transitions to a short search at
            // the last known position instead of giving officers wall vision.
            // MINI-060: Gardey Zafeh's PoliceImmunity reading - "police
            // would not trouble you" - blocks a new chase outright for its
            // duration.
            bool eligibleForPursuit = player != null
                        && heat >= chaseHeatThreshold
                        && distToPlayer <= giveUpDistance
                        && !GardeyZafehBuffState.IsActive(GardeyZafehBuff.PoliceImmunity);
            bool canSeePlayer = eligibleForPursuit && HasLineOfSight(player);
            bool wasPursuing = CurrentState == PoliceMovementState.Chase
                               || CurrentState == PoliceMovementState.Search;

            if (canSeePlayer)
            {
                _lastKnownPlayerPosition = player.transform.position;
                _searchUntil = Time.time + searchSeconds;
            }

            if (_exhausted)
            {
                _stamina = Mathf.Min(maxStamina, _stamina + recoveryPerSecond * Time.deltaTime);
                if (_stamina >= minimumResumeStamina) _exhausted = false;
                else
                {
                    IsChasing = false;
                    CurrentState = PoliceMovementState.Recover;
                    CancelStrike();
                    Animate(0f);
                    return;
                }
            }

            if (canSeePlayer)
                CurrentState = PoliceMovementState.Chase;
            else if (eligibleForPursuit && wasPursuing && Time.time < _searchUntil)
                CurrentState = PoliceMovementState.Search;
            else
                CurrentState = PoliceMovementState.Patrol;

            IsChasing = CurrentState == PoliceMovementState.Chase;

            if (!IsChasing)
            {
                CancelStrike();
            }
            else if (_strike.IsRunning)
            {
                AdvancePoliceAttack(Time.deltaTime);
                if (_strike.IsRunning)
                {
                    if (_pendingVictim != null) Face(_pendingVictim.transform.position);
                    Animate(0f);
                    return;
                }
            }

            Vector3? relocate = _steerer.ConsumeRelocation();
            if (relocate.HasValue) Relocate(relocate.Value);

            Vector3 destination;
            float speed;

            if (CurrentState == PoliceMovementState.Chase)
            {
                _stamina = Mathf.Max(0f, _stamina - chaseDrainPerSecond * Time.deltaTime);
                if (_stamina <= 0f)
                {
                    _exhausted = true;
                    IsChasing = false;
                    CurrentState = PoliceMovementState.Recover;
                    Animate(0f);
                    return;
                }
                destination = player.transform.position;
                speed = chaseSpeed;

                if (distToPlayer <= stopDistance)
                {
                    // Close enough: hold position, face the wanted character,
                    // and retaliate through the same timed contact system as
                    // player/companion/gang punches.
                    Face(player.transform.position);
                    TryBeginStrike(player);
                    Animate(0f);
                    return;
                }
            }
            else if (CurrentState == PoliceMovementState.Search)
            {
                _stamina = Mathf.Min(maxStamina, _stamina + recoveryPerSecond * 0.3f * Time.deltaTime);
                destination = _lastKnownPlayerPosition;
                speed = searchSpeed;
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
                if (CurrentState == PoliceMovementState.Patrol)
                {
                    _headingToB = !_headingToB;
                    _pauseTimer = pauseAtEndSeconds;
                }
                Animate(0f);
                return;
            }

            if (!dir.HasValue) { Animate(0f); return; }

            Transform avoidanceTarget = CurrentState == PoliceMovementState.Chase ? player?.transform : null;
            Vector3 safeDirection = dir.Value;
            if (!LocalSteeringSafety.TryDirection(
                    transform, _controller, safeDirection, avoidanceTarget, _sideSign, out safeDirection))
            {
                Animate(0f);
                return;
            }

            Vector3 vertical = _jumpMotor.Step(transform, _controller, safeDirection,
                CurrentState == PoliceMovementState.Chase, Time.deltaTime);
            _controller.Move(safeDirection * speed * Time.deltaTime + vertical);
            Face(transform.position + safeDirection);
            Animate(speed);
        }

        private bool TryBeginStrike(GameObject target)
        {
            if (target == null || _strike.IsRunning || Time.time < _nextStrike) return false;
            CharacterVitals victim = target.GetComponent<CharacterVitals>();
            if (victim == null || victim.IsDead) return false;

            _pendingVictim = victim;
            _nextStrike = Time.time + strikeCooldown;
            animationManager?.PlayAction(SimpleMeleeCombat.ActionId);
            return _strike.Begin(BuildStrikeProfile());
        }

        /// <summary>Advances the police windup/active/recovery timeline.
        /// Public for deterministic batch validation.</summary>
        public void AdvancePoliceAttack(float deltaSeconds)
        {
            if (!_strike.IsRunning) return;
            if (_strike.Advance(deltaSeconds)) ResolvePoliceStrike();
            if (!_strike.IsRunning) _pendingVictim = null;
        }

        private void ResolvePoliceStrike()
        {
            if (_pendingVictim == null || _pendingVictim.IsDead) return;
            if (!MeleeContactResolver.CanHitTarget(
                    transform, _pendingVictim.transform, BuildStrikeProfile())) return;
            _pendingVictim.Damage(strikeDamage);
        }

        private void CancelStrike()
        {
            _strike.Cancel();
            _pendingVictim = null;
        }

        private MeleeAttackProfile BuildStrikeProfile() => new MeleeAttackProfile(
            strikeWindupSeconds, strikeActiveSeconds, strikeRecoverySeconds,
            0.28f, strikeRange, strikeRadius, strikeArcDegrees);

        private bool HasLineOfSight(GameObject player)
        {
            if (player == null) return false;
            Vector3 from = transform.position + Vector3.up * 1.35f;
            Vector3 to = player.transform.position + Vector3.up * 1.1f;
            if (!Physics.Linecast(from, to, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore)) return true;
            Transform seen = hit.transform;
            return seen == player.transform || (seen != null && seen.IsChildOf(player.transform));
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
            animator.SetBool("Grounded", !_jumpMotor.IsAirborne);
            animator.SetBool("Jump", _jumpMotor.IsAirborne);
        }
    }
}
