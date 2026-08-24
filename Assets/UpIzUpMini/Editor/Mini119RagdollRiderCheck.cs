using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-119 follow-up: verifies SuperMotoRagdollRider
    /// actually builds and pins Sacat's hands/feet/hips to the bike's
    /// own anchors via real physics joints. Delete after use.</summary>
    public static class Mini119RagdollRiderCheck
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Check Ragdoll Rider (one-off)")]
        public static void Run()
        {
            var previousSimMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var spawner = Object.FindFirstObjectByType<VehicleSpawnController>();
            var method = typeof(VehicleSpawnController).GetMethod("SpawnStockDemoBikeAndDisableOurCharacter", BindingFlags.NonPublic | BindingFlags.Instance);
            var player = GameObject.Find("Sacat");
            method.Invoke(spawner, new object[] { player });

            var instance = GameObject.Find("StockDemoSuperMoto");
            if (instance == null) { Debug.LogError("MINI-119 RAGDOLL RIDER CHECK FAIL: bike not spawned."); return; }

            var rider = player.GetComponent<SuperMotoRagdollRider>();
            Debug.Log($"MINI-119 RAGDOLL RIDER CHECK: rider component found={rider != null}");
            if (rider == null) { Debug.LogError("MINI-119 RAGDOLL RIDER CHECK FAIL: SuperMotoRagdollRider was never added to Sacat."); Physics.simulationMode = previousSimMode; return; }

            var riderUpdate = typeof(SuperMotoRagdollRider).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);

            for (int i = 0; i < 20; i++)
            {
                try { riderUpdate?.Invoke(rider, null); }
                catch (TargetInvocationException ex) { Debug.LogError($"MINI-119 RAGDOLL RIDER CHECK: Update() threw on tick {i}: {ex.InnerException}"); break; }
                Physics.Simulate(0.02f);
            }

            var animatorCheck = player.GetComponentInChildren<Animator>(true);
            Debug.Log($"MINI-119 RAGDOLL RIDER CHECK: Animator found={animatorCheck != null}, enabled={(animatorCheck != null ? animatorCheck.enabled.ToString() : "n/a")}");

            LogJoint(player, HumanBodyBones.RightHand);
            LogJoint(player, HumanBodyBones.LeftHand);
            LogJoint(player, HumanBodyBones.RightFoot);
            LogJoint(player, HumanBodyBones.LeftFoot);
            LogJoint(player, HumanBodyBones.Hips);

            Physics.simulationMode = previousSimMode;
        }

        private static void LogJoint(GameObject player, HumanBodyBones bone)
        {
            var animator = player.GetComponentInChildren<Animator>(true);
            if (animator == null) { Debug.Log($"MINI-119 RAGDOLL RIDER CHECK: {bone} - no Animator found on player at all"); return; }
            Transform t = animator.GetBoneTransform(bone);
            if (t == null) { Debug.Log($"MINI-119 RAGDOLL RIDER CHECK: {bone} bone transform is null"); return; }

            var joint = t.GetComponent<ConfigurableJoint>();
            var rb = t.GetComponent<Rigidbody>();
            if (joint == null || joint.connectedBody == null)
            {
                Debug.Log($"MINI-119 RAGDOLL RIDER CHECK: {bone} - NO JOINT/connectedBody found (rb={(rb != null ? "present" : "MISSING")})");
                return;
            }

            Vector3 anchorWorldPos = joint.connectedBody.transform.position;
            float dist = Vector3.Distance(t.position, anchorWorldPos);
            Debug.Log($"MINI-119 RAGDOLL RIDER CHECK: {bone} distance to anchor '{joint.connectedBody.name}' = {dist:F4}m, rb.isKinematic={(rb != null ? rb.isKinematic.ToString() : "no rb")}");
        }
    }
}
