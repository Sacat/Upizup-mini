using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-119 follow-up: read-only inspection to find which of
    /// the scene's bridge groups sits nearest the Highland farm route
    /// (Montine turnoff -> farm safehouse), so "Highland starts after
    /// the bridge" can be implemented against the correct bridge instead
    /// of a guess. Makes no changes. Delete after use.</summary>
    public static class Mini119BridgeLocateCheck
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Locate Highland Bridge (one-off, read-only)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var terrains = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in terrains)
                Debug.Log($"MINI-119 BRIDGE LOCATE: Terrain found - name={t.gameObject.name}, active={t.gameObject.activeInHierarchy}, pos={t.transform.position}");
            if (terrains.Length == 0) Debug.Log("MINI-119 BRIDGE LOCATE: no Terrain components found anywhere (active or inactive).");

            var bridges = GameObject.Find("Bridges");
            if (bridges == null) { Debug.LogError("MINI-119 BRIDGE LOCATE: no 'Bridges' object in scene."); return; }

            Vector3? farmCenter = null;
            var farmPlot = GameObject.Find("FarmPlot_00");
            if (farmPlot != null) farmCenter = farmPlot.transform.position;
            var safehouse = GameObject.Find("FarmSafehouse_Building");

            var carDealer = GameObject.Find("NPC_CarDealer");
            Debug.Log($"MINI-119 BRIDGE LOCATE: farmPlot={(farmPlot != null ? farmPlot.transform.position.ToString() : "NOT FOUND")}, farmSafehouse={(safehouse != null ? safehouse.transform.position.ToString() : "NOT FOUND")}, LalayHouse={(GameObject.Find("LalayHouse") != null ? GameObject.Find("LalayHouse").transform.position.ToString() : "NOT FOUND")}, market Stall_FARM SHOP={(GameObject.Find("Stall_FARM SHOP") != null ? GameObject.Find("Stall_FARM SHOP").transform.position.ToString() : "NOT FOUND")}, NPC_CarDealer={(carDealer != null ? carDealer.transform.position.ToString() : "NOT FOUND")}.");

            int i = 0;
            foreach (Transform bridge in bridges.transform)
            {
                var renderer = bridge.GetComponentInChildren<Renderer>();
                Vector3 pos = renderer != null ? renderer.bounds.center : bridge.position;
                float distToFarm = farmCenter.HasValue ? Vector3.Distance(pos, farmCenter.Value) : -1f;
                Debug.Log($"MINI-119 BRIDGE LOCATE: bridge[{i}] name={bridge.name}, pos={pos}, distanceToFarmPlot={distToFarm:F1}m");
                i++;
            }

            var villagers = GameObject.FindObjectsByType<UpIzUpMini.Interaction.TownNPCInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var v in villagers)
            {
                if (v.Role == UpIzUpMini.Interaction.NpcRole.Villager)
                    Debug.Log($"MINI-119 BRIDGE LOCATE: villager NPC {v.name} at {v.transform.position}");
            }

            Debug.Log("MINI-119 BRIDGE LOCATE: done (read-only, no changes made).");
        }
    }
}
