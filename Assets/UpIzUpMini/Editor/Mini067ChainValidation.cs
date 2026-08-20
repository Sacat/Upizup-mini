using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Economy;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-067/068 static validation: the gold chain is a real, game-ready
    /// asset worn by both characters, and the bike has a home to return to.
    ///
    /// Deliberately checks the POLYGON BUDGET as well as presence. The source
    /// model was 1,995,768 triangles; if someone re-exports it without the
    /// Blender cleanup step the game still "works" and still shows a chain,
    /// it just quietly ships a two-million-poly necklace. That is exactly the
    /// kind of regression a static check should catch rather than a person.
    /// </summary>
    public static class Mini067ChainValidation
    {
        private const int MaxChainTris = 20000;

        [MenuItem("Up Iz Up Mini/MINI-067/Validate Chain + Bike Home")]
        public static void Validate()
        {
            bool pass = true;
            string fail = null;

            var chain = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UpIzUpMini/Art/Accessories/GoldChain18k.prefab");
            Check(ref pass, ref fail, chain != null, "GoldChain18k.prefab not found - run MINI-067/Build Gold Chain Prefab.");

            if (chain != null)
            {
                int tris = 0;
                foreach (var mf in chain.GetComponentsInChildren<MeshFilter>(true))
                    if (mf.sharedMesh != null) tris += mf.sharedMesh.triangles.Length / 3;

                Check(ref pass, ref fail, tris > 0, "the chain prefab has no mesh.");
                Check(ref pass, ref fail, tris <= MaxChainTris,
                    $"the chain is {tris} triangles, over the {MaxChainTris} budget - the Blender decimation step was skipped.");

                var rends = chain.GetComponentsInChildren<Renderer>(true);
                Check(ref pass, ref fail, rends.Length > 0 && rends[0].sharedMaterial != null,
                    "the chain prefab has no material, so it would render untextured.");
            }

            var item = AssetDatabase.LoadAssetAtPath<ShopItemDefinition>("Assets/UpIzUpMini/Data/Shop/chain_gold.asset");
            Check(ref pass, ref fail, item != null, "chain_gold.asset not found.");
            Check(ref pass, ref fail, item == null || item.price == 6000,
                $"expected the chain to cost 6000, found {(item != null ? item.price : -1)}.");

            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            // Boss C wears his unconditionally - it is character, not a purchase.
            Check(ref pass, ref fail, GameObject.Find("BossChain_18k") != null,
                "Boss C is not wearing BossChain_18k in the built scene.");

            // MINI-068: the bike's garage.
            var home = GameObject.Find("BikeHomePoint");
            Check(ref pass, ref fail, home != null, "BikeHomePoint marker missing from the scene.");

            var spawner = UnityEngine.Object.FindFirstObjectByType<Vehicles.VehicleSpawnController>();
            Check(ref pass, ref fail, spawner != null, "VehicleSpawner missing from the scene.");
            if (spawner != null)
            {
                var wired = new SerializedObject(spawner).FindProperty("bikeHome").objectReferenceValue;
                Check(ref pass, ref fail, wired != null, "VehicleSpawnController.bikeHome is not wired, so the bike would never return home.");
                Check(ref pass, ref fail, home == null || wired == null || (wired as Transform) == home.transform,
                    "VehicleSpawnController.bikeHome points at something other than BikeHomePoint.");
            }

            // MINI-080 removed the parked test TMAX from the built scene by
            // default (IncludeParkedTestVehicles = false) so a free bike
            // doesn't sit next to the safehouse outside of testing. Zero
            // bikes is now the expected steady state; more than one would
            // still mean ReturnBikeHome is stacking them, which is the thing
            // this check actually guards against.
            var bikes = UnityEngine.Object.FindObjectsByType<Vehicles.TmaxBikeController>(FindObjectsSortMode.None);
            Check(ref pass, ref fail, bikes.Length <= 1,
                $"expected at most 1 TMAX in the scene (0 by default now the test bike is removed, or 1 if parked/returned home), found {bikes.Length}.");

            if (bikes.Length == 1 && home != null)
            {
                float d = Vector3.Distance(bikes[0].transform.position, home.transform.position);
                Check(ref pass, ref fail, d < 0.5f,
                    $"the parked bike is {d:F2}m from BikeHomePoint - they are meant to be the same spot.");
            }

            if (pass)
                Debug.Log("MINI-067/068 VALIDATION PASS: the 18k chain is a cleaned, textured, budget-compliant prefab priced at 6000, Boss C wears it in the built scene, and the bike has a BikeHomePoint wired into VehicleSpawnController with at most one TMAX in the scene (0 by default since MINI-080 removed the parked test bike, 1 if parked/returned home). NOT covered: the player's own chain, which CharacterEquipment attaches at RUNTIME on purchase - that path was verified by rendered screenshots instead, since Play Mode does not tick in this batch environment.");
            else
                Debug.LogError($"MINI-067/068 VALIDATION FAIL: {fail}");
        }

        private static void Check(ref bool pass, ref string fail, bool condition, string message)
        {
            if (pass && !condition) { pass = false; fail = message; }
        }
    }
}
