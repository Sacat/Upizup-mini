using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UpIzUpMini.Character;
using UpIzUpMini.Combat;
using UpIzUpMini.Economy;
using UpIzUpMini.UI;

namespace UpIzUpMini.EditorTools
{
    /// <summary>Targeted MINI-183 scene patch. Never invokes the whole-world builder.</summary>
    public static class Mini183FirearmSetup
    {
        const string ScenePath = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string DataPath = "Assets/UpIzUpMini/Data/Shop/Mini183";

        [MenuItem("Up Iz Up Mini/MINI-183/Apply Firearms")]
        public static void Apply()
        {
            EnsureFolder(DataPath);
            var sidearm = Item("LalaySidearm", FirearmController.SidearmId, "Lalay Tool", ShopCategory.Firearm, 750, 12);
            var rounds = Item("SidearmRounds", "sidearm_ammo_12", "12 Sidearm Rounds", ShopCategory.Ammunition, 70, 12);
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var shops = UnityEngine.Object.FindObjectsByType<ShopPanelController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var trader = shops.FirstOrDefault(shop =>
            {
                var property = new SerializedObject(shop).FindProperty("panel");
                return property.objectReferenceValue != null && property.objectReferenceValue.name == "BlackMarketPanel";
            });
            if (trader == null) throw new InvalidOperationException("BlackMarketPanel shop was not found; scene was not saved.");

            var shopData = new SerializedObject(trader);
            var resale = shopData.FindProperty("resaleMode");
            var stock = shopData.FindProperty("stock");
            var alternate = shopData.FindProperty("alternateStock");
            if (alternate.arraySize == 0)
            {
                alternate.arraySize = stock.arraySize;
                for (int i = 0; i < stock.arraySize; i++)
                    alternate.GetArrayElementAtIndex(i).objectReferenceValue = stock.GetArrayElementAtIndex(i).objectReferenceValue;
            }
            stock.arraySize = 2;
            stock.GetArrayElementAtIndex(0).objectReferenceValue = sidearm;
            stock.GetArrayElementAtIndex(1).objectReferenceValue = rounds;
            resale.boolValue = false;
            shopData.FindProperty("alternateResaleMode").boolValue = true;
            shopData.FindProperty("shopTitle").stringValue = "BLACK MARKET - UNDER THE TABLE";
            shopData.ApplyModifiedPropertiesWithoutUndo();

            var players = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (players.Length != 2) throw new InvalidOperationException($"Expected two players, found {players.Length}; scene was not saved.");
            foreach (var player in players)
            {
                var controller = player.GetComponent<FirearmController>() ?? player.gameObject.AddComponent<FirearmController>();
                var animator = player.GetComponentInChildren<Animator>(true);
                if (animator == null || !animator.isHuman) throw new InvalidOperationException($"Humanoid Animator missing for {player.name}");
                var pose = animator.GetComponent<FirearmPose>() ?? animator.gameObject.AddComponent<FirearmPose>();
                var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                if (hand == null) throw new InvalidOperationException($"Right hand bone missing for {player.name}");
                var model = player.transform.Find("Mini183_Sidearm") ?? hand.Find("Mini183_Sidearm") ?? new GameObject("Mini183_Sidearm").transform;
                model.SetParent(player.transform, false);
                model.localPosition = Vector3.zero;
                model.localRotation = Quaternion.identity;
                model.localScale = Vector3.one;
                if (model.childCount == 0)
                {
                    Part(model, "Slide", new Vector3(0f, .015f, .09f), new Vector3(.05f, .055f, .18f), new Color(.16f, .18f, .2f));
                    Part(model, "Grip", new Vector3(0f, -.07f, .03f), new Vector3(.046f, .13f, .055f), new Color(.12f, .11f, .1f));
                    Part(model, "Muzzle", new Vector3(0f, .015f, .19f), new Vector3(.034f, .035f, .04f), new Color(.06f, .07f, .08f));
                }
                var muzzle = model.Find("ShotOrigin") ?? new GameObject("ShotOrigin").transform;
                muzzle.SetParent(model, false);
                muzzle.localPosition = new Vector3(0f, .015f, .22f);
                var renderers = model.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers) renderer.enabled = false;
                var data = new SerializedObject(controller);
                data.FindProperty("pose").objectReferenceValue = pose;
                data.FindProperty("muzzle").objectReferenceValue = muzzle;
                data.FindProperty("weaponRoot").objectReferenceValue = model;
                var renderProp = data.FindProperty("heldWeaponRenderers");
                renderProp.arraySize = renderers.Length;
                for (int i = 0; i < renderers.Length; i++) renderProp.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("MINI-183 APPLY PASS: black market stock, resale view, two players, sidearm pose and placeholder models saved.");
        }

        [MenuItem("Up Iz Up Mini/MINI-183/Verify Firearms")]
        public static void Verify()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var players = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (players.Length != 2 || players.Any(p => p.GetComponent<FirearmController>() == null || p.GetComponentInChildren<FirearmPose>(true) == null
                || p.transform.Find("Mini183_Sidearm") == null))
                throw new InvalidOperationException("MINI-183 player wiring failed.");
            var shops = UnityEngine.Object.FindObjectsByType<ShopPanelController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (!shops.Any(s => s.Stock != null && s.Stock.Length == 2 && s.Stock[0] != null && s.Stock[0].itemId == FirearmController.SidearmId))
                throw new InvalidOperationException("MINI-183 black market stock failed.");
            Debug.Log("MINI-183 VERIFY PASS: two controllers and sidearm stock in GrandBayProof.");
        }

        static ShopItemDefinition Item(string file, string id, string display, ShopCategory category, int price, int quantity)
        {
            string path = $"{DataPath}/{file}.asset";
            var item = AssetDatabase.LoadAssetAtPath<ShopItemDefinition>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ShopItemDefinition>();
                AssetDatabase.CreateAsset(item, path);
            }
            item.itemId = id;
            item.displayName = display;
            item.category = category;
            item.price = price;
            item.ammoId = FirearmController.AmmoId;
            item.ammoQuantity = quantity;
            EditorUtility.SetDirty(item);
            return item;
        }

        static void Part(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            part.GetComponent<Renderer>().sharedMaterial = material;
        }

        static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (var part in path.Split('/').Skip(1))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }
    }
}
