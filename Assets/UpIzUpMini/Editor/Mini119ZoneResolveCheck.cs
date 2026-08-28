using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.UI;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-119 follow-up: verifies AreaNameDisplay.ResolveArea
    /// actually reports the right zone name at every key landmark after
    /// the Lalay/Highland rezone (bridge, car dealer, farm safehouse,
    /// market, Dog Life block) - checked by real invocation, not just
    /// worked out by hand. Delete after use.</summary>
    public static class Mini119ZoneResolveCheck
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Check Zone Resolve (one-off, read-only)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var area = Object.FindFirstObjectByType<AreaNameDisplay>();
            if (area == null) { Debug.LogError("MINI-119 ZONE CHECK: no AreaNameDisplay."); return; }

            var resolveMethod = typeof(AreaNameDisplay).GetMethod("ResolveArea", BindingFlags.NonPublic | BindingFlags.Instance);
            if (resolveMethod == null) { Debug.LogError("MINI-119 ZONE CHECK: ResolveArea method not found."); return; }

            (string label, Vector3 pos)[] points =
            {
                ("Dog Life block (expect NO zone - west of the farm shop, per the final \"lalay is from the farm shop to the car dealer\" redefinition)", new Vector3(-38.59f, 0f, -152.24f)),
                ("Market/Stall_FARM SHOP (expect Lalay)", new Vector3(-6.75f, 0f, -142.97f)),
                ("LalayHouse (expect Lalay)", new Vector3(49.98f, 0f, -144.95f)),
                ("Bridge (expect Lalay, right at the crossing)", new Vector3(80.93f, 0f, -146.67f)),
                ("HighlandInroad villager, just past bridge (expect Highland)", new Vector3(92f, 0f, -144f)),
                ("Farm plot (expect Highland)", new Vector3(108.74f, 0f, -134.58f)),
                ("Farm safehouse (expect Highland)", new Vector3(124.97f, 0f, -112.55f)),
                ("HighlandUpper villager (expect Highland)", new Vector3(128f, 0f, -108f)),
                ("Car Dealer (expect Lalay)", new Vector3(149.30f, 0f, -180.35f)),
            };

            foreach (var (label, pos) in points)
            {
                string result = (string)resolveMethod.Invoke(area, new object[] { pos });
                Debug.Log($"MINI-119 ZONE CHECK: {label} -> resolved=\"{result}\"");
            }

            Debug.Log("MINI-119 ZONE CHECK: done.");
        }
    }
}
