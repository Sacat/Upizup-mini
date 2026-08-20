using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UpIzUpMini.Farming;
using UpIzUpMini.Interaction;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;
using UpIzUpMini.UI;

namespace UpIzUpMini.EditorTools
{
    /// <summary>Static migration gate and fixed evidence cameras for MINI-100.</summary>
    public static class Mini100GrandBayMapValidation
    {
        private const string ScenePath = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        private static readonly Vector2[] LalaySpine =
        {
            new Vector2(-63.29f, -144.47f), new Vector2(-0.82f, -153.07f), new Vector2(24.11f, -156.50f),
            new Vector2(45f, -159.94f), new Vector2(77.30f, -164.11f), new Vector2(110.37f, -168.64f),
            new Vector2(126.17f, -170.94f), new Vector2(136.67f, -174.64f), new Vector2(142.24f, -184.04f),
            new Vector2(150.86f, -190.48f), new Vector2(163.62f, -197.30f), new Vector2(176.30f, -198.82f),
            new Vector2(190.52f, -201.72f)
        };
        private static string EvidenceTask => Environment.GetEnvironmentVariable("UPIZUP_EVIDENCE_TASK") ?? "MINI-100";
        private static string EvidenceFolder => Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Tasks", EvidenceTask);

        [MenuItem("Up Iz Up Mini/MINI-100/Validate Migrated Grand Bay")]
        public static void Validate()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            List<string> problems = CollectProblems(scene);
            if (problems.Count == 0)
                Debug.Log("MINI-100 MIGRATION VALIDATION PASS: approved world, Lalay/Highland density, farm expansion placeholders, gameplay roles, colliders, and script references survived the transfer.");
            else
                Debug.LogError("MINI-100 MIGRATION VALIDATION FAIL:\n- " + string.Join("\n- ", problems));
            if (Application.isBatchMode) EditorApplication.Exit(problems.Count == 0 ? 0 : 1);
        }

        [MenuItem("Up Iz Up Mini/MINI-100/Capture Migrated Grand Bay")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Directory.CreateDirectory(EvidenceFolder);
            CaptureView($"{EvidenceTask}-Overview-1600x1000.png", new Vector3(110f, 335f, -95f), new Vector3(110f, 0f, -95f), 1600, 1000, true, 225f);
            CaptureView($"{EvidenceTask}-LalayShops-1280x720.png", new Vector3(36f, 28f, -137f), new Vector3(91f, 1.5f, -174f), 1280, 720, false, 0f);
            CaptureView($"{EvidenceTask}-HighlandConnection-1280x720.png", new Vector3(34f, 72f, -70f), new Vector3(86f, 1.5f, -128f), 1280, 720, false, 0f);
            CaptureView($"{EvidenceTask}-Backstreet-1280x720.png", new Vector3(55f, 82f, -156f), new Vector3(58f, 0f, -194f), 1280, 720, false, 0f);
            GameObject sacat = FindAnywhere("Sacat");
            if (sacat != null)
                CaptureView($"{EvidenceTask}-Spawn-1280x720.png", sacat.transform.position + new Vector3(-7f, 7f, -10f), sacat.transform.position + Vector3.up * 1.4f, 1280, 720, false, 0f);
            GameObject farm = FindAnywhere("FarmPlot_00");
            if (farm != null)
                CaptureView($"{EvidenceTask}-HighlandFarm-1280x720.png", farm.transform.position + new Vector3(-23f, 20f, -24f), farm.transform.position + Vector3.up, 1280, 720, false, 0f);
            GameObject house = FindAnywhere("LalayHouse");
            if (house != null)
                CaptureView($"{EvidenceTask}-LalaySafehouse-1280x720.png", house.transform.position + new Vector3(-13f, 9f, -10f), house.transform.position + Vector3.up * 2.7f, 1280, 720, false, 0f);
            CaptureRolePair("NPC_Brakes", "GrandBay_Catholic_Church_Graybox", $"{EvidenceTask}-Church-Brakes-1280x720.png", 14f, 7f);
            CaptureRolePair("NPC_BoatMan", "MooredBoat", $"{EvidenceTask}-Jetty-BoatRoute-1280x720.png", 18f, 9f);
            CaptureRoleGroup("NPC_DogLife_", $"{EvidenceTask}-DogLifeBlock-1280x720.png", 13f, 8f);
            CaptureRolePair("NPC_BossC", "BossC_SUV", $"{EvidenceTask}-BossCBlock-Rover-1280x720.png", 13f, 7f);
            GameObject paro = FindAnywhere("NPC_Vagrant");
            if (paro != null)
                CaptureView($"{EvidenceTask}-Paro-1280x720.png", paro.transform.position + new Vector3(-7f, 5f, -6f), paro.transform.position + Vector3.up * 1.5f, 1280, 720, false, 0f);
            Debug.Log("MINI-100 MIGRATION CAPTURE PASS: overview, Lalay, Highland, Highland farm, and playable spawn evidence saved.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        [MenuItem("Up Iz Up Mini/MINI-102/Validate GTA Minimap Heat Overlay")]
        public static void ValidateMiniMapHeatOverlay()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GtaMiniMapController controller = UnityEngine.Object.FindFirstObjectByType<GtaMiniMapController>(FindObjectsInactive.Include);
            EconomyManager economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>(FindObjectsInactive.Include);
            if (controller == null || economy == null)
            {
                Debug.LogError("MINI-102 MINIMAP VALIDATION FAIL: controller or economy manager missing.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            typeof(EconomyManager).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(economy, null);
            MethodInfo update = typeof(GtaMiniMapController).GetMethod("UpdateWantedOverlay", BindingFlags.Instance | BindingFlags.NonPublic);
            SerializedObject serialized = new SerializedObject(controller);
            Image overlay = serialized.FindProperty("wantedOverlay").objectReferenceValue as Image;
            Text label = serialized.FindProperty("wantedLabel").objectReferenceValue as Text;
            economy.AddHeat(49f);
            update?.Invoke(controller, null);
            bool hiddenBelowThreshold = overlay != null && !overlay.gameObject.activeSelf && label != null && !label.gameObject.activeSelf;
            economy.AddHeat(1f);
            update?.Invoke(controller, null);
            bool visibleAtThreshold = overlay != null && overlay.gameObject.activeSelf && overlay.color.a > 0f
                && label != null && label.gameObject.activeSelf && label.text.Contains("50%");
            if (hiddenBelowThreshold && visibleAtThreshold)
                Debug.Log("MINI-102 MINIMAP VALIDATION PASS: wanted wash is hidden at 49% and transparent red/blue police overlay plus 50% label activate at the exact threshold.");
            else
                Debug.LogError($"MINI-102 MINIMAP VALIDATION FAIL: hidden49={hiddenBelowThreshold}, visible50={visibleAtThreshold}, label='{label?.text}'.");
            if (Application.isBatchMode) EditorApplication.Exit(hiddenBelowThreshold && visibleAtThreshold ? 0 : 1);
        }

        private static void CaptureRolePair(string firstName, string secondName, string fileName, float distance, float height)
        {
            GameObject first = FindAnywhere(firstName);
            GameObject second = FindAnywhere(secondName);
            if (first == null || second == null) return;
            Vector3 target = Vector3.Lerp(first.transform.position, second.transform.position, 0.35f) + Vector3.up * 1.5f;
            Vector3 away = first.transform.position - second.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = new Vector3(-1f, 0f, -1f);
            Vector3 camera = first.transform.position + away.normalized * distance + Vector3.up * height;
            CaptureView(fileName, camera, target, 1280, 720, false, 0f);
        }

        private static void CaptureRoleGroup(string namePrefix, string fileName, float distance, float height)
        {
            Transform[] members = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(transform => transform.name.StartsWith(namePrefix, StringComparison.Ordinal)).ToArray();
            if (members.Length == 0) return;
            Vector3 target = members.Aggregate(Vector3.zero, (sum, member) => sum + member.position) / members.Length;
            CaptureView(fileName, target + new Vector3(-distance, height, -distance * 0.65f), target + Vector3.up * 1.4f, 1280, 720, false, 0f);
        }

        private static List<string> CollectProblems(Scene scene)
        {
            List<string> problems = new List<string>();
            GameObject world = FindRoot(scene, "GrandBayPhase1_ApprovedWorld_VA005");
            Require(world != null, "approved VA-005 world root missing", problems);
            foreach (string oldRoot in new[] { "GrandBayTerrain", "Sea", "CoastAndJetty", "LalayRoad", "LalayHouses", "MontineFarmPath" })
                Require(FindRoot(scene, oldRoot) == null, $"old synthetic root still present: {oldRoot}", problems);

            foreach (string required in new[] { "Sacat", "Franki", "MontineFarm", "FarmSafehouse", "FarmPlot_00", "NPC_Brakes", "NPC_Vagrant", "NPC_BoatMan", "MooredBoat", "MissionSystem", "NavMeshSurface" })
                Require(FindAnywhere(required) != null, $"required gameplay role missing: {required}", problems);

            if (world != null)
            {
                Transform[] mapItems = world.GetComponentsInChildren<Transform>(true);
                Transform lalayRoot = mapItems.FirstOrDefault(t => t.name == "Lalay_Dense_House_Massing");
                Transform highlandRoot = mapItems.FirstOrDefault(t => t.name == "Highland_Sparse_House_Massing");
                int lalayHouses = lalayRoot?.childCount ?? 0;
                int highlandHouses = highlandRoot?.childCount ?? 0;
                int tallHighland = highlandRoot == null ? 0 : highlandRoot.Cast<Transform>()
                    .Count(t => t.name.Contains("TwoStorey") || t.name.Contains("SmallApartment"));
                int futureFarms = mapItems.Count(t => t.name.StartsWith("Future_Farm_Parcel_", StringComparison.Ordinal));
                int roadColliders = mapItems.Count(t => t.GetComponent<Collider>() != null && HasAncestor(t, "Roads_OSM"));
                int bridgeColliders = mapItems.Count(t => t.GetComponent<Collider>() != null && HasAncestor(t, "Bridges"));
                // MINI-101 intentionally replaces a small number of house placeholders
                // with roadside shop lots while preserving a dense two-sided street.
                int roadsideStalls = mapItems.Length == 0 ? 0 : UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Count(transform => transform.name.StartsWith("Stall_", StringComparison.Ordinal));
                Require(lalayHouses >= 100 && lalayHouses + roadsideStalls >= 110,
                    $"Lalay occupied-lot density regressed after shop replacements: houses={lalayHouses}, stalls={roadsideStalls}", problems);
                Require(highlandHouses >= 18, $"Highland density regressed: {highlandHouses}", problems);
                Require(tallHighland >= 9, $"Highland needs two-storey/apartment massing: {tallHighland}", problems);
                Require(futureFarms >= 7, $"future farm placeholders regressed: {futureFarms}", problems);
                Require(roadColliders >= 9, $"driveable road collider coverage regressed: {roadColliders}", problems);
                Require(bridgeColliders >= 4, $"bridge collider coverage regressed: {bridgeColliders}", problems);
            }

            Require(UnityEngine.Object.FindObjectsByType<FarmPlot>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length >= 14,
                "gameplay farm plots did not survive migration", problems);
            Require(UnityEngine.Object.FindObjectsByType<TownNPCInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length >= 8,
                "town NPC gameplay roles did not survive migration", problems);

            ValidateRoadsideShops(problems);
            ValidateLalayRolesAndSafehouses(problems);
            ValidateHighlandFarm(problems);
            ValidateRoadJoin("Road_way_22917921", "Road_user_highland_lalay_inroad", "Lalay to Highland inroad", problems);
            ValidateRoadJoin("Road_user_highland_lalay_inroad", "Road_user_lalay_inland_coastal_connector", "Highland inroad to connector", problems);
            ValidateRoadJoin("Road_user_highland_lalay_inroad", "Road_user_highland_farm_spur", "Highland inroad to farm spur", problems);
            ValidateRoadJoin("Road_way_22917921", "Road_user_lalay_backstreet", "Lalay to south Backstreet", problems);
            ValidateBackstreetSide(problems);
            ValidateMissionRolePlacement(problems);
            ValidateMiniMap(problems);

            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                    if (transform.gameObject.GetComponents<Component>().Any(component => component == null))
                        problems.Add("missing script: " + HierarchyPath(transform));
            return problems;
        }

        private static void ValidateRoadsideShops(List<string> problems)
        {
            string[] stalls =
            {
                "Stall_FARM SHOP", "Stall_PRODUCE BUYER", "Stall_FOOD", "Stall_CLOTHES",
                "Stall_PHARMACY", "Stall_LAND AND SURVEYS", "Stall_CAR DEALER"
            };
            foreach (string name in stalls)
            {
                GameObject stall = FindAnywhere(name);
                Require(stall != null, $"roadside stall missing: {name}", problems);
                if (stall == null) continue;
                float x = stall.GetComponentsInChildren<Renderer>(true).Select(renderer => renderer.bounds.center.x).DefaultIfEmpty(stall.transform.position.x).Average();
                Vector3 centre = stall.GetComponentsInChildren<Renderer>(true).Select(renderer => renderer.bounds.center).DefaultIfEmpty(stall.transform.position).Aggregate(Vector3.zero, (sum, point) => sum + point)
                    / Mathf.Max(1, stall.GetComponentsInChildren<Renderer>(true).Length);
                Vector3 road = ApproximateLalayCentre(x);
                Require(HorizontalDistance(centre, road) >= 7.0f, $"{name} is still inside the road/sidewalk corridor ({HorizontalDistance(centre, road):0.00}m)", problems);
            }
        }

        private static void ValidateHighlandFarm(List<string> problems)
        {
            FarmPlot[] plots = UnityEngine.Object.FindObjectsByType<FarmPlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (plots.Length == 0) return;
            float minY = plots.Min(plot => plot.GetComponent<Renderer>()?.bounds.min.y ?? plot.transform.position.y);
            float maxY = plots.Max(plot => plot.GetComponent<Renderer>()?.bounds.min.y ?? plot.transform.position.y);
            Require(maxY - minY <= 0.25f, $"Highland farm plots are not level: delta={maxY - minY:0.00}m", problems);
            Vector3 centre = plots.Select(plot => plot.transform.position).Aggregate(Vector3.zero, (sum, point) => sum + point) / plots.Length;
            Require(centre.x >= 106f, $"Highland farm still blocks the inroad: centre x={centre.x:0.00}", problems);

            GameObject safehouse = FindAnywhere("FarmSafehouse_Building");
            if (safehouse != null)
            {
                Vector3 road = new Vector3(84f, safehouse.transform.position.y, -128f);
                Require(HorizontalDistance(safehouse.transform.position, road) >= 20f, "farm safehouse is still blocking the Highland road", problems);
            }
        }

        private static void ValidateLalayRolesAndSafehouses(List<string> problems)
        {
            string[] stationary =
            {
                "NPC_FarmShop", "NPC_Buyer", "NPC_FoodShop", "NPC_ApparelShop", "NPC_Pharmacy", "NPC_LandOffice",
                "NPC_CarDealer", "NPC_Normy", "NPC_Vagrant", "NPC_BlackMarket", "NPC_BossJ", "NPC_BossC",
                "NPC_GangRecruiter", "NPC_Villager", "NPC_Police", "NPC_PoliceShops"
            };
            foreach (string name in stationary)
            {
                GameObject npc = FindAnywhere(name);
                if (npc == null) continue;
                Vector3 road = ApproximateLalayCentre(npc.transform.position.x);
                Require(HorizontalDistance(npc.transform.position, road) >= 6.0f,
                    $"{name} remains in the Lalay vehicle lane ({HorizontalDistance(npc.transform.position, road):0.00}m from centre)", problems);
            }

            GameObject rover = FindAnywhere("BossC_SUV");
            if (rover != null)
            {
                Renderer[] roverRenderers = rover.GetComponentsInChildren<Renderer>(true);
                Vector3 roverCentre = roverRenderers.Length == 0 ? rover.transform.position : roverRenderers[0].bounds.center;
                if (roverRenderers.Length > 0)
                {
                    Bounds roverBounds = roverRenderers[0].bounds;
                    for (int i = 1; i < roverRenderers.Length; i++) roverBounds.Encapsulate(roverRenderers[i].bounds);
                    roverCentre = roverBounds.center;
                }
                Vector3 road = ApproximateLalayCentre(roverCentre.x);
                Require(HorizontalDistance(roverCentre, road) >= 7.0f, "Boss C Range Rover remains in the Lalay vehicle lane", problems);
                Vector3 tangent = ApproximateLalayCentre(roverCentre.x + 2f) - ApproximateLalayCentre(roverCentre.x - 2f);
                tangent.y = 0f;
                float parallelAngle = Mathf.Min(
                    Vector3.Angle(rover.transform.forward, tangent),
                    Vector3.Angle(rover.transform.forward, -tangent));
                Require(parallelAngle <= 20f,
                    $"Boss C Range Rover is not parked parallel to Lalay (angle={parallelAngle:0.0}, centre={roverCentre}, forward={rover.transform.forward}, tangent={tangent.normalized})", problems);
            }

            GameObject lalayHouse = FindAnywhere("LalayHouse");
            Require(lalayHouse != null && lalayHouse.GetComponentsInChildren<Transform>(true).Any(t => t.name == "LalaySafehouse_TwoStorey"),
                "Lalay safehouse is not the required two-storey house", problems);
            GameObject farmSafehouse = FindAnywhere("FarmSafehouse_Rest");
            CharacterSwitchManager switcher = UnityEngine.Object.FindFirstObjectByType<CharacterSwitchManager>(FindObjectsInactive.Include);
            if (farmSafehouse != null && switcher != null)
            {
                SerializedObject so = new SerializedObject(switcher);
                Vector3 spawn = so.FindProperty("safehouseSpawn").vector3Value;
                Require(HorizontalDistance(spawn, farmSafehouse.transform.position) <= 2f,
                    "default respawn is not the Highland safehouse", problems);
            }
        }

        private static void ValidateBackstreetSide(List<string> problems)
        {
            GameObject backstreet = FindAnywhere("Road_user_lalay_backstreet");
            if (backstreet == null) return;
            Renderer renderer = backstreet.GetComponentInChildren<Renderer>(true);
            if (renderer == null) return;
            Vector3 centre = renderer.bounds.center;
            Vector3 lalay = ApproximateLalayCentre(centre.x);
            Require(centre.z <= lalay.z - 12f,
                $"Backstreet is on the wrong side of Lalay (backstreet z={centre.z:0.0}, Lalay z={lalay.z:0.0})", problems);
        }

        private static void ValidateMissionRolePlacement(List<string> problems)
        {
            foreach (string name in new[] { "NPC_BossJ", "NPC_Normy" })
            {
                GameObject npc = FindAnywhere(name);
                Require(npc != null && npc.GetComponent<PatrolNPC>() != null, $"{name} does not have its short Lalay walking beat", problems);
            }
            Require(FindAnywhere("NPC_Police")?.GetComponent<PoliceOfficer>() != null, "Lalay police patrol is missing", problems);

            GameObject church = FindAnywhere("GrandBay_Catholic_Church_Graybox");
            GameObject brakes = FindAnywhere("NPC_Brakes");
            if (church != null && brakes != null)
                Require(HorizontalDistance(church.transform.position, brakes.transform.position) <= 20f, "Brakes is not beside the church", problems);

            int dogLife = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Count(transform => transform.name.StartsWith("NPC_DogLife_", StringComparison.Ordinal));
            Require(dogLife >= 4, $"Dog Life Lalay block is incomplete: {dogLife}/4", problems);
            Require(FindAnywhere("NPC_GangRecruiter") != null && FindAnywhere("NotAhWord_Zoomy") != null,
                "Not Ah Word Lalay block is incomplete", problems);
        }

        private static void ValidateMiniMap(List<string> problems)
        {
            GtaMiniMapController miniMap = UnityEngine.Object.FindFirstObjectByType<GtaMiniMapController>(FindObjectsInactive.Include);
            Require(miniMap != null, "GTA-style minimap controller missing", problems);
            int markers = UnityEngine.Object.FindObjectsByType<GtaMiniMapMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            Require(markers >= 18, $"minimap role coverage is incomplete: {markers} markers", problems);
            Require(FindAnywhere("GTA_MinimapCamera")?.GetComponent<Camera>() != null, "minimap camera missing", problems);
            Require(FindAnywhere("PoliceHeatOverlay") != null, "50% police-heat minimap overlay missing", problems);
        }

        private static void ValidateRoadJoin(string firstName, string secondName, string label, List<string> problems)
        {
            GameObject first = FindAnywhere(firstName);
            GameObject second = FindAnywhere(secondName);
            if (first == null || second == null)
            {
                problems.Add($"{label} road mesh missing");
                return;
            }
            Vector3[] a = WorldVertices(first);
            Vector3[] b = WorldVertices(second);
            float bestHorizontal = float.MaxValue;
            float bestVertical = float.MaxValue;
            foreach (Vector3 av in a)
            foreach (Vector3 bv in b)
            {
                float horizontal = HorizontalDistance(av, bv);
                if (horizontal >= bestHorizontal) continue;
                bestHorizontal = horizontal;
                bestVertical = Mathf.Abs(av.y - bv.y);
            }
            Require(bestHorizontal <= 2.5f, $"{label} has a horizontal gap of {bestHorizontal:0.00}m", problems);
            Require(bestVertical <= 0.30f, $"{label} has a vertical step of {bestVertical:0.00}m", problems);
        }

        private static Vector3[] WorldVertices(GameObject root)
        {
            return root.GetComponentsInChildren<MeshFilter>(true)
                .SelectMany(filter => filter.sharedMesh == null ? Array.Empty<Vector3>() : filter.sharedMesh.vertices.Select(vertex => filter.transform.TransformPoint(vertex)))
                .ToArray();
        }

        private static Vector3 ApproximateLalayCentre(float x)
        {
            if (x <= LalaySpine[0].x) return new Vector3(x, 0f, LalaySpine[0].y);
            for (int i = 1; i < LalaySpine.Length; i++)
            {
                if (x > LalaySpine[i].x) continue;
                float t = Mathf.InverseLerp(LalaySpine[i - 1].x, LalaySpine[i].x, x);
                return new Vector3(x, 0f, Mathf.Lerp(LalaySpine[i - 1].y, LalaySpine[i].y, t));
            }
            return new Vector3(x, 0f, LalaySpine[LalaySpine.Length - 1].y);
        }
        private static float HorizontalDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        private static void CaptureView(string fileName, Vector3 position, Vector3 target, int width, int height, bool orthographic, float size)
        {
            GameObject cameraObject = new GameObject("MINI100_EvidenceCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = position;
            camera.transform.LookAt(target);
            camera.fieldOfView = 58f;
            camera.farClipPlane = 900f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.14f, 0.18f);
            camera.orthographic = orthographic;
            camera.orthographicSize = size;
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 2 };
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(EvidenceFolder, fileName), image.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }

        private static bool HasAncestor(Transform transform, string name)
        {
            while (transform != null) { if (transform.name == name) return true; transform = transform.parent; }
            return false;
        }

        private static GameObject FindAnywhere(string name)
        {
            foreach (Transform transform in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (transform.name == name) return transform.gameObject;
            return null;
        }

        private static GameObject FindRoot(Scene scene, string name) => scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
        private static void Require(bool condition, string problem, List<string> problems) { if (!condition) problems.Add(problem); }
        private static string HierarchyPath(Transform transform) => transform.parent == null ? transform.name : HierarchyPath(transform.parent) + "/" + transform.name;
    }
}
