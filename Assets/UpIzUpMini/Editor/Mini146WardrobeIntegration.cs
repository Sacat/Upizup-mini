using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;
using UpIzUpMini.Interaction;
using Object=UnityEngine.Object;

namespace UpIzUpMini.EditorTools
{
    public static class Mini146WardrobeIntegration
    {
        const string Scene="Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string Art="Assets/UpIzUpMini/Art/Accessories/GoldWatchMobile";
        const string Out="Logs/Tasks/MINI-146";
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Require(bool ok,string why){if(!ok)throw new Exception("MINI146: "+why);}
        static void Awake(object o)=>o.GetType().GetMethod("Awake",Flags)?.Invoke(o,null);
        static string Hash(string path){using(var s=System.Security.Cryptography.SHA256.Create())return Convert.ToBase64String(s.ComputeHash(File.ReadAllBytes(path)));}
        static string Pose(Transform t)=>t.localPosition.ToString("F6")+t.localRotation.ToString("F6")+t.localScale.ToString("F6");
        static void Active(CharacterSwitchManager s,int index)=>typeof(CharacterSwitchManager).GetField("<ActiveIndex>k__BackingField",Flags).SetValue(s,index);
        static Transform[] Watches(CharacterEquipment e)=>e.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Equip_watch_rollie"&&t.gameObject.activeSelf).ToArray();

        public static void IntegrateValidateBuild()
        {
            Directory.CreateDirectory(Out);
            string backup=Out+"/GrandBayProof-Before-WatchIntegration.unity";
            if(!File.Exists(backup))File.Copy(Scene,backup);
            EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);
            var switcher=Object.FindFirstObjectByType<CharacterSwitchManager>();Require(switcher!=null,"switcher missing");
            var transforms=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).ToDictionary(t=>t,Pose);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/GoldWatchMobile.prefab");Require(prefab!=null,"approved prefab missing");
            foreach(var slot in switcher.Slots)
            {
                var eq=slot.root.GetComponent<CharacterEquipment>();Require(eq!=null,"equipment missing");
                var so=new SerializedObject(eq);
                var chain=so.FindProperty("chainPlacement").objectReferenceValue;
                var chainPrefab=so.FindProperty("chainPrefab").objectReferenceValue;
                so.FindProperty("watchPrefab").objectReferenceValue=prefab;
                so.FindProperty("watchPlacement").objectReferenceValue=AssetDatabase.LoadAssetAtPath<AccessoryPlacementProfile>(Art+"/"+slot.displayName+"WatchFit.asset");
                Require(so.FindProperty("watchPlacement").objectReferenceValue!=null,"fit missing");
                so.ApplyModifiedPropertiesWithoutUndo();
                Require(so.FindProperty("chainPlacement").objectReferenceValue==chain&&so.FindProperty("chainPrefab").objectReferenceValue==chainPrefab,"chain references changed");
            }
            Require(transforms.All(p=>p.Key!=null&&Pose(p.Key)==p.Value),"map/character transform changed");
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            string sceneHash=Hash(Scene),sacatFitHash=Hash(Art+"/SacatWatchFit.asset"),frankiFitHash=Hash(Art+"/FrankiWatchFit.asset");
            Validate();
            Require(Hash(Scene)==sceneHash&&Hash(Art+"/SacatWatchFit.asset")==sacatFitHash&&Hash(Art+"/FrankiWatchFit.asset")==frankiFitHash,"validation changed scene or fits");
            // Discard simulated ownership, menu state, teleports and rendered poses.
            EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);
            Mini001Build.BuildWindowsPlayer();
            Debug.Log("MINI146_WARDROBE_BUILD_PASS");
        }

        public static void Validate()
        {
            Directory.CreateDirectory(Out);
            var switcher=Object.FindFirstObjectByType<CharacterSwitchManager>();Awake(switcher);
            var economy=Object.FindFirstObjectByType<EconomyManager>();Awake(economy);
            var equipment=switcher.Slots.Select(s=>s.root.GetComponent<CharacterEquipment>()).ToArray();
            foreach(var e in equipment){Awake(e);e.RestoreWardrobe(null);}
            economy.LoadState(10000,0,null,null,ownedIds:new string[0],sacatOwnedIds:new string[0],frankiOwnedIds:new string[0]);
            var item=AssetDatabase.LoadAssetAtPath<ShopItemDefinition>("Assets/UpIzUpMini/Data/Shop/watch_rollie.asset");Require(item!=null,"shop item missing");
            Active(switcher,0);Require(economy.TryPurchase(item,out _),"Sacat purchase failed");
            Require(equipment[0].WatchEquipped&&Watches(equipment[0]).Length==1,"Sacat purchase not visible");
            Require(!equipment[1].WatchOwned&&Watches(equipment[1]).Length==0&&!equipment[1].SetWatchEquipped(true),"purchase leaked to Franki");
            foreach(int i in new[]{0,1})
            {
                Active(switcher,i);if(i==1)Require(economy.TryPurchase(item,out _),"Franki purchase failed");
                var e=equipment[i];e.RefreshEquipment();var watch=Watches(e).Single();
                var fit=AssetDatabase.LoadAssetAtPath<AccessoryPlacementProfile>(Art+"/"+switcher.Slots[i].displayName+"WatchFit.asset");
                Require(Vector3.Distance(watch.localPosition,fit.localPosition)<.000001f&&Quaternion.Angle(watch.localRotation,Quaternion.Euler(fit.localEulerAngles))<.001f&&Vector3.Distance(watch.localScale,fit.localScale)<.000001f,"approved wrist placement changed");
                Require(watch.GetComponentsInChildren<LODGroup>().Length==1,"primitive watch used instead of approved prefab");
                Require(e.SetWatchEquipped(false)&&e.WatchOwned&&!e.WatchEquipped&&Watches(e).Length==0,"remove must store, not sell");
                var save=new GameSave{ sacatUnequippedItems=equipment[0].CaptureWardrobe(),frankiUnequippedItems=equipment[1].CaptureWardrobe() };
                var json=JsonUtility.ToJson(save);var restored=JsonUtility.FromJson<GameSave>(json);
                e.SetWatchEquipped(true);e.RestoreWardrobe(i==0?restored.sacatUnequippedItems:restored.frankiUnequippedItems);
                Require(!e.WatchEquipped,"save roundtrip lost removal");
                var legacy=JsonUtility.FromJson<GameSave>("{}");e.RestoreWardrobe(i==0?legacy.sacatUnequippedItems:legacy.frankiUnequippedItems);
                Require(e.WatchEquipped&&Watches(e).Length==1,"legacy save default should retain owned watch");
                CaptureWrist(switcher.Slots[i].root,watch=null,name:switcher.Slots[i].displayName);
            }
            var home=Object.FindObjectsByType<SafehouseInteractable>(FindObjectsSortMode.None).First(h=>string.IsNullOrEmpty((string)typeof(SafehouseInteractable).GetField("requiredItemId",Flags).GetValue(h)));
            Active(switcher,1);var player=switcher.Slots[1].root;player.transform.position=home.transform.position+Vector3.forward*.5f;
            player.GetComponent<PlayerController>().IsControlled=true;
            home.Interact(player);Require(home.OpenWardrobe(),"owned home wardrobe unavailable");
            int money=economy.Money;Require(home.ChooseWatch(false)&&!equipment[1].WatchEquipped&&equipment[0].WatchEquipped,"wardrobe altered wrong wearer");
            Require(home.ChooseWatch(true)&&equipment[1].WatchEquipped&&economy.Money==money,"wardrobe charged money or failed");
            string prompt=home.PromptLabel;Require(prompt.Contains("Franki")&&prompt.Contains("[1] Wear")&&prompt.Contains("[2] Remove"),"wardrobe prompt missing");
            player.transform.position=home.transform.position+Vector3.forward*8;Require(!home.ChooseWatch(false),"remote wardrobe allowed");
            var locked=new GameObject("LockedWardrobeFixture").AddComponent<SafehouseInteractable>();typeof(SafehouseInteractable).GetField("requiredItemId",Flags).SetValue(locked,"unowned_test_house");
            locked.transform.position=player.transform.position;locked.Interact(player);Require(!locked.OpenWardrobe(),"locked house wardrobe allowed");Object.DestroyImmediate(locked.gameObject);
            Require(economy.TryResell(item,out _)&&!equipment[1].WatchOwned&&Watches(equipment[1]).Length==0&&equipment[0].WatchOwned,"resale isolation failed");
            File.WriteAllText(Out+"/validation.txt","PASS: per-character purchase; approved prefab and exact fits; wear/remove; independent JSON roundtrip and old save defaults; owned/near home only; resale isolation. No PlayerPrefs Save/Load called.\nWardrobe prompt:\n"+prompt);
            Debug.Log("MINI146_WARDROBE_VALIDATION_PASS");
        }
        static void CaptureWrist(GameObject actor,Transform watch,string name)
        {
            watch=actor.GetComponentsInChildren<Transform>().First(t=>t.name=="Equip_watch_rollie"&&t.gameObject.activeSelf);
            var cam=new GameObject("PurchasedWatchProofCamera").AddComponent<Camera>();cam.nearClipPlane=.001f;cam.fieldOfView=32;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.08f,.1f,.12f);
            cam.transform.position=watch.position+watch.up*.28f+actor.transform.up*.065f;cam.transform.LookAt(watch.position+watch.up*.018f);
            var rt=new RenderTexture(900,900,24){antiAliasing=4};var old=RenderTexture.active;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
            var tex=new Texture2D(900,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,900,900),0,0);tex.Apply();File.WriteAllBytes(Out+"/"+name+"-Purchased-Watch.png",tex.EncodeToPNG());
            cam.targetTexture=null;RenderTexture.active=old;Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);Object.DestroyImmediate(cam.gameObject);
        }
    }
}
