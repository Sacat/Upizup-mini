using UnityEngine;
using UnityEngine.UI;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;

namespace UpIzUpMini.UI
{
    /// <summary>
    /// Persistent gameplay HUD: health/stamina (active character),
    /// heat/money (shared), active character name, and the current
    /// 1-4 crop selection. Reads the relevant managers each frame rather
    /// than subscribing per-field - simplest correct approach for a HUD
    /// this small.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private Image healthFill;
        [SerializeField] private Image staminaFill;
        [SerializeField] private Image heatFill;
        [SerializeField] private Text moneyLabel;
        [SerializeField] private Text characterNameLabel;
        [SerializeField] private Text cropSelectionLabel;

        private void Update()
        {
            var slot = CharacterSwitchManager.Instance != null ? CharacterSwitchManager.Instance.Active : null;
            var vitals = slot?.vitals;

            if (healthFill != null)
            {
                healthFill.fillAmount = vitals != null ? vitals.Health / vitals.MaxHealth : 1f;
            }
            if (staminaFill != null)
            {
                staminaFill.fillAmount = vitals != null ? vitals.Stamina / vitals.MaxStamina : 1f;
            }
            if (heatFill != null)
            {
                heatFill.fillAmount = EconomyManager.Instance != null
                    ? EconomyManager.Instance.Heat / EconomyManager.MaxHeat : 0f;
            }
            if (moneyLabel != null && EconomyManager.Instance != null)
            {
                moneyLabel.text = $"${EconomyManager.Instance.Money}";
            }
            if (characterNameLabel != null)
            {
                characterNameLabel.text = slot != null ? slot.displayName : string.Empty;
            }
            if (cropSelectionLabel != null && CropSelectionController.Instance != null)
            {
                var crop = CropSelectionController.Instance.Selected;
                if (crop == null)
                {
                    cropSelectionLabel.text = string.Empty;
                }
                else
                {
                    int seeds = EconomyManager.Instance != null ? EconomyManager.Instance.GetSeeds(crop.cropId) : 0;
                    int held = EconomyManager.Instance != null ? EconomyManager.Instance.GetCount(crop.cropId) : 0;
                    cropSelectionLabel.text =
                        $"[{CropSelectionController.Instance.SelectedIndex + 1}] {crop.displayName}\n" +
                        $"seeds {seeds}   held {held}";
                }
            }
        }
    }
}
