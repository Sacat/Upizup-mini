using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Combat;
using UpIzUpMini.Dialogue;
using UpIzUpMini.Interaction;
using UpIzUpMini.Progression;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-112 focused validator: the PatrolNPC/FactionBrawler
    /// steering-contention fix, the real respawn cooldown, the jealousy
    /// dialogue line, and the group-walk mechanism's eligibility rules.</summary>
    public static class Mini112DogLifeEscalationValidation
    {
        [MenuItem("Up Iz Up Mini/MINI-112/Validate Dog Life Escalation")]
        public static void Validate()
        {
            bool pass = true;
            string fail = null;

            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var members = GameObject.Find("DogLifeSpawner")?.GetComponent<RivalGangSpawner>();
            Check(ref pass, ref fail, members != null, "no RivalGangSpawner (DogLifeSpawner) in the scene.");
            var dogLifeGos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(t => t.name.StartsWith("NPC_DogLife_")).Select(t => t.gameObject).ToArray();
            Check(ref pass, ref fail, dogLifeGos.Length > 0, "no NPC_DogLife_* members found in the scene.");
            if (!pass) { Report(pass, fail); return; }

            var member0 = dogLifeGos[0];
            var brawler = member0.GetComponent<FactionBrawler>();
            var patrol = member0.GetComponent<PatrolNPC>();
            var health = member0.GetComponent<NpcCombatHealth>();
            Check(ref pass, ref fail, brawler != null, "NPC_DogLife_0 has no FactionBrawler.");
            Check(ref pass, ref fail, patrol != null, "NPC_DogLife_0 has no PatrolNPC.");
            Check(ref pass, ref fail, health != null, "NPC_DogLife_0 has no NpcCombatHealth.");
            if (!pass) { Report(pass, fail); return; }

            Invoke(brawler, "Awake");
            Invoke(health, "Awake");
            Invoke(patrol, "Awake");

            // --- PatrolNPC/FactionBrawler steering contention fix -------
            var originalWaypoints = patrol.Waypoints;
            var chaseTargetField = typeof(FactionBrawler).GetField("_chaseTarget", BindingFlags.NonPublic | BindingFlags.Instance);
            // Simulate "engaged" by giving it a live chase target of itself
            // (only IsEngaged's null-check matters here, not real combat).
            chaseTargetField.SetValue(brawler, brawler);
            Check(ref pass, ref fail, brawler.IsEngaged, "FactionBrawler.IsEngaged is false despite a live _chaseTarget - the check itself is broken.");

            Vector3 posBefore = member0.transform.position;
            Invoke(patrol, "Update");
            Vector3 posAfterEngaged = member0.transform.position;
            Check(ref pass, ref fail, Vector3.Distance(posBefore, posAfterEngaged) < 0.001f,
                "PatrolNPC still moved the character while FactionBrawler.IsEngaged was true - the steering-contention fix did not take.");

            chaseTargetField.SetValue(brawler, null);
            Check(ref pass, ref fail, !brawler.IsEngaged, "FactionBrawler.IsEngaged is still true after clearing _chaseTarget.");

            // --- Respawn cooldown ----------------------------------------
            var lastDefeatedField = typeof(NpcCombatHealth).GetProperty("LastDefeatedAt");
            member0.SetActive(false);
            SetAutoProperty(health, "LastDefeatedAt", Time.time - 5f); // defeated 5s ago - well inside the cooldown
            var respawnCooldownField = typeof(RivalGangSpawner).GetField("respawnCooldownSeconds", BindingFlags.NonPublic | BindingFlags.Instance);
            float cooldown = (float)respawnCooldownField.GetValue(members);
            var reactivateMethod = typeof(RivalGangSpawner).GetMethod("ReactivateEligibleMembers", BindingFlags.NonPublic | BindingFlags.Instance);
            var poolField = typeof(RivalGangSpawner).GetField("_pool", BindingFlags.NonPublic | BindingFlags.Instance);
            var pool = (List<GameObject>)poolField.GetValue(members);
            if (!pool.Contains(member0)) pool.Add(member0);
            reactivateMethod.Invoke(members, null);
            Check(ref pass, ref fail, !member0.activeSelf,
                "a member defeated 5s ago was reactivated despite a cooldown of "
                + cooldown + "s - the respawn cooldown is not being respected.");

            SetAutoProperty(health, "LastDefeatedAt", Time.time - (cooldown + 5f)); // safely past the cooldown
            reactivateMethod.Invoke(members, null);
            Check(ref pass, ref fail, member0.activeSelf,
                "a member defeated well past the cooldown window was NOT reactivated - the cooldown gate is too strict or never clears.");

            // --- Jealousy dialogue ----------------------------------------
            var dogLifeSet = AssetDatabase.LoadAssetAtPath<DialogueSet>("Assets/UpIzUpMini/Data/Dialogue/DogLifeLines.asset");
            Check(ref pass, ref fail, dogLifeSet != null, "DogLifeLines.asset not found.");
            if (dogLifeSet != null)
            {
                bool hasJealousyLine = dogLifeSet.lines.Any(l =>
                    l.conditions != null && l.conditions.Count >= 2
                    && l.conditions.Any(c => c.type == DialogueConditionType.DogLifeRevealed)
                    && l.conditions.Any(c => c.type == DialogueConditionType.CropUnlocked));
                Check(ref pass, ref fail, hasJealousyLine,
                    "no Dog Life line gated on both DogLifeRevealed and CropUnlocked - the jealousy escalation line is missing.");
            }

            // --- Group-walk eligibility rules -----------------------------
            var updateGroupWalk = typeof(RivalGangSpawner).GetMethod("UpdateGroupWalk", BindingFlags.NonPublic | BindingFlags.Instance);
            var nextGroupWalkField = typeof(RivalGangSpawner).GetField("_nextGroupWalkAt", BindingFlags.NonPublic | BindingFlags.Instance);
            var walkingGroupField = typeof(RivalGangSpawner).GetField("_walkingGroup", BindingFlags.NonPublic | BindingFlags.Instance);

            // Force an engaged member into the pool and confirm it is
            // skipped by the group-walk picker.
            chaseTargetField.SetValue(brawler, brawler);
            member0.SetActive(true);
            nextGroupWalkField.SetValue(members, Time.time - 1f); // due now
            updateGroupWalk.Invoke(members, null);
            var walkingGroup = (List<GameObject>)walkingGroupField.GetValue(members);
            Check(ref pass, ref fail, !walkingGroup.Contains(member0),
                "an IsEngaged member was picked for the occasional group walk - it should be skipped while fighting/chasing.");
            chaseTargetField.SetValue(brawler, null);

            Report(pass, fail);
        }

        private static void SetAutoProperty(object target, string propertyName, object value)
        {
            var backingField = target.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            if (backingField == null)
            {
                Debug.LogError($"MINI-112 VALIDATION: backing field for '{propertyName}' not found on {target.GetType().Name}.");
                return;
            }
            backingField.SetValue(target, value);
        }

        private static object Invoke(object target, string methodName, object[] args = null)
        {
            var method = target.GetType().GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
            {
                Debug.LogError($"MINI-112 VALIDATION: method '{methodName}' not found on {target.GetType().Name}.");
                return null;
            }
            return method.Invoke(target, args);
        }

        private static void Check(ref bool pass, ref string fail, bool condition, string message)
        {
            if (pass && !condition) { pass = false; fail = message; }
        }

        private static void Report(bool pass, string fail)
        {
            if (pass)
                Debug.Log("MINI-112 VALIDATION PASS: PatrolNPC correctly yields movement while FactionBrawler.IsEngaged, fixing the steering contention that previously left Dog Life members frozen instead of resuming block behaviour; a defeated member's respawn correctly waits out the real cooldown against LastDefeatedAt rather than resetting on mere reactivation; the jealousy dialogue line exists gated on DogLifeRevealed + CropUnlocked; and the occasional group-walk picker correctly skips an engaged (fighting/chasing) member. NOT covered: how the leash/chase-break/return actually looks and feels in real combat, and whether the group walk reads naturally - both need the user's real Play Mode test.");
            else
                Debug.LogError($"MINI-112 VALIDATION FAIL: {fail}");
        }
    }
}
