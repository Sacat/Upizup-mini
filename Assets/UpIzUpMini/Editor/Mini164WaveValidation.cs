using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;
namespace UpIzUpMini.EditorTools {
[InitializeOnLoad] public static class Mini164WaveValidation { static Mini164WaveValidation(){if(SessionState.GetBool("Mini164Testing",false))EditorApplication.playModeStateChanged+=Changed;}
 static int phase;static double next;static CharacterEquipment[] equipment;static SkinnedMeshRenderer[] hair;static Mesh[] meshes;
 public static void Run(){SessionState.SetBool("Mini164Testing",true);EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity");EditorApplication.playModeStateChanged+=Changed;EditorApplication.EnterPlaymode();}
 static void Require(bool ok,string why){if(!ok)throw new Exception("MINI164: "+why);}
 static void Changed(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode){phase=0;next=EditorApplication.timeSinceStartup+3;EditorApplication.update+=Tick;}if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool("Mini164Testing",false);EditorApplication.playModeStateChanged-=Changed;Mini001Build.BuildWindowsPlayer();}}
 static void Tick(){if(EditorApplication.timeSinceStartup<next)return;try{
  if(phase==0){ UpIzUpMini.Economy.EconomyManager.Instance.LoadState(123,0,null,null,ownedIds:new string[0],sacatOwnedIds:new string[0],frankiOwnedIds:new string[0]);
   var roots=Resources.FindObjectsOfTypeAll<CharacterEquipment>().Where(c=>c.gameObject.scene.IsValid()&&(c.name=="Franki"||c.name=="Sacat")).OrderBy(c=>c.name).ToArray();
   Require(roots.Length==2,"both characters required");equipment=roots;hair=roots.Select(c=>c.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r=>r.name==(c.name=="Franki"?"Ch28_Hair":"Ch06"))).ToArray();meshes=hair.Select(h=>h.sharedMesh).ToArray();
   for(int i=0;i<2;i++){Require(meshes[i].name.Contains("Waves"),"saved wave mesh missing");Require(hair[i].sharedMaterials.Last().mainTexture!=null,"wave texture missing");equipment[i].gameObject.SetActive(true);equipment[i].SetTrialItem("cap_mike",true);}
  }else if(phase==1){for(int i=0;i<2;i++){Require(equipment[i].GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Equip_cap_mike"),"cap did not equip");equipment[i].SetTrialItem("cap_mike",false);}}
  else {for(int i=0;i<2;i++){Require(!equipment[i].GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Equip_cap_mike"),"cap did not remove");Require(hair[i].sharedMesh==meshes[i] && hair[i].enabled,"hair changed when cap removed");Require(hair[i].sharedMaterials.Last().mainTexture!=null,"hair material lost");}Debug.Log("MINI164_CAP_REMOVE_PASS: both characters equip/remove cap; wave mesh and material remain intact.");EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();return;}
  phase++;next=EditorApplication.timeSinceStartup+1;
 }catch(Exception e){SessionState.SetBool("Mini164Testing",false);Debug.LogException(e);EditorApplication.update-=Tick;EditorApplication.Exit(1);}}
}}


