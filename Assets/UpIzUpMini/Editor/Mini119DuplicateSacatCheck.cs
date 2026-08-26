using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UpIzUpMini.EditorTools
{
    public static class Mini119DuplicateSacatCheck
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Check For Duplicate Sacat (one-off)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);
            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            int count = 0;
            foreach (var root in roots)
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.gameObject.name == "Sacat")
                    {
                        count++;
                        Debug.Log($"MINI-119 DUP CHECK: found Sacat #{count} - parent={(t.parent != null ? t.parent.name : "none (root)")}, worldPos={t.position}, instanceID={t.gameObject.GetInstanceID()}");
                    }
                }
            }
            Debug.Log($"MINI-119 DUP CHECK: total Sacat objects found = {count}");

            var bike = GameObject.Find("StockDemoSuperMoto");
            if (bike != null)
            {
                var interactables = bike.GetComponents<UpIzUpMini.Vehicles.SuperMotoVehicleInteractable>();
                var seats = bike.GetComponents<UpIzUpMini.Vehicles.VehicleSeat>();
                var wheelieKeyRemaps = bike.GetComponents<UpIzUpMini.Vehicles.SuperMotoWheelieKeyRemap>();
                var trikeStabilizers = bike.GetComponents<UpIzUpMini.Vehicles.SuperMotoTrikeStabilizer>();
                var wheelieAssists = bike.GetComponents<UpIzUpMini.Vehicles.SuperMotoWheelieAssist>();
                var seatChildren = 0;
                foreach (Transform t in bike.transform) if (t.name == "Seat") seatChildren++;
                Debug.Log($"MINI-119 DUP CHECK: bike component counts - SuperMotoVehicleInteractable={interactables.Length}, VehicleSeat={seats.Length}, SuperMotoWheelieKeyRemap={wheelieKeyRemaps.Length}, SuperMotoTrikeStabilizer={trikeStabilizers.Length}, SuperMotoWheelieAssist={wheelieAssists.Length}, 'Seat' child GameObjects={seatChildren}");
            }
        }
    }
}
