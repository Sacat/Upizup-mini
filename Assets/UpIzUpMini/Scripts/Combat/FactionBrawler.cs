using System.Collections.Generic;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Progression;

namespace UpIzUpMini.Combat
{
    /// <summary>
    /// MINI-069. The Not Ah Word / Dog Life street fight.
    ///
    /// One component drives BOTH sides - the player's crew (the companion boy
    /// and any recruited members) and Dog Life themselves, per the user's "dont
    /// forget doglife should fight back as well". A single class rather than an
    /// attacker and a separate retaliator, because the behaviour is symmetric:
    /// see someone from the other side, close, swing. Having one side written
    /// twice is how the two drift out of step.
    ///
    /// It reuses the existing punch (<see cref="SimpleMeleeCombat.ActionId"/>)
    /// on the user's instruction - "you have my character and the other main
    /// character punching so you can use that animation for fights even the
    /// police for now" - so no new animation work is needed.
    ///
    /// Deliberately NOT merged into CompanionCombatAssist: that one is about
    /// police and carries its own heat side effects, and a gang scrap should not
    /// call the police on you.
    /// </summary>
    public class FactionBrawler : MonoBehaviour
    {
        public enum Side
        {
            /// <summary>The player's crew - the companion boy and recruits.</summary>
            NotAhWord,
            /// <summary>The rival gang.</summary>
            DogLife
        }

        [SerializeField] private Side side = Side.NotAhWord;
        [Tooltip("How far away an enemy is noticed in the first place.")]
        [SerializeField] private float engageRange = 8f;
        [Tooltip("Once a fight has started, how far this fighter will PURSUE before giving up. Larger than engageRange - that is what makes it a chase rather than a shove at arm's length.")]
        [SerializeField] private float chaseRange = 22f;
        [Tooltip("Stop fighting once this many or fewer of the enemy are still standing. 1 = the user's rule: when the rival gang is down to its last man, everyone backs off.")]
        [SerializeField] private int standDownWhenEnemiesLeft = 1;
        [Tooltip("How close before a punch lands.")]
        [SerializeField] private float attackRange = 2.1f;
        [Tooltip("Walk speed while closing on an enemy. 0 leaves movement to whatever else drives this character.")]
        [SerializeField] private float approachSpeed = 2.2f;
        [SerializeField] private float damage = 28f;
        [SerializeField] private float cooldown = 0.9f;

        [SerializeField] private HumanoidAnimationManager animationManager;
        [SerializeField] private CharacterVitals vitals;
        [SerializeField] private NpcCombatHealth npcHealth;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private PlayerController playerController;

        private float _nextAttack;
        private FactionBrawler _chaseTarget;

        /// <summary>Every brawler in the scene, so target selection is a list walk
        /// rather than a FindObjectsByType sweep per fighter per rescan. Same
        /// pattern as GangMemberController.All.</summary>
        public static readonly List<FactionBrawler> All = new List<FactionBrawler>();

        public Side Allegiance { get => side; set => side = value; }

        /// <summary>Down or dead fighters are neither targets nor attackers.</summary>
        public bool IsOutOfAction =>
            (vitals != null && vitals.IsDead) || (npcHealth != null && npcHealth.IsDown);

        private void Awake()
        {
            if (animationManager == null) animationManager = GetComponent<HumanoidAnimationManager>();
            if (vitals == null) vitals = GetComponent<CharacterVitals>();
            if (npcHealth == null) npcHealth = GetComponent<NpcCombatHealth>();
            if (characterController == null) characterController = GetComponent<CharacterController>();
            if (playerController == null) playerController = GetComponent<PlayerController>();
        }

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() => All.Remove(this);

        private void Update()
        {
            // The whole gang war is gated behind the Gardey Zafeh reading, which
            // is what reveals Dog Life as the ones robbing the plantation. Before
            // that the player has no reason to know they are enemies, and two
            // gangs brawling in the street would give the plot away early.
            var progression = ProgressionManager.Instance;
            if (progression == null || !progression.DogLifeRevealed) return;

            if (IsOutOfAction) return;

            // The character the player is actually driving swings manually with
            // F. Auto-punching on their behalf would take the fight out of their
            // hands - the ask was for "my other main character" to fight, i.e.
            // whichever one you are not currently controlling.
            if (playerController != null && playerController.IsControlled) return;

            int alliesStanding = CountStanding(side);
            int enemiesStanding = CountStanding(Opposite(side));
            if (side == Side.DogLife && alliesStanding <= 1 && enemiesStanding > 1)
            {
                RetreatFromLastEnemy();
                return;
            }

            // The user's stand-down rule: "when rival gang remains 1 they stop".
            // Applied symmetrically, so it reads as both sides breaking off
            // rather than one gang mercilessly hunting the last man down. Also
            // stops a fight grinding on forever once it is effectively decided.
            if (enemiesStanding <= standDownWhenEnemiesLeft)
            {
                _chaseTarget = null;
                return;
            }

            FactionBrawler target = FindNearestEnemy(out float dist);
            if (target == null) { _chaseTarget = null; return; }

            // Two different ranges on purpose: an enemy has to come within
            // engageRange to start something, but once this fighter is committed
            // he will follow out to chaseRange. Without the split, a chase ends
            // the instant the target backs off a step.
            bool committed = _chaseTarget != null && _chaseTarget == target && !target.IsOutOfAction;
            float limit = committed ? chaseRange : engageRange;
            if (dist > limit) { _chaseTarget = null; return; }

            _chaseTarget = target;

            FaceTarget(target.transform.position);

            if (dist > attackRange)
            {
                Approach(target.transform.position);
                return;
            }

            if (Time.time < _nextAttack) return;
            _nextAttack = Time.time + cooldown;

            animationManager?.PlayAction(SimpleMeleeCombat.ActionId);
            target.ReceiveHit(damage, transform.forward * 0.8f);
        }

        /// <summary>
        /// Takes a hit through whichever health component this fighter actually
        /// has - the player's crew are CharacterVitals, Dog Life are
        /// NpcCombatHealth. Routing both through one method is what lets the
        /// same brawler hurt either side.
        /// </summary>
        public void ReceiveHit(float amount, Vector3 push)
        {
            if (npcHealth != null) { npcHealth.Hit(amount, push); return; }
            vitals?.Damage(amount);
        }

        private void Approach(Vector3 worldPos)
        {
            if (approachSpeed <= 0f) return;

            Vector3 toTarget = worldPos - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f) return;

            Vector3 direction = toTarget.normalized;
            if (characterController != null
                && !LocalSteeringSafety.TryDirection(transform, characterController, direction, null, 1f, out direction))
                return;
            Vector3 step = direction * approachSpeed * Time.deltaTime;
            // Gravity included, or a fighter walking off a kerb hangs in the air -
            // CharacterController does not fall on its own.
            if (characterController != null && characterController.enabled)
            {
                characterController.Move(step + Vector3.down * 9.81f * Time.deltaTime);
            }
            else
            {
                transform.position += step;
            }
        }

        private void RetreatFromLastEnemy()
        {
            FactionBrawler enemy = FindNearestEnemy(out float distance);
            if (enemy == null || distance >= chaseRange)
            {
                gameObject.SetActive(false);
                return;
            }

            Vector3 away = transform.position - enemy.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.001f) away = -transform.forward;
            if (characterController != null
                && LocalSteeringSafety.TryDirection(transform, characterController, away, enemy.transform, -1f, out Vector3 safe))
            {
                characterController.Move(safe * 5.2f * Time.deltaTime + Vector3.down * 9.81f * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(safe), 10f * Time.deltaTime);
            }
        }

        private void FaceTarget(Vector3 worldPos)
        {
            Vector3 toTarget = worldPos - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(toTarget), 10f * Time.deltaTime);
        }

        private static Side Opposite(Side s) => s == Side.NotAhWord ? Side.DogLife : Side.NotAhWord;

        /// <summary>How many fighters on <paramref name="s"/> are still up.</summary>
        private static int CountStanding(Side s)
        {
            int n = 0;
            for (int i = 0; i < All.Count; i++)
            {
                var b = All[i];
                if (b == null || !b.isActiveAndEnabled) continue;
                if (b.side != s || b.IsOutOfAction) continue;
                n++;
            }
            return n;
        }

        private FactionBrawler FindNearestEnemy(out float nearestDist)
        {
            FactionBrawler nearest = null;
            nearestDist = float.MaxValue;

            for (int i = 0; i < All.Count; i++)
            {
                var other = All[i];
                if (other == null || other == this) continue;
                if (other.side == side) continue;
                if (other.IsOutOfAction) continue;
                if (!other.isActiveAndEnabled) continue;

                float d = Vector3.Distance(transform.position, other.transform.position);
                if (d < nearestDist) { nearestDist = d; nearest = other; }
            }

            return nearest;
        }
    }
}
