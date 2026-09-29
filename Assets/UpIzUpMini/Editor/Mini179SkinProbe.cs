using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;
namespace UpIzUpMini.EditorTools {
/// <summary>
/// MINI-179: hands look lighter than the arms on Sacat and Franki.
/// Probe - Play Mode: for both characters, raycast the forearm/wrist/hand against baked skinned meshes and report
///         renderer, material, texture and the albedo colour under each sample point; also renders close-ups.
/// </summary>
[InitializeOnLoad] public static class Mini179SkinProbe {
 const string Scene="Assets/UpIzUpMini/Scenes/GrandBayProof_ExpansionImport.unity",Out="Logs/Tasks/MINI-179",Key="Mini179Mode";
 static double next;static bool failed;
 static Mini179SkinProbe(){if(!string.IsNullOrEmpty(SessionState.GetString(Key,"")))EditorApplication.playModeStateChanged+=Changed;}
 public static void Probe(){Start("Probe");}
 public static void Fit(){Start("Fit");}
 public static void LegsBefore(){SessionState.SetString(Key+"Label","before");Start("Legs");}
 public static void LegsAfter(){SessionState.SetString(Key+"Label","after");Start("Legs");}
 // Same shots with the OLD flat skin swapped back on in Play Mode only (nothing saved) - a fair before image after the assets were changed.
 public static void LegsBeforeSim(){SessionState.SetString(Key+"Label","beforesim");Start("Legs");}
 // Edit mode, no Play Mode: every pants/shoes/shirt piece's "<name>_Skin" slot -> "<name>_ArmSkin" (same hand-matched colour), expansion scene only.
 public static void AssignLegSkin(){try{var scene=EditorSceneManager.OpenScene(Scene);var lines=new List<string>();
  foreach(var name in new[]{"Sacat","Franki"}){var arm=AssetDatabase.LoadAssetAtPath<Material>(MatDir+"/"+name+"_ArmSkin.mat");if(!arm)throw new Exception(name+"_ArmSkin.mat missing");
   var w=UnityEngine.Object.FindObjectsByType<OutfitWardrobe>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(x=>x.name==name);if(!w)throw new Exception("OutfitWardrobe missing on "+name);
   int n=0;foreach(var p in w.pieces){if(p==null||p.materials==null)continue;var copy=(Material[])p.materials.Clone();bool changed=false;for(int i=0;i<copy.Length;i++)if(copy[i]&&copy[i].name==name+"_Skin"){copy[i]=arm;changed=true;lines.Add(name+" "+p.id+" ["+p.slot+"] materials["+i+"] -> "+arm.name);}if(changed){p.materials=copy;n++;}}
   if(n==0)throw new Exception("Nothing to change on "+name);EditorUtility.SetDirty(w);}
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllLines(Out+"/AssignLegs.txt",lines);Debug.Log("MINI179_LEGS_ASSIGN_PASS");EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 // Play Mode: wear denim shorts so the legs show, probe shin/ankle/foot materials, render leg close-ups.
 static void LegsShots(string name,Transform character,Animator a,List<Surf> surfs,List<string> L,string label){
  var w=character.GetComponent<OutfitWardrobe>();if(w)w.Select("pants_shorts_denim",5);
  if(label=="beforesim"){var old=AssetDatabase.LoadAssetAtPath<Material>(MatDir+"/"+name+"_Skin.mat");foreach(var rn in new[]{"WardrobeShirt","WardrobePants","WardrobeShoes"}){var rr=character.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x=>x.name==rn);if(!rr)continue;var ms=rr.sharedMaterials;for(int i=0;i<ms.Length;i++)if(ms[i]&&ms[i].name==name+"_ArmSkin")ms[i]=old;rr.sharedMaterials=ms;}}
  var lo=a.GetBoneTransform(HumanBodyBones.LeftLowerLeg);var foot=a.GetBoneTransform(HumanBodyBones.LeftFoot);var toes=a.GetBoneTransform(HumanBodyBones.LeftToes);var up=a.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
  var pts=new List<(string,Vector3)>{("thigh-mid",Vector3.Lerp(up.position,lo.position,.5f)),("shin-25%",Vector3.Lerp(lo.position,foot.position,.25f)),("shin-60%",Vector3.Lerp(lo.position,foot.position,.6f)),("ankle",foot.position+Vector3.up*.03f),("foot",foot.position)};if(toes)pts.Add(("toes",toes.position));
  foreach(var (lab,p) in pts){foreach(var (approach,vec) in new[]{("front",character.forward),("out",-character.right)}){var ray=new Ray(p+vec*.5f,-vec);float best=float.PositiveInfinity;Surf bs=null;RaycastHit bh=default;foreach(var s in surfs)if(s.col.Raycast(ray,out var h,1f)&&h.distance<best){best=h.distance;bs=s;bh=h;}
    if(bs==null){L.Add(name+" "+lab+" ["+approach+"] no hit");continue;}int sub=SubmeshOf(bs.baked,bh.triangleIndex);var mats=bs.r.sharedMaterials;var mat=sub<mats.Length?mats[sub]:null;var tex=mat?ReadableTex(mat.mainTexture):null;string col="n/a";if(tex){var c=tex.GetPixelBilinear(bh.textureCoord.x,bh.textureCoord.y);col="albedo=("+c.r.ToString("F2")+","+c.g.ToString("F2")+","+c.b.ToString("F2")+")";}
    L.Add(name+" "+lab+" ["+approach+"] hit="+bs.r.name+" sub="+sub+" mat="+(mat?mat.name:"null")+" "+col+(mat&&mat.HasProperty("_Color")?" tint="+mat.color.ToString("F2"):""));}}
  var focus=Vector3.Lerp(lo.position,foot.position,.4f);
  foreach(var (view,pos) in new[]{("front",focus+character.forward*.8f+Vector3.up*.05f),("back",focus-character.forward*.8f+Vector3.up*.05f)}){var cam=new GameObject("Mini179LegCam").AddComponent<Camera>();cam.nearClipPlane=.01f;cam.farClipPlane=50;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.38f,.42f,.45f);cam.fieldOfView=45;cam.transform.position=pos;cam.transform.LookAt(focus);
   var rt=new RenderTexture(1000,800,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var t2=new Texture2D(1000,800,TextureFormat.RGB24,false);t2.ReadPixels(new Rect(0,0,1000,800),0,0);t2.Apply();File.WriteAllBytes(Out+"/Renders/"+name+"-legs-"+view+"-"+label+".png",t2.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(t2);UnityEngine.Object.DestroyImmediate(cam.gameObject);}}
 const string MatDir="Assets/UpIzUpMini/Art/Characters/Garments/Outfits166";
 static void Start(string mode){Directory.CreateDirectory(Out+"/Renders");SessionState.SetString(Key,mode);EditorSceneManager.OpenScene(Scene);EditorApplication.playModeStateChanged+=Changed;EditorApplication.EnterPlaymode();}
 static void Changed(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode){next=EditorApplication.timeSinceStartup+3;EditorApplication.update+=Run;}
  if(s==PlayModeStateChange.EnteredEditMode){var mode=SessionState.GetString(Key,"");SessionState.SetString(Key,"");if(mode=="Fit"&&!failed){try{AssignArmSkin();}catch(Exception e){failed=true;Debug.LogException(e);}}EditorApplication.Exit(failed?1:0);}}
 // Edit mode: point every shirt piece's skin slot (materials[1]) at the new arm-only material, expansion scene only.
 static void AssignArmSkin(){var scene=EditorSceneManager.OpenScene(Scene);var lines=new List<string>();
  foreach(var name in new[]{"Sacat","Franki"}){var arm=AssetDatabase.LoadAssetAtPath<Material>(MatDir+"/"+name+"_ArmSkin.mat");if(!arm)throw new Exception(name+"_ArmSkin.mat missing");
   var w=UnityEngine.Object.FindObjectsByType<OutfitWardrobe>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(x=>x.name==name);if(!w)throw new Exception("OutfitWardrobe missing on "+name);
   int n=0;foreach(var p in w.pieces){if(p==null||p.slot!=OutfitSlot.Shirt||p.materials==null||p.materials.Length<2)continue;var copy=(Material[])p.materials.Clone();lines.Add(name+" "+p.id+" materials[1]: "+(copy[1]?copy[1].name:"null")+" -> "+arm.name);copy[1]=arm;p.materials=copy;n++;}
   if(n==0)throw new Exception("No shirt pieces changed on "+name);EditorUtility.SetDirty(w);}
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllLines(Out+"/Assign.txt",lines);Debug.Log("MINI179_ASSIGN_PASS");}
 // Play Mode: dense sample of the real hand skin (WardrobeBody only) -> mean effective colour, then write the arm material.
 static void FitMaterial(string name,Transform character,Animator a,List<Surf> surfs,List<string> L){
  var hands=new[]{a.GetBoneTransform(HumanBodyBones.LeftHand),a.GetBoneTransform(HumanBodyBones.RightHand)};var chest=a.GetBoneTransform(HumanBodyBones.Chest).position;
  Color sum=Color.black;int count=0;float gloss=.4f,metal=0f;Material handMat=null;
  foreach(var hand in hands){var lowerArm=hand.parent;var dir=(hand.position-lowerArm.position).normalized;var centre=hand.position+dir*.05f;
   foreach(var vec in new[]{Vector3.ProjectOnPlane(centre-chest,Vector3.up).normalized,character.forward,-character.forward,Vector3.up,Vector3.down}){
    var u=Vector3.Cross(vec,Vector3.up);if(u.sqrMagnitude<.01f)u=Vector3.Cross(vec,character.forward);u.Normalize();var v=Vector3.Cross(vec,u).normalized;
    for(int i=-2;i<=2;i++)for(int j=-2;j<=2;j++){var p=centre+u*i*.02f+v*j*.02f;var ray=new Ray(p+vec*.4f,-vec);float best=float.PositiveInfinity;Surf bs=null;RaycastHit bh=default;
     foreach(var s in surfs)if(s.col.Raycast(ray,out var h,1f)&&h.distance<best){best=h.distance;bs=s;bh=h;}
     if(bs==null||bs.r.name!="WardrobeBody"||Vector3.Distance(bh.point,hand.position)>.12f)continue;
     int sub=SubmeshOf(bs.baked,bh.triangleIndex);var mats=bs.r.sharedMaterials;if(sub>=mats.Length||!mats[sub])continue;var m=mats[sub];if(sub!=0)continue; // sub 0 is the skin material
     var tex=ReadableTex(m.mainTexture);if(!tex)continue;var c=tex.GetPixelBilinear(bh.textureCoord.x,bh.textureCoord.y);var tint=m.HasProperty("_Color")?m.color:Color.white;
     sum+=new Color(c.r*tint.r,c.g*tint.g,c.b*tint.b,1);count++;handMat=m;}}}
  if(count<20)throw new Exception(name+": too few hand samples ("+count+")");
  var mean=sum/count;mean.a=1;if(handMat){if(handMat.HasProperty("_Glossiness"))gloss=handMat.GetFloat("_Glossiness");if(handMat.HasProperty("_Metallic"))metal=handMat.GetFloat("_Metallic");}
  string path=MatDir+"/"+name+"_ArmSkin.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,path);}
  mat.color=mean;mat.SetFloat("_Glossiness",gloss);mat.SetFloat("_Metallic",metal);mat.enableInstancing=true;EditorUtility.SetDirty(mat);AssetDatabase.SaveAssets();
  L.Add(name+" hand samples="+count+" mean effective albedo="+mean.ToString("F3")+" gloss="+gloss.ToString("F3")+" metallic="+metal+" hand material="+(handMat?handMat.name:"?")+" -> "+path);}

 static readonly Dictionary<string,Texture2D> texCache=new Dictionary<string,Texture2D>();
 static Texture2D ReadableTex(Texture t){if(!t)return null;var path=AssetDatabase.GetAssetPath(t);if(string.IsNullOrEmpty(path))return null;if(texCache.TryGetValue(path,out var c))return c;Texture2D r=null;try{var bytes=File.ReadAllBytes(path);r=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!r.LoadImage(bytes)){r=null;}}catch{r=null;}texCache[path]=r;return r;}
 class Surf{public SkinnedMeshRenderer r;public Mesh baked;public MeshCollider col;}
 static void Run(){if(EditorApplication.timeSinceStartup<next)return;EditorApplication.update-=Run;
  var baked=new List<Surf>();
  try{var L=new List<string>();
   foreach(var name in new[]{"Sacat","Franki"}){
    var eq=UnityEngine.Object.FindObjectsByType<CharacterEquipment>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(e=>e.name==name);if(!eq)throw new Exception("Missing "+name);eq.gameObject.SetActive(true);
    var a=eq.GetComponentInChildren<Animator>();L.Add("== "+name+" ==");
    // 1) inventory of every skinned renderer: mesh, materials, textures, tint
    foreach(var r in eq.GetComponentsInChildren<SkinnedMeshRenderer>(true)){var mats=r.sharedMaterials;L.Add("renderer "+r.name+" enabled="+r.enabled+" active="+r.gameObject.activeInHierarchy+" verts="+(r.sharedMesh?r.sharedMesh.vertexCount:0)+" submeshes="+(r.sharedMesh?r.sharedMesh.subMeshCount:0));
     for(int i=0;i<mats.Length;i++){var m=mats[i];if(!m){L.Add("   mat["+i+"]=null");continue;}var t=m.mainTexture;L.Add("   mat["+i+"]="+m.name+" shader="+m.shader.name+" tint="+(m.HasProperty("_Color")?m.color.ToString("F3"):"n/a")+" tex="+(t?AssetDatabase.GetAssetPath(t):"none"));}}
    // 2) raycast probes
    var surfs=new List<Surf>();foreach(var r in eq.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.sharedMesh)){var m=new Mesh();r.BakeMesh(m);var g=new GameObject("Mini179Surf");g.hideFlags=HideFlags.HideAndDontSave;g.transform.SetPositionAndRotation(r.transform.position,r.transform.rotation);g.transform.localScale=r.transform.lossyScale;var c=g.AddComponent<MeshCollider>();c.sharedMesh=m;surfs.Add(new Surf{r=r,baked=m,col=c});}
    baked.AddRange(surfs);Physics.SyncTransforms();
    if(SessionState.GetString(Key,"")=="Legs"){var w0=eq.GetComponent<OutfitWardrobe>();if(w0)w0.Select("pants_shorts_denim",5);Physics.SyncTransforms();foreach(var s in surfs){UnityEngine.Object.DestroyImmediate(s.col.gameObject);UnityEngine.Object.DestroyImmediate(s.baked);}surfs.Clear();baked.Clear();
     // rebuild surfaces AFTER the shorts are on, so probes hit the outfit that is actually worn
     foreach(var r in eq.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.sharedMesh)){var m=new Mesh();r.BakeMesh(m);var g=new GameObject("Mini179Surf");g.hideFlags=HideFlags.HideAndDontSave;g.transform.SetPositionAndRotation(r.transform.position,r.transform.rotation);g.transform.localScale=r.transform.lossyScale;var c=g.AddComponent<MeshCollider>();c.sharedMesh=m;surfs.Add(new Surf{r=r,baked=m,col=c});}
     baked.AddRange(surfs);Physics.SyncTransforms();LegsShots(name,eq.transform,a,surfs,L,SessionState.GetString(Key+"Label","x"));foreach(var s in surfs){UnityEngine.Object.DestroyImmediate(s.col.gameObject);UnityEngine.Object.DestroyImmediate(s.baked);}baked.Clear();continue;}
    if(SessionState.GetString(Key,"")=="Fit"){FitMaterial(name,eq.transform,a,surfs,L);foreach(var s in surfs){UnityEngine.Object.DestroyImmediate(s.col.gameObject);UnityEngine.Object.DestroyImmediate(s.baked);}baked.Clear();continue;}
    var chest=a.GetBoneTransform(HumanBodyBones.Chest).position;
    foreach(var side in new[]{"Left","Right"}){
     var lo=a.GetBoneTransform(side=="Left"?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);var hand=a.GetBoneTransform(side=="Left"?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);var up=a.GetBoneTransform(side=="Left"?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);
     var dir=(hand.position-lo.position).normalized;
     var pts=new List<(string,Vector3)>{("upperarm-mid",Vector3.Lerp(up.position,lo.position,.5f)),("forearm-25%",Vector3.Lerp(lo.position,hand.position,.25f)),("forearm-50%",Vector3.Lerp(lo.position,hand.position,.5f)),("forearm-75%",Vector3.Lerp(lo.position,hand.position,.75f)),("wrist",hand.position-dir*.02f),("hand",hand.position+dir*.05f),("fingers",hand.position+dir*.11f)};
     foreach(var (label,p) in pts){
      // approach from outside (away from the chest, horizontally), and also from the front/back to catch thin parts
      foreach(var (approach,vec) in new[]{("out",Vector3.ProjectOnPlane(p-chest,Vector3.up).normalized),("front",eq.transform.forward),("back",-eq.transform.forward)}){
       var ray=new Ray(p+vec*.5f,-vec);float best=float.PositiveInfinity;Surf bs=null;RaycastHit bh=default;
       foreach(var s in surfs)if(s.col.Raycast(ray,out var h,1.0f)&&h.distance<best){best=h.distance;bs=s;bh=h;}
       if(bs==null){L.Add(side+" "+label+" ["+approach+"] no hit");continue;}
       int sub=SubmeshOf(bs.baked,bh.triangleIndex);var mats=bs.r.sharedMaterials;var mat=sub<mats.Length?mats[sub]:null;var tex=mat?ReadableTex(mat.mainTexture):null;
       string col="n/a";if(tex){var c=tex.GetPixelBilinear(bh.textureCoord.x,bh.textureCoord.y);col="albedo=("+c.r.ToString("F2")+","+c.g.ToString("F2")+","+c.b.ToString("F2")+")";}
       string tint=mat&&mat.HasProperty("_Color")?" tint="+mat.color.ToString("F2"):"";
       L.Add(side+" "+label+" ["+approach+"] hit="+bs.r.name+" sub="+sub+" mat="+(mat?mat.name:"null")+" uv=("+bh.textureCoord.x.ToString("F3")+","+bh.textureCoord.y.ToString("F3")+") "+col+tint);
      }}}
    // 3) close-up renders of each forearm+hand from outside
    foreach(var side in new[]{"Left","Right"}){var lo=a.GetBoneTransform(side=="Left"?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);var hand=a.GetBoneTransform(side=="Left"?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
     var focus=Vector3.Lerp(lo.position,hand.position,.75f);var outDir=Vector3.ProjectOnPlane(focus-chest,Vector3.up).normalized;
     var cam=new GameObject("Mini179Cam").AddComponent<Camera>();cam.nearClipPlane=.01f;cam.farClipPlane=50;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.38f,.42f,.45f);cam.fieldOfView=30;cam.transform.position=focus+outDir*.75f+Vector3.up*.05f;cam.transform.LookAt(focus);
     var rt=new RenderTexture(1000,800,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex2=new Texture2D(1000,800,TextureFormat.RGB24,false);tex2.ReadPixels(new Rect(0,0,1000,800),0,0);tex2.Apply();File.WriteAllBytes(Out+"/Renders/"+name+"-"+side+"-arm-hand.png",tex2.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex2);UnityEngine.Object.DestroyImmediate(cam.gameObject);}
    foreach(var s in surfs){UnityEngine.Object.DestroyImmediate(s.col.gameObject);UnityEngine.Object.DestroyImmediate(s.baked);}baked.Clear();
   }
   if(SessionState.GetString(Key,"")=="Legs"){File.WriteAllLines(Out+"/Legs-"+SessionState.GetString(Key+"Label","x")+".txt",L.Where(x=>!x.StartsWith("renderer")&&!x.StartsWith("   mat")).ToList());Debug.Log("MINI179_LEGS_PASS");}else if(SessionState.GetString(Key,"")=="Fit"){File.WriteAllLines(Out+"/Fit.txt",L);Debug.Log("MINI179_FIT_PASS");}else{File.WriteAllLines(Out+"/Probe.txt",L);Debug.Log("MINI179_PROBE_PASS");}
  }catch(Exception e){failed=true;Debug.LogException(e);}finally{foreach(var s in baked){if(s.col)UnityEngine.Object.DestroyImmediate(s.col.gameObject);}EditorApplication.ExitPlaymode();}}
 static int SubmeshOf(Mesh m,int tri){int c=0;for(int i=0;i<m.subMeshCount;i++){int n=(int)m.GetIndexCount(i)/3;if(tri<c+n)return i;c+=n;}return 0;}
}}
