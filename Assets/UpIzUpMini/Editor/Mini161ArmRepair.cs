using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using Object=UnityEngine.Object;
namespace UpIzUpMini.EditorTools
{
 public static class Mini161ArmRepair
 {
  const string Scene="Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
  const string Art="Assets/UpIzUpMini/Art/Characters/Garments/Franki_ArmsRestored";
  const string Out="Logs/Tasks/MINI-161";
  public static void Preview(){Run(false);}
  public static void Integrate(){Run(true);}
  static void Check(bool ok,string message){if(!ok)throw new Exception("MINI161: "+message);}
  static void Run(bool save)
  {
   Directory.CreateDirectory(Out);AssetDatabase.Refresh();
   var mi=(ModelImporter)AssetImporter.GetAtPath(Art+".fbx");
   mi.animationType=ModelImporterAnimationType.Generic;mi.importAnimation=false;mi.SaveAndReimport();
   var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+".png");Check(tex!=null,"missing texture");
   var mat=AssetDatabase.LoadAssetAtPath<Material>(Art+".mat");
   if(mat==null){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,Art+".mat");}
   mat.mainTexture=tex;mat.color=Color.white;mat.SetFloat("_Glossiness",.15f);EditorUtility.SetDirty(mat);AssetDatabase.SaveAssets();
   EditorSceneManager.OpenScene(Scene);
   string before=File.ReadAllText(Scene);
   var franki=GameObject.Find("Franki");Check(franki!=null,"Franki missing");
   var protectedRenderers=Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)
    .Where(r=>!(r.name=="Ch28_Hoody"&&r.transform.IsChildOf(franki.transform)))
    .ToDictionary(r=>r,r=>Tuple.Create(r.sharedMesh,r.sharedMaterials,r.bones));
   // Reuse the established MINI159 mapping, but ONLY the affected shirt.
   typeof(Mini159IntegrateGarments).GetMethod("IntegrateCharacter",BindingFlags.NonPublic|BindingFlags.Static)
    .Invoke(null,new object[]{"Franki",Art+".fbx",new[]{"Ch28_Hoody"},new List<string>()});
   var shirt=franki.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r=>r.name=="Ch28_Hoody");shirt.sharedMaterial=mat;
   foreach(var p in protectedRenderers){Check(p.Key.sharedMesh==p.Value.Item1,"protected mesh changed");Check(p.Key.sharedMaterials.SequenceEqual(p.Value.Item2),"protected materials changed");Check(p.Key.bones.SequenceEqual(p.Value.Item3),"protected bones changed");}
   if(save)
   {
    string backup=Out+"/GrandBayProof-before-arms.unity";if(!File.Exists(backup))File.Copy(Scene,backup);
    EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    Debug.Log("MINI161_INTEGRATE_PASS only Franki shirt; protected renderer references unchanged");return;
   }
   CaptureBoth();Check(File.ReadAllText(Scene)==before,"preview saved live scene");Debug.Log("MINI161_PREVIEW_PASS");
  }
  static void CaptureBoth()
  {
   foreach(var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))l.enabled=false;
   RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.45f,.45f,.45f);RenderSettings.fog=false;
   var key=new GameObject("arm proof key").AddComponent<Light>();key.type=LightType.Directional;key.intensity=.9f;key.transform.rotation=Quaternion.Euler(25,-35,0);
   var cam=new GameObject("arm proof camera").AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.5f,.03f,.35f);cam.nearClipPlane=.01f;cam.farClipPlane=20;cam.fieldOfView=35;
   var log=new List<string>();
   foreach(var name in new[]{"Franki","Sacat"})
   {
    var root=GameObject.Find(name);var copy=Object.Instantiate(root);copy.name=name+"_isolated";copy.SetActive(true);copy.transform.position=new Vector3(0,500,0);copy.transform.rotation=Quaternion.identity;
    foreach(var b in copy.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;
    foreach(var r in copy.GetComponentsInChildren<Renderer>(true))r.enabled=r is SkinnedMeshRenderer&&(name=="Sacat"?r.name=="Ch06":r.name=="Ch28_Hoody"||r.name=="Ch28_Body");
    var anim=copy.GetComponentInChildren<Animator>(true);anim.enabled=true;anim.cullingMode=AnimatorCullingMode.AlwaysAnimate;anim.applyRootMotion=false;
    foreach(var motion in new[]{"Stand--Idle","Locomotion--Walk_N","Locomotion--Run_N"})
    {
     var clip=AssetDatabase.LoadAllAssetsAtPath("Assets/UpIzUpMini/Art/Animations/"+motion+".anim.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
     for(int f=0;f<3;f++)
     {
      var graph=PlayableGraph.Create("Arms");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
      var p=AnimationClipPlayable.Create(graph,clip);var output=AnimationPlayableOutput.Create(graph,"Pose",anim);output.SetSourcePlayable(p);graph.Play();p.SetTime(clip.length*(f+.2)/3);graph.Evaluate(0);graph.Destroy();
      foreach(bool left in new[]{true,false})
      {
       var shoulder=anim.GetBoneTransform(left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);
       var elbow=anim.GetBoneTransform(left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);
       var hand=anim.GetBoneTransform(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
       Vector3 center=(shoulder.position+elbow.position+hand.position)/3;
       string id=name+"-"+motion+"-"+f+"-"+(left?"Left":"Right");
       Capture(cam,center+new Vector3(left?-.9f:.9f,.05f,-1.15f),center,Out+"/"+id+".png");
       if(f==0)Capture(cam,center+new Vector3(left?-1.5f:1.5f,.1f,-2.3f),center,Out+"/"+id+"-Far.png");
       log.Add(id+" shoulder="+shoulder.position+" elbow="+elbow.position+" hand="+hand.position);
      }
     }
    }
    Object.DestroyImmediate(copy);
   }
   File.WriteAllLines(Out+"/motion-samples.txt",log);
  }
  static void Capture(Camera cam,Vector3 pos,Vector3 target,string path)
  {
   cam.transform.position=pos;cam.transform.LookAt(target);
   var rt=new RenderTexture(640,640,24);cam.targetTexture=rt;var previous=RenderTexture.active;cam.Render();RenderTexture.active=rt;
   var tex=new Texture2D(640,640,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,640,640),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());
   cam.targetTexture=null;RenderTexture.active=previous;Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);
  }
 }
}
