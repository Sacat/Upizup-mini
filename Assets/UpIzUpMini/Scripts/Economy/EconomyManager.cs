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

        private readonly Dictionary<string, int> _seeds = new Dictionary<string, int>();
        private readonly HashSet<string> _owned = new HashSet<string>();

        public int GetCount(string cropId) => _inventory.TryGetValue(cropId, out int c) ? c : 0;

        // --- Seeds -------------------------------------------------------
        // Planting consumes a seed; harvesting returns more than one so the
        // player can keep replanting (the "clone plants / get extra seeds"
        // behaviour from the larger game).

        public int GetSeeds(string cropId) => _seeds.TryGetValue(cropId, out int c) ? c : 0;

        public void AddSeeds(string cropId, int amount)
        {
            _seeds.TryGetValue(cropId, out int current);
            _seeds[cropId] = Mathf.Max(0, current + amount);
            OnChanged?.Invoke();
        }

        public bool TryConsumeSeed(string cropId)
        {
            if (GetSeeds(cropId) <= 0) return false;
            _seeds[cropId] = GetSeeds(cropId) - 1;
            OnChanged?.Invoke();
            return true;
        }

        // --- Purchases ---------------------------------------------------

        public bool OwnsItem(string itemId) => _owned.Contains(itemId);

        public bool TryPurchase(ShopItemDefinition item, out string message)
        {
            if (item == null) { message = "Nothing to buy."; return false; }

            if (item.category != ShopCategory.Seed && _owned.Contains(item.itemId))
            {
                message = $"You already have {item.displayName}.";
                return false;
            }

            if (Money < item.price)
            {
                message = $"{item.displayName} cost ${item.price}. You short.";
                return false;
            }

            Money -= item.price;

            if (item.category == ShopCategory.Seed && item.grantsCrop != null)
            {
                AddSeeds(item.grantsCrop.cropId, item.seedQuantity);
                message = $"Bought {item.seedQuantity} {item.displayName} for ${item.price}.";
            }
            else
            {
                _owned.Add(item.itemId);
                message = $"Bought {item.displayName} for ${item.price}.";
            }

            OnChanged?.Invoke();
            return true;
        }

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

        /// <summary>Restore saved state - see SaveLoadSystem.</summary>
        public void LoadState(int money, float heat, IReadOnlyList<string> ids, IReadOnlyList<int> counts)
        {
            Money = money;
            Heat = Mathf.Clamp(heat, 0f, MaxHeat);

            _inventory.Clear();
            if (ids != null && counts != null)
            {
                for (int i = 0; i < ids.Count && i < counts.Count; i++)
                {
                    _inventory[ids[i]] = counts[i];
                }
            }

            OnChanged?.Invoke();
        }
    }
}
