using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace UpIzUpMini.EditorTools {public static class Mini173Validation {
public static void Run(){try{
EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof_ExpansionImport.unity");var all=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);int active=all.Count(t=>t.name.StartsWith("Lalay_Shanty_")&&t.gameObject.activeInHierarchy);var homes=GameObject.Find("MINI173_LalayHomes").transform;
if(active!=0||homes.childCount!=31)throw new Exception("House replacement count mismatch");var field=GameObject.Find("GenevaField").transform;
if(field.GetComponentsInChildren<Transform>().Any(t=>new[]{"PitchLine","GoalPost","GoalCrossbar","GoalNet","MownStripe"}.Any(p=>t.name.StartsWith(p))))throw new Exception("Football objects remain");
var terrain=GameObject.Find("MINI168_Expansion").transform.Find("ExpansionTerrain");if(terrain.GetComponent<MeshFilter>().sharedMesh!=terrain.GetComponent<MeshCollider>().sharedMesh)throw new Exception("Terrain collision mesh mismatch");
var roads=Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Where(c=>c.name.StartsWith("ExpansionRoad_")).ToArray();int roadHouseHits=0;var allRoads=Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Where(r=>r.name.Contains("Road")).ToArray();
foreach(Transform h in homes)foreach(var c in h.GetComponentsInChildren<BoxCollider>()){var b=c.bounds;foreach(float x in new[]{b.min.x,b.center.x,b.max.x})foreach(float z in new[]{b.min.z,b.center.z,b.max.z})foreach(var road in allRoads)if(road.Raycast(new Ray(new Vector3(x,100,z),Vector3.down),out _,200))roadHouseHits++;}
if(roadHouseHits>0)throw new Exception("House footprint hits road "+roadHouseHits);
var ring=GameObject.Find("ExpansionRoad_9").GetComponent<MeshFilter>();float minDry=float.PositiveInfinity;foreach(var v in ring.sharedMesh.vertices){var p=ring.transform.TransformPoint(v);float shore=p.z<-160?250:p.z<-60?250+(p.z+160)*.55f:305+(p.z+60)*.25f;minDry=Mathf.Min(minDry,shore-p.x);}
if(minDry<2)throw new Exception("Roundabout too close to shoreline "+minDry);
Directory.CreateDirectory("Logs/Tasks/MINI-173");File.WriteAllText("Logs/Tasks/MINI-173/Validation.txt","PASS\nactiveShanties=0\nreplacementHomes=31\nfootballObjects=0\nroadHouseFootprintHits=0\nroundaboutMinimumShoreClearance="+minDry+"\nterrainMeshColliderMatches=true\n");Debug.Log("MINI173_VALIDATION_PASS");Mini173LalayRevision.Render();
}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}}

