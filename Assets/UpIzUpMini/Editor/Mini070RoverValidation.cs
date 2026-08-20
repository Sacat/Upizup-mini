using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-070 static validation: Boss C's SUV is the real Range Rover, at a
    /// believable size, within a sane triangle budget, and the box-and-cylinder
    /// placeholder is genuinely gone rather than sitting inside the new model.
    ///
    /// The triangle check exists for the same reason the gold chain's does: the
    /// source arrived at 1,984,875 triangles, and if someone re-exports it
    /// without the Blender cleanup step the game still looks correct while
    /// quietly shipping a two-million-poly prop.
    /// </summary>
    public static class Mini070RoverValidation
    {
        private const int MaxRoverTris = 30000;

        [MenuItem("Up Iz Up Mini/MINI-070/Validate Range Rover")]
        public static void Validate()
        {
            bool pass = true;
            string fail = null;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UpIzUpMini/Art/Vehicles/RangeRover.prefab");
            Check(ref pass, ref fail, prefab != null,
                "RangeRover.prefab not found - run MINI-070/Build Range Rover Prefab.");

            if (prefab != null)
            {
                int tris = 0;
                foreach (var mf in prefab.GetComponentsInChildren<MeshFilter>(true))
                    if (mf.sharedMesh != null) tris += mf.sharedMesh.triangles.Length / 3;

                Check(ref pass, ref fail, tris > 0, "the Range Rover prefab has no mesh.");
                Check(ref pass, ref fail, tris <= MaxRoverTris,
                    $"the Range Rover is {tris} triangles, over the {MaxRoverTris} budget - the Blender decimation step was skipped.");

                var rends = prefab.GetComponentsInChildren<Renderer>(true);
                Check(ref pass, ref fail, rends.Length > 0 && rends[0].sharedMaterial != null,
                    "the Range Rover prefab has no material.");

                // Simple collision only, same call as the TMAX - a 20k-tri mesh
                // collider on parked set dressing is not worth the cost.
                Check(ref pass, ref fail, prefab.GetComponent<BoxCollider>() != null,
                    "the Range Rover prefab has no BoxCollider.");
                Check(ref pass, ref fail, prefab.GetComponentInChildren<MeshCollider>(true) == null,
                    "the Range Rover prefab has a MeshCollider - it should use the simple box.");
            }

            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var suv = GameObject.Find("BossC_SUV");
            Check(ref pass, ref fail, suv != null, "BossC_SUV not found in the built scene.");

            if (suv != null)
            {
                Check(ref pass, ref fail, suv.transform.Find("RangeRover") != null,
                    "BossC_SUV has no RangeRover child - the placeholder was not replaced.");

                // The old stand-in built children literally named Body/Cabin/Wheel.
                // If any survive, the primitives are sitting inside the real car.
                foreach (Transform child in suv.transform)
                {
                    Check(ref pass, ref fail,
                        child.name != "Body" && child.name != "Cabin" && child.name != "Wheel",
                        $"placeholder geometry '{child.name}' is still under BossC_SUV alongside the real model.");
                }

                var rends = suv.GetComponentsInChildren<Renderer>(true);
                Check(ref pass, ref fail, rends.Length > 0, "BossC_SUV has no renderers.");
                if (rends.Length > 0)
                {
                    Bounds b = rends[0].bounds;
                    for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

                    // Longest horizontal axis, so the check survives the car being
                    // parked at any angle - a world AABB grows when rotated.
                    float longest = Mathf.Max(b.size.x, b.size.z);
                    Check(ref pass, ref fail, longest > 4.0f && longest < 6.5f,
                        $"the parked Range Rover measures {longest:F2}m along its longest horizontal axis - expected roughly {Mini070RangeRoverPrep.RealLengthM}m.");
                    Check(ref pass, ref fail, b.size.y > 1.3f && b.size.y < 2.6f,
                        $"the parked Range Rover is {b.size.y:F2}m tall - expected roughly 1.8m.");
                }
            }

            if (pass)
                Debug.Log($"MINI-070 VALIDATION PASS: RangeRover.prefab is a cleaned, textured, box-collided model inside a {MaxRoverTris}-triangle budget (down from 1,984,875 in the source), and Boss C's BossC_SUV now holds the real car at real-world size with no placeholder primitives left behind. NOT covered: how it reads in motion or from the gameplay camera - checked instead by rendered screenshots via MINI-070/Render Range Rover Check.");
            else
                Debug.LogError($"MINI-070 VALIDATION FAIL: {fail}");
        }

        private static void Check(ref bool pass, ref string fail, bool condition, string message)
        {
            if (pass && !condition) { pass = false; fail = message; }
        }
    }
}
