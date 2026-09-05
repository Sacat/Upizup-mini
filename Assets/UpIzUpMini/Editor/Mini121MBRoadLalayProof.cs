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
    /// Creates a rollback-safe MB Road System proof from the approved Lalay
    /// centerlines. The approved Map Lab and live gameplay scene are never saved.
    /// </summary>
    public static class Mini121MBRoadLalayProof
    {
        private const string SourceScene = "Assets/UpIzUpMini/Scenes/MapLab_LalayHighland.unity";
        private const string ProofScene = "Assets/UpIzUpMini/Scenes/MapLab_MBRoad_LalayProof.unity";
        private const string DataPath = "Assets/UpIzUpMini/Maps/GrandBayPhase1MapData.json";
        private const string StagingFolder = "Assets/UpIzUpMini/Maps/Regions/dm-dom-grand-bay-lalay-highland-v1/Staging/MBRoadLalayProof";
        private const string SourceMeshPath = StagingFolder + "/LalayRoadSourceMesh.asset";
        private const string MainRoadMaterialPath = "Assets/UpIzUpMini/Maps/MapLab/Materials/MainRoad.mat";
        private const float RoadWidth = 6.2f;
        private const float SourceTileLength = 4f;
        private const float SurfaceLift = 0.025f;
        private static readonly string[] LalayRoadIds = { "way/22917921", "way/23042701" };
        private static string EvidenceFolder => Path.Combine("Logs", "Tasks", "MINI-121");

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
            public PointData[] points;
        }

        [Serializable]
        private sealed class PointData
        {
            public float x;
            public float z;
        }

        [MenuItem("Up Iz Up Mini/MINI-121/Build MB Road Lalay Proof")]
        public static void Build()
        {
            Require(File.Exists(SourceScene), "Approved Map Lab source scene is missing.");
            Require(Directory.Exists("Packages/com.barmetler.roadsystem"), "MB Road System embedded package is missing.");
            EnsureAssetFolder(StagingFolder);
            Directory.CreateDirectory(EvidenceFolder);

            string sourceHashBefore = Sha256(SourceScene);
            Scene source = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
            CaptureExistingCamera("Camera_Lalay", Path.Combine(EvidenceFolder, "Legacy-Lalay-PlayerHeight-1280x720.png"), 1280, 720);
            CaptureOverhead(source, Path.Combine(EvidenceFolder, "Legacy-Lalay-Overhead-1600x1000.png"));

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ProofScene) != null)
                AssetDatabase.DeleteAsset(ProofScene);
            Require(AssetDatabase.CopyAsset(SourceScene, ProofScene), "Could not create the isolated proof-scene copy.");
            AssetDatabase.Refresh();

            Scene proof = EditorSceneManager.OpenScene(ProofScene, OpenSceneMode.Single);
            MapData data = LoadData();
            Dictionary<string, RoadData> routes = (data.roads ?? Array.Empty<RoadData>())
                .Where(road => LalayRoadIds.Contains(road.id))
                .ToDictionary(road => road.id, road => road);
            Require(routes.Count == LalayRoadIds.Length, "One or more approved Lalay polylines are missing.");

            GameObject mapRoot = GameObject.Find("MapLab_LalayHighland");
            Require(mapRoot != null, "Map Lab root is missing from the proof copy.");

            GameObject backupRoot = new GameObject("LegacyLalayRoad_Backup_DISABLED");
            backupRoot.transform.SetParent(mapRoot.transform, false);

            Dictionary<string, Vector3[]> sampledRoutes = new Dictionary<string, Vector3[]>();
            foreach (string id in LalayRoadIds)
            {
                GameObject legacy = GameObject.Find("Road_" + SafeName(id));
                Require(legacy != null, "Approved legacy Lalay road is missing: " + id);
                MeshCollider legacyCollider = legacy.GetComponent<MeshCollider>();
                Require(legacyCollider != null && legacyCollider.sharedMesh != null, "Legacy Lalay collider is missing: " + id);
                sampledRoutes[id] = SampleApprovedCenterline(routes[id], data.compression, legacyCollider);
                legacy.transform.SetParent(backupRoot.transform, true);
            }
            backupRoot.SetActive(false);

            Mesh sourceMesh = CreateOrReplaceSourceMesh();
            Material roadMaterial = AssetDatabase.LoadAssetAtPath<Material>(MainRoadMaterialPath);
            Require(roadMaterial != null, "Approved Map Lab main-road material is missing.");

            GameObject systemRoot = new GameObject("MBRoadSystem_LalayProof_ACTIVE");
            systemRoot.transform.SetParent(mapRoot.transform, false);
            Barmetler.RoadSystem.RoadSystem roadSystem = systemRoot.AddComponent<Barmetler.RoadSystem.RoadSystem>();
            roadSystem.ShowDebugInfo = false;
            roadSystem.ShowEdgeWeights = false;

            GameObject sourceObject = new GameObject("Lalay_6_2m_SourceMesh_DO_NOT_RENDER");
            sourceObject.transform.SetParent(systemRoot.transform, false);
            MeshFilter sourceFilter = sourceObject.AddComponent<MeshFilter>();
            sourceFilter.sharedMesh = sourceMesh;
            sourceObject.hideFlags = HideFlags.HideInHierarchy;

            foreach (string id in LalayRoadIds)
                CreateMBRoad(systemRoot.transform, sourceFilter, roadMaterial, id, sampledRoutes[id]);

            roadSystem.RebuildAllRoads();
            PersistGeneratedMeshes(systemRoot);

            EditorSceneManager.MarkSceneDirty(proof);
            Require(EditorSceneManager.SaveScene(proof, ProofScene), "Could not save MB Road Lalay proof scene.");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Require(sourceHashBefore == Sha256(SourceScene), "Safety stop: approved Map Lab source scene changed during proof generation.");
            Debug.Log("MINI-121 BUILD PASS: isolated MB Road System Lalay proof created; approved source and live gameplay scene were not modified.");
        }

        [MenuItem("Up Iz Up Mini/MINI-121/Validate MB Road Lalay Proof")]
        public static void Validate()
        {
            Scene scene = EditorSceneManager.OpenScene(ProofScene, OpenSceneMode.Single);
            Require(scene.IsValid(), "Proof scene could not be opened.");
            GameObject active = GameObject.Find("MBRoadSystem_LalayProof_ACTIVE");
            Require(active != null && active.activeInHierarchy, "Active MB Road System proof root is missing.");
            Barmetler.RoadSystem.RoadSystem system = active.GetComponent<Barmetler.RoadSystem.RoadSystem>();
            Require(system != null, "MB Road System component is missing.");

            Transform backup = FindTransformIncludingInactive("LegacyLalayRoad_Backup_DISABLED");
            Require(backup != null && !backup.gameObject.activeSelf, "Legacy rollback road must remain present and disabled in the proof.");
            Require(backup.childCount == LalayRoadIds.Length, "Legacy rollback root must contain both Lalay road polylines.");

            MapData data = LoadData();
            foreach (string id in LalayRoadIds)
            {
                RoadData expected = data.roads.First(road => road.id == id);
                GameObject generated = GameObject.Find("MBRoad_" + SafeName(id));
                Require(generated != null, "Generated MB road missing: " + id);
                Road road = generated.GetComponent<Road>();
                RoadMeshGenerator generator = generated.GetComponent<RoadMeshGenerator>();
                MeshFilter filter = generated.GetComponent<MeshFilter>();
                MeshCollider collider = generated.GetComponent<MeshCollider>();
                Require(road != null && generator != null, "MB Road System components missing: " + id);
                Require(filter != null && filter.sharedMesh != null && filter.sharedMesh.vertexCount > 0, "Generated road mesh missing: " + id);
                Require(collider != null && collider.sharedMesh == filter.sharedMesh, "Generated road collision missing or mismatched: " + id);
                Require(road.NumSegments == expected.points.Length - 1, $"Bezier segment count changed for {id}: {road.NumSegments}");

                Vector3 expectedStart = Horizontal(expected.points[0], data.compression);
                Vector3 expectedEnd = Horizontal(expected.points[expected.points.Length - 1], data.compression);
                Vector3 actualStart = generated.transform.TransformPoint(road[0]);
                Vector3 actualEnd = generated.transform.TransformPoint(road[-1]);
                Require(HorizontalDistance(expectedStart, actualStart) <= 0.05f, "Start route drifted: " + id);
                Require(HorizontalDistance(expectedEnd, actualEnd) <= 0.05f, "End route drifted: " + id);
            }

            int vertices = active.GetComponentsInChildren<MeshFilter>(true)
                .Where(filter => filter.gameObject.name.StartsWith("MBRoad_", StringComparison.Ordinal))
                .Sum(filter => filter.sharedMesh != null ? filter.sharedMesh.vertexCount : 0);
            Require(vertices < 5000, "Lalay proof exceeds the first-pass mobile road-mesh budget: " + vertices);
            Require(File.Exists(SourceScene) && File.Exists(ProofScene), "Source/proof scene pair is incomplete.");
            Debug.Log($"MINI-121 VALIDATION PASS: MB roads={LalayRoadIds.Length}, legacy backups={backup.childCount}, generatedVertices={vertices}, exact X/Z endpoints retained, source={SourceScene}, proof={ProofScene}.");
        }

        [MenuItem("Up Iz Up Mini/MINI-121/Capture MB Road Lalay Proof")]
        public static void Capture()
        {
            Scene proof = EditorSceneManager.OpenScene(ProofScene, OpenSceneMode.Single);
            Directory.CreateDirectory(EvidenceFolder);
            CaptureExistingCamera("Camera_Lalay", Path.Combine(EvidenceFolder, "MBRoad-Lalay-PlayerHeight-1280x720.png"), 1280, 720);
            CaptureOverhead(proof, Path.Combine(EvidenceFolder, "MBRoad-Lalay-Overhead-1600x1000.png"));
            Debug.Log("MINI-121 CAPTURE PASS: fixed legacy/new Lalay comparison screenshots saved.");
        }

        public static void BuildValidateCapture()
        {
            Build();
            Validate();
            Capture();
        }

        private static void CreateMBRoad(Transform parent, MeshFilter sourceFilter, Material material, string id, Vector3[] points)
        {
            Require(points != null && points.Length >= 2, "Road needs at least two points: " + id);
            GameObject go = new GameObject("MBRoad_" + SafeName(id));
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
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            MeshCollider collider = go.AddComponent<MeshCollider>();
            RoadMeshGenerator generator = go.AddComponent<RoadMeshGenerator>();
            generator.settings = new RoadMeshGenerator.RoadMeshSettings
            {
                SourceOrientation = MeshConversion.MeshOrientation.Presets["UNITY"],
                uvOffset = Vector2.up
            };
            generator.SourceMesh = sourceFilter;
            generator.AutoGenerate = false;
            generator.GenerateRoadMesh(1f);
            Require(filter.sharedMesh != null && filter.sharedMesh.vertexCount > 0, "MB Road System generated no mesh: " + id);
            collider.sharedMesh = filter.sharedMesh;
        }

        private static Vector3[] SampleApprovedCenterline(RoadData route, float compression, MeshCollider collider)
        {
            Physics.SyncTransforms();
            List<Vector3> result = new List<Vector3>(route.points.Length);
            foreach (PointData point in route.points)
            {
                float x = point.x * compression;
                float z = point.z * compression;
                Ray ray = new Ray(new Vector3(x, collider.bounds.max.y + 25f, z), Vector3.down);
                Require(collider.Raycast(ray, out RaycastHit hit, collider.bounds.size.y + 50f),
                    $"Could not sample approved road height at ({x:F2}, {z:F2}) for {route.id}.");
                result.Add(new Vector3(x, hit.point.y + SurfaceLift, z));
            }
            return result.ToArray();
        }

        private static Mesh CreateOrReplaceSourceMesh()
        {
            AssetDatabase.DeleteAsset(SourceMeshPath);
            float half = RoadWidth * 0.5f;
            Mesh mesh = new Mesh { name = "Lalay Road 6.2m x 4m Source Tile" };
            mesh.vertices = new[]
            {
                new Vector3(-half, 0f, 0f), new Vector3(half, 0f, 0f),
                new Vector3(-half, 0f, SourceTileLength), new Vector3(half, 0f, SourceTileLength)
            };
            mesh.normals = Enumerable.Repeat(Vector3.up, 4).ToArray();
            mesh.tangents = Enumerable.Repeat(new Vector4(1f, 0f, 0f, 1f), 4).ToArray();
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, SourceMeshPath);
            return AssetDatabase.LoadAssetAtPath<Mesh>(SourceMeshPath);
        }

        private static void PersistGeneratedMeshes(GameObject root)
        {
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!filter.gameObject.name.StartsWith("MBRoad_", StringComparison.Ordinal) || filter.sharedMesh == null) continue;
                string path = StagingFolder + "/" + filter.gameObject.name + "_Generated.asset";
                AssetDatabase.DeleteAsset(path);
                Mesh persistent = UnityEngine.Object.Instantiate(filter.sharedMesh);
                persistent.name = filter.gameObject.name + " Generated Mesh";
                AssetDatabase.CreateAsset(persistent, path);
                filter.sharedMesh = persistent;
                MeshCollider collider = filter.GetComponent<MeshCollider>();
                if (collider != null) collider.sharedMesh = persistent;
            }
        }

        private static void CaptureOverhead(Scene scene, string outputPath)
        {
            MapData data = LoadData();
            List<Vector3> points = data.roads.Where(road => LalayRoadIds.Contains(road.id))
                .SelectMany(road => road.points)
                .Select(point => Horizontal(point, data.compression)).ToList();
            float minX = points.Min(point => point.x), maxX = points.Max(point => point.x);
            float minZ = points.Min(point => point.z), maxZ = points.Max(point => point.z);
            Vector3 centre = new Vector3((minX + maxX) * 0.5f, 220f, (minZ + maxZ) * 0.5f);
            GameObject cameraObject = new GameObject("MINI121_Temporary_Overhead_Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = centre;
            camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max((maxZ - minZ) * 0.5f + 15f, (maxX - minX) / 3.2f + 15f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.07f, 0.08f);
            CaptureCamera(camera, outputPath, 1600, 1000);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }

        private static void CaptureExistingCamera(string cameraName, string outputPath, int width, int height)
        {
            GameObject cameraObject = GameObject.Find(cameraName);
            Require(cameraObject != null, "Evidence camera object is missing: " + cameraName);
            Camera camera = cameraObject.GetComponent<Camera>();
            Require(camera != null, "Evidence camera component is missing: " + cameraName);
            CaptureCamera(camera, outputPath, width, height);
        }

        private static void CaptureCamera(Camera camera, string outputPath, int width, int height)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? EvidenceFolder);
            RenderTexture texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            camera.targetTexture = texture;
            RenderTexture.active = texture;
            camera.Render();
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(outputPath, image.EncodeToPNG());
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(image);
            texture.Release();
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static MapData LoadData()
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(DataPath);
            Require(asset != null, "Map data is missing: " + DataPath);
            MapData data = JsonUtility.FromJson<MapData>(asset.text);
            Require(data != null && data.roads != null, "Map data could not be parsed.");
            return data;
        }

        private static Transform FindTransformIncludingInactive(string name)
        {
            return Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(transform =>
                transform.name == name && transform.gameObject.scene.IsValid());
        }

        private static Vector3 Horizontal(PointData point, float compression)
        {
            return new Vector3(point.x * compression, 0f, point.z * compression);
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        }

        private static string SafeName(string value)
        {
            return value.Replace('/', '_').Replace(' ', '_');
        }

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
            if (!condition) throw new InvalidOperationException("MINI-121: " + message);
        }
    }
}
