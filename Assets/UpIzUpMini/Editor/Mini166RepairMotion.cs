using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UpIzUpMini.Character;
namespace UpIzUpMini.EditorTools {
public static partial class Mini166Repair {
 public static void Motion(){try{
  EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory(Out+"/Motion");RenderSettings.fog=false;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.55f,.55f,.55f);
  foreach(var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))l.enabled=false;
  var key=new GameObject("motion light").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1;key.transform.rotation=Quaternion.Euler(35,-35,0);key.cullingMask=1<<31;
  var camera=new GameObject("motion camera").AddComponent<Camera>();camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.23f,.26f,.29f);camera.orthographic=true;camera.orthographicSize=.99f;camera.nearClipPlane=.01f;camera.farClipPlane=15;
  string[] clipNames={"Locomotion--Walk_N.anim.fbx","Locomotion--Run_N.anim.fbx","Moto/AA_MOTO_Idle.fbx","Stand--Idle.anim.fbx"};string[] labels={"Walk","Run","MotoIdle","KneeFlex"};
  foreach(string who in new[]{"Franki","Sacat"}){
   var root=GameObject.Find(who);root.SetActive(true);var pos=root.transform.position;var rotation=root.transform.rotation;root.transform.SetPositionAndRotation(new Vector3(0,300,0),Quaternion.identity);foreach(var r in root.GetComponentsInChildren<Renderer>(true))r.gameObject.layer=31;
   var animator=root.GetComponentInChildren<Animator>(true);animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;var w=root.GetComponent<OutfitWardrobe>();
   for(int outfit=0;outfit<3;outfit++){
    w.Restore(null);w.Select("shirt_tee_mike",1);if(outfit==1){w.Select("shirt_polo_lacos",0);w.Select("pants_trousers",2);w.Select("shoes_mike97",1);w.Select("hat_lacos",4);}if(outfit==2)w.Select("pants_shorts_denim",5);
    for(int clipIndex=0;clipIndex<clipNames.Length;clipIndex++){
     var clip=AssetDatabase.LoadAllAssetsAtPath("Assets/UpIzUpMini/Art/Animations/"+clipNames[clipIndex]).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));animator.Rebind();
     var graph=PlayableGraph.Create("Wardrobe motion");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var playable=AnimationClipPlayable.Create(graph,clip);var output=AnimationPlayableOutput.Create(graph,"animation",animator);output.SetSourcePlayable(playable);graph.Play();
     for(int frame=0;frame<18;frame++){playable.SetTime((frame/15.0)%clip.length);graph.Evaluate(0);
      if(clipIndex==3){float flex=Mathf.Sin(frame/17f*Mathf.PI);animator.GetBoneTransform(HumanBodyBones.Hips).position+=Vector3.down*.27f*flex;foreach(var bone in new[]{HumanBodyBones.LeftUpperLeg,HumanBodyBones.RightUpperLeg}){var t=animator.GetBoneTransform(bone);t.rotation=Quaternion.AngleAxis(-55*flex,root.transform.right)*t.rotation;}foreach(var bone in new[]{HumanBodyBones.LeftLowerLeg,HumanBodyBones.RightLowerLeg}){var t=animator.GetBoneTransform(bone);t.rotation=Quaternion.AngleAxis(100*flex,root.transform.right)*t.rotation;}foreach(var bone in new[]{HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot}){var t=animator.GetBoneTransform(bone);t.rotation=Quaternion.AngleAxis(-45*flex,root.transform.right)*t.rotation;}}
      foreach(var binding in w.bindings.Where(b=>b.renderer.enabled)){var mesh=WardrobePreviewMesh.Bake(binding.renderer);if(mesh.bounds.size.magnitude>3f||mesh.vertices.Any(v=>float.IsNaN(v.x)||float.IsInfinity(v.y)))throw new Exception("Motion bounds "+who+" "+binding.slot);UnityEngine.Object.DestroyImmediate(mesh);}Capture(camera,root,new Vector3(2,.95f,3),new Vector3(0,.9f,0),"Motion/"+who+"-"+outfit+"-"+labels[clipIndex]+"-"+frame.ToString("D3"));}
     graph.Destroy();Debug.Log("MINI166_MOTION_SEQUENCE "+who+" outfit="+outfit+" clip="+labels[clipIndex]);
    }
   }
   root.transform.SetPositionAndRotation(pos,rotation);foreach(var r in root.GetComponentsInChildren<Renderer>(true))r.gameObject.layer=0;
  }
  Debug.Log("MINI166_REPAIR_MOTION_PASS");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
}
