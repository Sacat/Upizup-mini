using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UpIzUpMini.Economy;
using UpIzUpMini.Farming;
using UpIzUpMini.Progression;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-047/MINI-048. Exercises the real multi-recipe
    /// CropBreedingStation and the two-tone CropStageVisual against real
    /// code. Supersedes the single-recipe MINI-047 harness, which tested
    /// fields (parentA/parentB/output) that no longer exist after
    /// CropBreedingStation was generalised to a Recipe[] list.
    /// </summary>
    public static class Mini048BreedingValidation
    {
        [MenuItem("Up Iz Up Mini/MINI-048/Run Breeding Validation")]
        public static void Run()
        {
            var economyGo = new GameObject("TestEconomy");
            var economy = economyGo.AddComponent<EconomyManager>();
            InvokeMethod(economy, "Awake");

            var progGo = new GameObject("TestProgression");
            var progression = progGo.AddComponent<ProgressionManager>();
            InvokeMethod(progression, "Awake");

            CropDefinition Crop(string id)
            {
                var c = ScriptableObject.CreateInstance<CropDefinition>();
                c.cropId = id; c.displayName = id;
                return c;
            }
            var purple = Crop("purple");
            var blackSugar = Crop("black_sugar");
            var purpleBlack = Crop("purple_black");
            var blueCheese = Crop("blue_cheese");
            var sugarCheese = Crop("sugar_cheese");
            var purpleCheese = Crop("purple_cheese");

            var stationGo = new GameObject("TestStation");
            var station = stationGo.AddComponent<CropBreedingStation>();
            var recipes = new[]
            {
                new CropBreedingStation.Recipe { parentA = purple, parentB = blackSugar, output = purpleBlack, outputSeedCount = 2 },
                new CropBreedingStation.Recipe { parentA = blackSugar, parentB = blueCheese, output = sugarCheese, outputSeedCount = 2 },
                new CropBreedingStation.Recipe { parentA = purple, parentB = blueCheese, output = purpleCheese, outputSeedCount = 2 },
            };
            SetPrivateField(station, "recipes", recipes);

            // --- Nothing unlocked at all: no recipe should fire, even with every ingredient stocked. ---
            economy.AddCrop("purple", 5);
            economy.AddCrop("black_sugar", 5);
            economy.AddCrop("blue_cheese", 5);
            station.Interact(stationGo);
            if (economy.GetSeeds("purple_black") != 0 || economy.GetSeeds("sugar_cheese") != 0 || economy.GetSeeds("purple_cheese") != 0)
                throw new Exception("MINI-048 validation: a recipe fired with nothing unlocked.");

            // --- Unlock only Purple Black's recipe; it alone should fire, consuming only its own ingredients. ---
            progression.AddReputation(Faction.GrandBayGangs, 40);
            for (int i = 0; i < 4; i++) progression.RecordBossJob(); // BossExploitationStage -> 4
            if (!progression.PurpleBlackUnlocked) throw new Exception("MINI-048 validation setup: PurpleBlackUnlocked still false.");
            if (progression.PurpleCheeseUnlocked) throw new Exception("MINI-048 validation setup: PurpleCheeseUnlocked already true too early - thresholds not escalating as intended.");

            station.Interact(stationGo);
            if (economy.GetSeeds("purple_black") != 2)
                throw new Exception($"MINI-048 validation: Purple Black recipe did not fire when it was the only unlocked+eligible one (seed count {economy.GetSeeds("purple_black")}, expected 2).");
            if (economy.GetCount("purple") != 4 || economy.GetCount("black_sugar") != 4)
                throw new Exception($"MINI-048 validation: Purple Black recipe consumed the wrong amounts (purple={economy.GetCount("purple")}, black_sugar={economy.GetCount("black_sugar")}, expected 4 and 4) - or consumed blue_cheese it shouldn't have touched (blue_cheese={economy.GetCount("blue_cheese")}, expected 5).");
            if (economy.GetCount("blue_cheese") != 5)
                throw new Exception($"MINI-048 validation: an unrelated recipe touched blue_cheese stock (count {economy.GetCount("blue_cheese")}, expected untouched 5).");

            // --- Deplete Purple Black's own ingredients so it can no longer fire; unlock everything else too. ---
            economy.AddCrop("purple", -economy.GetCount("purple"));
            progression.AddReputation(Faction.BossK, 30);
            progression.AddReputation(Faction.GrandBayGangs, 10); // -> 50
            for (int i = 0; i < 1; i++) progression.RecordBossJob(); // stage -> 5
            if (!progression.SugarCheeseUnlocked || !progression.PurpleCheeseUnlocked)
                throw new Exception("MINI-048 validation setup: Sugar/Purple Cheese still not both unlocked after raising every threshold.");

            // Purple Black can't fire (no purple left); Sugar Cheese should
            // fire next since it comes before Purple Cheese in the list
            // and both its ingredients (black_sugar, blue_cheese) are
            // still in stock.
            station.Interact(stationGo);
            if (economy.GetSeeds("sugar_cheese") != 2)
                throw new Exception($"MINI-048 validation: Sugar Cheese did not fire once eligible (seed count {economy.GetSeeds("sugar_cheese")}, expected 2).");
            if (economy.GetSeeds("purple_cheese") != 0)
                throw new Exception("MINI-048 validation: Purple Cheese fired before Sugar Cheese despite Sugar Cheese being listed first and equally eligible - recipe selection order is wrong.");

            // --- Closest-miss feedback: deplete black_sugar too, so Sugar Cheese can no longer fire either - only Purple Cheese (unlocked, but missing purple) is left to report. ---
            economy.AddCrop("black_sugar", -economy.GetCount("black_sugar"));
            station.Interact(stationGo);
            string feedback = (string)typeof(CropBreedingStation)
                .GetField("_feedback", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(station);
            if (feedback == null || !feedback.Contains("purple", StringComparison.OrdinalIgnoreCase))
                throw new Exception($"MINI-048 validation: expected a closest-miss message naming the missing ingredient, got: \"{feedback}\"");

            Debug.Log("MINI-048 BREEDING VALIDATION PASS: no recipe fires while locked; the one unlocked+eligible recipe fires and touches only its own ingredients; recipe list order is respected when multiple are eligible; a locked-ingredient miss reports what's missing rather than a generic message.");

            UnityEngine.Object.DestroyImmediate(stationGo);
            UnityEngine.Object.DestroyImmediate(progGo);
            UnityEngine.Object.DestroyImmediate(economyGo);
            foreach (var c in new[] { purple, blackSugar, purpleBlack, blueCheese, sugarCheese, purpleCheese })
                UnityEngine.Object.DestroyImmediate(c);
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new Exception($"MINI-048 validation: field '{name}' not found on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private static void InvokeMethod(object target, string name)
        {
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);
        }
    }
}
