using System.IO;
using UnityEditor;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-195: creates the muzzle flash texture and additive material under Resources (idempotent).</summary>
    public static class Mini195FxSetup
    {
        const string Dir = "Assets/UpIzUpMini/Resources/Weapons";

        [MenuItem("Up Iz Up Mini/MINI-195/Create Muzzle Flash Material")]
        public static void Run()
        {
            Directory.CreateDirectory(Dir);
            const int n = 128; var tex = new Texture2D(n, n, TextureFormat.RGBA32, true);
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float u = (x + .5f) / n * 2f - 1f, v = (y + .5f) / n * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v), a = Mathf.Atan2(v, u);
                float star = Mathf.Pow(Mathf.Abs(Mathf.Cos(a * 2.5f)), 3f) * 0.7f + 0.3f;       // spiky petals
                float fall = Mathf.Clamp01(1f - r / (star + .15f));
                float core = Mathf.Clamp01(1f - r / .35f);
                float val = Mathf.Clamp01(fall * fall * 1.4f + core * core);
                tex.SetPixel(x, y, new Color(1f, Mathf.Lerp(.55f, 1f, core), Mathf.Lerp(.15f, .75f, core), val));
            }
            tex.Apply();
            File.WriteAllBytes(Dir + "/MuzzleFlash.png", tex.EncodeToPNG());
            AssetDatabase.ImportAsset(Dir + "/MuzzleFlash.png");
            var imp = (TextureImporter)AssetImporter.GetAtPath(Dir + "/MuzzleFlash.png"); imp.alphaIsTransparency = true; imp.wrapMode = TextureWrapMode.Clamp; imp.SaveAndReimport();
            var shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(Dir + "/MuzzleFlash.mat");
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, Dir + "/MuzzleFlash.mat"); }
            mat.shader = shader; mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "/MuzzleFlash.png");
            if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", new Color(1f, 1f, 1f, 1f));
            EditorUtility.SetDirty(mat); AssetDatabase.SaveAssets();
            Debug.Log("MINI195_FX_PASS shader=" + mat.shader.name);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
