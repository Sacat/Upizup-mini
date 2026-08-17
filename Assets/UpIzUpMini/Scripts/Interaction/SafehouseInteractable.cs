using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;

namespace UpIzUpMini.Interaction
{
    /// <summary>
    /// A safehouse the boys can rest in: fully restores the active
    /// character's health and stamina, cools police heat right down, and
    /// saves the game - the "lie low" beat from Docs/STORY.md, where a
    /// safehouse is where heat stops mattering for a while.
    /// </summary>
    public class SafehouseInteractable : InteractableBase
    {
        [SerializeField] private string safehouseName = "Farm Safehouse";
        [SerializeField] private float heatRemovedOnRest = 60f;
        [SerializeField] private bool savesOnRest = true;

        // MINI-042: gates a second, purchasable safehouse (the Lalay
        // house) the same way LockedFarmPlot gates bought land - empty
        // keeps the original always-usable Montine farm safehouse
        // behaviour unchanged.
        [SerializeField] private string requiredItemId;

        private string _lastFeedback;

        private bool Owned => string.IsNullOrEmpty(requiredItemId)
            || (EconomyManager.Instance != null && EconomyManager.Instance.OwnsItem(requiredItemId));

        public override string PromptLabel
        {
            get
            {
                if (!Owned) return "[ E ] House (locked)";
                return _menuOpen ? "[1] Rest  [2] Save  [3] Load  [E] Leave" : "[ E ] Use bed";
            }
        }

        private bool _menuOpen;

        private void Update()
        {
            if (!_menuOpen) return;

            if (Input.GetKeyDown(KeyCode.Alpha1)) { Rest(); }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SaveLoadSystem.Instance?.Save();
                _lastFeedback = "Game saved.";
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SaveLoadSystem.Instance?.Load();
                _lastFeedback = "Game loaded.";
                _menuOpen = false;
            }
        }

        public override void Interact(GameObject interactor)
        {
            if (!Owned)
            {
                _lastFeedback = $"Dis house not yours yet. Buy the deed first, nuh.";
                return;
            }

            // E toggles the bed menu; the actual actions are 1/2/3 so
            // resting, saving and loading are separate deliberate choices.
            _menuOpen = !_menuOpen;
            if (!_menuOpen)
            {
                _lastFeedback = null;
                return;
            }

            _lastFeedback = "Rest to heal and cool off, or save/load.";
        }

        private void Rest()
        {
            var slot = CharacterSwitchManager.Instance?.Active;
            var vitals = slot?.vitals;

            if (vitals != null)
            {
                vitals.Restore();
            }

            float heatBefore = EconomyManager.Instance != null ? EconomyManager.Instance.Heat : 0f;
            EconomyManager.Instance?.AddHeat(-heatRemovedOnRest);
            float heatAfter = EconomyManager.Instance != null ? EconomyManager.Instance.Heat : 0f;

            if (savesOnRest)
            {
                SaveLoadSystem.Instance?.Save();
            }

            string who = slot != null ? slot.displayName : "Allu";
            _lastFeedback = heatBefore > 1f
                ? $"{who} rest up at {safehouseName}. Health and stamina full, heat down to {Mathf.RoundToInt(heatAfter)}. Saved."
                : $"{who} rest up at {safehouseName}. Health and stamina full. Saved.";
            _menuOpen = false;
        }

        public override string GetInteractionFeedback() => _lastFeedback;
    }
}
