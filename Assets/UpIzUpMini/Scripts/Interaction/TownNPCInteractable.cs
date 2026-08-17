using UnityEngine;
using UpIzUpMini.Economy;

namespace UpIzUpMini.Interaction
{
    /// <summary>
    /// FarmShop sells seeds/tools; ApparelShop sells clothing, footwear and
    /// accessories. They are separate shopfronts with separate stock, per
    /// the user's request - not one merged shop.
    /// </summary>
    public enum NpcRole
    {
        Villager, Police, FarmShop, Buyer, ApparelShop, Boss,
        LandOffice, CarDealer, BoatMan, FoodShop, Pharmacy, Vagrant, BlackMarket, StrainBoss
    }

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

        public NpcRole Role => role;
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
        [Tooltip("MINI-039: charged when this boss grants the strain's starter seed - per the user's explicit \"make them expensive\" ask. 0 keeps the old free-grant behaviour for roles that shouldn't charge.")]
        [SerializeField] private int seedPrice;

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
            NpcRole.Vagrant => "[ E ] Sell weed quietly",
            NpcRole.BlackMarket => "[ E ] Black Market",
            NpcRole.StrainBoss => $"[ E ] Talk to {npcName}",
            NpcRole.LandOffice => "[ E ] Land and Surveys",
            NpcRole.CarDealer => "[ E ] Vehicles",
            NpcRole.FoodShop => "[ E ] Food",
            NpcRole.Pharmacy => "[ E ] Pharmacy",
            NpcRole.BoatMan => "[ E ] Guadeloupe Run",
            _ => "[ E ] Talk"
        };

        public override void Interact(GameObject interactor)
        {
            switch (role)
            {
                case NpcRole.Buyer:
                    SellCrops(false, false, 1f, "ProduceBuyer");
                    break;

                case NpcRole.Vagrant:
                    SellCrops(true, false, 0.65f, "Vagrant");
                    break;

                case NpcRole.BlackMarket:
                    _lastFeedback = "I buying clean clothes cheap. Choose what you selling, mn.";
                    shop?.Open();
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
                case NpcRole.FoodShop:
                case NpcRole.Pharmacy:
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
                    // Boss K's offer - Docs/STORY.md Mission 5. Sells the
                    // illegal strain's starter seeds (MINI-039: priced,
                    // previously free) so the player can take the
                    // higher-paying, higher-heat work.
                    if (bossSeedCrop != null && EconomyManager.Instance != null
                        && EconomyManager.Instance.GetSeeds(bossSeedCrop.cropId) <= 0)
                    {
                        _lastFeedback = TryBuySeed(bossSeedCrop, bossOfferLine);
                    }
                    else
                    {
                        int weedHeld = EconomyManager.Instance.GetCount(bossSeedCrop.cropId);
                        if (weedHeld > 0) SellCrops(true, false, 1.35f, "BossK");
                        else _lastFeedback = bossFollowUpLine;
                    }
                    break;

                case NpcRole.StrainBoss:
                    var prog = UpIzUpMini.Progression.ProgressionManager.Instance;
                    bool unlocked = bossSeedCrop != null && prog != null && prog.IsCropUnlocked(bossSeedCrop.cropId);
                    if (!unlocked) _lastFeedback = "You not ready for this strain yet. Build your name first, nuh.";
                    else if (EconomyManager.Instance != null && EconomyManager.Instance.GetSeeds(bossSeedCrop.cropId) <= 0)
                    {
                        _lastFeedback = TryBuySeed(bossSeedCrop, $"Take three {bossSeedCrop.displayName} seed. This work carry more risk.");
                    }
                    else _lastFeedback = $"Bring back the {bossSeedCrop?.displayName ?? "crop"} when it ready.";
                    break;

                default:
                    _lastFeedback = NextLine(villagerLines, "Yea wii.");
                    break;
            }

            Missions.MissionSystem.Instance?.Notify(Missions.ObjectiveKind.TalkTo, npcName);
        }

        /// <summary>
        /// MINI-039. Charges seedPrice for the strain's three starter
        /// seeds rather than handing them over free - real money at risk
        /// for a real illegal-strain unlock, matching the user's "make
        /// them expensive" ask. Declines cleanly (no seed, no charge) if
        /// the player can't afford it, rather than letting Money go
        /// negative the way a death fee is allowed to.
        /// </summary>
        private string TryBuySeed(CropDefinition crop, string successLine)
        {
            var economy = EconomyManager.Instance;
            if (economy == null) return successLine;

            if (seedPrice > 0)
            {
                if (economy.Money < seedPrice)
                {
                    return $"{crop.displayName} seed cost ${seedPrice}. Allu short, nuh. Come back when you have it.";
                }
                economy.AddMoney(-seedPrice);
            }

            economy.AddSeeds(crop.cropId, 3);
            return seedPrice > 0 ? $"{successLine} That cost you ${seedPrice}." : successLine;
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

        private void SellCrops(bool illegalOnly, bool legalOnly, float multiplier, string buyerId)
        {
            if (Character.CharacterSwitchManager.Instance?.Active?.displayName == "Sacat") multiplier *= 1.15f;
            var progression = UpIzUpMini.Progression.ProgressionManager.Instance;
            bool bossSale = buyerId == "BossK";
            if (bossSale && progression != null) multiplier *= progression.BossPayoutMultiplier;
            if (EconomyManager.Instance != null && sellableCrops != null
                && EconomyManager.Instance.TrySellCrops(sellableCrops, illegalOnly, legalOnly,
                    multiplier, out int earned, out bool soldIllegal))
            {
                if (soldIllegal)
                {
                    bool missionSale = Missions.MissionSystem.Instance != null
                        && Missions.MissionSystem.Instance.IsCurrentObjective(Missions.ObjectiveKind.SellCrop, buyerId);
                    EconomyManager.Instance.AddHeat(missionSale ? 50f : 30f);
                    progression?.AddReputation(UpIzUpMini.Progression.Faction.GrandBayGangs, 4);
                    progression?.AddReputation(UpIzUpMini.Progression.Faction.Police, -5);
                }
                else progression?.AddReputation(UpIzUpMini.Progression.Faction.Farmers, 4);

                if (bossSale) progression?.RecordBossJob();
                _lastFeedback = bossSale && earned <= 0
                    ? "Boss K: Money tight. I holding your payment this time. Do the next job and we settle, nuh."
                    : $"Yea mn, sold for ${earned}.";
                Missions.MissionSystem.Instance?.Notify(Missions.ObjectiveKind.SellCrop, buyerId);
            }
            else _lastFeedback = NextLine(buyerLines, "Nothing for me right now, nuh.");
        }
    }
}
