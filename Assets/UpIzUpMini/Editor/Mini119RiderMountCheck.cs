using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-119 follow-up, one-off diagnostic: confirms Sacat
    /// actually landed on the bike seat (not floating off in space) after
    /// MountPlayerOnStockDemoBike. Delete after use.</summary>
    public static class Mini119RiderMountCheck
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Check Rider Mount (one-off)")]
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
            var rider = player.GetComponent<VehicleRider>();
            var seat = instance.GetComponentInChildren<VehicleSeat>();

            Debug.Log($"MINI-119 MOUNT CHECK: player.activeSelf={player.activeSelf}, VehicleRider.IsMounted={(rider != null ? rider.IsMounted.ToString() : "no VehicleRider")}, parent={player.transform.parent?.name ?? "none"}, worldPos={player.transform.position}, bikePos={instance.transform.position}, distanceFromBike={(player.transform.position - instance.transform.position).magnitude:F2}m");
            if (seat != null)
                Debug.Log($"MINI-119 MOUNT CHECK: seat anchor localPos={seat.SeatAnchor.localPosition}, leftHandTarget={(seat.LeftHandTarget != null ? seat.LeftHandTarget.name : "null")}, rightFootTarget={(seat.RightFootTarget != null ? seat.RightFootTarget.name : "null")}");

            Physics.simulationMode = previousSimMode;
        }
    }
}
