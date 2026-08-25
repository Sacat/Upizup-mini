using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-119 follow-up, user: "when i press play the walking
    /// animation is happening." Real cause: PlayerController.IsControlled
    /// is a plain C# auto-property, not a Unity-serialized field - it
    /// never actually saved as false, so it silently reset to its
    /// default true the moment Play rebuilt the scene, and
    /// PlayerController resumed driving the Animator normally while
    /// Sacat was still parented/posed from the Edit-mode mount trick.
    /// That trick only ever works within one continuous Play session
    /// (walk + F), not across a save - reverts him to genuinely normal
    /// free-roam here instead, using the real Dismount() code path, so
    /// Play starts clean. Does NOT touch the manually-built Rig 1 /
    /// constraints on Visual - those are real serialized references,
    /// untouched by this.</summary>
    public static class Mini119EditModeDismountSetup
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Edit-Mode Dismount Sacat (one-off)")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var player = GameObject.Find("Sacat");
            if (player == null) { Debug.LogError("MINI-119 EDIT-MODE DISMOUNT FAIL: Sacat not found."); return; }

            var bike = GameObject.Find("StockDemoSuperMoto");
            var interactable = bike != null ? bike.GetComponent<SuperMotoStockInteractable>() : null;
            if (interactable == null)
            {
                Debug.LogWarning("MINI-119 EDIT-MODE DISMOUNT: no SuperMotoStockInteractable found (already dismounted / not wired?) - falling back to a direct manual reset.");
                ManualReset(player);
            }
            else
            {
                var dismountField = typeof(SuperMotoStockInteractable).GetField("_mountedPlayer", BindingFlags.NonPublic | BindingFlags.Instance);
                var mountedPlayer = dismountField?.GetValue(interactable) as GameObject;
                if (mountedPlayer == null)
                {
                    Debug.LogWarning("MINI-119 EDIT-MODE DISMOUNT: interactable's HasRider is already false - falling back to a direct manual reset in case Sacat is still parented from a broken state.");
                    ManualReset(player);
                }
                else
                {
                    var dismountMethod = typeof(SuperMotoStockInteractable).GetMethod("Dismount", BindingFlags.NonPublic | BindingFlags.Instance);
                    try { dismountMethod.Invoke(interactable, null); }
                    catch (TargetInvocationException ex)
                    {
                        Debug.LogError($"MINI-119 EDIT-MODE DISMOUNT: real Dismount() threw: {ex.InnerException} - falling back to a direct manual reset.");
                        ManualReset(player);
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"MINI-119 EDIT-MODE DISMOUNT OK: Sacat back to free-roam and saved. worldPos={player.transform.position}, parent={(player.transform.parent != null ? player.transform.parent.name : "none")}. Your manually-built Rig 1/constraints on Visual are untouched.");
        }

        /// <summary>Belt-and-suspenders manual reset, in case the real
        /// Dismount() can't find him properly parented (e.g. the earlier
        /// Edit-mode mount left him in a state its own bookkeeping
        /// doesn't recognize).</summary>
        private static void ManualReset(GameObject player)
        {
            player.transform.SetParent(null, true);
            player.transform.localScale = Vector3.one;

            var characterController = player.GetComponent<CharacterController>();
            if (characterController != null) characterController.enabled = true;

            var animator = player.GetComponent<Animator>();
            if (animator != null) animator.speed = 1f;

            // IsControlled never actually needed resetting (it's not
            // serialized - see this file's own header), but set it
            // anyway for a live in-Editor Play test right after this
            // tool runs, before any save/reload cycle.
            var playerController = player.GetComponent<UpIzUpMini.Character.PlayerController>();
            if (playerController != null) playerController.IsControlled = true;
        }
    }
}
