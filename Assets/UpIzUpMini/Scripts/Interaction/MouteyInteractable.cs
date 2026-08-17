using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;
using UpIzUpMini.Progression;

namespace UpIzUpMini.Interaction
{
    /// <summary>
    /// MINI-059. Moutey (granny) - after you buy your chain, go to granny to
    /// get stronger against the police and evade faster. That's a mission:
    /// she blesses the chain, which permanently boosts both protagonists'
    /// run/evade (stamina regen + a police-evasion speed bonus) once unlocked.
    ///
    /// Pattern: buying a chain (chain_gold) satisfies the "claim your chain"
    /// step; Moutey checks you own it and, on her mission, applies a global,
    /// persistent evade boost (registered on both boys) and grants rep. The
    /// boost is a GameData flag so it persists past respawn/switch.
    /// </summary>
    public class MouteyInteractable : InteractableBase
    {
        [SerializeField] private string requiredChainItemId = "chain_gold";
        // The persistent, game-wide "granny blessed your chain" flag - read
        // by evasion/police code to apply the boost. Lives on ProgressionManager
        // so it survives death/respawn and character switches.
        private const string BoostKey = "moutey_chain_blessed";

        private string _lastFeedback;

        public override string PromptLabel => "[ E ] Talk to Moutey";

        public override void Interact(GameObject interactor)
        {
            var economy = EconomyManager.Instance;
            if (economy == null)
            {
                _lastFeedback = "Moutey out back by de yard.";
                return;
            }

            bool ownsChain = economy.OwnsItem(requiredChainItemId);
            var prog = ProgressionManager.Instance;
            bool blessed = prog != null && prog.GetChainBlessed();

            if (!ownsChain)
            {
                _lastFeedback =
                    "Granny Moutey look at allu. \"Boi, you doe have no chain yet. Go buy yuhself one - " +
                    "den I go fix allu so police doe catch allu so easy, yah wii.\"";
                return;
            }

            if (blessed)
            {
                _lastFeedback =
                    "Moutey pat allu chain. \"I already bless it, chile. You faster now, police trouble allu less. Go on.\"";
                return;
            }

            // Mission beat: granny blesses the chain -> evade/strength boost.
            if (prog != null) prog.SetChainBlessed(true);

            // Apply the boost to both protagonists.
            var switcher = CharacterSwitchManager.Instance;
            if (switcher?.Slots != null)
            {
                foreach (var slot in switcher.Slots)
                {
                    if (slot?.vitals != null) slot.vitals.BoostEvade(true);
                }
            }

            // Rep: getting stronger vs police is positive standing.
            prog?.AddReputation(Faction.GrandBayGangs, 8);

            Missions.MissionSystem.Instance?.Alert(
                "MOUTEY\nGranny bless allu chain. Allu run faster and police doe catch allu so easy now.");

            _lastFeedback =
                "Moutey trace di chain and nod. \"I bless it, chile. Police go tired before dey reach allu. Go on, make ah name.\"";
        }

        public override string GetInteractionFeedback() => _lastFeedback;
    }
}
