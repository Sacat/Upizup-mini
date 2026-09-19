using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace UpIzUpMini.EditorTools {public static class Mini173BayRevision {
 const string Art="Assets/UpIzUpMini/Art/Environment/Mini173/";
 static float Coast(float z){return z < -160 ? 250 : z < -60 ? 250+(z+160)*.55f : 305+(z+60)*.25f;}
 public static void Apply(){try{
 var scene=EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof_ExpansionImport.unity");var terrain=GameObject.Find("MINI168_Expansion").transform.Find("ExpansionTerrain");var f=terrain.GetComponent<MeshFilter>();var c=terrain.GetComponent<MeshCollider>();
 var baseline=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"TerrainBeforeCoast.asset");if(!baseline){baseline=Object.Instantiate(f.sharedMesh);AssetDatabase.CreateAsset(baseline,Art+"TerrainBeforeCoast.asset");}
 c.sharedMesh=baseline;Physics.SyncTransforms();
 var old=GameObject.Find("MINI173_CoastalBay");if(old)Object.DestroyImmediate(old);var root=new GameObject("MINI173_CoastalBay");
 var roads=Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Where(q=>q.name.StartsWith("ExpansionRoad_")).ToArray();
 float Height(Vector3 p){float raw=p.y;float shore=Coast(p.z)-p.x;float grade=Mathf.SmoothStep(0,1,Mathf.Clamp01(shore/18));float y=Mathf.Lerp(.32f,raw,grade);foreach(var r in roads)if(r.Raycast(new Ray(new Vector3(p.x,150,p.z),Vector3.down),out var hit,300))return hit.point.y-.025f;return y;}
 var vs=baseline.vertices.Select(v=>terrain.TransformPoint(v)).ToArray();var ts=baseline.triangles;var verts=new List<Vector3>();var tris=new List<int>();
 for(int t=0;t<ts.Length;t+=3){var poly=new List<Vector3>{vs[ts[t]],vs[ts[t+1]],vs[ts[t+2]]};var clipped=new List<Vector3>();
 for(int j=0;j<3;j++){var a=poly[j];var b=poly[(j+1)%3];float da=Coast(a.z)-a.x,db=Coast(b.z)-b.x;if(da>=0)clipped.Add(a);if((da>=0)!=(db>=0))clipped.Add(Vector3.Lerp(a,b,da/(da-db)));}
 if(clipped.Count<3)continue;int start=verts.Count;foreach(var p0 in clipped){var p=p0;p.y=Height(p);verts.Add(terrain.InverseTransformPoint(p));}for(int j=1;j<clipped.Count-1;j++){tris.Add(start);tris.Add(start+j);tris.Add(start+j+1);}}
 // Weld clipped triangle vertices before recalculating normals: avoid flat-shading
// the entire landscape and avoid tripling the terrain's vertex memory.
var unique=new Dictionary<Vector3Int,int>();var welded=new List<Vector3>();var remap=new int[verts.Count];
for(int i=0;i<verts.Count;i++){var key=Vector3Int.RoundToInt(verts[i]*10000);if(!unique.TryGetValue(key,out int j)){j=welded.Count;unique[key]=j;welded.Add(verts[i]);}remap[i]=j;}
for(int i=0;i<tris.Count;i++)tris[i]=remap[tris[i]];verts=welded;var mesh=new Mesh{name="Mini173CoastalTerrain",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh=Save(mesh,"CoastalTerrain");f.sharedMesh=mesh;c.sharedMesh=mesh;
 // Shore sand uses the same new collider heights, with a tiny visual offset; no floating platform.
 var sandV=new List<Vector3>();var sandT=new List<int>();var seaV=new List<Vector3>();var seaT=new List<int>();
 for(int i=0;i<=150;i++){float z=-160+i*2f;float x=Coast(z);foreach(float inset in new[]{0f,6f}){var p=new Vector3(x-inset,.32f,z);if(c.Raycast(new Ray(new Vector3(p.x,150,p.z),Vector3.down),out var hit,300))p.y=hit.point.y+.015f;sandV.Add(p);}seaV.Add(new Vector3(x,.25f,z));seaV.Add(new Vector3(900,.25f,z));if(i>0){int k=(i-1)*2;sandT.AddRange(new[]{k,k+1,k+2,k+1,k+3,k+2});seaT.AddRange(new[]{k,k+2,k+1,k+1,k+2,k+3});}}
 Make("CoastalBeach",sandV,sandT,Mat("VolcanicSand",new Color(.43f,.42f,.34f)),root.transform);
 var sea=GameObject.Find("Caribbean_Sea").GetComponent<Renderer>().sharedMaterial;Make("BayWater",seaV,seaT,sea,root.transform);
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("MINI173_COAST_PASS vertices="+verts.Count);EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 static Mesh Save(Mesh m,string name){var path=Art+name+".asset";var prior=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(prior){EditorUtility.CopySerialized(m,prior);Object.DestroyImmediate(m);return prior;}AssetDatabase.CreateAsset(m,path);return m;}
 static void Make(string n,List<Vector3> v,List<int> t,Material mat,Transform parent){var m=new Mesh{name=n};m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();var g=new GameObject(n);g.transform.SetParent(parent);g.AddComponent<MeshFilter>().sharedMesh=Save(m,n);g.AddComponent<MeshRenderer>().sharedMaterial=mat;}
 static Material Mat(string n,Color color){var m=AssetDatabase.LoadAssetAtPath<Material>(Art+n+".mat");if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,Art+n+".mat");}m.color=color;EditorUtility.SetDirty(m);return m;}
}}


