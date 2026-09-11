using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;

namespace UpIzUpMini.EditorTools {
public static class Mini164WaveHair {
 const string Scene="Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
 const string Dir="Assets/UpIzUpMini/Art/Characters/Garments/Waves";
 const string Out="Logs/Tasks/MINI-164";
 static void Require(bool value,string message){if(!value)throw new Exception(message);}
 public static void Preview(){Run(false);}
 public static void Integrate(){Run(true);}
 public static void Evidence(){EditorSceneManager.OpenScene(Scene);Render();Debug.Log("MINI164_SAVED_EVIDENCE_PASS");EditorApplication.Exit(0);}
 static void Run(bool save){
  Directory.CreateDirectory(Dir);Directory.CreateDirectory(Out);
  if(!File.Exists(Out+"/GrandBayProof-before-waves.unity"))File.Copy(Scene,Out+"/GrandBayProof-before-waves.unity");
  EditorSceneManager.OpenScene(Scene);
  Material mat=MakeMaterial();
  foreach(string who in new[]{"Franki","Sacat"}){
   var root=GameObject.Find(who);Require(root!=null,who+" missing");
   var live=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r=>r.name==(who=="Franki"?"Ch28_Hair":"Ch06"));
   // Clone the live mesh: Sacat's repaired clothes, UVs and skinning must survive.
   Mesh original=live.sharedMesh; if(who=="Sacat"){string baseline=Dir+"/SacatBeforeWaves.asset";var baseMesh=AssetDatabase.LoadAssetAtPath<Mesh>(baseline);if(baseMesh==null){baseMesh=UnityEngine.Object.Instantiate(original);AssetDatabase.CreateAsset(baseMesh,baseline);}original=baseMesh;}
   if(who=="Franki"){
    var src=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UpIzUpMini/Art/Characters/Garments/Franki_Hair.fbx");
    original=src.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r=>r.name=="Ch28_Hair").sharedMesh;
   }
   var verts=original.vertices;var normals=original.normals;var weights=original.boneWeights;
   var uv=original.uv;var selected=new List<int>();var keep=new List<int>[original.subMeshCount];
   int head=Array.FindIndex(live.bones,b=>b!=null && b.name.EndsWith(":Head"));
   Require(head>=0,"head bone missing");
   var local=verts.Select(v=>root.transform.InverseTransformPoint(live.transform.TransformPoint(v))).ToArray();
   for(int s=0;s<original.subMeshCount;s++){ var ids=original.GetTriangles(s).Distinct().Where(k=>weights[k].boneIndex0==head).ToArray();if(ids.Length>0)Debug.Log(who+" sub="+s+" mat="+(live.sharedMaterials[Mathf.Min(s,live.sharedMaterials.Length-1)]?.name ?? "null")+" head count="+ids.Length+" min="+new Vector3(ids.Min(k=>local[k].x),ids.Min(k=>local[k].y),ids.Min(k=>local[k].z))+" max="+new Vector3(ids.Max(k=>local[k].x),ids.Max(k=>local[k].y),ids.Max(k=>local[k].z)));
    keep[s]=new List<int>();var tris=original.GetTriangles(s);
    for(int i=0;i<tris.Length;i+=3){
     bool hair=who=="Franki";
     if(who=="Sacat" && s==2){
      hair=true;float y=0;
      for(int j=0;j<3;j++){var w=weights[tris[i+j]];hair &= w.boneIndex0==head && w.weight0>.5f;y+=local[tris[i+j]].y;}
      hair &= y/3f>1.59f;
     }
     for(int j=0;j<3;j++)(hair?selected:keep[s]).Add(tris[i+j]);
    }
   }
   Require(selected.Count>150,who+" no scalp selected");
   var points=selected.Distinct().Select(i=>local[i]).ToArray();
   var min=new Vector3(points.Min(p=>p.x),points.Min(p=>p.y),points.Min(p=>p.z));
   var max=new Vector3(points.Max(p=>p.x),points.Max(p=>p.y),points.Max(p=>p.z));
   Debug.Log(who+" scalp triangles="+selected.Count/3+" min="+min+" max="+max);
   var center=(min+max)*.5f;center.y=min.y;
   var radius=new Vector3((max.x-min.x)*.5f,max.y-min.y,(max.z-min.z)*.5f);
   var vv=verts.ToList();var nn=normals.ToList();var ww=weights.ToList();var uu=uv.ToList();var hairTris=new List<int>();
   // Replace card hair / fused cap with a continuous, low-profile scalp shell.
   // The old scalp triangles are omitted; body, face and headphone triangles stay intact.
   var fitRenderer=who=="Franki"?root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r=>r.name=="Ch28_Body"):live;var fitMesh=who=="Franki"?fitRenderer.sharedMesh:original;var fitVerts=fitMesh.vertices.Select(v=>root.transform.InverseTransformPoint(fitRenderer.transform.TransformPoint(v))).ToArray();var fitTriangles=who=="Franki"?fitMesh.triangles:selected.ToArray();int first=vv.Count;const int rings=24,segments=64;
   center=who=="Franki"?new Vector3(0,1.575f,.014f):new Vector3(0,1.600f,.005f);
   radius=who=="Franki"?new Vector3(.088f,.124f,.112f):new Vector3(.090f,.104f,.101f);
   for(int row=0;row<=rings;row++)for(int col=0;col<=segments;col++){
    float u=(float)col/segments,phi=u*Mathf.PI*2;
    float front=Mathf.SmoothStep(0,1,Mathf.Clamp01((Mathf.Sin(phi)+.15f)/.65f));
    float limit=who=="Sacat"?Mathf.Lerp(1.72f,1.60f,front):Mathf.Lerp(1.72f,1.19f,front);
    float theta=Mathf.Lerp(.001f,limit,(float)row/rings);
    var d=new Vector3(Mathf.Sin(theta)*Mathf.Cos(phi),Mathf.Cos(theta),Mathf.Sin(theta)*Mathf.Sin(phi));
    float edge=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.68f,1f,(float)row/rings));var fittedRadius=new Vector3(radius.x*(1-.10f*edge),radius.y,radius.z*(1-.025f*edge));var pos=center+Vector3.Scale(fittedRadius,d);var ray=(pos-center).normalized;float nearest=float.PositiveInfinity;for(int ti=0;ti<fitTriangles.Length;ti+=3){float hit=RayTriangle(center,ray,fitVerts[fitTriangles[ti]],fitVerts[fitTriangles[ti+1]],fitVerts[fitTriangles[ti+2]]);if(hit>.025f && hit<nearest)nearest=hit;}if(who=="Franki" && !float.IsInfinity(nearest))pos=center+ray*(nearest+.0015f);
    vv.Add(live.transform.InverseTransformPoint(root.transform.TransformPoint(pos)));
    nn.Add(live.transform.InverseTransformDirection(root.transform.TransformDirection(new Vector3(d.x/radius.x,d.y/radius.y,d.z/radius.z).normalized)));
    ww.Add(new BoneWeight{boneIndex0=head,weight0=1});uu.Add(new Vector2(u,theta/(Mathf.PI*.5f)));
   }
   for(int row=0;row<rings;row++)for(int col=0;col<segments;col++){
    int a=first+row*(segments+1)+col,b=a+1,c=a+segments+1,d=c+1;
    hairTris.AddRange(new[]{a,b,c,b,d,c});
   }   var mesh=new Mesh{name=who+" Short Waves",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
   mesh.SetVertices(vv);mesh.SetNormals(nn);mesh.SetUVs(0,uu);mesh.boneWeights=ww.ToArray();mesh.bindposes=original.bindposes;
   mesh.subMeshCount=keep.Length+1;for(int s=0;s<keep.Length;s++)mesh.SetTriangles(keep[s],s);mesh.SetTriangles(hairTris,keep.Length);
   mesh.RecalculateTangents();mesh.RecalculateBounds();
   // All original vertex positions, weights and body UVs are retained verbatim.
   Require(mesh.vertices.Take(verts.Length).SequenceEqual(verts),"body positions changed");
   string path=Dir+"/"+who+"Waves.asset";
   if(save){var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old==null)AssetDatabase.CreateAsset(mesh,path);else{old.Clear();old.indexFormat=mesh.indexFormat;old.vertices=mesh.vertices;old.normals=mesh.normals;old.uv=mesh.uv;old.tangents=mesh.tangents;old.boneWeights=mesh.boneWeights;old.bindposes=mesh.bindposes;old.subMeshCount=mesh.subMeshCount;for(int q=0;q<mesh.subMeshCount;q++)old.SetTriangles(mesh.GetTriangles(q),q);old.bounds=mesh.bounds;EditorUtility.SetDirty(old);mesh=old;}}
   live.sharedMesh=mesh;live.localBounds=mesh.bounds;
   var mats=live.sharedMaterials.Take(keep.Length).ToList();while(mats.Count<keep.Length)mats.Add(mat);mats.Add(mat);live.sharedMaterials=mats.ToArray();
  }
  AssetDatabase.SaveAssets();
  if(save){EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());Debug.Log("MINI164_WAVES_INTEGRATED");}
  else {Render();Debug.Log("MINI164_WAVES_PREVIEW_PASS scene not saved");}
  EditorApplication.Exit(0);
 }
 static float RayTriangle(Vector3 o,Vector3 d,Vector3 a,Vector3 b,Vector3 c){var e1=b-a;var e2=c-a;var p=Vector3.Cross(d,e2);float det=Vector3.Dot(e1,p);if(Mathf.Abs(det)<.00000001f)return -1;float inv=1/det;var t=o-a;float u=Vector3.Dot(t,p)*inv;if(u<0||u>1)return -1;var q=Vector3.Cross(t,e1);float v=Vector3.Dot(d,q)*inv;if(v<0||u+v>1)return -1;return Vector3.Dot(e2,q)*inv;}
 static Material MakeMaterial(){
  const int n=512;var tex=new Texture2D(n,n,TextureFormat.RGB24,true);var bump=new Texture2D(n,n,TextureFormat.RGB24,true);
  for(int y=0;y<n;y++)for(int x=0;x<n;x++){
   float u=(float)x/n,v=(float)y/n;
   float phase=2*Mathf.PI*(v*17+.14f*Mathf.Sin(u*6*Mathf.PI)+.055f*Mathf.Sin(u*14*Mathf.PI));
   float ridge=Mathf.Pow(.5f+.5f*Mathf.Cos(phase),3);
   float grain=Mathf.PerlinNoise(x*.63f,y*.93f);
   float c=.025f+.025f*ridge+.018f*grain;
   tex.SetPixel(x,y,new Color(c,c,c*1.03f));
   float slope=.10f*Mathf.Sin(phase);var normal=new Vector3(0,slope,1).normalized;
   bump.SetPixel(x,y,new Color(normal.x*.5f+.5f,normal.y*.5f+.5f,normal.z*.5f+.5f));
  }
  tex.Apply();bump.Apply();File.WriteAllBytes(Dir+"/BlackWaves.png",tex.EncodeToPNG());File.WriteAllBytes(Dir+"/BlackWavesNormal.png",bump.EncodeToPNG());AssetDatabase.Refresh();
  foreach(string file in new[]{"BlackWaves","BlackWavesNormal"}){var importer=(TextureImporter)AssetImporter.GetAtPath(Dir+"/"+file+".png");importer.textureType=file.EndsWith("Normal")?TextureImporterType.NormalMap:TextureImporterType.Default;importer.wrapMode=TextureWrapMode.Repeat;importer.mipmapEnabled=true;importer.maxTextureSize=512;importer.SaveAndReimport();}
  string path=Dir+"/BlackWaves.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,path);}
  mat.color=Color.white;mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Dir+"/BlackWaves.png");if(mat.HasProperty("_Glossiness"))mat.SetFloat("_Glossiness",.18f);if(mat.HasProperty("_BaseColor"))mat.SetColor("_BaseColor",Color.white);mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Dir+"/BlackWaves.png"));mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Dir+"/BlackWavesNormal.png"));mat.EnableKeyword("_NORMALMAP");mat.SetFloat("_BumpScale",.5f);mat.SetFloat("_Smoothness",.18f);mat.SetFloat("_Metallic",0);EditorUtility.SetDirty(mat);return mat;
 }
 static void Render(){
  foreach(var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))l.enabled=false;
  RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.55f,.55f,.55f);RenderSettings.fog=false;RenderSettings.skybox=null;
  var light=new GameObject("Wave proof key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(35,-35,0);
  var cam=new GameObject("Wave proof camera").AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.35f,.39f,.43f);cam.nearClipPlane=.01f;cam.farClipPlane=100;cam.fieldOfView=30;
  var economy=UnityEngine.Object.FindFirstObjectByType<UpIzUpMini.Economy.EconomyManager>();if(economy!=null)typeof(UpIzUpMini.Economy.EconomyManager).GetMethod("Awake",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(economy,null);int index=0;
  foreach(string who in new[]{"Franki","Sacat"}){
   var root=GameObject.Find(who);root.SetActive(true);root.transform.position=new Vector3(index++*4,300,0);root.transform.rotation=Quaternion.identity;
   foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))skin.updateWhenOffscreen=true;var a=root.GetComponentInChildren<Animator>();a.Rebind();a.Update(0);
   var head=a.GetBoneTransform(HumanBodyBones.Head);var focus=head.position+Vector3.up*.08f;
   var equipment=root.GetComponent<CharacterEquipment>()??root.GetComponentInChildren<CharacterEquipment>(); if(equipment!=null)typeof(CharacterEquipment).GetMethod("Awake",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(equipment,null);
   foreach(bool cap in new[]{false,true}){
    if(equipment!=null && cap)equipment.SetTrialItem("cap_mike",true);
    foreach(var view in new[]{"Front","Side","Crown","Full"}){
     Vector3 delta=view=="Side"?new Vector3(.65f, .08f,.15f):view=="Crown"?new Vector3(.30f,.44f,.44f):new Vector3(0,.04f,.70f);
     Vector3 target=view=="Full"?root.transform.position+Vector3.up*.95f:focus;
     if(view=="Full")delta=new Vector3(0,.3f,4);
     cam.transform.position=target+delta;cam.transform.LookAt(target);
     var rt=new RenderTexture(800,800,24);cam.targetTexture=rt;cam.Render();cam.Render();RenderTexture.active=rt;var t=new Texture2D(800,800,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,800,800),0,0);t.Apply();File.WriteAllBytes(Out+"/"+who+"-Waves-"+(cap?"Cap":"NoCap")+"-"+view+".png",t.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(t);
    }
   }
  }
 }
}
}













