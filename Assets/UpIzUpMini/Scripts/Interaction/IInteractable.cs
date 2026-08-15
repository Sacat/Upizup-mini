using UnityEngine;

namespace UpIzUpMini.Interaction
{
    /// <summary>
    /// Anything the player can walk up to and press the interact key on.
    /// Implemented by NPCs, farm plots, and (later) vehicles/doors/pickups.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>World position the prompt label is anchored above.</summary>
        Transform PromptAnchor { get; }

        /// <summary>Short label shown while in range, e.g. "[ E ] Talk".</summary>
        string PromptLabel { get; }

        bool CanInteract(GameObject interactor);

        void Interact(GameObject interactor);
    }
}
