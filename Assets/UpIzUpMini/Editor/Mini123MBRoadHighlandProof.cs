using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Barmetler.RoadSystem;
using Barmetler.RoadSystem.Util;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UpIzUpMini.Editor
{
    /// <summary>
    /// Extends the accepted Lalay spline proof to the rural Highland inroad.
    /// Highland deliberately receives no sidewalks, kerbs or frontage ribbons.
    /// </summary>
    public static class Mini123MBRoadHighlandProof
    {
        private const string LalayProofScene = "Assets/UpIzUpMini/Scenes/MapLab_MBRoad_LalayProof.unity";
        private const string CombinedProofScene = "Assets/UpIzUpMini/Scenes/MapLab_MBRoad_LalayHighlandProof.unity";
        private const string LiveScene = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        private const string SourceMapScene = "Assets/UpIzUpMini/Scenes/MapLab_LalayHighland.unity";
        private const string DataPath = "Assets/UpIzUpMini/Maps/GrandBayPhase1MapData.json";
        private const string StagingFolder = "Assets/UpIzUpMini/Maps/Regions/dm-dom-grand-bay-lalay-highland-v1/Staging/MBRoadHighlandProof";
        private const string SourceMeshPath = StagingFolder + "/HighlandInroadSourceMesh.asset";
        private const string GeneratedMeshPath = StagingFolder + "/MBRoad_user_highland_lalay_inroad_Generated.asset";
        private const string DirtSourceMeshPath = StagingFolder + "/RuralDirtTrackSourceMesh.asset";
        private const string BridgeApronMeshPath = StagingFolder + "/HighlandBridgeConnectionAprons.asset";
        private const string LalayConnectionMeshPath = StagingFolder + "/HighlandLalayConnectionApron.asset";
        private const string AsphaltMaterialPath = "Assets/UpIzUpMini/Maps/Regions/dm-dom-grand-bay-lalay-highland-v1/Staging/MBRoadLalayProof/LalayAsphalt_LineFree.mat";
        private const string DirtMaterialPath = "Assets/UpIzUpMini/Maps/MapLab/Materials/DirtTrack.mat";
        private const string HighlandRoadId = "user/highland_lalay_inroad";
        private const string GapConnectorId = "connector/00_way_387239000_way_23042701";
        private const string GapConnectorLegacyName = "Road_Connector_00_way_387239000_way_23042701";
        private static readonly string[] AdditionalRoadIds =
        {
            "user/highland_farm_spur",
            "user/lalay_backstreet",
            "user/lalay_inland_coastal_connector",
            "way/180962530",
            "way/254679575",
            "way/25802980",
            "way/387239000"
        };
        private const float RoadWidth = 6.2f;
        private const float SourceTileLength = 4f;
        private const float SurfaceLift = 0.025f;
        private static string EvidenceFolder => Path.Combine("Logs", "Tasks", "MINI-123");

        [Serializable]
        private sealed class MapData
        {
            public float compression = 0.3333333f;
            public RoadData[] roads;
        }

        [Serializable]
        private sealed class RoadData
        {
            public string id;
            public string roadClass;
            public string surface;
            public PointData[] points;
        }

        [Serializable]
        private sealed class PointData
        {
            public float x;
            public float z;
        }

        [MenuItem("Up Iz Up Mini/MINI-123/Build Rural Highland MB Road Proof")]
        public static void Build()
        {
            string sourceHash = Sha256(SourceMapScene);
            string liveHash = Sha256(LiveScene);
            Mini122MBRoadLalayUpgrade.Build();
            EnsureAssetFolder(StagingFolder);
            Directory.CreateDirectory(EvidenceFolder);

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CombinedProofScene) != null)
                AssetDatabase.DeleteAsset(CombinedProofScene);
            Require(AssetDatabase.CopyAsset(LalayProofScene, CombinedProofScene), "Could not copy the accepted Lalay proof.");
            AssetDatabase.Refresh();

            Scene scene = EditorSceneManager.OpenScene(CombinedProofScene, OpenSceneMode.Single);
            GameObject mapRoot = GameObject.Find("MapLab_LalayHighland");
            Require(mapRoot != null, "Map Lab root is missing.");
            int sidewalkCountBefore = CountNamedObjects("Sidewalk") + CountNamedObjects("KerbRamp") + CountNamedObjects("Frontage");

            MapData data = LoadData();
            RoadData route = data.roads.FirstOrDefault(road => road.id == HighlandRoadId);
            Require(route?.points != null && route.points.Length >= 2, "Approved Highland–Lalay inroad data is missing.");
            GameObject legacyRoad = GameObject.Find("Road_" + SafeName(HighlandRoadId));
            Require(legacyRoad != null, "Approved legacy Highland inroad is missing.");
            MeshCollider legacyCollider = legacyRoad.GetComponent<MeshCollider>();
            Require(legacyCollider != null && legacyCollider.sharedMesh != null, "Approved Highland inroad collider is missing.");
            Vector3[] sampledRoute = SampleApprovedCenterline(route, data.compression, legacyCollider);

            GameObject backupRoot = new GameObject("LegacyNonLalayRoads_Backup_DISABLED");
            backupRoot.transform.SetParent(mapRoot.transform, false);
            legacyRoad.transform.SetParent(backupRoot.transform, true);

            Material asphalt = AssetDatabase.LoadAssetAtPath<Material>(AsphaltMaterialPath);
            Require(asphalt != null && asphalt.mainTexture == null, "Line-free Lalay asphalt material is missing or uses painted lines.");
            Material dirt = AssetDatabase.LoadAssetAtPath<Material>(DirtMaterialPath);
            Require(dirt != null, "Dirt-track material is missing.");
            Mesh sourceMesh = CreateOrReplaceSourceMesh(SourceMeshPath, RoadWidth, "Paved Two-Vehicle Road 6.2m Source Tile");
            Mesh dirtSourceMesh = CreateOrReplaceSourceMesh(DirtSourceMeshPath, 4.8f, "Rural Dirt Track 4.8m Source Tile");
            GameObject systemRoot = new GameObject("MBRoadSystem_HighlandProof_ACTIVE");
            systemRoot.transform.SetParent(mapRoot.transform, false);
            Barmetler.RoadSystem.RoadSystem roadSystem = systemRoot.AddComponent<Barmetler.RoadSystem.RoadSystem>();
            roadSystem.ShowDebugInfo = false;
            roadSystem.ShowEdgeWeights = false;

            GameObject sourceObject = new GameObject("Highland_6_2m_SourceMesh_DO_NOT_RENDER");
            sourceObject.transform.SetParent(systemRoot.transform, false);
            sourceObject.hideFlags = HideFlags.HideInHierarchy;
            MeshFilter sourceFilter = sourceObject.AddComponent<MeshFilter>();
            sourceFilter.sharedMesh = sourceMesh;

            GameObject dirtSourceObject = new GameObject("Highland_Dirt_4_8m_SourceMesh_DO_NOT_RENDER");
            dirtSourceObject.transform.SetParent(systemRoot.transform, false);
            dirtSourceObject.hideFlags = HideFlags.HideInHierarchy;
            MeshFilter dirtSourceFilter = dirtSourceObject.AddComponent<MeshFilter>();
            dirtSourceFilter.sharedMesh = dirtSourceMesh;

            GameObject generatedRoad = CreateMBRoad(systemRoot.transform, HighlandRoadId, sourceFilter, asphalt, sampledRoute);
            PersistGeneratedMesh(generatedRoad, HighlandRoadId);
            CreateBridgeConnectionAprons(systemRoot.transform, generatedRoad.GetComponent<MeshCollider>(), sampledRoute, asphalt);
            CreateHighlandLalayConnectionApron(systemRoot.transform, generatedRoad.GetComponent<MeshCollider>(), sampledRoute, asphalt);

            foreach (string roadId in AdditionalRoadIds)
            {
                RoadData roadData = data.roads.FirstOrDefault(road => road.id == roadId);
                Require(roadData?.points != null && roadData.points.Length >= 2, $"Mapped phase-one route is missing: {roadId}.");
                GameObject legacy = GameObject.Find("Road_" + SafeName(roadId));
                Require(legacy != null, $"Legacy phase-one road is missing: {roadId}.");
                MeshCollider legacyMeshCollider = legacy.GetComponent<MeshCollider>();
                Require(legacyMeshCollider != null && legacyMeshCollider.sharedMesh != null, $"Legacy road collider is missing: {roadId}.");
                Vector3[] approvedRoute = SampleApprovedCenterline(roadData, data.compression, legacyMeshCollider);
                legacy.transform.SetParent(backupRoot.transform, true);
                bool dirtRoad = roadData.roadClass == "track" || roadData.surface == "dirt";
                GameObject generated = CreateMBRoad(systemRoot.transform, roadId,
                    dirtRoad ? dirtSourceFilter : sourceFilter, dirtRoad ? dirt : asphalt, approvedRoute);
                PersistGeneratedMesh(generated, roadId);
            }

            GameObject legacyGapConnector = GameObject.Find(GapConnectorLegacyName);
            Require(legacyGapConnector != null, "Measured way/387239000-to-way/23042701 connector is missing.");
            MeshCollider legacyGapCollider = legacyGapConnector.GetComponent<MeshCollider>();
            Require(legacyGapCollider != null && legacyGapCollider.sharedMesh != null, "Measured gap connector collider is missing.");
            Vector3[] connectorRoute = ExtractStraightRibbonCenterline(legacyGapCollider);
            legacyGapConnector.transform.SetParent(backupRoot.transform, true);
            GameObject generatedConnector = CreateMBRoad(systemRoot.transform, GapConnectorId, sourceFilter, asphalt, connectorRoute);
            PersistGeneratedMesh(generatedConnector, GapConnectorId);

            backupRoot.SetActive(false);
            Require(sidewalkCountBefore == CountNamedObjects("Sidewalk") + CountNamedObjects("KerbRamp") + CountNamedObjects("Frontage"),
                "Highland proof must not generate sidewalks, kerbs or frontage strips.");

            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene, CombinedProofScene), "Could not save combined road proof.");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Require(sourceHash == Sha256(SourceMapScene), "Safety stop: source Map Lab changed during proof generation.");
            Require(liveHash == Sha256(LiveScene), "Safety stop: live gameplay scene changed during proof generation.");
            Debug.Log("MINI-123 BUILD PASS: full remaining phase-one road network converted in a separate proof with zero generated sidewalks; source and live scenes unchanged.");
        }

        [MenuItem("Up Iz Up Mini/MINI-123/Validate Rural Highland MB Road Proof")]
        public static void Validate()
        {
            Scene scene = EditorSceneManager.OpenScene(CombinedProofScene, OpenSceneMode.Single);
            Require(scene.IsValid(), "Combined proof scene is invalid.");
            GameObject root = GameObject.Find("MBRoadSystem_HighlandProof_ACTIVE");
            Require(root != null && root.activeInHierarchy, "Highland MB Road System root is missing.");
            Require(root.GetComponentsInChildren<Transform>(true).All(transform =>
                    !ContainsAny(transform.name, "sidewalk", "kerb", "curb", "frontage")),
                "Highland MB proof contains an urban sidewalk/kerb/frontage object.");

            Material asphalt = AssetDatabase.LoadAssetAtPath<Material>(AsphaltMaterialPath);
            Material dirt = AssetDatabase.LoadAssetAtPath<Material>(DirtMaterialPath);
            MapData data = LoadData();
            string[] expectedIds = new[] { HighlandRoadId }.Concat(AdditionalRoadIds).Concat(new[] { GapConnectorId }).ToArray();
            int totalVertices = 0;
            foreach (string roadId in expectedIds)
            {
                GameObject generated = GameObject.Find("MBRoad_" + SafeName(roadId));
                Require(generated != null, $"Generated phase-one road is missing: {roadId}.");
                Road road = generated.GetComponent<Road>();
                MeshFilter filter = generated.GetComponent<MeshFilter>();
                MeshCollider collider = generated.GetComponent<MeshCollider>();
                MeshRenderer renderer = generated.GetComponent<MeshRenderer>();
                int expectedSegments = roadId == GapConnectorId ? 1 : data.roads.First(item => item.id == roadId).points.Length - 1;
                Require(road != null && road.NumSegments == expectedSegments, $"Spline segment count changed: {roadId}.");
                Require(filter?.sharedMesh != null && filter.sharedMesh.vertexCount > 0 && filter.sharedMesh.vertexCount < 5000,
                    $"Road mesh is missing or exceeds the per-road mobile budget: {roadId}.");
                Require(collider != null && collider.sharedMesh == filter.sharedMesh, $"Road collider does not match its mesh: {roadId}.");
                bool dirtRoad = roadId == "user/highland_farm_spur";
                Require(renderer != null && renderer.sharedMaterial == (dirtRoad ? dirt : asphalt),
                    $"Road uses the wrong district surface material: {roadId}.");
                totalVertices += filter.sharedMesh.vertexCount;
            }
            Require(asphalt != null && asphalt.mainTexture == null, "Paved phase-one roads must remain line-free.");
            Require(root.GetComponentsInChildren<Road>(true).Length == expectedIds.Length,
                "Unexpected active MB-road count in the remaining-network proof.");
            Transform backup = FindTransformIncludingInactive("LegacyNonLalayRoads_Backup_DISABLED");
            Require(backup != null && !backup.gameObject.activeSelf && backup.childCount == expectedIds.Length,
                "Every replaced road must remain present in the disabled rollback root.");
            GameObject aprons = GameObject.Find("HighlandBridgeConnectionAprons_Continuous");
            Require(aprons != null && aprons.GetComponent<MeshCollider>()?.sharedMesh == aprons.GetComponent<MeshFilter>()?.sharedMesh,
                "Highland bridge transition aprons are missing or not collidable.");
            Renderer deckRenderer = FindTransformIncludingInactive("Bridge_03_user_highland_lalay_inroad")?.Find("Driveable_Deck")?.GetComponent<Renderer>();
            Require(deckRenderer != null && HorizontalBoundsOverlap(aprons.GetComponent<Renderer>().bounds, deckRenderer.bounds),
                "Bridge transition aprons do not overlap the driveable bridge deck.");
            GameObject lalayConnection = GameObject.Find("HighlandLalayConnectionApron_Rounded");
            Require(lalayConnection != null && lalayConnection.GetComponent<MeshCollider>()?.sharedMesh == lalayConnection.GetComponent<MeshFilter>()?.sharedMesh,
                "Rounded Highland–Lalay intersection apron is missing or not collidable.");

            Debug.Log($"MINI-123 VALIDATION PASS: roads={expectedIds.Length}, generatedSidewalks=0, roadVertices={totalVertices}, legacyBackups={backup.childCount}, lineFree=True, dirtFarmSpur=True.");
        }

        [MenuItem("Up Iz Up Mini/MINI-123/Capture Rural Highland MB Road Proof")]
        public static void Capture()
        {
            Scene scene = EditorSceneManager.OpenScene(CombinedProofScene, OpenSceneMode.Single);
            Directory.CreateDirectory(EvidenceFolder);
            CaptureHighlandPlayerHeight(scene, Path.Combine(EvidenceFolder, "MBRoad-Highland-NoSidewalk-PlayerHeight-1280x720.png"));
            CaptureOverhead(scene, Path.Combine(EvidenceFolder, "MBRoad-Highland-NoSidewalk-Overhead-1280x720.png"));
            CaptureHighlandBridge(scene, Path.Combine(EvidenceFolder, "MBRoad-Highland-BridgeConnection-1280x720.png"));
            CaptureHighlandLalayConnection(scene, Path.Combine(EvidenceFolder, "MBRoad-Highland-LalayConnection-1280x720.png"));
            CaptureHighlandLalayDriveLine(scene, Path.Combine(EvidenceFolder, "MBRoad-Highland-Lalay-DriveLine-1280x720.png"));
            CaptureHighlandApproachDriveLine(scene, Path.Combine(EvidenceFolder, "MBRoad-Highland-Bridge-HighlandApproach-1280x720.png"));
            CaptureCompleteNetworkOverhead(scene, Path.Combine(EvidenceFolder, "MBRoad-Complete-Remaining-Network-Overhead-1280x720.png"));
            Debug.Log("MINI-123 CAPTURE PASS: Highland bridge and complete no-sidewalk network screenshots saved.");
        }

        public static void BuildValidateCapture()
        {
            Build();
            Validate();
            Capture();
        }

        public static void SurveyRemainingRoads()
        {
            EditorSceneManager.OpenScene(CombinedProofScene, OpenSceneMode.Single);
            Transform roadsRoot = GameObject.Find("Roads_OSM")?.transform;
            Require(roadsRoot != null, "Roads_OSM root is missing.");
            foreach (MeshCollider collider in roadsRoot.GetComponentsInChildren<MeshCollider>(true)
                         .Where(item => item.gameObject.activeInHierarchy)
                         .OrderBy(item => item.name))
                Debug.Log($"MINI-123 ROAD SURVEY: name={collider.name}, vertices={collider.sharedMesh?.vertexCount ?? 0}, bounds={collider.bounds}");
            Transform bridgesRoot = GameObject.Find("Bridges")?.transform;
            if (bridgesRoot != null)
                foreach (MeshCollider collider in bridgesRoot.GetComponentsInChildren<MeshCollider>(true)
                             .Where(item => item.gameObject.activeInHierarchy)
                             .OrderBy(item => item.name))
                    Debug.Log($"MINI-123 BRIDGE SURVEY: path={GetPath(collider.transform)}, bounds={collider.bounds}");
        }

        private static GameObject CreateMBRoad(Transform parent, string roadId, MeshFilter sourceFilter, Material material, Vector3[] points)
        {
            GameObject go = new GameObject("MBRoad_" + SafeName(roadId));
            go.transform.SetParent(parent, false);
            Road road = go.AddComponent<Road>();
            road.direction = RoadDirection.Bidirectional;
            road.RefreshEndPoints(false);
            road.MovePoint(0, points[0]);
            road.MovePoint(3, points[1]);
            for (int i = 2; i < points.Length; i++) road.AppendSegment(points[i], false, Vector3.up);
            road.AutoSetControlPoints = true;
            road.AutoSetAllControlPoints();

            MeshFilter filter = go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            MeshCollider collider = go.AddComponent<MeshCollider>();
            RoadMeshGenerator generator = go.AddComponent<RoadMeshGenerator>();
            generator.settings = new RoadMeshGenerator.RoadMeshSettings
            {
                SourceOrientation = MeshConversion.MeshOrientation.Presets["UNITY"],
                uvOffset = new Vector2(0f, 0.25f)
            };
            generator.SourceMesh = sourceFilter;
            generator.AutoGenerate = false;
            generator.GenerateRoadMesh(1f);
            Require(filter.sharedMesh != null && filter.sharedMesh.vertexCount > 0, $"MB Road System generated no mesh for {roadId}.");
            collider.sharedMesh = filter.sharedMesh;
            return go;
        }

        private static Vector3[] SampleApprovedCenterline(RoadData route, float compression, MeshCollider collider)
        {
            Physics.SyncTransforms();
            List<Vector3> result = new List<Vector3>();
            foreach (PointData point in route.points)
            {
                float x = point.x * compression, z = point.z * compression;
                Ray ray = new Ray(new Vector3(x, collider.bounds.max.y + 25f, z), Vector3.down);
                Require(collider.Raycast(ray, out RaycastHit hit, collider.bounds.size.y + 50f),
                    $"Could not sample approved Highland height at ({x:F2}, {z:F2}).");
                result.Add(new Vector3(x, hit.point.y + SurfaceLift, z));
            }
            return result.ToArray();
        }

        private static Vector3[] ExtractStraightRibbonCenterline(MeshCollider collider)
        {
            Vector3[] world = collider.sharedMesh.vertices.Select(collider.transform.TransformPoint).ToArray();
            Require(world.Length >= 4, "Measured gap connector mesh has too few vertices.");
            Vector2 mean = new Vector2(world.Average(vertex => vertex.x), world.Average(vertex => vertex.z));
            float xx = 0f, zz = 0f, xz = 0f;
            foreach (Vector3 vertex in world)
            {
                float x = vertex.x - mean.x, z = vertex.z - mean.y;
                xx += x * x; zz += z * z; xz += x * z;
            }
            float angle = 0.5f * Mathf.Atan2(2f * xz, xx - zz);
            Vector2 axis = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
            float[] projections = world.Select(vertex => Vector2.Dot(new Vector2(vertex.x, vertex.z) - mean, axis)).ToArray();
            float min = projections.Min(), max = projections.Max();
            float tolerance = Mathf.Max(0.08f, (max - min) * 0.03f);
            Vector3 start = Average(world.Where((vertex, index) => projections[index] <= min + tolerance));
            Vector3 end = Average(world.Where((vertex, index) => projections[index] >= max - tolerance));
            start.y = SampleColliderHeight(collider, start, start.y) + SurfaceLift;
            end.y = SampleColliderHeight(collider, end, end.y) + SurfaceLift;
            Require(HorizontalDistance(start, end) > 1f, "Measured gap connector centreline is degenerate.");
            return new[] { start, end };
        }

        private static Vector3 Average(IEnumerable<Vector3> points)
        {
            Vector3[] values = points.ToArray();
            Require(values.Length > 0, "Cannot average an empty road endpoint set.");
            Vector3 total = Vector3.zero;
            foreach (Vector3 value in values) total += value;
            return total / values.Length;
        }

        private static Mesh CreateOrReplaceSourceMesh(string assetPath, float width, string meshName)
        {
            AssetDatabase.DeleteAsset(assetPath);
            float half = width * 0.5f;
            Mesh mesh = new Mesh { name = meshName };
            mesh.vertices = new[]
            {
                new Vector3(-half, 0f, 0f), new Vector3(half, 0f, 0f),
                new Vector3(-half, 0f, SourceTileLength), new Vector3(half, 0f, SourceTileLength)
            };
            mesh.normals = Enumerable.Repeat(Vector3.up, 4).ToArray();
            mesh.tangents = Enumerable.Repeat(new Vector4(1f, 0f, 0f, 1f), 4).ToArray();
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0.25f), new Vector2(1f, 0.25f) };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, assetPath);
            return AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
        }

        private static void PersistGeneratedMesh(GameObject road, string roadId)
        {
            string assetPath = roadId == HighlandRoadId ? GeneratedMeshPath : StagingFolder + "/MBRoad_" + SafeName(roadId) + "_Generated.asset";
            AssetDatabase.DeleteAsset(assetPath);
            MeshFilter filter = road.GetComponent<MeshFilter>();
            Mesh saved = UnityEngine.Object.Instantiate(filter.sharedMesh);
            saved.name = "MBRoad " + roadId + " Generated Mesh";
            AssetDatabase.CreateAsset(saved, assetPath);
            filter.sharedMesh = saved;
            road.GetComponent<MeshCollider>().sharedMesh = saved;
        }

        private static void CreateBridgeConnectionAprons(Transform parent, MeshCollider roadCollider, Vector3[] route, Material asphalt)
        {
            Transform deckTransform = FindTransformIncludingInactive("Bridge_03_user_highland_lalay_inroad")?.Find("Driveable_Deck");
            Require(deckTransform != null, "Highland–Lalay bridge deck is missing.");
            Renderer deckRenderer = deckTransform.GetComponent<Renderer>();
            Require(deckRenderer != null, "Highland bridge renderer is missing.");
            Collider terrainCollider = FindTerrainCollider();
            Require(terrainCollider != null, "Map Lab terrain collider is missing for bridge grading.");
            Bounds deckBounds = deckRenderer.bounds;
            int nearestSegment = 0;
            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < route.Length - 1; i++)
            {
                Vector3 closest = ClosestPointOnHorizontalSegment(deckBounds.center, route[i], route[i + 1]);
                float distance = Vector2.Distance(new Vector2(closest.x, closest.z), new Vector2(deckBounds.center.x, deckBounds.center.z));
                if (distance < nearestDistance) { nearestDistance = distance; nearestSegment = i; }
            }
            Vector3 direction = Vector3.ProjectOnPlane(route[nearestSegment + 1] - route[nearestSegment], Vector3.up).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;
            float projectedHalfLength = Mathf.Abs(direction.x) * deckBounds.extents.x + Mathf.Abs(direction.z) * deckBounds.extents.z;
            float halfWidth = RoadWidth * 0.5f;
            float deckHalfWidth = halfWidth;
            MeshFilter deckFilter = deckTransform.GetComponent<MeshFilter>();
            if (deckFilter != null && deckFilter.sharedMesh != null)
            {
                foreach (Vector3 localVertex in deckFilter.sharedMesh.vertices)
                {
                    Vector3 worldVertex = deckTransform.TransformPoint(localVertex);
                    deckHalfWidth = Mathf.Max(deckHalfWidth, Mathf.Abs(Vector3.Dot(worldVertex - deckBounds.center, side)));
                }
                deckHalfWidth = Mathf.Min(deckHalfWidth, halfWidth + 1.25f);
            }
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uv = new List<Vector2>();

            foreach (float sign in new[] { -1f, 1f })
            {
                const int transitionSegments = 8;
                float innerDistance = projectedHalfLength - 0.9f;
                float outerDistance = projectedHalfLength + 4.25f;
                Vector3 outerCentre = deckBounds.center + direction * sign * outerDistance;
                float deckY = deckBounds.max.y + 0.028f;
                float roadY = SampleGroundSurface(outerCentre, deckY, roadCollider, terrainCollider) + 0.028f;
                int stripStart = vertices.Count;
                for (int i = 0; i <= transitionSegments; i++)
                {
                    float t = i / (float)transitionSegments;
                    float distance = Mathf.Lerp(innerDistance, outerDistance, t);
                    Vector3 centre = deckBounds.center + direction * sign * distance;
                    float intendedY = Mathf.Lerp(deckY, roadY, Mathf.SmoothStep(0f, 1f, t));
                    centre.y = Mathf.Max(intendedY, SampleGroundSurface(centre, intendedY, roadCollider, terrainCollider) + 0.028f);
                    float sectionHalfWidth = Mathf.Lerp(deckHalfWidth, halfWidth, Mathf.SmoothStep(0f, 1f, t));
                    Vector3 left = centre - side * sectionHalfWidth;
                    Vector3 right = centre + side * sectionHalfWidth;
                    left.y = Mathf.Max(centre.y, SampleGroundSurface(left, centre.y, roadCollider, terrainCollider) + 0.028f);
                    right.y = Mathf.Max(centre.y, SampleGroundSurface(right, centre.y, roadCollider, terrainCollider) + 0.028f);
                    float maxCrossfall = Mathf.Tan(8f * Mathf.Deg2Rad) * (sectionHalfWidth * 2f);
                    if (left.y - right.y > maxCrossfall) right.y = left.y - maxCrossfall;
                    else if (right.y - left.y > maxCrossfall) left.y = right.y - maxCrossfall;
                    vertices.Add(left);
                    vertices.Add(right);
                    uv.Add(new Vector2(0f, t));
                    uv.Add(new Vector2(1f, t));
                }
                for (int i = 0; i < transitionSegments; i++)
                {
                    int index = stripStart + i * 2;
                    triangles.Add(index); triangles.Add(index + 2); triangles.Add(index + 1);
                    triangles.Add(index + 1); triangles.Add(index + 2); triangles.Add(index + 3);
                    triangles.Add(index); triangles.Add(index + 1); triangles.Add(index + 2);
                    triangles.Add(index + 1); triangles.Add(index + 3); triangles.Add(index + 2);
                }
            }

            AssetDatabase.DeleteAsset(BridgeApronMeshPath);
            Mesh mesh = new Mesh { name = "Highland Bridge Continuous Road Aprons" };
            mesh.vertices = vertices.ToArray();
            mesh.uv = uv.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.normals = Enumerable.Repeat(Vector3.up, vertices.Count).ToArray();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, BridgeApronMeshPath);
            Mesh savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(BridgeApronMeshPath);
            GameObject apronObject = new GameObject("HighlandBridgeConnectionAprons_Continuous");
            apronObject.transform.SetParent(parent, false);
            apronObject.AddComponent<MeshFilter>().sharedMesh = savedMesh;
            apronObject.AddComponent<MeshRenderer>().sharedMaterial = asphalt;
            apronObject.AddComponent<MeshCollider>().sharedMesh = savedMesh;
        }

        private static void CreateHighlandLalayConnectionApron(Transform parent, MeshCollider highlandRoadCollider, Vector3[] route, Material asphalt)
        {
            const int segments = 48;
            const float radius = 4.45f;
            Vector3 lalayPoint = route[0];
            MeshCollider lalayCollider = GameObject.Find("MBRoad_way_22917921")?.GetComponent<MeshCollider>();
            Require(lalayCollider != null, "Lalay road collider is missing for the Highland connector grade.");
            Collider terrainCollider = FindTerrainCollider();
            Require(terrainCollider != null, "Map Lab terrain collider is missing for the Highland connector grade.");
            lalayPoint.y = SampleColliderHeight(lalayCollider, lalayPoint, lalayPoint.y) + 0.012f;
            Transform deckTransform = FindTransformIncludingInactive("Bridge_03_user_highland_lalay_inroad")?.Find("Driveable_Deck");
            Require(deckTransform != null, "Highland bridge is missing for its Lalay-side connector.");
            Bounds deckBounds = deckTransform.GetComponent<Renderer>().bounds;
            int nearestSegment = 0;
            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < route.Length - 1; i++)
            {
                Vector3 closest = ClosestPointOnHorizontalSegment(deckBounds.center, route[i], route[i + 1]);
                float distance = Vector2.Distance(new Vector2(closest.x, closest.z), new Vector2(deckBounds.center.x, deckBounds.center.z));
                if (distance < nearestDistance) { nearestDistance = distance; nearestSegment = i; }
            }
            Vector3 routeDirection = Vector3.ProjectOnPlane(route[nearestSegment + 1] - route[nearestSegment], Vector3.up).normalized;
            float projectedHalfLength = Mathf.Abs(routeDirection.x) * deckBounds.extents.x + Mathf.Abs(routeDirection.z) * deckBounds.extents.z;
            Vector3 endA = deckBounds.center + routeDirection * projectedHalfLength;
            Vector3 endB = deckBounds.center - routeDirection * projectedHalfLength;
            Vector3 bridgeExit = HorizontalDistance(endA, lalayPoint) < HorizontalDistance(endB, lalayPoint) ? endA : endB;
            bridgeExit.y = deckBounds.max.y + 0.008f;
            Vector3 direction = Vector3.ProjectOnPlane(lalayPoint - bridgeExit, Vector3.up).normalized;
            Require(direction.sqrMagnitude > 0.5f, "Highland–Lalay connector direction is invalid.");
            Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;
            float halfWidth = RoadWidth * 0.5f;
            const int rampSegments = 12;
            Vector3 bridgeJoin = bridgeExit - direction * 0.9f;
            bridgeJoin.y = bridgeExit.y;
            Vector3 bridgeSide = Vector3.Cross(Vector3.up, routeDirection).normalized;
            if (Vector3.Dot(bridgeSide, side) < 0f) bridgeSide = -bridgeSide;
            float bridgeHalfWidth = halfWidth;
            MeshFilter deckFilter = deckTransform.GetComponent<MeshFilter>();
            if (deckFilter != null && deckFilter.sharedMesh != null)
            {
                foreach (Vector3 localVertex in deckFilter.sharedMesh.vertices)
                {
                    Vector3 worldVertex = deckTransform.TransformPoint(localVertex);
                    bridgeHalfWidth = Mathf.Max(bridgeHalfWidth, Mathf.Abs(Vector3.Dot(worldVertex - deckBounds.center, bridgeSide)));
                }
                bridgeHalfWidth = Mathf.Min(bridgeHalfWidth, halfWidth + 1.25f);
            }
            Vector3[] vertices = new Vector3[segments + 1 + (rampSegments + 1) * 2];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[segments * 6 + rampSegments * 12];
            vertices[0] = lalayPoint;
            uv[0] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < segments; i++)
            {
                float angle = Mathf.PI * 2f * i / segments;
                vertices[i + 1] = lalayPoint + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                uv[i + 1] = new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f);
                int next = (i + 1) % segments + 1;
                int triangle = i * 6;
                triangles[triangle] = 0;
                triangles[triangle + 1] = i + 1;
                triangles[triangle + 2] = next;
                triangles[triangle + 3] = 0;
                triangles[triangle + 4] = next;
                triangles[triangle + 5] = i + 1;
            }
            int connectorVertex = segments + 1;
            for (int i = 0; i <= rampSegments; i++)
            {
                float t = i / (float)rampSegments;
                Vector3 centre = Vector3.Lerp(bridgeJoin, lalayPoint, t);
                float intendedY = Mathf.Lerp(bridgeExit.y, lalayPoint.y, t);
                // The legacy terrain rises slightly between the bridge deck and Lalay. Keep every
                // cross-section above the actual surface so no grass slit remains in the drive line.
                float sampledSurface = SampleGroundSurface(centre, intendedY, highlandRoadCollider, lalayCollider, terrainCollider);
                centre.y = Mathf.Max(intendedY, sampledSurface + 0.028f);
                Vector3 sectionSide = Vector3.Slerp(bridgeSide, side, Mathf.SmoothStep(0f, 1f, t)).normalized;
                float sectionHalfWidth = Mathf.Lerp(bridgeHalfWidth, halfWidth, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 2f)));
                Vector3 left = centre - sectionSide * sectionHalfWidth;
                Vector3 right = centre + sectionSide * sectionHalfWidth;
                left.y = Mathf.Max(centre.y, SampleGroundSurface(left, centre.y, highlandRoadCollider, lalayCollider, terrainCollider) + 0.028f);
                right.y = Mathf.Max(centre.y, SampleGroundSurface(right, centre.y, highlandRoadCollider, lalayCollider, terrainCollider) + 0.028f);

                // Bank toward the raised terrain side, but cap the crossfall so cars and bikes do
                // not meet a harsh sideways step. Raising the lower edge (never lowering the high
                // edge) guarantees that grass cannot break through the transition mesh.
                float maxCrossfall = Mathf.Tan(8f * Mathf.Deg2Rad) * (sectionHalfWidth * 2f);
                if (left.y - right.y > maxCrossfall) right.y = left.y - maxCrossfall;
                else if (right.y - left.y > maxCrossfall) left.y = right.y - maxCrossfall;
                int v = connectorVertex + i * 2;
                vertices[v] = left;
                vertices[v + 1] = right;
                uv[v] = new Vector2(0f, t);
                uv[v + 1] = new Vector2(1f, t);
            }
            int connectorTriangle = segments * 6;
            for (int i = 0; i < rampSegments; i++)
            {
                int v = connectorVertex + i * 2;
                int tri = connectorTriangle + i * 12;
                triangles[tri] = v; triangles[tri + 1] = v + 2; triangles[tri + 2] = v + 1;
                triangles[tri + 3] = v + 1; triangles[tri + 4] = v + 2; triangles[tri + 5] = v + 3;
                triangles[tri + 6] = v; triangles[tri + 7] = v + 1; triangles[tri + 8] = v + 2;
                triangles[tri + 9] = v + 1; triangles[tri + 10] = v + 3; triangles[tri + 11] = v + 2;
            }
            AssetDatabase.DeleteAsset(LalayConnectionMeshPath);
            Mesh mesh = new Mesh { name = "Highland Lalay Rounded Connection Apron" };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.normals = Enumerable.Repeat(Vector3.up, vertices.Length).ToArray();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, LalayConnectionMeshPath);
            Mesh saved = AssetDatabase.LoadAssetAtPath<Mesh>(LalayConnectionMeshPath);
            GameObject apron = new GameObject("HighlandLalayConnectionApron_Rounded");
            apron.transform.SetParent(parent, false);
            apron.AddComponent<MeshFilter>().sharedMesh = saved;
            apron.AddComponent<MeshRenderer>().sharedMaterial = asphalt;
            apron.AddComponent<MeshCollider>().sharedMesh = saved;
        }

        private static float SampleColliderHeight(MeshCollider collider, Vector3 point, float fallback)
        {
            Ray ray = new Ray(new Vector3(point.x, collider.bounds.max.y + 20f, point.z), Vector3.down);
            return collider.Raycast(ray, out RaycastHit hit, collider.bounds.size.y + 40f) ? hit.point.y : fallback;
        }

        private static float SampleGroundSurface(Vector3 point, float fallback, params Collider[] surfaces)
        {
            float highest = fallback;
            foreach (Collider surface in surfaces)
            {
                if (surface == null) continue;
                Ray ray = new Ray(new Vector3(point.x, surface.bounds.max.y + 20f, point.z), Vector3.down);
                if (surface.Raycast(ray, out RaycastHit hit, surface.bounds.size.y + 40f))
                    highest = Mathf.Max(highest, hit.point.y);
            }
            return highest;
        }

        private static Collider FindTerrainCollider()
        {
            Transform mesh = FindTransformIncludingInactive("Copernicus_GLO30_Terrain_Mesh");
            Collider collider = mesh?.GetComponent<Collider>() ?? mesh?.GetComponentInChildren<Collider>(true);
            if (collider != null) return collider;
            Transform terrain = FindTransformIncludingInactive("Copernicus_GLO30_Terrain");
            return terrain?.GetComponent<Collider>() ?? terrain?.GetComponentInChildren<Collider>(true);
        }

        private static Vector3 ClosestPointOnHorizontalSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            Vector2 p = new Vector2(point.x, point.z), av = new Vector2(a.x, a.z), bv = new Vector2(b.x, b.z);
            Vector2 ab = bv - av;
            float t = ab.sqrMagnitude > 0.0001f ? Mathf.Clamp01(Vector2.Dot(p - av, ab) / ab.sqrMagnitude) : 0f;
            Vector2 result = av + ab * t;
            return new Vector3(result.x, Mathf.Lerp(a.y, b.y, t), result.y);
        }

        private static bool HorizontalBoundsOverlap(Bounds a, Bounds b)
        {
            return a.min.x <= b.max.x && a.max.x >= b.min.x && a.min.z <= b.max.z && a.max.z >= b.min.z;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        }

        private static void CaptureOverhead(Scene scene, string outputPath)
        {
            MapData data = LoadData();
            RoadData road = data.roads.First(item => item.id == HighlandRoadId);
            List<Vector3> points = road.points.Select(point => new Vector3(point.x * data.compression, 0f, point.z * data.compression)).ToList();
            float minX = points.Min(point => point.x), maxX = points.Max(point => point.x);
            float minZ = points.Min(point => point.z), maxZ = points.Max(point => point.z);
            GameObject cameraObject = new GameObject("MINI123_Temporary_Highland_Overhead_Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3((minX + maxX) * 0.5f, 150f, (minZ + maxZ) * 0.5f);
            camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max((maxZ - minZ) * 0.5f + 18f, (maxX - minX) / 1.78f + 18f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.07f, 0.08f);
            CaptureCamera(camera, outputPath, 1280, 720);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }

        private static void CaptureExistingCamera(string name, string outputPath, int width, int height)
        {
            Camera camera = GameObject.Find(name)?.GetComponent<Camera>();
            Require(camera != null, "Evidence camera is missing: " + name);
            CaptureCamera(camera, outputPath, width, height);
        }

        private static void CaptureHighlandPlayerHeight(Scene scene, string outputPath)
        {
            GameObject generated = GameObject.Find("MBRoad_" + SafeName(HighlandRoadId));
            Require(generated != null, "Generated Highland road is missing for player-height evidence.");
            Road road = generated.GetComponent<Road>();
            Require(road != null && road.NumPoints >= 7, "Generated Highland spline lacks enough evidence points.");
            Vector3 eyeBase = generated.transform.TransformPoint(road[3]);
            Vector3 targetBase = generated.transform.TransformPoint(road[Mathf.Min(9, road.NumPoints - 1)]);
            Vector3 forward = Vector3.ProjectOnPlane(targetBase - eyeBase, Vector3.up).normalized;
            GameObject cameraObject = new GameObject("MINI123_Temporary_Highland_PlayerHeight_Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = eyeBase + Vector3.up * 1.65f - forward * 1.5f;
            // Judge the rural shoulder and road width at human eye level. Do
            // not aim down the district's grade like the old planning camera.
            camera.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            camera.fieldOfView = 58f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.07f, 0.08f);
            CaptureCamera(camera, outputPath, 1280, 720);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }

        private static void CaptureHighlandBridge(Scene scene, string outputPath)
        {
            Transform deck = FindTransformIncludingInactive("Bridge_03_user_highland_lalay_inroad")?.Find("Driveable_Deck");
            Require(deck != null, "Highland–Lalay bridge deck is missing.");
            Vector3 centre = deck.GetComponent<Renderer>().bounds.center;
            GameObject cameraObject = new GameObject("MINI123_Temporary_Highland_Bridge_Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = centre + new Vector3(-17f, 22f, -20f);
            camera.transform.rotation = Quaternion.LookRotation(centre - camera.transform.position, Vector3.up);
            camera.fieldOfView = 46f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.07f, 0.08f);
            CaptureCamera(camera, outputPath, 1280, 720);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }

        private static void CaptureHighlandLalayConnection(Scene scene, string outputPath)
        {
            Renderer apron = GameObject.Find("HighlandLalayConnectionApron_Rounded")?.GetComponent<Renderer>();
            Require(apron != null, "Highland–Lalay connection apron is missing for evidence.");
            Vector3 centre = apron.bounds.center;
            GameObject cameraObject = new GameObject("MINI123_Temporary_Highland_Lalay_Connection_Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = centre + new Vector3(-13f, 17f, -15f);
            camera.transform.rotation = Quaternion.LookRotation(centre - camera.transform.position, Vector3.up);
            camera.fieldOfView = 44f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.07f, 0.08f);
            CaptureCamera(camera, outputPath, 1280, 720);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }

        private static void CaptureHighlandLalayDriveLine(Scene scene, string outputPath)
        {
            Renderer apron = GameObject.Find("HighlandLalayConnectionApron_Rounded")?.GetComponent<Renderer>();
            Transform deck = FindTransformIncludingInactive("Bridge_03_user_highland_lalay_inroad")?.Find("Driveable_Deck");
            Require(apron != null && deck != null, "Highland–Lalay drive-line evidence targets are missing.");
            Vector3 deckCentre = deck.GetComponent<Renderer>().bounds.center;
            Vector3 towardLalay = Vector3.ProjectOnPlane(apron.bounds.center - deckCentre, Vector3.up).normalized;
            GameObject cameraObject = new GameObject("MINI123_Temporary_Highland_Lalay_DriveLine_Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = deckCentre - towardLalay * 2.7f + Vector3.up * 1.45f;
            Vector3 target = apron.bounds.center + towardLalay * 1.5f + Vector3.up * 0.35f;
            camera.transform.rotation = Quaternion.LookRotation(target - camera.transform.position, Vector3.up);
            camera.fieldOfView = 58f;
            camera.nearClipPlane = 0.05f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.07f, 0.08f);
            CaptureCamera(camera, outputPath, 1280, 720);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }

        private static void CaptureHighlandApproachDriveLine(Scene scene, string outputPath)
        {
            Transform deck = FindTransformIncludingInactive("Bridge_03_user_highland_lalay_inroad")?.Find("Driveable_Deck");
            Renderer road = GameObject.Find("MBRoad_user_highland_lalay_inroad")?.GetComponent<Renderer>();
            Require(deck != null && road != null, "Highland approach drive-line evidence targets are missing.");
            Vector3 deckCentre = deck.GetComponent<Renderer>().bounds.center;
            Vector3 towardHighland = Vector3.ProjectOnPlane(road.bounds.center - deckCentre, Vector3.up).normalized;
            GameObject cameraObject = new GameObject("MINI123_Temporary_Highland_Approach_DriveLine_Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = deckCentre - towardHighland * 2.7f + Vector3.up * 1.45f;
            Vector3 target = deckCentre + towardHighland * 17f + Vector3.up * 0.35f;
            camera.transform.rotation = Quaternion.LookRotation(target - camera.transform.position, Vector3.up);
            camera.fieldOfView = 58f;
            camera.nearClipPlane = 0.05f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.07f, 0.08f);
            CaptureCamera(camera, outputPath, 1280, 720);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }

        private static void CaptureCompleteNetworkOverhead(Scene scene, string outputPath)
        {
            GameObject root = GameObject.Find("MBRoadSystem_HighlandProof_ACTIVE");
            Require(root != null, "Complete-network evidence root is missing.");
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true).Where(renderer => renderer.enabled).ToArray();
            Require(renderers.Length > 0, "Complete-network evidence has no rendered roads.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            GameObject cameraObject = new GameObject("MINI123_Temporary_Complete_Network_Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(bounds.center.x, bounds.max.y + 220f, bounds.center.z);
            camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.extents.z + 16f, bounds.extents.x / 1.78f + 16f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.07f, 0.08f);
            CaptureCamera(camera, outputPath, 1280, 720);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }

        private static void CaptureCamera(Camera camera, string outputPath, int width, int height)
        {
            RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            camera.targetTexture = target;
            RenderTexture.active = target;
            camera.Render();
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(outputPath, image.EncodeToPNG());
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(image);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
        }

        private static MapData LoadData()
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(DataPath);
            Require(asset != null, "Map data is missing.");
            MapData data = JsonUtility.FromJson<MapData>(asset.text);
            Require(data?.roads != null, "Map data could not be parsed.");
            return data;
        }

        private static int CountNamedObjects(string fragment)
        {
            return Resources.FindObjectsOfTypeAll<Transform>().Count(transform =>
                transform.gameObject.scene.IsValid() && transform.name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool ContainsAny(string value, params string[] fragments)
        {
            return fragments.Any(fragment => value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static Transform FindTransformIncludingInactive(string name)
        {
            return Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(transform =>
                transform.name == name && transform.gameObject.scene.IsValid());
        }

        private static string GetPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }

        private static string SafeName(string value) => value.Replace('/', '_').Replace(' ', '_');

        private static string Sha256(string assetPath)
        {
            using SHA256 sha = SHA256.Create();
            using FileStream stream = File.OpenRead(assetPath);
            return BitConverter.ToString(sha.ComputeHash(stream));
        }

        private static void EnsureAssetFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("MINI-123: " + message);
        }
    }
}
