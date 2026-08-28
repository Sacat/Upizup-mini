using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.UI;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-119 follow-up, user: "i want highland to start from
    /// after the bridge. lalay would be the entire road." Confirmed via
    /// follow-up: the bridge is Bridge_03_user_highland_lalay_inroad
    /// (literally named for this transition, and the closest bridge to
    /// the farm - see Mini119BridgeLocateCheck).
    ///
    /// AreaNameDisplay only supports circular zones (center+radius,
    /// nearest-match-wins) - not a hard line along the road - so this
    /// approximates "entire road, split at the bridge" as two circles:
    /// Lalay covers the whole stretch from the far west (Dog Life
    /// block/Bridge_00) up to the Highland bridge; Highland covers from
    /// the bridge to the farm/safehouse cluster. Patches the live scene
    /// directly (additive, no full rebuild) and updates
    /// Mini011PhaseBSetup.cs's own zone numbers so a future rebuild
    /// matches. Delete after use.</summary>
    public static class Mini119RezoneLalayHighland
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Rezone Lalay-Highland At Bridge (one-off)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var area = Object.FindFirstObjectByType<AreaNameDisplay>();
            if (area == null) { Debug.LogError("MINI-119 REZONE: no AreaNameDisplay in scene."); return; }

            // MINI-119 follow-up, user: "lalay should end by the car
            // dealer." NPC_CarDealer sits at (149.30, -180.35) - well
            // PAST the bridge (80.93, -146.67) and even past the farm
            // safehouse cluster (104-128, -108 to -134), on a lower/
            // south road branch (z ~-150 to -180) distinct from the
            // farm inroad (z rising toward -108 as it climbs). A single
            // big Lalay circle reaching the dealer would sit closer to
            // the farm cluster than Highland's own tight circle does
            // (nearest-center-wins), wrongly relabeling the farm as
            // Lalay - so Lalay is TWO circles instead: one for the
            // original west-of-bridge stretch (market, Dog Life block,
            // LalayHouse), one for the bridge-to-dealer stretch. Both
            // are named "Lalay" - AreaNameDisplay just returns whichever
            // zone's centre is nearest, name collisions are fine.
            var lalayWestCenter = new Vector3(-20f, 0f, -140f);
            // MINI-119 follow-up fix: 70m left LalayHouse (49.98,-144.95)
            // just outside (~70.2m from centre) - confirmed via
            // Mini119ZoneResolveCheck resolving "" there. Widened with
            // margin.
            float lalayWestRadius = 85f;
            var lalayEastCenter = new Vector3(115f, 0f, -165f);
            float lalayEastRadius = 45f;
            // Highland: tight around the farm/safehouse cluster only -
            // starts right at the bridge, does not reach the dealer.
            var highlandCenter = new Vector3(104f, 0f, -128f);
            float highlandRadius = 40f;

            var so = new SerializedObject(area);
            var zonesProp = so.FindProperty("zones");
            zonesProp.arraySize = 3;

            var lalayWest = zonesProp.GetArrayElementAtIndex(0);
            lalayWest.FindPropertyRelative("areaName").stringValue = "Lalay";
            lalayWest.FindPropertyRelative("center").vector3Value = lalayWestCenter;
            lalayWest.FindPropertyRelative("radius").floatValue = lalayWestRadius;

            var lalayEast = zonesProp.GetArrayElementAtIndex(1);
            lalayEast.FindPropertyRelative("areaName").stringValue = "Lalay";
            lalayEast.FindPropertyRelative("center").vector3Value = lalayEastCenter;
            lalayEast.FindPropertyRelative("radius").floatValue = lalayEastRadius;

            var highland = zonesProp.GetArrayElementAtIndex(2);
            highland.FindPropertyRelative("areaName").stringValue = "Highland";
            highland.FindPropertyRelative("center").vector3Value = highlandCenter;
            highland.FindPropertyRelative("radius").floatValue = highlandRadius;

            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"MINI-119 REZONE: Lalay west center={lalayWestCenter} radius={lalayWestRadius}, Lalay east center={lalayEastCenter} radius={lalayEastRadius} (reaches the car dealer), Highland center={highlandCenter} radius={highlandRadius} (bridge to farm/safehouse only, does not reach the dealer).");

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("MINI-119 REZONE: scene saved.");
        }
    }
}
