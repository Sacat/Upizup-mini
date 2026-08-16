using UnityEngine;
using UpIzUpMini.Character;

namespace UpIzUpMini.Economy
{
    /// <summary>
    /// The Guadeloupe run, as an abstracted dispatch rather than a
    /// playable location - per DECISIONS.md D-007 and Docs/STORY.md
    /// Chapter Five. Pay the captain a $500 fee, send the whole crop
    /// inventory with the character who is NOT currently controlled, and
    /// after a wait they return with three times the local value.
    ///
    /// While away that character cannot be switched to, which is the
    /// mechanically interesting part: the player gives up their second
    /// body for the duration.
    ///
    /// No Guadeloupe map, route, or evasion detail is modelled.
    /// </summary>
    public class GuadeloupeTrade : MonoBehaviour
    {
        public static GuadeloupeTrade Instance { get; private set; }

        [SerializeField] private CropDefinition[] sellableCrops;
        [SerializeField] private int captainFee = 500;
        [SerializeField] private float tripSeconds = 600f;
        [SerializeField] private float priceMultiplier = 5f;

        public bool TripActive { get; private set; }
        public int AwayCharacterIndex { get; private set; } = -1;
        public float SecondsRemaining => TripActive ? Mathf.Max(0f, _returnAt - Time.time) : 0f;

        private float _returnAt;
        private int _cargoValue;

        private void Awake() => Instance = this;

        private void Update()
        {
            if (!TripActive || Time.time < _returnAt) return;
            CompleteTrip();
        }

        public string Interact()
        {
            if (UpIzUpMini.Progression.ProgressionManager.Instance != null
                && !UpIzUpMini.Progression.ProgressionManager.Instance.GrandBayWeedRouteEstablished)
                return "Captain doe know allu yet. Establish the Grand Bay weed route first.";
            if (TripActive)
            {
                return $"Di boat still out. Back in about {Mathf.CeilToInt(SecondsRemaining)}s.";
            }

            var economy = EconomyManager.Instance;
            var switcher = CharacterSwitchManager.Instance;
            if (economy == null || switcher == null) return "Boat not running today.";

            int cargo = ValueCargo(economy, out int units);
            if (units <= 0)
            {
                return "Bring produce and I go carry it across, yah wii. Nothing to load right now.";
            }

            if (economy.Money < captainFee)
            {
                return $"Di captain want ${captainFee} up front. Come back when you have it.";
            }

            // The courier is whichever boy is not currently controlled.
            int away = 1 - switcher.ActiveIndex;
            var slot = switcher.Slots != null && away < switcher.Slots.Length ? switcher.Slots[away] : null;
            if (slot?.root == null) return "Nobody free to carry it.";

            economy.AddMoney(-captainFee);
            ClearCargo(economy);

            bool smartCourier = string.Equals(slot.displayName, "Sacat", System.StringComparison.OrdinalIgnoreCase);
            _cargoValue = Mathf.RoundToInt(cargo * priceMultiplier * (smartCourier ? 1.2f : 1f));
            AwayCharacterIndex = away;
            TripActive = true;
            _returnAt = Time.time + tripSeconds;

            slot.root.SetActive(false);
            switcher.SetLocked(away, true);

            return $"{slot.displayName} gone with di produce. ${captainFee} paid. " +
                   $"Back in about {Mathf.CeilToInt(tripSeconds)}s.";
        }

        private void CompleteTrip()
        {
            TripActive = false;

            var switcher = CharacterSwitchManager.Instance;
            var slot = switcher?.Slots != null && AwayCharacterIndex >= 0 && AwayCharacterIndex < switcher.Slots.Length
                ? switcher.Slots[AwayCharacterIndex]
                : null;

            if (slot?.root != null)
            {
                // Return them next to the active character.
                var active = switcher.Active?.root;
                if (active != null)
                {
                    var cc = slot.root.GetComponent<CharacterController>();
                    if (cc != null) cc.enabled = false;
                    slot.root.transform.position = active.transform.position + new Vector3(1.6f, 0f, -1.2f);
                    if (cc != null) cc.enabled = true;
                }
                slot.root.SetActive(true);
                switcher.SetLocked(AwayCharacterIndex, false);
            }

            EconomyManager.Instance?.AddMoney(_cargoValue);
            Debug.Log($"Guadeloupe: returned with ${_cargoValue}");

            AwayCharacterIndex = -1;
            _cargoValue = 0;
        }

        private int ValueCargo(EconomyManager economy, out int units)
        {
            int value = 0;
            units = 0;
            if (sellableCrops == null) return 0;

            foreach (var crop in sellableCrops)
            {
                if (crop == null) continue;
                int count = economy.GetCount(crop.cropId);
                units += count;
                value += count * crop.sellPrice;
            }
            return value;
        }

        private void ClearCargo(EconomyManager economy)
        {
            if (sellableCrops == null) return;
            foreach (var crop in sellableCrops)
            {
                if (crop == null) continue;
                int count = economy.GetCount(crop.cropId);
                if (count > 0) economy.AddCrop(crop.cropId, -count);
            }
        }
    }
}
