using UnityEngine;
using UpIzUpMini.Economy;

namespace UpIzUpMini.Interaction
{
    public enum NpcRole { Villager, Police, Shopkeeper, Buyer }

    /// <summary>
    /// One interactable for every named-role NPC (villager, police,
    /// shopkeeper, produce/crop buyer) rather than a separate class per
    /// role, per AGENTS.md's "keep systems modular" without over-splitting
    /// for four small behaviour differences. Buyer sells the shared
    /// inventory via EconomyManager; Police's line reacts to current heat;
    /// Shopkeeper/Villager just talk.
    /// </summary>
    public class TownNPCInteractable : InteractableBase
    {
        [SerializeField] private NpcRole role = NpcRole.Villager;
        [SerializeField] private string npcName = "Villager";

        [TextArea(1, 3)]
        [SerializeField] private string villagerLine = "Ay bway, mind yuhself out dere, the sun hot today.";
        [TextArea(1, 3)]
        [SerializeField] private string shopkeeperLine = "Bring me good tomatoes and I go pay you fair, yah wii.";
        [TextArea(1, 3)]
        [SerializeField] private string policeCalmLine = "Morning. Everything alright over dey?";
        [TextArea(1, 3)]
        [SerializeField] private string policeSuspiciousLine = "I watching allu close, nuh. Careful.";

        [SerializeField] private CropDefinition[] sellableCrops;

        private string _lastFeedback;

        public override string PromptLabel => role == NpcRole.Buyer ? "[ E ] Sell" : "[ E ] Talk";

        public override void Interact(GameObject interactor)
        {
            switch (role)
            {
                case NpcRole.Buyer:
                    if (EconomyManager.Instance != null && sellableCrops != null
                        && EconomyManager.Instance.TrySellAll(sellableCrops, out int earned))
                    {
                        _lastFeedback = $"Sold for ${earned}.";
                    }
                    else
                    {
                        _lastFeedback = "Nothing to sell right now.";
                    }
                    break;

                case NpcRole.Police:
                    float heat = EconomyManager.Instance != null ? EconomyManager.Instance.Heat : 0f;
                    _lastFeedback = heat > 40f ? policeSuspiciousLine : policeCalmLine;
                    Debug.Log($"{npcName} (police): {_lastFeedback}");
                    break;

                case NpcRole.Shopkeeper:
                    _lastFeedback = shopkeeperLine;
                    Debug.Log($"{npcName} (shopkeeper): {shopkeeperLine}");
                    break;

                default:
                    _lastFeedback = villagerLine;
                    Debug.Log($"{npcName}: {villagerLine}");
                    break;
            }
        }

        public override string GetInteractionFeedback() => _lastFeedback;
    }
}
