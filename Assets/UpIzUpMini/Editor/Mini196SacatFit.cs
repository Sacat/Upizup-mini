using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-196: Sacat's wardrobe shirt used Franki's garment shape plus extra shoulder/arm/chest bulges and a flattening ellipse, so he lost the
    /// proportions of his original model. This fits the shirt torso to the ORIGINAL Ch06 silhouette per height band (centre line and half
    /// width/depth), with no added bulges. Only affects Sacat; Franki's generation path is untouched.
    /// </summary>
    public static partial class Mini166Repair
    {
        sealed class FitProfile
        {
            public const float Y0 = .80f, Dy = .02f; public const int N = 40;     // y .80 .. 1.58
            public float[] cx = new float[N], cz = new float[N], hx = new float[N], hz = new float[N]; public bool[] ok = new bool[N];
            public static FitProfile From(IEnumerable<Vector3> points)
            {
                var pts = points.ToArray(); var f = new FitProfile();
                for (int i = 0; i < N; i++)
                {
                    float y = Y0 + i * Dy;
                    var band = pts.Where(p => Mathf.Abs(p.y - y) <= Dy * 1.2f).ToArray();
                    var centre = band.Where(p => Mathf.Abs(p.x) < .12f).ToArray();
                    if (centre.Length < 4) continue;
                    f.cz[i] = (centre.Min(p => p.z) + centre.Max(p => p.z)) * .5f; f.hz[i] = (centre.Max(p => p.z) - centre.Min(p => p.z)) * .5f;
                    var wide = band.Where(p => Mathf.Abs(p.x) < .27f).ToArray();
                    f.cx[i] = 0f; f.hx[i] = wide.Length > 4 ? Mathf.Max(Mathf.Abs(wide.Min(p => p.x)), Mathf.Abs(wide.Max(p => p.x))) : 0f;
                    f.ok[i] = f.hz[i] > .02f;
                }
                return f;
            }
            public float Sample(float[] a, float y) { float t = (y - Y0) / Dy; int i = Mathf.Clamp(Mathf.FloorToInt(t), 0, N - 2); float u = Mathf.Clamp01(t - i); return Mathf.Lerp(a[i], a[i + 1], u); }
            public bool Has(float y) { int i = Mathf.Clamp(Mathf.FloorToInt((y - Y0) / Dy), 0, N - 2); return ok[i] && ok[i + 1]; }
        }

        static FitProfile fitOriginal, fitGarment;
        static bool sacatOriginalTorso;

        static void ComputeSacatFit(IEnumerable<Vector3> originalPoints, IEnumerable<Vector3> garmentPoints)
        {
            fitOriginal = FitProfile.From(originalPoints); fitGarment = FitProfile.From(garmentPoints);
        }

        static Vector3 FitPoint(Vector3 p)
        {
            var o = fitOriginal; var g = fitGarment; if (o == null || g == null) return p;
            float y = p.y; if (y < .88f || y > 1.5f || !o.Has(y) || !g.Has(y)) return p;
            // fade the fit in over the hem and out over the neck, and out sideways before the sleeves
            float vFade = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.88f, .96f, y)) * (1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1.40f, 1.50f, y)));
            float xFade = 1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.25f, .36f, Mathf.Abs(p.x)));
            float k = vFade * xFade; if (k <= 0f) return p;
            float sz = Mathf.Clamp(o.Sample(o.hz, y) / Mathf.Max(.02f, g.Sample(g.hz, y)), .8f, 1.35f);
            float czShift = o.Sample(o.cz, y) - g.Sample(g.cz, y);
            float sx = 1f;
            if (y < 1.26f) { float gx = g.Sample(g.hx, y), ox = o.Sample(o.hx, y); if (gx > .05f && ox > .05f) sx = Mathf.Clamp(ox / gx, .85f, 1.25f); }
            Vector3 q = p;
            q.z = g.Sample(g.cz, y) + (p.z - g.Sample(g.cz, y)) * sz + czShift;
            q.x = p.x * sx;
            return Vector3.Lerp(p, q, k);
        }

        [MenuItem("Up Iz Up Mini/MINI-196/Refit Sacat Shirt To Original Silhouette")]
        public static void RefitSacat()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(Environment.GetEnvironmentVariable("MINI196_SCENE") ?? "Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity", OpenSceneMode.Single);
                Directory.CreateDirectory(Out); Directory.CreateDirectory(Art);
                BuildCharacter(GameObject.Find("Sacat"));
                AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                Debug.Log("MINI196_SACAT_REFIT_PASS");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }
    }
}
