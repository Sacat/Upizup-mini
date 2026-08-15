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

        private string _lastFeedback;

        public override string PromptLabel => "[ E ] Rest";

        public override void Interact(GameObject interactor)
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
        }

        public override string GetInteractionFeedback() => _lastFeedback;
    }
}
