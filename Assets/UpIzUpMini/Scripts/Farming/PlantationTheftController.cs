using System.Collections.Generic;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Missions;
using UpIzUpMini.Progression;

namespace UpIzUpMini.Farming
{
    /// <summary>
    /// MINI-059: at low GrandBayGangs reputation, an unattended illegal
    /// crop can be volehed (stolen) while the player has been away from
    /// the plantation - per the roadmap brief's rules:
    ///
    /// - risk begins after player has been away (a real "away" timer, not
    ///   a check that fires the instant they leave);
    /// - not guaranteed every time (a probability roll, not automatic);
    /// - reputation affects risk (higher GrandBayGangs standing lowers it,
    ///   but never to a guaranteed zero on its own);
    /// - assigned guard reduces/prevents risk (a Not Ah Word member on
    ///   GuardPlantation duty, from MINI-058, fully prevents it - the only
    ///   sure protection).
    ///
    /// Deliberately does NOT name or reveal Dog Life anywhere in this
    /// controller's own text - the roadmap's own next task (MINI-060) is
    /// the actual reveal; this only "creates suspicion" per its
    /// instruction, via a generic theft notification.
    /// </summary>
    public class PlantationTheftController : MonoBehaviour
    {
        [SerializeField] private Vector3 plantationCenter;
        [SerializeField] private FarmPlot[] plots;
        [SerializeField] private float awayRadius = 40f;
        [SerializeField] private float awaySecondsBeforeRisk = 20f;
        [SerializeField] private float rollIntervalSeconds = 30f;
        [SerializeField] private float baseTheftChance = 0.12f;

        private float _awayTimer;
        private float _rollTimer;

        public void Configure(Vector3 center, FarmPlot[] farmPlots)
        {
            plantationCenter = center;
            plots = farmPlots;
        }

        private void Update()
        {
            var player = CharacterSwitchManager.Instance?.Active?.root;
            if (player == null || plots == null || plots.Length == 0) return;

            float dist = Vector3.Distance(player.transform.position, plantationCenter);
            if (dist <= awayRadius)
            {
                // Being back home resets both timers - risk only ever
                // accrues while genuinely away, matching "begins after the
                // player has been away" rather than ticking constantly.
                _awayTimer = 0f;
                _rollTimer = 0f;
                return;
            }

            _awayTimer += Time.deltaTime;
            if (_awayTimer < awaySecondsBeforeRisk) return;

            _rollTimer += Time.deltaTime;
            if (_rollTimer < rollIntervalSeconds) return;
            _rollTimer = 0f;

            if (IsGuarded()) return;

            if (Random.value > ComputeChance()) return;

            TryStealOne();
        }

        private static bool IsGuarded()
        {
            foreach (var member in GangMemberController.All)
            {
                if (member != null && member.IsRecruited && member.Assignment == GangAssignment.GuardPlantation)
                {
                    return true;
                }
            }
            return false;
        }

        private float ComputeChance()
        {
            var prog = ProgressionManager.Instance;
            int reputation = prog != null ? prog.GetReputation(Faction.GrandBayGangs) : 0;
            // Higher street standing lowers risk, but a guard is the only
            // thing that can zero it out entirely - "not guaranteed every
            // time" cuts both ways, so reputation alone never fully removes
            // the risk either.
            float repFactor = Mathf.Clamp01(1f - reputation / 100f);
            return Mathf.Clamp(baseTheftChance * Mathf.Lerp(0.3f, 1.4f, repFactor), 0.02f, 0.35f);
        }

        private void TryStealOne()
        {
            var candidates = new List<FarmPlot>();
            foreach (var plot in plots)
            {
                if (plot != null && plot.HasStealableZeb) candidates.Add(plot);
            }
            if (candidates.Count == 0) return;

            var target = candidates[Random.Range(0, candidates.Count)];
            string cropName = target.Voleh();
            if (!string.IsNullOrEmpty(cropName))
            {
                // MINI-060: stays deliberately vague ("somebody") until
                // Gardey Zafeh's reveal - only then does the notification
                // itself name Dog Life, matching "only after the reveal
                // should Dog Life rivalry become openly active."
                bool revealed = ProgressionManager.Instance != null && ProgressionManager.Instance.DogLifeRevealed;
                string who = revealed ? "Dog Life" : "Somebody";
                MissionSystem.Instance?.Alert($"{who} volehed your {cropName} while allu was away, nuh!");
            }
        }
    }
}
