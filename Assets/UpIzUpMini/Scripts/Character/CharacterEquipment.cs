using System.Collections.Generic;
using UnityEngine;
using UpIzUpMini.Economy;

namespace UpIzUpMini.Character
{
    /// <summary>
    /// Makes purchased apparel actually show on the character. Items are
    /// attached to Humanoid bones (head, chest, feet) via
    /// Animator.GetBoneTransform, which works because every character model
    /// was converted to a Humanoid rig in MINI-013 - so this needs no
    /// per-model bone names.
    ///
    /// Geometry is generated primitives rather than imported wearables:
    /// the project has no clothing assets, and a visible cap/chain/shades
    /// is more useful than nothing while remaining easy to swap for real
    /// meshes later.
    /// </summary>
    public class CharacterEquipment : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        private readonly Dictionary<string, GameObject> _spawned = new Dictionary<string, GameObject>();
        private bool _initialised;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        private void OnEnable()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnChanged += Refresh;
            }
        }

        private void OnDisable()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnChanged -= Refresh;
            }
        }

        private void Start() => Refresh();

        private void Refresh()
        {
            var economy = EconomyManager.Instance;
            if (economy == null || animator == null || !animator.isHuman) return;

            _initialised = true;

            Apply("cap_mike", HumanBodyBones.Head, economy.OwnsItem("cap_mike"),
                () => BuildCap(new Color(0.85f, 0.15f, 0.15f)));

            Apply("shades_ray", HumanBodyBones.Head, economy.OwnsItem("shades_ray"),
                () => BuildShades());

            Apply("chain_gold", HumanBodyBones.Chest, economy.OwnsItem("chain_gold"),
                () => BuildChain());

            Apply("watch_rollie", HumanBodyBones.LeftLowerArm, economy.OwnsItem("watch_rollie"),
                () => BuildWatch());

            // Clothing recolours the character's own garments rather than
            // adding geometry - the models default to black, so a bought
            // item visibly changes their outfit.
            ApplyGarment("shirt_lacos", new[] { "top", "tshirt", "shirt" }, new Color(0.90f, 0.94f, 0.96f));
            ApplyGarment("shorts_adibas", new[] { "bottom", "pants", "trouser" }, new Color(0.25f, 0.32f, 0.62f));
            ApplyGarment("shoes_mike", new[] { "shoes" }, new Color(0.95f, 0.95f, 0.95f));
            ApplyGarment("shoes_pumba", new[] { "shoes" }, new Color(0.85f, 0.25f, 0.20f));
        }

        private readonly System.Collections.Generic.Dictionary<Renderer, Material[]> _originalMaterials
            = new System.Collections.Generic.Dictionary<Renderer, Material[]>();

        /// <summary>
        /// Tints the character's existing garment material slots. Materials
        /// are cloned per-instance so only this character changes, and the
        /// originals are cached so an item can be removed cleanly.
        /// </summary>
        private void ApplyGarment(string itemId, string[] slotKeywords, Color color)
        {
            var economy = EconomyManager.Instance;
            if (economy == null || !economy.OwnsItem(itemId)) return;

            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (!_originalMaterials.ContainsKey(renderer))
                {
                    _originalMaterials[renderer] = renderer.sharedMaterials;
                }

                var mats = renderer.sharedMaterials;
                bool changed = false;

                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string n = mats[i].name.ToLowerInvariant();

                    bool match = false;
                    foreach (var keyword in slotKeywords)
                    {
                        if (n.Contains(keyword)) { match = true; break; }
                    }
                    if (!match) continue;

                    // Already the right colour - don't clone again every frame.
                    if (mats[i].color == color) continue;

                    mats[i] = new Material(mats[i]) { color = color };
                    changed = true;
                }

                if (changed) renderer.sharedMaterials = mats;
            }
        }

        private void Apply(string itemId, HumanBodyBones bone, bool owned, System.Func<GameObject> build)
        {
            bool present = _spawned.TryGetValue(itemId, out var existing) && existing != null;

            if (owned && !present)
            {
                Transform anchor = animator.GetBoneTransform(bone);
                if (anchor == null) return;

                var go = build();
                go.name = $"Equip_{itemId}";
                go.transform.SetParent(anchor, false);
                PositionOnBone(go.transform, itemId);
                _spawned[itemId] = go;
            }
            else if (!owned && present)
            {
                Destroy(existing);
                _spawned.Remove(itemId);
            }
        }

        private static void PositionOnBone(Transform t, string itemId)
        {
            switch (itemId)
            {
                case "cap_mike":
                    t.localPosition = new Vector3(0f, 0.16f, 0.01f);
                    t.localRotation = Quaternion.identity;
                    break;
                case "shades_ray":
                    t.localPosition = new Vector3(0f, 0.06f, 0.085f);
                    t.localRotation = Quaternion.identity;
                    break;
                case "chain_gold":
                    t.localPosition = new Vector3(0f, 0.14f, 0.08f);
                    t.localRotation = Quaternion.identity;
                    break;
                case "watch_rollie":
                    t.localPosition = new Vector3(0f, -0.22f, 0f);
                    t.localRotation = Quaternion.identity;
                    break;
            }
        }

        private static Material Mat(Color c)
        {
            var shader = Shader.Find("Standard");
            return new Material(shader) { color = c };
        }

        private static GameObject BuildCap(Color color)
        {
            var root = new GameObject("Cap");

            var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.transform.SetParent(root.transform, false);
            crown.transform.localScale = new Vector3(0.2f, 0.12f, 0.2f);
            crown.GetComponent<Renderer>().sharedMaterial = Mat(color);
            Destroy(crown.GetComponent<Collider>());

            var peak = GameObject.CreatePrimitive(PrimitiveType.Cube);
            peak.transform.SetParent(root.transform, false);
            peak.transform.localPosition = new Vector3(0f, -0.01f, 0.11f);
            peak.transform.localScale = new Vector3(0.18f, 0.02f, 0.12f);
            peak.GetComponent<Renderer>().sharedMaterial = Mat(color * 0.8f);
            Destroy(peak.GetComponent<Collider>());

            return root;
        }

        private static GameObject BuildShades()
        {
            var root = new GameObject("Shades");
            var lens = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lens.transform.SetParent(root.transform, false);
            lens.transform.localScale = new Vector3(0.17f, 0.045f, 0.02f);
            lens.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.05f, 0.05f, 0.07f));
            Destroy(lens.GetComponent<Collider>());
            return root;
        }

        private static GameObject BuildChain()
        {
            var root = new GameObject("Chain");
            var gold = Mat(new Color(0.95f, 0.78f, 0.2f));

            // Ring of small spheres approximating a chain.
            for (int i = 0; i < 10; i++)
            {
                float a = (i / 10f) * Mathf.PI * 2f;
                var link = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                link.transform.SetParent(root.transform, false);
                link.transform.localPosition = new Vector3(Mathf.Sin(a) * 0.075f, -Mathf.Abs(Mathf.Cos(a)) * 0.05f, Mathf.Cos(a) * 0.045f);
                link.transform.localScale = Vector3.one * 0.028f;
                link.GetComponent<Renderer>().sharedMaterial = gold;
                Destroy(link.GetComponent<Collider>());
            }

            var pendant = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pendant.transform.SetParent(root.transform, false);
            pendant.transform.localPosition = new Vector3(0f, -0.09f, 0.05f);
            pendant.transform.localScale = new Vector3(0.045f, 0.055f, 0.015f);
            pendant.GetComponent<Renderer>().sharedMaterial = gold;
            Destroy(pendant.GetComponent<Collider>());

            return root;
        }

        private static GameObject BuildWatch()
        {
            var root = new GameObject("Watch");
            var face = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            face.transform.SetParent(root.transform, false);
            face.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            face.transform.localScale = new Vector3(0.05f, 0.008f, 0.05f);
            face.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.9f, 0.85f, 0.5f));
            Destroy(face.GetComponent<Collider>());
            return root;
        }
    }
}
