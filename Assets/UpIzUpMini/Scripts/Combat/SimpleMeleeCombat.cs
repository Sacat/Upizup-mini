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
        // "Action" layer by HumanoidAnimationLayerBuilder. Kept as the
        // jab's own id (unchanged) for backward compatibility - it's
        // also MeleeMoveLibrary.Chain[0].id, the first combo step.
        public const string ActionId = MeleeMoveLibrary.JabId;

        // MINI-120 (combat bug-fix pass + combo request), user: "so
        // fighting system... i want you to remember the combat like a
        // system" then "ok what about combos?" Replaces the single
        // hardcoded punch with MeleeMoveLibrary.Chain - a real 3-hit
        // alternating combo (Jab -> Hook -> Right Hook), each step its
        // own data (clip/damage/reach/timing), not a bigger pile of
        // fields on this component. Old single-punch fields (range,
        // hitRadius, damage, windup/active/recovery) are gone - every
        // one of those numbers now lives per-move in the library,
        // including the tightened reach/bodyRadiusBonus fixing the
        // separately-reported over-generous hit detection.
        [Header("Combo")]
        [Tooltip("How long after a swing's recovery ends the combo chain stays alive - press Attack again within this window to advance to the next move, otherwise the next press restarts at the jab.")]
        [SerializeField] float comboResetSeconds = 1.0f;

        [Header("Cost")]
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
        int _comboStep;
        float _comboExpiresAt;
        readonly MeleeSwingTimeline swing = new MeleeSwingTimeline();
        public bool IsControlled => GetComponent<PlayerController>()?.IsControlled == true;
        public bool IsAttacking => swing.IsRunning;

        /// <summary>Which combo step will fire on the next Attack() call -
        /// exposed for UI (e.g. a combo counter) and deterministic tests.</summary>
        public int ComboStep => _comboStep;

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

            // The combo chain drops back to the jab if the player didn't
            // follow up in time - checked every frame, not just at the
            // next Attack() call, so ComboStep reads correctly for UI
            // even between presses.
            if (_comboStep != 0 && Time.time > _comboExpiresAt) _comboStep = 0;

            // F is both "punch" and "get on the bike" (MINI-069). Without this
            // guard you throw a punch on the same frame you mount, every time.
            if (Vehicles.BikeInteractable.ConsumedMountKeyThisFrame) return;
            if (!GameInput.WasPressed(GameAction.Attack) || Time.time < nextHit || swing.IsRunning) return;
            Attack();
        }

        /// <summary>
        /// Commits a swing and plays its animation. Damage is intentionally
        /// deferred until AdvanceAttack reaches the active window. Advances
        /// the combo chain one step (Jab -> Hook -> Right Hook -> Jab...).
        /// </summary>
        public void Attack()
        {
            if (swing.IsRunning || Time.time < nextHit) return;
            if (vitals != null && !vitals.TrySpendStamina(staminaCost)) return;

            var move = CurrentMove;
            nextHit = Time.time + move.TotalSeconds;
            _comboExpiresAt = Time.time + move.TotalSeconds + comboResetSeconds;
            _comboStep = (_comboStep + 1) % MeleeMoveLibrary.Chain.Length;

            // Plays even on a swing that connects with nothing - a real
            // attack animation reads as a fight, not just a damage tick.
            animationManager?.PlayAction(move.id);
            swing.Begin(move.BuildProfile());
        }

        /// <summary>Advances windup/active/recovery. Public for deterministic
        /// headless tests; runtime calls it once per controlled frame.</summary>
        public void AdvanceAttack(float deltaSeconds)
        {
            if (swing.Advance(deltaSeconds)) ResolveContact();
        }

        /// <summary>The move that just started (or is about to start) -
        /// _comboStep is advanced past it in Attack() before this is next
        /// read, so ResolveContact must capture the move BEFORE Attack()
        /// changes _comboStep, not re-derive it after the fact.</summary>
        private MeleeMoveLibrary.ComboMove _activeMove;
        private MeleeMoveLibrary.ComboMove CurrentMove
        {
            get
            {
                _activeMove = MeleeMoveLibrary.Chain[_comboStep];
                return _activeMove;
            }
        }

        private void ResolveContact()
        {
            float appliedDamage = CalculateAppliedDamage();
            if (MeleeContactResolver.TryFindNearest(
                    transform, _activeMove.BuildProfile(), NpcCombatHealth.All,
                    (NpcCombatHealth candidate) => !candidate.IsDown, out NpcCombatHealth target))
            {
                target.Hit(appliedDamage, transform.forward * .8f);
                var npc = target.GetComponent<TownNPCInteractable>();
                if (npc != null && npc.Role == NpcRole.Police)
                    EconomyManager.Instance?.AddHeat(EconomyManager.MaxHeat);
            }
        }

        /// <summary>
        /// The damage this swing deals, together-bonus included. Public
        /// and split out from Attack() so the bonus logic can be verified
        /// directly, independently of the timed contact query.
        /// </summary>
        public float CalculateAppliedDamage() => IsCompanionNearby() ? _activeMove.damage * togetherDamageMultiplier : _activeMove.damage;

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
