using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-119 follow-up, user (screenshot): "the character is
    /// badly on the bike and he bike looks too big compared to the
    /// characters." Determines conclusively whether the vendor's own
    /// rider mesh is still rendering (renderer-hide failed) or whether
    /// what's visible is genuinely Sacat in a bad IK pose - rather than
    /// guessing from the screenshot alone. Delete after use.</summary>
    public static class Mini119RiderVisibilityCheck
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Check Rider Visibility (one-off)")]
        public static void Run()
        {
            var previousSimMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var spawner = Object.FindFirstObjectByType<VehicleSpawnController>();
            var method = typeof(VehicleSpawnController).GetMethod("SpawnStockDemoBikeAndDisableOurCharacter", BindingFlags.NonPublic | BindingFlags.Instance);
            var player = GameObject.Find("Sacat");
            method.Invoke(spawner, new object[] { player });

            var instance = GameObject.Find("StockDemoSuperMoto");

            Debug.Log("MINI-119 VISIBILITY: --- all renderers under the bike instance ---");
            foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
            {
                Debug.Log($"MINI-119 VISIBILITY: {GetPath(r.transform)} enabled={r.enabled} activeInHierarchy={r.gameObject.activeInHierarchy}");
            }

            Debug.Log("MINI-119 VISIBILITY: --- Sacat's own renderers (wherever he now is) ---");
            foreach (var r in player.GetComponentsInChildren<Renderer>(true))
            {
                Debug.Log($"MINI-119 VISIBILITY: {GetPath(r.transform)} enabled={r.enabled} activeInHierarchy={r.gameObject.activeInHierarchy}");
            }

            var rider = player.GetComponent<VehicleRider>();
            Debug.Log($"MINI-119 VISIBILITY: VehicleRider.IsMounted={(rider != null ? rider.IsMounted.ToString() : "no VehicleRider")}, player.transform.parent={player.transform.parent?.name}");

            Physics.simulationMode = previousSimMode;
        }

        private static string GetPath(Transform t)
        {
            var s = t.name;
            var p = t.parent;
            while (p != null) { s = p.name + "/" + s; p = p.parent; }
            return s;
        }
    }
}
