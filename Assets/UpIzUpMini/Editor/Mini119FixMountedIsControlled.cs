using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-119 follow-up, user: "i liked that you put him on
    /// the bike but its just the walking animation that was going on
    /// while he was on." Doesn't dismount him (he stays exactly where
    /// he is, seated on the bike) - just re-applies IsControlled=false
    /// now that PlayerController.IsControlled is actually a serialized
    /// field (see that fix's own comment), so it finally sticks in the
    /// saved scene instead of silently resetting to true on the next
    /// Play. That's what was letting PlayerController.Update() keep
    /// driving the Animator's Speed parameter every frame, fighting the
    /// frozen/seated pose - stopping that is what stops the walking
    /// animation while he's parked on the bike.</summary>
    public static class Mini119FixMountedIsControlled
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Fix Mounted Sacat's IsControlled (one-off)")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var player = GameObject.Find("Sacat");
            if (player == null) { Debug.LogError("MINI-119 FIX ISCONTROLLED FAIL: Sacat not found."); return; }

            var playerController = player.GetComponent<PlayerController>();
            if (playerController == null) { Debug.LogError("MINI-119 FIX ISCONTROLLED FAIL: no PlayerController on Sacat."); return; }

            playerController.IsControlled = false;

            var characterController = player.GetComponent<CharacterController>();
            if (characterController != null) characterController.enabled = false;

            // MINI-119 follow-up, user: "there is some type of bycicle
            // animation going on... looks like he is ridding a bycicle."
            // Same class of problem as IsControlled - the Animator's own
            // runtime parameter state can't be saved either. Persists a
            // real component instead (MountedAnimatorFreeze), whose
            // Awake() re-applies the freeze automatically every Play
            // session from here on.
            if (player.GetComponent<MountedAnimatorFreeze>() == null)
                player.AddComponent<MountedAnimatorFreeze>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"MINI-119 FIX ISCONTROLLED OK: IsControlled=false and MountedAnimatorFreeze saved. Sacat left exactly where he was - parented={player.transform.parent?.name ?? "none"}, worldPos={player.transform.position}.");
        }
    }
}
