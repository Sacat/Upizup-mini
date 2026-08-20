using UnityEngine;
using UpIzUpMini.Combat;
using UpIzUpMini.Economy;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.Character
{
    /// <summary>
    /// MINI-046. The "other member automatically helps in situation" half
    /// of the user's ask. While this character is the follower (not the
    /// one you're controlling) and not off farming or already down, it
    /// throws its own punches at the nearest police officer already
    /// within range - a second attacker, not a copy of the player's F-key
    /// handling. It does not actively chase a target down; because it
    /// follows the player everywhere, anything harassing the player is
    /// usually already close enough. See SimpleMeleeCombat for the
    /// complementary "stronger together" damage bonus this doesn't itself
    /// grant - that lives on the player's own attack, not here.
    /// </summary>
    public class CompanionCombatAssist : MonoBehaviour
    {
        [SerializeField] private float engageRange = 6f;
        [SerializeField] private float attackRange = 1.65f;
        [SerializeField] private float hitRadius = 0.38f;
        [SerializeField] private float attackArcDegrees = 80f;
        [SerializeField] private float attackWindupSeconds = 0.18f;
        [SerializeField] private float attackActiveSeconds = 0.12f;
        [SerializeField] private float attackRecoverySeconds = 0.4f;
        [SerializeField] private float damage = 30f; // a touch under the player's own 35 - a helper, not a replacement
        [SerializeField] private float cooldown = 0.85f;
        [SerializeField] private float officerRescanSeconds = 0.5f;
        [SerializeField] private HumanoidAnimationManager animationManager;
        [SerializeField] private FollowController followController;
        [SerializeField] private FarmhandController farmhand;
        [SerializeField] private CharacterVitals vitals;

        private float _nextAttack;
        private float _nextRescan;
        private NpcCombatHealth[] _officers = System.Array.Empty<NpcCombatHealth>();
        private NpcCombatHealth _pendingTarget;
        private readonly MeleeSwingTimeline _swing = new MeleeSwingTimeline();

        private void Awake()
        {
            if (animationManager == null) animationManager = GetComponent<HumanoidAnimationManager>();
            if (followController == null) followController = GetComponent<FollowController>();
            if (farmhand == null) farmhand = GetComponent<FarmhandController>();
            if (vitals == null) vitals = GetComponent<CharacterVitals>();
        }

        private void Update()
        {
            if (followController == null || !followController.FollowingEnabled
                                         || farmhand != null && farmhand.IsWorking
                                         || vitals != null && vitals.IsDead)
            {
                _swing.Cancel();
                _pendingTarget = null;
                return;
            }

            if (_swing.IsRunning)
            {
                if (_pendingTarget != null && !_pendingTarget.IsDown)
                    FaceTarget(_pendingTarget.transform.position);
                AdvanceAttack(Time.deltaTime);
                if (_swing.IsRunning) return;
            }

            if (Time.time >= _nextRescan)
            {
                RescanOfficers();
                _nextRescan = Time.time + officerRescanSeconds;
            }

            NpcCombatHealth nearest = FindNearestLiveOfficer(out float nearestDist);
            if (nearest == null || nearestDist > engageRange) return;

            FaceTarget(nearest.transform.position);

            if (nearestDist > attackRange || Time.time < _nextAttack) return;

            _nextAttack = Time.time + cooldown;
            animationManager?.PlayAction(SimpleMeleeCombat.ActionId);
            _pendingTarget = nearest;
            _swing.Begin(BuildProfile());
        }

        public void AdvanceAttack(float deltaSeconds)
        {
            if (!_swing.IsRunning) return;
            if (_swing.Advance(deltaSeconds)) ResolvePendingStrike();
            if (!_swing.IsRunning) _pendingTarget = null;
        }

        private void ResolvePendingStrike()
        {
            NpcCombatHealth expected = _pendingTarget;
            if (expected == null || expected.IsDown) return;

            bool found = MeleeContactResolver.TryFindNearest(
                transform, BuildProfile(), NpcCombatHealth.All,
                (NpcCombatHealth candidate) => candidate == expected && !candidate.IsDown,
                out NpcCombatHealth target);
            if (!found) return;

            target.Hit(damage, transform.forward * 0.8f);
            EconomyManager.Instance?.AddHeat(EconomyManager.MaxHeat);
        }

        private MeleeAttackProfile BuildProfile() => new MeleeAttackProfile(
            attackWindupSeconds, attackActiveSeconds, attackRecoverySeconds,
            0.28f, attackRange, hitRadius, attackArcDegrees);

        private void FaceTarget(Vector3 worldPos)
        {
            Vector3 toTarget = worldPos - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toTarget), 10f * Time.deltaTime);
        }

        private NpcCombatHealth FindNearestLiveOfficer(out float nearestDist)
        {
            NpcCombatHealth nearest = null;
            nearestDist = float.MaxValue;
            foreach (var health in _officers)
            {
                if (health == null || health.IsDown) continue;
                float d = Vector3.Distance(transform.position, health.transform.position);
                if (d < nearestDist) { nearestDist = d; nearest = health; }
            }
            return nearest;
        }

        private void RescanOfficers()
        {
            var npcs = Object.FindObjectsByType<TownNPCInteractable>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var list = new System.Collections.Generic.List<NpcCombatHealth>(npcs.Length);
            foreach (var npc in npcs)
            {
                if (npc.Role != NpcRole.Police) continue;
                var health = npc.GetComponent<NpcCombatHealth>();
                if (health != null) list.Add(health);
            }
            _officers = list.ToArray();
        }
    }
}
