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
        private const string EvidenceFolder = "Logs/Tasks/MINI-095";

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
        private static readonly List<(Vector3[] points, float halfWidth)> s_roadClearances = new List<(Vector3[], float)>();
        private static float s_lalayBaseHeight;

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
            Material highlandMat = MaterialAsset("HighlandZone", new Color(0.82f, 0.08f, 0.34f));
            Material farmMat = MaterialAsset("FarmSoil", new Color(0.34f, 0.19f, 0.08f));
            Material jettyMat = MaterialAsset("Jetty", new Color(0.33f, 0.20f, 0.10f));
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

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("MapLab_LalayHighland");
            GameObject geography = Child(root, "Geography");
            GameObject roadRoot = Child(root, "Roads_OSM");
            GameObject sidewalkRoot = Child(root, "Lalay_Sidewalks");
            GameObject districtRoot = Child(root, "Approved_Districts_And_Lots");
            GameObject housesRoot = Child(root, "Lalay_Dense_House_Massing");
            GameObject labelsRoot = Child(root, "Labels");

            Bounds bounds = CalculateBounds(data);
            BuildTerrain(bounds, terrainMat, geography.transform);
            BuildSea(data, bounds, seaMat, geography.transform);

            HashSet<string> lalayIds = new HashSet<string>(data.lalayRoadIds ?? Array.Empty<string>());
            foreach (RoadData road in data.roads ?? Array.Empty<RoadData>())
            {
                if (road.points == null || road.points.Length < 2) continue;
                bool lalay = lalayIds.Contains(road.id);
                float width = RoadWidth(road.roadClass, lalay);
                Material material = road.roadClass == "track" || road.roadClass == "path" ? trackMat : (lalay || road.roadClass == "secondary" || road.roadClass == "tertiary" ? mainRoadMat : sideRoadMat);
                CreateRibbon("Road_" + SafeName(road.id), road.points, data.compression, width, 0f, 0.42f, material, roadRoot.transform, true);
                if (lalay)
                {
                    CreateRibbon("Lalay_Sidewalk_Left_" + SafeName(road.id), road.points, data.compression, 1.25f, 3.85f, 0.58f, sidewalkMat, sidewalkRoot.transform, true);
                    CreateRibbon("Lalay_Sidewalk_Right_" + SafeName(road.id), road.points, data.compression, 1.25f, -3.85f, 0.58f, sidewalkMat, sidewalkRoot.transform, true);
                }
            }

            foreach (WaterwayData waterway in data.waterways ?? Array.Empty<WaterwayData>())
            {
                if (waterway.points == null || waterway.points.Length < 2) continue;
                CreateRibbon("Waterway_" + SafeName(waterway.id), waterway.points, data.compression, waterway.waterClass == "river" ? 3.5f : 1.4f, 0f, 0.12f, waterwayMat, geography.transform, false);
            }

            ZoneData highland = (data.zones ?? Array.Empty<ZoneData>()).FirstOrDefault(z => z.id == "highland");
            if (highland != null)
            {
                CreateZoneOutline(highland, data.compression, highlandMat, districtRoot.transform);
                CreateGroundLabel(highland.displayName, ZoneCentre(highland.points, data.compression), new Color(1f, 0.84f, 0.92f), labelsRoot.transform, 7f);
            }

            Dictionary<string, AnchorData> anchors = (data.anchors ?? Array.Empty<AnchorData>()).ToDictionary(a => a.id, a => a);
            BuildLandmarkLots(anchors, data.compression, districtRoot.transform, labelsRoot.transform, farmMat, jettyMat);
            BuildDenseLalayHouses(data, lalayIds, anchors, wallMats, roofMats, housesRoot.transform);

            BuildLighting();
            BuildCameras(data, anchors, data.compression, bounds);
            CreateGroundLabel("LALAY — NARROW TWO-LANE HILL STREET", AnchorPosition(anchors, "grand_bay_credit_union", data.compression) + new Vector3(-40f, 0f, -12f), Color.white, labelsRoot.transform, 5.5f);
            CreateGroundLabel("BAY / FLAT END", AnchorPosition(anchors, "story_jetty", data.compression) + new Vector3(-15f, 0f, 18f), Color.white, labelsRoot.transform, 4.5f);

            EditorSceneManager.MarkSceneDirty(scene);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/UpIzUpMini/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"MINI-095 MAP LAB BUILD PASS: {data.roads.Length} road polylines, {housesRoot.transform.childCount} dense house masses, separate scene {ScenePath}.");
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
            Require(GameObject.Find("Camera_Overview") != null && GameObject.Find("Camera_Lalay") != null && GameObject.Find("Camera_Highland") != null, "Fixed cameras missing");
            int roads = GameObject.Find("Roads_OSM")?.transform.childCount ?? 0;
            int sidewalks = GameObject.Find("Lalay_Sidewalks")?.transform.childCount ?? 0;
            int houses = GameObject.Find("Lalay_Dense_House_Massing")?.transform.childCount ?? 0;
            int roadColliders = GameObject.Find("Roads_OSM")?.GetComponentsInChildren<MeshCollider>(true).Length ?? 0;
            Require(roads >= 80, $"Too few roads: {roads}");
            Require(roadColliders == roads, $"Every road must remain passable/collidable: roads={roads}, colliders={roadColliders}");
            Require(sidewalks >= 2, $"Lalay sidewalks missing: {sidewalks}");
            Require(houses >= 24, $"Dense Lalay massing too sparse: {houses}");
            int meshes = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            int materials = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(r => r.sharedMaterial).Where(m => m != null).Distinct().Count();
            Debug.Log($"MINI-095 MAP LAB VALIDATION PASS: roads={roads}, roadColliders={roadColliders}, sidewalks={sidewalks}, houseRoots={houses}, meshRenderers={meshes}, sharedMaterials={materials}, scene={scene.path}.");
        }

        [MenuItem("Up Iz Up Mini/MINI-095/Capture Map Lab Screenshots")]
        public static void CaptureScreenshots()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Directory.CreateDirectory(EvidenceFolder);
            Capture("Camera_Overview", "MapLab-Overview-1600x1000.png", 1600, 1000);
            Capture("Camera_Lalay", "MapLab-Lalay-1280x720.png", 1280, 720);
            Capture("Camera_Highland", "MapLab-Highland-1280x720.png", 1280, 720);
            Debug.Log("MINI-095 MAP LAB CAPTURE PASS: overview, Lalay gameplay-distance, and Highland gameplay-distance screenshots saved.");
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
            if (s_lalayGradeLines.Count == 0) return rawHeight;

            float closestDistance = float.MaxValue;
            float roadHeight = rawHeight;
            Vector3 query = new Vector3(x, 0f, z);
            foreach (Vector3[] line in s_lalayGradeLines)
            for (int i = 1; i < line.Length; i++)
            {
                float t;
                float distance = DistanceToSegmentXZ(query, line[i - 1], line[i], out t);
                if (distance >= closestDistance) continue;
                closestDistance = distance;
                roadHeight = Mathf.Lerp(line[i - 1].y, line[i].y, t);
            }

            // Preserve Dominica's surrounding relief, but make the narrow Lalay corridor
            // gentler for walking, bikes and mobile steering. The blend also grades yards.
            if (closestDistance >= 45f) return rawHeight;
            float blend = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(12f, 45f, closestDistance));
            return Mathf.Lerp(rawHeight, roadHeight, blend);
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
            s_roadClearances.Clear();
            HashSet<string> lalayIds = new HashSet<string>(data.lalayRoadIds ?? Array.Empty<string>());
            foreach (RoadData road in data.roads ?? Array.Empty<RoadData>())
            {
                if (road.points == null || road.points.Length < 2) continue;
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
            s_lalayBaseHeight = s_lalayGradeLines.SelectMany(line => line).Min(point => point.y);
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
            IEnumerable<Vector3> points = data.roads.SelectMany(r => r.points ?? Array.Empty<PointData>()).Select(p => World(p, data.compression));
            Bounds bounds = new Bounds(points.First(), Vector3.zero);
            foreach (Vector3 point in points) bounds.Encapsulate(point);
            bounds.Expand(new Vector3(80f, 0f, 80f));
            return bounds;
        }

        private static void BuildTerrain(Bounds bounds, Material material, Transform parent)
        {
            const int resolution = 65;
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

        private static GameObject CreateRibbon(string name, PointData[] source, float compression, float width, float lateralOffset, float yOffset, Material material, Transform parent, bool collider)
        {
            Vector3[] centres = TerrainConformingCentres(source, compression, yOffset, 3.5f);
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

        private static Vector3[] TerrainConformingCentres(PointData[] source, float compression, float yOffset, float maximumSpacing)
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

        private static void BuildLandmarkLots(Dictionary<string, AnchorData> anchors, float compression, Transform parent, Transform labels, Material farmMat, Material jettyMat)
        {
            AddLot(anchors, "highland_first_farm", "Highland_First_Farm", new Vector3(30f, 0.45f, 22f), farmMat, compression, parent, labels);
            AddLot(anchors, "upizup_block", "Up_Iz_Up_Block", new Vector3(18f, 0.55f, 13f), ColorMaterial("UpIzUpBlock", new Color(0.17f, 0.48f, 0.67f)), compression, parent, labels);
            AddLot(anchors, "dog_life_block", "Dog_Life_Block", new Vector3(20f, 0.55f, 13f), ColorMaterial("DogLifeBlock", new Color(0.55f, 0.16f, 0.14f)), compression, parent, labels);
            AddLot(anchors, "car_dealer", "Car_Dealer_Lot", new Vector3(24f, 0.45f, 17f), ColorMaterial("DealerLot", new Color(0.34f, 0.34f, 0.38f)), compression, parent, labels);
            AddLot(anchors, "grand_bay_primary_school", "Primary_School_Lot", new Vector3(32f, 0.55f, 22f), ColorMaterial("SchoolLot", new Color(0.12f, 0.48f, 0.43f)), compression, parent, labels);
            AddLot(anchors, "pierre_charles_secondary_school", "Secondary_School_Lot", new Vector3(38f, 0.55f, 26f), ColorMaterial("SecondarySchoolLot", new Color(0.11f, 0.40f, 0.38f)), compression, parent, labels);
            AddLot(anchors, "farmers_cooperative", "Farmers_Cooperative_Lot", new Vector3(21f, 0.55f, 14f), ColorMaterial("FarmersCoop", new Color(0.73f, 0.48f, 0.16f)), compression, parent, labels);
            Material futureLand = ColorMaterial("FutureFarmLand", new Color(0.27f, 0.43f, 0.20f));
            AddSilentLot(anchors, "highland_future_plot_02", "Future_Farm_Parcel_02_HiddenAtStart", new Vector3(27f, 0.22f, 20f), futureLand, compression, parent);
            AddSilentLot(anchors, "highland_future_plot_03", "Future_Farm_Parcel_03_HiddenAtStart", new Vector3(29f, 0.22f, 21f), futureLand, compression, parent);
            AddSilentLot(anchors, "highland_future_plot_04", "Future_Farm_Parcel_04_HiddenAtStart", new Vector3(25f, 0.22f, 19f), futureLand, compression, parent);

            Vector3 jetty = AnchorPosition(anchors, "story_jetty", compression);
            GameObject pier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pier.name = "Story_Jetty";
            pier.transform.SetParent(parent, false);
            pier.transform.position = jetty + new Vector3(8f, 1.1f, -8f);
            pier.transform.rotation = Quaternion.Euler(0f, -42f, 0f);
            pier.transform.localScale = new Vector3(4f, 1.2f, 30f);
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
            List<Vector3> polyline = data.roads.Where(r => lalayIds.Contains(r.id)).SelectMany(r => r.points.Select(p => World(p, data.compression, 0f))).ToList();
            if (polyline.Count < 2) return;
            List<(Vector3 point, Vector3 forward)> samples = SamplePolyline(polyline, 13.5f);
            System.Random random = new System.Random(95);
            int houseIndex = 0;
            foreach ((Vector3 point, Vector3 forward) sample in samples)
            {
                Vector3 side = new Vector3(-sample.forward.z, 0f, sample.forward.x).normalized;
                for (int sideSign = -1; sideSign <= 1; sideSign += 2)
                {
                    if (random.NextDouble() < 0.13) continue;
                    float setback = 9.2f + (float)random.NextDouble() * 2.8f;
                    Vector3 position = sample.point + side * sideSign * setback + sample.forward * ((float)random.NextDouble() - 0.5f) * 2.2f;
                    float width = 7.3f + (float)random.NextDouble() * 2.3f;
                    float depth = 6.4f + (float)random.NextDouble() * 2.2f;
                    if (OverlapsAnyRoad(position, width, depth, sample.forward)) continue;
                    string[] protectedLots = { "highland_first_farm", "highland_future_plot_02", "highland_future_plot_03", "highland_future_plot_04", "upizup_block", "dog_life_block", "car_dealer", "grand_bay_primary_school", "farmers_cooperative" };
                    if (protectedLots.Any(id => HorizontalDistance(position, AnchorPosition(anchors, id, data.compression)) < (id == "highland_first_farm" ? 23f : 15f))) continue;
                    bool twoStorey = random.NextDouble() < 0.24;
                    float height = twoStorey ? 6.4f : 3.5f;
                    position.y = HeightAt(position.x, position.z);
                    GameObject houseRoot = Child(parent.gameObject, $"Lalay_House_{houseIndex:00}");
                    houseRoot.transform.position = position;
                    houseRoot.transform.rotation = Quaternion.LookRotation(sample.forward, Vector3.up);
                    GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    body.name = "Body"; body.transform.SetParent(houseRoot.transform, false);
                    body.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
                    body.transform.localScale = new Vector3(width, height, depth);
                    body.GetComponent<Renderer>().sharedMaterial = walls[random.Next(walls.Length)];
                    GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    roof.name = "Corrugated_Roof"; roof.transform.SetParent(houseRoot.transform, false);
                    roof.transform.localPosition = new Vector3(0f, height + 0.35f, 0f);
                    roof.transform.localScale = new Vector3(width + 0.7f, 0.35f, depth + 0.8f);
                    roof.GetComponent<Renderer>().sharedMaterial = roofs[random.Next(roofs.Length)];
                    houseIndex++;
                }
            }
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
            string[] focusIds = { "dog_life_block", "car_dealer", "upizup_block", "highland_first_farm", "grand_bay_primary_school", "story_jetty", "pierre_charles_secondary_school" };
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
            CreateCamera("Camera_Highland", farm + new Vector3(-30f, 17f, 26f), farm + new Vector3(22f, 2f, -8f), false);
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
            if (lalay) return 6.2f;
            return roadClass switch { "secondary" => 6.5f, "tertiary" => 5.8f, "residential" => 4.2f, "unclassified" => 3.8f, "track" => 3.2f, "path" => 1.5f, _ => 2.8f };
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
