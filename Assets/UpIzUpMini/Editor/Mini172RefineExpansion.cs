using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UpIzUpMini.EditorTools
{
    public static class Mini172RefineExpansion
    {
        const string ScenePath = "Assets/UpIzUpMini/Scenes/GrandBayProof_ExpansionImport.unity";
        const string Art = "Assets/UpIzUpMini/Maps/Regions/dm-dom-grand-bay-expansion-v1/Generated";
        const string Out = "Logs/Tasks/MINI-172";

        [MenuItem("Up Iz Up Mini/MINI-172/Refine Roads School and Geneva Field")]
        public static void Apply()
        {
            try
            {
                Directory.CreateDirectory(Out);
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var root = GameObject.Find("MINI168_Expansion")?.transform;
                if (root == null) throw new InvalidOperationException("MINI168_Expansion missing.");

                var report = new List<string> { "MINI-172 refinement" };
                int smoothed = SmoothRoads(root, report);
                int patches = BuildJunctionPatches(root, report);
                FitSchool(root, report);
                BuildCricketGround(root, report);
                FitTerrainToRoads(root, report);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                report.Add($"roadsSmoothed={smoothed}");
                report.Add($"junctionPatches={patches}");
                File.WriteAllLines(Out + "/REFINEMENT-REPORT.txt", report);
                Debug.Log("MINI172_REFINEMENT_PASS " + string.Join(" | ", report));
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        [MenuItem("Up Iz Up Mini/MINI-172/Validate")]
        public static void Validate()
        {
            try
            {
                Directory.CreateDirectory(Out);
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var root = GameObject.Find("MINI168_Expansion")?.transform;
                if (root == null) throw new InvalidOperationException("Expansion root missing.");
                var lines = new List<string>();
                int roads = 0;
                foreach (var f in root.GetComponentsInChildren<MeshFilter>(false).Where(f => f.name.StartsWith("ExpansionRoad_")))
                {
                    roads++;
                    if (f.sharedMesh == null || f.sharedMesh.vertexCount < 4) throw new InvalidOperationException(f.name + " mesh invalid.");
                    if (f.GetComponent<MeshCollider>()?.sharedMesh != f.sharedMesh) throw new InvalidOperationException(f.name + " collider mismatch.");
                }
                var patches = root.Find("MINI172_JunctionPatches");
                int patchCount = patches == null ? 0 : patches.childCount;
                if (patchCount < 1) throw new InvalidOperationException("No paved junction patches were saved.");
                var schoolParts = root.GetComponentsInChildren<Renderer>(false).Where(r => r.name.StartsWith("SchoolTeachingWing")).ToArray();
                var roadColliders = root.GetComponentsInChildren<MeshCollider>(false).Where(r => r.name.StartsWith("ExpansionRoad_") && r.name != "ExpansionRoad_7").ToArray();
                int schoolRoadHits = 0; var schoolHitDetails = new List<string>();
                foreach (var school in schoolParts)
                {
                    Bounds b = school.bounds;
                    foreach (float x in new[] { b.min.x, b.center.x, b.max.x }) foreach (float z in new[] { b.min.z, b.center.z, b.max.z })
                        foreach (var road in roadColliders)
                            if (road.Raycast(new Ray(new Vector3(x, b.max.y + 10f, z), Vector3.down), out _, b.size.y + 20f)) { schoolRoadHits++; schoolHitDetails.Add(school.name + " sample(" + x.ToString("F1") + "," + z.ToString("F1") + ") on " + road.name); }
                }
                if (schoolRoadHits > 0) throw new InvalidOperationException("PCSS teaching-wing footprint still reaches a road at " + schoolRoadHits + " samples: " + string.Join("; ", schoolHitDetails));
                var field = root.Find("GenevaField");
                int wicketCount = field.GetComponentsInChildren<Transform>(false).Count(t => t.name == "CricketWicket");
                int standStepCount = field.GetComponentsInChildren<Transform>(false).Count(t => t.name == "CommunityStandStep");
                if (wicketCount != 6 || standStepCount != 6 || field.Find("CricketPitchStrip") == null) throw new InvalidOperationException("Cricket field detail count mismatch.");
                lines.Add("PASS"); lines.Add("roadMeshesAndColliders=" + roads); lines.Add("junctionPatches=" + patchCount);
                lines.Add("schoolTeachingWingRoadSurfaceHits=0"); lines.Add("cricketWickets=" + wicketCount); lines.Add("standSteps=" + standStepCount);
                File.WriteAllLines(Out + "/VALIDATION.txt", lines);
                Debug.Log("MINI172_VALIDATION_PASS " + string.Join(" | ", lines));
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        [MenuItem("Up Iz Up Mini/MINI-172/Render Evidence")]
        public static void Render()
        {
            try
            {
                Directory.CreateDirectory(Out + "/Renders");
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) c.enabled = false;
                RenderSettings.fog = false; RenderSettings.ambientLight = new Color(.66f, .69f, .72f);
                var camera = new GameObject("MINI172_EvidenceCamera").AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.55f, .70f, .86f); camera.farClipPlane = 1800f;
                var field = GameObject.Find("GenevaPlayingSurface").transform.position;
                Shot(camera, "01-field-overhead", field + Vector3.up * 115f, field, true, 48f);
                Shot(camera, "02-field-player-height", field + new Vector3(55f, 15f, -46f), field + Vector3.up * 1.3f, false, 52f);
                var school = GameObject.Find("SchoolCourtyard").transform.position;
                Shot(camera, "03-school-entrance", school + new Vector3(0f, 13f, 45f), school + Vector3.up * 2f, false, 53f);
                Shot(camera, "04-school-road-overhead", school + Vector3.up * 80f, school, true, 44f);
                Shot(camera, "05-road-network-overhead", new Vector3(105f, 225f, -12f), new Vector3(105f, 0f, -12f), true, 190f);
                Object.DestroyImmediate(camera.gameObject);
                Debug.Log("MINI172_RENDER_PASS");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        static void Shot(Camera c, string name, Vector3 position, Vector3 target, bool ortho, float size)
        {
            c.transform.position = position; c.transform.LookAt(target); c.orthographic = ortho; c.orthographicSize = size; c.fieldOfView = size;
            var rt = new RenderTexture(1600, 1000, 24); c.targetTexture = rt; c.Render(); RenderTexture.active = rt;
            var tex = new Texture2D(1600, 1000, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); tex.Apply();
            File.WriteAllBytes(Out + "/Renders/" + name + ".png", tex.EncodeToPNG());
            c.targetTexture = null; RenderTexture.active = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
        }

        static int SmoothRoads(Transform root, List<string> report)
        {
            // The bay approach has an asymmetric first cross-section copied
            // exactly from the retained live road. Restore and preserve that
            // authored seam; a symmetric ribbon rebuild would reopen it.
            var bayApproach = root.Find("ExpansionRoad_10");
            var bayMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Art + "/Import_ExpansionRoad_10.asset");
            if (bayApproach != null && bayMesh != null)
            {
                bayApproach.GetComponent<MeshFilter>().sharedMesh = bayMesh;
                bayApproach.GetComponent<MeshCollider>().sharedMesh = bayMesh;
            }
            int count = 0;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(false)
                         .Where(f => f.name.StartsWith("ExpansionRoad_") && f.name != "ExpansionRoad_9" && f.name != "ExpansionRoad_10"))
            {
                var source = filter.sharedMesh;
                if (source == null || source.vertexCount < 8 || source.vertexCount % 2 != 0) continue;
                var sv = source.vertices;
                var centres = Enumerable.Range(0, sv.Length / 2)
                    .Select(i => filter.transform.TransformPoint((sv[i * 2] + sv[i * 2 + 1]) * .5f)).ToList();
                var original = centres.ToArray();
                for (int pass = 0; pass < 2; pass++)
                {
                    var next = centres.ToArray();
                    for (int i = 1; i < centres.Count - 1; i++)
                    {
                        Vector3 candidate = (centres[i - 1] + centres[i] * 2f + centres[i + 1]) * .25f;
                        Vector3 delta = candidate - original[i];
                        if (delta.magnitude > 1.2f) candidate = original[i] + delta.normalized * 1.2f;
                        next[i] = candidate;
                    }
                    centres = next.ToList();
                }
                centres[0] = original[0];
                centres[centres.Count - 1] = original[original.Length - 1];
                float width = Vector3.Distance(filter.transform.TransformPoint(sv[0]), filter.transform.TransformPoint(sv[1]));
                var mesh = RibbonMesh("Mini172_" + filter.name, centres, width);
                mesh = SaveMesh(mesh, Art + "/Mini172_" + filter.name + ".asset");
                filter.sharedMesh = mesh;
                var collider = filter.GetComponent<MeshCollider>();
                if (collider != null) { collider.sharedMesh = null; collider.sharedMesh = mesh; }
                count++;
            }
            report.Add("Road centre lines received two bounded smoothing passes; exact endpoints retained.");
            return count;
        }

        static Mesh RibbonMesh(string name, IList<Vector3> line, float width)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int i = 0; i < line.Count; i++)
            {
                Vector3 a = line[Mathf.Max(0, i - 1)], b = line[Mathf.Min(line.Count - 1, i + 1)];
                Vector3 tangent = b - a; tangent.y = 0f;
                Vector3 side = Vector3.Cross(Vector3.up, tangent.normalized) * width * .5f;
                vertices.Add(line[i] - side);
                vertices.Add(line[i] + side);
                if (i > 0) { int k = i * 2; triangles.AddRange(new[] { k - 2, k, k - 1, k - 1, k, k + 1 }); }
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        static int BuildJunctionPatches(Transform root, List<string> report)
        {
            var old = root.Find("MINI172_JunctionPatches");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var holder = new GameObject("MINI172_JunctionPatches").transform;
            holder.SetParent(root, false);
            var roads = root.GetComponentsInChildren<MeshFilter>(false)
                .Where(f => f.name.StartsWith("ExpansionRoad_") && f.sharedMesh != null).ToArray();
            var endpoints = new List<(Vector3 p, float half, Material mat)>();
            foreach (var road in roads)
            {
                var v = road.sharedMesh.vertices;
                float half = Vector3.Distance(road.transform.TransformPoint(v[0]), road.transform.TransformPoint(v[1])) * .5f;
                endpoints.Add((road.transform.TransformPoint((v[0] + v[1]) * .5f), half, road.GetComponent<Renderer>().sharedMaterial));
                endpoints.Add((road.transform.TransformPoint((v[v.Length - 2] + v[v.Length - 1]) * .5f), half, road.GetComponent<Renderer>().sharedMaterial));
            }
            var used = new bool[endpoints.Count]; int made = 0;
            for (int i = 0; i < endpoints.Count; i++)
            {
                if (used[i]) continue;
                var cluster = Enumerable.Range(i, endpoints.Count - i)
                    .Where(j => Vector2.Distance(new Vector2(endpoints[i].p.x, endpoints[i].p.z), new Vector2(endpoints[j].p.x, endpoints[j].p.z)) <= 3.5f).ToArray();
                if (cluster.Length < 2) continue;
                foreach (int j in cluster) used[j] = true;
                Vector3 centre = Vector3.zero; float radius = 0f;
                foreach (int j in cluster) { centre += endpoints[j].p; radius = Mathf.Max(radius, endpoints[j].half + 1.0f); }
                centre /= cluster.Length; centre.y = cluster.Max(j => endpoints[j].p.y) + .012f;
                CreateDisc("RoadJunctionPatch_" + made, centre, radius, endpoints[i].mat, holder, 20);
                made++;
            }
            report.Add("Junction pads bridge endpoint seams and round the paved corner shoulders.");
            return made;
        }

        static void FitSchool(Transform root, List<string> report)
        {
            var left = root.Find("SchoolTeachingWing_0");
            var right = root.Find("SchoolTeachingWing_1");
            var courtyard = root.Find("SchoolCourtyard");
            if (left == null || right == null || courtyard == null) throw new InvalidOperationException("PCSS massing missing.");
            float centreX = (left.position.x + right.position.x) * .5f;
            foreach (var pair in new[] { (left, -11f), (right, 11f) })
            {
                Vector3 p = pair.Item1.position; p.x = centreX + pair.Item2; pair.Item1.position = p;
                var roof = root.Find(pair.Item1.name.Replace("TeachingWing", "MetalRoof"));
                if (roof != null) { p = roof.position; p.x = centreX + pair.Item2; roof.position = p; }
            }
            foreach (Transform child in root)
                if (child.name == "SchoolWindow")
                {
                    Vector3 p = child.position;
                    p.x = p.x < centreX ? centreX - 6.95f : centreX + 6.95f;
                    child.position = p;
                }
            // The entrance-facing right wing's north edge was sitting on
            // ExpansionRoad_4. Set an absolute 3m setback and carry its roof
            // and windows with it, keeping this pass idempotent.
            float rightTargetZ = 46.2967f;
            float rightDeltaZ = rightTargetZ - right.position.z;
            Vector3 rightPos = right.position; rightPos.z = rightTargetZ; right.position = rightPos;
            var rightRoof = root.Find("SchoolMetalRoof_1");
            if (rightRoof != null) { Vector3 p = rightRoof.position; p.z += rightDeltaZ; rightRoof.position = p; }
            foreach (Transform child in root)
                if (child.name == "SchoolWindow" && child.position.x > centreX)
                { Vector3 p = child.position; p.z += rightDeltaZ; child.position = p; }
            Vector3 scale = courtyard.localScale; scale.x = Mathf.Min(scale.x, 34f); courtyard.localScale = scale;
            report.Add("PCSS side wings moved 3m inward, courtyard narrowed from 40m to 34m, and the entrance-facing right wing received a 3m road setback; centroid and north-facing entrance retained.");
        }

        static void BuildCricketGround(Transform root, List<string> report)
        {
            var field = root.Find("GenevaField");
            if (field == null) throw new InvalidOperationException("GenevaField missing.");
            foreach (Transform child in field.Cast<Transform>().Where(t => t.name.StartsWith("Cricket") || t.name.StartsWith("CommunityStand") || t.name == "SidelineBench" || t.name == "BenchSupport").ToArray())
                Object.DestroyImmediate(child.gameObject);
            var surface = field.Find("GenevaPlayingSurface");
            Vector3 centre = surface.position;
            Vector3 forward = surface.forward;
            Vector3 right = surface.right;
            var clay = Material("Mini172CricketPitch", new Color(.56f, .39f, .22f));
            var white = Material("GenevaMarking", new Color(.93f, .94f, .86f));
            var concrete = Material("ExpansionConcrete", new Color(.59f, .57f, .49f));
            var seat = Material("Mini172StandSeat", new Color(.18f, .31f, .43f));

            GameObject Box(string name, Vector3 p, Vector3 size, Material mat)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(field, true);
                go.transform.position = p; go.transform.rotation = surface.rotation; go.transform.localScale = size;
                go.GetComponent<Renderer>().sharedMaterial = mat; return go;
            }
            Box("CricketPitchStrip", centre + Vector3.up * .10f, new Vector3(3.05f, .08f, 20.12f), clay);
            foreach (int end in new[] { -1, 1 })
            {
                Vector3 crease = centre + forward * (end * 8.8f) + Vector3.up * .16f;
                Box("CricketCrease", crease, new Vector3(3.7f, .025f, .10f), white);
                for (int s = -1; s <= 1; s++)
                {
                    Vector3 wicket = centre + forward * (end * 9.35f) + right * (s * .11f);
                    Box("CricketWicket", wicket + Vector3.up * .51f, new Vector3(.035f, 1.02f, .035f), white);
                }
                Box("CricketBail", centre + forward * (end * 9.35f) + Vector3.up * 1.02f, new Vector3(.36f, .035f, .035f), white);
            }
            for (int i = 0; i < 64; i++)
            {
                float a = i * Mathf.PI * 2f / 64f, b = (i + 1) * Mathf.PI * 2f / 64f;
                Vector3 pa = centre + right * (Mathf.Cos(a) * 18.5f) + forward * (Mathf.Sin(a) * 29.5f) + Vector3.up * .16f;
                Vector3 pb = centre + right * (Mathf.Cos(b) * 18.5f) + forward * (Mathf.Sin(b) * 29.5f) + Vector3.up * .16f;
                Vector3 d = pb - pa;
                var line = Box("CricketBoundary", (pa + pb) * .5f, new Vector3(.10f, .025f, d.magnitude), white);
                line.transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
                Object.DestroyImmediate(line.GetComponent<Collider>());
            }
            foreach (int sideSign in new[] { -1, 1 })
            {
                Vector3 basePos = centre + right * (sideSign * 24.5f) + forward * 5f;
                for (int tier = 0; tier < 3; tier++)
                {
                    Vector3 p = basePos - right * (sideSign * tier * .75f) + Vector3.up * (.28f + tier * .42f);
                    Box("CommunityStandStep", p, new Vector3(1.25f, .42f, 11f), concrete);
                    Vector3 benchP = p + right * (sideSign * .22f) + Vector3.up * .31f;
                    Box("CommunityStandSeat", benchP, new Vector3(.62f, .16f, 10.5f), seat);
                }
            }
            report.Add("Geneva field now includes a 20.12x3.05m cricket strip, two wickets/creases, oval boundary and two low three-tier community stands; football goals/markings retained for multi-use identity.");
        }

        static void FitTerrainToRoads(Transform root, List<string> report)
        {
            var terrain = root.Find("ExpansionTerrain");
            var filter = terrain.GetComponent<MeshFilter>(); var collider = terrain.GetComponent<MeshCollider>();
            var roads = root.GetComponentsInChildren<MeshCollider>(false)
                .Where(c => c.name.StartsWith("ExpansionRoad_") || c.name.StartsWith("RoadJunctionPatch_")).ToArray();
            var mesh = filter.sharedMesh; var vertices = mesh.vertices; int fitted = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 p = terrain.TransformPoint(vertices[i]); float y = float.NegativeInfinity;
                foreach (var road in roads)
                {
                    Bounds b = road.bounds;
                    if (p.x < b.min.x || p.x > b.max.x || p.z < b.min.z || p.z > b.max.z) continue;
                    if (road.Raycast(new Ray(new Vector3(p.x, b.max.y + 2f, p.z), Vector3.down), out var hit, b.size.y + 4f)) y = Mathf.Max(y, hit.point.y);
                }
                if (!float.IsNegativeInfinity(y)) { p.y = y - .03f; vertices[i] = terrain.InverseTransformPoint(p); fitted++; }
            }
            mesh.vertices = vertices; mesh.RecalculateNormals(); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);
            collider.sharedMesh = null; collider.sharedMesh = mesh;
            report.Add("terrainVerticesRefit=" + fitted);
        }

        static void CreateDisc(string name, Vector3 centre, float radius, Material mat, Transform parent, int segments)
        {
            var vertices = new List<Vector3> { centre };
            for (int i = 0; i <= segments; i++) { float a = i * Mathf.PI * 2f / segments; vertices.Add(centre + new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius)); }
            var triangles = new List<int>(); for (int i = 1; i <= segments; i++) triangles.AddRange(new[] { 0, i, i + 1 });
            var mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            mesh = SaveMesh(mesh, Art + "/" + name + ".asset");
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat; go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        static Material Material(string name, Color color)
        {
            string path = Art + "/" + name + ".mat"; var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Standard")) { name = name }; AssetDatabase.CreateAsset(mat, path); }
            mat.color = color; mat.enableInstancing = true; EditorUtility.SetDirty(mat); return mat;
        }

        static Mesh SaveMesh(Mesh mesh, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
            EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); return existing;
        }
    }
}
