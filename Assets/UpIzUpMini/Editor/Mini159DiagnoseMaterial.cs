using UnityEditor;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    public static class Mini159DiagnoseMaterial
    {
        [MenuItem("Up Iz Up Mini/MINI-159/Diagnose Sacat Material")]
        public static void Run()
        {
            var path = "Assets/UpIzUpMini/Art/Characters/Garments/Sacat_Ch06_Reshaped.fbx";
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (obj is Material m)
                {
                    Debug.Log($"MINI159MAT: '{m.name}' shader={m.shader.name} mainTex={(m.mainTexture != null ? m.mainTexture.name : "NULL")}");
                    for (int i = 0; i < ShaderUtil.GetPropertyCount(m.shader); i++)
                    {
                        string pn = ShaderUtil.GetPropertyName(m.shader, i);
                        var pt = ShaderUtil.GetPropertyType(m.shader, i);
                        if (pt == ShaderUtil.ShaderPropertyType.Float || pt == ShaderUtil.ShaderPropertyType.Range)
                            Debug.Log($"MINI159MAT:   {pn} = {m.GetFloat(pn)}");
                        else if (pt == ShaderUtil.ShaderPropertyType.Color)
                            Debug.Log($"MINI159MAT:   {pn} = {m.GetColor(pn)}");
                    }
                }
            }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
