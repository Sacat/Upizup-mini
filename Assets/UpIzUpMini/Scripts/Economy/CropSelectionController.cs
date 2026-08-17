using UnityEngine;

namespace UpIzUpMini.Economy
{
    /// <summary>Number keys 1-7 pick which crop the next [E] Plant uses.</summary>
    public class CropSelectionController : MonoBehaviour
    {
        public static CropSelectionController Instance { get; private set; }

        [SerializeField] private CropDefinition[] crops = new CropDefinition[4];

        public int SelectedIndex { get; private set; }
        public CropDefinition Selected => (crops != null && crops.Length > 0)
            ? crops[Mathf.Clamp(SelectedIndex, 0, crops.Length - 1)]
            : null;

        public CropDefinition[] AllCrops => crops;

        // MINI-047/MINI-048: extended to 10 (1-9 then 0 for the 10th) for
        // Purple Black, Blue Cheese, Sugar Cheese, and Purple Cheese -
        // interbred or boss-granted, not shop-bought, but still normal
        // plantable crops once you have the seed. Ten number-key slots is
        // a real UX limit worth revisiting (a proper crop-select menu
        // would scale better, especially for the mobile-first target) -
        // not addressed here, just flagged.
        private static readonly KeyCode[] Keys =
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
            KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8,
            KeyCode.Alpha9, KeyCode.Alpha0
        };

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            for (int i = 0; i < Keys.Length && i < crops.Length; i++)
            {
                if (Input.GetKeyDown(Keys[i]))
                {
                    if (UpIzUpMini.Progression.ProgressionManager.Instance != null
                        && !UpIzUpMini.Progression.ProgressionManager.Instance.IsCropUnlocked(crops[i].cropId)) continue;
                    SelectedIndex = i;
                }
            }
        }
    }
}
