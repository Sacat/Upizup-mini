using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;
using UpIzUpMini.Interaction;
using UpIzUpMini.Progression;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-060, follow-up revision: Gardey Zafeh no longer has her own
    /// NPC - the Boat Man now offers both services as an explicit choice
    /// (E = produce trade, R = Gardey Zafeh - "make a dialogue to choose
    /// which option you want", per the user). This harness targets
    /// `TownNPCInteractable.InteractGardeyZafeh()` (the R path) directly
    /// against the real `NPC_BoatMan`, since E's trade path is unchanged
    /// GuadeloupeTrade.cs logic already covered by MINI-043's own history.
    /// </summary>
    public static class Mini060GardeyZafehValidation
    {
        [MenuItem("Up Iz Up Mini/MINI-060/Validate Gardey Zafeh")]
        public static void Validate()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            bool pass = true;
            string fail = null;

            var boatManGo = GameObject.Find("NPC_BoatMan");
            var economy = GameObject.Find("EconomyManager")?.GetComponent<EconomyManager>();
            var progression = GameObject.Find("ProgressionManager")?.GetComponent<ProgressionManager>();

            if (boatManGo == null || economy == null || progression == null)
            {
                Debug.LogError("MINI-060 VALIDATION FAIL: NPC_BoatMan/EconomyManager/ProgressionManager not found in the built scene.");
                return;
            }

            // Confirm the old separate NPC is really gone, not just unused.
            if (GameObject.Find("NPC_GardeyZafeh") != null)
            {
                Debug.LogError("MINI-060 VALIDATION FAIL: NPC_GardeyZafeh still exists - expected her to be consolidated into NPC_BoatMan.");
                return;
            }

            var boatMan = boatManGo.GetComponent<TownNPCInteractable>();
            InvokeMethod(economy, "Awake");
            InvokeMethod(progression, "Awake");

            // --- 1) R without enough money -> declined, no reveal, no charge. ---
            string fb = boatMan.InteractGardeyZafeh();
            Check(ref pass, ref fail, !progression.DogLifeRevealed, "expected the reveal to NOT happen without enough money.");
            // MINI-060 follow-up-2: reveal price raised 1000 -> 3000 per
            // the user's "boat man price should be 3000".
            Check(ref pass, ref fail, fb.Contains("$3000"), $"expected a '$3000' price line when short of funds, got '{fb}'.");

            // --- 2) R with enough money -> exact charge, exact reveal, names Dog Life. ---
            if (pass)
            {
                economy.AddMoney(4000);
                int moneyBefore = economy.Money;
                fb = boatMan.InteractGardeyZafeh();
                Check(ref pass, ref fail, progression.DogLifeRevealed, "expected DogLifeRevealed to be true after paying for the reveal.");
                Check(ref pass, ref fail, economy.Money == moneyBefore - 3000, $"expected the reveal to cost exactly 3000, dropped by {moneyBefore - economy.Money}.");
                Check(ref pass, ref fail, fb.Contains("Dog Life"), $"expected the reveal line to name Dog Life, got '{fb}'.");
            }

            // --- 3) Next R: a free reading, one buff active. ---
            if (pass)
            {
                int moneyBefore = economy.Money;
                fb = boatMan.InteractGardeyZafeh();
                Check(ref pass, ref fail, GardeyZafehBuffState.AnyActive, "expected a reading to leave a buff active.");
                Check(ref pass, ref fail, economy.Money == moneyBefore, $"expected a reading to be free, but money changed from {moneyBefore} to {economy.Money}.");
                Check(ref pass, ref fail, fb.Contains("jalousie"), $"expected the jalousie flavour line on every reading, got '{fb}'.");
            }

            // --- 4) A second R while one reading is still active -> refused, no new grant. ---
            if (pass)
            {
                var activeBefore = GardeyZafehBuffState.Active;
                fb = boatMan.InteractGardeyZafeh();
                Check(ref pass, ref fail, fb.Contains("left on it"), $"expected a cooldown refusal while a reading is active, got '{fb}'.");
                Check(ref pass, ref fail, GardeyZafehBuffState.Active == activeBefore, "expected the active buff to be unchanged by a refused second visit.");
            }

            // --- 5) Each buff actually does what it claims (forced directly, not via the random draw). ---
            if (pass)
            {
                var vitalsGo = new GameObject("TestVitals");
                var vitals = vitalsGo.AddComponent<CharacterVitals>();
                InvokeMethod(vitals, "Awake");

                GardeyZafehBuffState.Grant(GardeyZafehBuff.HealthBoost, 60f);
                vitals.Damage(9999f);
                Check(ref pass, ref fail, !vitals.IsDead && vitals.Health >= vitals.MaxHealth - 0.01f,
                    $"expected HealthBoost to block damage entirely, health={vitals.Health}/{vitals.MaxHealth}.");

                GardeyZafehBuffState.Grant(GardeyZafehBuff.DoubleMoney, 60f);
                int before = economy.Money;
                economy.AddMoney(100);
                Check(ref pass, ref fail, economy.Money == before + 200, $"expected DoubleMoney to double a +100 gain to +200, got a change of {economy.Money - before}.");

                int beforeCost = economy.Money;
                economy.AddMoney(-50); // a cost, must NOT be doubled
                Check(ref pass, ref fail, economy.Money == beforeCost - 50, $"expected DoubleMoney to leave a cost (-50) untouched, got a change of {economy.Money - beforeCost}.");

                Object.DestroyImmediate(vitalsGo);
            }

            if (pass)
            {
                Debug.Log("MINI-060 GARDEY ZAFEH VALIDATION PASS (Boat Man consolidation): NPC_GardeyZafeh no longer exists, R on the Boat Man is refused without $3000, costs exactly that and names Dog Life once paid, every reading after that is free via R, only one reading is active at a time (a second R is refused with a wait line), and HealthBoost/DoubleMoney genuinely do what they claim (a cost is never doubled). Note: DisappearForTrip's actual away/return timing is NOT exercised here - Update() ticks don't advance in edit-mode batch runs, same limitation as PoliceReinforcementSpawner's walk-off coroutine.");
            }
            else
            {
                Debug.LogError($"MINI-060 GARDEY ZAFEH VALIDATION FAIL: {fail}");
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
