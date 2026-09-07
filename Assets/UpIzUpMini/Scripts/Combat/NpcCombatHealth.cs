using System.Collections.Generic;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.Combat
{
    public enum NpcDefeatPolicy { ExternalPool, FadeAndRespawnAtHome, RecoverAndReturnToPost }

    /// <summary>Health endpoint shared by melee and vehicle impacts. Pooled gangs retain their external respawner; ambient NPCs fade and return; sellers and named interactables always recover and walk back to their captured post.</summary>
    public class NpcCombatHealth : MonoBehaviour
    {
        public static readonly List<NpcCombatHealth> All = new List<NpcCombatHealth>();
        public const string HitReactionActionId = "HitReaction";
        public const string KnockedDownActionId = "KnockedDown";

        [SerializeField] float maxHealth = 100f;
        [SerializeField] float recoverSeconds = 12f;
        [SerializeField] HumanoidAnimationManager animationManager;
        [SerializeField] bool despawnOnDefeat;
        [SerializeField] float despawnDelay = 1.35f;
        [SerializeField] NpcRagdoll ragdoll;
        [SerializeField] float fatalLieSeconds = 3f;
        [SerializeField] float nonFatalLieSeconds = 2f;
        [SerializeField] float fadeSeconds = 1f;
        [SerializeField] NpcDefeatPolicy defeatPolicy = NpcDefeatPolicy.ExternalPool;
        [SerializeField] float worldRespawnSeconds = 100f;
        [SerializeField] float returnToPostSpeed = 1.65f;

        float health, recoverAt, forceRecoverAt, despawnAt, fadeStartAt, respawnAt;
        bool fading, awaitingRespawn, returningToPost;
        CharacterController controller;
        Animator animator;
        Renderer[] renderers;
        Vector3 homePosition;
        Quaternion homeRotation;
        Vector3 homeScale;
        bool initialized;

        public bool IsDown { get; private set; }
        public float Health => health;
        public NpcDefeatPolicy DefeatPolicy => defeatPolicy;
        public float LastDefeatedAt { get; private set; } = float.NegativeInfinity;

        void Awake()
        {
            EnsureInitialized();
        }

        // An authored inactive gang member can be reset by its pool before
        // Unity calls Awake. Capture its approved pose/scale before any reset
        // writes them, and never recapture a faded or displaced runtime pose.
        void EnsureInitialized()
        {
            if (initialized) return;
            health = maxHealth;
            controller = GetComponent<CharacterController>();
            animator = GetComponentInChildren<Animator>();
            if (animationManager == null) animationManager = GetComponent<HumanoidAnimationManager>();
            if (ragdoll == null) ragdoll = GetComponent<NpcRagdoll>();
            renderers = GetComponentsInChildren<Renderer>(true);
            homePosition = transform.position;
            homeRotation = transform.rotation;
            homeScale = transform.localScale;
            initialized = true;
            ConfigureFromRole(GetComponent<TownNPCInteractable>());
        }

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() => All.Remove(this);

        public void ConfigureFromRole(TownNPCInteractable npc)
        {
            if (npc == null) return;
            defeatPolicy = PolicyForRole(npc.Role);
        }

        public static NpcDefeatPolicy PolicyForRole(NpcRole role) =>
            role == NpcRole.Villager || role == NpcRole.Police
                ? NpcDefeatPolicy.FadeAndRespawnAtHome
                : NpcDefeatPolicy.RecoverAndReturnToPost;

        public void Hit(float damage, Vector3 push)
        {
            Vector3 velocity = push.sqrMagnitude > .0001f
                ? push.normalized * Mathf.Max(4f, push.magnitude * 6f) : Vector3.zero;
            HitFromImpact(damage, velocity, transform.position + Vector3.up);
        }

        public void HitFromImpact(float damage, Vector3 impactVelocity, Vector3 impactPoint)
        {
            if (IsDown || awaitingRespawn) return;
            health = Mathf.Max(0f, health - Mathf.Max(0f, damage));

            if (ragdoll != null)
            {
                ragdoll.Ragdoll(impactVelocity, impactPoint);
                IsDown = true;
                if (controller != null) controller.enabled = false;
                bool protectedNpc = defeatPolicy == NpcDefeatPolicy.RecoverAndReturnToPost;
                if (health <= 0f && !protectedNpc)
                {
                    LastDefeatedAt = Time.time;
                    fadeStartAt = Time.time + fatalLieSeconds;
                    fading = false;
                }
                else
                {
                    float severityDelay = Mathf.Lerp(0.15f, 0.85f,
                        Mathf.InverseLerp(4f, 12f, impactVelocity.magnitude));
                    recoverAt = Time.time + nonFatalLieSeconds + severityDelay;
                    forceRecoverAt = recoverAt + 1.25f;
                }
                return;
            }

            Vector3 push = impactVelocity / 6f;
            if (controller != null && controller.enabled) controller.Move(push);
            if (health <= 0f)
            {
                IsDown = true;
                recoverAt = Time.time + recoverSeconds;
                LastDefeatedAt = Time.time;
                if (despawnOnDefeat) despawnAt = Time.time + despawnDelay;
                bool played = animationManager != null && animationManager.BeginSustainedAction(KnockedDownActionId);
                if (!played) SetRenderers(false);
                if (controller != null) controller.enabled = false;
            }
            else animationManager?.PlayAction(HitReactionActionId);
        }

        void Update()
        {
            if (awaitingRespawn)
            {
                if (Time.time >= respawnAt)
                {
                    transform.SetPositionAndRotation(homePosition, homeRotation);
                    ResetForRespawn();
                }
                return;
            }
            if (returningToPost) { ReturnToPost(); return; }

            if (ragdoll != null && IsDown)
            {
                bool protectedNpc = defeatPolicy == NpcDefeatPolicy.RecoverAndReturnToPost;
                if (health <= 0f && !protectedNpc)
                {
                    if (!fading)
                    {
                        if (Time.time < fadeStartAt) return;
                        fading = true;
                        fadeStartAt = Time.time;
                        return;
                    }
                    float t = Mathf.Clamp01((Time.time - fadeStartAt) / fadeSeconds);
                    transform.localScale = homeScale * (1f - t);
                    if (t >= 1f)
                    {
                        if (defeatPolicy == NpcDefeatPolicy.FadeAndRespawnAtHome)
                        {
                            awaitingRespawn = true;
                            respawnAt = Time.time + worldRespawnSeconds;
                            SetRenderers(false);
                            transform.localScale = homeScale;
                        }
                        else { gameObject.SetActive(false); transform.localScale = homeScale; }
                    }
                    return;
                }

                if (recoverAt <= 0f || Time.time < recoverAt) return;
                if (Time.time < forceRecoverAt && !ragdoll.IsSettled()) return;
                ragdoll.Recover(true);
                IsDown = false;
                recoverAt = forceRecoverAt = 0f;
                if (protectedNpc) { health = maxHealth; returningToPost = true; }
                if (controller != null) controller.enabled = true;
                return;
            }

            if (despawnOnDefeat && IsDown && despawnAt > 0f && Time.time >= despawnAt)
            { gameObject.SetActive(false); return; }
            if (recoverAt <= 0f || Time.time < recoverAt) return;
            health = maxHealth;
            recoverAt = 0f;
            IsDown = false;
            animationManager?.EndSustainedAction();
            SetRenderers(true);
            if (controller != null) controller.enabled = true;
        }

        void ReturnToPost()
        {
            Vector3 delta = homePosition - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude <= .04f)
            {
                transform.SetPositionAndRotation(homePosition, homeRotation);
                returningToPost = false;
                SetWalkBlend(0f);
                return;
            }
            Vector3 direction = delta.normalized;
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(direction, Vector3.up), 360f * Time.deltaTime);
            Vector3 movement = direction * returnToPostSpeed;
            movement.y = -2f;
            if (controller != null && controller.enabled) controller.Move(movement * Time.deltaTime);
            else transform.position += direction * (returnToPostSpeed * Time.deltaTime);
            SetWalkBlend(1f);
        }

        void SetWalkBlend(float speed)
        {
            if (animator == null) return;
            animator.SetFloat("Speed", speed);
            animator.SetFloat("MotionSpeed", speed > .01f ? 1f : 0f);
        }

        void SetRenderers(bool visible)
        { if (renderers != null) foreach (var r in renderers) if (r != null) r.enabled = visible; }

        public void ResetForRespawn()
        {
            EnsureInitialized();
            if (controller == null) controller = GetComponent<CharacterController>();
            if (animationManager == null) animationManager = GetComponent<HumanoidAnimationManager>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (ragdoll == null) ragdoll = GetComponent<NpcRagdoll>();
            renderers = GetComponentsInChildren<Renderer>(true);
            health = maxHealth;
            recoverAt = forceRecoverAt = despawnAt = fadeStartAt = respawnAt = 0f;
            fading = awaitingRespawn = returningToPost = false;
            transform.localScale = homeScale;
            IsDown = false;
            if (ragdoll != null) ragdoll.Recover();
            animationManager?.EndSustainedAction();
            SetRenderers(true);
            SetWalkBlend(0f);
            if (controller != null) controller.enabled = true;
        }
    }
}
