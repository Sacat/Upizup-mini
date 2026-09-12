using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UpIzUpMini.Character;

namespace UpIzUpMini.EditorTools {
public static partial class Mini166Repair {
 const string Scene="Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
 const string Art="Assets/UpIzUpMini/Art/Characters/Garments/Outfits166";
 const string Out="Logs/Tasks/MINI-166/Repair";
 public static void FinalizeVisibility(){
  EditorSceneManager.OpenScene(Scene);
  foreach(var who in new[]{"Franki","Sacat"}){var root=GameObject.Find(who);var animator=root.GetComponentInChildren<Animator>(true);animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;EditorUtility.SetDirty(animator);foreach(var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)){if(!r.enabled)continue;r.updateWhenOffscreen=true;EditorUtility.SetDirty(r);}}
  EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());Debug.Log("MINI166_VISIBILITY_PASS");EditorApplication.Exit(0);
 }
 public static void Audit(){
  Directory.CreateDirectory(Out);EditorSceneManager.OpenScene(Scene);
  var lines=new List<string>();
  foreach(var name in new[]{"Franki","Sacat"}){
   var root=GameObject.Find(name);var w=root.GetComponent<OutfitWardrobe>();
   foreach(var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
    if(r.sharedMesh==null)continue;var mesh=r.sharedMesh;var m=root.transform.worldToLocalMatrix*r.transform.localToWorldMatrix;
    var v=mesh.vertices.Select(p=>m.MultiplyPoint3x4(p)).ToArray();
    lines.Add(name+" "+r.name+" path="+AssetDatabase.GetAssetPath(mesh)+" bounds="+mesh.bounds+" rootBounds="+BoundsOf(v)+" enabled="+r.enabled+" materials="+string.Join(",",r.sharedMaterials.Select(mat=>mat?mat.name:"null")));
    foreach(var suffix in new[]{":Head",":Neck",":Hips",":LeftArm",":LeftForeArm",":LeftUpLeg",":LeftLeg",":LeftFoot",":LeftToeBase"}){
     int i=Array.FindIndex(r.bones,b=>b&&b.name.EndsWith(suffix));if(i>=0&&i<mesh.bindposes.Length)lines.Add(suffix+" rest="+m.MultiplyPoint3x4(mesh.bindposes[i].inverse.MultiplyPoint3x4(Vector3.zero)).ToString("F4"));
    }
   }
   if(w)foreach(var p in w.pieces)lines.Add("PIECE "+name+" "+p.id+" mesh="+AssetDatabase.GetAssetPath(p.mesh)+" tint="+string.Join(",",p.tintSlots));
  }
  File.WriteAllLines(Out+"/baseline-audit.txt",lines);Debug.Log("MINI166_REPAIR_AUDIT_PASS");EditorApplication.Exit(0);
 }
 static Bounds BoundsOf(Vector3[] v){var b=new Bounds(v[0],Vector3.zero);foreach(var p in v)b.Encapsulate(p);return b;}
}
}
