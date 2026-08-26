using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-119 follow-up, user: "i want you to rig the
    /// character like what you told me... take control and open the
    /// supermoto to a point where i can rig the character on the bike
    /// manually." Mounts Sacat onto the real, preplaced SuperMoto using
    /// the exact same runtime Mount() code path walking up and pressing
    /// F uses, then SAVES that state into GrandBayProof.unity so it's
    /// there in Edit Mode the next time the scene is opened - no Play
    /// Mode needed to get him positioned.
    ///
    /// Honest limitation: the Animator doesn't evaluate/play a pose
    /// outside Play Mode, so he'll likely show up in a raw bind/T-pose
    /// here despite Mount() having "frozen" the Animator's parameters -
    /// SetFloat/SetBool only affect the live PlayableGraph, which only
    /// runs during Play. That doesn't block manual rig wiring though -
    /// his ROOT position (seated, parented to the bike) is correct, and
    /// that's what matters for placing Root/Mid/Tip bones and IK
    /// targets against the bike's real anchors.</summary>
    public static class Mini119EditModeMountSetup
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Edit-Mode Mount Sacat On Bike (one-off)")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var player = GameObject.Find("Sacat");
            if (player == null) { Debug.LogError("MINI-119 EDIT-MODE MOUNT FAIL: Sacat not found."); return; }

            var bike = GameObject.Find("StockDemoSuperMoto");
            if (bike == null) { Debug.LogError("MINI-119 EDIT-MODE MOUNT FAIL: StockDemoSuperMoto not found - is it still preplaced in the scene?"); return; }

            // The preplaced bike is just the raw prefab instance until
            // the spawn/wiring method runs - that's normally only ever
            // called at runtime (VehicleSpawnController.Update()), so it
            // has to be invoked here too, exactly like
            // Mini119MountFlowCheck/Mini119SpawnDiagnose already do, or
            // the bike won't have SuperMotoStockInteractable (or its
            // input-fix wiring) on it yet.
            var spawner = Object.FindFirstObjectByType<VehicleSpawnController>();
            if (spawner == null) { Debug.LogError("MINI-119 EDIT-MODE MOUNT FAIL: no VehicleSpawnController in the scene."); return; }
            var spawnMethod = typeof(VehicleSpawnController).GetMethod("SpawnStockDemoBikeAndDisableOurCharacter", BindingFlags.NonPublic | BindingFlags.Instance);
            try
            {
                spawnMethod.Invoke(spawner, new object[] { player });
            }
            catch (TargetInvocationException ex)
            {
                Debug.LogError($"MINI-119 EDIT-MODE MOUNT FAIL: spawn/wiring method threw: {ex.InnerException}");
                return;
            }

            // MINI-119 follow-up: updated for the VehicleRider/VehicleSeat
            // switch - SuperMotoStockInteractable no longer exists on the
            // spawned bike, SuperMotoVehicleInteractable does.
            var interactable = bike.GetComponent<SuperMotoVehicleInteractable>();
            if (interactable == null) { Debug.LogError("MINI-119 EDIT-MODE MOUNT FAIL: SuperMotoVehicleInteractable still missing after wiring - hand/foot anchors not found on the bike?"); return; }

            var mountMethod = typeof(SuperMotoVehicleInteractable).GetMethod("Mount", BindingFlags.NonPublic | BindingFlags.Instance);
            try
            {
                mountMethod.Invoke(interactable, new object[] { player });
            }
            catch (TargetInvocationException ex)
            {
                Debug.LogError($"MINI-119 EDIT-MODE MOUNT FAIL: Mount() threw: {ex.InnerException}");
                return;
            }

            // MINI-119 follow-up, user: "i want to see what i am doing
            // when i rig as well." Animator.GetBoneTransform needs a
            // bound Avatar - reads null in this same Editor batch-mode
            // context that's been a known gap all task (animator.avatar
            // is null here) - so bones are found by NAME instead,
            // bypassing that gap entirely. Gadd420.IK itself doesn't use
            // the Animator either - it walks up chainLength Transform
            // parents from whatever bone it's given - so it works fine
            // here too. Same one-shot posing technique already used for
            // SuperMotoRagdollRider.PoseOntoTargets earlier this task:
            // attach the IK solver, run its LateUpdate a few times to
            // converge, then remove it - leaves the bones bent toward
            // the bike's real anchors as plain Transform values, which
            // DOES render and save in Edit Mode (unlike anything routed
            // through the Animator).
            PoseLimbOntoAnchor(player.transform, "mixamorig9:RightHand", bike.transform, "RightHandPos");
            PoseLimbOntoAnchor(player.transform, "mixamorig9:LeftHand", bike.transform, "LeftHandPos");
            PoseLimbOntoAnchor(player.transform, "mixamorig9:RightFoot", bike.transform, "RightFootPos");
            PoseLimbOntoAnchor(player.transform, "mixamorig9:LeftFoot", bike.transform, "LeftFootPos");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"MINI-119 EDIT-MODE MOUNT OK: Sacat parented onto the bike's seat, posed toward its hand/foot anchors, and saved into the scene. worldPos={player.transform.position}. Open GrandBayProof.unity - he should show a rough seated pose now (a starting approximation, not the real Animator-driven pose - that only comes from an actual mounted Play session), good enough to see what you're doing while you rig.");
        }

        private static void PoseLimbOntoAnchor(Transform playerRoot, string boneName, Transform bikeRoot, string anchorName)
        {
            var bone = FindDeepByName(playerRoot, boneName);
            var anchor = FindDeepByName(bikeRoot, anchorName);
            if (bone == null || anchor == null)
            {
                Debug.LogWarning($"MINI-119 EDIT-MODE MOUNT: couldn't pose {boneName} onto {anchorName} - one or both not found.");
                return;
            }

            var ik = bone.gameObject.AddComponent<Gadd420.IK>();
            ik.chainLength = 2;
            ik.target = anchor;
            ik.iterations = 10;

            // Gadd420.IK has no [ExecuteInEditMode] - none of its
            // lifecycle methods run automatically outside Play Mode, not
            // even Awake() from AddComponent (that's a Play Mode-only
            // guarantee). Its Awake() is what actually allocates the
            // bone-chain arrays LateUpdate/DoIK read from - without
            // invoking it first, DoIK null-refs on those never-allocated
            // arrays.
            var awake = typeof(Gadd420.IK).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            awake?.Invoke(ik, null);

            var lateUpdate = typeof(Gadd420.IK).GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < 5; i++) lateUpdate?.Invoke(ik, null);

            Object.DestroyImmediate(ik);
        }

        private static Transform FindDeepByName(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                var found = FindDeepByName(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
