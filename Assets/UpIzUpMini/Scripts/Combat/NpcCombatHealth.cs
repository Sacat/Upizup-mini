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
        // Ids baked into the shared controller's Action/FullBodyOverride
        // layers by HumanoidAnimationLayerBuilder (see
        // Mini011PhaseBSetup.GetSharedActionEntries).
        public const string HitReactionActionId = "HitReaction";
        public const string KnockedDownActionId = "KnockedDown";

        [SerializeField] float maxHealth = 100f;
        [SerializeField] float recoverSeconds = 12f;
        // MINI-054: whether this target is a police officer. Hitting an
        // officer escalates police heat to max (GTA-style); fighting a rival
        // gang member does not. Set false on Dog Life gang members.
        [SerializeField] bool isOfficer = true;
        [SerializeField] HumanoidAnimationManager animationManager;
        float health;
        float recoverAt;
        CharacterController controller;

        /// <summary>True while lying down after being knocked out. Movement
        /// scripts (PoliceOfficer) check this and stop steering rather than
        /// fighting the sustained animation layer.</summary>
        public bool IsDown { get; private set; }

        /// <summary>True when this target is a police officer (hitting them
        /// escalates police heat to max). Set false on rival gang members so
        /// gang fights do not spike police heat.</summary>
        public bool IsOfficer => isOfficer;

        void Awake()
        {
            health = maxHealth;
            controller = GetComponent<CharacterController>();
            if (animationManager == null) animationManager = GetComponent<HumanoidAnimationManager>();
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
            if (recoverAt <= 0f || Time.time < recoverAt) return;

            health = maxHealth;
            recoverAt = 0f;
            IsDown = false;
            animationManager?.EndSustainedAction();
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = true;
            if (controller != null) controller.enabled = true;
        }
    }
}
