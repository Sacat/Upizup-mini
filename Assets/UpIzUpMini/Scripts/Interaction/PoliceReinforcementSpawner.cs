using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;

namespace UpIzUpMini.Interaction
{
    /// <summary>
    /// Spawns extra officers as police heat climbs, and despawns them once
    /// it falls again - the escalation described in Docs/STORY.md
    /// ("at 100 heat, one additional officer can spawn").
    ///
    /// Officers are pooled rather than created and destroyed repeatedly,
    /// per AGENTS.md's pooling guidance.
    ///
    /// MINI-041: the original version recomputed "wanted" purely from the
    /// *current* heat value every frame with no hysteresis, and toggled
    /// `SetActive` instantly both ways. PoliceHeatController cools heat at
    /// 9/sec the moment the player steps even slightly away from an
    /// officer, so a reinforcement that had just spawned at the 55/85
    /// thresholds would often pop back out of existence within a second or
    /// two of appearing - reported by the user as reinforcements
    /// "disappearing". Fixed with separate, lower despawn thresholds
    /// (proper hysteresis - heat has to drop further than it took to
    /// spawn, not just dip back under the same line) and a walk-off
    /// coroutine so leaving reads as the officer walking away rather than
    /// vanishing.
    /// </summary>
    public class PoliceReinforcementSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject officerTemplate;
        [SerializeField] private int maxReinforcements = 2;
        [SerializeField] private float firstSpawnHeat = 55f;
        [SerializeField] private float secondSpawnHeat = 85f;
        [SerializeField] private float firstDespawnHeat = 35f;
        [SerializeField] private float secondDespawnHeat = 65f;
        [SerializeField] private float spawnDistance = 22f;
        [SerializeField] private float walkOffSeconds = 2.5f;
        [SerializeField] private float walkOffSpeed = 4.3f;

        private readonly List<GameObject> _pool = new List<GameObject>();
        private readonly List<Coroutine> _leavingRoutines = new List<Coroutine>();
        private int _activeCount;

        private void Start()
        {
            if (officerTemplate == null) return;

            for (int i = 0; i < maxReinforcements; i++)
            {
                var clone = Instantiate(officerTemplate, transform);
                clone.name = $"PoliceReinforcement_{i}";
                clone.SetActive(false);
                _pool.Add(clone);
                _leavingRoutines.Add(null);
            }
        }

        private void Update()
        {
            if (_pool.Count == 0 || EconomyManager.Instance == null) return;

            float heat = EconomyManager.Instance.Heat;

            // Hysteresis: spawn thresholds only ever raise `wanted`, despawn
            // thresholds only ever lower it. A reinforcement that just
            // spawned at 55 needs heat back down to 35 to leave, not merely
            // back under 55 - a single-digit cooling tick can no longer
            // immediately undo a spawn.
            int wanted = _activeCount;
            if (heat >= secondSpawnHeat) wanted = Mathf.Max(wanted, 2);
            else if (heat >= firstSpawnHeat) wanted = Mathf.Max(wanted, 1);
            if (heat < firstDespawnHeat) wanted = 0;
            else if (heat < secondDespawnHeat) wanted = Mathf.Min(wanted, 1);
            wanted = Mathf.Clamp(wanted, 0, _pool.Count);

            if (wanted == _activeCount) return;

            var player = CharacterSwitchManager.Instance?.Active?.root;

            for (int i = 0; i < _pool.Count; i++)
            {
                bool shouldBeActive = i < wanted;
                // A walking-off officer is still activeSelf==true until its
                // coroutine finishes, so "already in the state we want"
                // must check the leaving flag too, not just activeSelf -
                // otherwise a heat spike part-way through a walk-off would
                // be mistaken for "already active, nothing to do" and the
                // officer would keep leaving anyway.
                bool isLeaving = _leavingRoutines[i] != null;

                if (shouldBeActive)
                {
                    if (_pool[i].activeSelf && !isLeaving) continue; // fully in already

                    // Re-entering before a pending walk-off finished -
                    // cancel it so the officer doesn't get deactivated out
                    // from under the player mid-return.
                    if (isLeaving)
                    {
                        StopCoroutine(_leavingRoutines[i]);
                        _leavingRoutines[i] = null;
                    }

                    if (player != null)
                    {
                        // Appear behind the player, off to one side, so they
                        // arrive from the town rather than materialising in view.
                        float angle = (i * 140f) * Mathf.Deg2Rad;
                        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spawnDistance;
                        _pool[i].transform.position = player.transform.position + offset;
                    }
                    var officerAi = _pool[i].GetComponent<PoliceOfficer>();
                    if (officerAi != null) officerAi.enabled = true;
                    _pool[i].SetActive(true);
                }
                else
                {
                    if (!_pool[i].activeSelf || isLeaving) continue; // already out or already leaving
                    _leavingRoutines[i] = StartCoroutine(WalkOffAndDeactivate(i, player));
                }
            }

            _activeCount = wanted;
        }

        /// <summary>
        /// Hands the officer off to a short scripted walk away from the
        /// player instead of an instant SetActive(false), then deactivates
        /// once clear. Disables PoliceOfficer for the duration so its own
        /// patrol/chase logic doesn't fight this movement.
        /// </summary>
        private IEnumerator WalkOffAndDeactivate(int index, GameObject player)
        {
            GameObject officer = _pool[index];
            var officerAi = officer.GetComponent<PoliceOfficer>();
            var controller = officer.GetComponent<CharacterController>();
            if (officerAi != null) officerAi.enabled = false;

            Vector3 awayDir = player != null
                ? (officer.transform.position - player.transform.position)
                : officer.transform.forward;
            awayDir.y = 0f;
            if (awayDir.sqrMagnitude < 0.01f) awayDir = officer.transform.forward;
            awayDir.Normalize();
            officer.transform.rotation = Quaternion.LookRotation(awayDir, Vector3.up);

            float elapsed = 0f;
            while (elapsed < walkOffSeconds)
            {
                if (controller != null && controller.enabled)
                {
                    controller.SimpleMove(awayDir * walkOffSpeed);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (officerAi != null) officerAi.enabled = true; // ready for reuse next spawn
            officer.SetActive(false);
            _leavingRoutines[index] = null;
        }
    }
}
