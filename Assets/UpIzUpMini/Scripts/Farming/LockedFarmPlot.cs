using UnityEngine;
using UpIzUpMini.Economy;

namespace UpIzUpMini.Farming
{
    /// <summary>
    /// An expansion plot that stays fenced off and unusable until the
    /// matching land item is bought from the Farm Shop. Gives the land
    /// purchase a real gameplay effect instead of only setting a flag.
    /// </summary>
    [RequireComponent(typeof(FarmPlot))]
    public class LockedFarmPlot : MonoBehaviour
    {
        [SerializeField] private string requiredItemId = "land_montine";
        [SerializeField] private GameObject lockedVisual;

        private FarmPlot _plot;
        private bool _unlocked;

        // MINI-080: same fix as CharacterEquipment - OnEnable only subscribed
        // if EconomyManager.Instance already existed at that exact moment,
        // with no execution-order guarantee against EconomyManager's own
        // Awake(). Tracked so Start() can retry (guaranteed to run after
        // every Awake() in the scene, including EconomyManager's).
        private bool _subscribed;

        private void Awake()
        {
            _plot = GetComponent<FarmPlot>();
            ApplyState(false);
        }

        private void OnEnable() => TrySubscribe();

        private void OnDisable()
        {
            if (_subscribed && EconomyManager.Instance != null) EconomyManager.Instance.OnChanged -= Check;
            _subscribed = false;
        }

        private void TrySubscribe()
        {
            if (_subscribed || EconomyManager.Instance == null) return;
            EconomyManager.Instance.OnChanged += Check;
            _subscribed = true;
        }

        private void Start()
        {
            TrySubscribe();
            Check();
        }

        private void Check()
        {
            bool owned = EconomyManager.Instance != null
                         && EconomyManager.Instance.OwnsItem(requiredItemId);
            if (owned == _unlocked) return;
            ApplyState(owned);
        }

        private void ApplyState(bool unlocked)
        {
            _unlocked = unlocked;
            if (lockedVisual != null) lockedVisual.SetActive(!unlocked);
            // Disabling the FarmPlot removes it from the interaction
            // registry, so no [E] Plant prompt appears on locked land.
            if (_plot != null) _plot.enabled = unlocked;
        }
    }
}
