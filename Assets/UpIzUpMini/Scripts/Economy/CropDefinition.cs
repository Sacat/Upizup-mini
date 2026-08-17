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

        // MINI-047. Unset (alpha 0, the default) means single-colour ripe
        // buds, same as every crop before this one - Purple Black is the
        // first to use it, alternating fruit between ripeColor and this so
        // it visibly shows both parent strains' colours at once.
        [Tooltip("Alpha 0 = single-colour ripe fruit (default). Set alpha > 0 to alternate fruit between ripeColor and this colour - e.g. Purple Black's orange+purple buds.")]
        public Color secondaryRipeColor = new Color(0f, 0f, 0f, 0f);
        public bool HasSecondaryRipeColor => secondaryRipeColor.a > 0.001f;
    }
}
