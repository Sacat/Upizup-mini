using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.EditorTools
{
    public static class Mini192Check
    {
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity", OpenSceneMode.Single);
            int n = 0, human = 0;
            foreach (var o in Object.FindObjectsByType<PoliceOfficer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                n++; var a = o.GetComponentInChildren<Animator>(true);
                if (a != null && a.isHuman) human++;
            }
            var prefab = Resources.Load<GameObject>("Weapons/PoliceSidearmVisual");
            var fc = Object.FindObjectsByType<UpIzUpMini.Combat.FirearmController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            Debug.Log("MINI192_CHECK officers=" + n + " humanoid=" + human + " prefabLoaded=" + (prefab != null) + " firearmControllers=" + fc);
            EditorApplication.Exit(0);
        }
    }
}
