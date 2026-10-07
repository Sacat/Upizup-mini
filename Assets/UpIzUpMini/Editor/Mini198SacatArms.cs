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
    /// MINI-198 (piece 1 of the Sacat rebuild: arms). Sacat's original model wore a hoodie, so the "arm" below the sleeve was never a bare arm.
    /// This builds real bare arms (shoulder to wrist) as a ring sweep along his own arm bones, with anatomical cross-sections taken from the original
    /// silhouette (deltoid cap, upper arm thicker at the back than the front, tapering elbow, forearm bulge, wrist), skinned to the same skeleton.
    /// The arms are a separate renderer, so any sleeve length or colour sits on top of them.
    /// </summary>
    public static partial class Mini166Repair
    {
        // station x (distance from the body centre), half-extent up/down, front, back, centre y, centre z
        static readonly float[,] ArmKeys =
        {
            { .190f, .052f, .050f, .052f, 1.372f, -.030f },
            { .220f, .062f, .060f, .062f, 1.368f, -.030f },
            { .260f, .052f, .054f, .058f, 1.367f, -.032f },
            { .310f, .046f, .051f, .056f, 1.367f, -.034f },
            { .360f, .044f, .049f, .054f, 1.367f, -.036f },
            { .410f, .042f, .045f, .048f, 1.368f, -.039f },
            { .440f, .040f, .042f, .044f, 1.369f, -.041f },
            { .490f, .042f, .045f, .045f, 1.371f, -.043f },
            { .550f, .039f, .042f, .042f, 1.373f, -.043f },
            { .600f, .032f, .038f, .038f, 1.377f, -.038f },
            { .640f, .029f, .037f, .037f, 1.377f, -.033f },
            { .685f, .029f, .040f, .040f, 1.372f, -.022f },
        };

        static float[] ArmAt(float x)
        {
            int n = ArmKeys.GetLength(0); x = Mathf.Clamp(x, ArmKeys[0, 0], ArmKeys[n - 1, 0]);
            int i = 0; while (i < n - 2 && x > ArmKeys[i + 1, 0]) i++;
            float t = Mathf.InverseLerp(ArmKeys[i, 0], ArmKeys[i + 1, 0], x); t = t * t * (3f - 2f * t);
            var r = new float[5]; for (int k = 0; k < 5; k++) r[k] = Mathf.Lerp(ArmKeys[i, k + 1], ArmKeys[i + 1, k + 1], t); return r;
        }

        static BoneWeight ArmWeight(Surface s, int side, float x)
        {
            string p = side < 0 ? "Left" : "Right";
            int shoulder = s.Bone(p + "Shoulder"), arm = s.Bone(p + "Arm"), fore = s.Bone(p + "ForeArm"), hand = s.Bone(p + "Hand");
            float wSh = 1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.19f, .25f, x));
            float wFore = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.38f, .50f, x));
            float wHand = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.60f, .66f, x)) * .5f;
            float wArm = Mathf.Max(0f, 1f - wFore) * (1f - wSh);
            var d = new[] { (shoulder, wSh * .5f), (arm, wArm + wSh * .5f), (fore, wFore * (1f - wHand)), (hand, wHand) };
            var order = d.OrderByDescending(e => e.Item2).Take(4).ToArray(); float sum = order.Sum(e => e.Item2); var w = new BoneWeight();
            w.boneIndex0 = order[0].Item1; w.weight0 = order[0].Item2 / sum; w.boneIndex1 = order[1].Item1; w.weight1 = order[1].Item2 / sum;
            w.boneIndex2 = order[2].Item1; w.weight2 = order[2].Item2 / sum; w.boneIndex3 = order[3].Item1; w.weight3 = order[3].Item2 / sum; return w;
        }

        static Shape BuildArms(Surface s)
        {
            var shape = new Shape(s, 1); const int ring = 24; const float step = .018f;
            foreach (int side in new[] { -1, 1 })
            {
                var stations = new List<V[]>(); var xs = new List<float>();
                for (float x = ArmKeys[0, 0]; x <= ArmKeys[ArmKeys.GetLength(0) - 1, 0] + .0001f; x += step) xs.Add(x);
                if (Mathf.Abs(xs.Last() - ArmKeys[ArmKeys.GetLength(0) - 1, 0]) > .002f) xs.Add(ArmKeys[ArmKeys.GetLength(0) - 1, 0]);
                foreach (float x in xs)
                {
                    var a = ArmAt(x); var ringV = new V[ring]; var w = ArmWeight(s, side, x);
                    for (int k = 0; k < ring; k++)
                    {
                        float ang = k * Mathf.PI * 2f / ring; float cy = Mathf.Cos(ang), cz = Mathf.Sin(ang);
                        float rz = cz >= 0 ? a[1] : a[2];
                        var p = new Vector3(side * x, a[3] + a[0] * cy, a[4] + rz * cz);
                        var n = new Vector3(0, cy / Mathf.Max(.01f, a[0]), cz / Mathf.Max(.01f, rz)).normalized;
                        ringV[k] = new V { p = p, n = n, uv = new Vector2(k / (float)ring, x), w = w };
                    }
                    stations.Add(ringV);
                }
                for (int i = 0; i + 1 < stations.Count; i++)
                    for (int k = 0; k < ring; k++)
                    {
                        int k2 = (k + 1) % ring; V a0 = stations[i][k], a1 = stations[i][k2], b0 = stations[i + 1][k], b1 = stations[i + 1][k2];
                        if (side < 0) { shape.Poly(new List<V> { a0, a1, b1 }, 0); shape.Poly(new List<V> { a0, b1, b0 }, 0); }
                        else { shape.Poly(new List<V> { a0, b1, a1 }, 0); shape.Poly(new List<V> { a0, b0, b1 }, 0); }
                    }
                // close the inner (shoulder) end with a fan, hidden inside the torso and shoulder
                var first = stations[0]; var c = new V { p = new Vector3(side * ArmKeys[0, 0], ArmKeys[0, 4], ArmKeys[0, 5]), n = new Vector3(-side, 0, 0), uv = Vector2.zero, w = first[0].w };
                for (int k = 0; k < ring; k++) { var a = first[k]; var b = first[(k + 1) % ring]; if (side < 0) shape.Poly(new List<V> { c, b, a }, 0); else shape.Poly(new List<V> { c, a, b }, 0); }
            }
            return shape;
        }

        static bool sacatBareArms;

        [MenuItem("Up Iz Up Mini/MINI-198/Build Sacat Bare Arms")]
        public static void BuildSacatArms()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(Environment.GetEnvironmentVariable("MINI196_SCENE") ?? "Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity", OpenSceneMode.Single);
                Directory.CreateDirectory(Out); Directory.CreateDirectory(Art);
                var root = GameObject.Find("Sacat");
                sacatBareArms = true; BuildCharacter(root);
                var source = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "Ch06");
                var surface = new Surface(root, source);
                var shape = BuildArms(surface);
                var br = Bind(root, "WardrobeArms", surface);
                br.sharedMesh = Asset(shape.Mesh("Sacat_Arms"), "Sacat_Arms.asset");
                // the same skin material as his hands, sampled at one forearm point so the arms match the hand tone exactly
                var skinMat = source.sharedMaterials[0];
                var uvMesh = source.sharedMesh; var m2 = root.transform.worldToLocalMatrix * source.transform.localToWorldMatrix; var uvs = uvMesh.uv; var vv = uvMesh.vertices;
                int best = -1; float bd = float.MaxValue; var target = new Vector3(.60f, 1.374f, -.043f);
                for (int i = 0; i < vv.Length; i++) { var p = m2.MultiplyPoint3x4(vv[i]); p.x = Mathf.Abs(p.x); float d = (p - target).sqrMagnitude; if (d < bd) { bd = d; best = i; } }
                var armMesh = br.sharedMesh; var uv2 = new List<Vector2>(); for (int i = 0; i < armMesh.vertexCount; i++) uv2.Add(uvs[best]); armMesh.SetUVs(0, uv2); EditorUtility.SetDirty(armMesh);
                br.sharedMaterials = new[] { skinMat };
                br.enabled = true; br.updateWhenOffscreen = true;
                sacatBareArms = false;
                AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                Debug.Log("MINI198_SACAT_ARMS_PASS verts=" + br.sharedMesh.vertexCount + " tris=" + br.sharedMesh.triangles.Length / 3);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }
    }
}
