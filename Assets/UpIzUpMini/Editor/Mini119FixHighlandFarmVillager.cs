using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-119 follow-up, user: "i dont want people through the
    /// farm are where the hedge is inside." Confirmed via
    /// Mini119FarmHedgeCheck that NPC_Villager_HighlandFarm (added this
    /// session) and one of its own patrol waypoints sit INSIDE the
    /// Highland farm's hedge enclosure. Moves both the villager and its
    /// patrol route outside, near the farm's own entrance gap, instead
    /// of deleting the villager outright. Delete after use.</summary>
    public static class Mini119FixHighlandFarmVillager
    {
        private const float HalfWidth = 11.2f;
        private const float FrontDepth = -7.6f;
        private const float RearDepth = 14.5f;

        [MenuItem("Up Iz Up Mini/MINI-119/Fix Highland Farm Villager Inside Hedge (one-off)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var screen = GameObject.Find("HighlandFarmPrivacyBushes");
            var villagerGo = GameObject.Find("NPC_Villager_HighlandFarm");
            if (screen == null || villagerGo == null)
            {
                Debug.LogError("MINI-119 FIX FARM VILLAGER: HighlandFarmPrivacyBushes or NPC_Villager_HighlandFarm not found.");
                return;
            }

            Transform rearHedge = null;
            float maxScaleX = float.MinValue;
            foreach (Transform child in screen.transform)
                if (child.localScale.x > maxScaleX) { maxScaleX = child.localScale.x; rearHedge = child; }
            if (rearHedge == null) { Debug.LogError("MINI-119 FIX FARM VILLAGER: no hedge children found."); return; }

            Vector3 dir = rearHedge.forward;
            Vector3 right = rearHedge.right;
            Vector3 farmCenter = rearHedge.position - dir * RearDepth;
            farmCenter.y = 0f;

            // Just outside the front entrance gap (relY well past
            // FrontDepth, i.e. further from the hedge than the
            // enclosure boundary), centred on the entrance.
            Vector3 newPos = GroundSnap(farmCenter + dir * -12f);
            Vector3 wpA = GroundSnap(farmCenter + dir * -12f + right * 8f);
            Vector3 wpB = GroundSnap(farmCenter + dir * -12f - right * 8f);

            villagerGo.transform.position = newPos;
            villagerGo.transform.rotation = Quaternion.LookRotation((wpA - wpB).normalized, Vector3.up);

            var patrol = villagerGo.GetComponent<PatrolNPC>();
            if (patrol != null)
            {
                var pso = new SerializedObject(patrol);
                var wp = pso.FindProperty("waypoints");
                wp.arraySize = 2;
                wp.GetArrayElementAtIndex(0).vector3Value = wpA;
                wp.GetArrayElementAtIndex(1).vector3Value = wpB;
                pso.ApplyModifiedPropertiesWithoutUndo();
            }

            Debug.Log($"MINI-119 FIX FARM VILLAGER: moved NPC_Villager_HighlandFarm to {newPos} (outside the hedge, near the entrance), patrol {wpA}<->{wpB}.");

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("MINI-119 FIX FARM VILLAGER: scene saved.");
        }

        private static Vector3 GroundSnap(Vector3 pos)
        {
            pos.y = 20f;
            if (Physics.Raycast(pos + Vector3.up * 40f, Vector3.down, out RaycastHit hit, 90f))
                return hit.point;
            return pos;
        }
    }
}
