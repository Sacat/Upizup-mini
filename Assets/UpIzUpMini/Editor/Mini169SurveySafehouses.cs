using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-169 diagnostic: dump EVERY child (mesh + collider info)
    /// of the FarmSafehouse_Building specifically, to find out whether it
    /// is genuinely open-fronted (walkable already) or blocked by a single
    /// bounding-box collider like the generic MINI-142 houses use.</summary>
    public static class Mini169SurveySafehouses
    {
        [MenuItem("Up Iz Up Mini/MINI-169/Survey Safehouse Exteriors")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);
            var farm = GameObject.Find("FarmSafehouse_Building");
            if (farm == null) { Debug.LogError("MINI169SURVEY: FarmSafehouse_Building not found"); }
            else
            {
                Debug.Log($"MINI169SURVEY: === FarmSafehouse_Building full children, pos={farm.transform.position} rot={farm.transform.eulerAngles} ===");
                foreach (Transform t in farm.GetComponentsInChildren<Transform>(true))
                {
                    t.TryGetComponent<MeshRenderer>(out var mr);
                    t.TryGetComponent<MeshFilter>(out var mf);
                    var col = t.GetComponents<Collider>();
                    string colInfo = col.Length == 0 ? "none" : string.Join(",", col.Select(c => $"{c.GetType().Name}(enabled={c.enabled},isTrigger={c.isTrigger},bounds={c.bounds.size})"));
                    string meshName = mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : "-";
                    string matName = mr != null && mr.sharedMaterial != null ? mr.sharedMaterial.name : "-";
                    Debug.Log($"MINI169SURVEY:   '{t.name}' localPos={t.localPosition} localScale={t.localScale} localRot={t.localEulerAngles} sharedMesh={meshName} material={matName} mesh={(mr!=null?mr.bounds.size.ToString():"-")} colliders=[{colInfo}]");
                }
            }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
