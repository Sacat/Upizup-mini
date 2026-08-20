using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UpIzUpMini.Dialogue;
using UpIzUpMini.Economy;
using UpIzUpMini.Progression;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-053: proves DialogueSet.SelectLine's actual behaviour rather
    /// than just "it compiles" - a real EconomyManager/ProgressionManager
    /// pair is instantiated in a throwaway scene so DialogueCondition's
    /// singleton reads (EconomyManager.Instance, ProgressionManager.
    /// Instance) resolve to real, controllable state, not nulls.
    /// </summary>
    public static class Mini053DialogueValidation
    {
        [MenuItem("Up Iz Up Mini/MINI-053/Validate Dialogue Selection")]
        public static void Validate()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // AddComponent does not run Awake outside Play Mode in this
            // batch-mode context (confirmed the hard way - EconomyManager.
            // Instance stayed null after AddHeat worked fine on the
            // instance itself), so Awake is invoked explicitly, matching
            // the established pattern in Mini049CheatCodeValidation.
            var economy = new GameObject("Economy").AddComponent<EconomyManager>();
            InvokeMethod(economy, "Awake");
            var progression = new GameObject("Progression").AddComponent<ProgressionManager>();
            InvokeMethod(progression, "Awake");

            var set = ScriptableObject.CreateInstance<DialogueSet>();
            set.lines = new List<DialogueLine>
            {
                new DialogueLine
                {
                    category = DialogueCategory.InnerThought,
                    text = "heat-gated",
                    conditions = new List<DialogueCondition>
                    {
                        new DialogueCondition { type = DialogueConditionType.MinHeat, threshold = 40 }
                    }
                },
                new DialogueLine
                {
                    category = DialogueCategory.Faction,
                    text = "rep-gated",
                    conditions = new List<DialogueCondition>
                    {
                        new DialogueCondition
                        {
                            type = DialogueConditionType.MinReputation,
                            faction = Faction.GrandBayGangs,
                            threshold = 25
                        }
                    }
                },
                new DialogueLine { category = DialogueCategory.Normal, text = "always-a" },
                new DialogueLine { category = DialogueCategory.Normal, text = "always-b" },
                new DialogueLine { category = DialogueCategory.Normal, text = "always-c" },
            };

            bool pass = true;
            string fail = null;

            // 1) Baseline (heat 0, no reputation): only the three
            // unconditional lines are eligible, and repeated calls cycle
            // through all three rather than always returning the first.
            var seen = new HashSet<string>();
            for (int i = 0; i < 3; i++)
            {
                var line = set.SelectLine();
                if (line == null || line.category != DialogueCategory.Normal)
                {
                    pass = false; fail = $"baseline call {i}: expected an unconditional Normal line, got {(line == null ? "null" : line.text)}";
                    break;
                }
                seen.Add(line.text);
            }
            if (pass && seen.Count != 3)
            {
                pass = false; fail = $"baseline cycling: expected 3 distinct lines across 3 calls, saw {seen.Count} ({string.Join(",", seen)})";
            }

            // 2) Raise heat above threshold - the conditional InnerThought
            // line must now win over every unconditional line, every call
            // (it is strictly more specific: 1 condition beats 0).
            if (pass)
            {
                economy.AddHeat(60f);
                for (int i = 0; i < 3; i++)
                {
                    var line = set.SelectLine();
                    if (line == null || line.text != "heat-gated")
                    {
                        pass = false; fail = $"high-heat call {i}: expected the heat-gated line to win, got {(line == null ? "null" : line.text)}";
                        break;
                    }
                }
            }

            // 3) Also raise gang reputation past its threshold - now TWO
            // lines are tied at specificity 1 (heat-gated, rep-gated); the
            // selector must cycle between exactly those two, never fall
            // back to an unconditional line while either conditional line
            // is still eligible.
            if (pass)
            {
                progression.AddReputation(Faction.GrandBayGangs, 30);
                var tieSeen = new HashSet<string>();
                for (int i = 0; i < 4; i++)
                {
                    var line = set.SelectLine();
                    if (line == null || (line.text != "heat-gated" && line.text != "rep-gated"))
                    {
                        pass = false; fail = $"tie call {i}: expected one of the two tied conditional lines, got {(line == null ? "null" : line.text)}";
                        break;
                    }
                    tieSeen.Add(line.text);
                }
                if (pass && tieSeen.Count != 2)
                {
                    pass = false; fail = $"tie cycling: expected both tied conditional lines to appear across 4 calls, saw {tieSeen.Count} ({string.Join(",", tieSeen)})";
                }
            }

            // 4) Heat and reputation both drop back down - selection must
            // fall back to the unconditional lines again (conditions are
            // re-evaluated live every call, not cached from selection #1).
            if (pass)
            {
                economy.AddHeat(-100f);
                progression.AddReputation(Faction.GrandBayGangs, -100);
                var line = set.SelectLine();
                if (line == null || line.category != DialogueCategory.Normal)
                {
                    pass = false; fail = $"fallback after cooling off: expected an unconditional Normal line again, got {(line == null ? "null" : line.text)}";
                }
            }

            if (pass)
            {
                Debug.Log("MINI-053 DIALOGUE VALIDATION PASS: unconditional lines cycle by default, a single eligible conditional line always wins over unconditional ones, two simultaneously-eligible conditional lines cycle between just themselves, and eligibility is re-evaluated live (not cached) once conditions stop matching.");
            }
            else
            {
                Debug.LogError($"MINI-053 DIALOGUE VALIDATION FAIL: {fail}");
            }
        }

        private static void InvokeMethod(object target, string name)
        {
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);
        }
    }
}
