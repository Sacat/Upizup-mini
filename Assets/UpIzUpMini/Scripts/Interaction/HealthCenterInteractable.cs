using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;

namespace UpIzUpMini.Interaction
{
    /// <summary>
    /// MINI-054. The Gwa Bay Health Center - a rest/heal point in town.
    /// Restoring the active character's health (and, like a safehouse,
    /// cooling heat) for a small fee, reflecting that an injury there costs
    /// money. The existing death handler already charges a "death fee owed
    /// to the health center", so this is the physical home of that system.
    /// </summary>
    public class HealthCenterInteractable : InteractableBase
    {
        [SerializeField] private float healCost = 25f;
        [SerializeField] private float heatRemovedOnHeal = 20f;

        private string _lastFeedback;

        public override string PromptLabel => "[ E ] Gwa Bay Health Center";

        public override void Interact(GameObject interactor)
        {
            var slot = CharacterSwitchManager.Instance?.Active;
            var vitals = slot?.vitals;

            var economy = EconomyManager.Instance;
            if (economy == null || vitals == null)
            {
                _lastFeedback = "Health center closed right now.";
                return;
            }

            if (economy.Money < healCost)
            {
                _lastFeedback = $"Healing cost ${healCost}. Allu short, nuh. Go make ah money firs.";
                return;
            }

            if (vitals.Health >= vitals.MaxHealth && economy.Heat <= 1f)
            {
                _lastFeedback = "Allu already good, no need to spend. Go on, keep busy.";
                return;
            }

            economy.AddMoney(-Mathf.RoundToInt(healCost));
            vitals.Restore();
            economy.AddHeat(-Mathf.RoundToInt(heatRemovedOnHeal));

            _lastFeedback = $"Patched up at Gwa Bay Health Center. Health full. Cost ${Mathf.RoundToInt(healCost)}.";
        }

        public override string GetInteractionFeedback() => _lastFeedback;
    }
}
