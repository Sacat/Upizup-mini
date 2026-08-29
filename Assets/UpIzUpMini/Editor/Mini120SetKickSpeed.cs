using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-120 follow-up, user: "the kick is too slow for
    /// sacat." Real, measured cause: Mixamo_KickSacat.fbx's clip is
    /// 2.17s long (vs Mixamo_KickFranki.fbx's 1.60s - a genuine 36%
    /// difference), while the actual swing timing (windup/active/
    /// recovery = 0.87s) is unrelated and much shorter - so the
    /// animation visibly drags on well past when the "hit" and the next
    /// allowed attack have already happened, reading as a slow kick.
    ///
    /// Fixed directly on the Animator STATE's own playback speed
    /// (a controller-asset property, completely independent of
    /// HumanoidAnimationManager's per-character ActionEntry data) -
    /// deliberately NOT adding a "speed" field to ActionEntry, since
    /// that struct is a value type already serialized on 20+ existing
    /// per-character entries across the live scene; a new field would
    /// default to 0 on all of those (a paused animation), a real
    /// regression risk for something that only needs to affect ONE
    /// specific state. Delete after use.</summary>
    public static class Mini120SetKickSpeed
    {
        private const string ControllerPath = "Assets/UpIzUpMini/Art/Animations/StarterAssetsThirdPerson.controller";

        [MenuItem("Up Iz Up Mini/MINI-120/Set Sacat Kick Speed (one-off)")]
        public static void Run()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null) { Debug.LogError("MINI-120 SET KICK SPEED: couldn't load " + ControllerPath); return; }

            const float targetSpeed = 1.5f; // 2.17s / 1.5 = ~1.45s, snappier than Franki's own 1.60s kick.
            bool found = false;

            foreach (var layer in controller.layers)
            {
                foreach (var child in layer.stateMachine.states)
                {
                    if (child.state.name != "MeleeKickSacat") continue;
                    float before = child.state.speed;
                    child.state.speed = targetSpeed;
                    Debug.Log($"MINI-120 SET KICK SPEED: 'MeleeKickSacat' on layer '{layer.name}' speed BEFORE={before}, AFTER={child.state.speed}.");
                    found = true;
                }
            }

            if (!found) { Debug.LogError("MINI-120 SET KICK SPEED: 'MeleeKickSacat' state not found on any layer."); return; }

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(ControllerPath, ImportAssetOptions.ForceUpdate);
            Debug.Log("MINI-120 SET KICK SPEED: done.");
        }
    }
}
