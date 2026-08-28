using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Combat;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-119 follow-up, user: "if i hit the npc they will
    /// behave like ragdoll... you can do it will the police, villagers,
    /// gang members." Mini011PhaseBSetup.cs was updated to wire NpcRagdoll
    /// into every fresh build, but the LIVE GrandBayProof scene already
    /// has these NPCs hand-placed/saved from an earlier run - per
    /// "manual placement is authoritative", this does NOT re-run the
    /// full world builder (which would blow away hours of manual bike/
    /// mount tuning done this session). Instead it patches the
    /// already-placed NPCs directly and saves the scene. Additive only:
    /// never removes or moves anything, only adds missing components.
    /// Delete after use.</summary>
    public static class Mini119AttachNpcRagdoll
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Attach NPC Ragdoll To Existing Scene NPCs (one-off)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            int villagerPatched = 0, policePatched = 0, gangPatched = 0, alreadyHad = 0;

            // Reuse the builder's own AddHumanoidAnimationManager (private
            // static) via reflection - it also bakes the shared
            // HitReaction/KnockedDown clip entries onto the manager, which
            // a hand-rolled AddComponent<HumanoidAnimationManager>() would
            // miss, silently leaving Villagers with no hit-reaction/
            // knockdown animation despite having the ragdoll wired.
            var addAnimManagerMethod = typeof(Mini011PhaseBSetup).GetMethod(
                "AddHumanoidAnimationManager", BindingFlags.NonPublic | BindingFlags.Static);

            // Police + Villager: both are TownNPCInteractable roles.
            var townNpcs = Object.FindObjectsByType<TownNPCInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var npc in townNpcs)
            {
                if (npc.Role != NpcRole.Police && npc.Role != NpcRole.Villager) continue;

                var go = npc.gameObject;
                if (go.GetComponent<NpcRagdoll>() != null) { alreadyHad++; continue; }

                var animator = go.GetComponentInChildren<Animator>();
                if (animator == null || !animator.isHuman)
                {
                    Debug.LogWarning($"MINI-119 ATTACH RAGDOLL: skipping {go.name} - no bound Humanoid Animator found.");
                    continue;
                }

                var animationManager = go.GetComponent<HumanoidAnimationManager>();
                if (animationManager == null)
                {
                    animationManager = (HumanoidAnimationManager)addAnimManagerMethod.Invoke(null, new object[] { go, animator });
                }

                var combatHealth = go.GetComponent<NpcCombatHealth>();
                if (combatHealth == null)
                {
                    combatHealth = go.AddComponent<NpcCombatHealth>();
                }

                var ragdoll = go.AddComponent<NpcRagdoll>();

                var chSo = new SerializedObject(combatHealth);
                var amProp = chSo.FindProperty("animationManager");
                if (amProp != null && amProp.objectReferenceValue == null) amProp.objectReferenceValue = animationManager;
                chSo.FindProperty("ragdoll").objectReferenceValue = ragdoll;
                chSo.ApplyModifiedPropertiesWithoutUndo();

                if (npc.Role == NpcRole.Police) policePatched++; else villagerPatched++;
            }

            // Gang members (Not Ah Word roster + Dog Life) already have
            // NpcCombatHealth from Mini011PhaseBSetup - just add the
            // missing NpcRagdoll and wire it in.
            var gangMembers = Object.FindObjectsByType<GangMemberController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var member in gangMembers)
            {
                var go = member.gameObject;
                if (go.GetComponent<NpcRagdoll>() != null) { alreadyHad++; continue; }

                var animator = go.GetComponentInChildren<Animator>();
                if (animator == null || !animator.isHuman)
                {
                    Debug.LogWarning($"MINI-119 ATTACH RAGDOLL: skipping {go.name} - no bound Humanoid Animator found.");
                    continue;
                }

                var combatHealth = go.GetComponent<NpcCombatHealth>();
                var ragdoll = go.AddComponent<NpcRagdoll>();
                if (combatHealth != null)
                {
                    var chSo = new SerializedObject(combatHealth);
                    chSo.FindProperty("ragdoll").objectReferenceValue = ragdoll;
                    chSo.ApplyModifiedPropertiesWithoutUndo();
                }
                gangPatched++;
            }

            Debug.Log($"MINI-119 ATTACH RAGDOLL: policePatched={policePatched}, villagerPatched={villagerPatched}, gangPatched={gangPatched}, alreadyHad={alreadyHad}");

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("MINI-119 ATTACH RAGDOLL: scene saved.");
        }
    }
}
