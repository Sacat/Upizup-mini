// MINI-206 (cloud design lane) - written without a Unity Editor, NOT COMPILED HERE. Review before use.
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UpIzUpMini.Character;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// Turns the measured MINI-206 accessory anchors (Docs/CharacterPipeline/MINI-206/Accessories/&lt;Char&gt;_AccessoryPlacements.json,
    /// character frame: x right, y up, z forward, metres, bind pose) into NEW AccessoryPlacementProfile assets, converted to the
    /// bone's real local space in Unity. Select the character root (the object with the Animator, Sacat or Franki, in its
    /// default pose, not in Play Mode) and run the menu item. Existing approved profiles are never touched: assets are written to
    /// Assets/UpIzUpMini/Art/Characters/Accessories/MINI206/ with useManualPlacement = false until the owner approves them.
    /// </summary>
    public static class Mini206AccessoryProfiles
    {
        [Serializable] class V3 { public float x, y, z; public Vector3 V => new Vector3(x, y, z); }
        [Serializable] class Item { public string id, humanBodyBone, ccBone, note, measuredJson; public V3 anchorCharacter, offsetFromBone, eulerCharacter, localScale; }
        [Serializable] class File { public string character, frame; public Item[] items; }

        const string JsonDir = "Docs/CharacterPipeline/MINI-206/Accessories";
        const string OutDir = "Assets/UpIzUpMini/Art/Characters/Accessories/MINI206";

        [MenuItem("Up Iz Up Mini/MINI-206/Create Accessory Profiles For Selected Character")]
        public static void Create()
        {
            var root = Selection.activeGameObject;
            var animator = root ? root.GetComponentInChildren<Animator>() : null;
            if (animator == null || !animator.isHuman) { Debug.LogError("[MINI-206] select Sacat or Franki (humanoid Animator)"); return; }
            string name = root.name.Contains("Franki") ? "Franki" : "Sacat";
            string path = Path.Combine(JsonDir, name + "_AccessoryPlacements.json");
            if (!System.IO.File.Exists(path)) { Debug.LogError("[MINI-206] missing " + path); return; }
            var data = JsonUtility.FromJson<File>(System.IO.File.ReadAllText(path));
            Directory.CreateDirectory(OutDir);
            Transform character = animator.transform;
            foreach (var it in data.items)
            {
                if (!Enum.TryParse(it.humanBodyBone, out HumanBodyBones hb)) { Debug.LogWarning("[MINI-206] unknown bone " + it.humanBodyBone); continue; }
                Transform bone = animator.GetBoneTransform(hb);
                if (bone == null) { Debug.LogWarning("[MINI-206] bone not mapped: " + hb); continue; }
                // character frame -> world: offsets are measured from the bone head in the bind pose, in character axes
                Vector3 s = character.lossyScale;
                Vector3 off = it.offsetFromBone.V; off = new Vector3(off.x * s.x, off.y * s.y, off.z * s.z);
                Vector3 worldPos = bone.position + character.rotation * off;
                Quaternion worldRot = character.rotation * Quaternion.Euler(it.eulerCharacter.V);
                var p = ScriptableObject.CreateInstance<AccessoryPlacementProfile>();
                p.useManualPlacement = false;   // the owner flips this after checking it in the Scene view
                p.localPosition = bone.InverseTransformPoint(worldPos);
                p.localEulerAngles = (Quaternion.Inverse(bone.rotation) * worldRot).eulerAngles;
                Vector3 bs = bone.lossyScale;
                p.localScale = new Vector3(it.localScale.x / Mathf.Max(1e-4f, bs.x), it.localScale.y / Mathf.Max(1e-4f, bs.y), it.localScale.z / Mathf.Max(1e-4f, bs.z));
                string asset = $"{OutDir}/{name}_{it.id}_MINI206.asset";
                AssetDatabase.CreateAsset(p, AssetDatabase.GenerateUniqueAssetPath(asset));
                Debug.Log($"[MINI-206] {name} {it.id} on {hb}: localPosition {p.localPosition:F4} localEuler {p.localEulerAngles:F1} ({it.note})");
            }
            AssetDatabase.SaveAssets();
        }
    }
}
