using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-119 follow-up, user: "you should have saved all what i did
    /// manually save it." One-shot diagnostic: opens the CURRENT, already
    /// saved GrandBayProof.unity (does NOT rebuild or modify it) and logs
    /// the exact world transform of every hand-edited object - the
    /// WalkThroughHedge segments, the 8 FarmPlot_RC objects, and
    /// FarmSafehouse - so those numbers can be read from the Console and
    /// baked into the generator as permanent post-build overrides,
    /// preserving the user's manual placement instead of losing it on the
    /// next rebuild.
    /// </summary>
    public static class Mini119ReadManualEdits
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Read Manual Farm Edits (read-only)")]
        public static void Read()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var farmParent = GameObject.Find("MontineFarm");
            if (farmParent == null)
            {
                Debug.LogError("MINI-119 READ: MontineFarm not found - cannot read farm plot positions.");
            }
            else
            {
                var plots = farmParent.GetComponentsInChildren<Transform>(true)
                    .Where(t => t.name.StartsWith("FarmPlot_"))
                    .OrderBy(t => t.name)
                    .ToArray();
                foreach (var plot in plots)
                {
                    Debug.Log($"MINI-119 FARM PLOT '{plot.name}': localPos={FormatV3(plot.localPosition)} worldPos={FormatV3(plot.position)} worldRot={FormatV3(plot.eulerAngles)} localScale={FormatV3(plot.localScale)} parent='{plot.parent?.name}'");
                }
                Debug.Log($"MINI-119 FARM PARENT 'MontineFarm': worldPos={FormatV3(farmParent.transform.position)} worldRot={FormatV3(farmParent.transform.eulerAngles)}");

                var hedgeParent = farmParent.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t.name == "HighlandFarmPrivacyBushes");
                if (hedgeParent == null)
                {
                    // May have been reparented/renamed by the manual edit - fall back to a scene-wide search.
                    hedgeParent = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                        .FirstOrDefault(t => t.name == "HighlandFarmPrivacyBushes");
                }
                if (hedgeParent == null)
                {
                    Debug.LogError("MINI-119 READ: HighlandFarmPrivacyBushes not found - cannot read hedge positions.");
                }
                else
                {
                    Debug.Log($"MINI-119 HEDGE PARENT 'HighlandFarmPrivacyBushes': worldPos={FormatV3(hedgeParent.position)} worldRot={FormatV3(hedgeParent.eulerAngles)} localScale={FormatV3(hedgeParent.localScale)}");
                    var hedges = hedgeParent.GetComponentsInChildren<Transform>(true)
                        .Where(t => t.name == "WalkThroughHedge")
                        .ToArray();
                    for (int i = 0; i < hedges.Length; i++)
                    {
                        var h = hedges[i];
                        Debug.Log($"MINI-119 HEDGE[{i}] '{h.name}': localPos={FormatV3(h.localPosition)} worldPos={FormatV3(h.position)} worldRot={FormatV3(h.eulerAngles)} localScale={FormatV3(h.localScale)}");
                    }
                }
            }

            var safehouse = GameObject.Find("FarmSafehouse");
            if (safehouse == null)
            {
                Debug.LogError("MINI-119 READ: FarmSafehouse not found.");
            }
            else
            {
                Debug.Log($"MINI-119 SAFEHOUSE 'FarmSafehouse': worldPos={FormatV3(safehouse.transform.position)} worldRot={FormatV3(safehouse.transform.eulerAngles)}");
            }

            var bikeHome = GameObject.Find("BikeHomePoint");
            if (bikeHome != null)
                Debug.Log($"MINI-119 BIKEHOME 'BikeHomePoint': worldPos={FormatV3(bikeHome.transform.position)}");

            Debug.Log("MINI-119 READ COMPLETE.");
        }

        private static string FormatV3(Vector3 v) => $"({v.x:F3}, {v.y:F3}, {v.z:F3})";
    }
}
