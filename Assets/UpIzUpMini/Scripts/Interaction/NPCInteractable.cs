using UnityEngine;

namespace UpIzUpMini.Interaction
{
    /// <summary>
    /// A standing NPC that shows a "[ E ] Talk" prompt and, on interact,
    /// speaks one short line. Placeholder for a future dialogue system.
    /// </summary>
    public class NPCInteractable : InteractableBase
    {
        [SerializeField] private string npcName = "Villager";

        [TextArea(1, 3)]
        [SerializeField]
        private string dialogueLine = "Ay bway, mind yuhself out dere, the sun hot today.";

        public override string PromptLabel => "[ E ] Talk";

        public override void Interact(GameObject interactor)
        {
            Debug.Log($"{npcName}: {dialogueLine}");
        }

        public override string GetInteractionFeedback() => dialogueLine;
    }
}
