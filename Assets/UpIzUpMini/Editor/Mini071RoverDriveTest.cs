using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-071. Real PhysX, no Play Mode: drops the Range Rover on a floor and
    /// drives it, stepping the simulation by hand with Physics.Simulate.
    ///
    /// Worth the effort because the failure modes here are the ones that waste
    /// a whole play session - a car that sinks through the ground, sits inert
    /// because the body collider is resting on the floor instead of the wheels,
    /// or tips over the moment it turns. All three are invisible to a static
    /// wiring check, and all three were live risks on this build.
    ///
    /// Measures the RIGIDBODY, never the input it was given. That distinction
    /// is on record from MINI-065, where a test that read the controller's own
    /// setpoint reported success for rounds while the bike was barely moving.
    /// </summary>
    public static class Mini071RoverDriveTest
    {
        [MenuItem("Up Iz Up Mini/MINI-071/Run Rover Drive Test (real physics)")]
        public static void Run()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(400f, 1f, 400f);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/UpIzUpMini/Art/Vehicles/RangeRover_Vehicle.prefab");
            if (prefab == null) { Debug.LogError("MINI-071 TEST: RangeRover_Vehicle.prefab not found."); return; }

            var car = ((GameObject)PrefabUtility.InstantiatePrefab(prefab));
            car.transform.position = new Vector3(0f, 0.6f, 0f);
            car.transform.rotation = Quaternion.identity;

            var controller = car.GetComponent<CarController>();
            var rb = car.GetComponent<Rigidbody>();
            if (controller == null || rb == null) { Debug.LogError("MINI-071 TEST: prefab is missing CarController/Rigidbody."); return; }

            // Edit Mode never calls Awake/FixedUpdate, so Physics.Simulate on its
            // own steps PhysX past a controller that has not initialised and is
            // not applying any torque - which is exactly the "settles fine, then
            // refuses to move" result this produced first time. Driven by
            // reflection, the same way the TMAX drop test does it.
            Invoke(controller, "Awake");

            var previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            const float dt = 0.02f;
            bool pass = true;
            string fail = null;

            try
            {
                // --- Phase 1: settle ---------------------------------------
                for (int i = 0; i < 150; i++) { controller.SetInput(0f, 0f, 0f); Invoke(controller, "FixedUpdate"); Physics.Simulate(dt); }

                float restY = car.transform.position.y;
                float restTilt = Vector3.Angle(car.transform.up, Vector3.up);
                Debug.Log($"MINI-071 TEST settle: y={restY:F3}, tilt={restTilt:F1}deg, speed={controller.SpeedKmh:F2}km/h");

                Check(ref pass, ref fail, restY > -0.5f, $"the car sank through the floor (y={restY:F2}).");
                Check(ref pass, ref fail, restTilt < 8f, $"the car settled tilted {restTilt:F1}deg off level.");
                Check(ref pass, ref fail, controller.SpeedKmh < 1.5f, $"the car drifted at rest ({controller.SpeedKmh:F2}km/h).");

                // --- Phase 2: accelerate -----------------------------------
                Vector3 startPos = car.transform.position;
                for (int i = 0; i < 250; i++) { controller.SetInput(1f, 0f, 0f); Invoke(controller, "FixedUpdate"); Physics.Simulate(dt); }

                float travelled = Vector3.Distance(startPos, car.transform.position);
                float topSpeed = controller.ForwardSpeedKmh;
                Debug.Log($"MINI-071 TEST throttle: travelled={travelled:F2}m in 5s, forward speed={topSpeed:F1}km/h, tilt={Vector3.Angle(car.transform.up, Vector3.up):F1}deg");

                // The one that actually matters: did the BODY move, not did the
                // input get accepted.
                Check(ref pass, ref fail, travelled > 8f,
                    $"the car barely moved under full throttle ({travelled:F2}m in 5s) - the wheels are probably not carrying the load.");
                Check(ref pass, ref fail, topSpeed > 10f, $"forward speed only reached {topSpeed:F1}km/h.");

                // --- Phase 3: steer, and stay on its wheels ----------------
                float worstTilt = 0f;
                float headingStart = car.transform.eulerAngles.y;
                for (int i = 0; i < 200; i++)
                {
                    controller.SetInput(0.6f, 1f, 0f);
                    Invoke(controller, "FixedUpdate");
                    Physics.Simulate(dt);
                    worstTilt = Mathf.Max(worstTilt, Vector3.Angle(car.transform.up, Vector3.up));
                }
                float turned = Mathf.DeltaAngle(headingStart, car.transform.eulerAngles.y);
                Debug.Log($"MINI-071 TEST steering: heading changed {turned:F1}deg, worst tilt during the turn {worstTilt:F1}deg");

                Check(ref pass, ref fail, Mathf.Abs(turned) > 15f, $"steering barely turned the car ({turned:F1}deg).");
                Check(ref pass, ref fail, worstTilt < 40f,
                    $"the car leaned {worstTilt:F1}deg mid-corner - a tall SUV rolling over means the centre of mass is too high.");

                // --- Phase 4: brake ----------------------------------------
                for (int i = 0; i < 200; i++) { controller.SetInput(0f, 0f, 1f); Invoke(controller, "FixedUpdate"); Physics.Simulate(dt); }
                Debug.Log($"MINI-071 TEST braking: speed after 4s of brake = {controller.SpeedKmh:F2}km/h");
                Check(ref pass, ref fail, controller.SpeedKmh < 3f,
                    $"the car would not stop under full brake ({controller.SpeedKmh:F2}km/h).");
            }
            finally
            {
                Physics.simulationMode = previousMode;
            }

            if (pass)
                Debug.Log("MINI-071 DRIVE TEST PASS: the Range Rover settles level on its wheels, accelerates under throttle, steers without rolling over, and stops under braking - all measured from the Rigidbody, not from the input it was handed.");
            else
                Debug.LogError($"MINI-071 DRIVE TEST FAIL: {fail}");
        }

        /// <summary>Calls a private MonoBehaviour message by name - Edit Mode
        /// does not run the Unity lifecycle, so the test has to drive it.</summary>
        private static void Invoke(object target, string method)
        {
            var m = target.GetType().GetMethod(method,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);
            m?.Invoke(target, null);
        }

        private static void Check(ref bool pass, ref string fail, bool condition, string message)
        {
            if (pass && !condition) { pass = false; fail = message; }
        }
    }
}
