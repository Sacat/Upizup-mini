using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// Structural regression proof for the MINI-131 wheel lock and restrained
    /// rider turn accent. Actual handling feel remains a built-player check.
    /// </summary>
    public static class Mini131TmaxVisualSyncValidation
    {
        private const string PrefabPath = "Assets/UpIzUpMini/Vehicles/TMAX_560.prefab";
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;

        [MenuItem("Up Iz Up Mini/MINI-131/Validate Wheel Lock and Rider Lean")]
        public static void Validate()
        {
            try
            {
                RunChecks();
                Debug.Log("MINI-131 VALIDATION PASS: TMAX wheel poses are stateless and late-running; rider turn lean is a short, restrained accent with a fast neutral return.");
            }
            catch (Exception ex)
            {
                Debug.LogError("MINI-131 VALIDATION FAIL: " + ex.Message);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        private static void RunChecks()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Require(prefab != null, "TMAX_560.prefab is missing.");

            var order = typeof(TmaxWheelVisuals).GetCustomAttribute<DefaultExecutionOrder>();
            Require(order != null && order.order >= 10000,
                "TmaxWheelVisuals must execute late enough to be the final wheel-pose writer.");

            var instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var visuals = instance.GetComponent<TmaxWheelVisuals>();
                Require(visuals != null, "TMAX prefab has no TmaxWheelVisuals component.");

                var awake = typeof(TmaxWheelVisuals).GetMethod("Awake", PrivateInstance);
                var late = typeof(TmaxWheelVisuals).GetMethod("LateUpdate", PrivateInstance);
                Require(awake != null && late != null, "Wheel synchronization lifecycle methods are missing.");
                awake.Invoke(visuals, null);

                Transform front = instance.transform.Find("VisualLeanRoot/FrontSteering/FrontWheel");
                Transform rear = instance.transform.Find("VisualLeanRoot/RearWheel");
                Require(front != null && rear != null, "The two approved TMAX wheel visual transforms are missing.");

                late.Invoke(visuals, null);
                Vector3 frontLocked = front.position;
                Vector3 rearLocked = rear.position;

                for (int i = 0; i < 250; i++)
                {
                    // Simulate an animator/order bug attempting to displace the
                    // wheel before our final visual pass. Every cycle must
                    // recover the exact same hub position, never accumulate it.
                    front.position += new Vector3(0.013f, 0f, -0.009f);
                    rear.position += new Vector3(-0.017f, 0f, 0.011f);
                    late.Invoke(visuals, null);
                    Require(PlanarDistance(front.position, frontLocked) < 0.0001f,
                        "Front wheel accumulated positional drift.");
                    Require(PlanarDistance(rear.position, rearLocked) < 0.0001f,
                        "Rear wheel accumulated positional drift.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }

            var go = new GameObject("MINI-131 Rider Defaults");
            try
            {
                var riderAnimation = go.AddComponent<BikeRiderAnimation>();
                Require(ReadFloat(riderAnimation, "leanInputThreshold") >= 0.5f,
                    "Lean pose still triggers on small steering taps.");
                Require(ReadFloat(riderAnimation, "leanReturnSmoothing") <= 0.2f,
                    "Rider does not return to neutral quickly enough.");
                Require(ReadFloat(riderAnimation, "leanPoseMaxSeconds") <= 0.4f,
                    "Authored lean clip is still held too long.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }

            var followLimit = typeof(BikeInteractable).GetField("RiderLeanFollowSafetyLimit", PrivateStatic);
            var maxLimit = typeof(BikeInteractable).GetField("RiderMaxLeanSafetyLimit", PrivateStatic);
            Require(followLimit != null && (float)followLimit.GetRawConstantValue() <= 0.55f,
                "Rider still follows too much of the bike's cosmetic roll.");
            Require(maxLimit != null && (float)maxLimit.GetRawConstantValue() <= 9f,
                "Rider roll safety ceiling is still excessive.");
        }

        private static float ReadFloat(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, PrivateInstance);
            Require(field != null, "Missing rider field: " + fieldName);
            return (float)field.GetValue(target);
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
