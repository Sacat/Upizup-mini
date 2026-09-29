using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-182 stage 1: READ-ONLY audit of every house-like root in the live scene (opened, never saved) + evidence renders.</summary>
    public static class Mini182LalayAudit
    {
        const string Live = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string Out = "Logs/Tasks/MINI-182";

        static string Sha(string p)
        {
            using var s = System.Security.Cryptography.SHA256.Create(); using var f = File.OpenRead(p);
            return System.BitConverter.ToString(s.ComputeHash(f)).Replace("-", "");
        }

        static bool IsHouseRoot(Transform t)
        {
            string n = t.name;
            if (n.StartsWith("Lalay_House_") || n.StartsWith("Lalay_Home_") || n.StartsWith("ExpansionHouse_") || n.StartsWith("Highland_House_") || n.StartsWith("Highland_SmallApartment_")) return true;
            var p = t.parent; return p != null && (p.name == "Lalay_Dense_House_Massing" || p.name == "Highland_Sparse_House_Massing");
        }

        [MenuItem("Up Iz Up Mini/MINI-182/Lalay Audit")]
        public static void Run()
        {
            Directory.CreateDirectory(Out + "/Renders");
            string h0 = Sha(Live);
            EditorSceneManager.OpenScene(Live, OpenSceneMode.Single);
            Physics.SyncTransforms();
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var roads = Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(c => c.enabled && (c.name.StartsWith("Road_") || c.name.StartsWith("ExpansionRoad_") || c.name.StartsWith("ImportTrim_Road"))).ToList();
            var roadPts = new List<Vector2>();
            foreach (var r in roads)
            {
                var m = r.sharedMesh; if (m == null) continue;
                foreach (var v in m.vertices) { var w = r.transform.TransformPoint(v); roadPts.Add(new Vector2(w.x, w.z)); }
            }
            var rows = new List<string> { "root,parent,active,category,x,minY,z,yaw,width,depth,height,storeysEst,hasApprovedArt,materials,roadDist,onRoad" };
            var stats = new Dictionary<string, int>();
            var lalay = new List<Bounds>();
            foreach (var t in all)
            {
                if (!IsHouseRoot(t)) continue;
                var rs = t.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) continue;
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                bool art = t.GetComponentsInChildren<Transform>(true).Any(c => c.name == "ApprovedHouse_MINI142");
                string cat = art ? "approved-art" : t.name.StartsWith("ExpansionHouse_") ? "expansion-massing" : t.name.StartsWith("Lalay_Home_") ? "procedural-home" : t.name.StartsWith("Lalay_House_") ? "lalay-house" : (t.parent != null && t.parent.name.StartsWith("Lalay")) ? "lalay-massing" : "highland-massing";
                if (t.name.Contains("Safehouse") || t.name.Contains("Estate") || t.name.Contains("Mansion")) cat = "special";
                var c2 = new Vector2(b.center.x, b.center.z);
                float rd = roadPts.Count == 0 ? -1 : roadPts.Min(p => Vector2.Distance(p, c2));
                var mats = string.Join("|", rs.SelectMany(r => r.sharedMaterials).Where(m => m != null).Select(m => m.name).Distinct().Take(4));
                bool onRoad = false;
                foreach (var r in roads) if (r.Raycast(new Ray(new Vector3(b.center.x, 400, b.center.z), Vector3.down), out _, 900)) { onRoad = true; break; }
                string key = (t.gameObject.activeInHierarchy ? "active " : "inactive ") + cat;
                stats[key] = stats.TryGetValue(key, out var n) ? n + 1 : 1;
                rows.Add(string.Format("{0},{1},{2},{3},{4:F1},{5:F1},{6:F1},{7:F0},{8:F1},{9:F1},{10:F1},{11},{12},{13},{14:F1},{15}",
                    t.name, t.parent != null ? t.parent.name : "", t.gameObject.activeInHierarchy, cat, b.center.x, b.min.y, b.center.z, t.eulerAngles.y,
                    b.size.x, b.size.z, b.size.y, Mathf.Max(1, Mathf.RoundToInt(b.size.y / 3.2f)), art, mats, rd, onRoad));
                bool lalayParent = t.parent != null && t.parent.name.StartsWith("Lalay");
                if (t.gameObject.activeInHierarchy && cat != "special" && (lalayParent || t.name.StartsWith("Lalay_") || (cat == "approved-art" && b.center.z < -95 && b.center.z > -220 && b.center.x < 120)))
                    lalay.Add(b);
            }
            File.WriteAllLines(Out + "/Audit.csv", rows);
            var sum = new List<string> { "MINI-182 Lalay audit (read-only)", "live sha before=" + h0 };
            foreach (var kv in stats.OrderBy(k => k.Key)) sum.Add(kv.Key + ": " + kv.Value);
            sum.Add("houses whose centre is on a road: " + rows.Skip(1).Count(r => r.EndsWith(",True")));
            sum.Add("active houses within 3m of a road vertex: " + rows.Skip(1).Count(r => { var f = r.Split(','); return f[2] == "True" && float.TryParse(f[14], out var d) && d < 3f; }));
            Bounds lb = lalay.Count > 0 ? lalay[0] : new Bounds(); foreach (var b in lalay) lb.Encapsulate(b);
            sum.Add(string.Format("Lalay-side active houses={0} bounds centre=({1:F0},{2:F0}) size=({3:F0},{4:F0})", lalay.Count, lb.center.x, lb.center.z, lb.size.x, lb.size.z));

            var go = new GameObject("AuditCam"); var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.55f, .7f, .9f); cam.farClipPlane = 2000;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = Color.white * .85f;
            var rt = new RenderTexture(1600, 1000, 24); cam.targetTexture = rt;
            void Shot(string name, Vector3 pos, Vector3 look, bool ortho, float size)
            {
                cam.orthographic = ortho; cam.orthographicSize = size; cam.fieldOfView = 55; cam.transform.position = pos; cam.transform.LookAt(look, ortho ? Vector3.forward : Vector3.up); cam.Render();
                var tex = new Texture2D(1600, 1000, TextureFormat.RGB24, false); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); tex.Apply();
                File.WriteAllBytes(Out + "/Renders/" + name + ".png", tex.EncodeToPNG()); Object.DestroyImmediate(tex);
            }
            float G(float x, float z) { return Physics.Raycast(new Vector3(x, 500, z), Vector3.down, out var h, 900) ? h.point.y : 0f; }
            var c0 = lb.center;
            Shot("a1_lalay_top", new Vector3(c0.x, 400, c0.z), new Vector3(c0.x, 0, c0.z), true, Mathf.Max(lb.size.x * .3f, lb.size.z * .55f, 45));
            Shot("a2_lalay_top_close", new Vector3(c0.x, 400, c0.z), new Vector3(c0.x, 0, c0.z), true, 40);
            Shot("a3_lalay_oblique", new Vector3(c0.x - 5, G(c0.x, c0.z) + 55, c0.z - 60), new Vector3(c0.x + 10, G(c0.x + 10, c0.z), c0.z + 5), false, 0);
            Shot("a4_lalay_street", new Vector3(c0.x - 40, G(c0.x - 40, c0.z) + 6, c0.z), new Vector3(c0.x + 20, G(c0.x + 20, c0.z) + 3, c0.z), false, 0);
            sum.Add("live sha after=" + Sha(Live));
            File.WriteAllLines(Out + "/Audit-Summary.txt", sum);
            Debug.Log("MINI182AUDIT " + string.Join(" | ", sum));
            EditorApplication.Exit(Sha(Live) == h0 ? 0 : 1);
        }
    }
}
