using UnityEditor;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-157: confirm both reshaped/coloured garment FBX assets
    /// (Sacat's texture-only reshape, Franki's already-reshaped+coloured
    /// mesh) import as valid Humanoid characters with a real SkinnedMeshRenderer,
    /// before any further Unity-side work. Read-only check, no scene/asset
    /// mutation beyond the standard import-settings pass every character FBX
    /// in this project already gets (see Mini016CharacterImport.cs).</summary>
    public static class Mini157VerifyReshaped
    {
        private static readonly string[] Models =
        {
            "Assets/UpIzUpMini/Art/Characters/Garments/Sacat_Ch06_Reshaped.fbx",
            "Assets/UpIzUpMini/Art/Characters/Garments/Franki_ReshapedGarments_Colored.fbx",
        };

        [MenuItem("Up Iz Up Mini/MINI-157/Verify Reshaped Garment FBX")]
        public static void Run()
        {
            foreach (var path in Models)
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                if (importer == null)
                {
                    Debug.LogError($"MINI157VERIFY: missing {path}");
                    continue;
                }
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = false;
                importer.SaveAndReimport();

                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var animator = go != null ? go.GetComponent<Animator>() : null;
                var renderers = go != null ? go.GetComponentsInChildren<SkinnedMeshRenderer>(true) : new SkinnedMeshRenderer[0];
                Debug.Log($"MINI157VERIFY {path}: valid={(animator != null && animator.avatar != null && animator.avatar.isValid)} " +
                          $"isHuman={(animator != null && animator.avatar != null && animator.avatar.isHuman)} " +
                          $"renderers={renderers.Length} totalVerts={System.Array.ConvertAll(renderers, r => r.sharedMesh != null ? r.sharedMesh.vertexCount : 0).Length}");
                foreach (var r in renderers)
                    Debug.Log($"MINI157VERIFY   renderer '{r.name}' mesh='{(r.sharedMesh != null ? r.sharedMesh.name : "null")}' verts={(r.sharedMesh != null ? r.sharedMesh.vertexCount : 0)}");
            }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
