using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;
using UpIzUpMini.InputSystem;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.Combat
{
    public class SimpleMeleeCombat : MonoBehaviour
    {
        // MINI-031: id baked into the shared controller's upper-body
        // "Action" layer by HumanoidAnimationLayerBuilder. Kept as a
        // constant here rather than a per-instance field since every
        // character that can throw a punch plays the same clip.
        public const string ActionId = "Melee";

        [Header("Contact")]
        [SerializeField] float range = 1.65f;
        [SerializeField] float hitRadius = 0.38f;
        [SerializeField] float forwardOffset = 0.3f;
        [SerializeField] float arcDegrees = 80f;
        [SerializeField] float windupSeconds = 0.16f;
        [SerializeField] float activeSeconds = 0.12f;
        [SerializeField] float recoverySeconds = 0.37f;

        [Header("Cost and damage")]
        [SerializeField] float damage = 35f;
        [SerializeField] float cooldown = .65f;
        [SerializeField] float staminaCost = 7f;
        [SerializeField] HumanoidAnimationManager animationManager;
        [SerializeField] CharacterVitals vitals;

        // MINI-046: "stronger when two main characters together battling
        // police or gang." A flat damage multiplier while the companion
        // is nearby, following (not off farming or on the Guadeloupe run),
        // and alive - having backup actually hits harder, not just looks
        // like it does.
        [SerializeField] float togetherRange = 5f;
        [SerializeField] float togetherDamageMultiplier = 1.5f;

        float nextHit;
        readonly MeleeSwingTimeline swing = new MeleeSwingTimeline();
        public bool IsControlled => GetComponent<PlayerController>()?.IsControlled == true;
        public bool IsAttacking => swing.IsRunning;

        void Awake()
        {
            if (animationManager == null) animationManager = GetComponent<HumanoidAnimationManager>();
            if (vitals == null) vitals = GetComponent<CharacterVitals>();
        }

        void Update()
        {
            if (!IsControlled)
            {
                swing.Cancel();
                return;
            }

            AdvanceAttack(Time.deltaTime);

            // F is both "punch" and "get on the bike" (MINI-069). Without this
            // guard you throw a punch on the same frame you mount, every time.
            if (Vehicles.BikeInteractable.ConsumedMountKeyThisFrame) return;
            if (!GameInput.WasPressed(GameAction.Attack) || Time.time < nextHit || swing.IsRunning) return;
            Attack();
        }

        /// <summary>
        /// Commits a swing and plays its animation. Damage is intentionally
        /// deferred until AdvanceAttack reaches the active window.
        /// </summary>
        public void Attack()
        {
            if (swing.IsRunning || Time.time < nextHit) return;
            if (vitals != null && !vitals.TrySpendStamina(staminaCost)) return;

            nextHit = Time.time + cooldown;
            // Plays even on a swing that connects with nothing - a real
            // attack animation reads as a fight, not just a damage tick.
            animationManager?.PlayAction(ActionId);
            swing.Begin(BuildProfile());
        }

        /// <summary>Advances windup/active/recovery. Public for deterministic
        /// headless tests; runtime calls it once per controlled frame.</summary>
        public void AdvanceAttack(float deltaSeconds)
        {
            if (swing.Advance(deltaSeconds)) ResolveContact();
        }

        private void ResolveContact()
        {
            float appliedDamage = CalculateAppliedDamage();
            if (MeleeContactResolver.TryFindNearest(
                    transform, BuildProfile(), NpcCombatHealth.All,
                    (NpcCombatHealth candidate) => !candidate.IsDown, out NpcCombatHealth target))
            {
                target.Hit(appliedDamage, transform.forward * .8f);
                var npc = target.GetComponent<TownNPCInteractable>();
                if (npc != null && npc.Role == NpcRole.Police)
                    EconomyManager.Instance?.AddHeat(EconomyManager.MaxHeat);
            }
        }

        private MeleeAttackProfile BuildProfile() => new MeleeAttackProfile(
            windupSeconds, activeSeconds, recoverySeconds,
            forwardOffset, range, hitRadius, arcDegrees);

        /// <summary>
        /// The damage this swing deals, together-bonus included. Public
        /// and split out from Attack() so the bonus logic can be verified
        /// directly, independently of the timed contact query.
        /// </summary>
        public float CalculateAppliedDamage() => IsCompanionNearby() ? damage * togetherDamageMultiplier : damage;

        /// <summary>
        /// True when the other boy is close by, on his feet, and free to
        /// actually help (not locked away on the Guadeloupe run, not head
        /// -down farming). Checked fresh each swing rather than cached -
        /// "together" is a moment-to-moment thing, not a toggle.
        /// </summary>
        private bool IsCompanionNearby()
        {
            var switcher = CharacterSwitchManager.Instance;
            if (switcher?.Slots == null || switcher.Slots.Length < 2) return false;

            int inactive = 1 - switcher.ActiveIndex;
            if (switcher.IsLocked(inactive)) return false;

            var other = inactive < switcher.Slots.Length ? switcher.Slots[inactive] : null;
            if (other?.root == null || !other.root.activeSelf) return false;

            var otherFarmhand = other.root.GetComponent<FarmhandController>();
            if (otherFarmhand != null && otherFarmhand.IsWorking) return false;

            var otherVitals = other.vitals;
            if (otherVitals != null && otherVitals.IsDead) return false;

            return Vector3.Distance(transform.position, other.root.transform.position) <= togetherRange;
        }
    }
}
