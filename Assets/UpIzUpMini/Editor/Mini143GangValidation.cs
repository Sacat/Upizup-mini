using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UpIzUpMini.Combat;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.EditorTools
{
    /// <summary>Exercises the real pool reset order on a never-activated member.
    /// No live scene is opened or modified. Does not prove NPC navigation feel.</summary>
    public static class Mini143GangValidation
    {
        const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [MenuItem("Up Iz Up Mini/MINI-143/Validate First Dog Life Activation")]
        public static void Validate()
        {
            GameObject poolObject = null;
            GameObject member = null;
            try
            {
                poolObject = new GameObject("MINI143_TestPool");
                var pool = poolObject.AddComponent<RivalGangSpawner>();
                member = new GameObject("MINI143_NeverActivatedMember");
                member.SetActive(false);
                Vector3 approvedScale = new Vector3(.92f, 1.03f, .91f);
                member.transform.localScale = approvedScale;
                member.transform.SetPositionAndRotation(new Vector3(-38.59f, 8f, -152.24f),
                    Quaternion.Euler(0f, 72f, 0f));
                var child = new GameObject("VisibleBody");
                child.transform.SetParent(member.transform, false);
                var renderer = child.AddComponent<MeshRenderer>();
                var health = member.AddComponent<NpcCombatHealth>();
                pool.AddMember(member);

                // Do NOT invoke Awake here: serialized inactive gang members
                // reach ResetForRespawn before their first Unity activation.
                Require(!member.activeSelf, "Fixture must begin inactive.");
                Invoke(pool, "ReactivateEligibleMembers");
                Require(member.activeSelf, "Fresh member did not activate.");
                RequireScale(member, approvedScale, "first activation");
                Require(health.Health > 0f && !health.IsDown, "Fresh member is not healthy.");
                Require(renderer.enabled, "Fresh member renderer is hidden.");

                // Batch Edit Mode does not invoke runtime Awake automatically;
                // model its first call AFTER the reset, matching a real boot.
                Invoke(health, "Awake");
                RequireScale(member, approvedScale, "post-activation Awake");
                Require(health.DefeatPolicy == NpcDefeatPolicy.ExternalPool,
                    "Gang changed away from the external pool policy.");

                health.Hit(1000f, Vector3.zero);
                Require(health.IsDown, "Fatal-hit fixture was not defeated.");
                member.SetActive(false);
                Invoke(pool, "ReactivateEligibleMembers");
                Require(!member.activeSelf, "Defeated member bypassed the 100-second cooldown.");

                // A later reset must restore the original approved scale,
                // not a transient shrink/fade or a stale renderer cache.
                member.transform.localScale = approvedScale * .05f;
                renderer.enabled = false;
                Field(typeof(NpcCombatHealth), "renderers").SetValue(health, null);
                Field(typeof(NpcCombatHealth), "<LastDefeatedAt>k__BackingField")
                    .SetValue(health, Time.time - 105f);
                Invoke(pool, "ReactivateEligibleMembers");
                Require(member.activeSelf, "Expired cooldown did not reactivate member.");
                RequireScale(member, approvedScale, "later respawn");
                Require(health.Health > 0f && !health.IsDown, "Respawn did not restore health.");
                Require(renderer.enabled, "Respawn failed to restore renderer visibility.");

                member.SetActive(false);
                Invoke(pool, "ReactivateEligibleMembers");
                RequireScale(member, approvedScale, "repeated pooling");
                Debug.Log("MINI-143 GANG VALIDATION PASS: never-activated pooled member retains its positive authored nonunit scale; delayed Awake is safe; defeat cooldown remains enforced; later and repeated respawns restore health, renderer and original scale. Live block visibility/navigation still needs capture/playtest.");
            }
            finally
            {
                if (member != null) UnityEngine.Object.DestroyImmediate(member);
                if (poolObject != null) UnityEngine.Object.DestroyImmediate(poolObject);
            }
        }

        static FieldInfo Field(Type type, string name) =>
            type.GetField(name, PrivateInstance) ?? throw new InvalidOperationException("Missing field: " + name);

        static void Invoke(object target, string method)
        {
            var info = target.GetType().GetMethod(method, PrivateInstance);
            if (info == null) throw new InvalidOperationException("Missing method: " + method);
            info.Invoke(target, null);
        }

        static void RequireScale(GameObject member, Vector3 expected, string stage) =>
            Require((member.transform.localScale - expected).sqrMagnitude < .0000001f,
                stage + " changed scale to " + member.transform.localScale + "; expected " + expected);

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("MINI-143 GANG: " + message);
        }
    }
}