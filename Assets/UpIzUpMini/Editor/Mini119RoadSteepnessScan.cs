using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-119, user: "the terrain as well there are some areas with a 90
    /// degree steep... i want to be able to drive properly." Read-only
    /// diagnostic (never modifies anything) measuring the REAL built
    /// geometry rather than guessing from a screenshot - same "measure,
    /// don't guess" practice this project already used to correct a wrong
    /// hedge-overlap assumption in MINI-113. Two things measured:
    /// 1) Every road/sidewalk ribbon's own internal slope, vertex to
    ///    vertex - flags anything steeper than maxSlopeDeg.
    /// 2) Every driveable road endpoint's distance to the nearest OTHER
    ///    road - flags a real gap (bigger than the generator's own
    ///    0.65m "tiny seam" auto-connector already handles, but small
    ///    enough it should plausibly be one connected road) for a
    ///    targeted, explicit fix rather than guessed geometry.
    /// </summary>
    public static class Mini119RoadSteepnessScan
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Scan Road+Sidewalk Steepness And Gaps (read-only)")]
        public static void Scan()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/MapLab_LalayHighland.unity", OpenSceneMode.Single);
            GameObject root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "MapLab_LalayHighland");
            if (root == null) { Debug.LogError("MINI-119 SCAN: MapLab_LalayHighland root not found."); return; }

            const float maxSlopeDeg = 40f; // a mountable, drivable slope ceiling - not a paved-road-grade number, deliberately generous so only genuine near-vertical steps are flagged
            int steepCount = 0;
            var meshFilters = root.GetComponentsInChildren<MeshFilter>(true)
                .Where(mf => mf.gameObject.name.StartsWith("Road_") || mf.gameObject.name.StartsWith("Lalay_Sidewalk_") || mf.gameObject.name.StartsWith("Lalay_KerbRamp_"))
                .ToArray();

            foreach (var mf in meshFilters)
            {
                var mesh = mf.sharedMesh;
                if (mesh == null) continue;
                var verts = mesh.vertices;
                var t = mf.transform;
                // Ribbon vertices are laid out in pairs per cross-section
                // (see CreateRibbon/CreateKerbRamp) - check consecutive
                // PAIRS (same side of the ribbon, next cross-section along
                // its length) rather than the two verts of one cross-
                // section itself (which are meant to differ in height for
                // a kerb ramp - that's by design, not a bug).
                for (int i = 0; i + 2 < verts.Length; i += 2)
                {
                    Vector3 a = t.TransformPoint(verts[i]);
                    Vector3 b = t.TransformPoint(verts[i + 2]);
                    float horiz = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
                    if (horiz < 0.05f) continue; // degenerate/near-coincident, not a real slope measurement
                    float vert = Mathf.Abs(a.y - b.y);
                    float slopeDeg = Mathf.Atan2(vert, horiz) * Mathf.Rad2Deg;
                    if (slopeDeg > maxSlopeDeg)
                    {
                        steepCount++;
                        Debug.LogWarning($"MINI-119 STEEP: '{mf.gameObject.name}' cross-section {i / 2}: {slopeDeg:F1} deg slope (vert={vert:F2}m over horiz={horiz:F2}m) at world {a}.");
                    }
                }
            }

            Debug.Log($"MINI-119 SCAN: checked {meshFilters.Length} road/sidewalk/kerb-ramp meshes, found {steepCount} cross-sections steeper than {maxSlopeDeg} deg.");

            // --- Gap scan: every driveable road endpoint vs every other ---
            var roadObjects = root.GetComponentsInChildren<MeshFilter>(true)
                .Where(mf => mf.gameObject.name.StartsWith("Road_") && !mf.gameObject.name.StartsWith("Road_Connector_"))
                .ToArray();

            var endpoints = new List<(string name, Vector3 point)>();
            foreach (var mf in roadObjects)
            {
                var mesh = mf.sharedMesh;
                if (mesh == null || mesh.vertexCount < 2) continue;
                var t = mf.transform;
                endpoints.Add((mf.gameObject.name, t.TransformPoint(mesh.vertices[0])));
                endpoints.Add((mf.gameObject.name, t.TransformPoint(mesh.vertices[mesh.vertexCount - 2])));
            }

            int gapCount = 0;
            for (int i = 0; i < endpoints.Count; i++)
            {
                float best = float.MaxValue;
                string bestName = null;
                for (int j = 0; j < endpoints.Count; j++)
                {
                    if (i == j || endpoints[i].name == endpoints[j].name) continue;
                    float d = Vector3.Distance(endpoints[i].point, endpoints[j].point);
                    if (d < best) { best = d; bestName = endpoints[j].name; }
                }
                // Below 0.65m: already sealed by BuildRoadGapConnectors.
                // Above ~12m: almost certainly not meant to connect at all
                // (different, unrelated roads that just happen to be
                // somewhat close) - flag only the real middle ground.
                if (best >= 0.65f && best <= 12f)
                {
                    gapCount++;
                    Debug.LogWarning($"MINI-119 GAP: '{endpoints[i].name}' endpoint at {endpoints[i].point} is {best:F2}m from the nearest point on '{bestName}' - likely meant to be one connected road.");
                }
            }
            Debug.Log($"MINI-119 SCAN: checked {endpoints.Count} road endpoints, found {gapCount} candidate real gaps (0.65m-12m from another road).");

            var connectors = root.GetComponentsInChildren<MeshFilter>(true)
                .Where(mf => mf.gameObject.name.StartsWith("Road_Connector_")).ToArray();
            Debug.Log($"MINI-119 CONNECTORS: {connectors.Length} built - " + string.Join(", ", connectors.Select(c => c.gameObject.name)));
        }
    }
}
