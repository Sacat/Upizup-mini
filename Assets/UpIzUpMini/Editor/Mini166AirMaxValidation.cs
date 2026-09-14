using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UpIzUpMini.Character;
using UpIzUpMini.UI;
namespace UpIzUpMini.EditorTools {
[InitializeOnLoad] public static class Mini166AirMaxValidation {
 const string Flag="Mini166AirMaxTesting",SaveKey="UpIzUpMini.Save.v1";static double ready;
 static Mini166AirMaxValidation(){if(SessionState.GetBool(Flag,false))EditorApplication.playModeStateChanged+=Changed;}
 public static void Run(){SessionState.SetBool(Flag,true);EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity");EditorApplication.playModeStateChanged+=Changed;EditorApplication.EnterPlaymode();}
 static void Changed(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode){ready=EditorApplication.timeSinceStartup+4;EditorApplication.update+=Check;}if(s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Flag,false);EditorApplication.Exit(0);}}
 static void Need(bool ok,string why){if(!ok)throw new Exception("AIRMAX_TEST: "+why);}
 static string State(OutfitWardrobe w)=>string.Join("|",w.Capture().OrderBy(x=>x.itemId).Select(x=>x.itemId+":"+x.colour));
 static void Check(){if(EditorApplication.timeSinceStartup<ready)return;EditorApplication.update-=Check;bool existed=PlayerPrefs.HasKey(SaveKey);string save=PlayerPrefs.GetString(SaveKey);try{
  var all=Resources.FindObjectsOfTypeAll<OutfitWardrobe>().Where(w=>w.gameObject.scene.IsValid()).ToArray();Need(all.Length==2,"two characters");
  foreach(var w in all){w.Restore(null);Need(w.pieces.Any(p=>p.id=="shoes_mike270"),"270 preserved");Need(w.Current(OutfitSlot.Shoes).itemId==(w.name=="Franki"?"shoes_mike90":"shoes_mike97"),"individual default");}
  foreach(var w in all){var other=all.Single(x=>x!=w);string old=State(other);var otherMesh=other.bindings.Single(b=>b.slot==OutfitSlot.Shoes).renderer.sharedMesh;string start=w.Current(OutfitSlot.Shoes).itemId;
   VisualWardrobePanel.Open(w.GetComponent<CharacterEquipment>());var panel=UnityEngine.Object.FindFirstObjectByType<VisualWardrobePanel>();
   foreach(string id in new[]{"shoes_mike90","shoes_mike97"}){
    Need(panel.ChoosePiece(id,1),"UI select "+id);var piece=w.pieces.Single(p=>p.id==id);Need(piece.mesh.subMeshCount==4&&piece.materials.Length==4,"material/ankle slots");Need(piece.mesh.vertexCount>10000,"approved detailed mesh used");
    var model=(GameObject)typeof(VisualWardrobePanel).GetField("model",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(panel);var preview=model.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="WardrobeShoes");Need(preview.sharedMaterials.Length==4&&preview.GetComponent<MeshFilter>().sharedMesh.vertexCount==piece.mesh.vertexCount,"preview matches actual shoe geometry");Need(State(other)==old&&other.bindings.Single(b=>b.slot==OutfitSlot.Shoes).renderer.sharedMesh==otherMesh,"other character unchanged");
   }
   panel.Close(false);Need(w.Current(OutfitSlot.Shoes).itemId==start,"Cancel restores shoes");VisualWardrobePanel.Open(w.GetComponent<CharacterEquipment>());panel=UnityEngine.Object.FindFirstObjectByType<VisualWardrobePanel>();Need(panel.ChoosePiece("shoes_mike90",3),"choose red 90");panel.Close(true);Need(w.Current(OutfitSlot.Shoes).colour==3,"Apply retains colour");
  }
  var franki=all.Single(w=>w.name=="Franki");var sacat=all.Single(w=>w.name=="Sacat");franki.Select("shoes_mike90",3);sacat.Select("shoes_mike97",6);SaveLoadSystem.Instance.Save();franki.Select("shoes_mike270",0);sacat.Select("shoes_mike90",2);SaveLoadSystem.Instance.Load();Need(franki.Current(OutfitSlot.Shoes).itemId=="shoes_mike90"&&franki.Current(OutfitSlot.Shoes).colour==3,"Franki shoe save/load");Need(sacat.Current(OutfitSlot.Shoes).itemId=="shoes_mike97"&&sacat.Current(OutfitSlot.Shoes).colour==6,"Sacat independent shoe save/load");
  Debug.Log("MINI166_AIRMAX_TEST_PASS: both models on both characters; actual wardrobe preview, Cancel/Apply, independent saved shoes/colours; 270 retained");
 }catch(Exception e){Debug.LogException(e);SessionState.SetBool(Flag,false);Restore(existed,save);EditorApplication.Exit(1);return;}finally{Restore(existed,save);}EditorApplication.ExitPlaymode();}
 static void Restore(bool existed,string save){if(existed)PlayerPrefs.SetString(SaveKey,save);else PlayerPrefs.DeleteKey(SaveKey);PlayerPrefs.Save();}
}
}
