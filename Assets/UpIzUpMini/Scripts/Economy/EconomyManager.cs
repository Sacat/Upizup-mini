using System;
using System.Collections.Generic;
using UnityEngine;

namespace UpIzUpMini.Economy
{
    /// <summary>
    /// Shared (not per-character) money, inventory, and heat - matches
    /// Docs/STORY.md: "They share money, inventory, heat, land, vehicles,
    /// and mission progress." One instance per scene.
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        [SerializeField] private int startingMoney = 20;
        [SerializeField] private float heatDecayPerSecond = 0.6f;

        private readonly Dictionary<string, int> _inventory = new Dictionary<string, int>();

        public int Money { get; private set; }
        public float Heat { get; private set; }
        public const float MaxHeat = 100f;

        public event Action OnChanged;

        private void Awake()
        {
            Instance = this;
            Money = startingMoney;
        }

        private void Update()
        {
            if (Heat > 0f)
            {
                Heat = Mathf.Max(0f, Heat - heatDecayPerSecond * Time.deltaTime);
            }
        }

        public int GetCount(string cropId) => _inventory.TryGetValue(cropId, out int c) ? c : 0;

        public void AddCrop(string cropId, int amount)
        {
            _inventory.TryGetValue(cropId, out int current);
            _inventory[cropId] = current + amount;
            OnChanged?.Invoke();
        }

        public bool TrySellAll(IReadOnlyList<CropDefinition> knownCrops, out int totalEarned)
        {
            totalEarned = 0;
            bool soldAnything = false;

            foreach (var crop in knownCrops)
            {
                int count = GetCount(crop.cropId);
                if (count <= 0) continue;

                totalEarned += count * crop.sellPrice;
                _inventory[crop.cropId] = 0;
                soldAnything = true;

                if (crop.isIllegal)
                {
                    AddHeat(12f);
                }
            }

            if (soldAnything)
            {
                Money += totalEarned;
                OnChanged?.Invoke();
            }

            return soldAnything;
        }

        public void AddHeat(float amount)
        {
            Heat = Mathf.Clamp(Heat + amount, 0f, MaxHeat);
            OnChanged?.Invoke();
        }
    }
}
