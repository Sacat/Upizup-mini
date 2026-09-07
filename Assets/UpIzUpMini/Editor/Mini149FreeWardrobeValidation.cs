using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;
namespace UpIzUpMini.EditorTools
{
    public static class Mini149FreeWardrobeValidation
    {
        static void Require(bool ok,string why){if(!ok)throw new Exception("MINI149: "+why);}
        static void Awake(object o)=>o.GetType().GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(o,null);
        public static void ValidateBuild()
        {
            const string scene="Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
            byte[] before=File.ReadAllBytes(scene);
            EditorSceneManager.OpenScene(scene);
            var s=UnityEngine.Object.FindFirstObjectByType<CharacterSwitchManager>();Awake(s);
            var economy=UnityEngine.Object.FindFirstObjectByType<EconomyManager>();Awake(economy);
            economy.LoadState(123,0,null,null,ownedIds:new string[0],sacatOwnedIds:new string[0],frankiOwnedIds:new string[0]);
            var a=s.Slots[0].root.GetComponent<CharacterEquipment>();var b=s.Slots[1].root.GetComponent<CharacterEquipment>();Awake(a);Awake(b);
            a.RestoreWardrobe(null);b.RestoreWardrobe(null);
            foreach(var id in CharacterEquipment.TrialItemIds)
            {
                Require(a.SetTrialItem(id,true),"trial refused");
                Require(a.GetComponentsInChildren<Transform>().Any(t=>t.name=="Equip_"+id),"trial missing");
                Require(!b.GetComponentsInChildren<Transform>().Any(t=>t.name=="Equip_"+id),"trial leaked to partner");
                Require(!economy.OwnsItem(id,0)&&economy.Money==123&&a.CaptureWardrobe().Count==0,"trial changed money/ownership/save");
            }
            Require(!a.SetTrialItem("land_montine",true),"trial allowed arbitrary item");a.ClearTrialItems();
            Require(!a.GetComponentsInChildren<Transform>().Any(t=>CharacterEquipment.TrialItemIds.Any(id=>t.name=="Equip_"+id)),"clear failed");
            Require(before.SequenceEqual(File.ReadAllBytes(scene)),"scene changed");
            Debug.Log("MINI149_FREE_TRIAL_PASS: independent trials, no money/ownership/save grants, clear restores, scene unchanged.");
            EditorSceneManager.OpenScene(scene);
            Mini001Build.BuildWindowsPlayer();
        }
    }
}
