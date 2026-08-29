using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-120 follow-up, user: "i didnt see dog life on the
    /// block." Read-only inspection of RivalGangSpawner's configured
    /// blockCentre/enterDistance against the actual placed NPC_DogLife_i
    /// positions and pool contents, instead of guessing. Delete after
    /// use.</summary>
    public static class Mini120DiagnoseDogLife
    {
        [MenuItem("Up Iz Up Mini/MINI-120/Diagnose Dog Life (one-off, read-only)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var spawnerGo = GameObject.Find("DogLifeSpawner");
            if (spawnerGo == null) { Debug.LogError("MINI-120 DIAGNOSE DOG LIFE: no DogLifeSpawner in scene."); return; }

            var spawner = spawnerGo.GetComponent<RivalGangSpawner>();
            var so = new SerializedObject(spawner);
            var blockCentre = so.FindProperty("blockCentre").vector3Value;
            var enterDistance = so.FindProperty("enterDistance").floatValue;
            var exitDistance = so.FindProperty("exitDistance").floatValue;
            var poolProp = so.FindProperty("_pool");

            Debug.Log($"MINI-120 DIAGNOSE DOG LIFE: DogLifeSpawner at {spawnerGo.transform.position}, blockCentre={blockCentre}, enterDistance={enterDistance}, exitDistance={exitDistance}, pool size={poolProp.arraySize}.");

            for (int i = 0; i < poolProp.arraySize; i++)
            {
                var member = poolProp.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
                if (member == null) { Debug.LogError($"MINI-120 DIAGNOSE DOG LIFE: pool[{i}] is NULL."); continue; }
                float dist = Vector3.Distance(member.transform.position, blockCentre);
                Debug.Log($"MINI-120 DIAGNOSE DOG LIFE: pool[{i}]='{member.name}' at {member.transform.position}, activeSelf={member.activeSelf}, distance from blockCentre={dist:F1}m.");
            }

            // Cross-check against Sacat's/Franki's actual position - is
            // either main character anywhere near the block right now?
            var sacat = GameObject.Find("Sacat");
            var franki = GameObject.Find("Franki");
            if (sacat != null) Debug.Log($"MINI-120 DIAGNOSE DOG LIFE: Sacat at {sacat.transform.position}, distance from blockCentre={Vector3.Distance(sacat.transform.position, blockCentre):F1}m.");
            if (franki != null) Debug.Log($"MINI-120 DIAGNOSE DOG LIFE: Franki at {franki.transform.position}, distance from blockCentre={Vector3.Distance(franki.transform.position, blockCentre):F1}m.");

            var roadDirection = so.FindProperty("roadDirection").vector3Value;
            Debug.Log($"MINI-120 DIAGNOSE DOG LIFE: roadDirection={roadDirection}.");
        }
    }
}
