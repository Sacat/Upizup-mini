using System;
using UnityEngine;

namespace UpIzUpMini.Progression
{
    public enum CareerPath { Undecided, LegitimateFarmer, WeedRoute }
    // MINI-057: Normy is his own faction, deliberately separate from
    // Police - he's a crooked individual, not representative of the
    // force, and bribing him should not itself move real Police standing.
    public enum Faction { BossK, Farmers, Police, GrandBayGangs, Normy }

    public class ProgressionManager : MonoBehaviour
    {
        public static ProgressionManager Instance { get; private set; }
        public CareerPath Path { get; private set; }
        public int BossKReputation { get; private set; }
        public int FarmerReputation { get; private set; }
        public int PoliceReputation { get; private set; }
        public int GangReputation { get; private set; }
        public int NormyReputation { get; private set; }
        public int BossExploitationStage { get; private set; }
        // MINI-043: the Guadeloupe run starts as an NPC-courier dispatch;
        // sending one of the two playable boys yourself unlocks after the
        // first NPC run completes. See GuadeloupeTrade.
        public bool GuadeloupeCharacterCourierUnlocked { get; private set; }
        // MINI-060: set once Gardey Zafeh reveals Dog Life as the real
        // source of the plantation theft (MINI-059) - "only after the
        // reveal should Dog Life rivalry become openly active." Read by
        // PlantationTheftController (names Dog Life in the notification
        // from then on) and Dog Life's own DialogueSet (a new, openly
        // hostile line becomes eligible).
        public bool DogLifeRevealed { get; private set; }
        public bool GrandBayWeedRouteEstablished => BossKReputation >= 20 && GangReputation >= 10;
        public bool BlackSugarUnlocked => BossKReputation >= 20 && BossExploitationStage >= 2;
        public bool PurpleUnlocked => GangReputation >= 25 && BossExploitationStage >= 3;
        // MINI-047: further out than Purple alone - "a skill unlocked at a
        // later mission" per the user's ask, requiring more of both boss
        // exploitation depth and gang standing than either parent strain.
        public bool PurpleBlackUnlocked => GangReputation >= 40 && BossExploitationStage >= 4;

        // MINI-048: Blue Cheese is a new base strain sitting alongside
        // Purple (same exploitation-stage tier, a bit more gang standing).
        // Its two hybrids escalate further still, in the order the user
        // asked for them - Sugar Cheese first, Purple Cheese deepest.
        public bool BlueCheeseUnlocked => GangReputation >= 30 && BossExploitationStage >= 3;
        public bool SugarCheeseUnlocked => BossKReputation >= 30 && GangReputation >= 30 && BossExploitationStage >= 4;
        public bool PurpleCheeseUnlocked => GangReputation >= 50 && BossExploitationStage >= 5;
        public float BossPayoutMultiplier => BossExploitationStage switch { 0 => 1f, 1 => .75f, 2 => .4f, _ => 0f };

        // MINI-111: Rasta's own strain-mentorship ladder - an ALTERNATE
        // unlock path alongside the existing Boss-exploitation-stage
        // formulas above, not a replacement. Legitimate-farming-path
        // players never see M9W-M11W (those are WeedRoute-only), so
        // without this, they had no route to any strain past the basics -
        // Rasta's missions are that route for everyone, regardless of path.
        public bool RastaTaughtBlackSugar { get; private set; }
        public bool RastaTaughtPurple { get; private set; }
        public bool RastaTaughtBlueCheese { get; private set; }
        public bool RastaTaughtPurpleBlack { get; private set; }
        public bool RastaTaughtSugarCheese { get; private set; }
        public bool RastaTaughtPurpleCheese { get; private set; }

        /// <summary>Applied automatically when one of Rasta's teaching
        /// missions becomes current (see Mission.unlocksCropId) - not
        /// called directly by NPC code, matching "use data definitions
        /// rather than duplicating crop-specific logic."</summary>
        public void MarkRastaTaught(string cropId)
        {
            switch (cropId)
            {
                case "black_sugar": RastaTaughtBlackSugar = true; break;
                case "purple": RastaTaughtPurple = true; break;
                case "blue_cheese": RastaTaughtBlueCheese = true; break;
                case "purple_black": RastaTaughtPurpleBlack = true; break;
                case "sugar_cheese": RastaTaughtSugarCheese = true; break;
                case "purple_cheese": RastaTaughtPurpleCheese = true; break;
                default: return; // bushers/tomato/etc. need no flag - already unlocked
            }
            OnChanged?.Invoke();
        }

        public event Action OnChanged;

        void Awake() => Instance = this;
        public void ChoosePath(CareerPath path) { if (Path == CareerPath.Undecided) Path = path; OnChanged?.Invoke(); }
        public void CommitToWeedRoute() { Path = CareerPath.WeedRoute; OnChanged?.Invoke(); }
        public void RevealDogLife() { if (DogLifeRevealed) return; DogLifeRevealed = true; OnChanged?.Invoke(); }
        public void AddReputation(Faction faction, int amount)
        {
            switch (faction) {
                case Faction.BossK: BossKReputation = Mathf.Clamp(BossKReputation + amount, -100, 100); break;
                case Faction.Farmers: FarmerReputation = Mathf.Clamp(FarmerReputation + amount, -100, 100); break;
                case Faction.Police: PoliceReputation = Mathf.Clamp(PoliceReputation + amount, -100, 100); break;
                case Faction.GrandBayGangs: GangReputation = Mathf.Clamp(GangReputation + amount, -100, 100); break;
                case Faction.Normy: NormyReputation = Mathf.Clamp(NormyReputation + amount, -100, 100); break;
            }
            OnChanged?.Invoke();
        }

        /// <summary>MINI-053: read-only mirror of AddReputation's per-field
        /// switch, so the new dialogue-condition system (and anything else
        /// that just needs to read a standing, not change it) doesn't need
        /// its own copy of the faction->field mapping.</summary>
        public int GetReputation(Faction faction) => faction switch
        {
            Faction.BossK => BossKReputation,
            Faction.Farmers => FarmerReputation,
            Faction.Police => PoliceReputation,
            Faction.GrandBayGangs => GangReputation,
            Faction.Normy => NormyReputation,
            _ => 0
        };
        public void RecordBossJob() { BossExploitationStage++; AddReputation(Faction.BossK, 10); AddReputation(Faction.Police, -8); AddReputation(Faction.GrandBayGangs, 5); }
        public void UnlockGuadeloupeCharacterCourier() { if (GuadeloupeCharacterCourierUnlocked) return; GuadeloupeCharacterCourierUnlocked = true; OnChanged?.Invoke(); }

        /// <summary>
        /// MINI-049 cheat code. Directly sets every backing field high
        /// enough to satisfy every unlock condition above, rather than
        /// simulating the many boss jobs/sales it would normally take -
        /// RecordBossJob's side effects (Police reputation dropping each
        /// call) aren't something a cheat should have to replay N times.
        /// Also picks a career path if none is chosen yet, since farm
        /// assignment (CompanionInteractable) gates on that separately
        /// from any of the reputation numbers here.
        /// </summary>
        public void UnlockEverything()
        {
            if (Path == CareerPath.Undecided) Path = CareerPath.WeedRoute;
            BossKReputation = 100;
            FarmerReputation = 100;
            PoliceReputation = 100;
            GangReputation = 100;
            // MINI-119, user: "the cheat 000000 gives you 100 rep as well
            // with all the others" - NormyReputation was the one faction
            // this cheat forgot.
            NormyReputation = 100;
            // Exactly 5, not higher - that's the deepest threshold any
            // unlock above actually checks (PurpleCheeseUnlocked). Note
            // BossPayoutMultiplier is already 0 for any stage >= 3 by
            // design (Boss K's cut gets worse the more exploited jobs
            // you've done) - unlocking every strain and keeping a live
            // Boss K payout are mutually exclusive in the existing
            // progression model, not something this cheat can dodge.
            BossExploitationStage = 5;
            GuadeloupeCharacterCourierUnlocked = true;
            // MINI-111: keep the cheat's own promise ("unlock everything")
            // true for Rasta's alternate path too, not just the Boss-stage
            // formulas above.
            RastaTaughtBlackSugar = RastaTaughtPurple = RastaTaughtBlueCheese
                = RastaTaughtPurpleBlack = RastaTaughtSugarCheese = RastaTaughtPurpleCheese = true;
            OnChanged?.Invoke();
        }
        public bool IsCropUnlocked(string id)
        {
            if (id == "black_sugar") return BlackSugarUnlocked || RastaTaughtBlackSugar;
            if (id == "purple") return PurpleUnlocked || RastaTaughtPurple;
            if (id == "purple_black") return PurpleBlackUnlocked || RastaTaughtPurpleBlack;
            if (id == "blue_cheese") return BlueCheeseUnlocked || RastaTaughtBlueCheese;
            if (id == "sugar_cheese") return SugarCheeseUnlocked || RastaTaughtSugarCheese;
            if (id == "purple_cheese") return PurpleCheeseUnlocked || RastaTaughtPurpleCheese;
            return true;
        }
    }
}
