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
    /// MINI-057: proves Normy's bribe mechanic against the real built
    /// scene's NPC and EconomyManager/ProgressionManager - cost/heat
    /// reduction/cooldown/relationship pricing, not just "it compiles."
    /// </summary>
    public static class Mini057NormyValidation
    {
        [MenuItem("Up Iz Up Mini/MINI-057/Validate Normy")]
        public static void Validate()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var normyGo = GameObject.Find("NPC_Normy");
            if (normyGo == null) { Debug.LogError("MINI-057 VALIDATION FAIL: NPC_Normy not found in the built scene."); return; }
            var normy = normyGo.GetComponent<TownNPCInteractable>();
            if (normy == null) { Debug.LogError("MINI-057 VALIDATION FAIL: NPC_Normy has no TownNPCInteractable."); return; }

            var economy = GameObject.Find("EconomyManager")?.GetComponent<EconomyManager>();
            var progression = GameObject.Find("ProgressionManager")?.GetComponent<ProgressionManager>();
            if (economy == null || progression == null)
            {
                Debug.LogError("MINI-057 VALIDATION FAIL: EconomyManager/ProgressionManager not found in the built scene.");
                return;
            }
            InvokeMethod(economy, "Awake");
            InvokeMethod(progression, "Awake");
            economy.AddMoney(1000);

            var actor = new GameObject("Actor");
            bool pass = true;
            string fail = null;

            // 1) No heat -> a flavour/fallback line, not a bribe transaction.
            normy.Interact(actor);
            string fb = normy.GetInteractionFeedback();
            Check(ref pass, ref fail, !fb.Contains("$"),
                $"step 1 (no heat): expected a flavour line with no price, got '{fb}'.");

            // 2) Real heat -> a paid bribe that actually reduces heat.
            if (pass)
            {
                economy.AddHeat(60f);
                float heatBefore = economy.Heat;
                int moneyBefore = economy.Money;
                normy.Interact(actor);
                fb = normy.GetInteractionFeedback();
                Check(ref pass, ref fail, fb.Contains("$80") && fb.Contains("forgotten"),
                    $"step 2 (first bribe, reputation 0): expected the $80 bribe line, got '{fb}'.");
                Check(ref pass, ref fail, Mathf.Approximately(economy.Heat, heatBefore - 35f),
                    $"step 2: expected heat to drop by exactly 35 (from {heatBefore}), got {economy.Heat}.");
                Check(ref pass, ref fail, economy.Money == moneyBefore - 80,
                    $"step 2: expected money to drop by exactly 80 (from {moneyBefore}), got {economy.Money}.");
                Check(ref pass, ref fail, progression.GetReputation(Faction.Normy) == 5,
                    $"step 2: expected NormyReputation to be exactly 5 after one bribe, got {progression.GetReputation(Faction.Normy)}.");
            }

            // 3) Still on cooldown -> no second charge even with heat present.
            if (pass)
            {
                int moneyBefore = economy.Money;
                normy.Interact(actor);
                fb = normy.GetInteractionFeedback();
                Check(ref pass, ref fail, fb.Contains("minute"),
                    $"step 3 (on cooldown): expected the cooldown line, got '{fb}'.");
                Check(ref pass, ref fail, economy.Money == moneyBefore,
                    $"step 3: expected no charge while on cooldown, but money changed from {moneyBefore} to {economy.Money}.");
            }

            // 4) Bypass the cooldown (reflection - this is what the timer
            // gates, and Time.time doesn't advance outside Play Mode) and
            // confirm a second bribe is CHEAPER now that NormyReputation
            // is 5 - the "relationship" the brief asked for.
            if (pass)
            {
                SetField(normy, "_normyReadyAt", 0f);
                economy.AddHeat(60f);
                int moneyBefore = economy.Money;
                normy.Interact(actor);
                fb = normy.GetInteractionFeedback();
                Check(ref pass, ref fail, fb.Contains("$75"),
                    $"step 4 (second bribe, reputation 5): expected a cheaper $75 bribe (80 - 5 reputation), got '{fb}'.");
                Check(ref pass, ref fail, economy.Money == moneyBefore - 75,
                    $"step 4: expected money to drop by exactly 75, got a drop of {moneyBefore - economy.Money}.");
            }

            UnityEngine.Object.DestroyImmediate(actor);

            if (pass)
            {
                Debug.Log("MINI-057 NORMY VALIDATION PASS: no charge when there's no heat to bribe away, a real bribe correctly costs $80/reduces heat by 35/grants +5 reputation, the cooldown genuinely blocks a second charge, and repeat business (higher NormyReputation) makes the next bribe cheaper.");
            }
            else
            {
                Debug.LogError($"MINI-057 NORMY VALIDATION FAIL: {fail}");
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

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            field?.SetValue(target, value);
        }
    }
}
