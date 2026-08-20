using System.Collections.Generic;
using UnityEngine;
using UpIzUpMini.Missions;

namespace UpIzUpMini.UI
{
    /// <summary>Small, contextual tutorial hints. Hints only describe a
    /// mechanic once it is relevant; locked crops, vehicles and services are
    /// never advertised early.</summary>
    public class GameplayHintController : MonoBehaviour
    {
        private readonly HashSet<string> _shown = new HashSet<string>();
        private string _pendingKey;
        private float _showAt;

        private void Update()
        {
            var missions = MissionSystem.Instance;
            var objective = missions != null ? missions.CurrentObjective : null;
            if (missions == null || objective == null) return;

            string key = $"{missions.CurrentMissionId}:{objective.kind}:{objective.targetId}";
            if (!_shown.Contains(key) && _pendingKey != key)
            {
                _pendingKey = key;
                _showAt = Time.time + 2.25f;
            }

            if (_pendingKey != key || Time.time < _showAt) return;

            string hint = HintFor(missions.CurrentMissionId, objective);
            _shown.Add(key);
            _pendingKey = null;
            if (!string.IsNullOrEmpty(hint)) missions.Alert($"HINT\n{hint}");
        }

        private static string HintFor(string missionId, MissionObjective objective)
        {
            if (missionId == "M1" && objective.kind == ObjectiveKind.HarvestCrop)
                return "You can clone a ripe plant to get more seeds using [ R ]. Harvest with [ E ].";
            if (missionId == "M3" && objective.kind == ObjectiveKind.BuyItem)
                return "The Land and Surveys man is standing in front of the LAND AND SURVEYS building, across from the road.";
            if (missionId == "M4" && objective.kind == ObjectiveKind.PlantCrop)
                return "Carrying weed or weed seeds near police raises heat. Plant it away from the road, then sell the harvest to get it out of your inventory.";
            if (missionId == "M5" && objective.kind == ObjectiveKind.TalkTo)
                return "Police notice weed and weed seeds. Plant or sell them before walking close to an officer.";
            if (missionId == "M15")
                return "Recruitment needs at least 20% street reputation. Your Street Rep meter is under the health bars.";
            if (missionId == "M17")
                return "The TNAX is now available because this mission needs longer travel. Earlier locked vehicles stayed hidden.";
            return null;
        }
    }
}
