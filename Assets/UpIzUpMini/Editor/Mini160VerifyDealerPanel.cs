using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.UI;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-160: verify VisualVehicleDealerPanel's actual 3D
    /// vehicle preview renders real content for every vehicle in stock -
    /// pulls the panel's internal RenderTexture via reflection (its
    /// content is produced by a real Camera.Render() call already, IMGUI
    /// itself can't be captured this way, only the 3D preview stage can).</summary>
    public static class Mini160VerifyDealerPanel
    {
        const string Out = "Logs/Tasks/MINI-160";

        [MenuItem("Up Iz Up Mini/MINI-160/Verify Dealer Panel Preview")]
        public static void Run()
        {
            Directory.CreateDirectory(Out);
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var dealerGo = GameObject.Find("NPC_CarDealer");
            if (dealerGo == null)
            {
                Debug.LogError("MINI160VERIFY: NPC_CarDealer not found");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }
            // Awake() never runs on scene objects outside Play Mode, so
            // VehicleSpawnController.Instance is still null here - force it
            // (documented pattern, see BuildAndVerification.md).
            var spawner = Object.FindFirstObjectByType<Vehicles.VehicleSpawnController>();
            if (spawner != null)
            {
                var awake = typeof(Vehicles.VehicleSpawnController).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
                awake?.Invoke(spawner, null);
            }
            Debug.Log($"MINI160VERIFY: spawner found={(spawner != null)} Instance set={(Vehicles.VehicleSpawnController.Instance != null)}");

            var interactable = dealerGo.GetComponentInChildren<Interaction.TownNPCInteractable>(true);
            var shopField = typeof(Interaction.TownNPCInteractable).GetField("shop", BindingFlags.NonPublic | BindingFlags.Instance);
            var shop = interactable != null ? (ShopPanelController)shopField.GetValue(interactable) : null;
            Debug.Log($"MINI160VERIFY: dealer found, interactable={(interactable != null)}, shop={(shop != null ? "found" : "NULL")}, stock={(shop != null ? shop.Stock.Length : -1)}");

            VisualVehicleDealerPanel.Open(shop, dealerGo.transform);

            var panelGo = GameObject.Find("Visual Vehicle Dealer");
            Debug.Log($"MINI160VERIFY: panel instance created={(panelGo != null)}");
            var panel = panelGo.GetComponent<VisualVehicleDealerPanel>();
            var type = typeof(VisualVehicleDealerPanel);
            var texField = type.GetField("texture", BindingFlags.NonPublic | BindingFlags.Instance);
            var vehiclesField = type.GetField("vehicles", BindingFlags.NonPublic | BindingFlags.Instance);
            var indexField = type.GetField("index", BindingFlags.NonPublic | BindingFlags.Instance);
            var rebuildMethod = type.GetMethod("RebuildPreview", BindingFlags.NonPublic | BindingFlags.Instance);

            var vehicles = (System.Collections.IList)vehiclesField.GetValue(panel);
            Debug.Log($"MINI160VERIFY: vehicle stock count={vehicles.Count}");

            for (int i = 0; i < vehicles.Count; i++)
            {
                indexField.SetValue(panel, i);
                rebuildMethod.Invoke(panel, null);
                var camField = type.GetField("previewCamera", BindingFlags.NonPublic | BindingFlags.Instance);
                var modelField = type.GetField("model", BindingFlags.NonPublic | BindingFlags.Instance);
                var cam = (Camera)camField.GetValue(panel);
                var mdl = (GameObject)modelField.GetValue(panel);
                Debug.Log($"MINI160VERIFY: cam.enabled={cam.enabled} cam.pos={cam.transform.position} cam.orthoSize={cam.orthographicSize} " +
                          $"cam.cullingMask={cam.cullingMask} model={(mdl != null)} modelActive={(mdl != null && mdl.activeInHierarchy)} " +
                          $"modelLayer={(mdl != null ? mdl.layer : -1)} rendererCount={(mdl != null ? mdl.GetComponentsInChildren<Renderer>(true).Length : 0)}");

                var rt = (RenderTexture)texField.GetValue(panel);
                var def = vehicles[i];
                var nameProp = def.GetType().GetField("displayName");
                string name = nameProp != null ? (string)nameProp.GetValue(def) : $"vehicle{i}";
                SaveRenderTexture(rt, $"{Out}/Preview-{name.Replace(" ", "")}.png");
                Debug.Log($"MINI160VERIFY: captured preview for '{name}'");
            }

            panel.Close();
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void SaveRenderTexture(RenderTexture rt, string path)
        {
            var old = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = old;
            Object.DestroyImmediate(tex);
        }
    }
}
