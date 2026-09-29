using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace UpIzUpMini.EditorTools
{
    public static class Mini182SurveyCoast
    {
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity", OpenSceneMode.Single);
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.parent != null && t.parent.parent != null) continue;
                string n = t.name; if (!(n.Contains("Seawall") || n.Contains("Beach") || n.Contains("Geneva") || n.Contains("Shore") || n.Contains("Coast") || n.Contains("Sea") || n.Contains("Water") || n.Contains("Ocean") || n.Contains("Roundabout") || n.Contains("MINI168") || n.Contains("MINI180"))) continue;
                var rs = t.GetComponentsInChildren<Renderer>(true); Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds(t.position, Vector3.zero); foreach (var r in rs) b.Encapsulate(r.bounds);
                Debug.Log($"MINI182COAST '{t.name}' parent='{t.parent?.name}' active={t.gameObject.activeSelf} children={t.childCount} centre=({b.center.x:F0},{b.center.y:F0},{b.center.z:F0}) size=({b.size.x:F0},{b.size.y:F0},{b.size.z:F0})");
                if (n.Contains("MINI168") || n.Contains("MINI180") || n.Contains("Seawall"))
                    foreach (Transform c in t) { if (c.name.StartsWith("Expansion") || c.name.StartsWith("ImportTrim")) continue; var rr = c.GetComponentsInChildren<Renderer>(true); if (rr.Length == 0) continue; var bb = rr[0].bounds; foreach (var x in rr) bb.Encapsulate(x.bounds); if (c.name.Contains("Geneva") || c.name.Contains("Beach") || c.name.Contains("Round") || c.name.Contains("Seawall")) Debug.Log($"MINI182COAST   child '{c.name}' n={c.childCount} centre=({bb.center.x:F0},{bb.center.y:F0},{bb.center.z:F0}) size=({bb.size.x:F0},{bb.size.y:F0},{bb.size.z:F0})"); }
            }
            EditorApplication.Exit(0);
        }
    }
}
