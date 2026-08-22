using UnityEngine;
using UnityEngine.UI;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;

namespace UpIzUpMini.UI
{
    /// <summary>
    /// Gameplay HUD: health (green), stamina (yellow) and heat (red) for
    /// the active character, plus money and crop selection.
    ///
    /// Heat is the escalating meter - it starts empty, fills as the player
    /// draws attention, deepens in colour as it climbs, and blinks once
    /// maxed until it cools back down.
    ///
    /// The character name flashes up and fades whenever the player switches
    /// between Franki and Sacat, so it is obvious who is being controlled
    /// without leaving a label on screen permanently.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private Image healthFill;
        [SerializeField] private Image staminaFill;
        [SerializeField] private Image heatFill;
        [SerializeField] private Text moneyLabel;
        [SerializeField] private Text characterNameLabel;
        [SerializeField] private Text cropSelectionLabel;
        [SerializeField] private Text inventoryLabel;
        [SerializeField] private Image reputationFill;
        [SerializeField] private Text reputationPercent;
        [SerializeField] private CropDefinition[] knownCrops;
        // MINI-110: "must show the return countdown" - connects to
        // GuadeloupeTrade's already-existing TripActive/AwayCharacterIndex/
        // SecondsRemaining API rather than tracking travel state here.
        [SerializeField] private Text courierTimerLabel;

        [Header("Meter percentage labels")]
        [SerializeField] private Text healthPercent;
        [SerializeField] private Text staminaPercent;
        [SerializeField] private Text heatPercent;

        [Header("Bar colours")]
        [SerializeField] private Color healthColor = new Color(0.20f, 0.80f, 0.25f);
        [SerializeField] private Color staminaColor = new Color(0.95f, 0.85f, 0.15f);
        [SerializeField] private Color heatLowColor = new Color(0.85f, 0.45f, 0.10f);
        [SerializeField] private Color heatMaxColor = new Color(0.95f, 0.10f, 0.10f);

        private string _lastName;

        private void Start()
        {
            if (healthFill != null) healthFill.color = healthColor;
            if (staminaFill != null) staminaFill.color = staminaColor;

            if (CharacterSwitchManager.Instance != null)
            {
                CharacterSwitchManager.Instance.OnActiveChanged += HandleActiveChanged;
            }
        }

        private void OnDestroy()
        {
            if (CharacterSwitchManager.Instance != null)
            {
                CharacterSwitchManager.Instance.OnActiveChanged -= HandleActiveChanged;
            }
        }

        private void HandleActiveChanged(CharacterSlot slot)
        {
            if (slot == null) return;
            _lastName = slot.displayName;
        }

        private void Update()
        {
            var slot = CharacterSwitchManager.Instance != null ? CharacterSwitchManager.Instance.Active : null;
            var vitals = slot?.vitals;

            if (healthFill != null)
            {
                healthFill.fillAmount = vitals != null && vitals.MaxHealth > 0f
                    ? vitals.Health / vitals.MaxHealth : 1f;
                healthFill.color = healthColor;
                if (healthPercent != null)
                    healthPercent.text = $"Health  {Mathf.RoundToInt(healthFill.fillAmount * 100f)}%";
            }

            if (staminaFill != null)
            {
                // Tracks the active character's stamina, so it visibly
                // drains while running and refills when they ease off.
                staminaFill.fillAmount = vitals != null && vitals.MaxStamina > 0f
                    ? vitals.Stamina / vitals.MaxStamina : 1f;
                staminaFill.color = staminaColor;
                if (staminaPercent != null)
                    staminaPercent.text = $"Energy  {Mathf.RoundToInt(staminaFill.fillAmount * 100f)}%";
            }

            if (heatFill != null)
            {
                float heat01 = EconomyManager.Instance != null
                    ? EconomyManager.Instance.Heat / EconomyManager.MaxHeat : 0f;
                heatFill.fillAmount = heat01;

                Color c = Color.Lerp(heatLowColor, heatMaxColor, heat01);

                // Blink while maxed out, until it cools off.
                if (heat01 >= 0.99f)
                {
                    float blink = Mathf.PingPong(Time.unscaledTime * 4f, 1f);
                    c = Color.Lerp(heatMaxColor, Color.white, blink * 0.6f);
                }

                heatFill.color = c;
                if (heatPercent != null)
                    heatPercent.text = $"Heat  {Mathf.RoundToInt(heat01 * 100f)}%";
            }

            if (moneyLabel != null && EconomyManager.Instance != null)
            {
                moneyLabel.text = $"${EconomyManager.Instance.Money}";
            }

            if (courierTimerLabel != null)
            {
                var trade = GuadeloupeTrade.Instance;
                if (trade != null && trade.TripActive && trade.AwayCharacterIndex >= 0)
                {
                    string awayName = "Someone";
                    var awaySlots = CharacterSwitchManager.Instance?.Slots;
                    if (awaySlots != null && trade.AwayCharacterIndex < awaySlots.Length)
                        awayName = awaySlots[trade.AwayCharacterIndex].displayName;
                    int seconds = Mathf.CeilToInt(trade.SecondsRemaining);
                    courierTimerLabel.text = $"{awayName} in Guadeloupe - back in {seconds}s";
                    courierTimerLabel.enabled = true;
                }
                else
                {
                    courierTimerLabel.enabled = false;
                }
            }

            UpdateNameFlash(slot);

            var progression = UpIzUpMini.Progression.ProgressionManager.Instance;
            float reputation01 = progression != null
                ? Mathf.Clamp01(progression.GangReputation / 100f)
                : 0f;
            if (reputationFill != null) reputationFill.fillAmount = reputation01;
            if (reputationPercent != null)
                reputationPercent.text = $"Street Rep  {Mathf.RoundToInt(reputation01 * 100f)}%";

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
                        $"Seeds [{seeds}]   Held [{held}]";
                }
            }

            if (inventoryLabel != null && EconomyManager.Instance != null)
            {
                var lines = new System.Text.StringBuilder("INVENTORY");
                if (knownCrops != null)
                {
                    foreach (var crop in knownCrops)
                    {
                        if (crop == null) continue;
                        lines.Append($"\n{crop.displayName}: {EconomyManager.Instance.GetCount(crop.cropId)}  Seeds: {EconomyManager.Instance.GetSeeds(crop.cropId)}");
                    }
                }
                var rep = UpIzUpMini.Progression.ProgressionManager.Instance;
                if (rep != null)
                    // MINI-055: display-only "Boss J" - the underlying
                    // BossKReputation field/Faction.BossK enum value are
                    // untouched (see TownNPCInteractable's PromptLabel note).
                    lines.Append($"\nREP  Boss J {rep.BossKReputation}% | Farmers {rep.FarmerReputation}% | Police {rep.PoliceReputation}% | Gangs {rep.GangReputation}% | Normy {rep.NormyReputation}%");
                inventoryLabel.text = lines.ToString();
            }
        }

        private void UpdateNameFlash(CharacterSlot slot)
        {
            if (characterNameLabel == null) return;

            // Show on first run too, not only on a switch.
            if (_lastName == null && slot != null)
            {
                _lastName = slot.displayName;
            }

            characterNameLabel.text = string.IsNullOrEmpty(_lastName)
                ? string.Empty
                : $"ACTIVE: {_lastName.ToUpperInvariant()}";

            float alpha = 1f;

            var c = characterNameLabel.color;
            c.a = alpha;
            characterNameLabel.color = c;
        }
    }
}
