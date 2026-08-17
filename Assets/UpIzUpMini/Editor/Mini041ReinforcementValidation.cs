using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UpIzUpMini.Economy;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-041. Exercises PoliceReinforcementSpawner's hysteresis math
    /// against a real EconomyManager heat sequence, reading the private
    /// `_activeCount` field via reflection (Update() and Start() don't run
    /// outside Play mode, so both are invoked manually - same pattern as
    /// Mini028FarmhandValidation/Mini038CombatValidation). The walk-off
    /// coroutine itself starts but can't tick without the player loop
    /// running, so this proves the spawn/despawn *decision*, not the
    /// visual walk-away - that's what the headless player run is for.
    /// </summary>
    public static class Mini041ReinforcementValidation
    {
        [MenuItem("Up Iz Up Mini/MINI-041/Run Reinforcement Validation")]
        public static void Run()
        {
            var economyGo = new GameObject("TestEconomy");
            var economy = economyGo.AddComponent<EconomyManager>();
            InvokeAwake(economy);

            var template = new GameObject("TestOfficerTemplate");
            template.AddComponent<CharacterController>();

            var spawnerGo = new GameObject("TestSpawner");
            var spawner = spawnerGo.AddComponent<PoliceReinforcementSpawner>();
            var so = new SerializedObject(spawner);
            so.FindProperty("officerTemplate").objectReferenceValue = template;
            so.ApplyModifiedPropertiesWithoutUndo();

            InvokeMethod(spawner, "Start");

            SetHeat(economy, 0f);
            InvokeMethod(spawner, "Update");
            AssertActiveCount(spawner, 0, "at heat 0");

            SetHeat(economy, 60f);
            InvokeMethod(spawner, "Update");
            AssertActiveCount(spawner, 1, "at heat 60 (>= firstSpawnHeat 55)");

            // The bug this fixes: heat dipping back under the spawn
            // threshold (but still above the despawn threshold) must NOT
            // immediately undo the spawn.
            SetHeat(economy, 50f);
            InvokeMethod(spawner, "Update");
            AssertActiveCount(spawner, 1, "at heat 50 (below spawn threshold but above despawn threshold - must stay spawned)");

            SetHeat(economy, 90f);
            InvokeMethod(spawner, "Update");
            AssertActiveCount(spawner, 2, "at heat 90 (>= secondSpawnHeat 85)");

            SetHeat(economy, 60f);
            InvokeMethod(spawner, "Update");
            AssertActiveCount(spawner, 1, "at heat 60 (< secondDespawnHeat 65 - drop from 2 to 1)");

            SetHeat(economy, 20f);
            InvokeMethod(spawner, "Update");
            AssertActiveCount(spawner, 0, "at heat 20 (< firstDespawnHeat 35 - both should be gone)");

            Debug.Log("MINI-041 REINFORCEMENT VALIDATION PASS: spawn thresholds raise, despawn thresholds are strictly lower (hysteresis holds), no premature undo of a spawn from a small heat dip.");

            UnityEngine.Object.DestroyImmediate(spawnerGo);
            UnityEngine.Object.DestroyImmediate(template);
            UnityEngine.Object.DestroyImmediate(economyGo);
        }

        private static void SetHeat(EconomyManager economy, float heat)
        {
            economy.LoadState(economy.Money, heat, new List<string>(), new List<int>());
        }

        private static void AssertActiveCount(PoliceReinforcementSpawner spawner, int expected, string context)
        {
            int actual = (int)typeof(PoliceReinforcementSpawner)
                .GetField("_activeCount", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(spawner);
            if (actual != expected)
                throw new Exception($"MINI-041 validation failed {context}: expected _activeCount={expected}, got {actual}.");
        }

        private static void InvokeAwake(MonoBehaviour behaviour) => InvokeMethod(behaviour, "Awake");

        private static void InvokeMethod(MonoBehaviour behaviour, string name)
        {
            behaviour.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(behaviour, null);
        }
    }
}
