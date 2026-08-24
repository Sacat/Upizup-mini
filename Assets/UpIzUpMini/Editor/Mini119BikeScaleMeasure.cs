using UnityEditor;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-119 follow-up, user: "i want the bike scaled because
    /// the world would look awkward" (i.e. scale the bike DOWN to match
    /// Sacat, not Sacat up - would break MINI-083's approved uniform
    /// character height). Measures the vendor rider's real height (the
    /// proportion the bike was actually modelled/authored against) versus
    /// Sacat's own established real height (1.85m, MINI-083) to compute a
    /// real scale factor instead of guessing one.</summary>
    public static class Mini119BikeScaleMeasure
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Measure Bike-vs-Sacat Scale (one-off)")]
        public static void Run()
        {
            var bikePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/MotorbikePhysicsTool/Prefabs/BikesWithRagdolls/SuperMotoWRagdoll.prefab");
            var sacatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UpIzUpMini/Art/Characters/Mainchar.fbx");

            if (bikePrefab == null) { Debug.LogError("MINI-119 SCALE MEASURE FAIL: bike prefab not found."); return; }

            var bikeInstance = (GameObject)PrefabUtility.InstantiatePrefab(bikePrefab);
            bikeInstance.transform.position = Vector3.zero;
            bikeInstance.transform.rotation = Quaternion.identity;

            // Vendor rider's real height, as authored (whatever proportion
            // the bike was actually built/animated against).
            Transform rider = null;
            foreach (Transform t in bikeInstance.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Rider 1") { rider = t; break; }
            }

            float riderHeight = 0f;
            if (rider != null)
            {
                var renderers = rider.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length > 0)
                {
                    Bounds b = renderers[0].bounds;
                    foreach (var r in renderers) b.Encapsulate(r.bounds);
                    riderHeight = b.size.y;
                }
            }

            // Real seat height and wheelbase too, for a sanity cross-check.
            var rearWheel = FindDeep(bikeInstance.transform, "RearWheelPos");
            var frontWheel = FindDeep(bikeInstance.transform, "FrontWheelPos");
            float wheelbase = (rearWheel != null && frontWheel != null)
                ? Vector3.Distance(rearWheel.position, frontWheel.position) : -1f;

            // Overall bike bounds for a real world footprint number too.
            var bikeRenderers = bikeInstance.GetComponentsInChildren<Renderer>(true);
            Bounds bikeBounds = bikeRenderers.Length > 0 ? bikeRenderers[0].bounds : new Bounds();
            foreach (var r in bikeRenderers) bikeBounds.Encapsulate(r.bounds);

            const float sacatHeight = 1.85f; // MINI-083, already established/approved

            Debug.Log($"MINI-119 SCALE MEASURE: vendor rider real height={riderHeight:F3}m, Sacat's own established height={sacatHeight:F3}m");
            if (riderHeight > 0.01f)
            {
                float neededBikeScale = sacatHeight > 0 ? 1f : 1f; // placeholder, real ratio below
                float ratio = riderHeight / sacatHeight; // >1 means the bike was modelled for a TALLER rider than Sacat, i.e. bike is oversized relative to him
                Debug.Log($"MINI-119 SCALE MEASURE: riderHeight/SacatHeight ratio={ratio:F3} - if >1, the bike (and its rider proportions) were authored for someone taller than Sacat, so scaling the WHOLE bike prefab by 1/{ratio:F3} = {1f / ratio:F3} would make Sacat look correctly proportioned on it, same as the vendor's own rider does now.");
            }
            Debug.Log($"MINI-119 SCALE MEASURE: bike overall bounds size={bikeBounds.size}, wheelbase={wheelbase:F3}m, overall length(z)={bikeBounds.size.z:F3}m, height(y)={bikeBounds.size.y:F3}m");

            Object.DestroyImmediate(bikeInstance);
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                var r = FindDeep(child, name);
                if (r != null) return r;
            }
            return null;
        }
    }
}
