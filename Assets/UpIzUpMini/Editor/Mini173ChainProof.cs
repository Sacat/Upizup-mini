using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;
namespace UpIzUpMini.EditorTools {
[InitializeOnLoad] public static class Mini173ChainProof {
 const string Key="Mini173ChainProof";static double next;static bool failed;
 static Mini173ChainProof(){if(SessionState.GetBool(Key,false))EditorApplication.playModeStateChanged+=Changed;}
 public static void Run(){Directory.CreateDirectory("Logs/Tasks/MINI-173/Renders");SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof_ExpansionImport.unity");EditorApplication.playModeStateChanged+=Changed;EditorApplication.EnterPlaymode();}
 static void Changed(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode){next=EditorApplication.timeSinceStartup+3;EditorApplication.update+=Check;}if(s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.Exit(failed?1:0);}}
 static void Check(){if(EditorApplication.timeSinceStartup<next)return;EditorApplication.update-=Check;try{
 var all=UnityEngine.Object.FindObjectsByType<CharacterEquipment>(FindObjectsInactive.Include,FindObjectsSortMode.None);var lines=new System.Collections.Generic.List<string>();
 foreach(var name in new[]{"Sacat","Franki"}){var eq=all.FirstOrDefault(e=>e.name==name);if(!eq)throw new Exception("Missing "+name);eq.gameObject.SetActive(true);typeof(CharacterEquipment).GetField("alwaysEquipped",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(eq,new[]{"chain_gold"});eq.RefreshEquipment();var chain=eq.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Equip_chain_gold");if(!chain)throw new Exception("Chain missing "+name);
 foreach(var mf in chain.GetComponentsInChildren<MeshFilter>())Debug.Log("CHAINMESH "+mf.name+" readable="+mf.sharedMesh.isReadable+" vertices="+mf.sharedMesh.vertexCount);
 var a=eq.GetComponentInChildren<Animator>();var neck=a.GetBoneTransform(HumanBodyBones.Neck);var chest=a.GetBoneTransform(HumanBodyBones.Chest);var focus=(neck.position+chest.position)*.5f;
 var c=new GameObject("ChainProofCamera").AddComponent<Camera>();c.nearClipPlane=.01f;c.farClipPlane=100;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.38f,.42f,.45f);
 Mini173LalayRevision.Shot(c,name+"-chain-front",focus+eq.transform.forward*1.15f+Vector3.up*.12f,focus,false,40);
 Mini173LalayRevision.Shot(c,name+"-chain-side",focus+eq.transform.right*1.05f+eq.transform.forward*.12f,focus,false,40);
 lines.Add(name+" chainWorld="+chain.position+" neck="+neck.position+" chest="+chest.position+" correction="+CharacterEquipment.ChainNeckwardCorrection);UnityEngine.Object.DestroyImmediate(c.gameObject);
 }File.WriteAllLines("Logs/Tasks/MINI-173/ChainProof.txt",lines);Debug.Log("MINI173_CHAIN_CAPTURE_PASS");
 }catch(Exception e){failed=true;Debug.LogException(e);}finally{next=EditorApplication.timeSinceStartup+2;EditorApplication.update+=Settled;}}
static void Settled(){if(EditorApplication.timeSinceStartup<next)return;EditorApplication.update-=Settled;try{foreach(var eq in UnityEngine.Object.FindObjectsByType<CharacterEquipment>(FindObjectsSortMode.None).Where(e=>e.name=="Sacat"||e.name=="Franki")){var a=eq.GetComponentInChildren<Animator>();var focus=(a.GetBoneTransform(HumanBodyBones.Neck).position+a.GetBoneTransform(HumanBodyBones.Chest).position)*.5f;var c=new GameObject("SettledChainCamera").AddComponent<Camera>();c.nearClipPlane=.01f;Mini173LalayRevision.Shot(c,eq.name+"-chain-settled",focus+eq.transform.forward*1.2f+eq.transform.right*.35f,focus,false,40);UnityEngine.Object.DestroyImmediate(c.gameObject);}Debug.Log("MINI173_CHAIN_SETTLED_CAPTURE_PASS");}catch(Exception e){failed=true;Debug.LogException(e);}finally{EditorApplication.ExitPlaymode();}}}}



