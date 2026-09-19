using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace UpIzUpMini.EditorTools
{
    public static class Mini171SurveyFarm
    {
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof_ExpansionImport.unity", OpenSceneMode.Single);
            var f = GameObject.Find("FarmSafehouse_Rest"); Debug.Log("MINI171FARM anchor " + (f ? f.transform.position.ToString() : "none"));
            var c = f ? f.transform.position : Vector3.zero;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                var r = t.GetComponent<Renderer>(); if (r == null) continue;
                if (Vector2.Distance(new Vector2(r.bounds.center.x, r.bounds.center.z), new Vector2(c.x, c.z)) > 45) continue;
                Debug.Log($"MINI171FARM '{t.name}' parent='{t.parent?.name}' gp='{t.parent?.parent?.name}' c=({r.bounds.center.x:F1},{r.bounds.center.y:F1},{r.bounds.center.z:F1}) size=({r.bounds.size.x:F1},{r.bounds.size.y:F1},{r.bounds.size.z:F1}) mat={r.sharedMaterial?.name}");
            }
            EditorApplication.Exit(0);
        }
    }
}
