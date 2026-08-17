using System.Collections.Generic;
using UnityEngine;
using UpIzUpMini.Economy;

namespace UpIzUpMini.Character
{
    /// <summary>
    /// MINI-064. Slot-based equipment system (the researched "boxes then
    /// customise" pattern). Reads which wearables the player owns, builds a
    /// bone-name -> Transform map from the Humanoid rig once, and shows only
    /// the owned item per body slot (equipping hides any other item on the
    /// same slot). Data-driven via WearableDefinition, so the chain, shades,
    /// cap, watch, and future garments all place correctly without per-item
    /// hardcoding.
    ///
    /// This supersedes the piecemeal, hardcoded CharacterEquipment spawning
    /// (which placed the chain with scattered spheres that read wrong).
    /// CharacterEquipment is left in place for the garment recolours it
    /// handles, but bone-attached wearables move here.
    /// </summary>
    public class EquipmentSystem : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [Tooltip("All wearables the player can own for this character. Ownership checked against EconomyManager.")]
        [SerializeField] private WearableDefinition[] catalog;

        // Atlas of this character's bone transforms by name, so a wearable's
        // bone (a Humanoid bone enum -> name) resolves to the real transform.
        private readonly Dictionary<string, Transform> _boneMap = new Dictionary<string, Transform>();
        private readonly Dictionary<int, GameObject> _perSlot = new Dictionary<int, GameObject>();

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        private void OnEnable()
        {
            if (EconomyManager.Instance != null) EconomyManager.Instance.OnChanged += Refresh;
        }

        private void OnDisable()
        {
            if (EconomyManager.Instance != null) EconomyManager.Instance.OnChanged -= Refresh;
        }

        private void Start()
        {
            BuildBoneMap();
            Refresh();
        }

        /// <summary>Cache this character's bone transforms by name once.</summary>
        private void BuildBoneMap()
        {
            _boneMap.Clear();
            if (animator == null || !animator.isHuman) return;

            // Probe the common Humanoid bones we attach to.
            HumanBodyBones[] bones = {
                HumanBodyBones.Head, HumanBodyBones.Chest, HumanBodyBones.UpperChest,
                HumanBodyBones.Neck, HumanBodyBones.Hips, HumanBodyBones.Spine,
                HumanBodyBones.LeftLowerArm, HumanBodyBones.RightLowerArm,
                HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot,
                HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg,
            };
            foreach (var b in bones)
            {
                var t = animator.GetBoneTransform(b);
                if (t != null && !_boneMap.ContainsKey(t.name)) _boneMap[t.name] = t;
                else if (t != null) _boneMap[t.name] = t;
            }
        }

        /// <summary>Called on start and whenever the economy (owned items) changes.</summary>
        private void Refresh()
        {
            if (catalog == null) return;
            var economy = EconomyManager.Instance;
            if (economy == null || animator == null) return;

            // Track which slots got a fresh item this pass.
            var touched = new HashSet<int>();

            foreach (var wearable in catalog)
            {
                if (wearable == null) continue;
                bool owned = !string.IsNullOrEmpty(wearable.requiresItemId)
                    && economy.OwnsItem(wearable.requiresItemId);
                int slot = (int)wearable.slot;
                touched.Add(slot);

                bool shown = _perSlot.TryGetValue(slot, out var existing) && existing != null;
                if (owned && !shown)
                {
                    Transform anchor = ResolveBone(wearable.bone);
                    if (anchor == null) continue;
                    var go = BuildWearable(wearable);
                    go.name = $"Wear_{wearable.requiresItemId}";
                    go.transform.SetParent(anchor, false);
                    go.transform.localPosition = wearable.localPosition;
                    go.transform.localRotation = Quaternion.Euler(wearable.localEulerAngles);
                    go.transform.localScale = wearable.localScale;
                    if (wearable.swings)
                    {
                        var swing = go.AddComponent<AccessorySwing>();
                        swing.Initialize(anchor, go.transform.localPosition);
                    }
                    _perSlot[slot] = go;
                }
                else if (!owned && shown)
                {
                    Destroy(existing);
                    _perSlot.Remove(slot);
                }
            }

            // Hide any per-slot item not refreshed this pass (slots with no
            // owned catalog entry cleared).
            foreach (var pair in new List<KeyValuePair<int, GameObject>>(_perSlot))
            {
                if (!touched.Contains(pair.Key) && pair.Value != null)
                {
                    Destroy(pair.Value);
                    _perSlot.Remove(pair.Key);
                }
            }
        }

        private Transform ResolveBone(HumanBodyBones bone)
        {
            if (animator == null) return null;
            var t = animator.GetBoneTransform(bone);
            if (t != null) return t;
            // Fallback: match by the bone name in our map.
            if (_boneMap.Count > 0)
            {
                string name = (bone.ToString());
                // HumanBodyBones enum names differ from mixamo names; match
                // common mixamo bone names by suffix.
                foreach (var kv in _boneMap)
                {
                    if (kv.Key.Contains(name, System.StringComparison.OrdinalIgnoreCase)
                        || name.Contains(kv.Key, System.StringComparison.OrdinalIgnoreCase))
                        return kv.Value;
                }
            }
            return null;
        }

        private static GameObject BuildWearable(WearableDefinition def)
        {
            var root = new GameObject("Wear");

            if (def.mesh != null)
            {
                var mf = root.AddComponent<MeshFilter>();
                mf.sharedMesh = def.mesh;
                var mr = root.AddComponent<MeshRenderer>();
                if (def.material != null) mr.sharedMaterial = def.material;
                return root;
            }

            // Use a single shared gold material for all chain parts. Creating
            // many new Material(Shader.Find("Standard")) instances can yield
            // pink/magenta in some render paths - one material, assigned to
            // everything, avoids that entirely.
            var gold = new Material(Shader.Find("Standard")) { color = new Color(0.96f, 0.80f, 0.25f) };

            // Swinging items are chains: build a dense puff-mariner look
            // (thick overlapping gold rows in a "U" around the neckline,
            // plus a hanging leaf pendant). Sits HIGH on the chest and hugs
            // the torso rather than floating low/wide.
            if (def.swings && def.requiresItemId.Contains("chain"))
            {
                // 4 dense strands, each a thick row that starts at the left
                // shoulder line, dips to the centre, and up the right - a
                // true mariner "puff". More links + overlap = looks solid.
                int strands = 4;
                int per = 15;
                float widthX = 0.10f;   // tight across the upper pecs
                float topY = 0.06f;     // collar-height in Chest-bone space
                for (int s = 0; s < strands; s++)
                {
                    float rowY = topY - 0.02f * s;   // stack downward from collar
                    for (int i = 0; i < per; i++)
                    {
                        float f = (i / (float)(per - 1)) * 2f - 1f; // -1..1
                        var link = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        link.transform.SetParent(root.transform, false);
                        float x = f * widthX;
                        // Gentle U: stays near the collar, mild centre sag
                        // so it follows the upper pec, not the abdomen.
                        float arc = topY - (0.045f * (1f - f * f));
                        float z = 0.04f + (0.015f * (1f - f * f)); // slight forward bulge
                        link.transform.localPosition = new Vector3(x, arc + rowY - topY, z);
                        link.transform.localScale = Vector3.one * 0.043f;
                        link.GetComponent<Renderer>().sharedMaterial = gold;
                        Object.Destroy(link.GetComponent<Collider>());
                    }
                }
                // Distinct hanging leaf pendant at the centre bottom, lower
                // than the chain rows so it reads as a drop.
                var pendant = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pendant.transform.SetParent(root.transform, false);
                pendant.transform.localPosition = new Vector3(0f, -0.14f, 0.08f);
                pendant.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                pendant.transform.localScale = new Vector3(0.038f, 0.085f, 0.024f);
                pendant.GetComponent<Renderer>().sharedMaterial = gold;
                Object.Destroy(pendant.GetComponent<Collider>());

                var pendantLink = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pendantLink.transform.SetParent(root.transform, false);
                pendantLink.transform.localPosition = new Vector3(0f, -0.09f, 0.075f);
                pendantLink.transform.localScale = Vector3.one * 0.032f;
                pendantLink.GetComponent<Renderer>().sharedMaterial = gold;
                Object.Destroy(pendantLink.GetComponent<Collider>());
                return root;
            }

            // Other wearables: a small, clearly-visible marker (not a giant
            // cube). The caller applies localScale.
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.transform.SetParent(root.transform, false);
            marker.GetComponent<Renderer>().sharedMaterial = gold;
            Object.Destroy(marker.GetComponent<Collider>());
            return root;
        }
    }
}
