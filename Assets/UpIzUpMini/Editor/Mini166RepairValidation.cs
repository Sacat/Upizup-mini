using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UpIzUpMini.Character;
using UpIzUpMini.UI;
namespace UpIzUpMini.EditorTools {
[InitializeOnLoad] public static class Mini166RepairValidation {
 const string Flag="Mini166RepairTesting",SaveKey="UpIzUpMini.Save.v1";static double next;
 static Mini166RepairValidation(){if(SessionState.GetBool(Flag,false))EditorApplication.playModeStateChanged+=Changed;}
 static void Require(bool ok,string why){if(!ok)throw new Exception("MINI166_REPAIR_TEST: "+why);}
 public static void Run(){SessionState.SetBool(Flag,true);EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity");EditorApplication.playModeStateChanged+=Changed;EditorApplication.EnterPlaymode();}
 static void Changed(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode){next=EditorApplication.timeSinceStartup+4;EditorApplication.update+=Check;}if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Flag,false);EditorApplication.playModeStateChanged-=Changed;EditorApplication.Exit(0);}}
 static void Check(){if(EditorApplication.timeSinceStartup<next)return;EditorApplication.update-=Check;bool hadSave=PlayerPrefs.HasKey(SaveKey);string originalSave=PlayerPrefs.GetString(SaveKey);try{
  var all=Resources.FindObjectsOfTypeAll<OutfitWardrobe>().Where(w=>w.gameObject.scene.IsValid()).ToArray();Require(all.Length==2,"expected two playable wardrobes");
  foreach(var w in all){w.Restore(null);Require(w.pieces.Length==10,w.name+" must have nine designs plus No Hat");Require(w.bindings.Select(b=>b.renderer).Distinct().Count()==4,"slot renderers must be independent");
   foreach(var piece in w.pieces){Require(w.Select(piece.id,3),"select "+piece.id);var r=w.bindings.First(b=>b.slot==piece.slot).renderer;Require(r.enabled==(piece.mesh!=null),"visibility "+piece.id);if(!piece.mesh)continue;Require(r.sharedMaterials.Length==piece.mesh.subMeshCount,"material count "+piece.id);Require(piece.mesh.bindposes.Length==r.bones.Length,"bone palette "+piece.id);var baked=WardrobePreviewMesh.Bake(r);Require(baked.vertexCount>20&&baked.bounds.size.magnitude>.05f&&baked.bounds.size.magnitude<3f,"deformed bounds "+w.name+piece.id);Require(baked.vertices.All(v=>!float.IsNaN(v.x)&&!float.IsInfinity(v.y)),"invalid vertex");UnityEngine.Object.Destroy(baked);
    for(int i=0;i<r.sharedMaterials.Length;i++){var block=new MaterialPropertyBlock();r.GetPropertyBlock(block,i);if(piece.tintSlots.Contains(i))Require(block.GetColor("_Color")==OutfitWardrobe.Colours[3],"fabric tint missing");else Require(block.isEmpty,"skin/sole tinted "+piece.id);}
   }
   Require(!w.Select("missing",0)&&!w.Select("shirt_tee_mike",-1),"invalid selection accepted");Require(w.pieces.First(p=>p.id=="shoes_mike90").mesh!=w.pieces.First(p=>p.id=="shoes_mike97").mesh&&w.pieces.First(p=>p.id=="shoes_mike270").mesh!=w.pieces.First(p=>p.id=="shoes_mike90").mesh,"shoes share geometry");w.Restore(null);
  }
  var sacat=all.First(w=>w.name=="Sacat");var franki=all.First(w=>w.name=="Franki");
  Require(sacat.Current(OutfitSlot.Shirt).itemId!=franki.Current(OutfitSlot.Shirt).itemId&&sacat.Current(OutfitSlot.Pants).itemId!=franki.Current(OutfitSlot.Pants).itemId,"distinct character starting outfits");
  Require(!sacat.bindings.Select(b=>b.renderer).Intersect(franki.bindings.Select(b=>b.renderer)).Any(),"character renderers must not be shared");
  sacat.Select("pants_shorts_denim",5);sacat.Select("shirt_polo_lacos",4);franki.Select("pants_trousers",2);franki.Select("shirt_tee_mike",3);
  var eq=sacat.GetComponent<CharacterEquipment>();eq.SetHeadphonesEquipped(false);SaveLoadSystem.Instance.Save();sacat.Restore(null);franki.Restore(null);eq.SetHeadphonesEquipped(true);SaveLoadSystem.Instance.Load();
  Require(sacat.Current(OutfitSlot.Pants).itemId=="pants_shorts_denim"&&sacat.Current(OutfitSlot.Shirt).colour==4,"Sacat actual save/load");Require(franki.Current(OutfitSlot.Pants).itemId=="pants_trousers"&&franki.Current(OutfitSlot.Shirt).colour==3,"Franki actual save/load isolation");Require(!eq.HeadphonesEquipped,"headphone save/load");
  var legacy=JsonUtility.FromJson<GameSave>("{}");sacat.Restore(legacy.sacatOutfit);Require(sacat.Capture().Count==4,"legacy defaults");
  foreach(var w in all){var other=all.First(x=>x!=w);string otherState=string.Join(";",other.Capture().OrderBy(c=>c.itemId).Select(c=>c.itemId+":"+c.colour));var equipment=w.GetComponent<CharacterEquipment>();w.Restore(null);string startingShirt=w.Current(OutfitSlot.Shirt).itemId;Time.timeScale=1;VisualWardrobePanel.Open(equipment);Require(VisualWardrobePanel.IsOpen,"wardrobe open "+w.name);var panel=UnityEngine.Object.FindFirstObjectByType<VisualWardrobePanel>();Require(panel.ChoosePiece("shirt_polo_lacos",4),"UI choose polo");Require(panel.ChoosePiece("pants_shorts_denim",5),"UI choose shorts");
   var model=(GameObject)typeof(VisualWardrobePanel).GetField("model",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(panel);var shirt=model.GetComponentsInChildren<MeshRenderer>().First(r=>r.name=="WardrobeShirt");var block=new MaterialPropertyBlock();shirt.GetPropertyBlock(block,0);Require(block.GetColor("_Color")==OutfitWardrobe.Colours[4],"preview tint differs from player");
   panel.RestoreOpeningOutfit();Require(w.Current(OutfitSlot.Shirt).itemId==startingShirt,"Restore opening");panel.ChoosePiece("shirt_polo_lacos",3);panel.Close(false);Require(w.Current(OutfitSlot.Shirt).itemId==startingShirt,"Cancel");
   VisualWardrobePanel.Open(equipment);panel.ChoosePiece("shirt_polo_lacos",4);panel.Close(true);Require(w.Current(OutfitSlot.Shirt).colour==4,"Apply");
   Require(otherState==string.Join(";",other.Capture().OrderBy(c=>c.itemId).Select(c=>c.itemId+":"+c.colour)),"UI changed the other character's outfit");
   foreach(var binding in other.bindings){var choice=other.Current(binding.slot);var piece=other.pieces.First(p=>p.id==choice.itemId);Require(binding.renderer.sharedMesh==piece.mesh,"other character's visible mesh changed");if(piece.mesh){var tint=new MaterialPropertyBlock();binding.renderer.GetPropertyBlock(tint,0);Require(tint.GetColor("_Color")==OutfitWardrobe.Colours[choice.colour],"other character's visible colour changed");}}
   equipment.SetAccessoryEquipped("shades_ray",true);Require(equipment.AccessoryEquipped("shades_ray"),"shades wear");equipment.SetAccessoryEquipped("shades_ray",false);Require(!equipment.AccessoryEquipped("shades_ray"),"shades remove");
  }
  eq.SetHeadphonesEquipped(true);Require(eq.HeadphonesEquipped,"headphones wear");eq.SetHeadphonesEquipped(false);Require(!eq.HeadphonesEquipped,"headphones remove");Require(sacat.GetComponentsInChildren<SkinnedMeshRenderer>().Any(r=>r.name=="WardrobeBody"&&r.enabled),"body remains after removal");
  Debug.Log("MINI166_REPAIR_TEST_PASS: all designs/bones/bounds; independent tint masks; actual SaveLoadSystem round trip and legacy defaults; UI preview colour, Restore/Cancel/Apply; removable accessories; character isolation.");
 }catch(Exception e){Debug.LogException(e);SessionState.SetBool(Flag,false);RestoreSave(hadSave,originalSave);EditorApplication.Exit(1);return;}finally{RestoreSave(hadSave,originalSave);}EditorApplication.ExitPlaymode();}
 static void RestoreSave(bool existed,string value){if(existed)PlayerPrefs.SetString(SaveKey,value);else PlayerPrefs.DeleteKey(SaveKey);PlayerPrefs.Save();}
}
}
