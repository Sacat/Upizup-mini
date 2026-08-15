using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;
using UpIzUpMini.Missions;

namespace UpIzUpMini.Interaction
{
    /// <summary>Lets the controlled boy give the other boy a farm order face-to-face.</summary>
    public class CompanionInteractable : InteractableBase
    {
        [SerializeField] private FarmhandController farmhand;
        private string _feedback;

        public override bool CanInteract(GameObject interactor)
        {
            return farmhand != null && interactor != gameObject
                   && CharacterSwitchManager.Instance?.Active?.root != gameObject;
        }

        public override string PromptLabel
        {
            get
            {
                string crop = CropSelectionController.Instance?.Selected?.displayName ?? "selected crop";
                return farmhand != null && farmhand.IsWorking
                    ? "[ E ] Ask to follow"
                    : $"[ E ] Send to farm: {crop}";
            }
        }

        public override void Interact(GameObject interactor)
        {
            if (farmhand == null) return;
            var crop = CropSelectionController.Instance?.Selected;
            farmhand.ToggleWorking(crop);
            string name = gameObject.name;
            _feedback = farmhand.IsWorking
                ? $"{name}: Yea wii, I staying to plant and tend {crop?.displayName ?? "that"}."
                : $"{name}: Alright mn, I coming with you.";
            if (farmhand.IsWorking)
                MissionSystem.Instance?.Notify(ObjectiveKind.AssignFarmhand, crop?.cropId);
        }

        public override string GetInteractionFeedback() => _feedback;
    }
}
