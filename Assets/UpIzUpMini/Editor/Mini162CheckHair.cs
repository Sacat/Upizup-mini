using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    public static class Mini162CheckHair
    {
        [MenuItem("Up Iz Up Mini/MINI-162/Check Hair State")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);
            foreach (var name in new[] { "Sacat", "Franki" })
            {
                var root = GameObject.Find(name);
                Debug.Log($"MINI162HAIR: ==== {name} ====");
                foreach (var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (!r.name.ToLower().Contains("hair") && r.name != "Ch06") continue;
                    var mat = r.sharedMaterial;
                    Debug.Log($"MINI162HAIR:  renderer '{r.name}' enabled={r.enabled} active={r.gameObject.activeInHierarchy} " +
                              $"mesh={(r.sharedMesh != null ? r.sharedMesh.name : "null")} verts={(r.sharedMesh != null ? r.sharedMesh.vertexCount : 0)} " +
                              $"mat={(mat != null ? mat.name : "null")} mainTex={(mat != null && mat.mainTexture != null ? mat.mainTexture.name : "NULL")}");
                }
            }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
