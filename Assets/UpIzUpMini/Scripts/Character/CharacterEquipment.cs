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
        [SerializeField] private int characterIndex;
        [SerializeField] private AccessoryPlacementProfile chainPlacement;

        /// <summary>
        /// MINI-067 chain placement, relative to the CHEST bone but expressed in
        /// the CHARACTER's own frame (forward/up), never the bone's local axes -
        /// this rig's bone rest orientations are not world-aligned.
        ///
        /// Both numbers were found by rendering a sweep and looking, not
        /// guessed. Forward has to clear a bulky torso (anything under ~0.14 is
        /// swallowed by the sweater), and the chain hangs DOWNWARD from where it
        /// is pinned, so "up" is really how far above the chest bone the collar
        /// sits - pinned at the bone itself the loop ends up round the belly.
        /// </summary>
        public const float ChainForward = 0.044f;
        public const float ChainUp = 0.383f;
        public const float ChainSide = -0.032f;

        /// <summary>Pitch about the character's right axis. The model is a FLAT
        /// loop, so tilting it is what lets the top arc sit behind the neck
        /// while the bottom still hangs clear of a chest that curves outward.</summary>
        public const float ChainTilt = -25.6f;

        /// <summary>
        /// Boss C is a different build on a different rig, so he gets his own
        /// numbers rather than being forced through the player's. Sharing one
        /// set put his chain in the wrong place the moment the player's was
        /// tuned.
        /// </summary>
        public const float BossChainForward = 0.195f;
        public const float BossChainUp = 0.356f;
        public const float BossChainTilt = -6.1f;

        /// <summary>
        /// Intended on-screen width of the chain, in metres.
        ///
        /// Enforced explicitly because the accessory is parented to a BONE, and
        /// bone scale differs wildly between rigs - measured 0.308m on the
        /// player against 0.068m on Boss C from the very same prefab. Left
        /// alone, the same necklace is a chunky chain on one character and a
        /// bracelet on another.
        /// </summary>
        public const float ChainWidth = 0.27f;

        /// <summary>
        /// Cancels out an anchor bone's own scale so an accessory ends up the
        /// intended size on any rig. Call after parenting.
        /// </summary>
        public static void NormaliseAccessoryScale(Transform accessory, Transform anchor, float targetWidth)
        {
            var mf = accessory.GetComponentInChildren<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return;

            // Measured from the MESH's own local bounds times the world scale,
            // never from Renderer.bounds. Renderer.bounds is a world-axis-aligned
            // box, so once the chain is rotated - and it is, differently on every
            // rig - its X extent is the chain's THICKNESS rather than its width,
            // and the correction comes out wildly wrong. Boss C sits on a Blender
            // "metarig" whose bones are oriented nothing like the player's
            // mixamorig, which is exactly how this surfaced.
            float localWidth = mf.sharedMesh.bounds.size.x;
            if (localWidth <= 0.0001f) return;

            float worldWidth = localWidth * mf.transform.lossyScale.x;
            if (worldWidth <= 0.0001f) return;

            float k = targetWidth / worldWidth;
            Vector3 ls = accessory.localScale;
            accessory.localScale = new Vector3(ls.x * k, ls.y * k, ls.z * k);
        }

        [Tooltip("MINI-067: the real 18k gold chain model. When set, it replaces the generated ring-of-spheres placeholder BuildChain still provides as a fallback, so a character with no prefab wired keeps working rather than wearing nothing.")]
        [SerializeField] private GameObject chainPrefab;

        [Tooltip("Item ids this character wears REGARDLESS of what the player owns - for NPCs whose look is part of their character rather than a purchase. Boss C wears the gold chain because he is the man who already has one; it is not bought, and selling yours must not strip his.")]
        [SerializeField] private string[] alwaysEquipped = new string[0];

        private readonly Dictionary<string, GameObject> _spawned = new Dictionary<string, GameObject>();
        private bool _initialised;

        // MINI-080: real bug - "when i bought a chain it did not show up on
        // my chest". OnEnable only subscribed to EconomyManager.OnChanged if
        // Instance already existed AT THAT EXACT MOMENT. Unity gives no
        // guarantee which component's Awake/OnEnable runs first, and neither
        // class had a [DefaultExecutionOrder] pinning it - so on any run
        // where EconomyManager.Awake() (which sets Instance) happened to run
        // AFTER this OnEnable, the subscription silently never happened.
        // Refresh() then only ever ran once, from Start() - before anything
        // was owned - and never again for the rest of the session, no matter
        // what got bought. Confirmed with a purchase-simulation script:
        // TryPurchase succeeded, money was deducted, but no Equip_chain_gold
        // ever appeared.
        //
        // Fixed by tracking whether the subscription actually happened, and
        // retrying in Start() - Unity guarantees ALL Awake() calls in the
        // scene finish before ANY Start() call runs, so by Start() time
        // EconomyManager.Instance is guaranteed set regardless of component
        // order. OnEnable is kept too (not replaced) so a real enable/disable
        // cycle later in the game still re-subscribes correctly.
        private bool _subscribed;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        private void OnEnable() => TrySubscribe();

        private void OnDisable()
        {
            if (_subscribed && EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnChanged -= Refresh;
            }
            _subscribed = false;
        }

        private void TrySubscribe()
        {
            if (_subscribed || EconomyManager.Instance == null) return;
            EconomyManager.Instance.OnChanged += Refresh;
            _subscribed = true;
        }

        private void Start()
        {
            TrySubscribe();   // fallback for the OnEnable-ran-too-early race
            Refresh();
        }

        private void Refresh()
        {
            var economy = EconomyManager.Instance;
            if (animator == null || !animator.isHuman) return;
            // An always-equipped NPC must still get dressed when there is no
            // economy at all (a test scene, or a scene loaded before the
            // manager wakes) - only the OWNED items genuinely need one.
            if (economy == null && (alwaysEquipped == null || alwaysEquipped.Length == 0)) return;

            _initialised = true;

            // Restore authored garments first so sold clothing disappears,
            // then apply whatever is still owned.
            foreach (var pair in _originalMaterials)
            {
                if (pair.Key != null) pair.Key.sharedMaterials = pair.Value;
            }

            Apply("cap_mike", HumanBodyBones.Head, Has(economy, "cap_mike"),
                () => BuildCap(new Color(0.85f, 0.15f, 0.15f)));

            Apply("shades_ray", HumanBodyBones.Head, Has(economy, "shades_ray"),
                () => BuildShades());

            Apply("chain_gold", HumanBodyBones.Chest, Has(economy, "chain_gold"),
                () => BuildChain());

            Apply("watch_rollie", HumanBodyBones.LeftLowerArm, Has(economy, "watch_rollie"),
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
            if (!Has(economy, itemId)) return;

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

        /// <summary>
        /// Owned by the player, OR worn unconditionally by this character.
        /// Kept as one predicate so an always-equipped item cannot be stripped
        /// by a sale, and so a null economy is survivable.
        /// </summary>
        private bool Has(EconomyManager economy, string itemId)
        {
            if (alwaysEquipped != null)
            {
                for (int i = 0; i < alwaysEquipped.Length; i++)
                {
                    if (alwaysEquipped[i] == itemId) return true;
                }
            }
            return economy != null && economy.OwnsItem(itemId, characterIndex);
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

                if (itemId == "chain_gold")
                {
                    if (chainPlacement != null && chainPlacement.useManualPlacement)
                    {
                        go.transform.localPosition = chainPlacement.localPosition;
                        go.transform.localRotation = Quaternion.Euler(chainPlacement.localEulerAngles);
                        go.transform.localScale = chainPlacement.localScale;
                    }
                    else
                    {
                        // Placed in the character's frame, then converted back
                        // to a bone-local offset for the swing to settle around.
                        go.transform.rotation = transform.rotation * Quaternion.Euler(ChainTilt, 0f, 0f);
                        go.transform.position = anchor.position
                            + transform.forward * ChainForward
                            + transform.up * ChainUp
                            + transform.right * ChainSide;
                        NormaliseAccessoryScale(go.transform, anchor, ChainWidth);
                    }
                }
                else
                {
                    PositionOnBone(go.transform, itemId);
                }

                // MINI-045: the chain hangs rather than sitting rigid on
                // the bone. Capturing localPosition after PositionOnBone
                // means AccessorySwing doesn't need its own copy of the
                // per-item offset table above - it just settles wherever
                // this method already decided the item's rest pose is.
                if (itemId == "chain_gold")
                {
                    var swing = go.AddComponent<AccessorySwing>();
                    // MINI-067: hold the chain in the CHARACTER's frame, not
                    // the chest bone's. This rig's bone rest orientations are
                    // not world-aligned - the same trap already documented on
                    // Boss C's necklace - and a real modelled chain shows that
                    // immediately where a ring of spheres did not.
                    swing.Initialize(anchor, go.transform.localPosition, go.transform.rotation);
                }

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

        /// <summary>
        /// MINI-067. The real model when one is wired, otherwise the original
        /// generated placeholder.
        ///
        /// The source model was 1,995,768 polygons with two 8192x8192 textures
        /// (~60MB) - unusable on this project's mobile-first target for a prop
        /// the size of a necklace. It is decimated to 8k tris with 1024 maps
        /// (2.0MB) by Mini067GoldChainPrep before it ever reaches here, so this
        /// method is only ever instantiating the cleaned prefab.
        /// </summary>
        private GameObject BuildChain()
        {
            if (chainPrefab != null)
            {
                var real = Instantiate(chainPrefab);
                // Colliders on a bone-parented accessory would fight the
                // character controller from inside its own capsule.
                foreach (var c in real.GetComponentsInChildren<Collider>(true)) Destroy(c);
                return real;
            }

            return BuildChainPlaceholder();
        }

        private static GameObject BuildChainPlaceholder()
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
