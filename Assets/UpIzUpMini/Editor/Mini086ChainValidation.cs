using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;

namespace UpIzUpMini.EditorTools
{
    public static class Mini086ChainValidation
    {
        private const string ScenePath = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        private const string ProfilePath = "Assets/UpIzUpMini/Data/Equipment/SacatChainPlacement.asset";

        [MenuItem("Up Iz Up Mini/MINI-086/Validate Approved Chain Distribution")]
        public static void Validate()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            bool pass = true;
            string failure = null;

            var profile = AssetDatabase.LoadAssetAtPath<AccessoryPlacementProfile>(ProfilePath);
            Check(ref pass, ref failure, profile != null && profile.useManualPlacement,
                "Sacat's approved manual placement profile is missing or disabled.");
            Check(ref pass, ref failure, profile != null && profile.fittedChildren != null && profile.fittedChildren.Count > 0,
                "Sacat's fitted child-chain transforms were not captured.");

            var sacat = GameObject.Find("Sacat");
            var franki = GameObject.Find("Franki");
            var bossC = GameObject.Find("NPC_BossC");
            var bossJ = GameObject.Find("NPC_BossJ");
            Check(ref pass, ref failure, sacat != null && franki != null, "Sacat or Franki is missing.");
            Check(ref pass, ref failure, bossC != null && FindChild(bossC.transform, "BossChain_18k") != null,
                "Boss C does not wear the cleaned GoldChain18k prefab.");
            Check(ref pass, ref failure, bossJ != null && FindChild(bossJ.transform, "BossChain_18k") == null,
                "Boss J still has a BossChain_18k child.");

            var sacatEquipment = sacat != null ? sacat.GetComponent<CharacterEquipment>() : null;
            var frankiEquipment = franki != null ? franki.GetComponent<CharacterEquipment>() : null;
            Check(ref pass, ref failure, sacatEquipment != null && frankiEquipment != null,
                "CharacterEquipment is missing from a protagonist.");
            if (sacatEquipment != null)
            {
                var so = new SerializedObject(sacatEquipment);
                Check(ref pass, ref failure, so.FindProperty("chainPlacement").objectReferenceValue == profile,
                    "Sacat is not wired to the approved chain profile.");
                Check(ref pass, ref failure, so.FindProperty("characterIndex").intValue == 0,
                    "Sacat's wearable owner index is not 0.");
            }
            if (frankiEquipment != null)
            {
                var so = new SerializedObject(frankiEquipment);
                Check(ref pass, ref failure, so.FindProperty("characterIndex").intValue == 1,
                    "Franki's wearable owner index is not 1.");
                Check(ref pass, ref failure, so.FindProperty("chainPrefab").objectReferenceValue != null,
                    "Franki cannot display the purchased real chain.");
            }

            // Exercise the exact transaction path that failed for the user:
            // Sacat is active, buys the chain, the UI's deterministic refresh
            // runs, and only Sacat receives Equip_chain_gold.
            var economy = Object.FindFirstObjectByType<EconomyManager>();
            var switcher = Object.FindFirstObjectByType<CharacterSwitchManager>();
            var item = AssetDatabase.LoadAssetAtPath<ShopItemDefinition>("Assets/UpIzUpMini/Data/Shop/chain_gold.asset");
            InvokeAwake(economy);
            InvokeAwake(switcher);
            economy?.SetMoney(10000);
            bool bought = economy != null && item != null && economy.TryPurchase(item, out _);
            sacatEquipment?.RefreshEquipment();
            Check(ref pass, ref failure, bought, "The chain purchase transaction failed in validation.");
            Check(ref pass, ref failure, sacat != null && FindChild(sacat.transform, "Equip_chain_gold") != null,
                "Sacat bought the chain but Equip_chain_gold did not appear.");
            var equippedChain = sacat != null ? FindChild(sacat.transform, "Equip_chain_gold") : null;
            if (equippedChain != null && profile != null && profile.fittedChildren != null)
            {
                foreach (var pose in profile.fittedChildren)
                {
                    var fitted = equippedChain.Find(pose.relativePath);
                    Check(ref pass, ref failure, fitted != null,
                        $"Purchased chain is missing fitted child '{pose.relativePath}'.");
                    if (fitted == null) break;
                    Check(ref pass, ref failure,
                        Vector3.Distance(fitted.localPosition, pose.localPosition) < 0.0001f
                        && Quaternion.Angle(fitted.localRotation, Quaternion.Euler(pose.localEulerAngles)) < 0.01f
                        && Vector3.Distance(fitted.localScale, pose.localScale) < 0.0001f,
                        $"Purchased chain did not reproduce the approved fit for '{pose.relativePath}'.");
                    if (!pass) break;
                }
            }
            Check(ref pass, ref failure, franki != null && FindChild(franki.transform, "Equip_chain_gold") == null,
                "Buying Sacat's chain incorrectly equipped Franki too.");

            if (pass)
                Debug.Log("MINI-086 CHAIN VALIDATION PASS: the approved Sacat root and fitted child transforms are reproduced on purchase, only Sacat equips it, Boss C wears the cleaned real chain, and Boss J has none.");
            else
                throw new System.InvalidOperationException("MINI-086 CHAIN VALIDATION FAIL: " + failure);
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }

        private static void InvokeAwake(object target)
        {
            if (target == null) return;
            target.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);
        }

        private static void Check(ref bool pass, ref string failure, bool condition, string message)
        {
            if (pass && !condition) { pass = false; failure = message; }
        }
    }
}
