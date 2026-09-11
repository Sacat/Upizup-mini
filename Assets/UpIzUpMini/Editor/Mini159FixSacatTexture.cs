using UnityEditor;
using UnityEngine;
using System.Linq;

namespace UpIzUpMini.EditorTools
{
    public static class Mini159FixSacatTexture
    {
        [MenuItem("Up Iz Up Mini/MINI-159/Fix Sacat Texture Wiring")]
        public static void Run()
        {
            var path = "Assets/UpIzUpMini/Art/Characters/Garments/Sacat_Ch06_Reshaped.fbx";
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in assets)
                Debug.Log($"MINI159FIX: asset '{a.name}' type={a.GetType().Name}");

            var tex = assets.OfType<Texture2D>().FirstOrDefault(t => t.name.Contains("Reshaped") || t.name.Contains("Diffuse"));
            var mat = assets.OfType<Material>().FirstOrDefault(m => m.name == "Ch06_body_Reshaped");
            if (tex == null)
            {
                // fall back: the repainted PNG copied in from the Blender output
                // (the FBX's embed_textures export silently failed to include
                // it - a real bug caught by checking mainTexture, not assumed).
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UpIzUpMini/Art/Characters/Garments/Ch06_1001_Diffuse_Reshaped.png");
            }
            Debug.Log($"MINI159FIX: chosen tex={(tex != null ? tex.name : "STILL NULL")} mat={(mat != null ? mat.name : "NULL")}");
            if (tex != null && mat != null)
            {
                mat.mainTexture = tex;
                EditorUtility.SetDirty(mat);
                AssetDatabase.SaveAssets();
                Debug.Log("MINI159FIX: wired mainTexture on " + mat.name);
            }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
