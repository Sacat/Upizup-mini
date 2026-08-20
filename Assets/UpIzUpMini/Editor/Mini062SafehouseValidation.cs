using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-062: proves the Lalay House's ownership gate, and the new
    /// "[4] Set Respawn" selectable-respawn mechanic (previously a single
    /// hardcoded farm-safehouse spawn baked at scene-build time with no
    /// in-game way to change it) - against the real built scene, not a
    /// duplicate in-memory setup. Rest/save/load themselves predate this
    /// task (MINI-013/MINI-042) and aren't re-proven here.
    /// </summary>
    public static class Mini062SafehouseValidation
    {
        [MenuItem("Up Iz Up Mini/MINI-062/Validate Safehouse Respawn")]
        public static void Validate()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            bool pass = true;
            string fail = null;

            var lalayGo = GameObject.Find("LalayHouse_Rest");
            var farmGo = GameObject.Find("FarmSafehouse_Rest");
            var economy = GameObject.Find("EconomyManager")?.GetComponent<EconomyManager>();
            var switcher = Object.FindFirstObjectByType<CharacterSwitchManager>();
            var deed = AssetDatabase.LoadAssetAtPath<ShopItemDefinition>("Assets/UpIzUpMini/Data/Shop/prop_safehouse.asset");

            if (lalayGo == null || farmGo == null || economy == null || switcher == null || deed == null)
            {
                Debug.LogError("MINI-062 VALIDATION FAIL: LalayHouse_Rest/FarmSafehouse_Rest/EconomyManager/CharacterSwitchManager/prop_safehouse.asset not found in the built scene.");
                return;
            }

            var lalay = lalayGo.GetComponent<SafehouseInteractable>();
            var farm = farmGo.GetComponent<SafehouseInteractable>();
            InvokeMethod(economy, "Awake");
            InvokeMethod(switcher, "Awake");

            // --- 1) Deed naming: no longer the stale "Montine" name for a
            // Lalay-area house. ---
            Check(ref pass, ref fail, deed.displayName == "Lalay House Deed",
                $"expected the deed's display name to be 'Lalay House Deed', got '{deed.displayName}'.");

            // --- 2) Locked before purchase: Interact refuses, SetRespawnHere refuses. ---
            var actor = new GameObject("Actor");
            lalay.Interact(actor);
            string fb = lalay.GetInteractionFeedback();
            Check(ref pass, ref fail, fb != null && fb.Contains("not yours"),
                $"expected an unowned Lalay House to refuse Interact(), got '{fb}'.");

            fb = lalay.SetRespawnHere();
            Check(ref pass, ref fail, fb.Contains("not yours"),
                $"expected SetRespawnHere() to refuse on an unowned house, got '{fb}'.");
            Check(ref pass, ref fail, switcher.RespawnLabel != "Lalay House",
                "expected the refused SetRespawnHere() to NOT have changed the active respawn label.");

            // --- 3) Farm safehouse always usable (no requiredItemId), and
            // is the default respawn point before any selection is made. ---
            if (pass)
            {
                Vector3 defaultRespawn = switcher.CurrentRespawnPoint;
                fb = farm.SetRespawnHere();
                Check(ref pass, ref fail, fb.Contains("wake up"),
                    $"expected the always-owned farm safehouse to accept SetRespawnHere(), got '{fb}'.");
                Check(ref pass, ref fail, switcher.CurrentRespawnPoint == defaultRespawn,
                    "expected explicitly selecting the farm safehouse to be a no-op on the respawn point (it was already the default).");
                Check(ref pass, ref fail, switcher.RespawnLabel == "Farm Safehouse",
                    $"expected RespawnLabel to read 'Farm Safehouse', got '{switcher.RespawnLabel}'.");
            }

            // --- 4) Buy the deed, then the Lalay House unlocks. ---
            if (pass)
            {
                economy.AddMoney(3000);
                bool purchased = economy.TryPurchase(deed, out string purchaseMsg);
                Check(ref pass, ref fail, purchased, $"expected the deed purchase to succeed, got '{purchaseMsg}'.");
                Check(ref pass, ref fail, economy.OwnsItem("prop_safehouse"), "expected OwnsItem(prop_safehouse) to be true after purchase.");
            }

            // --- 5) Selecting the now-owned Lalay House actually moves the
            // real respawn point CharacterSwitchManager will use. ---
            if (pass)
            {
                Vector3 farmRespawn = switcher.CurrentRespawnPoint;
                fb = lalay.SetRespawnHere();
                Check(ref pass, ref fail, fb.Contains("wake up"),
                    $"expected a now-owned Lalay House to accept SetRespawnHere(), got '{fb}'.");
                Check(ref pass, ref fail, switcher.RespawnLabel == "Lalay House",
                    $"expected RespawnLabel to read 'Lalay House' after selecting it, got '{switcher.RespawnLabel}'.");
                Check(ref pass, ref fail, switcher.CurrentRespawnPoint != farmRespawn,
                    "expected CurrentRespawnPoint to actually change after selecting a different house - it's the same point RespawnAtSafehouse() reads.");
                Check(ref pass, ref fail,
                    Vector3.Distance(switcher.CurrentRespawnPoint, lalayGo.transform.position) < 5f,
                    $"expected the selected respawn point to be near the Lalay House itself, got a distance of {Vector3.Distance(switcher.CurrentRespawnPoint, lalayGo.transform.position):F2}.");
            }

            // --- 6) Save/load round-trips the selection (not just the
            // in-memory CharacterSwitchManager field). ---
            if (pass)
            {
                var saveSystemGo = GameObject.Find("SaveLoadSystem");
                var saveSystem = saveSystemGo?.GetComponent<UpIzUpMini.SaveLoadSystem>();
                if (saveSystem == null)
                {
                    Debug.LogError("MINI-062 VALIDATION FAIL: SaveLoadSystem not found in the built scene.");
                    return;
                }
                InvokeMethod(saveSystem, "Awake");

                Vector3 selected = switcher.CurrentRespawnPoint;
                string selectedLabel = switcher.RespawnLabel;
                saveSystem.Save();

                // Simulate a fresh session losing the in-memory selection.
                switcher.LoadRespawnPoint(Vector3.zero, null); // no-op guard check first
                Check(ref pass, ref fail, switcher.CurrentRespawnPoint == selected,
                    "expected LoadRespawnPoint(zero) to be treated as 'no selection' and leave the current respawn point untouched.");

                saveSystem.Load();
                Check(ref pass, ref fail, switcher.CurrentRespawnPoint == selected,
                    $"expected a real Load() to restore the saved Lalay House respawn point, got {switcher.CurrentRespawnPoint} instead of {selected}.");
                Check(ref pass, ref fail, switcher.RespawnLabel == selectedLabel,
                    $"expected a real Load() to restore the saved respawn label '{selectedLabel}', got '{switcher.RespawnLabel}'.");
            }

            if (pass)
            {
                Debug.Log("MINI-062 SAFEHOUSE VALIDATION PASS: the Lalay House deed is correctly named and gates both Interact() and the new [4] Set Respawn action until bought; the always-owned farm safehouse is the default respawn point; buying the deed genuinely unlocks the house; selecting it actually moves CharacterSwitchManager's real respawn point (the one RespawnAtSafehouse() reads); and the selection round-trips through a real Save()/Load() cycle.");
            }
            else
            {
                Debug.LogError($"MINI-062 VALIDATION FAIL: {fail}");
            }
        }

        private static void Check(ref bool pass, ref string fail, bool condition, string message)
        {
            if (pass && !condition) { pass = false; fail = message; }
        }

        private static void InvokeMethod(object target, string name)
        {
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);
        }
    }
}
