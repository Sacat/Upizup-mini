using System;
using System.Collections.Generic;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;
using UpIzUpMini.Farming;

namespace UpIzUpMini
{
    [Serializable]
    public class PlotSave
    {
        public int state;
        public string cropId;
        public float timer;
    }

    [Serializable]
    public class GameSave
    {
        public int money;
        public float heat;
        public List<string> inventoryIds = new List<string>();
        public List<int> inventoryCounts = new List<int>();
        public List<PlotSave> plots = new List<PlotSave>();
        public Vector3 smartPosition;
        public Vector3 strongPosition;
        public int activeCharacter;

        public List<string> seedIds = new List<string>();
        public List<int> seedCounts = new List<int>();
        public List<string> ownedItemIds = new List<string>();

        public int missionIndex;
        public int objectiveIndex;
        public List<int> objectiveProgress = new List<int>();
    }

    /// <summary>
    /// Save/load to PlayerPrefs as JSON. PlayerPrefs (rather than a file)
    /// keeps this working identically on Windows, Android, and WebGL -
    /// relevant because this project targets mobile and possibly browser
    /// (see DECISIONS.md D-008), and WebGL has no ordinary filesystem.
    ///
    /// F5 saves, F9 loads.
    /// </summary>
    public class SaveLoadSystem : MonoBehaviour
    {
        private const string SaveKey = "UpIzUpMini.Save.v1";

        [SerializeField] private CropDefinition[] knownCrops;

        public static SaveLoadSystem Instance { get; private set; }
        public string LastMessage { get; private set; }
        public float LastMessageTime { get; private set; }

        private void Awake() => Instance = this;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5)) Save();
            if (Input.GetKeyDown(KeyCode.F9)) Load();
        }

        public void Save()
        {
            var save = new GameSave();

            if (EconomyManager.Instance != null)
            {
                save.money = EconomyManager.Instance.Money;
                save.heat = EconomyManager.Instance.Heat;
                foreach (var crop in knownCrops)
                {
                    if (crop == null) continue;
                    save.inventoryIds.Add(crop.cropId);
                    save.inventoryCounts.Add(EconomyManager.Instance.GetCount(crop.cropId));
                }

                EconomyManager.Instance.CaptureExtras(save.seedIds, save.seedCounts, save.ownedItemIds);
            }

            var missions = Missions.MissionSystem.Instance;
            if (missions != null)
            {
                save.missionIndex = missions.SaveMissionIndex;
                save.objectiveIndex = missions.SaveObjectiveIndex;
                save.objectiveProgress = missions.CaptureObjectiveProgress();
            }

            foreach (var plot in FindPlotsOrdered())
            {
                save.plots.Add(new PlotSave
                {
                    state = plot.GetSaveState(),
                    cropId = plot.GetSaveCropId(),
                    timer = plot.GetSaveTimer()
                });
            }

            var switcher = CharacterSwitchManager.Instance;
            if (switcher != null)
            {
                save.activeCharacter = switcher.ActiveIndex;
                var slots = switcher.Slots;
                if (slots != null && slots.Length > 1)
                {
                    if (slots[0]?.root != null) save.smartPosition = slots[0].root.transform.position;
                    if (slots[1]?.root != null) save.strongPosition = slots[1].root.transform.position;
                }
            }

            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(save));
            PlayerPrefs.Save();
            Notify("Game saved.");
        }

        public void Load()
        {
            if (!PlayerPrefs.HasKey(SaveKey))
            {
                Notify("No save found.");
                return;
            }

            var save = JsonUtility.FromJson<GameSave>(PlayerPrefs.GetString(SaveKey));
            if (save == null)
            {
                Notify("Save was unreadable.");
                return;
            }

            EconomyManager.Instance?.LoadState(
                save.money, save.heat, save.inventoryIds, save.inventoryCounts,
                save.seedIds, save.seedCounts, save.ownedItemIds);

            Missions.MissionSystem.Instance?.LoadState(
                save.missionIndex, save.objectiveIndex, save.objectiveProgress);

            var plots = FindPlotsOrdered();
            for (int i = 0; i < plots.Count && i < save.plots.Count; i++)
            {
                var ps = save.plots[i];
                plots[i].LoadState(ps.state, FindCrop(ps.cropId), ps.timer);
            }

            var switcher = CharacterSwitchManager.Instance;
            if (switcher != null)
            {
                var slots = switcher.Slots;
                if (slots != null && slots.Length > 1)
                {
                    TeleportSlot(slots[0], save.smartPosition);
                    TeleportSlot(slots[1], save.strongPosition);
                }
                switcher.SwitchTo(save.activeCharacter);
            }

            Notify("Game loaded.");
        }

        private static void TeleportSlot(CharacterSlot slot, Vector3 pos)
        {
            if (slot?.root == null || pos == Vector3.zero) return;
            // CharacterController overrides direct transform writes, so
            // disable it for the teleport frame.
            var cc = slot.root.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            slot.root.transform.position = pos;
            if (cc != null) cc.enabled = true;
        }

        private CropDefinition FindCrop(string id)
        {
            if (string.IsNullOrEmpty(id) || knownCrops == null) return null;
            foreach (var c in knownCrops)
            {
                if (c != null && c.cropId == id) return c;
            }
            return null;
        }

        private static List<FarmPlot> FindPlotsOrdered()
        {
            var plots = new List<FarmPlot>(
                UnityEngine.Object.FindObjectsByType<FarmPlot>(FindObjectsSortMode.None));
            // Stable order so save slots line up with the same plots on load.
            plots.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return plots;
        }

        private void Notify(string message)
        {
            LastMessage = message;
            LastMessageTime = Time.unscaledTime;
            Debug.Log($"SaveLoad: {message}");
        }
    }
}
