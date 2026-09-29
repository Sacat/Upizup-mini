using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace UpIzUpMini.EditorTools
{
    public static class Mini182SurveyExp
    {
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity", OpenSceneMode.Single);
            var exp = GameObject.Find("MINI168_Expansion").transform;
            foreach (Transform c in exp)
            {
                if (c.name.StartsWith("ExpansionRoad_") || c.name.StartsWith("ImportTrim") || c.name.StartsWith("Geneva") && c.name != "GenevaField") continue;
                var rs = c.GetComponentsInChildren<Renderer>(true); Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds(c.position, Vector3.zero); foreach (var r in rs) b.Encapsulate(r.bounds);
                var mats = string.Join("|", rs.SelectMany(r => r.sharedMaterials).Where(m => m != null).Select(m => m.name).Distinct().Take(3));
                Debug.Log($"MINI182EXP '{c.name}' active={c.gameObject.activeSelf} kids={c.childCount} centre=({b.center.x:F0},{b.center.y:F1},{b.center.z:F0}) size=({b.size.x:F1},{b.size.y:F1},{b.size.z:F1}) yaw={c.eulerAngles.y:F0} mats={mats} col={c.GetComponentsInChildren<Collider>(true).Length}");
            }
            var f = GameObject.Find("GenevaField").transform; foreach (Transform c in f) { var mf = c.GetComponent<MeshFilter>(); if (c.GetSiblingIndex() < 6 || c.name.Contains("Pitch") || c.name.Contains("Grass") || c.name.Contains("Field")) { var r = c.GetComponent<Renderer>(); Debug.Log($"MINI182FIELD '{c.name}' y={c.position.y:F2} size={(r ? r.bounds.size : Vector3.zero)} col={c.GetComponent<Collider>() != null}"); } }
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { string n = t.name; if (n.Contains("Boundar") || n.Contains("Barrier") || n.Contains("Invisible") || n.Contains("Wall_") || n.Contains("Blocker") || n.Contains("Fence_Limit") || n.Contains("Limit")) { var col = t.GetComponentsInChildren<Collider>(true); Debug.Log($"MINI182BLOCK '{n}' parent='{t.parent?.name}' active={t.gameObject.activeSelf} colliders={col.Length} pos={t.position}"); } }
            EditorApplication.Exit(0);
        }
    }
}
