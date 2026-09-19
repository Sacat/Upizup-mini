using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    public static class Mini171RepairPass
    {
        const string ScenePath = "Assets/UpIzUpMini/Scenes/GrandBayProof_ExpansionImport.unity";
        const string LivePath = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string CopyPath = "Assets/UpIzUpMini/Scenes/MapLab_GrandBayExpansionCopy.unity";
        const string ReportPath = "Logs/Tasks/MINI-171/REPAIR-REPORT.txt";

        [MenuItem("Up Iz Up Mini/MINI-171/Apply Playable Map Repairs")]
        public static void Apply()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
                string liveHash = Hash(LivePath), copyHash = Hash(CopyPath);
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var report = new List<string> { "MINI-171 repair report" };

                var terrain = GameObject.Find("ExpansionTerrain");
                if (terrain == null) throw new InvalidOperationException("ExpansionTerrain not found.");
                var terrainFilter = terrain.GetComponent<MeshFilter>();
                var terrainCollider = terrain.GetComponent<MeshCollider>();
                if (terrainFilter == null || terrainCollider == null) throw new InvalidOperationException("ExpansionTerrain mesh/collider missing.");

                var roads = UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None)
                    .Where(c => c.gameObject.activeInHierarchy && IsGroundRoad(c.name)).ToArray();
                var mesh = terrainFilter.sharedMesh;
                var vertices = mesh.vertices;
                int fitted = 0;
                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector3 world = terrain.transform.TransformPoint(vertices[i]);
                    float bestY = float.NegativeInfinity;
                    foreach (var road in roads)
                    {
                        Bounds b = road.bounds;
                        if (world.x < b.min.x || world.x > b.max.x || world.z < b.min.z || world.z > b.max.z) continue;
                        if (road.Raycast(new Ray(new Vector3(world.x, b.max.y + 2f, world.z), Vector3.down), out var hit, b.size.y + 4f))
                            bestY = Mathf.Max(bestY, hit.point.y);
                    }
                    if (!float.IsNegativeInfinity(bestY))
                    {
                        world.y = bestY - 0.03f;
                        vertices[i] = terrain.transform.InverseTransformPoint(world);
                        fitted++;
                    }
                }
                mesh.vertices = vertices;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                EditorUtility.SetDirty(mesh);
                terrainCollider.sharedMesh = null;
                terrainCollider.sharedMesh = mesh;
                report.Add($"roadColliders={roads.Length}");
                report.Add($"terrainVerticesFlushUnderRoads={fitted}");

                Physics.SyncTransforms();
                int dogLifeMoved = 0, waypointsMoved = 0;
                foreach (var patrol in UnityEngine.Object.FindObjectsByType<Interaction.PatrolNPC>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    bool dogLife = patrol.name.StartsWith("NPC_DogLife_", StringComparison.OrdinalIgnoreCase);
                    if (!dogLife) continue;
                    Vector3 p = patrol.transform.position;
                    if (GroundY(terrainCollider, p.x, p.z, out float y))
                    {
                        SetPosition(patrol.gameObject, new Vector3(p.x, y + 0.05f, p.z));
                        dogLifeMoved++;
                    }
                    var so = new SerializedObject(patrol);
                    var points = so.FindProperty("waypoints");
                    if (points != null && points.isArray)
                    {
                        for (int i = 0; i < points.arraySize; i++)
                        {
                            var point = points.GetArrayElementAtIndex(i);
                            Vector3 q = point.vector3Value;
                            if (GroundY(terrainCollider, q.x, q.z, out float qy))
                            {
                                point.vector3Value = new Vector3(q.x, qy + 0.05f, q.z);
                                waypointsMoved++;
                            }
                        }
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
                report.Add($"dogLifeActorsGrounded={dogLifeMoved}");
                report.Add($"dogLifeWaypointsGrounded={waypointsMoved}");

                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
                AssetDatabase.SaveAssets();
                if (Hash(LivePath) != liveHash || Hash(CopyPath) != copyHash)
                    throw new InvalidOperationException("Protected source scene changed during repair.");
                report.Add("protectedSceneHashes=unchanged");
                File.WriteAllLines(ReportPath, report);
                Debug.Log("MINI171_REPAIR_PASS " + string.Join(" | ", report));
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        static bool IsGroundRoad(string name) => name.StartsWith("Road_")
            || name.StartsWith("ExpansionRoad_") || name.StartsWith("ImportTrim_Road_");

        static bool GroundY(Collider terrain, float x, float z, out float y)
        {
            if (terrain.Raycast(new Ray(new Vector3(x, terrain.bounds.max.y + 20f, z), Vector3.down), out var hit, terrain.bounds.size.y + 40f))
            { y = hit.point.y; return true; }
            y = 0f; return false;
        }

        static void SetPosition(GameObject go, Vector3 p)
        {
            var cc = go.GetComponent<CharacterController>();
            bool enabled = cc != null && cc.enabled;
            if (enabled) cc.enabled = false;
            go.transform.position = p;
            if (enabled) cc.enabled = true;
        }

        static string Hash(string path)
        {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
        }
    }
}
