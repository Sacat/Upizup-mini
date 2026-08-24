using UnityEditor;
using UnityEngine;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-071. Turns the static Range Rover prop into a driveable vehicle
    /// prefab: Rigidbody, four WheelColliders, a driver's door marker, a seat
    /// and a camera target.
    ///
    /// Everything is placed from the model's MEASURED bounds rather than hand
    /// numbers, the same discipline the TMAX prep uses - a re-export at a
    /// different size then still produces a car whose wheels are at its corners.
    ///
    /// KNOWN LIMITATION, deliberate: the source is a single merged mesh
    /// (`geometry_0`), so the visible wheels cannot be isolated and will NOT
    /// spin or steer. The WheelColliders are invisible and positioned by
    /// proportion. Splitting the wheels out is real modelling work and was out
    /// of scope for "just get in and drive".
    /// </summary>
    public static class Mini071RoverVehiclePrep
    {
        private const string PropPath = "Assets/UpIzUpMini/Art/Vehicles/RangeRover.prefab";
        private const string VehiclePath = "Assets/UpIzUpMini/Art/Vehicles/RangeRover_Vehicle.prefab";

        /// <summary>Kerb weight of a real Range Rover, in kg.</summary>
        public const float MassKg = 2500f;

        [MenuItem("Up Iz Up Mini/MINI-071/Build Driveable Range Rover")]
        public static void BuildVehicle()
        {
            var prop = AssetDatabase.LoadAssetAtPath<GameObject>(PropPath);
            if (prop == null)
            {
                Debug.LogError($"MINI-071 FAIL: {PropPath} not found. Run MINI-070/Build Range Rover Prefab first.");
                return;
            }

            var root = new GameObject("RangeRover_Vehicle");
            var body = (GameObject)PrefabUtility.InstantiatePrefab(prop, root.transform);
            body.name = "Body";
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;

            var renderers = body.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Debug.LogError("MINI-071 FAIL: the Range Rover prop has no renderers.");
                Object.DestroyImmediate(root);
                return;
            }

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

            float length = b.size.z;
            float width = b.size.x;
            float height = b.size.y;

            // Real-car proportions, applied to the measured box.
            float wheelRadius = height * 0.20f;          // ~0.36m on a 1.8m-tall SUV
            float axleZ = length * 0.31f;                // wheelbase ~62% of length
            float trackX = width * 0.40f;                // track ~80% of width

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = MassKg;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            // MINI-119 follow-up: same fix as the bike - Unity's own docs
            // document the default solver iteration count (6) as a common
            // cause of WheelCollider suspension jitter, recommending a
            // targeted per-Rigidbody override. Never set before this.
            rb.solverIterations = 16;
            rb.solverVelocityIterations = 12;

            // Body collision sits ABOVE the wheels. A box spanning the whole
            // height would rest on the ground and the WheelColliders would never
            // take any load - the car would sit there refusing to move.
            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(width * 0.92f, height - wheelRadius, length * 0.94f);
            box.center = new Vector3(0f, wheelRadius + (height - wheelRadius) * 0.5f, 0f);

            var wheels = new GameObject("Wheels");
            wheels.transform.SetParent(root.transform, false);

            WheelCollider fl = MakeWheel(wheels.transform, "FrontLeft", new Vector3(-trackX, wheelRadius, axleZ), wheelRadius, MassKg);
            WheelCollider fr = MakeWheel(wheels.transform, "FrontRight", new Vector3(trackX, wheelRadius, axleZ), wheelRadius, MassKg);
            WheelCollider rl = MakeWheel(wheels.transform, "RearLeft", new Vector3(-trackX, wheelRadius, -axleZ), wheelRadius, MassKg);
            WheelCollider rr = MakeWheel(wheels.transform, "RearRight", new Vector3(trackX, wheelRadius, -axleZ), wheelRadius, MassKg);

            // Markers: driver's door on the LEFT (this project drives on the
            // left, so the driver's side is the right in real Dominica - but the
            // model is generic and the door marker is what the code uses, so it
            // is placed on the driver's side as modelled).
            var door = new GameObject("DriverDoor");
            door.transform.SetParent(root.transform, false);
            door.transform.localPosition = new Vector3(-width * 0.5f, wheelRadius + 0.4f, axleZ * 0.35f);

            var seat = new GameObject("DriverSeat");
            seat.transform.SetParent(root.transform, false);
            seat.transform.localPosition = new Vector3(-width * 0.22f, wheelRadius + 0.32f, axleZ * 0.30f);

            // MINI-073: three passenger seats - front passenger plus two rear -
            // so recruited crew have somewhere to sit. Mirrored off the driver
            // seat's own offsets rather than new hand numbers.
            var seatsParent = new GameObject("PassengerSeats");
            seatsParent.transform.SetParent(root.transform, false);

            var frontPassenger = new GameObject("Seat_FrontPassenger");
            frontPassenger.transform.SetParent(seatsParent.transform, false);
            frontPassenger.transform.localPosition = new Vector3(width * 0.22f, wheelRadius + 0.32f, axleZ * 0.30f);

            var rearLeftSeat = new GameObject("Seat_RearLeft");
            rearLeftSeat.transform.SetParent(seatsParent.transform, false);
            rearLeftSeat.transform.localPosition = new Vector3(-width * 0.22f, wheelRadius + 0.32f, -axleZ * 0.35f);

            var rearRightSeat = new GameObject("Seat_RearRight");
            rearRightSeat.transform.SetParent(seatsParent.transform, false);
            rearRightSeat.transform.localPosition = new Vector3(width * 0.22f, wheelRadius + 0.32f, -axleZ * 0.35f);

            var camTarget = new GameObject("CameraTarget");
            camTarget.transform.SetParent(root.transform, false);
            camTarget.transform.localPosition = new Vector3(0f, height * 0.75f, 0f);

            var car = root.AddComponent<CarController>();
            var cso = new SerializedObject(car);
            cso.FindProperty("frontLeft").objectReferenceValue = fl;
            cso.FindProperty("frontRight").objectReferenceValue = fr;
            cso.FindProperty("rearLeft").objectReferenceValue = rl;
            cso.FindProperty("rearRight").objectReferenceValue = rr;
            cso.ApplyModifiedPropertiesWithoutUndo();

            // MINI-078: CarInteractable added BEFORE CarRadioController - real
            // bug found and fixed here. CarRadioController carries
            // [RequireComponent(typeof(CarInteractable))], and Unity's
            // RequireComponent auto-adds a dependency THE INSTANT it is
            // missing, synchronously, at AddComponent time - not deferred to
            // runtime. With the radio added first (the previous order), that
            // auto-add fired before the real, explicitly-wired CarInteractable
            // below ever ran, so the LATER `root.AddComponent<CarInteractable>()`
            // created a SECOND instance alongside the auto-added blank one -
            // CarInteractable carried no [DisallowMultipleComponent], so Unity
            // allowed it silently. CarRadioController.Awake() then grabbed
            // WHICHEVER instance GetComponent found first, which was the
            // blank auto-added one whose HasDriver can never become true -
            // exactly why pressing M did nothing ("not hearing the music").
            // Fixed two ways: reordered so the real instance already exists
            // before anything can trigger an auto-add, AND added
            // [DisallowMultipleComponent] to the class itself so a future
            // ordering mistake fails LOUDLY (Unity refuses the second
            // AddComponent) instead of silently duplicating again.
            var interactable = root.AddComponent<CarInteractable>();
            var iso = new SerializedObject(interactable);
            iso.FindProperty("car").objectReferenceValue = car;
            iso.FindProperty("driverDoor").objectReferenceValue = door.transform;
            iso.FindProperty("seatAnchor").objectReferenceValue = seat.transform;
            iso.FindProperty("cameraTarget").objectReferenceValue = camTarget.transform;
            var passengerSeatsProp = iso.FindProperty("passengerSeats");
            passengerSeatsProp.arraySize = 3;
            passengerSeatsProp.GetArrayElementAtIndex(0).objectReferenceValue = frontPassenger.transform;
            passengerSeatsProp.GetArrayElementAtIndex(1).objectReferenceValue = rearLeftSeat.transform;
            passengerSeatsProp.GetArrayElementAtIndex(2).objectReferenceValue = rearRightSeat.transform;
            iso.ApplyModifiedPropertiesWithoutUndo();

            // MINI-074/078: the in-car radio. One licensed track, toggled by
            // the driver only - see CarRadioController for the full reasoning.
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/UpIzUpMini/Audio/Music/UpIsUp.mp3");
            var audioSource = root.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.spatialBlend = 0f;
            // MINI-081, user: "lower the radio music" - was defaulting to
            // AudioSource's own 1.0 (full volume), never set explicitly.
            audioSource.volume = 0.45f;
            var radio = root.AddComponent<CarRadioController>();
            var radioSo = new SerializedObject(radio);
            radioSo.FindProperty("source").objectReferenceValue = audioSource;
            radioSo.FindProperty("car").objectReferenceValue = interactable;
            radioSo.ApplyModifiedPropertiesWithoutUndo();
            if (clip == null)
                Debug.LogWarning("MINI-074: UpIsUp.mp3 not found at Assets/UpIzUpMini/Audio/Music/UpIsUp.mp3 - radio will have nothing to play.");

            Vector3 boxSize = box.size;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, VehiclePath, out bool ok);
            Object.DestroyImmediate(root);

            if (!ok || prefab == null)
            {
                Debug.LogError("MINI-071 FAIL: SaveAsPrefabAsset reported failure.");
                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"MINI-071 VEHICLE OK: {VehiclePath} built. MEASURED body {width:F2}w x {length:F2}l x {height:F2}h; " +
                      $"wheelRadius={wheelRadius:F3}, axleZ=+/-{axleZ:F3} (wheelbase {axleZ * 2f:F2}m), trackX=+/-{trackX:F3}; " +
                      $"mass={MassKg}kg; bodyCollider={boxSize}. Visible wheels do NOT spin - the source is one merged mesh.");
        }

        private static WheelCollider MakeWheel(Transform parent, string name, Vector3 localPos, float radius, float carMass)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var wc = go.AddComponent<WheelCollider>();
            wc.radius = radius;
            wc.suspensionDistance = 0.28f;
            wc.wheelDampingRate = 1.2f;
            // MINI-119 follow-up, user: "it still glitches bad on the
            // same rough terrain." This was explicitly set to 0 -
            // Unity's own scripting reference/community explicitly
            // documents forceAppPointDistance=0 as a real cause of
            // WheelCollider jitter/instability on rough terrain, since
            // the suspension force then applies at the collider's own
            // local origin instead of below the Rigidbody's real centre
            // of mass. 0.3m below the wheel's rest position is the
            // commonly-documented starting point for a normal vehicle.
            wc.forceAppPointDistance = 0.3f;

            // Spring sized from the car's own mass - a spring tuned for a 220kg
            // bike would let a 2.5-tonne SUV sink straight through its travel.
            var spring = wc.suspensionSpring;
            spring.spring = carMass * 45f;
            spring.damper = carMass * 6f;
            spring.targetPosition = 0.5f;
            wc.suspensionSpring = spring;

            var fwd = wc.forwardFriction;
            fwd.extremumSlip = 0.4f; fwd.extremumValue = 1f;
            fwd.asymptoteSlip = 0.8f; fwd.asymptoteValue = 0.6f;
            fwd.stiffness = 2.2f;
            wc.forwardFriction = fwd;

            // Sideways grip deliberately higher than forward: a tall, heavy SUV
            // that slides sideways under normal cornering feels broken.
            var side = wc.sidewaysFriction;
            side.extremumSlip = 0.25f; side.extremumValue = 1f;
            side.asymptoteSlip = 0.5f; side.asymptoteValue = 0.75f;
            side.stiffness = 2.6f;
            wc.sidewaysFriction = side;

            return wc;
        }
    }
}
