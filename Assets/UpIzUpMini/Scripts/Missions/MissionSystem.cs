using System;
using System.Collections.Generic;
using UnityEngine;
using UpIzUpMini.Economy;

namespace UpIzUpMini.Missions
{
    public enum ObjectiveKind
    {
        TalkTo,        // reach and talk to a named NPC
        Switch,        // switch character with Tab
        PlantCrop,     // plant a given crop id
        WaterAny,      // water any planted plot
        HarvestCrop,   // harvest a given crop id (count)
        SellCrop,      // sell crops to the buyer
        BuySeeds,      // purchase seeds from the farm shop
        ReachArea,     // walk into a world position
    }

    [Serializable]
    public class MissionObjective
    {
        public ObjectiveKind kind;
        [TextArea(1, 2)] public string instruction;
        public string targetId;       // npc name / crop id / shop item id
        public int requiredCount = 1;
        public Vector3 markerPosition;
        public bool hasMarker = true;

        [NonSerialized] public int progress;
        public bool IsComplete => progress >= Mathf.Max(1, requiredCount);
    }

    [Serializable]
    public class Mission
    {
        public string missionId;
        public string title;
        [TextArea(1, 3)] public string briefing;
        public int rewardMoney;
        public List<MissionObjective> objectives = new List<MissionObjective>();
    }

    /// <summary>
    /// GTA-style mission runner: one active objective at a time, each with
    /// a short instruction and a world marker, advancing as the player
    /// performs the action. Gameplay systems report events in
    /// (Notify/NotifyCount) rather than the mission system polling them,
    /// so missions stay decoupled from farming/economy internals.
    /// </summary>
    public class MissionSystem : MonoBehaviour
    {
        public static MissionSystem Instance { get; private set; }

        [SerializeField] private List<Mission> missions = new List<Mission>();

        public Mission Current => _missionIndex < missions.Count ? missions[_missionIndex] : null;
        public MissionObjective CurrentObjective
        {
            get
            {
                var m = Current;
                if (m == null) return null;
                return _objectiveIndex < m.objectives.Count ? m.objectives[_objectiveIndex] : null;
            }
        }

        public bool AllComplete => _missionIndex >= missions.Count;
        public string Banner { get; private set; }
        public float BannerTime { get; private set; }

        private int _missionIndex;
        private int _objectiveIndex;

        public event Action<Mission> OnMissionComplete;

        private void Awake() => Instance = this;

        private void Start()
        {
            if (Current != null) ShowBanner($"{Current.title}\n{Current.briefing}");
        }

        /// <summary>Report a one-shot gameplay event, e.g. talking to an NPC.</summary>
        public void Notify(ObjectiveKind kind, string targetId = null)
        {
            NotifyCount(kind, targetId, 1);
        }

        /// <summary>Report a gameplay event with an amount, e.g. harvesting 3 tomatoes.</summary>
        public void NotifyCount(ObjectiveKind kind, string targetId, int amount)
        {
            var obj = CurrentObjective;
            if (obj == null || obj.kind != kind) return;

            // Empty targetId on the objective means "any of this kind".
            if (!string.IsNullOrEmpty(obj.targetId)
                && !string.Equals(obj.targetId, targetId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            obj.progress += Mathf.Max(1, amount);
            if (!obj.IsComplete)
            {
                ShowBanner($"{obj.instruction}  ({obj.progress}/{obj.requiredCount})");
                return;
            }

            AdvanceObjective();
        }

        private void AdvanceObjective()
        {
            var mission = Current;
            if (mission == null) return;

            _objectiveIndex++;
            if (_objectiveIndex < mission.objectives.Count)
            {
                ShowBanner(CurrentObjective.instruction);
                return;
            }

            if (mission.rewardMoney > 0 && EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddMoney(mission.rewardMoney);
            }

            ShowBanner($"MISSION COMPLETE\n{mission.title}" +
                       (mission.rewardMoney > 0 ? $"\n+${mission.rewardMoney}" : string.Empty));
            OnMissionComplete?.Invoke(mission);

            _missionIndex++;
            _objectiveIndex = 0;

            if (Current != null)
            {
                // Small delay isn't modelled; the next briefing simply
                // replaces the completion banner on the next event.
                _pendingBriefing = $"{Current.title}\n{Current.briefing}";
                _pendingBriefingAt = Time.time + 3.5f;
            }
        }

        private string _pendingBriefing;
        private float _pendingBriefingAt;

        private void Update()
        {
            if (_pendingBriefing != null && Time.time >= _pendingBriefingAt)
            {
                ShowBanner(_pendingBriefing);
                _pendingBriefing = null;
            }

            // Position objectives complete by proximity rather than an event.
            var obj = CurrentObjective;
            if (obj != null && obj.kind == ObjectiveKind.ReachArea)
            {
                var player = Character.CharacterSwitchManager.Instance?.Active?.root;
                if (player != null &&
                    Vector3.Distance(player.transform.position, obj.markerPosition) < 5f)
                {
                    obj.progress = obj.requiredCount;
                    AdvanceObjective();
                }
            }
        }

        private void ShowBanner(string text)
        {
            Banner = text;
            BannerTime = Time.time;
        }

        public void SetMissions(List<Mission> newMissions)
        {
            missions = newMissions;
            _missionIndex = 0;
            _objectiveIndex = 0;
        }
    }
}
