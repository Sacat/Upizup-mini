using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UpIzUpMini.Character;
namespace UpIzUpMini.EditorTools {
public static partial class Mini166Repair {
 public static void ShoulderBaseline(){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(Scene);RenderAll();Debug.Log("MINI166_SHOULDER_BASELINE_PASS");EditorApplication.Exit(0);}
 static void RenderAll(){
  RenderSettings.fog=false;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.55f,.55f,.55f);
  foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))light.enabled=false;
  var key=new GameObject("Proof Key").AddComponent<Light>();key.type=LightType.Directional;key.intensity=.85f;key.transform.rotation=Quaternion.Euler(35,-35,0);key.cullingMask=1<<31;
  var fill=new GameObject("Proof Fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.3f;fill.transform.rotation=Quaternion.Euler(25,150,0);fill.cullingMask=1<<31;
  var camera=new GameObject("Proof Camera").AddComponent<Camera>();camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.23f,.26f,.29f);camera.orthographic=true;camera.orthographicSize=.96f;camera.nearClipPlane=.01f;camera.farClipPlane=15;
  var idle=AssetDatabase.LoadAllAssetsAtPath("Assets/UpIzUpMini/Art/Animations/Stand--Idle.anim.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
  foreach(string who in new[]{"Franki","Sacat"}){
   var root=GameObject.Find(who);root.SetActive(true);var originalPos=root.transform.position;var originalRot=root.transform.rotation;root.transform.SetPositionAndRotation(new Vector3(0,300,0),Quaternion.identity);
   foreach(var r in root.GetComponentsInChildren<Renderer>(true))r.gameObject.layer=31;
   var animator=root.GetComponentInChildren<Animator>(true);animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.applyRootMotion=false;animator.Rebind();
   var graph=PlayableGraph.Create("Wardrobe proof");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var p=AnimationClipPlayable.Create(graph,idle);var output=AnimationPlayableOutput.Create(graph,"pose",animator);output.SetSourcePlayable(p);graph.Play();p.SetTime(.7);graph.Evaluate(.01f);
   var wardrobe=root.GetComponent<OutfitWardrobe>();wardrobe.Restore(null);
   Capture(camera,root,new Vector3(0,.9f,4),new Vector3(0,.9f,0),who+"-Individual-Default");
   wardrobe.Select("shirt_tee_mike",1);wardrobe.Select("pants_jeans",5);
   camera.orthographicSize=.36f;Capture(camera,root,new Vector3(0,1.4f,4),new Vector3(0,1.4f,0),who+"-Shoulders-Front");Capture(camera,root,new Vector3(2,1.5f,-3),new Vector3(0,1.4f,0),who+"-Shoulders-Back");camera.orthographicSize=.96f;
   Capture(camera,root,new Vector3(0,.9f,4),new Vector3(0,.9f,0),who+"-Tee-Jeans-Front");
   wardrobe.Select("shirt_polo_lacos",0);wardrobe.Select("pants_trousers",2);wardrobe.Select("shoes_mike97",1);wardrobe.Select("hat_lacos",4);
   Capture(camera,root,new Vector3(0,.9f,4),new Vector3(0,.9f,0),who+"-Polo-Trousers-Cap-Front");
   Capture(camera,root,new Vector3(3,.9f,2),new Vector3(0,.9f,0),who+"-Polo-Trousers-Cap-Side");
   wardrobe.Select("pants_shorts_denim",5);wardrobe.Select("hat_none",4);wardrobe.Select("shirt_tee_mike",1);
   Capture(camera,root,new Vector3(0,.9f,4),new Vector3(0,.9f,0),who+"-Shorts-Front");
   Capture(camera,root,new Vector3(0,.9f,-4),new Vector3(0,.9f,0),who+"-Shorts-Back");
   camera.orthographicSize=.26f;Capture(camera,root,new Vector3(2,.2f,2),new Vector3(0,.15f,0),who+"-Mike97-Feet");wardrobe.Select("shoes_mike90",2);Capture(camera,root,new Vector3(2,.2f,2),new Vector3(0,.15f,0),who+"-Mike90-Feet");camera.orthographicSize=.96f;
   graph.Destroy();root.transform.SetPositionAndRotation(originalPos,originalRot);foreach(var r in root.GetComponentsInChildren<Renderer>(true))r.gameObject.layer=0;
  }
 }
 static void Capture(Camera camera,GameObject root,Vector3 offset,Vector3 target,string name){
  // Bake the selected meshes immediately. Editor Camera.Render does not advance
  // the skinner between same-frame mesh swaps; static copies avoid stale GPU data.
  var bakedObjects=new System.Collections.Generic.List<GameObject>();var visible=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.sharedMesh).ToArray();
  foreach(var r in visible){var mesh=WardrobePreviewMesh.Bake(r);var go=new GameObject("Baked proof");go.layer=31;go.transform.SetParent(r.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterials=r.sharedMaterials;var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);mr.SetPropertyBlock(block);for(int i=0;i<r.sharedMaterials.Length;i++){block.Clear();r.GetPropertyBlock(block,i);mr.SetPropertyBlock(block,i);}bakedObjects.Add(go);r.enabled=false;}
  camera.transform.position=root.transform.position+offset;camera.transform.LookAt(root.transform.position+target);var rt=new RenderTexture(640,800,24){antiAliasing=4};camera.targetTexture=rt;camera.Render();camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(640,800,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,640,800),0,0);tex.Apply();File.WriteAllBytes(Out+"/"+name+".png",tex.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);
  foreach(var go in bakedObjects){UnityEngine.Object.DestroyImmediate(go.GetComponent<MeshFilter>().sharedMesh);UnityEngine.Object.DestroyImmediate(go);}foreach(var r in visible)r.enabled=true;
 }
}
}
