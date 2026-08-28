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

        // MINI-119 follow-up, user: "if i hit the npc they will behave
        // like ragdoll... if there health is 0 then they fade after
        // lying down for 3 seconds. if not then they walk." NpcRagdoll
        // is optional (added only to police/villagers/gang members per
        // the user's own scope - shopkeepers/dealers/mission NPCs never
        // get one) - Hit() below only ragdolls if one is actually
        // present, otherwise falls back to the original animation-only
        // reaction so nothing else that already calls Hit() breaks.
        [SerializeField] NpcRagdoll ragdoll;
        [Tooltip("How long a FATAL hit lies ragdolled before fading, per the user's own number.")]
        [SerializeField] float fatalLieSeconds = 3f;
        [Tooltip("How long a non-fatal ragdoll hit lies down before getting back up and walking.")]
        [SerializeField] float nonFatalLieSeconds = 2f;
        [SerializeField] float fadeSeconds = 1f;

        float health;
        float recoverAt;
        float despawnAt;
        float fadeStartAt;
        bool fading;
        CharacterController controller;
        Renderer[] renderers;

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
            if (ragdoll == null) ragdoll = GetComponent<NpcRagdoll>();
            renderers = GetComponentsInChildren<Renderer>(true);
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

            // MINI-119 follow-up, user: "if i hit the npc they will
            // behave like ragdoll... if there health is 0 then they
            // fade after lying down for 3 seconds. if not then they
            // walk." A ragdoll reaction fires for EVERY hit that lands
            // (not just the fatal one) - fatal vs non-fatal only
            // changes what happens once they're down: fade away, or get
            // back up and resume walking. ragdoll is only present on
            // police/villagers/gang members per the user's own scope -
            // shopkeepers/dealers/mission NPCs never get one, so they
            // fall through to the original animation-only reaction
            // below unchanged.
            if (ragdoll != null)
            {
                Vector3 impactVelocity = push.sqrMagnitude > 0.0001f
                    ? push.normalized * Mathf.Max(4f, push.magnitude * 6f)
                    : Vector3.zero;
                ragdoll.Ragdoll(impactVelocity);
                IsDown = true;
                if (controller != null) controller.enabled = false;

                if (health <= 0f)
                {
                    LastDefeatedAt = Time.time;
                    fadeStartAt = Time.time + fatalLieSeconds;
                    fading = false;
                }
                else
                {
                    recoverAt = Time.time + nonFatalLieSeconds;
                }
                return;
            }

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
                    foreach (var r in renderers) r.enabled = false;
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
            if (ragdoll != null && IsDown)
            {
                if (health <= 0f)
                {
                    // Fatal: lie ragdolled for fatalLieSeconds, then
                    // shrink-fade to nothing and deactivate. Scale
                    // (not material alpha) so this works regardless of
                    // whatever shader each NPC model happens to use.
                    if (!fading)
                    {
                        if (Time.time < fadeStartAt) return;
                        fading = true;
                        fadeStartAt = Time.time;
                        return;
                    }

                    float t = Mathf.Clamp01((Time.time - fadeStartAt) / fadeSeconds);
                    transform.localScale = Vector3.one * (1f - t);
                    if (t >= 1f)
                    {
                        gameObject.SetActive(false);
                        transform.localScale = Vector3.one;
                    }
                    return;
                }

                // Non-fatal: lie down briefly, then get back up and walk.
                if (recoverAt <= 0f || Time.time < recoverAt) return;
                ragdoll.Recover();
                IsDown = false;
                recoverAt = 0f;
                if (controller != null) controller.enabled = true;
                return;
            }

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
            foreach (var r in renderers) r.enabled = true;
            if (controller != null) controller.enabled = true;
        }

        public void ResetForRespawn()
        {
            health = maxHealth;
            recoverAt = 0f;
            despawnAt = 0f;
            fadeStartAt = 0f;
            fading = false;
            transform.localScale = Vector3.one;
            IsDown = false;
            if (ragdoll != null) ragdoll.Recover();
            animationManager?.EndSustainedAction();
            foreach (var r in renderers) r.enabled = true;
            if (controller != null) controller.enabled = true;
        }
    }
}
