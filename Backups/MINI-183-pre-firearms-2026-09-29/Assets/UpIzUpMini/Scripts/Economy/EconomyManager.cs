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

        private readonly Dictionary<string, int> _inventory = new Dictionary<string, int>();

        // MINI-073: "make the pharmacy items be able to store in your
        // inventory and able to use after even food as well." Food/pill
        // purchases used to be consumed the instant you bought them - now
        // they stash here and are used on demand later (InventoryPanelController,
        // opened with I). _consumableCatalog is a lookup back to the full
        // ShopItemDefinition (heal amount, boost numbers, display name) since
        // the stash itself only remembers id->count, the same way _seeds only
        // remembers cropId->count and relies on CropDefinition lookups elsewhere.
        private readonly Dictionary<string, int> _consumables = new Dictionary<string, int>();
        private readonly Dictionary<string, ShopItemDefinition> _consumableCatalog = new Dictionary<string, ShopItemDefinition>();

        public int Money { get; private set; }
        public float Heat { get; private set; }
        public const float MaxHeat = 100f;

        // MINI-060 follow-up-2 cheat code: "heat stays 0" - a hard lock
        // rather than a one-off AddHeat(-Heat), so nothing (police
        // proximity, a mission event) can push it back up afterward.
        public bool HeatLocked { get; private set; }

        public void LockHeatAtZero()
        {
            HeatLocked = true;
            Heat = 0f;
            OnChanged?.Invoke();
        }

        /// <summary>Reverses LockHeatAtZero() - used by the "000000" cheat's
        /// own toggle-off. Heat stays at 0 (where the lock left it) rather
        /// than jumping anywhere; normal AddHeat() calls resume affecting it.</summary>
        public void UnlockHeat()
        {
            HeatLocked = false;
            OnChanged?.Invoke();
        }

        public event Action OnChanged;

        private void Awake()
        {
            Instance = this;
            Money = startingMoney;
        }

        // Heat is driven entirely by PoliceHeatController (proximity to
        // officers), not by a flat timer - so it stays at 0 unless police
        // are actually nearby.

        private readonly Dictionary<string, int> _seeds = new Dictionary<string, int>();
        private readonly HashSet<string> _owned = new HashSet<string>();
        private readonly Dictionary<int, HashSet<string>> _characterOwned =
            new Dictionary<int, HashSet<string>>();

        public int GetCount(string cropId) => _inventory.TryGetValue(cropId, out int c) ? c : 0;

        // --- Seeds -------------------------------------------------------
        // Planting consumes a seed; harvesting returns more than one so the
        // player can keep replanting (the "clone plants / get extra seeds"
        // behaviour from the larger game).

        public int GetSeeds(string cropId) => _seeds.TryGetValue(cropId, out int c) ? c : 0;

        public bool HasIllegalGoods()
        {
            foreach (string id in new[] { "bushers", "black_sugar", "purple", "purple_black", "blue_cheese", "sugar_cheese", "purple_cheese" })
                if (GetCount(id) > 0 || GetSeeds(id) > 0) return true;
            return false;
        }

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

        public bool OwnsItem(string itemId, int characterIndex)
        {
            return _characterOwned.TryGetValue(characterIndex, out var owned)
                   && owned.Contains(itemId);
        }

        private static bool IsCharacterWearable(ShopItemDefinition item) =>
            item != null && (item.category == ShopCategory.Clothing
                             || item.category == ShopCategory.Footwear
                             || item.category == ShopCategory.Accessory);

        private static bool IsLegacyWearableId(string itemId) =>
            itemId == "cap_mike" || itemId == "shirt_lacos" || itemId == "shorts_adibas"
            || itemId == "shoes_mike" || itemId == "shoes_pumba" || itemId == "chain_gold"
            || itemId == "shades_ray" || itemId == "watch_rollie";

        private HashSet<string> CharacterSet(int characterIndex)
        {
            if (!_characterOwned.TryGetValue(characterIndex, out var owned))
            {
                owned = new HashSet<string>();
                _characterOwned[characterIndex] = owned;
            }
            return owned;
        }

        public bool TryResell(ShopItemDefinition item, out string message)
        {
            int owner = Character.CharacterSwitchManager.Instance != null
                ? Character.CharacterSwitchManager.Instance.ActiveIndex : 0;
            bool removed = item != null && (IsCharacterWearable(item)
                ? CharacterSet(owner).Remove(item.itemId)
                : _owned.Remove(item.itemId));
            if (!removed)
            {
                message = item == null ? "Nothing selected." : $"You doe own {item.displayName}.";
                return false;
            }
            int payout = Mathf.Max(1, Mathf.RoundToInt(item.price * 0.55f));
            Money += payout;
            message = $"Black market take {item.displayName} for ${payout}.";
            OnChanged?.Invoke();
            return true;
        }

        public bool TryPurchase(ShopItemDefinition item, out string message)
        {
            if (item == null) { message = "Nothing to buy."; return false; }

            // Consumables can be bought repeatedly; possessions cannot.
            int owner = Character.CharacterSwitchManager.Instance != null
                ? Character.CharacterSwitchManager.Instance.ActiveIndex : 0;
            bool alreadyOwned = IsCharacterWearable(item)
                ? CharacterSet(owner).Contains(item.itemId)
                : _owned.Contains(item.itemId);
            if (!item.IsConsumable && alreadyOwned)
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
            else if (item.category == ShopCategory.Food || item.category == ShopCategory.Enhancement)
            {
                // MINI-073: stashed rather than used immediately - see the
                // class-level remark above _consumables.
                AddConsumable(item);
                message = $"Bought {item.displayName} for ${item.price} - press [ I ] to use it.";
            }
            else
            {
                if (IsCharacterWearable(item)) CharacterSet(owner).Add(item.itemId);
                else _owned.Add(item.itemId);
                message = $"Bought {item.displayName} for ${item.price}.";
            }

            OnChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Called once at scene build time with every Food/Pharmacy item that
        /// exists, so UseConsumable can look effect numbers back up from a
        /// bare item id. Safe to call repeatedly (e.g. once per shop) -
        /// re-registering the same id just overwrites with the same def.
        /// </summary>
        public void RegisterConsumableCatalog(IEnumerable<ShopItemDefinition> items)
        {
            if (items == null) return;
            foreach (var item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.itemId)) continue;
                _consumableCatalog[item.itemId] = item;
            }
        }

        private void AddConsumable(ShopItemDefinition item)
        {
            _consumableCatalog[item.itemId] = item;
            _consumables.TryGetValue(item.itemId, out int current);
            _consumables[item.itemId] = current + 1;
        }

        public int GetConsumableCount(string itemId) =>
            _consumables.TryGetValue(itemId, out int c) ? c : 0;

        /// <summary>MINI-110: hands a stashed consumable over to an NPC
        /// (Normy's item favour) rather than using it - deliberately does
        /// NOT apply the item's vitals effect the way UseConsumable does.</summary>
        public bool TrySpendConsumable(string itemId, int count, out string message)
        {
            int have = GetConsumableCount(itemId);
            if (have < count)
            {
                string name = _consumableCatalog.TryGetValue(itemId, out var def) && def != null ? def.displayName : itemId;
                message = $"Allu need {count} {name} - only have {have}.";
                return false;
            }

            _consumables[itemId] = have - count;
            message = null;
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>Every stashed item id with a count > 0 and its catalog
        /// definition, for the inventory panel to list. A List snapshot
        /// rather than yielding live off the dictionary, since UseConsumable
        /// mutates _consumables and a caller iterating while using would
        /// otherwise be modifying the collection it is enumerating.</summary>
        public List<(ShopItemDefinition item, int count)> GetConsumablesInCategory(ShopCategory category)
        {
            var result = new List<(ShopItemDefinition, int)>();
            foreach (var kv in _consumables)
            {
                if (kv.Value <= 0) continue;
                if (!_consumableCatalog.TryGetValue(kv.Key, out var def) || def == null) continue;
                if (def.category != category) continue;
                result.Add((def, kv.Value));
            }
            return result;
        }

        /// <summary>
        /// MINI-073: applies a stashed item's effect to whoever is currently
        /// controlled, and consumes one from the stash. Reasonable, meaningful
        /// effects per the user's ask - food heals (and gives a small stamina
        /// top-up, same 0.5x-of-heal convention the old instant-use path had),
        /// pharmacy items give a genuine timed stamina/regen boost. Nothing
        /// invented here: these are the same healAmount/staminaBoost/
        /// regenMultiplier/boostSeconds numbers the item already carried -
        /// only WHEN they apply changed, not what they do.
        /// </summary>
        public bool UseConsumable(string itemId, out string message)
        {
            if (!_consumableCatalog.TryGetValue(itemId, out var item) || item == null)
            {
                message = "Don't have that.";
                return false;
            }
            if (GetConsumableCount(itemId) <= 0)
            {
                message = $"Out of {item.displayName}.";
                return false;
            }

            var vitals = Character.CharacterSwitchManager.Instance?.Active?.vitals;
            if (vitals == null)
            {
                message = "Nobody here to take it.";
                return false;
            }

            if (item.healAmount > 0f) vitals.Heal(item.healAmount);
            if (item.staminaBoost > 0f || item.regenMultiplier > 1f)
            {
                vitals.ApplyBoost(item.staminaBoost, item.regenMultiplier, item.boostSeconds);
            }
            else
            {
                vitals.RestoreStamina(item.healAmount * 0.5f);
            }

            _consumables[itemId] = GetConsumableCount(itemId) - 1;

            message = item.category == ShopCategory.Food
                ? $"Ate {item.displayName}. Feeling better."
                : $"Took {item.displayName}. Boost for {Mathf.RoundToInt(item.boostSeconds)}s.";

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
            => TrySellCrops(knownCrops, false, false, 1f, out totalEarned, out _);

        public bool TrySellCrops(IReadOnlyList<CropDefinition> knownCrops, bool illegalOnly,
            bool legalOnly, float priceMultiplier, out int totalEarned, out bool soldIllegal)
        {
            totalEarned = 0;
            soldIllegal = false;
            bool soldAnything = false;

            foreach (var crop in knownCrops)
            {
                int count = GetCount(crop.cropId);
                if (count <= 0) continue;
                if (illegalOnly && !crop.isIllegal) continue;
                if (legalOnly && crop.isIllegal) continue;

                totalEarned += Mathf.RoundToInt(count * crop.sellPrice * priceMultiplier);
                _inventory[crop.cropId] = 0;
                soldAnything = true;
                soldIllegal |= crop.isIllegal;
            }

            if (soldAnything)
            {
                Money += totalEarned;
                OnChanged?.Invoke();
            }

            return soldAnything;
        }

        public void AddMoney(int amount)
        {
            // MINI-060: Gardey Zafeh's DoubleMoney reading - only doubles
            // actual income, never a cost/fee (those pass negative amounts
            // through this same method), so a purchase during the buff
            // isn't accidentally doubled in the player's favour or against
            // them.
            if (amount > 0 && GardeyZafehBuffState.IsActive(GardeyZafehBuff.DoubleMoney)) amount *= 2;
            Money += amount;
            OnChanged?.Invoke();
        }

        /// <summary>MINI-049. Sets an exact total rather than adding a
        /// delta - used by the cheat code so re-entering it doesn't stack
        /// money on top of itself.</summary>
        public void SetMoney(int amount)
        {
            Money = amount;
            OnChanged?.Invoke();
        }

        public void AddHeat(float amount)
        {
            if (HeatLocked) { Heat = 0f; return; }
            Heat = Mathf.Clamp(Heat + amount, 0f, MaxHeat);
            OnChanged?.Invoke();
        }

        /// <summary>Snapshot seeds and owned items for saving.</summary>
        public void CaptureExtras(List<string> seedIds, List<int> seedCounts, List<string> ownedIds)
        {
            foreach (var kv in _seeds)
            {
                seedIds.Add(kv.Key);
                seedCounts.Add(kv.Value);
            }
            ownedIds.AddRange(_owned);
        }

        public void CaptureCharacterOwned(int characterIndex, List<string> ownedIds)
        {
            if (ownedIds == null) return;
            if (_characterOwned.TryGetValue(characterIndex, out var owned)) ownedIds.AddRange(owned);
        }

        /// <summary>MINI-073: snapshot the stashed consumables for saving.
        /// Kept as a separate method (rather than folding into CaptureExtras)
        /// so SaveLoadSystem's existing call site did not need its signature
        /// changed - callers that don't care about consumables (none exist
        /// yet, but future ones might) are unaffected.</summary>
        public void CaptureConsumables(List<string> ids, List<int> counts)
        {
            foreach (var kv in _consumables)
            {
                if (kv.Value <= 0) continue;
                ids.Add(kv.Key);
                counts.Add(kv.Value);
            }
        }

        public void LoadConsumables(IReadOnlyList<string> ids, IReadOnlyList<int> counts)
        {
            _consumables.Clear();
            if (ids == null || counts == null) return;
            for (int i = 0; i < ids.Count && i < counts.Count; i++)
            {
                _consumables[ids[i]] = counts[i];
            }
        }

        /// <summary>Restore saved state - see SaveLoadSystem.</summary>
        public void LoadState(
            int money, float heat,
            IReadOnlyList<string> ids, IReadOnlyList<int> counts,
            IReadOnlyList<string> seedIds = null, IReadOnlyList<int> seedCounts = null,
            IReadOnlyList<string> ownedIds = null,
            IReadOnlyList<string> sacatOwnedIds = null,
            IReadOnlyList<string> frankiOwnedIds = null,
            int legacyWearableOwner = 0)
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

            if (seedIds != null && seedCounts != null)
            {
                _seeds.Clear();
                for (int i = 0; i < seedIds.Count && i < seedCounts.Count; i++)
                {
                    _seeds[seedIds[i]] = seedCounts[i];
                }
            }

            if (ownedIds != null)
            {
                _owned.Clear();
                foreach (var id in ownedIds)
                {
                    if (IsLegacyWearableId(id)) CharacterSet(legacyWearableOwner).Add(id);
                    else _owned.Add(id);
                }
            }


            _characterOwned.Clear();
            if (sacatOwnedIds != null)
                foreach (var id in sacatOwnedIds) CharacterSet(0).Add(id);
            if (frankiOwnedIds != null)
                foreach (var id in frankiOwnedIds) CharacterSet(1).Add(id);

            // Old saves stored wearables in the shared list. Migrate those to
            // whoever was active when the save was made, without dressing both
            // boys from one purchase.
            if (ownedIds != null)
                foreach (var id in ownedIds)
                    if (IsLegacyWearableId(id)) CharacterSet(legacyWearableOwner).Add(id);

            OnChanged?.Invoke();
        }
    }
}
