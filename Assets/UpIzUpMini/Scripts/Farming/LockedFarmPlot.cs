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

        private void Awake()
        {
            _plot = GetComponent<FarmPlot>();
            ApplyState(false);
        }

        private void OnEnable()
        {
            if (EconomyManager.Instance != null) EconomyManager.Instance.OnChanged += Check;
        }

        private void OnDisable()
        {
            if (EconomyManager.Instance != null) EconomyManager.Instance.OnChanged -= Check;
        }

        private void Start() => Check();

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
