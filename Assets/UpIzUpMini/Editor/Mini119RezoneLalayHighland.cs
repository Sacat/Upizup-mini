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

            // Bridge_03_user_highland_lalay_inroad ~ (80.93, -146.67).
            // Lalay: far west (Bridge_00 ~x=-65) to the bridge (x~81),
            // centred with margin.
            var lalayCenter = new Vector3(8f, 0f, -148f);
            float lalayRadius = 85f;
            // Highland: the bridge (x~81) to the safehouse/upper farm
            // cluster (x~128), centred with margin.
            var highlandCenter = new Vector3(104f, 0f, -128f);
            float highlandRadius = 40f;

            var so = new SerializedObject(area);
            var zonesProp = so.FindProperty("zones");
            zonesProp.arraySize = 2;

            var lalay = zonesProp.GetArrayElementAtIndex(0);
            lalay.FindPropertyRelative("areaName").stringValue = "Lalay";
            lalay.FindPropertyRelative("center").vector3Value = lalayCenter;
            lalay.FindPropertyRelative("radius").floatValue = lalayRadius;

            var highland = zonesProp.GetArrayElementAtIndex(1);
            highland.FindPropertyRelative("areaName").stringValue = "Highland";
            highland.FindPropertyRelative("center").vector3Value = highlandCenter;
            highland.FindPropertyRelative("radius").floatValue = highlandRadius;

            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"MINI-119 REZONE: Lalay now center={lalayCenter} radius={lalayRadius} (whole road up to the bridge), Highland now center={highlandCenter} radius={highlandRadius} (bridge to the farm/safehouse).");

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("MINI-119 REZONE: scene saved.");
        }
    }
}
