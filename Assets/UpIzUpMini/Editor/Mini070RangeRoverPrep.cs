using UnityEditor;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-070. Turns the user's downloaded Range Rover into a game-ready
    /// prefab, replacing the box-and-cylinder placeholder that stood in for
    /// Boss C's black SUV.
    ///
    /// The source (Hi3D, ~62MB) arrived at 1,984,875 triangles with two
    /// 8192x8192 textures - the same shape of problem as the gold chain in
    /// MINI-067, and the same answer: decimate and resize in headless Blender
    /// BEFORE Unity ever sees it. 20k tris and 2048 maps here rather than the
    /// chain's 8k/1024, because a car occupies far more screen area than a
    /// necklace. Result is 5.6MB.
    ///
    /// Scale is MEASURED from the imported bounds, never hardcoded. The model
    /// arrives unit-normalised (longest axis exactly 1.0), so a hand-picked
    /// multiplier would be meaningless - and would silently break if the asset
    /// were ever re-exported at a different size.
    /// </summary>
    public static class Mini070RangeRoverPrep
    {
        private const string SourcePath = "Assets/UpIzUpMini/Art/Vehicles/RangeRover_clean.glb";
        private const string PrefabPath = "Assets/UpIzUpMini/Art/Vehicles/RangeRover.prefab";

        /// <summary>Real Range Rover length, bumper to bumper, in metres. The
        /// characters measure ~1.8m tall, so this keeps the car in proportion
        /// with the people standing next to it.</summary>
        public const float RealLengthM = 4.95f;

        [MenuItem("Up Iz Up Mini/MINI-070/Build Range Rover Prefab")]
        public static void BuildPrefab()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
            if (source == null)
            {
                Debug.LogError($"MINI-070 FAIL: {SourcePath} not found. Run the Blender cleanup step first.");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Debug.LogError("MINI-070 FAIL: the cleaned model has no renderers.");
                Object.DestroyImmediate(instance);
                return;
            }

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

            // The car's LENGTH is whichever horizontal axis is longer. Derived
            // rather than assumed: glTF's Z-up to Y-up conversion decides which
            // of X/Z the length lands on, and guessing wrong would scale the
            // car by its width.
            bool lengthIsZ = b.size.z >= b.size.x;
            float authoredLength = Mathf.Max(lengthIsZ ? b.size.z : b.size.x, 0.0001f);
            float scale = RealLengthM / authoredLength;

            var root = new GameObject("RangeRover");
            instance.transform.SetParent(root.transform, false);
            instance.transform.localScale = Vector3.one * scale;

            // Sit the wheels on the ground and centre the car on its own origin,
            // so placing the prefab is just "put it where it should stand"
            // rather than needing a per-placement height fudge.
            instance.transform.localPosition = new Vector3(
                -b.center.x * scale,
                -b.min.y * scale,
                -b.center.z * scale);

            // Face the car along +Z when the mesh happens to be modelled along X,
            // so callers can always treat the prefab's forward as the car's nose.
            if (!lengthIsZ) root.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            // One simple box collider, matching the TMAX's approach - a 20k-tri
            // mesh collider on set dressing is not worth the physics cost.
            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(
                (lengthIsZ ? b.size.x : b.size.z) * scale,
                b.size.y * scale,
                (lengthIsZ ? b.size.z : b.size.x) * scale);
            box.center = new Vector3(0f, box.size.y * 0.5f, 0f);

            // Read anything needed for the log BEFORE the temp object dies -
            // the collider is destroyed along with it.
            Vector3 colliderSize = box.size;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool ok);
            Object.DestroyImmediate(root);

            if (!ok || prefab == null)
            {
                Debug.LogError("MINI-070 FAIL: SaveAsPrefabAsset reported failure.");
                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"MINI-070 RANGE ROVER OK: {PrefabPath} built. MEASURED authored bounds size={b.size}; " +
                      $"length axis={(lengthIsZ ? "Z" : "X")}; scaled by {scale:F4} to {RealLengthM}m long; " +
                      $"collider={colliderSize}; renderers={renderers.Length}.");
        }
    }
}
