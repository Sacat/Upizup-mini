using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-171 read-only survey. Reports (1) every direct child of
    /// the expansion copy's MINI168_Expansion root with world bounds and
    /// triangle count, (2) what else in the copy is disabled/replaced, and
    /// (3) the live scene's ground colliders, so the overlap between the
    /// expansion terrain and the live Lalay/Highland ground is measured
    /// instead of assumed. Saves nothing.</summary>
    public static class Mini171SurveyExpansion
    {
        const string Copy = "Assets/UpIzUpMini/Scenes/MapLab_GrandBayExpansionCopy.unity";
        const string Live = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";

        [MenuItem("Up Iz Up Mini/MINI-171/Survey Expansion Vs Live")]
        public static void Run()
        {
            EditorSceneManager.OpenScene(Copy, OpenSceneMode.Single);
            var exp = GameObject.Find("MINI168_Expansion");
            if (exp == null) { Debug.LogError("MINI171SURVEY: MINI168_Expansion not found"); Finish(1); return; }
            Debug.Log($"MINI171SURVEY: COPY expansion root children={exp.transform.childCount} parent={exp.transform.parent?.name}");
            long totalTris = 0;
            foreach (Transform c in exp.transform)
            {
                var bounds = Bounds(c.gameObject, out int tris, out int meshes);
                totalTris += tris;
                Debug.Log($"MINI171SURVEY:   [{(c.gameObject.activeSelf ? "on" : "off")}] '{c.name}' meshes={meshes} tris={tris} center=({bounds.center.x:F0},{bounds.center.y:F0},{bounds.center.z:F0}) size=({bounds.size.x:F0},{bounds.size.y:F0},{bounds.size.z:F0})");
            }
            Debug.Log($"MINI171SURVEY: COPY expansion total triangles (unique meshes, instances counted per renderer)={totalTris}");

            var map = exp.transform.parent;
            if (map != null)
                foreach (Transform c in map)
                {
                    if (c == exp.transform) continue;
                    var b = Bounds(c.gameObject, out int t, out int m);
                    Debug.Log($"MINI171SURVEY: COPY sibling [{(c.gameObject.activeSelf ? "on" : "off")}] '{c.name}' meshes={m} tris={t} center=({b.center.x:F0},{b.center.z:F0}) size=({b.size.x:F0},{b.size.z:F0})");
                }

            EditorSceneManager.OpenScene(Live, OpenSceneMode.Single);
            foreach (var mc in Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None))
            {
                if (mc.sharedMesh == null) continue;
                var b = mc.bounds;
                if (b.size.x < 150f && b.size.z < 150f) continue; // only large ground-like colliders
                Debug.Log($"MINI171SURVEY: LIVE big MeshCollider '{mc.name}' path={Path(mc.transform)} enabled={mc.enabled} center=({b.center.x:F0},{b.center.y:F0},{b.center.z:F0}) size=({b.size.x:F0},{b.size.y:F0},{b.size.z:F0})");
            }
            foreach (var t in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
                Debug.Log($"MINI171SURVEY: LIVE Unity Terrain component on '{t.name}'");
            Finish(0);
        }

        static Bounds Bounds(GameObject go, out int tris, out int meshes)
        {
            tris = 0; meshes = 0; bool first = true; var b = new Bounds();
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                meshes++; tris += mf.sharedMesh.triangles.Length / 3;
                var r = mf.GetComponent<Renderer>();
                if (r == null) continue;
                if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
            }
            return b;
        }

        static string Path(Transform t)
        {
            var parts = new System.Collections.Generic.List<string>();
            for (var p = t; p != null; p = p.parent) parts.Add(p.name);
            parts.Reverse();
            return string.Join("/", parts.Take(4));
        }

        static void Finish(int code) { if (Application.isBatchMode) EditorApplication.Exit(code); }
    }
}
