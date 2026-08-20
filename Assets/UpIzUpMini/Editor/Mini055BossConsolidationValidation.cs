using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Economy;
using UpIzUpMini.Interaction;
using UpIzUpMini.Progression;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-055: proves the consolidated Boss C actually offers the right
    /// strain at the right price as progression unlocks cross, in the
    /// right order, with the right fallback messages - not just "it
    /// compiles and the scene builds." Real EconomyManager/
    /// ProgressionManager instances drive real unlock thresholds
    /// (RecordBossJob/AddReputation), same as real play would.
    /// </summary>
    public static class Mini055BossConsolidationValidation
    {
        [MenuItem("Up Iz Up Mini/MINI-055/Validate Boss Consolidation")]
        public static void Validate()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            bool pass = true;
            string fail = null;

            try
            {
                var economy = new GameObject("Economy").AddComponent<EconomyManager>();
                InvokeMethod(economy, "Awake");
                economy.AddMoney(2000);

                var progression = new GameObject("Progression").AddComponent<ProgressionManager>();
                InvokeMethod(progression, "Awake");

                var blackSugar = MakeCrop("black_sugar", "Black Sugar");
                var purple = MakeCrop("purple", "Purple");
                var blueCheese = MakeCrop("blue_cheese", "Blue Cheese");

                var npcGo = new GameObject("TestBossC");
                var npc = npcGo.AddComponent<TownNPCInteractable>();
                var so = new SerializedObject(npc);
                so.FindProperty("role").enumValueIndex = (int)NpcRole.StrainBoss;
                so.FindProperty("npcName").stringValue = "BossC";
                var cropsProp = so.FindProperty("multiBossCrops");
                cropsProp.arraySize = 3;
                cropsProp.GetArrayElementAtIndex(0).objectReferenceValue = blackSugar;
                cropsProp.GetArrayElementAtIndex(1).objectReferenceValue = purple;
                cropsProp.GetArrayElementAtIndex(2).objectReferenceValue = blueCheese;
                var pricesProp = so.FindProperty("multiBossSeedPrices");
                pricesProp.arraySize = 3;
                pricesProp.GetArrayElementAtIndex(0).intValue = 320;
                pricesProp.GetArrayElementAtIndex(1).intValue = 500;
                pricesProp.GetArrayElementAtIndex(2).intValue = 650;
                so.ApplyModifiedPropertiesWithoutUndo();

                var actor = new GameObject("Actor");

                // 1) Nothing unlocked yet -> "not ready" fallback.
                npc.Interact(actor);
                string fb = npc.GetInteractionFeedback();
                Check(ref pass, ref fail, fb.Contains("not ready"), $"step 1 (nothing unlocked): expected a 'not ready' line, got '{fb}'.");

                // 2) Unlock Black Sugar only (BossKReputation>=20, stage>=2).
                if (pass)
                {
                    progression.RecordBossJob();
                    progression.RecordBossJob(); // stage=2, BossKReputation=20
                    int moneyBefore = economy.Money;
                    npc.Interact(actor);
                    fb = npc.GetInteractionFeedback();
                    Check(ref pass, ref fail, fb.Contains("Black Sugar") && fb.Contains("$320"),
                        $"step 2 (Black Sugar unlocked): expected a Black Sugar offer at $320, got '{fb}'.");
                    Check(ref pass, ref fail, economy.GetSeeds("black_sugar") == 3,
                        $"step 2: expected 3 Black Sugar seeds after purchase, got {economy.GetSeeds("black_sugar")}.");
                    Check(ref pass, ref fail, economy.Money == moneyBefore - 320,
                        $"step 2: expected money to drop by exactly 320 (from {moneyBefore}), got {economy.Money}.");
                }

                // 3) Black Sugar already seeded, nothing else unlocked yet -> "bring back" fallback.
                if (pass)
                {
                    npc.Interact(actor);
                    fb = npc.GetInteractionFeedback();
                    Check(ref pass, ref fail, fb.Contains("Bring back"),
                        $"step 3 (already seeded, nothing else unlocked): expected a 'bring back' line, got '{fb}'.");
                }

                // 4) Unlock Purple + Blue Cheese too (stage>=3, GangRep>=30).
                if (pass)
                {
                    progression.RecordBossJob(); // stage=3
                    progression.AddReputation(Faction.GrandBayGangs, 30);
                    npc.Interact(actor);
                    fb = npc.GetInteractionFeedback();
                    Check(ref pass, ref fail, fb.Contains("Purple") && !fb.Contains("Blue Cheese") && fb.Contains("$500"),
                        $"step 4 (Purple+Blue Cheese unlocked, array order): expected Purple offered before Blue Cheese at $500, got '{fb}'.");
                }

                // 5) Purple now seeded -> Blue Cheese offered next.
                if (pass)
                {
                    npc.Interact(actor);
                    fb = npc.GetInteractionFeedback();
                    Check(ref pass, ref fail, fb.Contains("Blue Cheese") && fb.Contains("$650"),
                        $"step 5 (Blue Cheese next): expected a Blue Cheese offer at $650, got '{fb}'.");
                }

                // 6) All three unlocked and seeded -> "bring back" fallback again.
                if (pass)
                {
                    npc.Interact(actor);
                    fb = npc.GetInteractionFeedback();
                    Check(ref pass, ref fail, fb.Contains("Bring back"),
                        $"step 6 (all seeded): expected a 'bring back' line, got '{fb}'.");
                }

                UnityEngine.Object.DestroyImmediate(actor);
                UnityEngine.Object.DestroyImmediate(npcGo);
                UnityEngine.Object.DestroyImmediate(blackSugar);
                UnityEngine.Object.DestroyImmediate(purple);
                UnityEngine.Object.DestroyImmediate(blueCheese);
            }
            catch (Exception e)
            {
                pass = false;
                fail = $"unexpected exception: {e}";
            }

            if (pass)
            {
                Debug.Log("MINI-055 BOSS CONSOLIDATION VALIDATION PASS: the consolidated Boss C offers no strain until unlocked, offers the correct next unseeded strain at its own price in array order once unlocked, and falls back to a 'bring back' line once every currently-unlocked strain is already seeded.");
            }
            else
            {
                Debug.LogError($"MINI-055 BOSS CONSOLIDATION VALIDATION FAIL: {fail}");
            }
        }

        private static void Check(ref bool pass, ref string fail, bool condition, string message)
        {
            if (pass && !condition) { pass = false; fail = message; }
        }

        private static CropDefinition MakeCrop(string id, string name)
        {
            var crop = ScriptableObject.CreateInstance<CropDefinition>();
            crop.cropId = id;
            crop.displayName = name;
            crop.isIllegal = true;
            return crop;
        }

        private static void InvokeMethod(object target, string name)
        {
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);
        }
    }
}
