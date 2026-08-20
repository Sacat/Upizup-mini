using System;
using System.Collections.Generic;
using UnityEngine;
using UpIzUpMini.Economy;
using UpIzUpMini.Progression;

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
        BuyItem,       // purchase a specific shop item (e.g. land)
        EscapeHeat,    // let police heat cool back below a threshold
        AssignFarmhand,// leave the inactive protagonist tending a crop
        ChoosePath,    // L legitimate farming / K risky weed route
        FollowNpc,     // MINI-081: stay near a named, moving NPC (walking or driving) for a stretch of time
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

        // MINI-081: FollowNpc only. requiredCount doubles as the number of
        // seconds of sustained closeness needed; followRange is how close
        // counts as "with them." targetId is the NPC GameObject's name,
        // looked up (and cached) the same way ReachArea already keys off a
        // world position rather than a live reference.
        [SerializeField] private float followRange = 6f;
        public float FollowRange => followRange > 0f ? followRange : 6f;
        [NonSerialized] public float followTimer;
        [NonSerialized] public GameObject followTargetCache;

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
        public CareerPath requiredPath = CareerPath.Undecided;
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

        // MINI-054: lets the opening conversation (OpeningConversationController)
        // play out on the same banner before M1's own briefing appears,
        // without any Awake/Start execution-order dependency between the
        // two components - Mini011PhaseBSetup just sets this to the
        // conversation's own total duration at scene-build time.
        [SerializeField] private float firstBriefingDelay = 0f;

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
        public string CurrentMissionId => Current != null ? Current.missionId : string.Empty;
        public string Banner { get; private set; }
        public float BannerTime { get; private set; }

        private int _missionIndex;
        private int _objectiveIndex;

        public event Action<Mission> OnMissionComplete;

        private void Awake() => Instance = this;

        private void Start()
        {
            if (Current == null) return;

            string banner = $"{Current.title}\n{Current.briefing}";
            if (firstBriefingDelay > 0f)
            {
                // Reuses the same deferred-banner mechanism Update() already
                // drives for post-completion briefings (_pendingBriefing/
                // _pendingBriefingAt) - not a second timer implementation.
                _pendingBriefing = banner;
                _pendingBriefingAt = Time.time + firstBriefingDelay;
            }
            else
            {
                ShowBanner(banner);
            }
            PrepareCurrentObjective();
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
                PrepareCurrentObjective();
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
            SkipUnavailableMissions();

            if (Current != null)
            {
                PrepareCurrentObjective();
                // Small delay isn't modelled; the next briefing simply
                // replaces the completion banner on the next event.
                _pendingBriefing = $"{Current.title}\n{Current.briefing}";
                _pendingBriefingAt = Time.time + 3.5f;
            }
        }

        private void SkipUnavailableMissions()
        {
            var path = ProgressionManager.Instance != null ? ProgressionManager.Instance.Path : CareerPath.Undecided;
            while (_missionIndex < missions.Count && missions[_missionIndex].requiredPath != CareerPath.Undecided
                   && missions[_missionIndex].requiredPath != path) _missionIndex++;
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
            if (obj == null) return;

            if (obj.kind == ObjectiveKind.ReachArea)
            {
                var player = Character.CharacterSwitchManager.Instance?.Active?.root;
                if (player != null &&
                    Vector3.Distance(player.transform.position, obj.markerPosition) < 5f)
                {
                    obj.progress = obj.requiredCount;
                    AdvanceObjective();
                }
            }
            else if (obj.kind == ObjectiveKind.FollowNpc)
            {
                // MINI-081, user: "even having to follow npcs walking or
                // driving etc." Forgiving by design - straying out of
                // range pauses the timer rather than resetting it, since a
                // hard "lose the escort and fail" was not asked for.
                var player = Character.CharacterSwitchManager.Instance?.Active?.root;
                if (obj.followTargetCache == null && !string.IsNullOrEmpty(obj.targetId))
                {
                    obj.followTargetCache = GameObject.Find(obj.targetId);
                }
                if (player != null && obj.followTargetCache != null)
                {
                    float d = Vector3.Distance(player.transform.position, obj.followTargetCache.transform.position);
                    if (d <= obj.FollowRange) obj.followTimer += Time.deltaTime;

                    if (obj.followTimer >= Mathf.Max(1, obj.requiredCount))
                    {
                        obj.progress = obj.requiredCount;
                        AdvanceObjective();
                    }
                }
            }
            else if (obj.kind == ObjectiveKind.EscapeHeat)
            {
                float heat = EconomyManager.Instance != null ? EconomyManager.Instance.Heat : 0f;
                var player = Character.CharacterSwitchManager.Instance?.Active?.root;
                bool reachedLayLowArea = !obj.hasMarker
                    || player == null
                    || Vector3.Distance(player.transform.position, obj.markerPosition) <= 22f;

                // If the player already reached the safe area with zero heat,
                // the objective is complete. The previous high-heat latch made
                // this objective impossible after heat had legitimately cooled
                // before the objective became active.
                if (heat <= 8f && reachedLayLowArea)
                {
                    obj.progress = obj.requiredCount;
                    AdvanceObjective();
                }
            }
        }

        private void PrepareCurrentObjective()
        {
            var obj = CurrentObjective;
            if (obj == null) return;
            obj.followTimer = 0f;
            obj.followTargetCache = null;
        }

        public bool HasReachedMission(string missionId)
        {
            if (string.IsNullOrEmpty(missionId)) return true;
            for (int i = 0; i < missions.Count; i++)
            {
                if (!string.Equals(missions[i].missionId, missionId, StringComparison.OrdinalIgnoreCase)) continue;
                return _missionIndex >= i;
            }
            return false;
        }

        public bool HasCompletedMission(string missionId)
        {
            if (string.IsNullOrEmpty(missionId)) return true;
            for (int i = 0; i < missions.Count; i++)
            {
                if (!string.Equals(missions[i].missionId, missionId, StringComparison.OrdinalIgnoreCase)) continue;
                return _missionIndex > i;
            }
            return false;
        }

        public bool IsCurrentObjective(ObjectiveKind kind, string targetId = null)
        {
            var objective = CurrentObjective;
            if (objective == null || objective.kind != kind) return false;
            return string.IsNullOrEmpty(targetId) || string.IsNullOrEmpty(objective.targetId)
                   || string.Equals(objective.targetId, targetId, StringComparison.OrdinalIgnoreCase);
        }

        public void FailCurrentMission(string reason)
        {
            var mission = Current;
            if (mission == null) return;
            foreach (var objective in mission.objectives) objective.progress = 0;
            _objectiveIndex = 0;
            PrepareCurrentObjective();
            ShowBanner($"MISSION FAILED\n{reason}\nReturn to the safehouse.");
        }

        private void ShowBanner(string text)
        {
            Banner = text;
            BannerTime = Time.time;
        }

        public void Alert(string text) => ShowBanner(text);

        public void SetMissions(List<Mission> newMissions)
        {
            missions = newMissions;
            _missionIndex = 0;
            _objectiveIndex = 0;
            PrepareCurrentObjective();
        }

        // --- Save/load ---------------------------------------------------

        public int SaveMissionIndex => _missionIndex;
        public int SaveObjectiveIndex => _objectiveIndex;

        public List<int> CaptureObjectiveProgress()
        {
            var progress = new List<int>();
            var m = Current;
            if (m == null) return progress;
            foreach (var o in m.objectives) progress.Add(o.progress);
            return progress;
        }

        public void LoadState(int missionIndex, int objectiveIndex, IReadOnlyList<int> progress)
        {
            _missionIndex = Mathf.Clamp(missionIndex, 0, missions.Count);
            _objectiveIndex = Mathf.Max(0, objectiveIndex);

            var m = Current;
            if (m != null)
            {
                _objectiveIndex = Mathf.Clamp(_objectiveIndex, 0, Mathf.Max(0, m.objectives.Count - 1));
                if (progress != null)
                {
                    for (int i = 0; i < m.objectives.Count && i < progress.Count; i++)
                    {
                        m.objectives[i].progress = progress[i];
                    }
                }
                ShowBanner(CurrentObjective != null ? CurrentObjective.instruction : m.title);
                PrepareCurrentObjective();
            }
        }
    }
}
