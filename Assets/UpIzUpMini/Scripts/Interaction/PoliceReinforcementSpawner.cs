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
    /// </summary>
    public class PoliceReinforcementSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject officerTemplate;
        [SerializeField] private int maxReinforcements = 2;
        [SerializeField] private float firstSpawnHeat = 55f;
        [SerializeField] private float secondSpawnHeat = 85f;
        [SerializeField] private float spawnDistance = 22f;

        private readonly List<GameObject> _pool = new List<GameObject>();
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
            }
        }

        private void Update()
        {
            if (_pool.Count == 0 || EconomyManager.Instance == null) return;

            float heat = EconomyManager.Instance.Heat;
            int wanted = 0;
            if (heat >= firstSpawnHeat) wanted = 1;
            if (heat >= secondSpawnHeat) wanted = 2;
            wanted = Mathf.Min(wanted, _pool.Count);

            if (wanted == _activeCount) return;

            var player = CharacterSwitchManager.Instance?.Active?.root;

            for (int i = 0; i < _pool.Count; i++)
            {
                bool shouldBeActive = i < wanted;
                if (shouldBeActive == _pool[i].activeSelf) continue;

                if (shouldBeActive && player != null)
                {
                    // Appear behind the player, off to one side, so they
                    // arrive from the town rather than materialising in view.
                    float angle = (i * 140f) * Mathf.Deg2Rad;
                    Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spawnDistance;
                    _pool[i].transform.position = player.transform.position + offset;
                }

                _pool[i].SetActive(shouldBeActive);
            }

            _activeCount = wanted;
        }
    }
}
