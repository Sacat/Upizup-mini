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
        public bool GrandBayWeedRouteEstablished => BossKReputation >= 20 && GangReputation >= 10;
        public bool BlackSugarUnlocked => BossKReputation >= 20 && BossExploitationStage >= 2;
        public bool PurpleUnlocked => GangReputation >= 25 && BossExploitationStage >= 3;
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
        public bool IsCropUnlocked(string id)
        {
            if (id == "black_sugar") return BlackSugarUnlocked;
            if (id == "purple") return PurpleUnlocked;
            return true;
        }
    }
}
