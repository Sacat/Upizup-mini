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
        [SerializeField] private GameObject headphonesAccessory;
        private const string HeadphonesId = "headphones_studio";
        public bool HeadphonesAvailable => headphonesAccessory != null;
        public bool HeadphonesEquipped => HeadphonesAvailable && !_unequippedItems.Contains(HeadphonesId);
        public bool SetHeadphonesEquipped(bool wear)
        {
            if (!HeadphonesAvailable) return false;
            if (wear) _unequippedItems.Remove(HeadphonesId); else _unequippedItems.Add(HeadphonesId);
            RefreshEquipment();
            return true;
        }
        [SerializeField] private int characterIndex;
        [SerializeField] private AccessoryPlacementProfile chainPlacement;
        [SerializeField] private GameObject watchPrefab;
        [SerializeField] private AccessoryPlacementProfile watchPlacement;
        private readonly HashSet<string> _unequippedItems = new HashSet<string>();
        // Temporary try-ons never grant ownership, advance missions or enter saves.
        private readonly HashSet<string> _trialItems = new HashSet<string>();
        public static readonly string[] TrialItemIds = { "watch_rollie", "cap_mike", "shades_ray" };
        public static readonly string[] TrialItemLabels = { "Gold watch", "Existing cap (prototype)", "Existing shades (prototype)" };
        public bool SetTrialItem(string id, bool wear)
        {
            if (System.Array.IndexOf(TrialItemIds,id)<0) return false;
            if(wear)_trialItems.Add(id);else _trialItems.Remove(id);
            RefreshEquipment();return true;
        }
        public void ClearTrialItems(){_trialItems.Clear();RefreshEquipment();}
        public List<string> CaptureTrialItems()=>new List<string>(_trialItems);
        public void RestoreTrialItems(IEnumerable<string> ids)
        {
            _trialItems.Clear();
            if(ids!=null)foreach(var id in ids)if(System.Array.IndexOf(TrialItemIds,id)>=0)_trialItems.Add(id);
            RefreshEquipment();
        }

        // Ownership remains in EconomyManager; wardrobe selections belong to each wearer.
        // Missing fields in legacy saves mean owned items keep their old equipped default.
        public List<string> CaptureWardrobe() => new List<string>(_unequippedItems);
        public void RestoreWardrobe(IEnumerable<string> unequipped)
        {
            _unequippedItems.Clear();
            if(unequipped!=null)foreach(var id in unequipped)if(!string.IsNullOrEmpty(id))_unequippedItems.Add(id);
            RefreshEquipment();
        }
        public bool WatchOwned => Has(EconomyManager.Instance,"watch_rollie");
        public bool WatchEquipped => _trialItems.Contains("watch_rollie") || WatchOwned && !_unequippedItems.Contains("watch_rollie");
        public bool SetWatchEquipped(bool equipped)
        {
            if(!WatchOwned)return false;
            if(equipped)_unequippedItems.Remove("watch_rollie");else _unequippedItems.Add("watch_rollie");
            RefreshEquipment();return true;
        }

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

        /// <summary>
        /// Purchase UI calls this after a wearable transaction. The economy
        /// event remains the normal update path, but this direct refresh makes
        /// the visible result deterministic even if an old scene/save entered
        /// with a missed or late subscription.
        /// </summary>
        public void RefreshEquipment()
        {
            TrySubscribe();
            Refresh();
        }

        private void Refresh()
        {
            if (headphonesAccessory != null) headphonesAccessory.SetActive(HeadphonesEquipped);
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

            var outfit = GetComponent<OutfitWardrobe>();
            bool hatSlotOwnsCap = outfit != null && outfit.HasSlot(OutfitSlot.Hat);
            Apply("cap_mike", HumanBodyBones.Head, !hatSlotOwnsCap && (_trialItems.Contains("cap_mike") || Has(economy, "cap_mike")),
                () => BuildCap(new Color(0.85f, 0.15f, 0.15f)));

            Apply("shades_ray", HumanBodyBones.Head, _trialItems.Contains("shades_ray") || Has(economy, "shades_ray"),
                () => BuildShades());

            Apply("chain_gold", HumanBodyBones.Chest, Has(economy, "chain_gold"),
                () => BuildChain());

            Apply("watch_rollie", HumanBodyBones.LeftLowerArm, WatchEquipped,
                () => BuildWatch());

            // Clothing recolours the character's own garments rather than
            // adding geometry - the models default to black, so a bought
            // item visibly changes their outfit. Same MINI-166 regression as
            // cap_mike above: suppress per-slot as OutfitWardrobe takes over
            // each one, not all-or-nothing on component presence.
            if (outfit == null || !outfit.HasSlot(OutfitSlot.Shirt))
                ApplyGarment("shirt_lacos", new[] { "top", "tshirt", "shirt" }, new Color(0.90f, 0.94f, 0.96f));
            if (outfit == null || !outfit.HasSlot(OutfitSlot.Pants))
                ApplyGarment("shorts_adibas", new[] { "bottom", "pants", "trouser" }, new Color(0.25f, 0.32f, 0.62f));
            if (outfit == null || !outfit.HasSlot(OutfitSlot.Shoes))
            {
                ApplyGarment("shoes_mike", new[] { "shoes" }, new Color(0.95f, 0.95f, 0.95f));
                ApplyGarment("shoes_pumba", new[] { "shoes" }, new Color(0.85f, 0.25f, 0.20f));
            }
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

                if (itemId == "watch_rollie" && watchPlacement != null && watchPlacement.useManualPlacement)
                {
                    go.transform.localPosition=watchPlacement.localPosition;
                    go.transform.localRotation=Quaternion.Euler(watchPlacement.localEulerAngles);
                    go.transform.localScale=watchPlacement.localScale;
                    ApplyFittedChildren(go.transform,watchPlacement);
                    // MINI-152: user explicitly requested a lower wrist position.
                    // Preserve approved orbit, rotation, size and radial clearance;
                    // change only distance along the forearm toward the hand.
                    var hand=animator.GetBoneTransform(HumanBodyBones.LeftHand);
                    if(hand!=null)
                    {
                        var axis=(hand.position-anchor.position).normalized;
                        var wristTarget=hand.position-axis*.018f;
                        go.transform.position+=axis*Vector3.Dot(wristTarget-go.transform.position,axis);
                    }
                }
                else if (itemId == "chain_gold")
                {
                    if (chainPlacement != null && chainPlacement.useManualPlacement)
                    {
                        go.transform.localPosition = chainPlacement.localPosition;
                        go.transform.localRotation = Quaternion.Euler(chainPlacement.localEulerAngles);
                        go.transform.localScale = chainPlacement.localScale;
                        ApplyFittedChildren(go.transform, chainPlacement);
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
                existing.SetActive(false);
                if (Application.isPlaying) Destroy(existing);
                else DestroyImmediate(existing);
                _spawned.Remove(itemId);
            }
        }

        private static void ApplyFittedChildren(Transform accessoryRoot, AccessoryPlacementProfile profile)
        {
            if (profile.fittedChildren == null) return;

            foreach (var pose in profile.fittedChildren)
            {
                if (pose == null || string.IsNullOrEmpty(pose.relativePath)) continue;
                var child = accessoryRoot.Find(pose.relativePath);
                if (child == null)
                    child = RecreateFittedDuplicate(accessoryRoot, pose.relativePath);
                if (child == null)
                {
                    Debug.LogWarning($"Accessory fitted child '{pose.relativePath}' was not found under {accessoryRoot.name}.");
                    continue;
                }

                child.localPosition = pose.localPosition;
                child.localRotation = Quaternion.Euler(pose.localEulerAngles);
                child.localScale = pose.localScale;
            }
        }

        private static Transform RecreateFittedDuplicate(Transform accessoryRoot, string relativePath)
        {
            int slash = relativePath.LastIndexOf('/');
            string parentPath = slash >= 0 ? relativePath.Substring(0, slash) : string.Empty;
            string childName = slash >= 0 ? relativePath.Substring(slash + 1) : relativePath;
            int copySuffix = childName.LastIndexOf(" (", System.StringComparison.Ordinal);
            if (copySuffix <= 0 || !childName.EndsWith(")", System.StringComparison.Ordinal)) return null;

            string sourceName = childName.Substring(0, copySuffix);
            Transform parent = string.IsNullOrEmpty(parentPath) ? accessoryRoot : accessoryRoot.Find(parentPath);
            Transform source = parent != null ? parent.Find(sourceName) : null;
            if (source == null) return null;

            var copy = Instantiate(source.gameObject, parent, false);
            copy.name = childName;
            return copy.transform;
        }

        private void PositionOnBone(Transform t, string itemId)
        {
            if(itemId=="cap_mike" || itemId=="shades_ray")
            {
                // Head bones have rig-dependent axes and inherited scale.
                // Specify offsets in character metres, then keep the resulting
                // bone-local transform so animation still carries the accessory.
                var anchor=t.parent;
                t.rotation=transform.rotation;
                // MINI-166: measured against the real skeleton (a render
                // showed the cap floating above the skull with a visible
                // gap) - the Head bone sits near ear/jaw level, not the
                // crown (HeadTop_End is ~0.21m above Head on this rig), and
                // the cap's own crown-sphere mesh has a further ~0.06m
                // downward radius before its surface even reaches the
                // placement point. .10f only got the sphere's BOTTOM to
                // ~0.04m above Head, well short of the real hairline - was
                // never actually resting on the head. Raised to .16f and
                // confirmed by a real Play Mode render on both characters
                // (Logs/Tasks/MINI-166/Sacat-PM-Head-Side.png, Franki-PM-
                // Head-Side.png) - cap now reads as sitting on the head, no
                // gap. Watch checked in the same pass and found already
                // correct (1.8cm from the hand, wraps the wrist properly in
                // the render) - not changed.
                t.position=anchor.position+transform.up*(itemId=="cap_mike"?.16f:.025f)
                    +transform.forward*(itemId=="cap_mike"?.0f:.105f);
                var s=anchor.lossyScale;
                t.localScale=new Vector3(1f/Mathf.Max(.0001f,Mathf.Abs(s.x)),1f/Mathf.Max(.0001f,Mathf.Abs(s.y)),1f/Mathf.Max(.0001f,Mathf.Abs(s.z)));
                return;
            }
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

        private GameObject BuildWatch()
        {
            if(watchPrefab!=null)return Instantiate(watchPrefab);
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
