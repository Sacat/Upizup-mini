using System;
using UnityEngine;

namespace UpIzUpMini.Progression
{
    public enum CareerPath { Undecided, LegitimateFarmer, WeedRoute }
    public enum Faction { BossK, Farmers, Police, GrandBayGangs }

    public class ProgressionManager : MonoBehaviour
    {
        public static ProgressionManager Instance { get; private set; }
        public CareerPath Path { get; private set; }
        public int BossKReputation { get; private set; }
        public int FarmerReputation { get; private set; }
        public int PoliceReputation { get; private set; }
        public int GangReputation { get; private set; }
        public int BossExploitationStage { get; private set; }
        // MINI-043: the Guadeloupe run starts as an NPC-courier dispatch;
        // sending one of the two playable boys yourself unlocks after the
        // first NPC run completes. See GuadeloupeTrade.
        public bool GuadeloupeCharacterCourierUnlocked { get; private set; }
        // MINI-056: once the player sends someone to Gwada to a Gardey
        // Zafeh (seer), they learn Dog Life was stealing their zeb. Only
        // after this reveal can gang rivalry begin (sparkable fights/wars).
        public bool DogLifeRivalryRevealed { get; private set; }
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
        public event Action OnChanged;

        void Awake() => Instance = this;
        public void ChoosePath(CareerPath path) { if (Path == CareerPath.Undecided) Path = path; OnChanged?.Invoke(); }
        public void AddReputation(Faction faction, int amount)
        {
            switch (faction) {
                case Faction.BossK: BossKReputation = Mathf.Clamp(BossKReputation + amount, -100, 100); break;
                case Faction.Farmers: FarmerReputation = Mathf.Clamp(FarmerReputation + amount, -100, 100); break;
                case Faction.Police: PoliceReputation = Mathf.Clamp(PoliceReputation + amount, -100, 100); break;
                case Faction.GrandBayGangs: GangReputation = Mathf.Clamp(GangReputation + amount, -100, 100); break;
            }
            OnChanged?.Invoke();
        }
        public void RecordBossJob() { BossExploitationStage++; AddReputation(Faction.BossK, 10); AddReputation(Faction.Police, -8); AddReputation(Faction.GrandBayGangs, 5); }
        public void UnlockGuadeloupeCharacterCourier() { if (GuadeloupeCharacterCourierUnlocked) return; GuadeloupeCharacterCourierUnlocked = true; OnChanged?.Invoke(); }

        /// <summary>
        /// MINI-056. Called when the player sends someone to Gwada to a
        /// Gardey Zafeh (seer) - they learn Dog Life was stealing their zeb.
        /// This is the story reveal that unlocks gang rivalry (sparkable
        /// fights/wars). Idempotent.
        /// </summary>
        public void RevealDogLifeRivalry()
        {
            if (DogLifeRivalryRevealed) return;
            DogLifeRivalryRevealed = true;
            OnChanged?.Invoke();
        }

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
            // Exactly 5, not higher - that's the deepest threshold any
            // unlock above actually checks (PurpleCheeseUnlocked). Note
            // BossPayoutMultiplier is already 0 for any stage >= 3 by
            // design (Boss K's cut gets worse the more exploited jobs
            // you've done) - unlocking every strain and keeping a live
            // Boss K payout are mutually exclusive in the existing
            // progression model, not something this cheat can dodge.
            BossExploitationStage = 5;
            GuadeloupeCharacterCourierUnlocked = true;
            OnChanged?.Invoke();
        }
        public bool IsCropUnlocked(string id)
        {
            if (id == "black_sugar") return BlackSugarUnlocked;
            if (id == "purple") return PurpleUnlocked;
            if (id == "purple_black") return PurpleBlackUnlocked;
            if (id == "blue_cheese") return BlueCheeseUnlocked;
            if (id == "sugar_cheese") return SugarCheeseUnlocked;
            if (id == "purple_cheese") return PurpleCheeseUnlocked;
            return true;
        }
    }
}
