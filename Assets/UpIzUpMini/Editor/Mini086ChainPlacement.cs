using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UpIzUpMini.Character;

namespace UpIzUpMini.EditorTools
{
    public static class Mini086ChainPlacement
    {
        private const string PreviewName = "MANUAL_CHAIN_PREVIEW_SACAT";
        private const string ScenePath = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        private const string ProfilePath = "Assets/UpIzUpMini/Data/Equipment/SacatChainPlacement.asset";
        private const string ChainPath = "Assets/UpIzUpMini/Art/Accessories/GoldChain18k.prefab";

        [MenuItem("Up Iz Up Mini/MINI-086/Prepare Sacat Chain For Manual Placement")]
        public static void PrepareSacatChain()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var sacat = GameObject.Find("Sacat");
            var animator = sacat != null ? sacat.GetComponentInChildren<Animator>() : null;
            var chest = animator != null ? animator.GetBoneTransform(HumanBodyBones.Chest) : null;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChainPath);
            if (sacat == null || chest == null || prefab == null)
                throw new System.InvalidOperationException("Sacat, Chest bone, or GoldChain18k prefab is missing. Rebuild GrandBayProof first.");

            var old = GameObject.Find(PreviewName);
            if (old != null) Object.DestroyImmediate(old);

            var preview = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            preview.name = PreviewName;
            preview.tag = "EditorOnly";
            preview.transform.SetParent(chest, false);
            preview.transform.rotation = sacat.transform.rotation * Quaternion.Euler(CharacterEquipment.ChainTilt, 0f, 0f);
            preview.transform.position = chest.position
                + sacat.transform.forward * CharacterEquipment.ChainForward
                + sacat.transform.up * CharacterEquipment.ChainUp
                + sacat.transform.right * CharacterEquipment.ChainSide;
            CharacterEquipment.NormaliseAccessoryScale(preview.transform, chest, CharacterEquipment.ChainWidth);

            Selection.activeGameObject = preview;
            SceneView.lastActiveSceneView?.FrameSelected();
            EditorSceneManager.MarkSceneDirty(sacat.scene);
            EditorSceneManager.SaveScene(sacat.scene);
            Debug.Log("MINI-086: Sacat chain preview selected. Move/rotate/scale it, then run Capture Selected Sacat Chain Placement.");
        }

        [MenuItem("Up Iz Up Mini/MINI-086/Capture Selected Sacat Chain Placement")]
        public static void CaptureSacatChain()
        {
            var preview = Selection.activeGameObject;
            if (preview == null || preview.name != PreviewName)
                throw new System.InvalidOperationException($"Select {PreviewName} first.");

            EnsureFolder("Assets/UpIzUpMini/Data");
            EnsureFolder("Assets/UpIzUpMini/Data/Equipment");
            var profile = AssetDatabase.LoadAssetAtPath<AccessoryPlacementProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<AccessoryPlacementProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            profile.useManualPlacement = true;
            profile.localPosition = preview.transform.localPosition;
            profile.localEulerAngles = preview.transform.localEulerAngles;
            profile.localScale = preview.transform.localScale;
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            Debug.Log($"MINI-086: captured user chain placement to {ProfilePath}. Rebuild the scene to wire it into Sacat.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            string name = path.Substring(slash + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
