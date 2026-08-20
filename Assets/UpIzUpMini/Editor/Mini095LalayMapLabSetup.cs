using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UpIzUpMini.Editor
{
    public static class Mini095LalayMapLabSetup
    {
        private const string DataPath = "Assets/UpIzUpMini/Maps/GrandBayPhase1MapData.json";
        private const string HeightPath = "Assets/UpIzUpMini/Maps/GrandBayPhase1Height.json";
        private const string ScenePath = "Assets/UpIzUpMini/Scenes/MapLab_LalayHighland.unity";
        private const string MaterialFolder = "Assets/UpIzUpMini/Maps/MapLab/Materials";
        private static string EvidenceFolder => Path.Combine("Logs", "Tasks",
            Environment.GetEnvironmentVariable("UPIZUP_EVIDENCE_TASK") ?? "MINI-099");
        private static readonly Rect PhaseOneBounds = new Rect(-75f, -225f, 350f, 210f);
        private static readonly HashSet<string> PhaseOneSupportingRoadIds = new HashSet<string>
        {
            // A deliberately small, connected phase-one network. The full OSM road
            // catalogue stays in JSON for future districts but is not mass-rendered.
            "user/lalay_inland_coastal_connector",
            "user/lalay_backstreet",
            "user/highland_lalay_inroad",
            "user/highland_farm_spur",
            "way/254679575",
            "way/25802980",
            "way/180962530",
            "way/387239000"
        };
        private static readonly int[] ShantyVariants = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };
        private const float ShantyScale = 2.1f;

        [Serializable] private sealed class MapData
        {
            public int schemaVersion;
            public float compression = 0.3333333f;
            public string sourceAttribution;
            public string[] lalayRoadIds;
            public RoadData[] roads;
            public WaterwayData[] waterways;
            public CoastlineData coastline;
            public ZoneData[] zones;
            public AnchorData[] anchors;
        }

        [Serializable] private sealed class RoadData { public string id; public string name; public string roadClass; public string surface; public PointData[] points; }
        [Serializable] private sealed class WaterwayData { public string id; public string name; public string waterClass; public PointData[] points; }
        [Serializable] private sealed class CoastlineData { public PointData[] points; }
        [Serializable] private sealed class ZoneData { public string id; public string displayName; public PointData[] points; }
        [Serializable] private sealed class AnchorData { public string id; public string displayName; public string category; public float x; public float z; public bool userVerified; }
        [Serializable] private sealed class PointData { public float x; public float z; }
        [Serializable] private sealed class HeightData
        {
            public float south; public float west; public float north; public float east;
            public float originLatitude; public float originLongitude;
            public int width; public int height;
            public float minElevationMetres; public float maxElevationMetres;
            public float horizontalCompression; public float verticalCompression;
            public float[] samplesSouthToNorth;
        }

        private static HeightData s_heightData;
        private static float s_compression = 0.3333333f;
        private static readonly List<Vector3[]> s_lalayGradeLines = new List<Vector3[]>();
        private static readonly List<Vector3[]> s_driveableGradeLines = new List<Vector3[]>();
        private static readonly List<(Vector3[] points, float halfWidth)> s_roadClearances = new List<(Vector3[], float)>();
        private static float s_lalayBaseHeight;
        private static Vector3 s_farmPadCentre;
        private static float s_farmPadHeight;
        private static readonly Vector2 FarmPadHalfSize = new Vector2(24f, 18f);
        private static Material s_houseDoorMaterial;
        private static Material s_houseWindowMaterial;

        [MenuItem("Up Iz Up Mini/MINI-095/Build Lalay Highland Map Lab")]
        public static void BuildScene()
        {
            MapData data = LoadData();
            s_heightData = LoadHeightData();
            s_compression = data.compression;
            PrepareRoadGeometry(data);
            EnsureFolder("Assets/UpIzUpMini/Maps/MapLab");
            EnsureFolder(MaterialFolder);

            Material terrainMat = MaterialAsset("Terrain", new Color(0.20f, 0.38f, 0.19f));
            Material seaMat = MaterialAsset("Sea", new Color(0.05f, 0.33f, 0.48f));
            Material mainRoadMat = MaterialAsset("MainRoad", new Color(0.17f, 0.18f, 0.17f));
            Material sideRoadMat = MaterialAsset("SideRoad", new Color(0.28f, 0.29f, 0.27f));
            Material trackMat = MaterialAsset("DirtTrack", new Color(0.43f, 0.27f, 0.12f));
            Material sidewalkMat = MaterialAsset("ConcreteSidewalk", new Color(0.57f, 0.58f, 0.56f));
            Material waterwayMat = MaterialAsset("Waterway", new Color(0.10f, 0.48f, 0.65f));
            Material farmMat = MaterialAsset("FarmSoil", new Color(0.34f, 0.19f, 0.08f));
            Material jettyMat = MaterialAsset("Jetty", new Color(0.33f, 0.20f, 0.10f));
            Material bridgeMat = MaterialAsset("BridgeDeck", new Color(0.25f, 0.27f, 0.27f));
            Material bridgeRailMat = MaterialAsset("BridgeRail", new Color(0.72f, 0.74f, 0.72f));
            Material barrierMat = MaterialAsset("FutureExpansionBarrier", new Color(0.91f, 0.42f, 0.08f));
            Material churchWallMat = MaterialAsset("ChurchWall", new Color(0.82f, 0.78f, 0.67f));
            Material churchRoofMat = MaterialAsset("ChurchRoof", new Color(0.48f, 0.12f, 0.10f));
            Material baySandMat = MaterialAsset("BaySand", new Color(0.72f, 0.60f, 0.40f));
            Material bayStoneMat = MaterialAsset("BayStone", new Color(0.34f, 0.35f, 0.33f));
            Material roofRed = MaterialAsset("RoofRed", new Color(0.60f, 0.12f, 0.09f));
            Material roofBlue = MaterialAsset("RoofBlue", new Color(0.10f, 0.38f, 0.58f));
            Material roofSilver = MaterialAsset("RoofSilver", new Color(0.64f, 0.66f, 0.64f));
            Material[] wallMats =
            {
                MaterialAsset("WallCream", new Color(0.78f, 0.70f, 0.54f)),
                MaterialAsset("WallMint", new Color(0.43f, 0.66f, 0.52f)),
                MaterialAsset("WallPink", new Color(0.70f, 0.43f, 0.48f)),
                MaterialAsset("WallBlue", new Color(0.38f, 0.55f, 0.68f)),
                MaterialAsset("WallConcrete", new Color(0.50f, 0.49f, 0.46f))
            };
            Material[] roofMats = { roofRed, roofBlue, roofSilver };
            // Reuse two district-wide materials so facade detail does not add
            // a unique material/draw-call family to the mobile graybox.
            s_houseDoorMaterial = trackMat;
            s_houseWindowMaterial = waterwayMat;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("MapLab_LalayHighland");
            GameObject geography = Child(root, "Geography");
            GameObject roadRoot = Child(root, "Roads_OSM");
            GameObject sidewalkRoot = Child(root, "Lalay_Sidewalks");
            GameObject bridgeRoot = Child(root, "Bridges");
            GameObject boundaryRoot = Child(root, "PhaseOne_Boundaries");
            GameObject districtRoot = Child(root, "Approved_Districts_And_Lots");
            GameObject housesRoot = Child(root, "Lalay_Dense_House_Massing");
            GameObject highlandHousesRoot = Child(root, "Highland_Sparse_House_Massing");
            GameObject bayShoreRoot = Child(root, "Bay_Sand_And_Stones");
            GameObject labelsRoot = Child(root, "Labels");

            Bounds bounds = CalculateBounds(data);
            BuildTerrain(bounds, terrainMat, geography.transform);
            BuildSea(data, bounds, seaMat, geography.transform);

            HashSet<string> lalayIds = new HashSet<string>(data.lalayRoadIds ?? Array.Empty<string>());
            foreach (RoadData road in data.roads ?? Array.Empty<RoadData>())
            {
                if (!ShouldRenderRoad(data, road, lalayIds)) continue;
                bool lalay = lalayIds.Contains(road.id);
                float width = RoadWidth(road.roadClass, lalay);
                Material material = road.roadClass == "track" || road.roadClass == "path" ? trackMat : (lalay || road.roadClass == "secondary" || road.roadClass == "tertiary" ? mainRoadMat : sideRoadMat);
                CreateRibbon("Road_" + SafeName(road.id), road.points, data.compression, width, 0f, 0.62f, material, roadRoot.transform, true, lalay ? 0.015f : IsDriveableRoad(road) ? 0.10f : -1f);
                if (lalay)
                {
                    CreateRibbon("Lalay_Sidewalk_Left_" + SafeName(road.id), road.points, data.compression, 1.25f, 3.85f, 0.76f, sidewalkMat, sidewalkRoot.transform, true, 0.015f);
                    CreateRibbon("Lalay_Sidewalk_Right_" + SafeName(road.id), road.points, data.compression, 1.25f, -3.85f, 0.76f, sidewalkMat, sidewalkRoot.transform, true, 0.015f);
                }
            }
            int connectorCount = BuildRoadGapConnectors(data, sideRoadMat, roadRoot.transform);

            foreach (WaterwayData waterway in data.waterways ?? Array.Empty<WaterwayData>())
            {
                if (waterway.points == null || waterway.points.Length < 2) continue;
                CreateRibbon("Waterway_" + SafeName(waterway.id), waterway.points, data.compression, waterway.waterClass == "river" ? 3.5f : 1.4f, 0f, 0.12f, waterwayMat, geography.transform, false);
            }
            int bridgeCount = BuildBridges(data, bridgeMat, bridgeRailMat, bridgeRoot.transform);
            int blockedExitCount = BuildPhaseOneBoundaries(data, barrierMat, boundaryRoot.transform);

            // Highland stays in data as an approved district, but the user's pink
            // annotation was only a planning highlight and must not render in-game.

            Dictionary<string, AnchorData> anchors = (data.anchors ?? Array.Empty<AnchorData>()).ToDictionary(a => a.id, a => a);
            BuildLandmarkLots(anchors, data.compression, districtRoot.transform, labelsRoot.transform, farmMat, jettyMat);
            BuildChurch(anchors, data.compression, churchWallMat, churchRoofMat, districtRoot.transform, labelsRoot.transform);
            BuildBayShoreline(baySandMat, bayStoneMat, bayShoreRoot.transform);
            BuildDenseLalayHouses(data, lalayIds, anchors, wallMats, roofMats, housesRoot.transform);
            BuildSparseHighlandHouses(data, anchors, wallMats, roofMats, highlandHousesRoot.transform);

            BuildLighting();
            BuildCameras(data, anchors, data.compression, bounds);
            CreateGroundLabel("LALAY — NARROW TWO-LANE HILL STREET", AnchorPosition(anchors, "grand_bay_credit_union", data.compression) + new Vector3(-40f, 0f, -12f), Color.white, labelsRoot.transform, 5.5f);
            CreateGroundLabel("BAY / FLAT END", AnchorPosition(anchors, "story_jetty", data.compression) + new Vector3(-15f, 0f, 18f), Color.white, labelsRoot.transform, 4.5f);

            EditorSceneManager.MarkSceneDirty(scene);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/UpIzUpMini/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"MINI-095 MAP LAB BUILD PASS: {data.roads.Length} sourced road polylines + {connectorCount} gap connectors, {bridgeCount} bridges, {blockedExitCount} future exits blocked, {housesRoot.transform.childCount} Lalay houses, {highlandHousesRoot.transform.childCount} Highland houses, separate scene {ScenePath}.");
        }

        [MenuItem("Up Iz Up Mini/MINI-095/Validate Map Lab")]
        public static void Validate()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject root = GameObject.Find("MapLab_LalayHighland");
            Require(root != null, "MapLab root missing");
            Require(GameObject.Find("Outline_Highland_First_Farm") != null && GameObject.Find("Farm_Plot_Starting_Active") != null, "Highland farm missing");
            Require(GameObject.Find("Copernicus_GLO30_Terrain") != null, "Copernicus DEM terrain missing");
            Require(GameObject.Find("Story_Jetty") != null, "Story jetty missing");
            Require(GameObject.Find("GrandBay_Catholic_Church_Graybox") != null, "Mapped bay church missing");
            Require(GameObject.Find("Bay_Sand_Patch") != null, "Sand-and-stone bay treatment missing");
            Require(GameObject.Find("Zone_Highland_UserApproved_Outline") == null, "Planning-only pink Highland outline must not render");
            Require(GameObject.Find("Road_user_lalay_backstreet") != null && GameObject.Find("Road_user_lalay_backstreet").GetComponent<MeshCollider>() != null,
                "Backstreet must be a continuous collidable road");
            Require(GameObject.Find("Camera_Overview") != null && GameObject.Find("Camera_Lalay") != null && GameObject.Find("Camera_Highland") != null && GameObject.Find("Camera_Bay") != null, "Fixed cameras missing");
            int roads = GameObject.Find("Roads_OSM")?.transform.childCount ?? 0;
            int sidewalks = GameObject.Find("Lalay_Sidewalks")?.transform.childCount ?? 0;
            int houses = GameObject.Find("Lalay_Dense_House_Massing")?.transform.childCount ?? 0;
            int highlandHouses = GameObject.Find("Highland_Sparse_House_Massing")?.transform.childCount ?? 0;
            int roadColliders = GameObject.Find("Roads_OSM")?.GetComponentsInChildren<MeshCollider>(true).Length ?? 0;
            int bridges = GameObject.Find("Bridges")?.transform.childCount ?? 0;
            int bridgeColliders = GameObject.Find("Bridges")?.GetComponentsInChildren<Collider>(true).Length ?? 0;
            int blockedExits = GameObject.Find("PhaseOne_Boundaries")?.transform.Cast<Transform>().Count(child => child.name.StartsWith("FutureExitBarrier_", StringComparison.Ordinal)) ?? 0;
            Require(roads >= 8 && roads <= 12, $"Curated phase-one network must stay simple: {roads}");
            Require(roadColliders == roads, $"Every road must remain passable/collidable: roads={roads}, colliders={roadColliders}");
            Require(bridges >= 1 && bridgeColliders >= bridges, $"River crossings need collidable bridges: bridges={bridges}, colliders={bridgeColliders}");
            foreach (Transform bridge in GameObject.Find("Bridges").transform)
            {
                Transform deck = bridge.Find("Driveable_Deck");
                Transform left = bridge.Find("Guard_Rail_Left");
                Transform right = bridge.Find("Guard_Rail_Right");
                Require(deck != null && left != null && right != null, $"Bridge parts missing on {bridge.name}");
                Require(deck.GetComponent<MeshCollider>() != null, $"Bridge deck must be driveable on {bridge.name}");
                Require(Mathf.Abs(left.localScale.z - right.localScale.z) < 0.01f && Mathf.Abs(left.localScale.x - right.localScale.x) < 0.01f,
                    $"Bridge rails must have equal spacing/length on {bridge.name}");
                Vector3 railMidpoint = (left.position + right.position) * 0.5f;
                Vector3 deckCentre = deck.GetComponent<Renderer>().bounds.center;
                Require(HorizontalDistance(railMidpoint, deckCentre) < 1.5f, $"Bridge rails must be centred evenly around {bridge.name}");
            }
            Require(blockedExits >= 1, "Later-version road exits must be visibly blocked");
            Require(sidewalks >= 2, $"Lalay sidewalks missing: {sidewalks}");
            Require(houses >= 105 && houses <= 125, $"Dense Lalay massing must fill both sides without exceeding the mobile graybox budget: {houses}");
            Require(highlandHouses >= 10 && highlandHouses <= 18, $"Highland housing should be moderately populated without matching Lalay density: {highlandHouses}");
            int highlandApartments = GameObject.Find("Highland_Sparse_House_Massing")?.transform.Cast<Transform>().Count(child => child.name.Contains("SmallApartment")) ?? 0;
            int highlandTallHomes = GameObject.Find("Highland_Sparse_House_Massing")?.transform.Cast<Transform>().Count(child => child.name.Contains("TwoStorey") || child.name.Contains("SmallApartment")) ?? 0;
            Require(highlandApartments >= 3, $"Highland needs several small apartment houses: {highlandApartments}");
            Require(highlandTallHomes >= highlandHouses / 2, $"Most Highland buildings should be two-storey or small apartments: tall={highlandTallHomes}, total={highlandHouses}");
            int shanties = GameObject.Find("Lalay_Dense_House_Massing")?.transform.Cast<Transform>().Count(child => child.name.Contains("Shanty")) ?? 0;
            Require(shanties >= 15 && shanties <= houses / 2, $"Shanties should remain a minority but still be represented: shanties={shanties}, houses={houses}");
            int sideA = GameObject.Find("Lalay_Dense_House_Massing")?.transform.Cast<Transform>().Count(child => child.name.Contains("_SideA_")) ?? 0;
            int sideB = GameObject.Find("Lalay_Dense_House_Massing")?.transform.Cast<Transform>().Count(child => child.name.Contains("_SideB_")) ?? 0;
            Require(sideA >= 36 && sideB >= 36 && Mathf.Max(sideA, sideB) <= Mathf.Min(sideA, sideB) * 1.5f, $"Both sides of Lalay must be dense and reasonably balanced around real intersections: SideA={sideA}, SideB={sideB}");
            Require(GameObject.Find("Lalay_Dense_House_Massing")?.transform.Cast<Transform>().All(child => !(child.position.x > 165f && child.position.z < -140f)) ?? false, "Lower bay/jetty exclusion must contain no houses");
            Require(GameObject.Find("Lalay_Dense_House_Massing")?.transform.Cast<Transform>().All(child => child.position.x <= 200f) ?? false, "Eastern coastal no-house strip must contain no houses");
            int meshes = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            int materials = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(r => r.sharedMaterial).Where(m => m != null).Distinct().Count();
            Debug.Log($"MINI-095 MAP LAB VALIDATION PASS: roads={roads}, roadColliders={roadColliders}, bridges={bridges}, bridgeColliders={bridgeColliders}, blockedExits={blockedExits}, sidewalks={sidewalks}, LalayHouses={houses}, HighlandHouses={highlandHouses}, meshRenderers={meshes}, sharedMaterials={materials}, scene={scene.path}.");
        }

        [MenuItem("Up Iz Up Mini/MINI-095/Capture Map Lab Screenshots")]
        public static void CaptureScreenshots()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Directory.CreateDirectory(EvidenceFolder);
            Capture("Camera_Overview", "MapLab-Overview-1600x1000.png", 1600, 1000);
            Capture("Camera_Lalay", "MapLab-Lalay-1280x720.png", 1280, 720);
            Capture("Camera_Highland", "MapLab-Highland-1280x720.png", 1280, 720);
            Capture("Camera_Bay", "MapLab-Bay-1280x720.png", 1280, 720);
            Debug.Log("MINI-095 MAP LAB CAPTURE PASS: overview, Lalay, Highland, and bay gameplay-distance screenshots saved.");
        }

        public static void ValidateAndCapture()
        {
            Validate();
            CaptureScreenshots();
        }

        public static void BuildValidateCapture()
        {
            BuildScene();
            Validate();
            CaptureScreenshots();
        }

        private static MapData LoadData()
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(DataPath);
            if (asset == null) throw new InvalidOperationException("Missing map data: " + DataPath);
            MapData data = JsonUtility.FromJson<MapData>(asset.text);
            if (data == null || data.roads == null || data.roads.Length == 0) throw new InvalidOperationException("Map data contains no roads.");
            return data;
        }

        private static HeightData LoadHeightData()
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(HeightPath);
            if (asset == null) throw new InvalidOperationException("Missing height data: " + HeightPath);
            HeightData data = JsonUtility.FromJson<HeightData>(asset.text);
            if (data == null || data.samplesSouthToNorth == null || data.samplesSouthToNorth.Length != data.width * data.height)
                throw new InvalidOperationException("Height data dimensions do not match sample count.");
            return data;
        }

        private static float HeightAt(float x, float z)
        {
            float rawHeight = RawHeightAt(x, z);
            Vector3 query = new Vector3(x, 0f, z);
            float result = rawHeight;
            if (TryNearestGrade(query, s_driveableGradeLines, out float roadHeight, out float closestDistance) && closestDistance < 55f)
            {
                // One shared terrain/road grade prevents the Lalay/Highland junction
                // from choosing the Lalay height for the ground and a second height
                // for the connector. The wide falloff removes the cliff walls while
                // retaining the surrounding Dominica relief outside gameplay space.
                float roadBlend = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(14f, 55f, closestDistance));
                result = Mathf.Lerp(rawHeight, roadHeight, roadBlend);
            }

            // Farming needs a genuinely level working surface, not a visual slab
            // sitting over a slope. Blend a bounded flat pad back into the graded
            // Highland terrain; gameplay plots/safehouse are placed inside it.
            float padEdgeDistance = Mathf.Max(
                Mathf.Abs(x - s_farmPadCentre.x) - FarmPadHalfSize.x,
                Mathf.Abs(z - s_farmPadCentre.z) - FarmPadHalfSize.y);
            if (padEdgeDistance < 14f)
            {
                float padBlend = padEdgeDistance <= 0f ? 1f : 1f - Mathf.SmoothStep(0f, 1f, padEdgeDistance / 14f);
                result = Mathf.Lerp(result, s_farmPadHeight, padBlend);
            }
            return result;
        }

        private static bool TryNearestGrade(Vector3 query, IEnumerable<Vector3[]> lines, out float height, out float distance)
        {
            distance = float.MaxValue;
            height = 0f;
            foreach (Vector3[] line in lines)
            for (int i = 1; i < line.Length; i++)
            {
                float t;
                float candidate = DistanceToSegmentXZ(query, line[i - 1], line[i], out t);
                if (candidate >= distance) continue;
                distance = candidate;
                height = Mathf.Lerp(line[i - 1].y, line[i].y, t);
            }
            return distance < float.MaxValue;
        }

        private static float RawHeightAt(float x, float z)
        {
            if (s_heightData == null) return 0f;
            float realX = x / s_compression;
            float realZ = z / s_compression;
            float longitude = s_heightData.originLongitude + realX / 107500f;
            float latitude = s_heightData.originLatitude + realZ / 110650f;
            float gx = Mathf.Clamp01((longitude - s_heightData.west) / (s_heightData.east - s_heightData.west)) * (s_heightData.width - 1);
            float gz = Mathf.Clamp01((latitude - s_heightData.south) / (s_heightData.north - s_heightData.south)) * (s_heightData.height - 1);
            int x0 = Mathf.FloorToInt(gx); int z0 = Mathf.FloorToInt(gz);
            int x1 = Mathf.Min(x0 + 1, s_heightData.width - 1); int z1 = Mathf.Min(z0 + 1, s_heightData.height - 1);
            float tx = gx - x0; float tz = gz - z0;
            float a = Mathf.Lerp(SampleHeight(x0, z0), SampleHeight(x1, z0), tx);
            float b = Mathf.Lerp(SampleHeight(x0, z1), SampleHeight(x1, z1), tx);
            float elevation = Mathf.Lerp(a, b, tz);
            return Mathf.Max(0f, (elevation - s_heightData.minElevationMetres) * s_heightData.verticalCompression);
        }

        private static void PrepareRoadGeometry(MapData data)
        {
            s_lalayGradeLines.Clear();
            s_driveableGradeLines.Clear();
            s_roadClearances.Clear();
            HashSet<string> lalayIds = new HashSet<string>(data.lalayRoadIds ?? Array.Empty<string>());
            foreach (RoadData road in data.roads ?? Array.Empty<RoadData>())
            {
                if (!ShouldRenderRoad(data, road, lalayIds)) continue;
                Vector3[] points = road.points.Select(p =>
                {
                    float x = p.x * data.compression;
                    float z = p.z * data.compression;
                    return new Vector3(x, RawHeightAt(x, z), z);
                }).ToArray();
                bool lalay = lalayIds.Contains(road.id);
                if (lalay) s_lalayGradeLines.Add(points);
                // Lalay houses are deliberately close to the main street. Its known
                // setback already keeps them off that road, so only crossing/side roads
                // participate in the overlap rejection.
                if (!lalay) s_roadClearances.Add((points, RoadWidth(road.roadClass, false) * 0.5f));
            }
            SmoothLalayGradeLines();
            foreach (Vector3[] line in s_lalayGradeLines) s_driveableGradeLines.Add(line);
            foreach (RoadData road in (data.roads ?? Array.Empty<RoadData>()).Where(r => ShouldRenderRoad(data, r, lalayIds) && IsDriveableRoad(r) && !lalayIds.Contains(r.id)))
            {
                Vector3[] raw = road.points.Select(p =>
                {
                    float x = p.x * data.compression;
                    float z = p.z * data.compression;
                    return new Vector3(x, RawHeightAt(x, z), z);
                }).ToArray();
                float maximumSlope = road.id == "user/lalay_inland_coastal_connector" || road.id == "user/lalay_backstreet" ? 0.025f
                    : road.id == "user/highland_lalay_inroad" ? 0.03f
                    : road.id == "user/highland_farm_spur" ? 0.035f
                    : 0.08f;
                s_driveableGradeLines.Add(SmoothRoadGrade(raw, maximumSlope, s_driveableGradeLines));
            }
            AnchorData farmAnchor = (data.anchors ?? Array.Empty<AnchorData>()).FirstOrDefault(anchor => anchor.id == "highland_first_farm");
            if (farmAnchor != null)
            {
                s_farmPadCentre = new Vector3(farmAnchor.x * data.compression, 0f, farmAnchor.z * data.compression);
                s_farmPadHeight = TryNearestGrade(s_farmPadCentre, s_driveableGradeLines, out float gradeHeight, out _)
                    ? gradeHeight : RawHeightAt(s_farmPadCentre.x, s_farmPadCentre.z);
                s_farmPadCentre.y = s_farmPadHeight;
            }
            s_lalayBaseHeight = s_lalayGradeLines.SelectMany(line => line).Min(point => point.y);
        }

        private static Vector3[] SmoothRoadGrade(Vector3[] raw, float maximumSlope, List<Vector3[]> connectedLines)
        {
            float length = LineLength(raw);
            bool startConnected = TryKnownHeight(raw[0], connectedLines, out float connectedStart);
            bool endConnected = TryKnownHeight(raw[raw.Length - 1], connectedLines, out float connectedEnd);
            float startHeight = startConnected ? connectedStart : raw[0].y;
            float endHeight;
            if (endConnected)
            {
                endHeight = connectedEnd;
                if (!startConnected)
                    startHeight = endHeight - Mathf.Clamp(raw[raw.Length - 1].y - raw[0].y, -length * maximumSlope, length * maximumSlope);
            }
            else
            {
                endHeight = startHeight + Mathf.Clamp(raw[raw.Length - 1].y - raw[0].y, -length * maximumSlope, length * maximumSlope);
            }
            Vector3[] line = new Vector3[raw.Length];
            float travelled = 0f;
            line[0] = new Vector3(raw[0].x, startHeight, raw[0].z);
            for (int i = 1; i < raw.Length; i++)
            {
                travelled += HorizontalDistance(raw[i - 1], raw[i]);
                float t = length > 0.001f ? travelled / length : 0f;
                line[i] = new Vector3(raw[i].x, Mathf.Lerp(startHeight, endHeight, t), raw[i].z);
            }
            return line;
        }

        private static void SmoothLalayGradeLines()
        {
            if (s_lalayGradeLines.Count == 0) return;
            List<Vector3[]> ordered = s_lalayGradeLines.OrderByDescending(LineLength).ToList();
            List<Vector3[]> smoothed = new List<Vector3[]>();
            for (int lineIndex = 0; lineIndex < ordered.Count; lineIndex++)
            {
                Vector3[] raw = ordered[lineIndex];
                float length = LineLength(raw);
                float startHeight;
                float endHeight;
                if (lineIndex == 0)
                {
                    // Anchor the low/bay end to the DEM and limit the entire Lalay
                    // corridor to a mild 1.5% continuous grade.
                    bool startIsLower = raw[0].y <= raw[raw.Length - 1].y;
                    float low = startIsLower ? raw[0].y : raw[raw.Length - 1].y;
                    float rise = Mathf.Min(Mathf.Abs(raw[raw.Length - 1].y - raw[0].y), length * 0.015f);
                    startHeight = startIsLower ? low : low + rise;
                    endHeight = startIsLower ? low + rise : low;
                }
                else
                {
                    bool startsAtKnownJunction = TryKnownHeight(raw[0], smoothed, out float knownStart);
                    bool endsAtKnownJunction = TryKnownHeight(raw[raw.Length - 1], smoothed, out float knownEnd);
                    if (startsAtKnownJunction)
                    {
                        startHeight = knownStart;
                        endHeight = startHeight + Mathf.Clamp(raw[raw.Length - 1].y - raw[0].y, -length * 0.015f, length * 0.015f);
                    }
                    else if (endsAtKnownJunction)
                    {
                        endHeight = knownEnd;
                        startHeight = endHeight - Mathf.Clamp(raw[raw.Length - 1].y - raw[0].y, -length * 0.015f, length * 0.015f);
                    }
                    else
                    {
                        startHeight = raw[0].y;
                        endHeight = startHeight + Mathf.Clamp(raw[raw.Length - 1].y - raw[0].y, -length * 0.015f, length * 0.015f);
                    }
                }

                Vector3[] line = new Vector3[raw.Length];
                float travelled = 0f;
                line[0] = new Vector3(raw[0].x, startHeight, raw[0].z);
                for (int i = 1; i < raw.Length; i++)
                {
                    travelled += HorizontalDistance(raw[i - 1], raw[i]);
                    float t = length > 0.001f ? travelled / length : 0f;
                    line[i] = new Vector3(raw[i].x, Mathf.Lerp(startHeight, endHeight, t), raw[i].z);
                }
                smoothed.Add(line);
            }
            s_lalayGradeLines.Clear();
            s_lalayGradeLines.AddRange(smoothed);
        }

        private static float LineLength(Vector3[] points)
        {
            float length = 0f;
            for (int i = 1; i < points.Length; i++) length += HorizontalDistance(points[i - 1], points[i]);
            return length;
        }

        private static bool TryKnownHeight(Vector3 endpoint, List<Vector3[]> lines, out float height)
        {
            foreach (Vector3 point in lines.SelectMany(line => line))
            {
                if (HorizontalDistance(endpoint, point) > 0.5f) continue;
                height = point.y;
                return true;
            }
            height = 0f;
            return false;
        }

        private static float SampleHeight(int x, int z) => s_heightData.samplesSouthToNorth[z * s_heightData.width + x];

        private static Vector3 World(PointData p, float compression, float yOffset = 0f)
        {
            float x = p.x * compression;
            float z = p.z * compression;
            return new Vector3(x, HeightAt(x, z) + yOffset, z);
        }

        private static Bounds CalculateBounds(MapData data)
        {
            HashSet<string> lalayIds = new HashSet<string>(data.lalayRoadIds ?? Array.Empty<string>());
            IEnumerable<Vector3> points = data.roads.Where(r => ShouldRenderRoad(data, r, lalayIds)).SelectMany(r => r.points ?? Array.Empty<PointData>()).Select(p => World(p, data.compression));
            Bounds bounds = new Bounds(points.First(), Vector3.zero);
            foreach (Vector3 point in points) bounds.Encapsulate(point);
            bounds.Expand(new Vector3(45f, 0f, 45f));
            return bounds;
        }

        private static void BuildTerrain(Bounds bounds, Material material, Transform parent)
        {
            const int resolution = 129;
            Vector3[] vertices = new Vector3[resolution * resolution];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];
            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                float tx = x / (float)(resolution - 1);
                float tz = z / (float)(resolution - 1);
                float wx = Mathf.Lerp(bounds.min.x, bounds.max.x, tx);
                float wz = Mathf.Lerp(bounds.min.z, bounds.max.z, tz);
                int i = z * resolution + x;
                vertices[i] = new Vector3(wx, HeightAt(wx, wz), wz);
                uv[i] = new Vector2(tx, tz);
            }
            int t = 0;
            for (int z = 0; z < resolution - 1; z++)
            for (int x = 0; x < resolution - 1; x++)
            {
                int i = z * resolution + x;
                triangles[t++] = i; triangles[t++] = i + resolution; triangles[t++] = i + 1;
                triangles[t++] = i + 1; triangles[t++] = i + resolution; triangles[t++] = i + resolution + 1;
            }
            CreateMeshObject("Copernicus_GLO30_Terrain", vertices, triangles, uv, material, parent, true);
        }

        private static void BuildSea(MapData data, Bounds bounds, Material material, Transform parent)
        {
            PointData[] coast = data.coastline?.points ?? Array.Empty<PointData>();
            if (coast.Length < 2) return;
            List<Vector3> vertices = coast.Select(p => { Vector3 v = World(p, data.compression); v.y = 0.25f; return v; }).ToList();
            vertices.Add(new Vector3(bounds.max.x, 0.25f, bounds.min.z));
            vertices.Add(new Vector3(bounds.min.x, 0.25f, bounds.min.z));
            int[] triangles = new int[(vertices.Count - 2) * 3];
            for (int i = 0; i < vertices.Count - 2; i++) { triangles[i * 3] = 0; triangles[i * 3 + 1] = i + 1; triangles[i * 3 + 2] = i + 2; }
            CreateMeshObject("Caribbean_Sea", vertices.ToArray(), triangles, vertices.Select(v => new Vector2(v.x, v.z)).ToArray(), material, parent, false);
        }

        private static GameObject CreateRibbon(string name, PointData[] source, float compression, float width, float lateralOffset, float yOffset, Material material, Transform parent, bool collider, float maximumGrade = -1f)
        {
            Vector3[] centres = TerrainConformingCentres(source, compression, yOffset, 3.5f, maximumGrade);
            Vector3[] vertices = new Vector3[centres.Length * 2];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[(centres.Length - 1) * 6];
            float distance = 0f;
            for (int i = 0; i < centres.Length; i++)
            {
                Vector3 before = centres[Mathf.Max(0, i - 1)];
                Vector3 after = centres[Mathf.Min(centres.Length - 1, i + 1)];
                Vector3 forward = after - before; forward.y = 0f; forward.Normalize();
                Vector3 side = new Vector3(-forward.z, 0f, forward.x);
                Vector3 centre = centres[i] + side * lateralOffset;
                vertices[i * 2] = centre - side * (width * 0.5f);
                vertices[i * 2 + 1] = centre + side * (width * 0.5f);
                if (i > 0) distance += Vector3.Distance(centres[i - 1], centres[i]);
                uv[i * 2] = new Vector2(0f, distance * 0.1f);
                uv[i * 2 + 1] = new Vector2(1f, distance * 0.1f);
            }
            for (int i = 0; i < centres.Length - 1; i++)
            {
                int b = i * 6; int v = i * 2;
                triangles[b] = v; triangles[b + 1] = v + 1; triangles[b + 2] = v + 2;
                triangles[b + 3] = v + 1; triangles[b + 4] = v + 3; triangles[b + 5] = v + 2;
            }
            return CreateMeshObject(name, vertices, triangles, uv, material, parent, collider);
        }

        private static Vector3[] TerrainConformingCentres(PointData[] source, float compression, float yOffset, float maximumSpacing, float maximumGrade = -1f)
        {
            List<Vector3> centres = new List<Vector3>();
            for (int i = 1; i < source.Length; i++)
            {
                Vector3 a = new Vector3(source[i - 1].x * compression, 0f, source[i - 1].z * compression);
                Vector3 b = new Vector3(source[i].x * compression, 0f, source[i].z * compression);
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(a, b) / maximumSpacing));
                int start = i == 1 ? 0 : 1;
                for (int step = start; step <= steps; step++)
                {
                    Vector3 centre = Vector3.Lerp(a, b, step / (float)steps);
                    // Use the same analytically graded surface that builds the terrain.
                    // Independent road grading made ribbons float over or sink beneath
                    // the terrain when two corridors were close together.
                    centre.y = HeightAt(centre.x, centre.z) + yOffset;
                    centres.Add(centre);
                }
            }
            return centres.ToArray();
        }

        private static void CreateZoneOutline(ZoneData zone, float compression, Material material, Transform parent)
        {
            PointData[] closed = zone.points.Concat(new[] { zone.points[0] }).ToArray();
            CreateRibbon("Zone_Highland_UserApproved_Outline", closed, compression, 2.2f, 0f, 0.72f, material, parent, false);
        }

        private static int BuildRoadGapConnectors(MapData data, Material material, Transform parent)
        {
            HashSet<string> lalayIds = new HashSet<string>(data.lalayRoadIds ?? Array.Empty<string>());
            RoadData[] driveable = (data.roads ?? Array.Empty<RoadData>())
                .Where(r => ShouldRenderRoad(data, r, lalayIds) && IsDriveableRoad(r)).ToArray();
            HashSet<string> connectedPairs = new HashSet<string>();
            int count = 0;
            foreach (RoadData road in driveable)
            {
                PointData[] endpoints = { road.points[0], road.points[road.points.Length - 1] };
                foreach (PointData endpointData in endpoints)
                {
                    Vector3 endpoint = World(endpointData, data.compression);
                    if (!PhaseOneBounds.Contains(new Vector2(endpoint.x, endpoint.z))) continue;
                    RoadData bestRoad = null;
                    Vector3 bestPoint = Vector3.zero;
                    // Only seal tiny digitising seams. Larger auto-links created the
                    // spiderweb seen in Highland and must be explicit map data instead.
                    float bestDistance = 0.65f;
                    foreach (RoadData other in driveable)
                    {
                        if (other == road) continue;
                        Vector3[] points = other.points.Select(p => World(p, data.compression)).ToArray();
                        for (int i = 1; i < points.Length; i++)
                        {
                            float t;
                            float distance = DistanceToSegmentXZ(endpoint, points[i - 1], points[i], out t);
                            if (distance < 0.65f || distance >= bestDistance) continue;
                            bestDistance = distance;
                            bestRoad = other;
                            bestPoint = Vector3.Lerp(points[i - 1], points[i], t);
                        }
                    }
                    if (bestRoad == null) continue;
                    string pair = string.CompareOrdinal(road.id, bestRoad.id) < 0 ? road.id + "|" + bestRoad.id : bestRoad.id + "|" + road.id;
                    if (!connectedPairs.Add(pair)) continue;
                    PointData[] connector =
                    {
                        new PointData { x = endpoint.x / data.compression, z = endpoint.z / data.compression },
                        new PointData { x = bestPoint.x / data.compression, z = bestPoint.z / data.compression }
                    };
                    float width = Mathf.Max(3.2f, Mathf.Min(RoadWidth(road.roadClass, false), RoadWidth(bestRoad.roadClass, false)));
                    CreateRibbon($"Road_Connector_{count:00}_{SafeName(road.id)}_{SafeName(bestRoad.id)}", connector, data.compression, width, 0f, 0.44f, material, parent, true);
                    count++;
                }
            }
            return count;
        }

        private static int BuildBridges(MapData data, Material deckMaterial, Material railMaterial, Transform parent)
        {
            HashSet<string> lalayIds = new HashSet<string>(data.lalayRoadIds ?? Array.Empty<string>());
            HashSet<string> bridgePairs = new HashSet<string>();
            int count = 0;
            foreach (RoadData road in (data.roads ?? Array.Empty<RoadData>()).Where(r => ShouldRenderRoad(data, r, lalayIds) && IsDriveableRoad(r)))
            foreach (WaterwayData waterway in data.waterways ?? Array.Empty<WaterwayData>())
            {
                Vector3[] roadPoints = road.points.Select(p => World(p, data.compression)).ToArray();
                Vector3[] waterPoints = waterway.points.Select(p => World(p, data.compression)).ToArray();
                for (int ri = 1; ri < roadPoints.Length; ri++)
                for (int wi = 1; wi < waterPoints.Length; wi++)
                {
                    if (!TrySegmentIntersectionXZ(roadPoints[ri - 1], roadPoints[ri], waterPoints[wi - 1], waterPoints[wi], out Vector3 crossing)) continue;
                    if (!PhaseOneBounds.Contains(new Vector2(crossing.x, crossing.z))) continue;
                    string pair = road.id + "|" + waterway.id;
                    if (!bridgePairs.Add(pair)) continue;
                    Vector3 direction = roadPoints[ri] - roadPoints[ri - 1]; direction.y = 0f; direction.Normalize();
                    float roadWidth = RoadWidth(road.roadClass, false);
                    float length = (waterway.waterClass == "river" ? 12f : 8f) + roadWidth;
                    Vector3 a = crossing - direction * length * 0.5f;
                    Vector3 b = crossing + direction * length * 0.5f;
                    GameObject group = Child(parent.gameObject, $"Bridge_{count:00}_{SafeName(road.id)}");
                    PointData[] deckLine =
                    {
                        new PointData { x = a.x / data.compression, z = a.z / data.compression },
                        new PointData { x = b.x / data.compression, z = b.z / data.compression }
                    };
                    CreateRibbon("Driveable_Deck", deckLine, data.compression, roadWidth + 0.8f, 0f, 0.72f, deckMaterial, group.transform, true);
                    Vector3 side = new Vector3(-direction.z, 0f, direction.x);
                    for (int sign = -1; sign <= 1; sign += 2)
                    {
                        GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        rail.name = sign < 0 ? "Guard_Rail_Left" : "Guard_Rail_Right";
                        rail.transform.SetParent(group.transform, false);
                        Vector3 railPosition = crossing + side * sign * (roadWidth * 0.5f + 0.55f);
                        railPosition.y = HeightAt(railPosition.x, railPosition.z) + 1.05f;
                        rail.transform.position = railPosition;
                        rail.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                        rail.transform.localScale = new Vector3(0.28f, 0.75f, length);
                        rail.GetComponent<Renderer>().sharedMaterial = railMaterial;
                    }
                    count++;
                }
            }
            return count;
        }

        private static int BuildPhaseOneBoundaries(MapData data, Material material, Transform parent)
        {
            HashSet<string> lalayIds = new HashSet<string>(data.lalayRoadIds ?? Array.Empty<string>());
            int count = 0;
            foreach (RoadData road in (data.roads ?? Array.Empty<RoadData>()).Where(r => ShouldRenderRoad(data, r, lalayIds) && IsDriveableRoad(r)))
            for (int i = 1; i < road.points.Length; i++)
            {
                Vector3 a = World(road.points[i - 1], data.compression);
                Vector3 b = World(road.points[i], data.compression);
                bool aInside = PhaseOneBounds.Contains(new Vector2(a.x, a.z));
                bool bInside = PhaseOneBounds.Contains(new Vector2(b.x, b.z));
                if (aInside == bInside) continue;
                Vector3 inside = aInside ? a : b;
                Vector3 outside = aInside ? b : a;
                for (int step = 0; step < 12; step++)
                {
                    Vector3 middle = (inside + outside) * 0.5f;
                    if (PhaseOneBounds.Contains(new Vector2(middle.x, middle.z))) inside = middle; else outside = middle;
                }
                Vector3 direction = b - a; direction.y = 0f; direction.Normalize();
                GameObject barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
                barrier.name = $"FutureExitBarrier_{count:00}_{SafeName(road.id)}";
                barrier.transform.SetParent(parent, false);
                inside.y = HeightAt(inside.x, inside.z) + 0.9f;
                barrier.transform.position = inside;
                barrier.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                barrier.transform.localScale = new Vector3(RoadWidth(road.roadClass, false) + 1.5f, 1.8f, 0.65f);
                barrier.GetComponent<Renderer>().sharedMaterial = material;
                count++;
            }

            AddInvisibleBoundaryWall("PhaseBoundary_West", new Vector3(PhaseOneBounds.xMin - 1f, 12f, PhaseOneBounds.center.y), new Vector3(2f, 24f, PhaseOneBounds.height), parent);
            AddInvisibleBoundaryWall("PhaseBoundary_East", new Vector3(PhaseOneBounds.xMax + 1f, 12f, PhaseOneBounds.center.y), new Vector3(2f, 24f, PhaseOneBounds.height), parent);
            AddInvisibleBoundaryWall("PhaseBoundary_South", new Vector3(PhaseOneBounds.center.x, 12f, PhaseOneBounds.yMin - 1f), new Vector3(PhaseOneBounds.width, 24f, 2f), parent);
            AddInvisibleBoundaryWall("PhaseBoundary_North", new Vector3(PhaseOneBounds.center.x, 12f, PhaseOneBounds.yMax + 1f), new Vector3(PhaseOneBounds.width, 24f, 2f), parent);
            return count;
        }

        private static void AddInvisibleBoundaryWall(string name, Vector3 position, Vector3 size, Transform parent)
        {
            GameObject wall = new GameObject(name);
            wall.transform.SetParent(parent, false);
            wall.transform.position = position;
            BoxCollider collider = wall.AddComponent<BoxCollider>();
            collider.size = size;
        }

        private static void BuildChurch(Dictionary<string, AnchorData> anchors, float compression, Material wallMaterial, Material roofMaterial, Transform parent, Transform labels)
        {
            Vector3 position = AnchorPosition(anchors, "grand_bay_catholic_church", compression);
            GameObject church = Child(parent.gameObject, "GrandBay_Catholic_Church_Graybox");
            church.transform.position = position;
            church.transform.rotation = Quaternion.Euler(0f, -28f, 0f);
            CreateChurchPart("Nave", new Vector3(0f, 3.3f, 0f), new Vector3(10f, 6.6f, 18f), wallMaterial, church.transform);
            CreateChurchPart("Roof", new Vector3(0f, 7.0f, 0f), new Vector3(11f, 1.0f, 19f), roofMaterial, church.transform);
            CreateChurchPart("Front_Tower", new Vector3(0f, 5.0f, -10f), new Vector3(4.2f, 10f, 4.2f), wallMaterial, church.transform);
            CreateChurchPart("Tower_Roof", new Vector3(0f, 10.3f, -10f), new Vector3(5f, 0.8f, 5f), roofMaterial, church.transform);
            CreateChurchPart("Cross_Vertical", new Vector3(0f, 12.3f, -10f), new Vector3(0.35f, 3f, 0.35f), roofMaterial, church.transform);
            CreateChurchPart("Cross_Horizontal", new Vector3(0f, 12.7f, -10f), new Vector3(1.7f, 0.35f, 0.35f), roofMaterial, church.transform);
            CreateGroundLabel("CHURCH OF ST. PATRICK", position + new Vector3(0f, 0f, 13f), Color.white, labels, 4.4f);
        }

        private static void BuildBayShoreline(Material sandMaterial, Material stoneMaterial, Transform parent)
        {
            Vector2[] footprint =
            {
                new Vector2(218f, -220f), new Vector2(274f, -220f), new Vector2(274f, -128f),
                new Vector2(247f, -128f), new Vector2(232f, -148f), new Vector2(216f, -177f),
                new Vector2(214f, -202f)
            };
            const float cell = 5f;
            List<Vector3> sandVertices = new List<Vector3>();
            List<Vector2> sandUv = new List<Vector2>();
            List<int> sandTriangles = new List<int>();
            for (float z = -220f; z < -132f; z += cell)
            for (float x = 212f; x < 274f; x += cell)
            {
                if (!PointInPolygon(new Vector2(x + cell * 0.5f, z + cell * 0.5f), footprint)) continue;
                int first = sandVertices.Count;
                Vector2[] corners =
                {
                    new Vector2(x, z), new Vector2(x + cell, z),
                    new Vector2(x, z + cell), new Vector2(x + cell, z + cell)
                };
                foreach (Vector2 corner in corners)
                {
                    sandVertices.Add(new Vector3(corner.x, HeightAt(corner.x, corner.y) + 0.32f, corner.y));
                    sandUv.Add(corner * 0.05f);
                }
                sandTriangles.Add(first); sandTriangles.Add(first + 2); sandTriangles.Add(first + 1);
                sandTriangles.Add(first + 1); sandTriangles.Add(first + 2); sandTriangles.Add(first + 3);
            }
            GameObject sand = CreateMeshObject("Bay_Sand_Patch", sandVertices.ToArray(), sandTriangles.ToArray(), sandUv.ToArray(), sandMaterial, parent, false);
            sand.name = "Bay_Sand_Patch";

            System.Random random = new System.Random(9801);
            int placed = 0;
            int attempts = 0;
            while (placed < 26 && attempts++ < 160)
            {
                float x = Mathf.Lerp(218f, 267f, (float)random.NextDouble());
                float z = Mathf.Lerp(-211f, -134f, (float)random.NextDouble());
                Vector2 point = new Vector2(x, z);
                if (!PointInPolygon(point, footprint)) continue;
                Vector3 world = new Vector3(x, HeightAt(x, z), z);
                if (DistanceToAnyDriveableRoad(world) < 5.5f) continue;
                if (HorizontalDistance(world, new Vector3(193f, 0f, -171f)) < 13f) continue;
                if (HorizontalDistance(world, new Vector3(224f, 0f, -181f)) < 11f) continue;
                GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stone.name = $"Bay_Stone_{placed:00}";
                stone.transform.SetParent(parent, false);
                stone.transform.position = world + Vector3.up * 0.28f;
                stone.transform.rotation = Quaternion.Euler((float)random.NextDouble() * 18f, (float)random.NextDouble() * 180f, (float)random.NextDouble() * 18f);
                float scale = 0.35f + (float)random.NextDouble() * 0.65f;
                stone.transform.localScale = new Vector3(scale * 1.4f, scale * 0.45f, scale);
                stone.GetComponent<Renderer>().sharedMaterial = stoneMaterial;
                UnityEngine.Object.DestroyImmediate(stone.GetComponent<Collider>());
                placed++;
            }
        }

        private static float DistanceToAnyDriveableRoad(Vector3 point)
        {
            float closest = float.MaxValue;
            foreach ((Vector3[] points, float halfWidth) road in s_roadClearances)
            for (int i = 1; i < road.points.Length; i++)
                closest = Mathf.Min(closest, DistanceToSegmentXZ(point, road.points[i - 1], road.points[i], out _) - road.halfWidth);
            return closest;
        }

        private static bool PointInPolygon(Vector2 point, Vector2[] polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 a = polygon[i]; Vector2 b = polygon[j];
                if ((a.y > point.y) != (b.y > point.y) && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside;
        }

        private static void CreateChurchPart(string name, Vector3 localPosition, Vector3 scale, Material material, Transform parent)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static bool IsDriveableRoad(RoadData road) => road?.points != null && road.points.Length >= 2 && road.roadClass != "path" && road.roadClass != "track";

        private static bool ShouldRenderRoad(MapData data, RoadData road, HashSet<string> lalayIds)
        {
            if (road?.points == null || road.points.Length < 2) return false;
            return lalayIds.Contains(road.id) || PhaseOneSupportingRoadIds.Contains(road.id);
        }

        private static bool TrySegmentIntersectionXZ(Vector3 a, Vector3 b, Vector3 c, Vector3 d, out Vector3 intersection)
        {
            Vector2 p = new Vector2(a.x, a.z); Vector2 r = new Vector2(b.x - a.x, b.z - a.z);
            Vector2 q = new Vector2(c.x, c.z); Vector2 s = new Vector2(d.x - c.x, d.z - c.z);
            float cross = r.x * s.y - r.y * s.x;
            if (Mathf.Abs(cross) < 0.0001f) { intersection = Vector3.zero; return false; }
            Vector2 qp = q - p;
            float t = (qp.x * s.y - qp.y * s.x) / cross;
            float u = (qp.x * r.y - qp.y * r.x) / cross;
            if (t < 0f || t > 1f || u < 0f || u > 1f) { intersection = Vector3.zero; return false; }
            Vector2 hit = p + r * t;
            intersection = new Vector3(hit.x, HeightAt(hit.x, hit.y), hit.y);
            return true;
        }

        private static void BuildLandmarkLots(Dictionary<string, AnchorData> anchors, float compression, Transform parent, Transform labels, Material farmMat, Material jettyMat)
        {
            AddLot(anchors, "highland_first_farm", "Highland_First_Farm", new Vector3(30f, 0.45f, 22f), farmMat, compression, parent, labels);
            AddLot(anchors, "grand_bay_primary_school", "Primary_School_Lot", new Vector3(32f, 0.55f, 22f), ColorMaterial("SchoolLot", new Color(0.12f, 0.48f, 0.43f)), compression, parent, labels);
            Material futureLand = ColorMaterial("FutureFarmLand", new Color(0.27f, 0.43f, 0.20f));
            AddSilentLot(anchors, "highland_future_plot_02", "Future_Farm_Parcel_02_HiddenAtStart", new Vector3(27f, 0.22f, 20f), futureLand, compression, parent);
            AddSilentLot(anchors, "highland_future_plot_03", "Future_Farm_Parcel_03_HiddenAtStart", new Vector3(29f, 0.22f, 21f), futureLand, compression, parent);
            AddSilentLot(anchors, "highland_future_plot_04", "Future_Farm_Parcel_04_HiddenAtStart", new Vector3(25f, 0.22f, 19f), futureLand, compression, parent);
            AddSilentLot(anchors, "highland_future_plot_05", "Future_Farm_Parcel_05_HiddenAtStart", new Vector3(26f, 0.22f, 19f), futureLand, compression, parent);
            AddSilentLot(anchors, "highland_future_plot_06", "Future_Farm_Parcel_06_HiddenAtStart", new Vector3(28f, 0.22f, 20f), futureLand, compression, parent);
            AddSilentLot(anchors, "highland_future_plot_07", "Future_Farm_Parcel_07_HiddenAtStart", new Vector3(25f, 0.22f, 18f), futureLand, compression, parent);
            AddSilentLot(anchors, "highland_future_plot_08", "Future_Farm_Parcel_08_HiddenAtStart", new Vector3(24f, 0.22f, 18f), futureLand, compression, parent);

            Vector3 jetty = AnchorPosition(anchors, "story_jetty", compression);
            GameObject pier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pier.name = "Story_Jetty";
            pier.transform.SetParent(parent, false);
            // Run from the landward jetty anchor, across the beach, and beyond the
            // sand edge into open water so the boat route reads clearly.
            pier.transform.position = jetty + new Vector3(25f, 1.1f, -5f);
            pier.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            pier.transform.localScale = new Vector3(4.5f, 1.2f, 72f);
            pier.GetComponent<Renderer>().sharedMaterial = jettyMat;
            CreateGroundLabel("JETTY / GUADELOUPE BOAT", jetty + new Vector3(-5f, 0f, 13f), Color.white, labels, 4.5f);

            // Only the player's first plot is visible at the start. Later land parcels
            // exist as inactive anchors and are revealed by progression, not as slabs.
            Vector3 farm = AnchorPosition(anchors, "highland_first_farm", compression);
            GameObject plot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plot.name = "Farm_Plot_Starting_Active";
            plot.transform.SetParent(parent, false);
            farm.y = HeightAt(farm.x, farm.z) + 0.18f;
            plot.transform.position = farm;
            plot.transform.localScale = new Vector3(5.5f, 0.22f, 6.5f);
            plot.GetComponent<Renderer>().sharedMaterial = farmMat;
        }

        private static void BuildDenseLalayHouses(MapData data, HashSet<string> lalayIds, Dictionary<string, AnchorData> anchors, Material[] walls, Material[] roofs, Transform parent)
        {
            List<(Vector3 point, Vector3 forward)> samples = new List<(Vector3, Vector3)>();
            foreach (RoadData lalayRoad in data.roads.Where(r => lalayIds.Contains(r.id) && r.points != null && r.points.Length >= 2))
                samples.AddRange(SamplePolyline(lalayRoad.points.Select(p => World(p, data.compression, 0f)).ToList(), 3.72f));
            if (samples.Count == 0) return;
            System.Random random = new System.Random(98);
            int houseIndex = 0;
            foreach ((Vector3 point, Vector3 forward) sample in samples)
            {
                Vector3 side = new Vector3(-sample.forward.z, 0f, sample.forward.x).normalized;
                for (int sideSign = -1; sideSign <= 1; sideSign += 2)
                {
                    string sideLabel = sideSign < 0 ? "SideA" : "SideB";
                    float setback = 7.8f + (float)random.NextDouble() * 1.8f;
                    Vector3 position = sample.point + side * sideSign * setback + sample.forward * ((float)random.NextDouble() - 0.5f) * 1.4f;
                    float width = 4.2f + (float)random.NextDouble() * 1.2f;
                    float depth = 3.8f + (float)random.NextDouble() * 1.0f;
                    // The user's phase-one satellite crop confirms the low coastal strip
                    // around the jetty is open land. The mapped church is built separately.
                    if (position.x > 200f || (position.x > 165f && position.z < -140f)) continue;
                    Vector3 houseForward = -side * sideSign;
                    if (OverlapsNonLalayRoad(position, width, depth, houseForward, data, lalayIds)) continue;
                    string[] protectedLots = { "highland_first_farm", "highland_future_plot_02", "highland_future_plot_03", "highland_future_plot_04", "grand_bay_primary_school", "grand_bay_catholic_church" };
                    if (protectedLots.Any(id => HorizontalDistance(position, AnchorPosition(anchors, id, data.compression)) < (id == "highland_first_farm" ? 23f : id == "grand_bay_catholic_church" ? 18f : 15f))) continue;
                    position.y = HeightAt(position.x, position.z);
                    Quaternion facingRoad = Quaternion.LookRotation(houseForward, Vector3.up) * Quaternion.Euler(0f, (float)(random.NextDouble() - 0.5f) * 6f, 0f);
                    double buildingRoll = random.NextDouble();
                    bool useShanty = buildingRoll < 0.28;
                    bool twoStorey = buildingRoll > 0.76;
                    if (useShanty)
                    {
                        int variant = ShantyVariants[random.Next(ShantyVariants.Length)];
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/ArteriaShantyTown/ShantyTown1/shanty{variant}.fbx");
                        if (prefab != null)
                        {
                            GameObject shanty = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                            shanty.name = $"Lalay_Shanty_{sideLabel}_{houseIndex:000}_V{variant:00}";
                            shanty.transform.position = position;
                            shanty.transform.rotation = facingRoad;
                            shanty.transform.localScale = Vector3.one * ShantyScale;
                            AddBoundsCollider(shanty);
                        }
                        else
                        {
                            BuildProceduralHouse(parent, sideLabel, houseIndex, position, facingRoad, width, depth, false, walls[random.Next(walls.Length)], roofs[random.Next(roofs.Length)]);
                        }
                    }
                    else
                    {
                        BuildProceduralHouse(parent, sideLabel, houseIndex, position, facingRoad, width, depth, twoStorey, walls[random.Next(walls.Length)], roofs[random.Next(roofs.Length)]);
                    }
                    houseIndex++;
                }
            }
        }

        private static void BuildSparseHighlandHouses(MapData data, Dictionary<string, AnchorData> anchors, Material[] walls, Material[] roofs, Transform parent)
        {
            string[] housingRoadIds = { "user/lalay_inland_coastal_connector", "user/highland_lalay_inroad" };
            Rect highlandHousingBounds = new Rect(28f, -166f, 165f, 94f);
            string[] protectedAnchors =
            {
                "highland_first_farm", "highland_future_plot_02", "highland_future_plot_03",
                "highland_future_plot_04", "highland_future_plot_05", "highland_future_plot_06",
                "highland_future_plot_07", "highland_future_plot_08", "grand_bay_primary_school"
            };
            List<Vector3> placed = new List<Vector3>();
            System.Random random = new System.Random(991);
            int candidateIndex = 0;
            foreach (RoadData road in data.roads.Where(r => housingRoadIds.Contains(r.id)))
            foreach ((Vector3 point, Vector3 forward) sample in SamplePolyline(road.points.Select(p => World(p, data.compression)).ToList(), 13f))
            {
                if (placed.Count >= 18) return;
                if (!highlandHousingBounds.Contains(new Vector2(sample.point.x, sample.point.z))) continue;
                Vector3 side = new Vector3(-sample.forward.z, 0f, sample.forward.x).normalized;
                for (int sideSign = -1; sideSign <= 1; sideSign += 2)
                {
                    if (placed.Count >= 18) return;
                    candidateIndex++;
                    Vector3 position = sample.point + side * sideSign * (10.8f + (float)random.NextDouble() * 2.2f);
                    float width = 4.6f + (float)random.NextDouble() * 1.2f;
                    float depth = 4.1f + (float)random.NextDouble() * 1.0f;
                    Vector3 houseForward = -side * sideSign;
                    if (!highlandHousingBounds.Contains(new Vector2(position.x, position.z))) continue;
                    if (OverlapsAnyRoad(position, width, depth, houseForward)) continue;
                    if (placed.Any(existing => HorizontalDistance(existing, position) < 8.5f)) continue;
                    if (protectedAnchors.Any(id => HorizontalDistance(position, AnchorPosition(anchors, id, data.compression)) < 12f)) continue;
                    position.y = HeightAt(position.x, position.z);
                    Quaternion rotation = Quaternion.LookRotation(houseForward, Vector3.up) * Quaternion.Euler(0f, (float)(random.NextDouble() - 0.5f) * 5f, 0f);
                    Material wall = walls[random.Next(walls.Length)];
                    Material roof = roofs[random.Next(roofs.Length)];
                    if (placed.Count % 4 == 1)
                        BuildSmallApartment(parent, placed.Count, position, rotation, wall, roof);
                    else
                        BuildProceduralHouse(parent, "Highland", placed.Count, position, rotation, width, depth, random.NextDouble() < 0.78,
                            wall, roof, "Highland");
                    placed.Add(position);
                }
            }
        }

        private static void BuildSmallApartment(Transform parent, int index, Vector3 position, Quaternion rotation, Material wall, Material roofMaterial)
        {
            GameObject root = Child(parent.gameObject, $"Highland_SmallApartment_{index:000}");
            root.transform.position = position;
            root.transform.rotation = rotation;
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "TwoStorey_Apartment_Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 3.7f, 0f);
            body.transform.localScale = new Vector3(7.2f, 7.4f, 5.6f);
            body.GetComponent<Renderer>().sharedMaterial = wall;
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Corrugated_Roof";
            roof.transform.SetParent(root.transform, false);
            roof.transform.localPosition = new Vector3(0f, 7.75f, 0f);
            roof.transform.localScale = new Vector3(7.9f, 0.35f, 6.3f);
            roof.GetComponent<Renderer>().sharedMaterial = roofMaterial;
            for (int floor = 0; floor < 2; floor++)
            {
                GameObject balcony = GameObject.CreatePrimitive(PrimitiveType.Cube);
                balcony.name = $"Front_Balcony_{floor + 1}";
                balcony.transform.SetParent(root.transform, false);
                balcony.transform.localPosition = new Vector3(0f, 2.2f + floor * 3.2f, -3.15f);
                balcony.transform.localScale = new Vector3(6.5f, 0.22f, 0.9f);
                balcony.GetComponent<Renderer>().sharedMaterial = roofMaterial;
            }
        }

        private static void BuildProceduralHouse(Transform parent, string sideLabel, int index, Vector3 position, Quaternion rotation, float width, float depth, bool twoStorey, Material wall, Material roofMaterial, string district = "Lalay")
        {
            float height = twoStorey ? 6.4f : 3.5f;
            GameObject houseRoot = Child(parent.gameObject, $"{district}_House_{sideLabel}_{index:000}_{(twoStorey ? "TwoStorey" : "OneStorey")}");
            houseRoot.transform.position = position;
            houseRoot.transform.rotation = rotation;
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body"; body.transform.SetParent(houseRoot.transform, false);
            body.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            body.transform.localScale = new Vector3(width, height, depth);
            body.GetComponent<Renderer>().sharedMaterial = wall;
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Corrugated_Roof"; roof.transform.SetParent(houseRoot.transform, false);
            roof.transform.localPosition = new Vector3(0f, height + 0.35f, 0f);
            roof.transform.localScale = new Vector3(width + 0.7f, 0.35f, depth + 0.8f);
            roof.GetComponent<Renderer>().sharedMaterial = roofMaterial;

            // Lightweight facade detail makes the massing read as homes at
            // gameplay distance without adding unique textures/materials per house.
            AddFacadePanel(houseRoot.transform, "Front_Door", new Vector3(0f, 1.05f, depth * 0.5f + 0.035f), new Vector3(0.85f, 2.05f, 0.07f), s_houseDoorMaterial);
            AddFacadePanel(houseRoot.transform, "Window_Left", new Vector3(-width * 0.27f, 1.75f, depth * 0.5f + 0.04f), new Vector3(0.75f, 0.85f, 0.06f), s_houseWindowMaterial);
            AddFacadePanel(houseRoot.transform, "Window_Right", new Vector3(width * 0.27f, 1.75f, depth * 0.5f + 0.04f), new Vector3(0.75f, 0.85f, 0.06f), s_houseWindowMaterial);
            if (twoStorey)
            {
                AddFacadePanel(houseRoot.transform, "Upper_Window_Left", new Vector3(-width * 0.25f, 4.75f, depth * 0.5f + 0.04f), new Vector3(0.8f, 0.9f, 0.06f), s_houseWindowMaterial);
                AddFacadePanel(houseRoot.transform, "Upper_Window_Right", new Vector3(width * 0.25f, 4.75f, depth * 0.5f + 0.04f), new Vector3(0.8f, 0.9f, 0.06f), s_houseWindowMaterial);
            }
        }

        private static void AddFacadePanel(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material)
        {
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = name;
            panel.transform.SetParent(parent, false);
            panel.transform.localPosition = localPosition;
            panel.transform.localScale = scale;
            panel.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(panel.GetComponent<Collider>());
        }

        private static void AddBoundsCollider(GameObject instance)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            BoxCollider collider = instance.GetComponent<BoxCollider>();
            if (collider == null) collider = instance.AddComponent<BoxCollider>();
            collider.center = instance.transform.InverseTransformPoint(bounds.center);
            Vector3 scale = instance.transform.lossyScale;
            collider.size = new Vector3(
                bounds.size.x / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
                bounds.size.y / Mathf.Max(0.0001f, Mathf.Abs(scale.y)),
                bounds.size.z / Mathf.Max(0.0001f, Mathf.Abs(scale.z)));
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f; return Vector3.Distance(a, b);
        }

        private static bool OverlapsAnyRoad(Vector3 position, float width, float depth, Vector3 houseForward)
        {
            houseForward.y = 0f;
            houseForward.Normalize();
            Vector3 houseRight = new Vector3(houseForward.z, 0f, -houseForward.x);
            foreach ((Vector3[] points, float halfWidth) road in s_roadClearances)
            for (int i = 1; i < road.points.Length; i++)
            {
                Vector3 roadDirection = road.points[i] - road.points[i - 1];
                roadDirection.y = 0f;
                if (roadDirection.sqrMagnitude < 0.0001f) continue;
                roadDirection.Normalize();
                Vector3 roadNormal = new Vector3(-roadDirection.z, 0f, roadDirection.x);
                float houseExtentTowardRoad = Mathf.Abs(Vector3.Dot(roadNormal, houseRight)) * width * 0.5f
                                             + Mathf.Abs(Vector3.Dot(roadNormal, houseForward)) * depth * 0.5f;
                if (DistanceToSegmentXZ(position, road.points[i - 1], road.points[i], out _) < road.halfWidth + houseExtentTowardRoad + 0.5f)
                    return true;
            }
            return false;
        }

        private static bool OverlapsNonLalayRoad(Vector3 position, float width, float depth, Vector3 houseForward, MapData data, HashSet<string> lalayIds)
        {
            houseForward.y = 0f;
            houseForward.Normalize();
            Vector3 houseRight = new Vector3(houseForward.z, 0f, -houseForward.x);
            foreach (RoadData road in data.roads ?? Array.Empty<RoadData>())
            {
                if (!ShouldRenderRoad(data, road, lalayIds) || !IsDriveableRoad(road) || lalayIds.Contains(road.id)) continue;
                Vector3[] points = road.points.Select(p => World(p, data.compression)).ToArray();
                float halfWidth = RoadWidth(road.roadClass, false) * 0.5f;
                for (int i = 1; i < points.Length; i++)
                {
                    Vector3 roadDirection = points[i] - points[i - 1];
                    roadDirection.y = 0f;
                    if (roadDirection.sqrMagnitude < 0.0001f) continue;
                    roadDirection.Normalize();
                    Vector3 roadNormal = new Vector3(-roadDirection.z, 0f, roadDirection.x);
                    float houseExtentTowardRoad = Mathf.Abs(Vector3.Dot(roadNormal, houseRight)) * width * 0.5f
                                                 + Mathf.Abs(Vector3.Dot(roadNormal, houseForward)) * depth * 0.5f;
                    if (DistanceToSegmentXZ(position, points[i - 1], points[i], out _) < halfWidth + houseExtentTowardRoad + 0.45f)
                        return true;
                }
            }
            return false;
        }

        private static float DistanceToSegmentXZ(Vector3 point, Vector3 a, Vector3 b, out float t)
        {
            Vector2 p = new Vector2(point.x, point.z);
            Vector2 start = new Vector2(a.x, a.z);
            Vector2 delta = new Vector2(b.x - a.x, b.z - a.z);
            float lengthSquared = delta.sqrMagnitude;
            t = lengthSquared > 0.0001f ? Mathf.Clamp01(Vector2.Dot(p - start, delta) / lengthSquared) : 0f;
            return Vector2.Distance(p, start + delta * t);
        }

        private static List<(Vector3 point, Vector3 forward)> SamplePolyline(List<Vector3> points, float spacing)
        {
            List<(Vector3, Vector3)> result = new List<(Vector3, Vector3)>();
            float carry = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                Vector3 a = points[i - 1]; Vector3 b = points[i];
                Vector3 delta = b - a; delta.y = 0f;
                float length = delta.magnitude; if (length < 0.01f) continue;
                Vector3 direction = delta / length;
                float distance = spacing - carry;
                while (distance <= length)
                {
                    Vector3 p = Vector3.Lerp(a, b, distance / length);
                    result.Add((p, direction));
                    distance += spacing;
                }
                carry = Mathf.Max(0f, length - (distance - spacing));
            }
            return result;
        }

        private static void AddLot(Dictionary<string, AnchorData> anchors, string id, string name, Vector3 size, Material material, float compression, Transform parent, Transform labels)
        {
            if (!anchors.TryGetValue(id, out AnchorData anchor)) return;
            Vector3 p = AnchorPosition(anchors, id, compression);
            p += id switch
            {
                "upizup_block" => new Vector3(0f, 0f, -11f),
                "dog_life_block" => new Vector3(0f, 0f, -11f),
                "car_dealer" => new Vector3(0f, 0f, 11f),
                "farmers_cooperative" => new Vector3(0f, 0f, -10f),
                _ => Vector3.zero
            };
            p.y = HeightAt(p.x, p.z);
            float halfX = size.x * 0.5f;
            float halfZ = size.z * 0.5f;
            PointData[] outline =
            {
                new PointData { x = (p.x - halfX) / compression, z = (p.z - halfZ) / compression },
                new PointData { x = (p.x + halfX) / compression, z = (p.z - halfZ) / compression },
                new PointData { x = (p.x + halfX) / compression, z = (p.z + halfZ) / compression },
                new PointData { x = (p.x - halfX) / compression, z = (p.z + halfZ) / compression },
                new PointData { x = (p.x - halfX) / compression, z = (p.z - halfZ) / compression }
            };
            GameObject lot = CreateRibbon("Outline_" + name, outline, compression, 0.55f, 0f, 0.34f, material, parent, false);
            lot.name = "Outline_" + name;
            CreateGroundLabel(anchor.displayName.ToUpperInvariant(), p + new Vector3(0f, 0f, size.z * 0.65f), Color.white, labels, 4.2f);
        }

        private static void AddSilentLot(Dictionary<string, AnchorData> anchors, string id, string name, Vector3 size, Material material, float compression, Transform parent)
        {
            if (!anchors.ContainsKey(id)) return;
            Vector3 p = AnchorPosition(anchors, id, compression);
            GameObject lot = new GameObject(name);
            lot.transform.SetParent(parent, false);
            lot.transform.position = p;
            lot.SetActive(false);
        }

        private static Vector3 AnchorPosition(Dictionary<string, AnchorData> anchors, string id, float compression)
        {
            if (!anchors.TryGetValue(id, out AnchorData anchor)) return Vector3.zero;
            float x = anchor.x * compression; float z = anchor.z * compression;
            return new Vector3(x, HeightAt(x, z), z);
        }

        private static Vector3 ZoneCentre(PointData[] points, float compression)
        {
            if (points == null || points.Length == 0) return Vector3.zero;
            Vector3 centre = points.Select(p => World(p, compression)).Aggregate(Vector3.zero, (sum, p) => sum + p) / points.Length;
            centre.y = HeightAt(centre.x, centre.z);
            return centre;
        }

        private static void BuildLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.72f, 0.78f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.50f, 0.43f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.22f, 0.16f);
            GameObject lightGo = new GameObject("Directional Light");
            Light light = lightGo.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.1f; light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        }

        private static void BuildCameras(MapData data, Dictionary<string, AnchorData> anchors, float compression, Bounds bounds)
        {
            string[] focusIds = { "dog_life_block", "car_dealer", "upizup_block", "highland_first_farm", "grand_bay_primary_school", "story_jetty", "grand_bay_catholic_church" };
            Vector3[] focusPoints = focusIds.Select(id => AnchorPosition(anchors, id, compression)).ToArray();
            Bounds focus = new Bounds(focusPoints[0], Vector3.zero);
            foreach (Vector3 point in focusPoints) focus.Encapsulate(point);
            focus.Expand(new Vector3(80f, 0f, 80f));
            Vector3 centre = focus.center; centre.y = HeightAt(centre.x, centre.z);
            Camera overview = CreateCamera("Camera_Overview", centre + new Vector3(0f, 420f, 0f), centre, true);
            overview.orthographicSize = Mathf.Max(focus.size.x * 0.35f, focus.size.z * 0.60f);

            Vector3 storyBlock = AnchorPosition(anchors, "upizup_block", compression);
            RoadData lalayRoad = data.roads.First(r => data.lalayRoadIds.Contains(r.id) && r.points.Length > 6);
            Vector3[] lalayPoints = lalayRoad.points.Select(p => World(p, compression, 0.5f)).ToArray();
            int closest = Enumerable.Range(0, lalayPoints.Length).OrderBy(i => (lalayPoints[i] - storyBlock).sqrMagnitude).First();
            int ahead = Mathf.Min(lalayPoints.Length - 1, closest + 9);
            int behind = Mathf.Max(0, closest - 2);
            Vector3 lalay = lalayPoints[closest];
            Vector3 lalayForward = lalayPoints[ahead] - lalayPoints[behind]; lalayForward.y = 0f; lalayForward.Normalize();
            Vector3 lalaySide = new Vector3(-lalayForward.z, 0f, lalayForward.x);
            CreateCamera("Camera_Lalay", lalay - lalayForward * 14f + lalaySide * 1.2f + Vector3.up * 5.2f, lalayPoints[ahead] + Vector3.up * 2.2f, false);

            Vector3 farm = AnchorPosition(anchors, "highland_first_farm", compression);
            CreateCamera("Camera_Highland", farm + new Vector3(-55f, 72f, 58f), farm + new Vector3(18f, 1f, -5f), false);

            Vector3 church = AnchorPosition(anchors, "grand_bay_catholic_church", compression);
            Vector3 jetty = AnchorPosition(anchors, "story_jetty", compression);
            Vector3 bayFocus = Vector3.Lerp(church, jetty, 0.45f);
            CreateCamera("Camera_Bay", church + new Vector3(-38f, 24f, -22f), bayFocus + Vector3.up * 2.5f, false);
        }

        private static Camera CreateCamera(string name, Vector3 position, Vector3 target, bool orthographic)
        {
            GameObject go = new GameObject(name);
            Camera camera = go.AddComponent<Camera>(); camera.orthographic = orthographic; camera.fieldOfView = 50f; camera.nearClipPlane = 0.3f; camera.farClipPlane = 1800f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.12f, 0.25f, 0.31f); camera.enabled = false;
            go.transform.position = position; go.transform.LookAt(target);
            return camera;
        }

        private static void CreateGroundLabel(string text, Vector3 position, Color color, Transform parent, float size)
        {
            GameObject go = new GameObject("Label_" + SafeName(text)); go.transform.SetParent(parent, false);
            go.transform.position = position + Vector3.up * 1.1f; go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            TextMesh mesh = go.AddComponent<TextMesh>(); mesh.text = text; mesh.anchor = TextAnchor.MiddleCenter; mesh.alignment = TextAlignment.Center; mesh.fontSize = 48; mesh.characterSize = size * 0.022f; mesh.color = color;
        }

        private static GameObject CreateMeshObject(string name, Vector3[] vertices, int[] triangles, Vector2[] uv, Material material, Transform parent, bool collider)
        {
            Mesh mesh = new Mesh { name = name + "_Mesh" }; mesh.indexFormat = vertices.Length > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.vertices = vertices; mesh.triangles = triangles; mesh.uv = uv; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            GameObject go = new GameObject(name); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }

        private static float RoadWidth(string roadClass, bool lalay)
        {
            if (roadClass == "path") return 1.5f;
            if (roadClass == "track") return 4.8f;
            // Every paved phase-one road uses the same two-vehicle width.
            return 6.2f;
        }

        private static Material ColorMaterial(string name, Color color) => MaterialAsset(name, color);

        private static Material MaterialAsset(string name, Color color, bool transparent = false)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            if (transparent)
            {
                material.SetFloat("_Mode", 3f); material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0); material.DisableKeyword("_ALPHATEST_ON"); material.EnableKeyword("_ALPHABLEND_ON"); material.renderQueue = 3000;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/'); string current = parts[0];
            for (int i = 1; i < parts.Length; i++) { string next = current + "/" + parts[i]; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]); current = next; }
        }

        private static GameObject Child(GameObject parent, string name) { GameObject go = new GameObject(name); go.transform.SetParent(parent.transform, false); return go; }
        private static string SafeName(string value) => new string((value ?? "Unnamed").Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException("MINI-095 VALIDATION FAIL: " + message); }

        private static void Capture(string cameraName, string fileName, int width, int height)
        {
            Camera camera = GameObject.Find(cameraName)?.GetComponent<Camera>();
            if (camera == null) throw new InvalidOperationException("Missing capture camera: " + cameraName);
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 2 };
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active; camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
            File.WriteAllBytes(Path.Combine(EvidenceFolder, fileName), image.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
