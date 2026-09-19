using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace UpIzUpMini.EditorTools {
 public static class Mini173Survey {
 public static void Run(){
 EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof_ExpansionImport.unity");
 Directory.CreateDirectory("Logs/Tasks/MINI-173");
 var ts=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
 File.WriteAllLines("Logs/Tasks/MINI-173/Survey.txt",ts.Where(t=>t.name.Contains("Shanty")||t.name.Contains("Sea")||t.name.Contains("Coast")||t.name.Contains("Bay")||t.name.Contains("Geneva")||t.name.Contains("Roundabout")).Select(t=>t.name+" parent="+t.parent?.name+" position="+t.position+" scale="+t.lossyScale+" bounds="+Bounds(t)));
 EditorApplication.Exit(0);
 }
 static string Bounds(Transform t){var rs=t.GetComponentsInChildren<Renderer>(); if(rs.Length==0)return "none";var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b.ToString();}
 }
}
