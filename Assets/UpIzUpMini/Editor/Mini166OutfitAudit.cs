using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace UpIzUpMini.EditorTools {
public static class Mini166OutfitAudit {
 public static void Run(){Directory.CreateDirectory("Logs/Tasks/MINI-166");EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity");var lines=new System.Collections.Generic.List<string>();
 foreach(string who in new[]{"Sacat","Franki"}){var root=GameObject.Find(who);var a=root.GetComponentInChildren<Animator>();foreach(var bone in new[]{HumanBodyBones.Head,HumanBodyBones.Neck,HumanBodyBones.Hips,HumanBodyBones.Chest,HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.LeftToes}){var t=a.GetBoneTransform(bone);lines.Add(who+" "+bone+" "+root.transform.InverseTransformPoint(t.position).ToString("F4"));}
 foreach(var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)){var v=r.sharedMesh.vertices.Select(p=>root.transform.InverseTransformPoint(r.transform.TransformPoint(p))).ToArray();lines.Add(who+" renderer="+r.name+" path="+AssetDatabase.GetAssetPath(r.sharedMesh)+" tris="+r.sharedMesh.triangles.Length/3+" materials="+string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"null"))+" min="+new Vector3(v.Min(p=>p.x),v.Min(p=>p.y),v.Min(p=>p.z)).ToString("F4")+" max="+new Vector3(v.Max(p=>p.x),v.Max(p=>p.y),v.Max(p=>p.z)).ToString("F4"));}}
 File.WriteAllLines("Logs/Tasks/MINI-166/audit.txt",lines);EditorApplication.Exit(0);}
}}
