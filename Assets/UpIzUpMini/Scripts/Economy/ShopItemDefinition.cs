using UnityEngine;

namespace UpIzUpMini.Economy
{
    public enum ShopCategory
    {
        Seed, Clothing, Footwear, Accessory, Vehicle, Boat, Property, Land,
        /// <summary>Food - restores health. Consumed on purchase.</summary>
        Food,
        /// <summary>Pills/enhancements - temporary stamina boost. Consumed on purchase.</summary>
        Enhancement,
    }

    /// <summary>
    /// A generic purchasable. Deliberately one type covering seeds,
    /// clothing, accessories, vehicles, boats, property and land rather
    /// than a separate class per kind - so new shop stock is data, not
    /// code (see the future-extensibility note recorded during MINI-011).
    ///
    /// Brand names are intentionally fictional near-misses of real brands
    /// (e.g. "Mike", "Lacostes") per the user's direction, so no real
    /// trademark is used.
    /// </summary>
    [CreateAssetMenu(menuName = "Up Iz Up Mini/Shop Item", fileName = "NewShopItem")]
    public class ShopItemDefinition : ScriptableObject
    {
        public string itemId = "item";
        public string displayName = "Item";
        public ShopCategory category = ShopCategory.Clothing;
        public int price = 50;

        [TextArea(1, 3)]
        public string description;

        [Header("Seed items only")]
        public CropDefinition grantsCrop;
        public int seedQuantity = 3;

        [Header("Consumables (Food / Enhancement)")]
        public float healAmount;
        public float staminaBoost;
        public float regenMultiplier = 1f;
        public float boostSeconds = 30f;

        /// <summary>
        /// Consumables are used immediately and can be bought repeatedly,
        /// unlike one-off possessions such as a chain or a vehicle.
        /// </summary>
        public bool IsConsumable => category == ShopCategory.Food
                                    || category == ShopCategory.Enhancement
                                    || category == ShopCategory.Seed;
    }
}
