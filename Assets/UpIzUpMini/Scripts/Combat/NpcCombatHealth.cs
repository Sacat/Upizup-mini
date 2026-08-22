using System.Collections.Generic;
using UnityEngine;
using UpIzUpMini.Character;

namespace UpIzUpMini.Combat
{
    /// <summary>
    /// MINI-038. NPC side of melee combat. Every non-fatal hit plays a
    /// stagger reaction; taking enough of them (health reaches 0) plays a
    /// sustained fall/lie-down pose instead of instantly hiding the
    /// character, and holds it for <see cref="recoverSeconds"/> before
    /// getting back up. Both use HumanoidAnimationManager, so this needs no
    /// Animator wiring of its own beyond that reference.
    /// </summary>
    public class NpcCombatHealth : MonoBehaviour
    {
        public static readonly List<NpcCombatHealth> All = new List<NpcCombatHealth>();
        // Ids baked into the shared controller's Action/FullBodyOverride
        // layers by HumanoidAnimationLayerBuilder (see
        // Mini011PhaseBSetup.GetSharedActionEntries).
        public const string HitReactionActionId = "HitReaction";
        public const string KnockedDownActionId = "KnockedDown";

        [SerializeField] float maxHealth = 100f;
        [SerializeField] float recoverSeconds = 12f;
        [SerializeField] HumanoidAnimationManager animationManager;
        [SerializeField] bool despawnOnDefeat;
        [SerializeField] float despawnDelay = 1.35f;
        float health;
        float recoverAt;
        float despawnAt;
        CharacterController controller;

        /// <summary>True while lying down after being knocked out. Movement
        /// scripts (PoliceOfficer) check this and stop steering rather than
        /// fighting the sustained animation layer.</summary>
        public bool IsDown { get; private set; }
        public float Health => health;

        /// <summary>MINI-112: Time.time this fighter was last knocked out
        /// via the despawn path (float.NegativeInfinity if never). Read by
        /// RivalGangSpawner to gate how soon a defeated Dog Life member is
        /// allowed to reappear - previously OnEnable's own ResetForRespawn
        /// meant simply reactivating the GameObject undid a defeat
        /// instantly, however little time had actually passed.</summary>
        public float LastDefeatedAt { get; private set; } = float.NegativeInfinity;

        void Awake()
        {
            health = maxHealth;
            controller = GetComponent<CharacterController>();
            if (animationManager == null) animationManager = GetComponent<HumanoidAnimationManager>();
        }

        void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
            // MINI-112: used to unconditionally ResetForRespawn() here,
            // which meant simply reactivating the GameObject undid a
            // defeat instantly regardless of how little time had passed.
            // The only caller that reactivates a despawnOnDefeat character
            // (RivalGangSpawner) now calls ResetForRespawn() itself, after
            // checking a real cooldown against LastDefeatedAt.
        }

        void OnDisable()
        {
            All.Remove(this);
        }

        public void Hit(float damage, Vector3 push)
        {
            // Already down - can't be hit again until they get back up.
            if (IsDown) return;

            health = Mathf.Max(0f, health - damage);
            if (controller != null && controller.enabled) controller.Move(push);

            if (health <= 0f)
            {
                IsDown = true;
                recoverAt = Time.time + recoverSeconds;
                LastDefeatedAt = Time.time;
                if (despawnOnDefeat) despawnAt = Time.time + despawnDelay;
                // Held at full weight (no auto-fade, unlike a one-shot
                // PlayAction) so the character stays lying down for the
                // whole recovery window instead of springing back to idle
                // mid-clip.
                bool played = animationManager != null && animationManager.BeginSustainedAction(KnockedDownActionId);
                if (!played)
                {
                    // No clip baked in (or no animation manager on this
                    // character) - fall back to the old vanish-until-
                    // recovered behaviour rather than leaving a standing,
                    // unresponsive body.
                    foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = false;
                }
                if (controller != null) controller.enabled = false;
            }
            else
            {
                animationManager?.PlayAction(HitReactionActionId);
            }
        }

        void Update()
        {
            if (despawnOnDefeat && IsDown && despawnAt > 0f && Time.time >= despawnAt)
            {
                gameObject.SetActive(false);
                return;
            }
            if (recoverAt <= 0f || Time.time < recoverAt) return;

            health = maxHealth;
            recoverAt = 0f;
            IsDown = false;
            animationManager?.EndSustainedAction();
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = true;
            if (controller != null) controller.enabled = true;
        }

        public void ResetForRespawn()
        {
            health = maxHealth;
            recoverAt = 0f;
            despawnAt = 0f;
            IsDown = false;
            animationManager?.EndSustainedAction();
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = true;
            if (controller != null) controller.enabled = true;
        }
    }
}
