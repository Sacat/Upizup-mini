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

        // MINI-062: the player's chosen "wake up here" house, if any -
        // Vector3.zero means "never picked one", which
        // CharacterSwitchManager.LoadRespawnPoint treats as no selection
        // rather than a real point at the world origin.
        public Vector3 respawnPosition;
        public string respawnLabel;

        public List<string> seedIds = new List<string>();
        public List<int> seedCounts = new List<int>();
        public List<string> ownedItemIds = new List<string>();
        public List<string> sacatOwnedItemIds = new List<string>();
        public List<string> frankiOwnedItemIds = new List<string>();
        public List<string> sacatUnequippedItems = new List<string>();
        public List<string> frankiUnequippedItems = new List<string>();
        public List<OutfitChoice> sacatOutfit;
        public List<OutfitChoice> frankiOutfit;

        // MINI-073: stashed food/pharmacy items - see EconomyManager's
        // CaptureConsumables/LoadConsumables.
        public List<string> consumableIds = new List<string>();
        public List<int> consumableCounts = new List<int>();
        public List<string> ammoIds = new List<string>();
        public List<int> ammoCounts = new List<int>();
        public int sidearmMagazine;

        // MINI-066: where the bike was left. Vector3.zero means "no bike in the
        // world yet", which Load treats as "leave it wherever the scene put
        // it" rather than teleporting it to the world origin - the same
        // never-set convention respawnPosition above already uses.
        public Vector3 bikePosition;
        public Vector3 bikeEuler;

        public int missionIndex;
        public string missionId;
        public int missionSchemaVersion;
        public string resumeAfterToolMissionId;
        public int resumeAfterToolObjectiveIndex;
        public List<int> resumeAfterToolProgress = new List<int>();
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

            // MINI-066: remember where the bike was left, so it is still there
            // after a save/load instead of snapping back to wherever the scene
            // originally placed it. Found by name rather than a serialized
            // reference because the bike may be a scene-placed one OR spawned
            // at runtime by VehicleSpawnController after purchase.
            var bike = UnityEngine.Object.FindFirstObjectByType<Vehicles.TmaxBikeController>();
            if (bike != null)
            {
                save.bikePosition = bike.transform.position;
                save.bikeEuler = bike.transform.eulerAngles;
            }

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
                EconomyManager.Instance.CaptureCharacterOwned(0, save.sacatOwnedItemIds);
                EconomyManager.Instance.CaptureCharacterOwned(1, save.frankiOwnedItemIds);
                EconomyManager.Instance.CaptureConsumables(save.consumableIds, save.consumableCounts);
                EconomyManager.Instance.CaptureAmmo(save.ammoIds, save.ammoCounts);
            }
            if (EconomyManager.Instance != null) save.sidearmMagazine = EconomyManager.Instance.SidearmMagazine;

            var missions = Missions.MissionSystem.Instance;
            if (missions != null)
            {
                save.missionIndex = missions.SaveMissionIndex;
                save.missionId = missions.CurrentMissionId;
                save.missionSchemaVersion = 2;
                save.resumeAfterToolMissionId = missions.ResumeAfterToolMissionId;
                save.resumeAfterToolObjectiveIndex = missions.ResumeAfterToolObjectiveIndex;
                save.resumeAfterToolProgress = missions.CaptureResumeAfterToolProgress();
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
                    save.sacatUnequippedItems=slots[0]?.root?.GetComponent<CharacterEquipment>()?.CaptureWardrobe();
                    save.frankiUnequippedItems=slots[1]?.root?.GetComponent<CharacterEquipment>()?.CaptureWardrobe();
                    save.sacatOutfit=slots[0]?.root?.GetComponent<OutfitWardrobe>()?.Capture();
                    save.frankiOutfit=slots[1]?.root?.GetComponent<OutfitWardrobe>()?.Capture();
                }
                save.respawnPosition = switcher.CurrentRespawnPoint;
                save.respawnLabel = switcher.RespawnLabel;
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
                save.seedIds, save.seedCounts, save.ownedItemIds,
                save.sacatOwnedItemIds, save.frankiOwnedItemIds, save.activeCharacter);
            EconomyManager.Instance?.LoadConsumables(save.consumableIds, save.consumableCounts);
            EconomyManager.Instance?.LoadAmmo(save.ammoIds, save.ammoCounts);
            EconomyManager.Instance?.LoadSidearmMagazine(save.sidearmMagazine);

            // MINI-068: the bike is returned to its HOME spot outside the farm
            // safehouse on every load, not to wherever it was left.
            //
            // This deliberately reverses the save-the-last-position behaviour
            // added a few tasks ago, at the user's explicit request and for
            // their stated reason: "once i have bought the bike it should be in
            // my safe house exact where you have it even every respawn. so if i
            // forget it anywhere i will see it there." A vehicle you can strand
            // on the far side of the map with no way back is a soft-lock; a
            // fixed garage spot is the usual fix.
            //
            // bikePosition/bikeEuler are still WRITTEN to the save, so the old
            // behaviour can be restored without a save-format change if the
            // user ever wants "park it where you leave it" back.
            Vehicles.VehicleSpawnController.ReturnBikeHome();

            var missionSystem = Missions.MissionSystem.Instance;
            if (missionSystem != null)
            {
                missionSystem.ClearResumeAfterTool();
                int resolvedMissionIndex = missionSystem.ResolveSavedMissionIndex(
                    save.missionId, save.missionIndex, save.missionSchemaVersion);
                int toolMissionIndex = missionSystem.ResolveSavedMissionIndex("M12T", 0, 2);
                bool needsLegacyToolMission = save.missionSchemaVersion < 2
                    && resolvedMissionIndex > toolMissionIndex
                    && EconomyManager.Instance != null
                    && !EconomyManager.Instance.OwnsItem(Combat.FirearmController.SidearmId);
                if (needsLegacyToolMission)
                {
                    string resumeId = missionSystem.MissionIdAt(resolvedMissionIndex);
                    missionSystem.LoadState(toolMissionIndex, 0, null);
                    missionSystem.SetResumeAfterTool(resumeId, save.objectiveIndex, save.objectiveProgress);
                }
                else
                {
                    missionSystem.LoadState(resolvedMissionIndex, save.objectiveIndex, save.objectiveProgress);
                    if (!string.IsNullOrEmpty(save.resumeAfterToolMissionId))
                        missionSystem.SetResumeAfterTool(save.resumeAfterToolMissionId,
                            save.resumeAfterToolObjectiveIndex, save.resumeAfterToolProgress);
                }
            }

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
                    slots[0]?.root?.GetComponent<CharacterEquipment>()?.RestoreWardrobe(save.sacatUnequippedItems);
                    slots[1]?.root?.GetComponent<CharacterEquipment>()?.RestoreWardrobe(save.frankiUnequippedItems);
                    slots[0]?.root?.GetComponent<OutfitWardrobe>()?.Restore(save.sacatOutfit);
                    slots[1]?.root?.GetComponent<OutfitWardrobe>()?.Restore(save.frankiOutfit);
                }
                switcher.SwitchTo(save.activeCharacter);
                switcher.LoadRespawnPoint(save.respawnPosition, save.respawnLabel);
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
