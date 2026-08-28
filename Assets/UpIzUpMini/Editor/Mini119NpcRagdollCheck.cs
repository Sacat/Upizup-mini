using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Combat;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-119 follow-up: verifies NpcRagdoll is actually wired
    /// onto Police/Villager/Gang NPCs after Mini011PhaseBSetup runs, and
    /// that NpcCombatHealth.Hit() drives the ragdoll->recover / ragdoll-
    /// >fade timers correctly for both a non-fatal and a fatal hit.
    /// Delete after use.</summary>
    public static class Mini119NpcRagdollCheck
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Check NPC Ragdoll (one-off)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var villager = GameObject.Find("NPC_Villager");
            var health = villager != null ? villager.GetComponent<NpcCombatHealth>() : null;
            var ragdoll = villager != null ? villager.GetComponent<NpcRagdoll>() : null;
            Debug.Log($"MINI-119 RAGDOLL CHECK: NPC_Villager found={villager != null}, has NpcCombatHealth={health != null}, has NpcRagdoll={ragdoll != null}");

            if (health == null || ragdoll == null)
            {
                Debug.LogError("MINI-119 RAGDOLL CHECK: villager missing expected components - aborting.");
                return;
            }

            var ragdollField = typeof(NpcCombatHealth).GetField("ragdoll", BindingFlags.NonPublic | BindingFlags.Instance);
            var wired = ragdollField.GetValue(health) as NpcRagdoll;
            Debug.Log($"MINI-119 RAGDOLL CHECK: NpcCombatHealth.ragdoll field wired correctly={wired == ragdoll}");

            // Force Awake so health/controller/renderers are initialised
            // the same way runtime would (Editor batch mode doesn't run
            // MonoBehaviour lifecycle automatically outside Play).
            var awake = typeof(NpcCombatHealth).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            awake.Invoke(health, null);

            // Non-fatal hit: should ragdoll + IsDown=true, health > 0.
            health.Hit(10f, Vector3.forward);
            Debug.Log($"MINI-119 RAGDOLL CHECK: after non-fatal Hit, IsDown={health.IsDown}, Health={health.Health}, ragdoll.IsRagdolled={ragdoll.IsRagdolled}");

            var recoverAtField = typeof(NpcCombatHealth).GetField("recoverAt", BindingFlags.NonPublic | BindingFlags.Instance);
            float recoverAt = (float)recoverAtField.GetValue(health);
            Debug.Log($"MINI-119 RAGDOLL CHECK: non-fatal recoverAt set (>{Time.time})={recoverAt > Time.time}");

            // Reset and test the fatal path.
            health.ResetForRespawn();
            Debug.Log($"MINI-119 RAGDOLL CHECK: after ResetForRespawn, IsDown={health.IsDown}, Health={health.Health}, ragdoll.IsRagdolled={ragdoll.IsRagdolled}, scale={villager.transform.localScale}");

            health.Hit(1000f, Vector3.forward);
            var fadeStartAtField = typeof(NpcCombatHealth).GetField("fadeStartAt", BindingFlags.NonPublic | BindingFlags.Instance);
            float fadeStartAt = (float)fadeStartAtField.GetValue(health);
            Debug.Log($"MINI-119 RAGDOLL CHECK: after fatal Hit, IsDown={health.IsDown}, Health={health.Health}, ragdoll.IsRagdolled={ragdoll.IsRagdolled}, fadeStartAt scheduled(>{Time.time})={fadeStartAt > Time.time}");

            // Check the same for a Police officer and a Not Ah Word gang
            // member, to confirm the other two attachment sites also got
            // wired (villager check above already covers Villager role).
            var police = GameObject.FindObjectsByType<UpIzUpMini.Interaction.TownNPCInteractable>(FindObjectsSortMode.None);
            int policeCount = 0, policeWithRagdoll = 0, gangCount = 0, gangWithRagdoll = 0;
            foreach (var npc in police)
            {
                if (npc.Role == UpIzUpMini.Interaction.NpcRole.Police)
                {
                    policeCount++;
                    if (npc.GetComponent<NpcRagdoll>() != null) policeWithRagdoll++;
                }
            }
            var gangMembers = GameObject.FindObjectsByType<UpIzUpMini.Character.GangMemberController>(FindObjectsSortMode.None);
            foreach (var g in gangMembers)
            {
                gangCount++;
                if (g.GetComponent<NpcRagdoll>() != null) gangWithRagdoll++;
            }
            Debug.Log($"MINI-119 RAGDOLL CHECK: Police NPCs={policeCount}, withRagdoll={policeWithRagdoll}; GangMember NPCs={gangCount}, withRagdoll={gangWithRagdoll}");

            Debug.Log("MINI-119 RAGDOLL CHECK: done.");
        }
    }
}
