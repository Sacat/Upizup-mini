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
    /// MINI-199 (piece 2 of the Sacat rebuild: torso and neck). Sacat's original model has no bare torso or neck: the hoodie IS his upper body, and the
    /// "neck" was a flat jagged skin sheet. This builds a real torso, trapezius slope, shoulder yoke and neck as one skinned ring sweep (hips-side hidden
    /// under the shirt), then lets the shirt hug it: the hood is collapsed onto a polo neckline and its back is laid onto the torso surface.
    /// </summary>
    public static partial class Mini166Repair
    {
        static bool sacatBareTorso;

        // y, half width x, half depth front, half depth back, centre z
        static readonly float[,] TorsoKeys =
        {
            { 1.080f, .136f, .112f, .105f,  .018f },
            { 1.200f, .152f, .115f, .108f,  .012f },
            { 1.280f, .168f, .104f, .106f,  .000f },
            { 1.340f, .166f, .090f, .098f, -.015f },
            { 1.380f, .146f, .080f, .090f, -.025f },
            { 1.405f, .125f, .072f, .082f, -.030f },
            { 1.430f, .085f, .063f, .074f, -.030f },
            { 1.460f, .056f, .055f, .064f, -.030f },
            { 1.500f, .050f, .052f, .057f, -.030f },
            { 1.560f, .050f, .052f, .056f, -.030f },
        };

        static float[] TorsoAt(float y)
        {
            int n = TorsoKeys.GetLength(0); y = Mathf.Clamp(y, TorsoKeys[0, 0], TorsoKeys[n - 1, 0]);
            int i = 0; while (i < n - 2 && y > TorsoKeys[i + 1, 0]) i++;
            float t = Mathf.InverseLerp(TorsoKeys[i, 0], TorsoKeys[i + 1, 0], y); t = t * t * (3f - 2f * t);
            var r = new float[4]; for (int k = 0; k < 4; k++) r[k] = Mathf.Lerp(TorsoKeys[i, k + 1], TorsoKeys[i + 1, k + 1], t); return r;
        }

        // back and front surface z of the torso skin at a given (x, y); null outside the torso width
        static float TorsoBackZ(float x, float y) { var a = TorsoAt(y); float u = Mathf.Clamp(Mathf.Abs(x) / a[0], 0f, .985f); return a[3] - a[2] * Mathf.Sqrt(1f - u * u); }
        static float TorsoFrontZ(float x, float y) { var a = TorsoAt(y); float u = Mathf.Clamp(Mathf.Abs(x) / a[0], 0f, .985f); return a[3] + a[1] * Mathf.Sqrt(1f - u * u); }
        // the polo neckline: highest point the shirt may reach at lateral position x (neck base in the middle, falling along the trapezius)
        // beyond the base of the neck the shirt keeps its own shoulder line (the limit is released), so shoulders are not flattened
        static float NeckLineYRelease(float x, bool front) { float ax = Mathf.Abs(x); return NeckLineY(x, front) + .25f * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.09f, .14f, ax)); }
        static float NeckLineY(float x, bool front) { float ax = Mathf.Abs(x); return Mathf.Lerp(front ? 1.425f : 1.446f, front ? 1.385f : 1.40f, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.045f, .14f, ax))); }

        static BoneWeight TorsoWeight(Surface s, float x, float y)
        {
            int spine1 = s.Bone("Spine1"), spine2 = s.Bone("Spine2"), neck = s.Bone("Neck"), head = s.Bone("Head"), shoulder = s.Bone(x < 0 ? "LeftShoulder" : "RightShoulder");
            float yS2 = s.Rest("Spine2").y, yN = s.Rest("Neck").y;
            float wNeck = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(yS2 + .02f, yN, y));
            float wHead = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(yN + .02f, yN + .08f, y));
            float wSh = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.13f, .21f, Mathf.Abs(x))) * .55f * (1f - wHead);
            float wLow = 1f - wNeck;
            var d = new[] { (spine1, wLow * .35f * (1f - wSh)), (spine2, (wLow * .65f + wNeck * (1f - wHead) * .35f) * (1f - wSh)), (neck, wNeck * (1f - wHead) * .65f * (1f - wSh)), (head, wHead), (shoulder, wSh) };
            var order = d.OrderByDescending(e => e.Item2).Take(4).ToArray(); float sum = Mathf.Max(.0001f, order.Sum(e => e.Item2)); var w = new BoneWeight();
            w.boneIndex0 = order[0].Item1; w.weight0 = order[0].Item2 / sum; w.boneIndex1 = order[1].Item1; w.weight1 = order[1].Item2 / sum;
            w.boneIndex2 = order[2].Item1; w.weight2 = order[2].Item2 / sum; w.boneIndex3 = order[3].Item1; w.weight3 = order[3].Item2 / sum; return w;
        }

        static Shape BuildTorso(Surface s)
        {
            var shape = new Shape(s, 1); const int ring = 32;
            var ys = new List<float>(); for (float y = TorsoKeys[0, 0]; y <= TorsoKeys[TorsoKeys.GetLength(0) - 1, 0] + .0001f; y += .02f) ys.Add(y);
            var stations = new List<V[]>();
            foreach (float y in ys)
            {
                var a = TorsoAt(y); var rv = new V[ring];
                for (int k = 0; k < ring; k++)
                {
                    float ang = k * Mathf.PI * 2f / ring; float cx = Mathf.Cos(ang), sz = Mathf.Sin(ang); float rz = sz >= 0 ? a[1] : a[2];
                    var p = new Vector3(a[0] * cx, y, a[3] + rz * sz);
                    var n = new Vector3(cx / Mathf.Max(.01f, a[0]), 0, sz / Mathf.Max(.01f, rz)).normalized;
                    rv[k] = new V { p = p, n = n, uv = Vector2.zero, w = TorsoWeight(s, p.x, y) };
                }
                stations.Add(rv);
            }
            for (int i = 0; i + 1 < stations.Count; i++)
                for (int k = 0; k < ring; k++)
                {
                    int k2 = (k + 1) % ring; V a0 = stations[i][k], a1 = stations[i][k2], b0 = stations[i + 1][k], b1 = stations[i + 1][k2];
                    shape.Poly(new List<V> { a0, b1, a1 }, 0); shape.Poly(new List<V> { a0, b0, b1 }, 0);
                }
            // caps: bottom (hidden in the shirt) and top (hidden in the head)
            var bot = stations[0]; var top = stations[stations.Count - 1];
            var cb = new V { p = new Vector3(0, ys[0], TorsoKeys[0, 4]), n = Vector3.down, uv = Vector2.zero, w = bot[0].w }; var ct = new V { p = new Vector3(0, ys.Last(), TorsoKeys[TorsoKeys.GetLength(0) - 1, 4]), n = Vector3.up, uv = Vector2.zero, w = top[0].w };
            for (int k = 0; k < ring; k++) { var a = bot[k]; var b = bot[(k + 1) % ring]; shape.Poly(new List<V> { cb, a, b }, 0); var c = top[k]; var d = top[(k + 1) % ring]; shape.Poly(new List<V> { ct, d, c }, 0); }
            return shape;
        }

        [MenuItem("Up Iz Up Mini/MINI-199/Build Sacat Arms + Torso + Neck")]
        public static void BuildSacatTorso()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(Environment.GetEnvironmentVariable("MINI196_SCENE") ?? "Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity", OpenSceneMode.Single);
                Directory.CreateDirectory(Out); Directory.CreateDirectory(Art);
                var root = GameObject.Find("Sacat");
                sacatBareArms = true; sacatBareTorso = true; BuildCharacter(root);
                var source = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "Ch06");
                var surface = new Surface(root, source);
                var skinMat = source.sharedMaterials[0];
                var uvMesh = source.sharedMesh; var m2 = root.transform.worldToLocalMatrix * source.transform.localToWorldMatrix; var uvs = uvMesh.uv; var vv = uvMesh.vertices;
                int best = -1; float bd = float.MaxValue; var target = new Vector3(.60f, 1.374f, -.043f);
                for (int i = 0; i < vv.Length; i++) { var p = m2.MultiplyPoint3x4(vv[i]); p.x = Mathf.Abs(p.x); float d = (p - target).sqrMagnitude; if (d < bd) { bd = d; best = i; } }
                // the neck/torso takes its colour from the lower face so the neck matches the head; arms keep the forearm sample (matches the hands)
                int headIdx = surface.Bone("Head"), bestHead = best; float bdh = float.MaxValue; var tH = new Vector3(0f, 1.49f, -.02f);
                var weights = uvMesh.boneWeights;
                for (int i = 0; i < vv.Length; i++) { if (weights[i].boneIndex0 != headIdx || weights[i].weight0 < .6f) continue; var p = m2.MultiplyPoint3x4(vv[i]); float d = (p - tH).sqrMagnitude; if (d < bdh) { bdh = d; bestHead = i; } }
                void Piece(string rendererName, string assetName, Shape shape)
                {
                    var br = Bind(root, rendererName, surface); var mesh = Asset(shape.Mesh(assetName), assetName + ".asset"); br.sharedMesh = mesh;
                    var uv2 = new List<Vector2>(); for (int i = 0; i < mesh.vertexCount; i++) uv2.Add(uvs[rendererName == "WardrobeTorso" ? bestHead : best]); mesh.SetUVs(0, uv2); EditorUtility.SetDirty(mesh);
                    br.sharedMaterials = new[] { rendererName == "WardrobeTorso" ? AssetDatabase.LoadAssetAtPath<Material>(Art + "/Sacat_Skin.mat") : skinMat }; br.enabled = true; br.updateWhenOffscreen = true;
                }
                Piece("WardrobeArms", "Sacat_Arms", BuildArms(surface));
                Piece("WardrobeTorso", "Sacat_Torso", BuildTorso(surface));
                sacatBareArms = false; sacatBareTorso = false;
                AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                Debug.Log("MINI199_SACAT_TORSO_PASS");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }
    }
}
