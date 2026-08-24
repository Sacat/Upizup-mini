using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-119 follow-up, user: "i want to see it where i have all my
    /// scenes i dont want to be moving all over." Adds the imported
    /// Motorbike Physics Tool's demo scene to File > Build Settings'
    /// "Scenes In Build" list, alongside the project's own scenes, so it
    /// can be opened from the same familiar place instead of hunting
    /// through the asset's own folder. Purely additive - appends, never
    /// touches the existing entries.
    /// </summary>
    public static class Mini119AddDemoSceneToBuildSettings
    {
        private const string DemoScenePath = "Assets/MotorbikePhysicsTool/SampleScene/Demo_Scene.unity";

        [MenuItem("Up Iz Up Mini/MINI-119/Add Motorbike Demo Scene To Build Settings")]
        public static void Add()
        {
            var existing = EditorBuildSettings.scenes.ToList();
            if (existing.Any(s => s.path == DemoScenePath))
            {
                Debug.Log("MINI-119: Demo_Scene already in Build Settings - nothing to do.");
                return;
            }

            existing.Add(new EditorBuildSettingsScene(DemoScenePath, true));
            EditorBuildSettings.scenes = existing.ToArray();
            Debug.Log($"MINI-119: added {DemoScenePath} to Build Settings ({existing.Count} scenes listed now).");
        }
    }
}
