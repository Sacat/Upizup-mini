using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Economy;
using UpIzUpMini.UI;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-113 focused validator: the road-junction height fix,
    /// BikeHomePoint's relocation next to the migrated safehouse, and
    /// vehicle minimap markers appearing on purchase.</summary>
    public static class Mini113WorldCleanupValidation
    {
        [MenuItem("Up Iz Up Mini/MINI-113/Validate World Cleanup")]
        public static void Validate()
        {
            bool pass = true;
            string fail = null;

            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            // --- Road-ribbon junction: no real bump between the two ------
            // actually-connected roads that had one ------------------------
            GameObject spur = GameObject.Find("Road_user_highland_farm_spur");
            GameObject inroad = GameObject.Find("Road_user_highland_lalay_inroad");
            Check(ref pass, ref fail, spur != null && inroad != null, "Road_user_highland_farm_spur or Road_user_highland_lalay_inroad missing.");
            if (spur != null && inroad != null)
            {
                var spurFilter = spur.GetComponentInChildren<MeshFilter>();
                var inroadFilter = inroad.GetComponentInChildren<MeshFilter>();
                Vector3[] spurWorld = spurFilter.sharedMesh.vertices.Select(v => spurFilter.transform.TransformPoint(v)).ToArray();
                Vector3[] inroadWorld = inroadFilter.sharedMesh.vertices.Select(v => inroadFilter.transform.TransformPoint(v)).ToArray();
                float bestH = float.MaxValue, bestV = 0f;
                foreach (var sv in spurWorld)
                foreach (var iv in inroadWorld)
                {
                    float h = Vector2.Distance(new Vector2(sv.x, sv.z), new Vector2(iv.x, iv.z));
                    if (h >= bestH) continue;
                    bestH = h; bestV = Mathf.Abs(sv.y - iv.y);
                }
                Check(ref pass, ref fail, bestV < 0.05f,
                    $"highland_farm_spur <-> highland_lalay_inroad vertical step is {bestV:F3}m (was 0.286m before this fix) - junction smoothing did not take.");
            }

            // --- BikeHomePoint sits near the real, migrated safehouse ----
            GameObject bikeHome = GameObject.Find("BikeHomePoint");
            GameObject safehouse = GameObject.Find("FarmSafehouse");
            Check(ref pass, ref fail, bikeHome != null && safehouse != null, "BikeHomePoint or FarmSafehouse missing.");
            if (bikeHome != null && safehouse != null)
            {
                float d = Vector3.Distance(bikeHome.transform.position, safehouse.transform.position);
                Check(ref pass, ref fail, d < 15f,
                    $"BikeHomePoint is {d:F2}m from FarmSafehouse (was 303.52m before this fix) - still orphaned from the migrated safehouse.");
            }

            // --- Vehicle minimap markers appear on purchase --------------
            var economy = Object.FindFirstObjectByType<EconomyManager>();
            var spawner = Object.FindFirstObjectByType<VehicleSpawnController>();
            Check(ref pass, ref fail, economy != null && spawner != null, "EconomyManager or VehicleSpawnController missing.");
            if (economy != null && spawner != null)
            {
                Invoke(economy, "Awake");
                Invoke(spawner, "Awake");
                string feedback = spawner.SpawnPurchasedVehicle("tmax_560");
                Check(ref pass, ref fail, !string.IsNullOrEmpty(feedback), $"SpawnPurchasedVehicle('tmax_560') returned no feedback: '{feedback}'.");

                var tmax = GameObject.Find("PlayerTMAX");
                Check(ref pass, ref fail, tmax != null, "PlayerTMAX not found in the scene after SpawnPurchasedVehicle.");
                if (tmax != null)
                {
                    var marker = tmax.GetComponent<GtaMiniMapMarker>();
                    Check(ref pass, ref fail, marker != null, "PlayerTMAX has no GtaMiniMapMarker after purchase.");
                    Check(ref pass, ref fail, marker != null && marker.Kind == MiniMapMarkerKind.Vehicle,
                        $"PlayerTMAX's marker kind is {marker?.Kind} - expected Vehicle.");
                }
            }

            Report(pass, fail);
        }

        private static object Invoke(object target, string methodName, object[] args = null)
        {
            var method = target.GetType().GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
            {
                Debug.LogError($"MINI-113 VALIDATION: method '{methodName}' not found on {target.GetType().Name}.");
                return null;
            }
            return method.Invoke(target, args);
        }

        private static void Check(ref bool pass, ref string fail, bool condition, string message)
        {
            if (pass && !condition) { pass = false; fail = message; }
        }

        private static void Report(bool pass, string fail)
        {
            if (pass)
                Debug.Log("MINI-113 VALIDATION PASS: the Highland farm spur/inroad junction no longer has a real vertical step; BikeHomePoint sits near the real, migrated FarmSafehouse instead of 303m away in the old world; and a purchased TMAX carries a Vehicle-kind minimap marker. NOT covered: how the smoothed junction and repositioned bike home actually look/drive/feel in a real playthrough.");
            else
                Debug.LogError($"MINI-113 VALIDATION FAIL: {fail}");
        }
    }
}
