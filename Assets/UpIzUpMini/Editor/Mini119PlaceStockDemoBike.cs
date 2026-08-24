using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-119 follow-up, user: "i want the same bike that is spawning
    /// i can mount on" + "can you place him on the bike and build the
    /// exe." Places a permanent "StockDemoSuperMoto" instance directly
    /// into GrandBayProof.unity (Edit Mode, saved - not a runtime-only
    /// spawn) at the real Lalay road spot, so
    /// VehicleSpawnController.SpawnStockDemoBikeAndDisableOurCharacter's
    /// new preplaced-bike check (GameObject.Find("StockDemoSuperMoto"))
    /// picks this exact object up and reuses it instead of instantiating
    /// a separate copy - it's the same one you get on in Play Mode.
    ///
    /// Deliberately does NOT permanently parent Sacat onto the bike's
    /// seat in the saved scene - his real GameObject is what
    /// CharacterSwitchManager tracks for ordinary free-roam play, so
    /// nesting him under the bike at save-time would break normal
    /// walking/missions the instant the scene loads. "On the bike" is
    /// delivered the correct way instead: walk up and press F, using the
    /// already-working mount flow - against this exact, same, preplaced
    /// bike.
    /// </summary>
    public static class Mini119PlaceStockDemoBike
    {
        private const string MainScenePath = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        private const string BikePrefabPath = "Assets/MotorbikePhysicsTool/Prefabs/BikesWithRagdolls/SuperMotoWRagdoll.prefab";
        private const float BikeScale = 0.961f;

        [MenuItem("Up Iz Up Mini/MINI-119/Place Permanent Stock Demo Bike")]
        public static void Place()
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            var existing = GameObject.Find("StockDemoSuperMoto");
            if (existing != null)
            {
                Debug.Log("MINI-119 PLACE STOCK DEMO BIKE: 'StockDemoSuperMoto' already exists in the scene - leaving it as-is, not duplicating.");
                return;
            }

            var bikePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BikePrefabPath);
            if (bikePrefab == null)
            {
                Debug.LogError($"MINI-119 PLACE STOCK DEMO BIKE FAIL: {BikePrefabPath} not found.");
                return;
            }

            var bike = (GameObject)PrefabUtility.InstantiatePrefab(bikePrefab, scene);
            bike.name = "StockDemoSuperMoto";
            bike.transform.localScale = Vector3.one * BikeScale;

            // Same Lalay-road placement VehicleSpawnController itself
            // uses for a fresh spawn - so the preplaced bike lands
            // exactly where the user already confirmed liking it
            // ("i like how the bike spawns").
            Vector3 forward = Vector3.forward;
            Vector3 spawnPos;
            var farmShopStall = GameObject.Find("Stall_FARM SHOP");
            var produceBuyerStall = GameObject.Find("Stall_PRODUCE BUYER");
            if (farmShopStall != null && produceBuyerStall != null)
            {
                Vector3 mid = (farmShopStall.transform.position + produceBuyerStall.transform.position) * 0.5f;
                spawnPos = GroundSnap(mid);
                Vector3 acrossRoad = produceBuyerStall.transform.position - farmShopStall.transform.position;
                acrossRoad.y = 0f;
                if (acrossRoad.sqrMagnitude > 0.01f)
                    forward = Vector3.Cross(Vector3.up, acrossRoad.normalized);
            }
            else
            {
                var lalaySign = GameObject.Find("Sign_LALAY");
                spawnPos = lalaySign != null ? GroundSnap(lalaySign.transform.position - new Vector3(4.6f, 0f, 0f)) : Vector3.zero;
            }

            float wheelBottomOffset = 0.4f;
            var gadd = bike.GetComponent<Gadd420.RB_Controller>();
            if (gadd != null && gadd.wheelColliders != null && gadd.wheelColliders.Length >= 2
                && gadd.wheelColliders[0] != null && gadd.wheelColliders[1] != null)
            {
                float rearBottom = gadd.wheelColliders[0].transform.position.y - gadd.wheelColliders[0].radius;
                float frontBottom = gadd.wheelColliders[1].transform.position.y - gadd.wheelColliders[1].radius;
                wheelBottomOffset = -Mathf.Min(rearBottom, frontBottom);
            }
            bike.transform.SetPositionAndRotation(
                spawnPos + Vector3.up * (wheelBottomOffset + 0.1f),
                Quaternion.LookRotation(forward, Vector3.up));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"MINI-119 PLACE STOCK DEMO BIKE OK: 'StockDemoSuperMoto' placed at {bike.transform.position} and saved into {MainScenePath}. Sacat was left untouched (still free-roam) - VehicleSpawnController will reuse THIS exact bike instead of spawning a new one; walk up and press F to mount it.");
        }

        private static Vector3 GroundSnap(Vector3 pos)
        {
            if (Physics.Raycast(pos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 30f))
                return hit.point;
            return pos;
        }
    }
}
