using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UpIzUpMini.Economy;
using UpIzUpMini.Interaction;
using UpIzUpMini.Progression;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-056: proves the Rasta mentor's dialogue actually tracks the
    /// player's real strain-unlock tier, against the ACTUAL built scene's
    /// NPC and DialogueSet asset (not a duplicate built in-memory) - if a
    /// future scene-builder change ever detaches the asset or breaks the
    /// condition wiring, this catches it against the real content.
    /// </summary>
    public static class Mini056RastaMentorValidation
    {
        [MenuItem("Up Iz Up Mini/MINI-056/Validate Rasta Mentor")]
        public static void Validate()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var mentorGo = GameObject.Find("NPC_RastaMentor");
            if (mentorGo == null) { Debug.LogError("MINI-056 VALIDATION FAIL: NPC_RastaMentor not found in the built scene."); return; }
            var mentor = mentorGo.GetComponent<TownNPCInteractable>();
            if (mentor == null) { Debug.LogError("MINI-056 VALIDATION FAIL: NPC_RastaMentor has no TownNPCInteractable."); return; }

            var economy = GameObject.Find("EconomyManager")?.GetComponent<EconomyManager>();
            var progression = GameObject.Find("ProgressionManager")?.GetComponent<ProgressionManager>();
            if (economy == null || progression == null)
            {
                Debug.LogError("MINI-056 VALIDATION FAIL: EconomyManager/ProgressionManager not found in the built scene.");
                return;
            }
            // Same reason as every other validation harness in this
            // project: AddComponent doesn't run Awake outside Play Mode,
            // but these ARE already-built scene objects whose Awake ran
            // when Mini011PhaseBSetup's SerializedObject writes were
            // applied... except MonoBehaviours added via script still
            // need it invoked explicitly here, same as always.
            InvokeMethod(economy, "Awake");
            InvokeMethod(progression, "Awake");

            var actor = new GameObject("Actor");
            bool pass = true;
            string fail = null;

            mentor.Interact(actor);
            string fb = mentor.GetInteractionFeedback();
            Check(ref pass, ref fail, !fb.Contains("Black Sugar") && !fb.Contains("Purple work") && !fb.Contains("top a di mountain"),
                $"step 1 (nothing unlocked): expected a fallback line, got '{fb}'.");

            if (pass)
            {
                progression.RecordBossJob();
                progression.RecordBossJob(); // BossKReputation=20, stage=2 -> Black Sugar unlocked
                mentor.Interact(actor);
                fb = mentor.GetInteractionFeedback();
                Check(ref pass, ref fail, fb.Contains("Black Sugar a grow good"),
                    $"step 2 (Black Sugar unlocked): expected the Black Sugar line, got '{fb}'.");
            }

            if (pass)
            {
                // RecordBossJob itself also adds GrandBayGangs +5 each
                // call (3 calls so far across steps 2-3 = +15) - accounted
                // for here so total GangReputation lands at exactly 25
                // (Purple's own threshold) without also crossing Blue
                // Cheese's higher 30 threshold early.
                progression.RecordBossJob(); // stage=3, GangReputation +5 (15 total)
                progression.AddReputation(Faction.GrandBayGangs, 10); // 25 total -> Purple unlocked, Blue Cheese not yet
                mentor.Interact(actor);
                fb = mentor.GetInteractionFeedback();
                Check(ref pass, ref fail, fb.Contains("Purple work no easy") && !fb.Contains("Black Sugar a grow good"),
                    $"step 3 (Purple unlocked too): expected the deeper Purple line to win over the Black Sugar one, got '{fb}'.");
            }

            if (pass)
            {
                progression.AddReputation(Faction.GrandBayGangs, 5); // 30 total -> Blue Cheese unlocked too
                mentor.Interact(actor);
                fb = mentor.GetInteractionFeedback();
                Check(ref pass, ref fail, fb.Contains("top a di mountain"),
                    $"step 4 (all three unlocked): expected the deepest Blue Cheese line to win, got '{fb}'.");
            }

            UnityEngine.Object.DestroyImmediate(actor);

            if (pass)
            {
                Debug.Log("MINI-056 RASTA MENTOR VALIDATION PASS: the mentor's dialogue starts on a fallback line, switches to Black Sugar's line once that tier unlocks, then to Purple's (not Black Sugar's) once both are unlocked, and finally to Blue Cheese's once all three are - the deepest-earned tier always wins, checked against the real built scene's NPC and DialogueSet.");
            }
            else
            {
                Debug.LogError($"MINI-056 RASTA MENTOR VALIDATION FAIL: {fail}");
            }
        }

        private static void Check(ref bool pass, ref string fail, bool condition, string message)
        {
            if (pass && !condition) { pass = false; fail = message; }
        }

        private static void InvokeMethod(object target, string name)
        {
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);
        }
    }
}
