using UnityEngine;
using UpIzUpMini.Economy;

namespace UpIzUpMini.Interaction
{
    /// <summary>
    /// FarmShop sells seeds/tools; ApparelShop sells clothing, footwear and
    /// accessories. They are separate shopfronts with separate stock, per
    /// the user's request - not one merged shop.
    /// </summary>
    public enum NpcRole { Villager, Police, FarmShop, Buyer, ApparelShop, Boss, LandOffice, CarDealer, BoatMan }

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

        // Phrasing follows the real Dominican conversation the user
        // supplied: "yea wii", "yah wii", "mn", "nuh", "facts", "irie",
        // "allu", "doe" (don't), "dem". Used naturally and sparingly
        // rather than in every sentence.
        [TextArea(1, 3)]
        [SerializeField] private string[] villagerLines =
        {
            "Yea wii, the sun hot today mn.",
            "How di val na? I hear it was irie.",
            "Allu doe miss nothing much round here, nuh.",
            "Chhh. Lucky you.",
        };

        [TextArea(1, 3)]
        [SerializeField] private string shopkeeperLine =
            "Yea mn, I have seed and ting. Take a look nuh.";

        [SerializeField] private UI.ShopPanelController shop;

        [TextArea(1, 3)]
        [SerializeField] private string bossOfferLine =
            "Allu working hard for small money. Take dis Bushers seed - one harvest pay more than all dat tomato. But keep it far from di road, nuh.";
        [TextArea(1, 3)]
        [SerializeField] private string bossFollowUpLine =
            "Grow it good and bring it back. Police doe have to know nothing.";
        [SerializeField] private CropDefinition bossSeedCrop;

        [TextArea(1, 3)]
        [SerializeField] private string[] buyerLines =
        {
            "Bring me good tomato and I go pay you fair, yah wii.",
            "Facts. Quality does sell itself.",
        };

        [TextArea(1, 3)]
        [SerializeField] private string policeCalmLine = "Morning. Everything alright over dey?";
        [TextArea(1, 3)]
        [SerializeField] private string policeSuspiciousLine = "I watching allu close, nuh. Doe try nothing.";

        private int _lineIndex;

        [SerializeField] private CropDefinition[] sellableCrops;

        private string _lastFeedback;

        public override string PromptLabel => role switch
        {
            NpcRole.Buyer => "[ E ] Sell",
            NpcRole.FarmShop => "[ E ] Farm Shop",
            NpcRole.ApparelShop => "[ E ] Clothes Shop",
            NpcRole.Boss => "[ E ] Talk to Boss K",
            _ => "[ E ] Talk"
        };

        public override void Interact(GameObject interactor)
        {
            switch (role)
            {
                case NpcRole.Buyer:
                    if (EconomyManager.Instance != null && sellableCrops != null
                        && EconomyManager.Instance.TrySellAll(sellableCrops, out int earned))
                    {
                        _lastFeedback = $"Yea mn, sold for ${earned}.";
                        Missions.MissionSystem.Instance?.Notify(Missions.ObjectiveKind.SellCrop);
                    }
                    else
                    {
                        _lastFeedback = NextLine(buyerLines, "Nothing to sell right now, nuh.");
                    }
                    break;

                case NpcRole.Police:
                    float heat = EconomyManager.Instance != null ? EconomyManager.Instance.Heat : 0f;
                    _lastFeedback = heat > 40f ? policeSuspiciousLine : policeCalmLine;
                    Debug.Log($"{npcName} (police): {_lastFeedback}");
                    break;

                case NpcRole.FarmShop:
                case NpcRole.ApparelShop:
                case NpcRole.LandOffice:
                case NpcRole.CarDealer:
                    _lastFeedback = shopkeeperLine;
                    shop?.Open();
                    break;

                case NpcRole.BoatMan:
                    // Guadeloupe run - DECISIONS.md D-007: abstracted
                    // dispatch, no Guadeloupe map, 3x price.
                    _lastFeedback = GuadeloupeTrade.Instance != null
                        ? GuadeloupeTrade.Instance.Interact()
                        : "Boat not running today.";
                    break;

                case NpcRole.Boss:
                    // Boss K's offer - Docs/STORY.md Mission 5. Grants the
                    // illegal strain's seeds so the player can take the
                    // higher-paying, higher-heat work.
                    if (bossSeedCrop != null && EconomyManager.Instance != null
                        && EconomyManager.Instance.GetSeeds(bossSeedCrop.cropId) <= 0)
                    {
                        EconomyManager.Instance.AddSeeds(bossSeedCrop.cropId, 3);
                        _lastFeedback = bossOfferLine;
                    }
                    else
                    {
                        _lastFeedback = bossFollowUpLine;
                    }
                    break;

                default:
                    _lastFeedback = NextLine(villagerLines, "Yea wii.");
                    break;
            }

            Missions.MissionSystem.Instance?.Notify(Missions.ObjectiveKind.TalkTo, npcName);
        }

        /// <summary>Cycles through an NPC's lines so repeat talks vary.</summary>
        private string NextLine(string[] lines, string fallback)
        {
            if (lines == null || lines.Length == 0) return fallback;
            string line = lines[_lineIndex % lines.Length];
            _lineIndex++;
            return line;
        }

        public override string GetInteractionFeedback() => _lastFeedback;
    }
}
