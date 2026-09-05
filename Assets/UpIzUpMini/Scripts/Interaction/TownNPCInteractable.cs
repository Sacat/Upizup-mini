using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;
using UpIzUpMini.Progression;

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
        // MINI-057: a crooked officer, deliberately not part of the
        // PoliceOfficer patrol/chase system - "Normy is not representative
        // of all police" per the brief, so he doesn't detect/chase like a
        // real officer, he just stands around and takes bribes.
        Normy,
        // MINI-058: recruits up to recruitPool.Length Not Ah Word members
        // (the brief's player-gang name) for a fee, from a pre-built
        // pooled roster (see Mini011PhaseBSetup.BuildNotAhWordRoster) -
        // same pooled-not-instantiated-on-the-fly approach as
        // PoliceReinforcementSpawner, just player-triggered instead of
        // heat-triggered.
        GangRecruiter
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
        private void Awake() => EnsurePhysicalHitCollider(gameObject);

        public static Collider EnsurePhysicalHitCollider(GameObject npc)
        {
            if (npc == null) return null;
            CharacterController controller = npc.GetComponent<CharacterController>();
            if (controller == null) controller = npc.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.95f, 0f);
            controller.height = 1.85f;
            controller.radius = 0.32f;
            controller.stepOffset = 0.3f;
            controller.slopeLimit = 50f;
            return controller;
        }

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
            "Take a look and see what making sense for you, nuh.";

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

        // MINI-055: Boss consolidation. Boss C offers every higher-tier
        // strain (Black Sugar, Purple, Blue Cheese...) from ONE NPC instead
        // of three separate ones (previously BossM/BossP/BossQ), each
        // becoming available as its own progression unlock crosses. Purely
        // additive to the single-crop bossSeedCrop path above - a
        // StrainBoss with no multiBossCrops set (there are none left after
        // this task, but nothing forces that) falls back to the original
        // single-crop behaviour unchanged.
        [SerializeField] private CropDefinition[] multiBossCrops;
        [SerializeField] private int[] multiBossSeedPrices;

        [TextArea(1, 3)]
        [SerializeField] private string[] buyerLines =
        {
            "Bring me good tomato and I go pay you fair, yah wii.",
            "Facts. Quality does sell itself.",
        };

        // MINI-119, user: "the dialogue for the boatman should be dynamic
        // as well." GuadeloupeTrade.Interact() already varies its dialogue
        // by real state (no route yet / boat away / cash short / trip
        // just started, etc.) - but the single most common repeat case,
        // walking up with nothing loaded, always answered with the exact
        // same fixed sentence. HandleBoatMan() now rotates that one case
        // through a small set of lines, the same NextLine mechanism every
        // other villager already uses for repeat-talk variety.
        [TextArea(1, 3)]
        [SerializeField] private string[] boatManIdleLines =
        {
            "Bring produce and I go carry it across, yah wii. Nothing to load right now.",
            "Boat ready when allu have something worth di crossing, mn.",
            "Nothing in your hand for me today, nuh. Come back when you loaded up.",
        };

        [TextArea(1, 3)]
        [SerializeField] private string policeCalmLine = "Morning. Everything alright over dey?";
        [TextArea(1, 3)]
        [SerializeField] private string policeSuspiciousLine = "I watching allu close, nuh. Doe try nothing.";

        // MINI-057: Normy - self-interested assistance (a bribe that cools
        // heat) plus a relationship that makes repeat business cheaper.
        [Tooltip("Base bribe cost - reduced slightly as NormyReputation grows (repeat business).")]
        [SerializeField] private int normyBaseBribeCost = 100;
        [SerializeField] private int normyMinBribeCost = 100;
        [SerializeField] private float normyHeatReduction = 20f;
        [SerializeField] private float normyBribeCooldownSeconds = 45f;
        // MINI-109: the Clean Face (M5B) mission payment is a distinct,
        // one-time transaction from the reusable ambient bribe below - see
        // HandleNormy's missionPending branch for why they must not share
        // the ambient service's heat/cooldown gate.
        [SerializeField] private int normyMissionPaymentCost = 100;
        private float _normyReadyAt;

        // MINI-058: Not Ah Word recruitment. Pooled, not instantiated on
        // the fly (see NpcRole.GangRecruiter's own comment) - recruiting
        // just SetActive(true)s + prices the next un-recruited pool slot,
        // so "up to four members" is enforced by the pool's own size.
        [SerializeField] private GangMemberController[] recruitPool;
        [SerializeField] private int recruitCost = 150;

        // MINI-060 follow-up: the user asked to fold Gardey Zafeh into the
        // SAME boat man NPC instead of a second character on the jetty -
        // "his job is just to provide transportation to Guadeloupe."
        // Reveal is a one-time paid event; every reading after that is
        // free but rate-limited by GardeyZafehBuffState.AnyActive (only
        // one reading active at once). Any successful trip (reveal,
        // reading, or a real produce sale) sends him "away" - his own
        // GameObject disappears (SetActive false) for boatManAwaySeconds,
        // matching "he will disappear until the time is up."
        // MINI-060 follow-up-2: "the boat man price should be 3000" - taken
        // to mean this reveal price (the most recently-discussed boat man
        // price in context), not captainFee (the unrelated Guadeloupe
        // produce-trade fee, unchanged at 500). Flagged to the user in case
        // that reading is wrong.
        [SerializeField] private int gardeyZafehRevealCost = 3000;
        [SerializeField] private float gardeyZafehBuffDurationSeconds = 600f; // 10 minutes
        [SerializeField] private float boatManAwaySeconds = 500f;
        // "Jalousie" (jealousy) is the one Guadeloupean Kreyol word used
        // here deliberately - it's the actual term the user gave me for
        // this exact line, not a guess like MINI-054's Unconfirmed terms.
        [TextArea(1, 3)]
        [SerializeField] private string gardeyZafehJalousieLine =
            "I see riches coming for allu. But mind di jalousie people close to you, chile - dat's how good ting does turn bad.";

        private int _lineIndex;
        private bool _hasMet;

        // MINI-081: "some drug missions" - see the intervention roll inside
        // SellCrops() below.
        [SerializeField, Range(0f, 1f)] private float drugMissionPoliceInterventionChance = 0.35f;
        [SerializeField] private float drugMissionPoliceInterventionExtraHeat = 25f;

        [SerializeField] private CropDefinition[] sellableCrops;

        // MINI-053: optional data-driven dialogue foundation. Purely
        // additive - unset (the default for every NPC not explicitly
        // wired to one), villagerLines/etc keep working exactly as before.
        // When set, the villager (default) case prefers it, falling back
        // to villagerLines only if no line is eligible.
        [SerializeField] private Dialogue.DialogueSet dialogueSet;

        private string _lastFeedback;
        private bool _cleanTalkCounted;

        // MINI-060 follow-up-2 bugfix: "boatman disappeared, he did not
        // return." Root cause - DisappearForTrip used to
        // gameObject.SetActive(false) on the coroutine's own GameObject,
        // which Unity terminates immediately, killing the coroutine before
        // its second WaitForSeconds/reactivation line ever ran. Fixed by
        // never deactivating the root (so the coroutine, and this script,
        // keep running) - only the "Visual" child model is hidden, driven
        // by a plain timestamp checked every Update() instead of a second
        // yield. CanInteract() is overridden so InteractionDetector still
        // can't select him while away, same end result as before minus the
        // fragile coroutine-across-SetActive dependency.
        private bool _away;
        private float _awayUntil;
        private Transform _visual;

        // MINI-081: set while the paid-for Dog Life reveal trip is in
        // progress; the reveal itself (flag + line) fires from Update()
        // the moment he actually returns, not at the moment of payment.
        private bool _pendingDogLifeReveal;

        public override string PromptLabel => role switch
        {
            NpcRole.Buyer => "[ E ] Sell",
            NpcRole.FarmShop => "[ E ] Farm Shop",
            NpcRole.ApparelShop => "[ E ] Clothes Shop",
            // MINI-055: display-only rename (Boss K -> Boss J, the
            // lower-level/Bushers boss per the Boss consolidation brief).
            // The underlying npcName/targetId/Faction.BossK/BossKReputation
            // plumbing is untouched - those are internal identifiers
            // several missions already key off, not player-visible text,
            // so renaming them would be a much larger, riskier change for
            // no player-facing benefit.
            NpcRole.Boss => "[ E ] Talk to Boss J",
            NpcRole.Vagrant => "[ E ] Talk to Paro",
            NpcRole.BlackMarket => "[ E ] Black Market",
            NpcRole.StrainBoss => $"[ E ] Talk to {npcName}",
            NpcRole.LandOffice => "[ E ] Land and Surveys",
            NpcRole.CarDealer => "[ E ] Vehicles",
            NpcRole.FoodShop => "[ E ] Food",
            NpcRole.Pharmacy => "[ E ] Pharmacy",
            // MINI-060 follow-up: one prompt covering everything he now
            // does (produce trade, the Dog Life reveal, and readings) -
            // the exact response is decided inside HandleBoatMan() each
            // press, same pattern as Normy's single "[ E ] Talk to Normy".
            NpcRole.BoatMan => "[ E ] Guadeloupe Run",
            NpcRole.Normy => "[ E ] Talk to Normy",
            NpcRole.GangRecruiter => $"[ E ] Recruit (${recruitCost})",
            _ => "[ E ] Talk"
        };

        public override void Interact(GameObject interactor)
        {
            if (!CanUseCurrentRole(out string lockedFeedback))
            {
                _lastFeedback = lockedFeedback;
                return;
            }

            bool firstMeeting = !_hasMet;
            _hasMet = true;

            switch (role)
            {
                case NpcRole.Buyer:
                    SellCrops(false, false, 1f, "ProduceBuyer");
                    break;

                case NpcRole.Vagrant:
                    if (firstMeeting)
                        _lastFeedback = FirstMeetingDialogue();
                    else
                        SellCrops(true, false, 0.65f, "Vagrant");
                    break;

                case NpcRole.BlackMarket:
                    _lastFeedback = "I buying clothes and accessories allu done with. Show me what you have, mn.";
                    // MINI-082: pass this NPC's own transform so the shop
                    // panel can fade itself shut when the player walks away.
                    shop?.Open(transform);
                    break;

                case NpcRole.Police:
                    float heat = EconomyManager.Instance != null ? EconomyManager.Instance.Heat : 0f;
                    _lastFeedback = heat > 40f ? policeSuspiciousLine : policeCalmLine;
                    Debug.Log($"{npcName} (police): {_lastFeedback}");
                    if (!_cleanTalkCounted && EconomyManager.Instance != null && !EconomyManager.Instance.HasIllegalGoods()
                        && Missions.MissionSystem.Instance != null
                        && Missions.MissionSystem.Instance.IsCurrentObjective(Missions.ObjectiveKind.TalkToCleanPolice, "Police"))
                    {
                        _cleanTalkCounted = true;
                        Missions.MissionSystem.Instance.Notify(Missions.ObjectiveKind.TalkToCleanPolice, "Police");
                    }
                    break;

                case NpcRole.FarmShop:
                case NpcRole.ApparelShop:
                case NpcRole.LandOffice:
                case NpcRole.CarDealer:
                case NpcRole.FoodShop:
                case NpcRole.Pharmacy:
                    _lastFeedback = ShopDialogueForRole();
                    shop?.Open(transform);
                    break;

                case NpcRole.BoatMan:
                    _lastFeedback = HandleBoatMan();
                    break;

                case NpcRole.Boss:
                    // Boss K's offer - Docs/STORY.md Mission 5. Sells the
                    // illegal strain's starter seeds (MINI-039: priced,
                    // previously free) so the player can take the
                    // higher-paying, higher-heat work.
                    //
                    // MINI-109: root cause of "Black Sugar delivery to Boss
                    // J sometimes does not advance" - this gate used to
                    // check ONLY bossSeedCrop (hardcoded to whichever crop
                    // is first found isIllegal, i.e. Bushers) even though
                    // Boss J's own sellableCrops list (and TrySellCrops
                    // itself) already handles every illegal strain. A
                    // player holding harvested Black Sugar but zero Bushers
                    // measured weedHeld==0 and SellCrops() was never even
                    // called, so the sale - and the SellCrop/BossK mission
                    // notify inside it - never happened. Now checks whether
                    // ANY of Boss J's actual sellable illegal crops are
                    // held, matching what a sale would really do.
                    bool anyWeedHeld = HasAnySellableIllegalStock();
                    if (anyWeedHeld)
                    {
                        SellCrops(true, false, 1.35f, "BossK");
                    }
                    else if (bossSeedCrop != null && EconomyManager.Instance != null
                        && EconomyManager.Instance.GetSeeds(bossSeedCrop.cropId) <= 0)
                    {
                        _lastFeedback = TryBuySeed(bossSeedCrop, seedPrice, bossOfferLine);
                    }
                    else
                    {
                        _lastFeedback = bossFollowUpLine;
                    }
                    break;

                case NpcRole.StrainBoss:
                    _lastFeedback = multiBossCrops != null && multiBossCrops.Length > 0
                        ? HandleMultiStrainBoss()
                        : HandleSingleStrainBoss();
                    break;

                case NpcRole.Normy:
                    _lastFeedback = HandleNormy();
                    break;

                case NpcRole.GangRecruiter:
                    _lastFeedback = HandleRecruit();
                    break;

                default:
                    // MINI-119, user: "rasta would have to sell blue cheese
                    // seeds. he can say try out this new strain i have
                    // blue cheese" - a real, repeatable sell (like Boss J's
                    // own seed sales), not a one-time gift, so the player
                    // can restock. Returns null (falls through to normal
                    // villager dialogue) until Blue Cheese is actually
                    // unlocked - never advertised early.
                    string rastaOffer = npcName == "Rasta" ? TryOfferRastaBlueCheeseSeed() : null;
                    if (rastaOffer != null) { _lastFeedback = rastaOffer; break; }

                    var line = dialogueSet != null ? dialogueSet.SelectLine() : null;
                    _lastFeedback = line != null ? line.text : NextLine(villagerLines, "Yea wii.");
                    break;
            }

            if (firstMeeting && role != NpcRole.Vagrant)
            {
                string intro = FirstMeetingDialogue();
                if (!string.IsNullOrEmpty(intro)) _lastFeedback = $"{intro}\n{_lastFeedback}";
            }

            Missions.MissionSystem.Instance?.Notify(Missions.ObjectiveKind.TalkTo, npcName);
        }

        private bool CanUseCurrentRole(out string feedback)
        {
            feedback = "Keep doing your ting. I'll maybe organize you when you build up ur self";
            switch (role)
            {
                case NpcRole.Boss: return ProgressionGate.CanUseBossJ;
                case NpcRole.Vagrant: return ProgressionGate.CanUseParo;
                case NpcRole.StrainBoss: return ProgressionGate.CanUseBossC;
                case NpcRole.BoatMan: return ProgressionGate.CanUseBoat;
                case NpcRole.BlackMarket: return ProgressionGate.CanUseBlackMarket;
                case NpcRole.Normy: return ProgressionGate.CanUseNormy;
                case NpcRole.GangRecruiter:
                    if (!ProgressionGate.IsMissionReached("M15")) return false;
                    int rep = ProgressionManager.Instance != null ? ProgressionManager.Instance.GangReputation : 0;
                    if (rep < 20)
                    {
                        feedback = $"Build your street reputation first. You at {Mathf.Max(0, rep)}%; I need to see 20%.";
                        return false;
                    }
                    return true;
                default: return true;
            }
        }

        private string FirstMeetingDialogue() => role switch
        {
            NpcRole.FarmShop => "Farm Seller: First time I seeing allu here. I sell legal seed for the Highland plots.",
            NpcRole.Buyer => "Produce Buyer: I buy clean crop from local farmers. Bring it ripe and I pay fair.",
            NpcRole.ApparelShop => "Clothes Man: Welcome, fellas. Clothes, shoes and accessories inside.",
            NpcRole.LandOffice => "Land and Surveys Man: I handle surveyed lots and property deeds for Grand Bay.",
            NpcRole.CarDealer => "Vehicle Dealer: Save your money first; transport does change how far allu can work.",
            NpcRole.FoodShop => "Food Vendor: If allu hungry, pass through. I have local food ready.",
            NpcRole.Pharmacy => "Pharmacy Clerk: I have health and energy supplies when the road wearing allu down.",
            NpcRole.Vagrant => "Paro: Eh boss, easy. I sleeping rough these days, but I might take a little thing off allu hand.",
            NpcRole.BlackMarket => "Black Market Trader: Things you done wearing can still make a little money here.",
            NpcRole.Boss => "Boss J: I hearing Sacat and Franki trying to make a name farming up Highland.",
            NpcRole.StrainBoss => $"{npcName}: Boss J send allu? Higher-grade work have higher consequences.",
            NpcRole.Police => $"{npcName}: First time I seeing allu on this stretch. Keep out of trouble.",
            // MINI-110: the approved four-line first introduction, per
            // Docs/CLAUDE-HANDOFF-CURRENT.md section 6 - replacing the
            // earlier generic one-liner.
            NpcRole.BoatMan =>
                "Boat Man: I see allu on allu hustle. Dat is a good ting.\n" +
                "Boat Man: When allu want go up and make some euro, talk to me.\n" +
                "Sacat/Franki: So you could check some business by Gardey in Gwada?\n" +
                "Boat Man: Awright. I will introduce allu to the scene up dere one time.",
            NpcRole.Normy => "Normy: People call me Normy. If you need information, we could reason.",
            NpcRole.GangRecruiter => "Recruiter: Respect come before numbers. Show the block allu serious first.",
            _ => $"{npcName}: Wah happen? I seeing allu around Grand Bay now.",
        };

        private string ShopDialogueForRole() => role switch
        {
            NpcRole.FarmShop => "Farm Seller: Tomato, banana and carrot seed ready. Pick what your plot need.",
            NpcRole.ApparelShop => "Clothes Man: I have clothes, shoes and accessories. Each purchase is for the boy buying it.",
            NpcRole.LandOffice => "Land and Surveys Man: Available surveyed lots showing on the list. More unlock as allu progress.",
            NpcRole.CarDealer => "Vehicle Dealer: Only transport allu qualify for will show here.",
            NpcRole.FoodShop => "Food Vendor: Choose something and put it in your inventory for when you need it.",
            NpcRole.Pharmacy => "Pharmacy Clerk: These supplies can help health or stamina; use them from inventory.",
            _ => shopkeeperLine,
        };

        /// <summary>
        /// MINI-039. Charges the given price for the strain's three
        /// starter seeds rather than handing them over free - real money
        /// at risk for a real illegal-strain unlock, matching the user's
        /// "make them expensive" ask. Declines cleanly (no seed, no charge)
        /// if the player can't afford it, rather than letting Money go
        /// negative the way a death fee is allowed to.
        ///
        /// MINI-055: price is now an explicit parameter rather than always
        /// reading the single `seedPrice` field, since a consolidated Boss
        /// C NPC offers several strains at different prices from one
        /// character - the field is still used for the single-crop Boss
        /// (K/J) and legacy single-crop StrainBoss paths.
        /// </summary>
        /// <summary>
        /// MINI-119, user: "rasta would have to sell blue cheese seeds. he
        /// can say try out this new strain i have blue cheese." Rasta's
        /// sellableCrops was wired (Mini011PhaseBSetup.BuildRastaMentor) to
        /// the full crop list specifically so this lookup works. Gated on
        /// ProgressionManager.IsCropUnlocked so Rasta never advertises
        /// Blue Cheese before his own teaching mission has actually taught
        /// it (RastaTaughtBlueCheese, set via Mission.unlocksCropId). Price
        /// matches Boss C's own Blue Cheese seed price for consistency.
        /// Returns null (falls through to normal villager dialogue) when
        /// not applicable - crop missing, not yet unlocked, or already
        /// stocked - rather than always answering with a sales pitch.
        /// </summary>
        private string TryOfferRastaBlueCheeseSeed()
        {
            if (ProgressionManager.Instance == null || !ProgressionManager.Instance.IsCropUnlocked("blue_cheese"))
                return null;

            var crop = sellableCrops != null
                ? System.Array.Find(sellableCrops, c => c != null && c.cropId == "blue_cheese")
                : null;
            if (crop == null) return null;

            var economy = EconomyManager.Instance;
            if (economy != null && economy.GetSeeds(crop.cropId) > 0) return null;

            return TryBuySeed(crop, 650, "Rasta: Try out this new strain I have, Blue Cheese.");
        }

        private string TryBuySeed(CropDefinition crop, int price, string successLine)
        {
            var economy = EconomyManager.Instance;
            if (economy == null) return successLine;

            if (price > 0)
            {
                if (economy.Money < price)
                {
                    return $"{crop.displayName} seed cost ${price}. Allu short, nuh. Come back when you have it.";
                }
                economy.AddMoney(-price);
            }

            economy.AddSeeds(crop.cropId, 3);
            return price > 0 ? $"{successLine} That cost you ${price}." : successLine;
        }

        /// <summary>Legacy single-crop StrainBoss path - kept for backward
        /// compatibility; every StrainBoss built by MINI-055 onward uses
        /// HandleMultiStrainBoss instead.</summary>
        private string HandleSingleStrainBoss()
        {
            var prog = UpIzUpMini.Progression.ProgressionManager.Instance;
            bool unlocked = bossSeedCrop != null && prog != null && prog.IsCropUnlocked(bossSeedCrop.cropId);
            if (!unlocked) return "You not ready for this strain yet. Build your name first, nuh.";
            if (EconomyManager.Instance != null && EconomyManager.Instance.GetSeeds(bossSeedCrop.cropId) <= 0)
            {
                return TryBuySeed(bossSeedCrop, seedPrice, $"Take three {bossSeedCrop.displayName} seed. This work carry more risk.");
            }
            return $"Bring back the {bossSeedCrop?.displayName ?? "crop"} when it ready.";
        }

        /// <summary>
        /// MINI-055: one NPC (Boss C) offering several strains as they
        /// unlock, in authoring order. Offers the first unlocked strain the
        /// player doesn't already hold seed for; if every currently-
        /// unlocked strain is already seeded/growing, sends them back to
        /// harvest; if none are unlocked yet, the standard "not ready" line.
        /// </summary>
        private string HandleMultiStrainBoss()
        {
            var prog = UpIzUpMini.Progression.ProgressionManager.Instance;
            var economy = EconomyManager.Instance;
            bool anyUnlocked = false;

            for (int i = 0; i < multiBossCrops.Length; i++)
            {
                var crop = multiBossCrops[i];
                if (crop == null) continue;

                bool unlocked = prog != null && prog.IsCropUnlocked(crop.cropId);
                if (!unlocked) continue;
                anyUnlocked = true;

                if (economy != null && economy.GetSeeds(crop.cropId) <= 0)
                {
                    int price = (multiBossSeedPrices != null && i < multiBossSeedPrices.Length) ? multiBossSeedPrices[i] : 0;
                    return TryBuySeed(crop, price, $"Take three {crop.displayName} seed. This work carry more risk.");
                }
            }

            return anyUnlocked
                ? "Bring back what you growing when it ready."
                : "You not ready for this strain yet. Build your name first, nuh.";
        }

        /// <summary>
        /// MINI-057: self-interested assistance - pay Normy to cool your
        /// heat. Not representative of real police (PoliceOfficer/
        /// PatrolNPC never take bribes) - a deliberately separate,
        /// crooked mechanic. Cost falls slightly as NormyReputation grows
        /// from repeat business, floored at normyMinBribeCost so it's
        /// never free. Rate-limited so it can't be spammed into a
        /// zero-heat safety net every frame. When there's no heat to sell
        /// him a favour for, falls back to dialogueSet (MINI-053) for
        /// "information" flavour instead - a natural place for
        /// reputation/heat-conditional insider lines.
        /// </summary>
        /// <summary>MINI-110: Normy's item favour ("Small Ting") - checked
        /// FIRST, before the M5B mission-payment branch and the ambient
        /// service, since it's a distinct earlier obligation. Returns null
        /// (not handled) if the current objective isn't a DeliverItem one,
        /// so HandleNormy can fall through to its other branches.</summary>
        private string HandleNormyFavour()
        {
            // DeliverItem's targetId is the ITEM id, not an NPC id - this
            // is only ever invoked from Normy's own role branch below, so
            // any active DeliverItem objective is understood to be for him.
            var mission = Missions.MissionSystem.Instance;
            var obj = mission?.CurrentObjective;
            if (obj == null || obj.kind != Missions.ObjectiveKind.DeliverItem) return null;

            var economy = EconomyManager.Instance;
            if (economy == null) return "Not now, mn.";

            int need = Mathf.Max(1, obj.requiredCount);
            if (!economy.TrySpendConsumable(obj.targetId, need, out string shortMsg))
            {
                return shortMsg;
            }

            mission.NotifyCount(Missions.ObjectiveKind.DeliverItem, obj.targetId, need);
            return "Preciate dat, yeah. Dat go help.";
        }

        private string HandleNormy()
        {
            var favourResponse = HandleNormyFavour();
            if (favourResponse != null) return favourResponse;

            var economy = EconomyManager.Instance;
            var prog = UpIzUpMini.Progression.ProgressionManager.Instance;

            if (economy == null) return "Not now, mn.";

            // MINI-109: root cause of "Clean Face can remain stuck at
            // Normy" - the mission's BribeNormy objective previously had
            // no path except the ambient service below it, which refuses
            // to do anything once economy.Heat <= 0.5f. M5A (the mission
            // immediately before this one) is "go rest at the safehouse
            // and cool down" - so the player reaches Normy for M5B with
            // heat already at or near zero, the exact state the ambient
            // gate treats as "nothing to bribe for," and the mission could
            // never complete. This is a separate, one-time payment: it
            // does not touch the ambient cooldown/heat gate at all, and it
            // is naturally idempotent - once Notify() advances the
            // mission past this objective, IsCurrentObjective stops
            // matching and this branch stops firing on its own.
            bool missionPending = Missions.MissionSystem.Instance != null
                && Missions.MissionSystem.Instance.IsCurrentObjective(Missions.ObjectiveKind.BribeNormy, "Normy");

            if (missionPending)
            {
                if (economy.Money < normyMissionPaymentCost)
                {
                    return $"Dat go cost you ${normyMissionPaymentCost}. Allu short, nuh - come back when you have it.";
                }

                economy.AddMoney(-normyMissionPaymentCost);
                economy.AddHeat(-normyHeatReduction);
                prog?.AddReputation(UpIzUpMini.Progression.Faction.Normy, 5);
                Missions.MissionSystem.Instance.Notify(Missions.ObjectiveKind.BribeNormy, "Normy");
                return $"Say no more. Dat cost you ${normyMissionPaymentCost}. We square.";
            }

            if (economy.Heat <= 0.5f)
            {
                var line = dialogueSet != null ? dialogueSet.SelectLine() : null;
                return line != null ? line.text : "Nothing for me to look away from right now.";
            }

            if (Time.time < _normyReadyAt)
            {
                int wait = Mathf.CeilToInt(_normyReadyAt - Time.time);
                return $"Give it a minute, nuh - can't keep looking away every five seconds. ({wait}s)";
            }

            int reputation = prog != null ? prog.GetReputation(UpIzUpMini.Progression.Faction.Normy) : 0;
            int cost = Mathf.Max(normyMinBribeCost, normyBaseBribeCost - reputation);

            if (economy.Money < cost)
            {
                return $"Dat go cost you ${cost}. Allu short, nuh - come back when you have it.";
            }

            economy.AddMoney(-cost);
            economy.AddHeat(-normyHeatReduction);
            prog?.AddReputation(UpIzUpMini.Progression.Faction.Normy, 5);
            _normyReadyAt = Time.time + normyBribeCooldownSeconds;
            Missions.MissionSystem.Instance?.Notify(Missions.ObjectiveKind.BribeNormy, "Normy");

            return $"Say no more. I cool it by 20%. Dat cost you ${cost}.";
        }

        /// <summary>
        /// MINI-058: recruits the next un-recruited member of the pooled
        /// Not Ah Word roster, capped at recruitPool.Length (up to four,
        /// per the brief) by construction - there's simply nothing left to
        /// recruit once every pool slot is active. New recruits start
        /// Following whichever boy is currently active.
        /// </summary>
        private string HandleRecruit()
        {
            var economy = EconomyManager.Instance;
            if (economy == null || recruitPool == null) return "Not now, mn.";

            GangMemberController free = null;
            foreach (var member in recruitPool)
            {
                if (member != null && !member.IsRecruited) { free = member; break; }
            }

            if (free == null)
            {
                return "You already have a full team riding with you, nuh.";
            }

            if (economy.Money < recruitCost)
            {
                return $"Dat go cost you ${recruitCost} to ride with us. Allu short, nuh.";
            }

            var activeRoot = Character.CharacterSwitchManager.Instance?.Active?.root;
            economy.AddMoney(-recruitCost);
            free.transform.position = transform.position + transform.forward * 1.5f;
            free.gameObject.SetActive(true);
            free.Recruit(activeRoot != null ? activeRoot.transform : transform);

            return $"{free.MemberName} in now. Not Ah Word getting bigger, seen. That cost you ${recruitCost}.";
        }

        /// <summary>
        /// MINI-060 follow-up: one boat man now does everything - "his job
        /// is just to provide transportation to Guadeloupe," per the user,
        /// covering the produce trade (unchanged, GuadeloupeTrade.cs is
        /// untouched) AND what used to be a separate Gardey Zafeh NPC's
        /// reveal/readings. Priority per interact: (1) the one-time Dog
        /// Life reveal/readings, letting the player choose which trip to
        /// send him on rather than the game silently picking. E is the
        /// produce trade (unchanged, GuadeloupeTrade.cs untouched); R is
        /// GardeyZafeh (see InteractGardeyZafeh/GardeyZafehLabel below,
        /// wired the same dual-prompt way FarmPlot's [E] Harvest/[R] Clone
        /// already works). Either choice sends him "away" for
        /// boatManAwaySeconds - "his job is just to provide transportation,"
        /// so both trips cost the same travel time.
        /// </summary>
        private string HandleBoatMan()
        {
            bool tripAlreadyActive = GuadeloupeTrade.Instance != null && GuadeloupeTrade.Instance.TripActive;
            string tradeResult = GuadeloupeTrade.Instance != null ? GuadeloupeTrade.Instance.Interact() : "Boat not running today.";
            bool tripStartedNow = GuadeloupeTrade.Instance != null && GuadeloupeTrade.Instance.TripActive && !tripAlreadyActive;
            if (tripStartedNow) StartCoroutine(DisappearForTrip());

            // MINI-119: the "nothing to load" line is by far the most
            // common repeat interaction with him (every walk-up before the
            // player is actually carrying produce) - swap in a rotating
            // line instead of always the exact same sentence.
            if (tradeResult == "Bring produce and I go carry it across, yah wii. Nothing to load right now.")
                return NextLine(boatManIdleLines, tradeResult);

            return tradeResult;
        }

        /// <summary>Secondary prompt/action on the Boat Man only - "[ R ]
        /// ..." asking about/consulting Gardey Zafeh, kept as an explicit
        /// second choice (not an automatic fallback) per the user's "make
        /// a dialogue to choose which option you want."</summary>
        public string GardeyZafehLabel => role == NpcRole.BoatMan
            && Progression.ProgressionManager.Instance != null
            && Progression.ProgressionManager.Instance.BlackSugarUnlocked
            ? (Progression.ProgressionManager.Instance.DogLifeRevealed
                ? "[ R ] Ask Gardey Zafeh for a reading"
                : $"[ R ] Ask about Gardey Zafeh (${gardeyZafehRevealCost})")
            : null;

        /// <summary>Called by InteractionDetector's R key, same pattern as
        /// FarmPlot.Clone(). Returns null if this NPC isn't the Boat Man,
        /// so InteractionDetector can no-op cleanly for every other role.</summary>
        public string InteractGardeyZafeh()
        {
            if (role != NpcRole.BoatMan) return null;

            var progression = Progression.ProgressionManager.Instance;
            var economy = EconomyManager.Instance;
            if (progression == null || economy == null) { _lastFeedback = "Not now, mn."; return _lastFeedback; }
            if (!progression.BlackSugarUnlocked) return null;

            if (_pendingDogLifeReveal)
            {
                // MINI-081, user: "I want the information after the person
                // comes back" - he's already been paid and sent off; while
                // he's away there is nothing new to say yet.
                _lastFeedback = "Mi still out dere finding out, chile. Give mi time to come back.";
                return _lastFeedback;
            }

            if (!progression.DogLifeRevealed)
            {
                if (economy.Money < gardeyZafehRevealCost)
                {
                    _lastFeedback = $"Dat information cost you ${gardeyZafehRevealCost}. Come back when you have it.";
                    return _lastFeedback;
                }
                economy.AddMoney(-gardeyZafehRevealCost);
                // MINI-081: the reveal itself (RevealDogLife + the actual
                // line) is now withheld until he actually returns from the
                // trip - see DisappearForTrip/Update below - instead of
                // handing over the information in the same breath as
                // sending him away, which read like he never really left.
                _pendingDogLifeReveal = true;
                StartCoroutine(DisappearForTrip());
                _lastFeedback = $"Awright, mi gone find out for you, chile. That cost you ${gardeyZafehRevealCost} - come back when I return.";
                return _lastFeedback;
            }

            if (GardeyZafehBuffState.AnyActive)
            {
                int wait = Mathf.CeilToInt(GardeyZafehBuffState.SecondsRemaining);
                _lastFeedback = $"Yuh still under mi last reading, chile - {wait}s left on it.";
                return _lastFeedback;
            }

            var buff = (GardeyZafehBuff)Random.Range(1, 5); // skip None (0)
            GardeyZafehBuffState.Grant(buff, gardeyZafehBuffDurationSeconds);
            string readingLine = buff switch
            {
                GardeyZafehBuff.EnergyBoost => "I see energy in yuh, chile - yuh stamina staying full for a while now.",
                GardeyZafehBuff.HealthBoost => "Mi see strength in yuh body - nothing can hurt you for a while now.",
                GardeyZafehBuff.PoliceImmunity => "Di law blind to allu for a while now - walk easy.",
                GardeyZafehBuff.DoubleMoney => "Mi see money doubling in yuh hand for a while now.",
                _ => "..."
            };
            StartCoroutine(DisappearForTrip());
            _lastFeedback = $"{readingLine} {gardeyZafehJalousieLine}";
            return _lastFeedback;
        }

        /// <summary>
        /// "He will disappear until the time is up" - his Visual model
        /// hides (and CanInteract() refuses him) for boatManAwaySeconds.
        /// Delays the actual hide briefly so InteractionDetector's feedback
        /// box (which clears itself the moment an in-range interactable
        /// goes missing) doesn't hide the line the player just triggered
        /// before they can read it. See the _away/_awayUntil fields' note
        /// above for why this no longer SetActive(false)s the root.
        /// </summary>
        private System.Collections.IEnumerator DisappearForTrip()
        {
            yield return new WaitForSeconds(4f);
            if (_visual == null) _visual = transform.Find("Visual");
            if (_visual != null) _visual.gameObject.SetActive(false);
            _away = true;
            _awayUntil = Time.time + Mathf.Max(0f, boatManAwaySeconds - 4f);
        }

        /// <summary>MINI-060 follow-up-2 cheat code support: called by the
        /// "0" x6 cheat to fast-forward the boat man back from his trip.</summary>
        public void SkipTravelTime(float seconds)
        {
            if (_away) _awayUntil -= seconds;
        }

        public void EnsurePresentForMission()
        {
            if (role != NpcRole.BoatMan) return;
            _away = false;
            _awayUntil = 0f;
            if (_visual == null) _visual = transform.Find("Visual");
            if (_visual != null) _visual.gameObject.SetActive(true);
            CompletePendingDogLifeReveal();
        }

        public override bool CanInteract(GameObject interactor)
        {
            if (_away) return false;
            return base.CanInteract(interactor);
        }

        private void Update()
        {
            if (_away && Time.time >= _awayUntil)
            {
                _away = false;
                if (_visual == null) _visual = transform.Find("Visual");
                if (_visual != null) _visual.gameObject.SetActive(true);
                CompletePendingDogLifeReveal();
            }
        }

        private void CompletePendingDogLifeReveal()
        {
            if (!_pendingDogLifeReveal) return;
            _pendingDogLifeReveal = false;
            Progression.ProgressionManager.Instance?.RevealDogLife();
            string revealLine = "Is Dog Life volehing yuh Zeb, chile - dey watching yuh plantation when you gone. Guard it, or send me back to Gwada for protection sometime.";
            _lastFeedback = revealLine;
            Missions.MissionSystem.Instance?.Alert($"GARDEY ZAFEH\n{revealLine}");
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

        /// <summary>MINI-109: whether the player holds ANY illegal crop
        /// this NPC's own sellableCrops list would actually accept -
        /// mirrors TrySellCrops' real sell criteria (illegal + count > 0),
        /// rather than a single hardcoded crop ID.</summary>
        private bool HasAnySellableIllegalStock()
        {
            if (EconomyManager.Instance == null || sellableCrops == null) return false;
            foreach (var c in sellableCrops)
            {
                if (c != null && c.isIllegal && EconomyManager.Instance.GetCount(c.cropId) > 0) return true;
            }
            return false;
        }

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

                    // MINI-081, user: "police intervening on some drug
                    // missions." A mission delivery already spikes heat
                    // above the ambient patrol chase threshold (45) - this
                    // adds a further chance, on a MISSION sale specifically,
                    // of a harder spike straight past the reinforcement
                    // threshold (55) so it reads as an actual intervention,
                    // not just a nearby cop noticing - "some," not every
                    // time, so it can't be relied on or predicted.
                    if (missionSale && Random.value < drugMissionPoliceInterventionChance)
                    {
                        EconomyManager.Instance.AddHeat(drugMissionPoliceInterventionExtraHeat);
                        Missions.MissionSystem.Instance?.Alert(
                            "POLICE!\nSomebody talk - dey watching dis handoff. Move!");
                    }
                }
                else progression?.AddReputation(UpIzUpMini.Progression.Faction.Farmers, 4);

                if (bossSale) progression?.RecordBossJob();
                // MINI-055: display-only "Boss J" (see PromptLabel note) -
                // buyerId/targetId matching still uses the internal "BossK"
                // identifier, unaffected by this text.
                string productionPraise = new[]
                {
                    "Boii, you know how to plant dat.",
                    "Yah, dat is some good stuff.",
                    "Boii, you getting some nice buds.",
                    "Yah my boi, I like that production. Keep on doing your ting."
                }[Random.Range(0, 4)];
                _lastFeedback = bossSale && earned <= 0
                    ? "Boss J: Next time I'll give allu more.\nFranki: Always a next time."
                    : bossSale
                        ? $"Boss J: {productionPraise} ${earned} for that. You can sell small amounts to the Paro in Lalay too, but he paying less."
                        : buyerId == "Vagrant"
                            ? $"Paro: Respect, boss. I scrape up ${earned}. Doh bring police by me, nuh."
                            : $"Yea mn, sold for ${earned}.";
                Missions.MissionSystem.Instance?.Notify(Missions.ObjectiveKind.SellCrop, buyerId);
            }
            else _lastFeedback = buyerId == "Vagrant"
                ? "Paro: I doh have money for tomato and them thing. If is a little weed, we could reason."
                : NextLine(buyerLines, "Nothing for me right now, nuh.");
        }
    }
}
