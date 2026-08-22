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
        private bool _markerSystemTaught;

        private void Update()
        {
            var missions = MissionSystem.Instance;
            var objective = missions != null ? missions.CurrentObjective : null;
            if (missions == null || objective == null) return;

            // Teach the universal navigation language once, separately from
            // the objective-specific hint. Every mission marker uses this
            // same blinking/clamped yellow blip, not only Highland missions.
            if (objective.hasMarker && !_markerSystemTaught)
            {
                const string markerKey = "mission-marker-system";
                if (_pendingKey != markerKey)
                {
                    _pendingKey = markerKey;
                    _showAt = Time.time + 1.2f;
                }
                if (Time.time >= _showAt)
                {
                    _markerSystemTaught = true;
                    _shown.Add(markerKey);
                    _pendingKey = null;
                    missions.Alert("HINT\nYELLOW means your current mission. The blinking yellow dot stays on the minimap edge when the destination is far away; follow it until the world marker appears.");
                }
                return;
            }

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
            if (objective.kind == ObjectiveKind.RestAtSafehouse)
                return "Use the Highland safehouse bed and choose [1] Rest to heal and cool down.";
            if (objective.kind == ObjectiveKind.TalkToCleanPolice)
                return "Carry no weed and no weed seeds, then speak to two regular officers to build a clean face.";
            if (objective.kind == ObjectiveKind.BribeNormy)
                return "Normy is crooked, not a normal officer. Give him $100 to remove 20% heat; you can return after his cooldown.";
            // MINI-117, user: "using what buttons are the mixed strains
            // planted. give hint n instructions to the process" - the
            // breeding station's own prompt only says "[E] Interbreed
            // strains"; it never explains the crop-select number key or
            // that the player needs to hold both parent crops first.
            //
            // MINI-119, user: "give a hint to every crop number as we go
            // along especially with the strains" - extended down to every
            // Rasta-taught base strain too (M13C2-M13C4), not just the
            // three hybrids. Key numbers below match CropSpecs' build
            // order in Mini011PhaseBSetup.cs: 5=Black Sugar, 6=Purple,
            // 7=Blue Cheese, 8=Purple Sugar (purple_black), 9=Sugar
            // Cheese, 0=Purple Cheese. M13C5's key was also corrected here
            // (was 7, but MINI-119 swapped Blue Cheese and Purple Sugar's
            // number-key order - Purple Sugar is now 8, not 7).
            if (missionId == "M13C2" && objective.kind == ObjectiveKind.HarvestCrop)
                return "Press [ 5 ] to select Black Sugar seed, then [ E ] on an empty plot to plant it, water it, and harvest when ripe.";
            if (missionId == "M13C3" && objective.kind == ObjectiveKind.HarvestCrop)
                return "Press [ 6 ] to select Purple seed, then [ E ] on an empty plot to plant it, water it, and harvest when ripe.";
            if (missionId == "M13C4" && objective.kind == ObjectiveKind.HarvestCrop)
                return "Press [ 7 ] to select Blue Cheese seed, then [ E ] on an empty plot to plant it, water it, and harvest when ripe.";
            if (missionId == "M13C5" && objective.kind == ObjectiveKind.HarvestCrop)
                return "Hold both Purple and Black Sugar, then press [ E ] at the breeding station to cross them into Purple Sugar seed. Press [ 8 ] to select Purple Sugar, then [ E ] on an empty plot to plant, water and harvest it like any other crop.";
            if (missionId == "M13C6" && objective.kind == ObjectiveKind.HarvestCrop)
                return "Hold both Blue Cheese and Black Sugar, then press [ E ] at the breeding station to cross them into Sugar Cheese seed. Press [ 9 ] to select Sugar Cheese, then plant/water/harvest as usual.";
            if (missionId == "M13C7" && objective.kind == ObjectiveKind.HarvestCrop)
                return "Hold both Blue Cheese and Purple, then press [ E ] at the breeding station to cross them into Purple Cheese seed. Press [ 0 ] to select Purple Cheese, then plant/water/harvest as usual.";
            if (missionId == "M15")
                return "Recruitment needs at least 20% street reputation. Your Street Rep meter is under the health bars.";
            if (missionId == "M17")
                return "The TNAX is now available because this mission needs longer travel. Earlier locked vehicles stayed hidden.";
            return null;
        }
    }
}
