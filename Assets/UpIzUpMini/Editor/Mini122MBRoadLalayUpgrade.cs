using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Barmetler.RoadSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UpIzUpMini.Editor
{
    /// <summary>Adds a line-free mobile asphalt treatment and a real MB junction to the accepted MINI-121 proof.</summary>
    public static class Mini122MBRoadLalayUpgrade
    {
        private const string ProofScene = "Assets/UpIzUpMini/Scenes/MapLab_MBRoad_LalayProof.unity";
        private const string StagingFolder = "Assets/UpIzUpMini/Maps/Regions/dm-dom-grand-bay-lalay-highland-v1/Staging/MBRoadLalayProof";
        private const string PackageNormalSource = "Packages/com.barmetler.roadsystem/Samples~/RoadSystem/Textures/Road007_1K_Normal.png";
        private const string NormalTexturePath = StagingFolder + "/LalayAsphalt_Normal_512.png";
        private const string AsphaltMaterialPath = StagingFolder + "/LalayAsphalt_LineFree.mat";
        private const string JunctionMeshPath = StagingFolder + "/MBRoad_LalayJunctionSurface.asset";
        private const string JunctionSidewalkMeshPath = StagingFolder + "/MBRoad_LalayJunctionSidewalk.asset";
        private static readonly string[] JunctionBlockingHouseNames =
        {
            "Lalay_House_SideA_111_OneStorey",
            "Lalay_House_SideB_112_TwoStorey"
        };
        // Pull each square road end back from the shared point and bridge the
        // gap with one round apron.  A small overlap hides seams while keeping
        // the approved OSM centre point at the middle of the turn.
        private const float JunctionAnchorRadius = 3.0f;
        private const float JunctionSurfaceRadius = 4.4f;
        private static readonly string[] RoadNames = { "MBRoad_way_22917921", "MBRoad_way_23042701" };
        private static string EvidenceFolder => Path.Combine("Logs", "Tasks", "MINI-122");

        [MenuItem("Up Iz Up Mini/MINI-122/Build Lalay Asphalt And Junction Proof")]
        public static void Build()
        {
            // Recreate the accepted proof first so this patch remains repeatable and
            // never accumulates manual changes across runs.
            Mini121MBRoadLalayProof.Build();
            Scene scene = EditorSceneManager.OpenScene(ProofScene, OpenSceneMode.Single);
            GameObject systemRoot = GameObject.Find("MBRoadSystem_LalayProof_ACTIVE");
            Require(systemRoot != null, "Accepted MINI-121 road-system root is missing.");
            RemoveJunctionBlockingHouse();

            Texture2D normal = ImportMobileNormalTexture();
            Material asphalt = CreateOrReplaceLineFreeAsphalt(normal);
            MeshFilter sourceFilter = FindTransformIncludingHidden("Lalay_6_2m_SourceMesh_DO_NOT_RENDER")?.GetComponent<MeshFilter>();
            Require(sourceFilter != null && sourceFilter.sharedMesh != null, "Accepted Lalay source mesh is missing.");
            SetLongitudinalUvScale(sourceFilter.sharedMesh);

            List<Road> roads = new List<Road>();
            foreach (string roadName in RoadNames)
            {
                GameObject roadObject = GameObject.Find(roadName);
                Require(roadObject != null, "Accepted MB road is missing: " + roadName);
                Road road = roadObject.GetComponent<Road>();
                Require(road != null && road.NumSegments > 0, "Road spline is invalid: " + roadName);
                roads.Add(road);
                roadObject.GetComponent<MeshRenderer>().sharedMaterial = asphalt;
            }

            Vector3 junctionPoint = roads[0].transform.TransformPoint(roads[0][0]);
            Require(Vector3.Distance(junctionPoint, roads[1].transform.TransformPoint(roads[1][0])) < 0.05f,
                "The two accepted Lalay roads no longer share their approved endpoint.");

            Transform existing = FindTransformIncludingHidden("MBRoad_LalayWestJunction");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            GameObject junctionObject = new GameObject("MBRoad_LalayWestJunction");
            junctionObject.transform.SetParent(systemRoot.transform, false);
            junctionObject.transform.position = junctionPoint;
            Intersection intersection = junctionObject.AddComponent<Intersection>();

            for (int i = 0; i < roads.Count; i++)
            {
                Road road = roads[i];
                Vector3 next = road.transform.TransformPoint(road[3]);
                Vector3 direction = Vector3.ProjectOnPlane(next - junctionPoint, Vector3.up).normalized;
                GameObject anchorObject = new GameObject("Anchor_" + road.gameObject.name);
                anchorObject.transform.SetParent(junctionObject.transform, false);
                anchorObject.transform.localPosition = direction * JunctionAnchorRadius;
                anchorObject.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                RoadAnchor anchor = anchorObject.AddComponent<RoadAnchor>();
                anchor.SetRoad(road, true);
            }

            intersection.Invalidate(false);
            Barmetler.RoadSystem.RoadSystem roadSystem = systemRoot.GetComponent<Barmetler.RoadSystem.RoadSystem>();
            Require(roadSystem != null, "MB Road System component is missing.");
            roadSystem.RebuildAllRoads();

            foreach (Road road in roads)
            {
                RoadMeshGenerator generator = road.GetComponent<RoadMeshGenerator>();
                Require(generator != null, "Road mesh generator is missing: " + road.name);
                generator.settings.uvOffset = new Vector2(0f, 0.25f);
                generator.GenerateRoadMesh(1f);
                road.GetComponent<MeshCollider>().sharedMesh = road.GetComponent<MeshFilter>().sharedMesh;
            }
            PersistRoadMeshes(roads);
            CreateJunctionSurface(junctionObject.transform, asphalt);
            Material sidewalkMaterial = FindTransformIncludingHidden("Lalay_Sidewalk_Left_way_23042701")
                ?.GetComponent<MeshRenderer>()?.sharedMaterial;
            Require(sidewalkMaterial != null, "Approved Lalay sidewalk material is missing.");
            CreateRoundedJunctionSidewalk(junctionObject.transform, sidewalkMaterial);

            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene, ProofScene), "Could not save upgraded Lalay proof.");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("MINI-122 BUILD PASS: line-free Lalay asphalt and connected two-anchor MB Road System junction created in the isolated proof.");
        }

        [MenuItem("Up Iz Up Mini/MINI-122/Validate Lalay Asphalt And Junction Proof")]
        public static void Validate()
        {
            Scene scene = EditorSceneManager.OpenScene(ProofScene, OpenSceneMode.Single);
            Require(scene.IsValid(), "Proof scene is invalid.");
            Material asphalt = AssetDatabase.LoadAssetAtPath<Material>(AsphaltMaterialPath);
            Require(asphalt != null, "Line-free asphalt material is missing.");
            Require(asphalt.mainTexture == null, "Lalay asphalt must not use the package color map because it contains road lines.");
            Require(asphalt.GetTexture("_BumpMap") != null, "Subtle asphalt normal detail is missing.");

            TextureImporter importer = AssetImporter.GetAtPath(NormalTexturePath) as TextureImporter;
            Require(importer != null && importer.maxTextureSize <= 512 && importer.mipmapEnabled,
                "Mobile asphalt normal must be mipmapped and no larger than 512px.");

            GameObject junctionObject = GameObject.Find("MBRoad_LalayWestJunction");
            Require(junctionObject != null, "Connected Lalay junction is missing.");
            foreach (string houseName in JunctionBlockingHouseNames)
                Require(FindTransformIncludingHidden(houseName) == null,
                    "A user-rejected blue house still obstructs the rounded junction: " + houseName);
            Intersection intersection = junctionObject.GetComponent<Intersection>();
            Require(intersection != null && intersection.AnchorPoints.Length == 2, "Junction must expose exactly two road anchors.");
            Transform roundedSurface = junctionObject.transform.Find("JunctionSurface_LineFree_Rounded");
            Require(roundedSurface != null, "Rounded line-free junction surface is missing.");
            MeshCollider junctionCollider = roundedSurface.GetComponent<MeshCollider>();
            Require(junctionCollider != null && junctionCollider.sharedMesh == roundedSurface.GetComponent<MeshFilter>().sharedMesh,
                "Rounded junction must have one matching continuous collider.");
            foreach (RoadAnchor anchor in intersection.AnchorPoints)
                Require(Mathf.Abs(anchor.transform.localPosition.magnitude - JunctionAnchorRadius) < 0.05f,
                    "Junction road anchors must be pulled back evenly for a round connection.");
            Transform roundedSidewalk = junctionObject.transform.Find("JunctionSidewalk_Rounded_NoCollider");
            Require(roundedSidewalk != null && roundedSidewalk.GetComponent<Collider>() == null,
                "Rounded sidewalk cleanup is missing or adds an unwanted second collider.");

            foreach (string roadName in RoadNames)
            {
                GameObject roadObject = GameObject.Find(roadName);
                Require(roadObject != null, "Road missing: " + roadName);
                Road road = roadObject.GetComponent<Road>();
                Require(road.start != null && road.start.Intersection == intersection, "Road is not connected to the shared MB junction: " + roadName);
                Require(roadObject.GetComponent<MeshRenderer>().sharedMaterial == asphalt, "Road does not use line-free asphalt: " + roadName);
                MeshFilter filter = roadObject.GetComponent<MeshFilter>();
                MeshCollider collider = roadObject.GetComponent<MeshCollider>();
                Require(filter.sharedMesh != null && collider.sharedMesh == filter.sharedMesh, "Road mesh/collider mismatch: " + roadName);
            }

            GameObject legacy = FindTransformIncludingHidden("LegacyLalayRoad_Backup_DISABLED")?.gameObject;
            Require(legacy != null && !legacy.activeSelf && legacy.transform.childCount == 2, "Legacy rollback road was not preserved.");
            int vertices = RoadNames.Sum(name => GameObject.Find(name).GetComponent<MeshFilter>().sharedMesh.vertexCount);
            Require(vertices < 5000, "Upgraded Lalay road exceeds first-pass mobile vertex budget: " + vertices);
            Debug.Log($"MINI-122 VALIDATION PASS: connectedAnchors={intersection.AnchorPoints.Length}, lineFree=True, normalMax={importer.maxTextureSize}, mipmaps={importer.mipmapEnabled}, roadVertices={vertices}, legacyBackups={legacy.transform.childCount}.");
        }

        [MenuItem("Up Iz Up Mini/MINI-122/Capture Lalay Asphalt And Junction Proof")]
        public static void Capture()
        {
            Scene scene = EditorSceneManager.OpenScene(ProofScene, OpenSceneMode.Single);
            Directory.CreateDirectory(EvidenceFolder);
            CaptureExistingCamera("Camera_Lalay", Path.Combine(EvidenceFolder, "LineFreeAsphalt-Lalay-PlayerHeight-1280x720.png"), 1280, 720);
            CaptureOverhead(scene, Path.Combine(EvidenceFolder, "LineFreeAsphalt-Lalay-Overhead-1600x1000.png"));
            CaptureJunction(scene, Path.Combine(EvidenceFolder, "LineFreeAsphalt-Lalay-Junction-1280x720.png"));
            Debug.Log("MINI-122 CAPTURE PASS: line-free player-height, overhead and junction screenshots saved.");
        }

        public static void BuildValidateCapture()
        {
            Build();
            Validate();
            Capture();
        }

        public static void SurveyJunctionObjects()
        {
            EditorSceneManager.OpenScene(ProofScene, OpenSceneMode.Single);
            Transform junction = GameObject.Find("MBRoad_LalayWestJunction")?.transform;
            Require(junction != null, "Junction is missing for obstacle survey.");
            foreach (Renderer renderer in Resources.FindObjectsOfTypeAll<Renderer>()
                         .Where(item => item.gameObject.scene.IsValid() && item.enabled && item.gameObject.activeInHierarchy)
                         .Where(item => Vector3.Distance(item.bounds.ClosestPoint(junction.position), junction.position) < 8f)
                         .OrderBy(item => Vector3.Distance(item.bounds.center, junction.position)))
            {
                Debug.Log($"MINI-122 JUNCTION NEARBY: name={renderer.name}, path={GetPath(renderer.transform)}, center={renderer.bounds.center}, size={renderer.bounds.size}");
            }

            Camera camera = GameObject.Find("Camera_Lalay")?.GetComponent<Camera>();
            Require(camera != null, "Lalay proof camera is missing for obstruction survey.");
            foreach (float viewportX in new[] { 0.03f, 0.08f, 0.13f })
            {
                Ray ray = camera.ViewportPointToRay(new Vector3(viewportX, 0.5f, 0f));
                foreach (var hit in Resources.FindObjectsOfTypeAll<Renderer>()
                             .Where(item => item.gameObject.scene.IsValid() && item.enabled && item.gameObject.activeInHierarchy)
                             .Select(item => new { Renderer = item, Hit = item.bounds.IntersectRay(ray, out float distance), Distance = distance })
                             .Where(item => item.Hit && item.Distance > 0f)
                             .OrderBy(item => item.Distance)
                             .Take(8))
                    Debug.Log($"MINI-122 BLUE-RAY x={viewportX:F2}: distance={hit.Distance:F2}, path={GetPath(hit.Renderer.transform)}, center={hit.Renderer.bounds.center}, size={hit.Renderer.bounds.size}");
            }
        }

        private static Texture2D ImportMobileNormalTexture()
        {
            Require(File.Exists(PackageNormalSource), "Included MB Road System normal texture is missing.");
            File.Copy(PackageNormalSource, NormalTexturePath, true);
            AssetDatabase.ImportAsset(NormalTexturePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(NormalTexturePath) as TextureImporter;
            Require(importer != null, "Could not configure imported asphalt normal.");
            importer.textureType = TextureImporterType.NormalMap;
            importer.sRGBTexture = false;
            importer.maxTextureSize = 512;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(NormalTexturePath);
        }

        private static void RemoveJunctionBlockingHouse()
        {
            foreach (string houseName in JunctionBlockingHouseNames)
            {
                Transform blockingHouse = FindTransformIncludingHidden(houseName);
                Require(blockingHouse != null, "Expected junction-blocking house is missing from the clean proof source: " + houseName);
                UnityEngine.Object.DestroyImmediate(blockingHouse.gameObject);
            }
        }

        private static Material CreateOrReplaceLineFreeAsphalt(Texture2D normal)
        {
            AssetDatabase.DeleteAsset(AsphaltMaterialPath);
            Shader shader = Shader.Find("Standard");
            Require(shader != null, "Built-in Standard shader is unavailable.");
            Material material = new Material(shader) { name = "Lalay Asphalt - Line Free Mobile" };
            material.color = new Color(0.20f, 0.205f, 0.20f, 1f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Glossiness", 0.16f);
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", 0.28f);
            material.EnableKeyword("_NORMALMAP");
            AssetDatabase.CreateAsset(material, AsphaltMaterialPath);
            return AssetDatabase.LoadAssetAtPath<Material>(AsphaltMaterialPath);
        }

        private static void SetLongitudinalUvScale(Mesh mesh)
        {
            Vector2[] uv = mesh.uv;
            Require(uv != null && uv.Length == 4, "Unexpected accepted source-mesh UV layout.");
            uv[0] = new Vector2(0f, 0f);
            uv[1] = new Vector2(1f, 0f);
            uv[2] = new Vector2(0f, 0.25f);
            uv[3] = new Vector2(1f, 0.25f);
            mesh.uv = uv;
            EditorUtility.SetDirty(mesh);
        }

        private static void CreateJunctionSurface(Transform junction, Material asphalt)
        {
            const int segments = 48;
            Vector3[] vertices = new Vector3[segments + 1];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[segments * 6];
            vertices[0] = new Vector3(0f, 0.01f, 0f);
            uv[0] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < segments; i++)
            {
                float angle = Mathf.PI * 2f * i / segments;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * JunctionSurfaceRadius, 0.01f, Mathf.Sin(angle) * JunctionSurfaceRadius);
                uv[i + 1] = new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f);
                int next = (i + 1) % segments + 1;
                int triangle = i * 6;
                // Keep both windings: the proof combines package-generated
                // roads with a procedural X/Z apron, and this avoids a render-
                // pipeline culling mismatch without requiring a second material.
                triangles[triangle] = 0;
                triangles[triangle + 1] = next;
                triangles[triangle + 2] = i + 1;
                triangles[triangle + 3] = 0;
                triangles[triangle + 4] = i + 1;
                triangles[triangle + 5] = next;
            }

            AssetDatabase.DeleteAsset(JunctionMeshPath);
            Mesh mesh = new Mesh { name = "MB Road Lalay Junction Surface" };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.normals = Enumerable.Repeat(Vector3.up, vertices.Length).ToArray();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, JunctionMeshPath);

            GameObject surface = new GameObject("JunctionSurface_LineFree_Rounded");
            surface.transform.SetParent(junction, false);
            Mesh savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(JunctionMeshPath);
            surface.AddComponent<MeshFilter>().sharedMesh = savedMesh;
            surface.AddComponent<MeshRenderer>().sharedMaterial = asphalt;
            surface.AddComponent<MeshCollider>().sharedMesh = savedMesh;
        }

        private static void CreateRoundedJunctionSidewalk(Transform junction, Material sidewalkMaterial)
        {
            const int segments = 96;
            const float innerRadius = JunctionSurfaceRadius + 0.04f;
            const float outerRadius = JunctionSurfaceRadius + 1.12f;
            const float openingHalfAngle = 47f;
            Vector3[] vertices = new Vector3[segments * 2];
            Vector2[] uv = new Vector2[vertices.Length];
            List<int> triangles = new List<int>();
            Vector3[] roadDirections = junction.GetComponentsInChildren<RoadAnchor>()
                .Select(anchor => Vector3.ProjectOnPlane(anchor.transform.forward, Vector3.up).normalized)
                .ToArray();
            Require(roadDirections.Length == 2, "Rounded sidewalk expects exactly two Lalay road openings.");

            for (int i = 0; i < segments; i++)
            {
                float angle = Mathf.PI * 2f * i / segments;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices[i * 2] = direction * innerRadius + Vector3.up * 0.018f;
                vertices[i * 2 + 1] = direction * outerRadius + Vector3.up * 0.018f;
                uv[i * 2] = new Vector2(i / (float)segments, 0f);
                uv[i * 2 + 1] = new Vector2(i / (float)segments, 1f);

                float midpointAngle = Mathf.PI * 2f * (i + 0.5f) / segments;
                Vector3 midpointDirection = new Vector3(Mathf.Cos(midpointAngle), 0f, Mathf.Sin(midpointAngle));
                bool roadOpening = roadDirections.Any(roadDirection =>
                    Vector3.Angle(midpointDirection, roadDirection) < openingHalfAngle);
                if (roadOpening) continue;
                int next = (i + 1) % segments;
                int inner = i * 2, outer = inner + 1, nextInner = next * 2, nextOuter = nextInner + 1;
                triangles.Add(inner); triangles.Add(nextOuter); triangles.Add(outer);
                triangles.Add(inner); triangles.Add(nextInner); triangles.Add(nextOuter);
            }

            AssetDatabase.DeleteAsset(JunctionSidewalkMeshPath);
            Mesh mesh = new Mesh { name = "MB Road Lalay Rounded Junction Sidewalk" };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles.ToArray();
            mesh.normals = Enumerable.Repeat(Vector3.up, vertices.Length).ToArray();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, JunctionSidewalkMeshPath);

            GameObject sidewalk = new GameObject("JunctionSidewalk_Rounded_NoCollider");
            sidewalk.transform.SetParent(junction, false);
            sidewalk.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(JunctionSidewalkMeshPath);
            sidewalk.AddComponent<MeshRenderer>().sharedMaterial = sidewalkMaterial;
        }

        private static void PersistRoadMeshes(IEnumerable<Road> roads)
        {
            foreach (Road road in roads)
            {
                MeshFilter filter = road.GetComponent<MeshFilter>();
                string path = StagingFolder + "/" + road.gameObject.name + "_Generated.asset";
                AssetDatabase.DeleteAsset(path);
                Mesh persistent = UnityEngine.Object.Instantiate(filter.sharedMesh);
                persistent.name = road.gameObject.name + " Generated Mesh";
                AssetDatabase.CreateAsset(persistent, path);
                filter.sharedMesh = persistent;
                road.GetComponent<MeshCollider>().sharedMesh = persistent;
            }
        }

        private static void CaptureJunction(Scene scene, string outputPath)
        {
            Transform junction = GameObject.Find("MBRoad_LalayWestJunction").transform;
            Vector3 directionA = Vector3.ProjectOnPlane(junction.GetChild(0).forward, Vector3.up).normalized;
            Vector3 directionB = Vector3.ProjectOnPlane(junction.GetChild(1).forward, Vector3.up).normalized;
            Vector3 viewDirection = (directionA + directionB).normalized;
            if (viewDirection.sqrMagnitude < 0.1f) viewDirection = Vector3.forward;
            GameObject cameraObject = new GameObject("MINI122_Temporary_Junction_Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = junction.position - viewDirection * 13f + Vector3.up * 7f;
            camera.transform.rotation = Quaternion.LookRotation(junction.position + Vector3.up * 0.5f - camera.transform.position, Vector3.up);
            camera.fieldOfView = 48f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.07f, 0.08f);
            CaptureCamera(camera, outputPath, 1280, 720);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }

        private static void CaptureOverhead(Scene scene, string outputPath)
        {
            Road[] roads = RoadNames.Select(name => GameObject.Find(name).GetComponent<Road>()).ToArray();
            List<Vector3> anchors = new List<Vector3>();
            foreach (Road road in roads)
                for (int i = 0; i < road.NumPoints; i += 3) anchors.Add(road.transform.TransformPoint(road[i]));
            float minX = anchors.Min(point => point.x), maxX = anchors.Max(point => point.x);
            float minZ = anchors.Min(point => point.z), maxZ = anchors.Max(point => point.z);
            GameObject cameraObject = new GameObject("MINI122_Temporary_Overhead_Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3((minX + maxX) * 0.5f, 220f, (minZ + maxZ) * 0.5f);
            camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max((maxZ - minZ) * 0.5f + 15f, (maxX - minX) / 3.2f + 15f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.07f, 0.08f);
            CaptureCamera(camera, outputPath, 1600, 1000);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }

        private static void CaptureExistingCamera(string name, string outputPath, int width, int height)
        {
            Camera camera = GameObject.Find(name)?.GetComponent<Camera>();
            Require(camera != null, "Evidence camera is missing: " + name);
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

        private static Transform FindTransformIncludingHidden(string name)
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

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("MINI-122: " + message);
        }
    }
}
