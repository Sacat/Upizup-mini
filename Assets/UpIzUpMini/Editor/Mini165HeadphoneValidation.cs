using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.UI;
namespace UpIzUpMini.EditorTools {
 [InitializeOnLoad] public static class Mini165HeadphoneValidation {
  static Mini165HeadphoneValidation(){if(SessionState.GetBool("Mini165Testing",false))EditorApplication.playModeStateChanged+=Changed;}
  static double next;
  static void Require(bool ok,string why){if(!ok)throw new Exception("MINI165: "+why);}
  public static void Run(){SessionState.SetBool("Mini165Testing",true);EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity");EditorApplication.playModeStateChanged+=Changed;EditorApplication.EnterPlaymode();}
  static void Changed(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode){next=EditorApplication.timeSinceStartup+3;EditorApplication.update+=Check;}if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool("Mini165Testing",false);EditorApplication.playModeStateChanged-=Changed;Mini001Build.BuildWindowsPlayer();}}
  static void Check(){if(EditorApplication.timeSinceStartup<next)return;EditorApplication.update-=Check;try{
   var all=Resources.FindObjectsOfTypeAll<CharacterEquipment>().Where(e=>e.gameObject.scene.IsValid()).ToArray();var eq=all.First(e=>e.name=="Sacat");var franki=all.First(e=>e.name=="Franki");
   Require(eq.HeadphonesAvailable&&!franki.HeadphonesAvailable,"accessory assigned to wrong character");
   var accessory=eq.GetComponentsInChildren<Transform>(true).First(t=>t.name=="HeadphonesAccessory").gameObject;var body=eq.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r=>r.name=="Ch06");var mesh=body.sharedMesh;
   eq.RestoreWardrobe(null);Require(eq.HeadphonesEquipped&&accessory.activeSelf,"old-save default changed");
   for(int i=0;i<3;i++){Require(eq.SetHeadphonesEquipped(false)&&!accessory.activeSelf,"remove failed");Require(body.sharedMesh==mesh&&body.enabled&&body.sharedMaterials.Last().mainTexture!=null,"hair/body changed");Require(eq.SetHeadphonesEquipped(true)&&accessory.activeSelf,"wear failed");}
   eq.SetHeadphonesEquipped(false);var saved=JsonUtility.ToJson(new GameSave{sacatUnequippedItems=eq.CaptureWardrobe()});eq.SetHeadphonesEquipped(true);eq.RestoreWardrobe(JsonUtility.FromJson<GameSave>(saved).sacatUnequippedItems);Require(!eq.HeadphonesEquipped&&!accessory.activeSelf,"save-data roundtrip failed");
   Time.timeScale=1;VisualWardrobePanel.Open(eq);Require(VisualWardrobePanel.IsOpen,"wardrobe did not open");var panel=UnityEngine.Object.FindFirstObjectByType<VisualWardrobePanel>();eq.SetHeadphonesEquipped(true);typeof(VisualWardrobePanel).GetMethod("RebuildPreview",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(panel,null);panel.Close(false);Require(!eq.HeadphonesEquipped&&!accessory.activeSelf,"Cancel did not restore opening outfit");
   VisualWardrobePanel.Open(eq);eq.SetHeadphonesEquipped(true);panel.Close(true);Require(eq.HeadphonesEquipped&&accessory.activeSelf,"Apply did not retain selection");
   Require(!franki.SetHeadphonesEquipped(true),"unavailable accessory accepted");
   Debug.Log("MINI165_HEADPHONES_PASS: repeat wear/remove; hair preserved; GameSave JSON roundtrip; legacy defaults; wardrobe Cancel/Apply; character isolation.");EditorApplication.ExitPlaymode();
  }catch(Exception e){SessionState.SetBool("Mini165Testing",false);Debug.LogException(e);EditorApplication.Exit(1);}}
 }
}
