using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-119 follow-up, user: "i dont want people through the
    /// farm are where the hedge is inside" - no NPCs walking inside the
    /// Highland farm's own hedge enclosure (HighlandFarmPrivacyBushes,
    /// built by Mini011PhaseBSetup.BuildFarmPrivacyScreen: a rectangle
    /// halfWidth=11.2 laterally, frontDepth=-7.6 to rearDepth=14.5 along
    /// the farm's own forward axis, centred on farmCenter). Read-only:
    /// reconstructs farmCenter/dir/right from the rear hedge's own
    /// transform (it was built with rotation = LookRotation(dir, up)),
    /// then reports which known NPCs/patrol waypoints fall inside.
    /// Delete after use.</summary>
    public static class Mini119FarmHedgeCheck
    {
        private const float HalfWidth = 11.2f;
        private const float FrontDepth = -7.6f;
        private const float RearDepth = 14.5f;

        [MenuItem("Up Iz Up Mini/MINI-119/Check Farm Hedge Containment (one-off, read-only)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var screen = GameObject.Find("HighlandFarmPrivacyBushes");
            if (screen == null) { Debug.LogError("MINI-119 HEDGE CHECK: no HighlandFarmPrivacyBushes in scene."); return; }

            Transform rearHedge = null;
            float maxZExtent = float.MinValue;
            // The rear hedge is the longest one (halfWidth*2+hedgeDepth =
            // 23.6, vs the side hedges' enclosureDepth = 22.1, vs the
            // front runs' frontRun ~8.2) - pick by local scale.x, the
            // most reliable distinguishing dimension regardless of
            // world rotation.
            foreach (Transform child in screen.transform)
            {
                if (child.localScale.x > maxZExtent) { maxZExtent = child.localScale.x; rearHedge = child; }
            }
            if (rearHedge == null) { Debug.LogError("MINI-119 HEDGE CHECK: no hedge children found."); return; }

            Vector3 dir = rearHedge.forward;
            Vector3 right = rearHedge.right;
            Vector3 farmCenter = rearHedge.position - dir * RearDepth;
            farmCenter.y = 0f;

            Debug.Log($"MINI-119 HEDGE CHECK: reconstructed farmCenter={farmCenter}, dir={dir}, right={right}.");

            bool IsInside(Vector3 p, out float relX, out float relY)
            {
                Vector3 delta = p - farmCenter; delta.y = 0f;
                relX = Vector3.Dot(delta, right);
                relY = Vector3.Dot(delta, dir);
                return Mathf.Abs(relX) <= HalfWidth && relY >= FrontDepth && relY <= RearDepth;
            }

            void CheckPoint(string label, Vector3 p)
            {
                bool inside = IsInside(p, out float relX, out float relY);
                Debug.Log($"MINI-119 HEDGE CHECK: {label} at {p} -> relX={relX:F1}, relY={relY:F1}, INSIDE HEDGE={inside}");
            }

            var villagers = GameObject.FindObjectsByType<TownNPCInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var v in villagers)
            {
                CheckPoint($"NPC {v.name}", v.transform.position);
                var patrol = v.GetComponent<PatrolNPC>();
                if (patrol != null)
                {
                    var wpField = typeof(PatrolNPC).GetField("waypoints", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var wps = wpField?.GetValue(patrol) as Vector3[];
                    if (wps != null)
                        for (int i = 0; i < wps.Length; i++)
                            CheckPoint($"  {v.name} waypoint[{i}]", wps[i]);
                }
            }

            Debug.Log("MINI-119 HEDGE CHECK: done (read-only, no changes made).");
        }
    }
}
