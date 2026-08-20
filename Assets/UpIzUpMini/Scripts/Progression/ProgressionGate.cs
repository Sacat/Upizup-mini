using UpIzUpMini.Economy;
using UpIzUpMini.Missions;

namespace UpIzUpMini.Progression
{
    /// <summary>
    /// One source of truth for content that must stay hidden until the story
    /// reaches it. UI and interactables both call this class so an item cannot
    /// be hidden in a menu but still bought through another code path.
    /// </summary>
    public static class ProgressionGate
    {
        public static bool IsMissionReached(string missionId) =>
            MissionSystem.Instance != null && MissionSystem.Instance.HasReachedMission(missionId);

        public static bool IsItemUnlocked(ShopItemDefinition item)
        {
            if (item == null) return false;

            return item.itemId switch
            {
                "cap_mike" or "shirt_lacos" or "shorts_adibas" or
                "shoes_mike" or "shoes_pumba" or "shades_ray" or
                "watch_rollie" or "chain_gold" => IsMissionReached("M2"),
                "land_montine" => IsMissionReached("M3"),
                "land_hillside" => ProgressionManager.Instance != null
                    && ProgressionManager.Instance.Path == CareerPath.LegitimateFarmer
                    && IsMissionReached("M9L"),
                "prop_safehouse" => IsMissionReached("M12"),
                "tmax_560" => IsMissionReached("M17"),
                "prop_lalay_estate" => IsMissionReached("M18"),
                "range_rova" => MissionSystem.Instance != null && MissionSystem.Instance.HasCompletedMission("M18"),
                _ => true,
            };
        }

        public static bool CanUseBossJ => IsMissionReached("M4");
        public static bool CanUseParo => IsMissionReached("M7");
        public static bool CanUseBossC => ProgressionManager.Instance != null
            && ProgressionManager.Instance.Path == CareerPath.WeedRoute
            && IsMissionReached("M10W");
        public static bool CanUseBoat => IsMissionReached("M14")
            || (ProgressionManager.Instance != null
                && ProgressionManager.Instance.Path == CareerPath.WeedRoute
                && MissionSystem.Instance != null
                && MissionSystem.Instance.CurrentMissionId == "M11W");
        public static bool CanUseBlackMarket => IsMissionReached("M12");
        public static bool CanUseNormy => IsMissionReached("M12");
        public static bool CanRecruit
        {
            get
            {
                int rep = ProgressionManager.Instance != null
                    ? ProgressionManager.Instance.GangReputation
                    : 0;
                return IsMissionReached("M15") && rep >= 20;
            }
        }
    }
}
