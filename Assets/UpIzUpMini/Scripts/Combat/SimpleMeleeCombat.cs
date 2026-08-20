using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;

namespace UpIzUpMini.Combat
{
    public class SimpleMeleeCombat : MonoBehaviour
    {
        // MINI-031: id baked into the shared controller's upper-body
        // "Action" layer by HumanoidAnimationLayerBuilder. Kept as a
        // constant here rather than a per-instance field since every
        // character that can throw a punch plays the same clip.
        public const string ActionId = "Melee";

        [SerializeField] float range = 2.1f;
        [SerializeField] float damage = 35f;
        [SerializeField] float cooldown = .65f;
        [SerializeField] HumanoidAnimationManager animationManager;

        // MINI-046: "stronger when two main characters together battling
        // police or gang." A flat damage multiplier while the companion
        // is nearby, following (not off farming or on the Guadeloupe run),
        // and alive - having backup actually hits harder, not just looks
        // like it does.
        [SerializeField] float togetherRange = 5f;
        [SerializeField] float togetherDamageMultiplier = 1.5f;

        float nextHit;
        public bool IsControlled => GetComponent<PlayerController>()?.IsControlled == true;

        void Awake()
        {
            if (animationManager == null) animationManager = GetComponent<HumanoidAnimationManager>();
        }

        void Update()
        {
            // F is both "punch" and "get on the bike" (MINI-069). Without this
            // guard you throw a punch on the same frame you mount, every time.
            if (Vehicles.BikeInteractable.ConsumedMountKeyThisFrame) return;
            if (!IsControlled || !Input.GetKeyDown(KeyCode.F) || Time.time < nextHit) return;
            nextHit = Time.time + cooldown;
            Attack();
        }

        /// <summary>
        /// The actual swing, split out from Update() so it can be driven
        /// directly by a test harness - Input.GetKeyDown can't be faked in
        /// a headless batch-mode run, so a harness calling Update() itself
        /// would never reach this logic at all.
        /// </summary>
        public void Attack()
        {
            // Plays even on a swing that connects with nothing - a real
            // attack animation reads as a fight, not just a damage tick.
            animationManager?.PlayAction(ActionId);

            float appliedDamage = CalculateAppliedDamage();

            Vector3 center = transform.position + transform.forward * 1.1f + Vector3.up;
            foreach (var hit in Physics.OverlapSphere(center, range, ~0, QueryTriggerInteraction.Ignore))
            {
                var target = hit.GetComponentInParent<NpcCombatHealth>();
                if (target == null) continue;
                target.Hit(appliedDamage, transform.forward * .8f);
                // Only police carry NpcCombatHealth today, so landing any
                // punch here is striking an officer - per the user's
                // explicit ask, that's an instant max-heat escalation
                // (GTA-style "you hit a cop"), not a gradual heat tick like
                // proximity/contraband. AddHeat clamps at MaxHeat, so this
                // jumps straight there regardless of the current value.
                EconomyManager.Instance?.AddHeat(EconomyManager.MaxHeat);
                break;
            }
        }

        /// <summary>
        /// The damage this swing deals, together-bonus included. Public
        /// and split out from Attack() so the bonus logic can be verified
        /// directly - Attack()'s actual target-finding goes through
        /// Physics.OverlapSphere, which is not reliable to drive from an
        /// edit-mode test harness (colliders created in the same frame
        /// aren't guaranteed registered in the physics broadphase outside
        /// Play mode).
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
