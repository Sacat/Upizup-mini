using UnityEngine;

namespace UpIzUpMini.Economy
{
    /// <summary>
    /// Grants a few starting seeds so the player can plant immediately
    /// rather than being blocked behind a shop trip on first launch.
    /// </summary>
    public class StarterInventory : MonoBehaviour
    {
        [SerializeField] private CropDefinition[] startingSeeds;
        [SerializeField] private int seedsPerCrop = 3;

        private void Start()
        {
            if (EconomyManager.Instance == null || startingSeeds == null) return;

            foreach (var crop in startingSeeds)
            {
                // Illegal strains have to be unlocked through the story,
                // not handed out at the start (Docs/STORY.md).
                if (crop == null || crop.isIllegal) continue;
                EconomyManager.Instance.AddSeeds(crop.cropId, seedsPerCrop);
            }
        }
    }
}
