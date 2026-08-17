using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;
using UpIzUpMini.Missions;
using UpIzUpMini.Progression;

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

        // MINI-043: a real functional gate, not just when the tutorial text
        // mentions it (MINI-030 only moved when the *hint* appears). Reuses
        // the M8 career choice already recorded on ProgressionManager
        // rather than adding a second, redundant unlock flag - "later
        // unlock" per the user's ask means after that choice is made.
        // Calling the farmhand back to follow (already working -> false)
        // is never gated - he can only ever be in that state after a
        // legitimate unlocked assignment in the first place.
        private static bool FarmhandUnlocked =>
            ProgressionManager.Instance != null
            && ProgressionManager.Instance.Path != CareerPath.Undecided;

        public override string PromptLabel
        {
            get
            {
                if (farmhand != null && !farmhand.IsWorking && !FarmhandUnlocked)
                    return "[ E ] Send to farm (locked)";

                string crop = CropSelectionController.Instance?.Selected?.displayName ?? "selected crop";
                return farmhand != null && farmhand.IsWorking
                    ? "[ E ] Ask to follow"
                    : $"[ E ] Send to farm: {crop}";
            }
        }

        public override void Interact(GameObject interactor)
        {
            if (farmhand == null) return;

            if (!farmhand.IsWorking && !FarmhandUnlocked)
            {
                _feedback = $"{gameObject.name}: Nah mn, we sticking together till you choose your road, nuh.";
                return;
            }

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
