using UnityEngine;

namespace UpIzUpMini.Economy
{
    /// <summary>Number keys 1-4 pick which crop the next [E] Plant uses.</summary>
    public class CropSelectionController : MonoBehaviour
    {
        public static CropSelectionController Instance { get; private set; }

        [SerializeField] private CropDefinition[] crops = new CropDefinition[4];

        public int SelectedIndex { get; private set; }
        public CropDefinition Selected => (crops != null && crops.Length > 0)
            ? crops[Mathf.Clamp(SelectedIndex, 0, crops.Length - 1)]
            : null;

        public CropDefinition[] AllCrops => crops;

        private static readonly KeyCode[] Keys =
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
            KeyCode.Alpha5, KeyCode.Alpha6
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
