using UnityEditor;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-119 follow-up, one-off diagnostic: maps the
    /// SuperMotoWRagdoll prefab's IK anchor hierarchy (are the hand/foot
    /// target Transforms parented to the bike frame, independent of the
    /// rider, or to the rider itself?) before building a character-swap
    /// plan around them. Delete after use.</summary>
    public static class Mini119RiderRigInspect
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Inspect Rider Rig (one-off)")]
        public static void Run()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/MotorbikePhysicsTool/Prefabs/BikesWithRagdolls/SuperMotoWRagdoll.prefab");
            if (prefab == null) { Debug.LogError("MINI-119 RIG INSPECT FAIL: prefab not found."); return; }

            string[] names = { "RightHandPos", "LeftHandPos", "RightFootPos", "LeftFootPos",
                "RightHandPole", "LeftHandPole", "RightFootPole", "LeftFootPole", "HandPos" };

            foreach (var n in names)
            {
                var t = FindDeep(prefab.transform, n);
                if (t == null) { Debug.Log($"MINI-119 RIG: {n} - NOT FOUND"); continue; }
                Debug.Log($"MINI-119 RIG: {n} parent-chain: {PathTo(t)}  localPos={t.localPosition}");
            }

            // Also log the rider root and any Animator on it.
            var rider = FindDeep(prefab.transform, "RiderNotForRagdolls") ?? FindDeep(prefab.transform, "Rider");
            if (rider != null)
            {
                var anim = rider.GetComponentInChildren<Animator>();
                Debug.Log($"MINI-119 RIG: rider root={rider.name}, Animator={(anim != null ? anim.runtimeAnimatorController?.name : "none")}");
            }
        }

        private static string PathTo(Transform t)
        {
            var s = t.name;
            var p = t.parent;
            while (p != null) { s = p.name + "/" + s; p = p.parent; }
            return s;
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
