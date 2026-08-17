using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;
using UpIzUpMini.Progression;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-043. Exercises the real GuadeloupeTrade/ProgressionManager code
    /// for the staged NPC-courier-first unlock: the first Interact() must
    /// not touch either playable character, the completed trip must unlock
    /// character dispatch, and the second Interact() must then actually
    /// send and lock a character. Time-driven completion (Update() polling
    /// _returnAt) doesn't run outside Play mode, so CompleteTrip is invoked
    /// directly via reflection rather than waiting - same pattern as the
    /// other MINI-03x validation harnesses.
    /// </summary>
    public static class Mini043GuadeloupeValidation
    {
        [MenuItem("Up Iz Up Mini/MINI-043/Run Guadeloupe Validation")]
        public static void Run()
        {
            var economyGo = new GameObject("TestEconomy");
            var economy = economyGo.AddComponent<EconomyManager>();
            InvokeMethod(economy, "Awake");

            var progGo = new GameObject("TestProgression");
            var progression = progGo.AddComponent<ProgressionManager>();
            InvokeMethod(progression, "Awake");
            progression.AddReputation(Faction.BossK, 20);
            progression.AddReputation(Faction.GrandBayGangs, 10);
            if (!progression.GrandBayWeedRouteEstablished)
                throw new Exception("MINI-043 validation setup failed: GrandBayWeedRouteEstablished still false.");

            var char0Root = new GameObject("TestChar0"); char0Root.AddComponent<CharacterController>();
            var char1Root = new GameObject("TestChar1"); char1Root.AddComponent<CharacterController>();
            var switchGo = new GameObject("TestSwitch");
            var switcher = switchGo.AddComponent<CharacterSwitchManager>();
            var slots = new[]
            {
                new CharacterSlot { displayName = "Deril", root = char0Root },
                new CharacterSlot { displayName = "Franki", root = char1Root },
            };
            typeof(CharacterSwitchManager).GetField("slots", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(switcher, slots);
            InvokeMethod(switcher, "Awake");
            InvokeMethod(switcher, "Start");

            var crop = ScriptableObject.CreateInstance<CropDefinition>();
            crop.cropId = "test_tomato";
            crop.sellPrice = 10;

            var tradeGo = new GameObject("TestTrade");
            var trade = tradeGo.AddComponent<GuadeloupeTrade>();
            var so = new SerializedObject(trade);
            var cropsProp = so.FindProperty("sellableCrops");
            cropsProp.arraySize = 1;
            cropsProp.GetArrayElementAtIndex(0).objectReferenceValue = crop;
            so.ApplyModifiedPropertiesWithoutUndo();
            InvokeMethod(trade, "Awake");

            economy.AddMoney(2000);
            economy.AddCrop("test_tomato", 20);

            // --- First run: must be an NPC courier, no character touched. ---
            string msg1 = trade.Interact();
            if (!trade.TripActive) throw new Exception($"MINI-043 validation: first Interact() did not start a trip. Message: {msg1}");
            if (trade.AwayCharacterIndex != -1)
                throw new Exception($"MINI-043 validation: first (should-be-NPC) run set AwayCharacterIndex={trade.AwayCharacterIndex}, expected -1.");
            if (!char0Root.activeSelf || !char1Root.activeSelf)
                throw new Exception("MINI-043 validation: a playable character was deactivated on the first (NPC-courier) run.");
            if (switcher.IsLocked(0) || switcher.IsLocked(1))
                throw new Exception("MINI-043 validation: a playable character was locked on the first (NPC-courier) run.");

            InvokeMethod(trade, "CompleteTrip");
            if (!progression.GuadeloupeCharacterCourierUnlocked)
                throw new Exception("MINI-043 validation: completing the first NPC run did not unlock character dispatch.");
            if (trade.TripActive) throw new Exception("MINI-043 validation: trip still active after CompleteTrip.");

            // --- Second run: character dispatch should now be used. ---
            economy.AddCrop("test_tomato", 20);
            string msg2 = trade.Interact();
            if (!trade.TripActive) throw new Exception($"MINI-043 validation: second Interact() did not start a trip. Message: {msg2}");
            if (trade.AwayCharacterIndex != 1)
                throw new Exception($"MINI-043 validation: second (should-be-character) run set AwayCharacterIndex={trade.AwayCharacterIndex}, expected 1 (the inactive slot).");
            if (char1Root.activeSelf)
                throw new Exception("MINI-043 validation: the courier character was not deactivated on the second (character) run.");
            if (!switcher.IsLocked(1))
                throw new Exception("MINI-043 validation: the courier character was not locked on the second (character) run.");

            InvokeMethod(trade, "CompleteTrip");
            if (!char1Root.activeSelf) throw new Exception("MINI-043 validation: courier character not reactivated after CompleteTrip.");
            if (switcher.IsLocked(1)) throw new Exception("MINI-043 validation: courier character still locked after CompleteTrip.");

            Debug.Log("MINI-043 GUADELOUPE VALIDATION PASS: first run is NPC-only (no character touched), completing it unlocks character dispatch, second run correctly sends and locks a real character, both trips resolve cleanly.");

            UnityEngine.Object.DestroyImmediate(tradeGo);
            UnityEngine.Object.DestroyImmediate(switchGo);
            UnityEngine.Object.DestroyImmediate(char0Root);
            UnityEngine.Object.DestroyImmediate(char1Root);
            UnityEngine.Object.DestroyImmediate(progGo);
            UnityEngine.Object.DestroyImmediate(economyGo);
            UnityEngine.Object.DestroyImmediate(crop);
        }

        private static void InvokeMethod(MonoBehaviour behaviour, string name)
        {
            behaviour.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(behaviour, null);
        }
    }
}
