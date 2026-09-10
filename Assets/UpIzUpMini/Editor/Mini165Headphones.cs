using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;
namespace UpIzUpMini.EditorTools {
 public static class Mini165Headphones {
  const string Scene="Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
  const string Out="Logs/Tasks/MINI-165";
  const string Dir="Assets/UpIzUpMini/Art/Characters/Garments/Headphones";
  static void Require(bool ok,string why){if(!ok)throw new Exception("MINI165: "+why);}
  public static void Preview(){Run(false);}
  public static void Integrate(){Run(true);}
  public static void Evidence(){EditorSceneManager.OpenScene(Scene);Capture();Debug.Log("MINI165_SAVED_EVIDENCE_PASS");EditorApplication.Exit(0);}
  static void Run(bool save){
   Directory.CreateDirectory(Out);Directory.CreateDirectory(Dir);
   if(!File.Exists(Out+"/GrandBayProof-before-headphones.unity"))File.Copy(Scene,Out+"/GrandBayProof-before-headphones.unity");
   EditorSceneManager.OpenScene(Scene);var root=GameObject.Find("Sacat");var r=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(x=>x.name=="Ch06");
   string baseline=Dir+"/SacatWithHeadphones.asset";var m=AssetDatabase.LoadAssetAtPath<Mesh>(baseline);
   if(m==null){m=UnityEngine.Object.Instantiate(r.sharedMesh);AssetDatabase.CreateAsset(m,baseline);}
   var v=m.vertices.Select(x=>root.transform.InverseTransformPoint(r.transform.TransformPoint(x))).ToArray();var tris=m.GetTriangles(0);var parent=Enumerable.Range(0,v.Length).ToArray();var weld=new Dictionary<Vector3Int,int>();
   int Find(int a){while(parent[a]!=a){parent[a]=parent[parent[a]];a=parent[a];}return a;}
   void Union(int a,int b){parent[Find(a)]=Find(b);}
   foreach(int k in tris){var p=v[k];var key=new Vector3Int(Mathf.RoundToInt(p.x*100000),Mathf.RoundToInt(p.y*100000),Mathf.RoundToInt(p.z*100000));if(weld.TryGetValue(key,out int other))Union(k,other);else weld[key]=k;}
   for(int i=0;i<tris.Length;i+=3){Union(tris[i],tris[i+1]);Union(tris[i],tris[i+2]);}
   var groups=new Dictionary<int,List<int>>();for(int i=0;i<tris.Length;i+=3){int key=Find(tris[i]);if(!groups.ContainsKey(key))groups[key]=new List<int>();groups[key].AddRange(new[]{tris[i],tris[i+1],tris[i+2]});}
   var chosen=new List<int>();var retained=new List<int>();int pieces=0;
   foreach(var g in groups.Values){var ids=g.Distinct().ToArray();var min=new Vector3(ids.Min(k=>v[k].x),ids.Min(k=>v[k].y),ids.Min(k=>v[k].z));var max=new Vector3(ids.Max(k=>v[k].x),ids.Max(k=>v[k].y),ids.Max(k=>v[k].z));
    bool isHeadphone=min.y>1.53f && (min.x>.06f || max.x<-.06f || max.y>1.68f);
    if(isHeadphone){chosen.AddRange(g);pieces++;}else retained.AddRange(g);
   }
   Require(pieces==6 && chosen.Count/3==6760,"source geometry changed: re-audit headphone components");
   var body=UnityEngine.Object.Instantiate(m);body.name="Sacat Waves Without Headphones";body.SetTriangles(retained,0);
   Require(body.vertices.SequenceEqual(m.vertices),"body vertices changed");for(int s=1;s<m.subMeshCount;s++)Require(body.GetTriangles(s).SequenceEqual(m.GetTriangles(s)),"hair or other submesh changed");
   var used=chosen.Distinct().ToArray();var map=new Dictionary<int,int>();for(int i=0;i<used.Length;i++)map[used[i]]=i;
   var accessory=new Mesh{name="Sacat Original Headphones"};var verts=m.vertices;var normal=m.normals;var uv=m.uv;var tangent=m.tangents;var weights=m.boneWeights;
   accessory.vertices=used.Select(k=>verts[k]).ToArray();accessory.normals=used.Select(k=>normal[k]).ToArray();accessory.uv=used.Select(k=>uv[k]).ToArray();accessory.tangents=used.Select(k=>tangent[k]).ToArray();accessory.boneWeights=used.Select(k=>weights[k]).ToArray();accessory.bindposes=m.bindposes;accessory.triangles=chosen.Select(k=>map[k]).ToArray();accessory.RecalculateBounds();
   if(save){body=Store(body,Dir+"/SacatWithoutHeadphones.asset");accessory=Store(accessory,Dir+"/SacatHeadphones.asset");}
   r.sharedMesh=body;
   var existing=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="HeadphonesAccessory");var go=existing!=null?existing.gameObject:new GameObject("HeadphonesAccessory");go.transform.SetParent(r.transform.parent,false);go.transform.localPosition=r.transform.localPosition;go.transform.localRotation=r.transform.localRotation;go.transform.localScale=r.transform.localScale;
   var hr=go.GetComponent<SkinnedMeshRenderer>();if(hr==null)hr=go.AddComponent<SkinnedMeshRenderer>();hr.sharedMesh=accessory;hr.sharedMaterial=r.sharedMaterials[0];hr.bones=r.bones;hr.rootBone=r.rootBone;hr.localBounds=accessory.bounds;hr.shadowCastingMode=r.shadowCastingMode;hr.receiveShadows=r.receiveShadows;
   var equipment=root.GetComponent<CharacterEquipment>();var so=new SerializedObject(equipment);so.FindProperty("headphonesAccessory").objectReferenceValue=go;so.ApplyModifiedPropertiesWithoutUndo();
   AssetDatabase.SaveAssets();if(save){EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());Debug.Log("MINI165_INTEGRATED: six pieces / 6760 triangles, original positions/UVs/bones retained.");}else{Capture();Debug.Log("MINI165_PREVIEW_PASS scene not saved");}EditorApplication.Exit(0);
  }
  static Mesh Store(Mesh mesh,string path){var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}Require(old.vertexCount==mesh.vertexCount,"existing output differs: inspect before overwrite");return old;}
  static void Capture(){
   foreach(var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))l.enabled=false;
   RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.55f,.55f,.55f);RenderSettings.fog=false;RenderSettings.skybox=null;
   var key=new GameObject("Headphone proof key").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.2f;key.transform.rotation=Quaternion.Euler(35,-35,0);
   var root=GameObject.Find("Sacat");root.SetActive(true);root.transform.position=new Vector3(0,300,0);root.transform.rotation=Quaternion.identity;foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))skin.updateWhenOffscreen=true;
   var a=root.GetComponentInChildren<Animator>();a.Rebind();a.Update(0);var focus=a.GetBoneTransform(HumanBodyBones.Head).position+Vector3.up*.08f;var eq=root.GetComponent<CharacterEquipment>();
   var camera=new GameObject("Headphone proof camera").AddComponent<Camera>();camera.fieldOfView=30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.35f,.39f,.43f);camera.nearClipPlane=.01f;camera.farClipPlane=100;
   foreach(bool wear in new[]{true,false}){Require(eq.SetHeadphonesEquipped(wear),"toggle rejected");foreach(string view in new[]{"Front","Side","Crown","Full"}){
    Vector3 target=focus;Vector3 delta=view=="Side"?new Vector3(.65f,.08f,.15f):view=="Crown"?new Vector3(.30f,.44f,.44f):new Vector3(0,.04f,.70f);if(view=="Full"){target=root.transform.position+Vector3.up*.95f;delta=new Vector3(0,.3f,4);}
    camera.transform.position=target+delta;camera.transform.LookAt(target);var rt=new RenderTexture(800,800,24);camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;var t=new Texture2D(800,800,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,800,800),0,0);t.Apply();File.WriteAllBytes(Out+"/Sacat-Headphones-"+(wear?"On":"Off")+"-"+view+".png",t.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(t);
   }}
  }
 }
}
