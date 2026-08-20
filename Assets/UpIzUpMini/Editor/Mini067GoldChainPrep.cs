using UnityEditor;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-067. Turns the user's supplied "Gold Chain 18k" model into a
    /// prefab small enough to hang on a character's chest bone.
    ///
    /// The source at E:\Assets\Gold Chain 18k.glb is 1,995,768 polygons with
    /// two 8192x8192 textures (~60MB) - a generated/sculpted asset, not a
    /// game-ready one. For a prop the size of a necklace on a mobile-first
    /// project that is indefensible, so it is decimated to ~8k tris with
    /// 1024 maps (2.0MB) in Blender first; this script only ever consumes
    /// the cleaned result.
    ///
    /// Scale is MEASURED from the imported renderer bounds rather than
    /// hardcoded: the model authors at ~0.75m wide (roughly a person's whole
    /// torso), so a guessed multiplier would be wrong the moment the source
    /// is re-exported at a different size.
    /// </summary>
    public static class Mini067GoldChainPrep
    {
        private const string SourcePath = "Assets/UpIzUpMini/Art/Accessories/GoldChain18k_clean.glb";
        private const string PrefabPath = "Assets/UpIzUpMini/Art/Accessories/GoldChain18k.prefab";

        /// <summary>Chain width across the chest, in metres. Raised from 0.20
        /// to 0.27 per the user - a puffed mariner is a deliberately chunky
        /// chain and 0.20 read as a thin necklace at gameplay distance. The
        /// characters measure ~1.8m tall for scale.</summary>
        public const float TargetWidthM = 0.27f;

        [MenuItem("Up Iz Up Mini/MINI-067/Build Gold Chain Prefab")]
        public static void BuildPrefab()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
            if (source == null)
            {
                Debug.LogError($"MINI-067 FAIL: {SourcePath} not found. Run the Blender cleanup step first.");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.name = "GoldChain18k";

            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Debug.LogError("MINI-067 FAIL: the cleaned chain has no renderers.");
                Object.DestroyImmediate(instance);
                return;
            }

            // Combined bounds in WORLD space, with the instance at identity, so
            // this is the model's own authored size.
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

            float authoredWidth = Mathf.Max(b.size.x, 0.0001f);
            float scale = TargetWidthM / authoredWidth;

            // A root wrapper carries the scale and the offset, leaving the
            // imported hierarchy untouched - so a re-import of the GLB does not
            // silently invalidate hand-tuned numbers baked into its transform.
            var root = new GameObject("GoldChain18k");
            instance.transform.SetParent(root.transform, false);
            instance.transform.localScale = Vector3.one * scale;

            // Hang from the TOP of the model: the chest bone sits at the base
            // of the neck, so aligning the chain's top edge to the origin makes
            // it drape down the chest instead of being centred on the collar.
            float topOffset = (b.center.y + b.extents.y) * scale;
            instance.transform.localPosition = new Vector3(-b.center.x * scale, -topOffset, -b.center.z * scale);

            foreach (var c in root.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool ok);
            Object.DestroyImmediate(root);

            if (!ok || prefab == null)
            {
                Debug.LogError("MINI-067 FAIL: SaveAsPrefabAsset reported failure.");
                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"MINI-067 CHAIN OK: {PrefabPath} built. MEASURED authored bounds size={b.size} centre={b.center}; " +
                      $"scaled by {scale:F4} to {TargetWidthM}m wide; renderers={renderers.Length}.");
        }
    }
}
