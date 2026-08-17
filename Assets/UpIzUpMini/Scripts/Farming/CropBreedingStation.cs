using UnityEngine;
using UpIzUpMini.Economy;
using UpIzUpMini.Interaction;
using UpIzUpMini.Progression;

namespace UpIzUpMini.Farming
{
    /// <summary>
    /// MINI-047/MINI-048. "Interbreeding weed plants" - combines one
    /// harvested crop of each of two parent strains (the actual grown
    /// product, not seed - genetic material from a plant, not a packet)
    /// into seed of a new hybrid strain. A crafting-style combine rather
    /// than a literal plant-to-plant pollination simulation: the economy
    /// already tracks crops as inventory counts, not individual plant
    /// objects, so consuming two counted items to produce a new counted
    /// item is the honest fit for how this game's farming model actually
    /// works.
    ///
    /// MINI-048 generalised this from one fixed recipe to a list -
    /// Purple Black (Purple x Black Sugar), Sugar Cheese (Black Sugar x
    /// Blue Cheese), and Purple Cheese (Purple x Blue Cheese) all live on
    /// one station rather than three separate benches. One [E] press finds
    /// the first recipe that's both unlocked and has both ingredients on
    /// hand and breeds it; if none qualify, the feedback explains the
    /// closest miss (an unlocked recipe missing an ingredient, in
    /// preference over "nothing unlocked yet").
    /// </summary>
    public class CropBreedingStation : InteractableBase
    {
        [System.Serializable]
        public struct Recipe
        {
            public CropDefinition parentA;
            public CropDefinition parentB;
            public CropDefinition output;
            public int outputSeedCount;
        }

        [SerializeField] private Recipe[] recipes = System.Array.Empty<Recipe>();

        private string _feedback;

        public override string PromptLabel => "[ E ] Interbreed strains";

        public override void Interact(GameObject interactor)
        {
            var economy = EconomyManager.Instance;
            var progression = ProgressionManager.Instance;
            if (economy == null || recipes == null)
            {
                _feedback = "Nothing set up here yet.";
                return;
            }

            Recipe? closestMiss = null;

            foreach (var recipe in recipes)
            {
                if (recipe.parentA == null || recipe.parentB == null || recipe.output == null) continue;

                bool unlocked = progression == null || progression.IsCropUnlocked(recipe.output.cropId);
                if (!unlocked) continue;

                bool hasIngredients = economy.GetCount(recipe.parentA.cropId) >= 1
                                      && economy.GetCount(recipe.parentB.cropId) >= 1;
                if (hasIngredients)
                {
                    economy.AddCrop(recipe.parentA.cropId, -1);
                    economy.AddCrop(recipe.parentB.cropId, -1);
                    int count = recipe.outputSeedCount > 0 ? recipe.outputSeedCount : 1;
                    economy.AddSeeds(recipe.output.cropId, count);
                    _feedback = $"Crossed it. {count} {recipe.output.displayName} seed in hand - allu see what dat road bring.";
                    return;
                }

                // Remember the first unlocked recipe we can't complete yet,
                // so a player who has unlocked something but is short an
                // ingredient gets told what they're short, not a generic
                // "nothing ready" that reads like nothing is unlocked.
                closestMiss ??= recipe;
            }

            if (closestMiss.HasValue)
            {
                var r = closestMiss.Value;
                _feedback = $"Need one grown {r.parentA.displayName} and one grown {r.parentB.displayName} to cross - not seeds, the real thing.";
            }
            else
            {
                _feedback = "Not yet, nuh. None of dese crossings ready for allu right now.";
            }
        }

        public override string GetInteractionFeedback() => _feedback;
    }
}
