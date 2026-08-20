using UnityEngine;
using UpIzUpMini.Character;
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

        [TextArea(1, 3)]
        [SerializeField] private string policeCalmLine = "Morning. Everything alright over dey?";
        [TextArea(1, 3)]
        [SerializeField] private string policeSuspiciousLine = "I watching allu close, nuh. Doe try nothing.";

        // MINI-057: Normy - self-interested assistance (a bribe that cools
        // heat) plus a relationship that makes repeat business cheaper.
        [Tooltip("Base bribe cost - reduced slightly as NormyReputation grows (repeat business).")]
        [SerializeField] private int normyBaseBribeCost = 80;
        [SerializeField] private int normyMinBribeCost = 30;
        [SerializeField] private float normyHeatReduction = 35f;
        [SerializeField] private float normyBribeCooldownSeconds = 45f;
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
            NpcRole.Vagrant => "[ E ] Sell weed quietly",
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
                    // MINI-082: pass this NPC's own transform so the shop
                    // panel can fade itself shut when the player walks away.
                    shop?.Open(transform);
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
                    if (bossSeedCrop != null && EconomyManager.Instance != null
                        && EconomyManager.Instance.GetSeeds(bossSeedCrop.cropId) <= 0)
                    {
                        _lastFeedback = TryBuySeed(bossSeedCrop, seedPrice, bossOfferLine);
                    }
                    else
                    {
                        int weedHeld = EconomyManager.Instance.GetCount(bossSeedCrop.cropId);
                        if (weedHeld > 0) SellCrops(true, false, 1.35f, "BossK");
                        else _lastFeedback = bossFollowUpLine;
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
                    var line = dialogueSet != null ? dialogueSet.SelectLine() : null;
                    _lastFeedback = line != null ? line.text : NextLine(villagerLines, "Yea wii.");
                    break;
            }

            Missions.MissionSystem.Instance?.Notify(Missions.ObjectiveKind.TalkTo, npcName);
        }

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
        private string HandleNormy()
        {
            var economy = EconomyManager.Instance;
            var prog = UpIzUpMini.Progression.ProgressionManager.Instance;

            if (economy == null) return "Not now, mn.";

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

            return $"Say no more. Consider it forgotten - dat cost you ${cost}.";
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
            return tradeResult;
        }

        /// <summary>Secondary prompt/action on the Boat Man only - "[ R ]
        /// ..." asking about/consulting Gardey Zafeh, kept as an explicit
        /// second choice (not an automatic fallback) per the user's "make
        /// a dialogue to choose which option you want."</summary>
        public string GardeyZafehLabel => role == NpcRole.BoatMan
            ? (Progression.ProgressionManager.Instance != null && Progression.ProgressionManager.Instance.DogLifeRevealed
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

                // MINI-081: deliver the Dog Life reveal now that he is
                // actually back, rather than back when he was paid.
                if (_pendingDogLifeReveal)
                {
                    _pendingDogLifeReveal = false;
                    Progression.ProgressionManager.Instance?.RevealDogLife();
                    string revealLine = $"Is Dog Life volehing yuh Zeb, chile - dey watching yuh plantation when you gone. Guard it, or send me back to Gwada for protection sometime.";
                    _lastFeedback = revealLine;
                    Missions.MissionSystem.Instance?.Alert($"GARDEY ZAFEH\n{revealLine}");
                }
            }
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
                _lastFeedback = bossSale && earned <= 0
                    ? "Boss J: Money tight. I holding your payment this time. Do the next job and we settle, nuh."
                    : $"Yea mn, sold for ${earned}.";
                Missions.MissionSystem.Instance?.Notify(Missions.ObjectiveKind.SellCrop, buyerId);
            }
            else _lastFeedback = NextLine(buyerLines, "Nothing for me right now, nuh.");
        }
    }
}
