using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-199 read-only: samples Sacat's skin texture colour at the face, neck-side, forearm and hand regions.</summary>
    public static class Mini199SkinTone
    {
        static Texture2D Readable(Texture tex)
        {
            var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(tex, rt); var prev = RenderTexture.active; RenderTexture.active = rt;
            var t2 = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false); t2.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0); t2.Apply(); RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt); return t2;
        }

        public static void Run()
        {
            try
            {
                EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity", OpenSceneMode.Single);
                var root = GameObject.Find("Sacat"); var smr = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "Ch06");
                var mat = smr.sharedMaterials[0]; var tex = Readable(mat.mainTexture); var mesh = smr.sharedMesh; var uv = mesh.uv; var bw = mesh.boneWeights;
                var m = root.transform.worldToLocalMatrix * smr.transform.localToWorldMatrix; var vs = mesh.vertices.Select(v => m.MultiplyPoint3x4(v)).ToArray();
                int head = Array.FindIndex(smr.bones, b => b && b.name.EndsWith(":Head"));
                Color Avg(Func<int, bool> pick)
                {
                    Color c = Color.black; int n = 0; for (int i = 0; i < vs.Length; i++) if (pick(i)) { c += tex.GetPixelBilinear(uv[i].x, uv[i].y); n++; }
                    return n > 0 ? c / n : Color.magenta;
                }
                Debug.Log("MINI199_TONE shader=" + mat.shader.name + " matColor=" + (mat.HasProperty("_Color") ? mat.color.ToString() : "-"));
                Debug.Log("MINI199_TONE face(head bone, y 1.52-1.70, z>0.03)=" + Avg(i => bw[i].boneIndex0 == head && bw[i].weight0 > .6f && vs[i].y > 1.52f && vs[i].y < 1.70f && vs[i].z > .03f));
                Debug.Log("MINI199_TONE jaw/neck(head bone, y 1.45-1.52)=" + Avg(i => bw[i].boneIndex0 == head && bw[i].weight0 > .6f && vs[i].y > 1.45f && vs[i].y < 1.52f));
                Debug.Log("MINI199_TONE forearm(x .56-.62)=" + Avg(i => Mathf.Abs(vs[i].x) > .56f && Mathf.Abs(vs[i].x) < .62f));
                Debug.Log("MINI199_TONE hand(x .72-.82)=" + Avg(i => Mathf.Abs(vs[i].x) > .72f && Mathf.Abs(vs[i].x) < .82f));
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
