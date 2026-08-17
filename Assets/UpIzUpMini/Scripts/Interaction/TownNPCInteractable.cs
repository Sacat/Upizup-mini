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
        LandOffice, CarDealer, BoatMan, FoodShop, Pharmacy, Vagrant, BlackMarket, StrainBoss,
        StrainTeacher, Normy
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
        [SerializeField] private UI.SeedBuyDialogue seedBuyDialogue;

        [TextArea(1, 3)]
        [SerializeField] private string bossOfferLine =
            "Yea wii, wah is di word? Allu working hard for small money. Take dis Bushers seed - one harvest pay more than all dat tomato. But keep it far from di road, nuh.";
        [TextArea(1, 3)]
        [SerializeField] private string bossFollowUpLine =
            "Grow it good and bring it back. Police doe have to know nothing.";
        // Boss J is quietly vollehing (stealing) from the player under the
        // table, but per the user this is NEVER said directly in dialogue -
        // the character notices it in his own mind. This inner-monologue
        // line is where that unspoken suspicion lives.
        [TextArea(1, 3)]
        [SerializeField] private string bossInnerMonologue =
            "(Somethin doh feel right with this man. He pay light, he laugh heavy. I watchin how much he keeps for heself...) ";
        [SerializeField] private CropDefinition bossSeedCrop;
        [Tooltip("MINI-039: charged when this boss grants the strain's starter seed - per the user's explicit \"make them expensive\" ask. 0 keeps the old free-grant behaviour for roles that shouldn't charge.")]
        [SerializeField] private int seedPrice;

        // StrainTeacher - an older Rasta who teaches each new strain after
        // you complete his missions. Jamaican-sounding, per the user
        // (everyone else is Dominican).
        [TextArea(1, 3)]
        [SerializeField] private string[] strainTeacherLines =
        {
            "Selassie I, yute. Wah di word diah?",
            "You wan learn di higher herb? First you prove yourself, mon.",
            "Zeb is life, but respect it. Build your name, den we talk.",
            "One love, bredren. Jah guide.",
        };

        // Boss C (StrainBoss) vends the strong strain which breaks down
        // into the higher strains. He holds a roster of tier-ordered strain
        // crops and serves whichever is lowest-tier/next, so one boss
        // replaces the old three strain bosses.
        [SerializeField] private CropDefinition[] strainRoster;

        // Normy - a crooked cop who gives the player missions. Sounds
        // Dominican like everyone else.
        [TextArea(1, 3)]
        [SerializeField] private string normyLine =
            "Psst. Awa wii, doh look surprise. I Normy - I wear di badge but I got my own work. You help me, I help you, and police stay off your back. Wah is di word?";

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
            NpcRole.Boss => "[ E ] Talk to Boss J",
            NpcRole.Vagrant => "[ E ] Sell zeb quietly",
            NpcRole.BlackMarket => "[ E ] Black Market",
            NpcRole.StrainBoss => $"[ E ] Talk to {npcName}",
            NpcRole.StrainTeacher => "[ E ] Talk to Rasta",
            NpcRole.Normy => "[ E ] Talk to Normy",
            NpcRole.LandOffice => "[ E ] Land and Surveys",
            NpcRole.CarDealer => "[ E ] Vehicles",
            NpcRole.FoodShop => "[ E ] Food",
            NpcRole.Pharmacy => "[ E ] Pharmacy",
            NpcRole.BoatMan => "[ E ] Gwada Run",
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
                    // Boss J's offer (renamed from Boss K per the user's
                    // two-boss redesign). Sells the illegal strain's starter
                    // seeds through a real seed-buy dialogue box (MINI-053).
                    // The player's inner monologue quietly hints that Boss J
                    // is vollehing, never said aloud.
                    _lastFeedback = bossInnerMonologue;
                    if (bossSeedCrop != null && EconomyManager.Instance != null
                        && EconomyManager.Instance.GetSeeds(bossSeedCrop.cropId) <= 0)
                    {
                        if (seedBuyDialogue != null)
                        {
                            seedBuyDialogue.Open(bossSeedCrop, seedPrice, bossOfferLine, bossFollowUpLine);
                        }
                        else _lastFeedback = TryBuySeed(bossSeedCrop, bossOfferLine);
                    }
                    else
                    {
                        int zebHeld = EconomyManager.Instance.GetCount(bossSeedCrop.cropId);
                        if (zebHeld > 0) SellCrops(true, false, 1.35f, "Boss J");
                        else _lastFeedback += "\n" + bossFollowUpLine;
                    }
                    break;

                case NpcRole.StrainTeacher:
                    // Older Rasta who teaches new strains after you complete
                    // his missions. Jamaican-sounding.
                    _lastFeedback = NextLine(strainTeacherLines, "One love, bredren.");
                    break;

                case NpcRole.Normy:
                    // Crooked cop; gives the player missions. Dominican
                    // accent. When the player carries his ordered parcel
                    // (has sellable crops), buys it quietly so his N1
                    // mission completes; otherwise keeps talking.
                    _lastFeedback = normyLine;
                    bool onNormy = Missions.MissionSystem.Instance != null
                        && Missions.MissionSystem.Instance.IsCurrentObjective(Missions.ObjectiveKind.SellCrop, "Normy");
                    if (onNormy && EconomyManager.Instance != null && sellableCrops != null
                        && EconomyManager.Instance.TrySellCrops(sellableCrops, false, false, 1.1f,
                            out int earnedN, out bool _))
                    {
                        _lastFeedback = earnedN > 0
                            ? $"Quiet, quiet. Di parcel gone. ${earnedN} for allu - and I doe see nothing."
                            : "Nothing to move right now, nuh. Go plant what I tell you.";
                        Missions.MissionSystem.Instance?.Notify(Missions.ObjectiveKind.SellCrop, "Normy");
                    }
                    break;

                case NpcRole.StrainBoss:
                    // Boss C (MINI-053 two-boss redesign) - vends the strong
                    // strain that breaks down into black sugar / purple /
                    // blue cheese. Uses the tier-ordered strain roster so one
                    // boss serves the progressively stronger strains.
                    var prog = UpIzUpMini.Progression.ProgressionManager.Instance;
                    CropDefinition offer = StrainForPlayer(prog);
                    if (offer == null) _lastFeedback = "You not ready for this strain yet. Build your name first, nuh.";
                    else if (EconomyManager.Instance != null && EconomyManager.Instance.GetSeeds(offer.cropId) <= 0)
                    {
                        string pitch = $"Take three {offer.displayName} seed. This work carry more risk.";
                        if (seedBuyDialogue != null)
                            seedBuyDialogue.Open(offer, seedPrice, pitch, $"Bring back the {offer.displayName} when it ready.");
                        else _lastFeedback = TryBuySeed(offer, pitch);
                    }
                    else _lastFeedback = $"Bring back the {offer?.displayName ?? "crop"} when it ready.";
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

        /// <summary>
        /// MINI-053. Boss C serves his strain roster in tier order: pick the
        /// lowest-tier strain the player can access next (the first one they
        /// don't already hold seeds for, or the lowest unowned one). Returns
        /// null if none are available yet so the boss tells them to build
        /// their name first.
        /// </summary>
        private CropDefinition StrainForPlayer(UpIzUpMini.Progression.ProgressionManager prog)
        {
            if (strainRoster == null || strainRoster.Length == 0)
            {
                // Fall back to the single legacy strain field.
                return (prog == null || bossSeedCrop == null || prog.IsCropUnlocked(bossSeedCrop.cropId))
                    ? bossSeedCrop : null;
            }

            var economy = EconomyManager.Instance;
            foreach (var strain in strainRoster)
            {
                if (strain == null) continue;
                bool unlocked = prog == null || prog.IsCropUnlocked(strain.cropId);
                if (unlocked) return strain;
            }
            // Nothing unlocked yet - return the cheapest/first tier so the
            // player has a clear next goal (the boss still withholds it
            // behind the reputation/unlock check elsewhere).
            return strainRoster[0];
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
            bool bossSale = buyerId == "Boss J";
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
                    ? "Boss J: Money tight. I holding your payment this time. Do the next job and we settle, nuh."
                    : $"Yea mn, sold for ${earned}.";
                Missions.MissionSystem.Instance?.Notify(Missions.ObjectiveKind.SellCrop, buyerId);
            }
            else _lastFeedback = NextLine(buyerLines, "Nothing for me right now, nuh.");
        }
    }
}
