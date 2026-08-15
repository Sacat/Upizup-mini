using UnityEngine;

namespace UpIzUpMini.Economy
{
    /// <summary>
    /// Data-driven crop/product definition. Deliberately generic (id,
    /// display name, price, category) rather than a tomato-only type, so
    /// the same shape can later cover cosmetics/vehicles/property when the
    /// shop system grows beyond crops - see PROJECT-HANDOFF.md/memory note
    /// on future economy extensibility.
    /// </summary>
    [CreateAssetMenu(menuName = "Up Iz Up Mini/Crop Definition", fileName = "NewCrop")]
    public class CropDefinition : ScriptableObject
    {
        public string cropId = "tomato";
        public string displayName = "Tomato";
        public int sellPrice = 5;
        public bool isIllegal;

        [Header("Growth visuals")]
        public float growDurationSeconds = 18f;
        public Color unripeColor = new Color(0.3f, 0.55f, 0.25f);
        public Color ripeColor = new Color(0.75f, 0.12f, 0.1f);
    }
}
