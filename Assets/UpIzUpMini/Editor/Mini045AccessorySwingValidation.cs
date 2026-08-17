using System;
using UnityEditor;
using UnityEngine;
using UpIzUpMini.Character;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-045. Drives AccessorySwing's spring integration directly via
    /// Simulate(dt, restWorldPos) with an explicit dt, since Time.deltaTime
    /// is 0 outside Play mode and LateUpdate would never do anything if
    /// called through the normal Unity lifecycle in this harness. Proves
    /// three things a subtle sign or stability bug in a hand-rolled spring
    /// could easily get wrong: it stays essentially at rest when the
    /// anchor isn't moving, it grows (but stays clamped) while being
    /// dragged, and it actually settles back down afterwards rather than
    /// oscillating forever or diverging.
    /// </summary>
    public static class Mini045AccessorySwingValidation
    {
        [MenuItem("Up Iz Up Mini/MINI-045/Run Accessory Swing Validation")]
        public static void Run()
        {
            var anchorGo = new GameObject("TestAnchor");
            var chainGo = new GameObject("TestChain");
            var swing = chainGo.AddComponent<AccessorySwing>();

            Vector3 localOffset = new Vector3(0f, 0.14f, 0.08f);
            swing.Initialize(anchorGo.transform, localOffset);

            const float dt = 1f / 60f;

            // 1. Anchor at rest: offset should stay essentially at zero,
            // not drift from numerical error.
            for (int i = 0; i < 30; i++)
            {
                swing.Simulate(dt, anchorGo.transform.TransformPoint(localOffset));
            }
            if (swing.SwingOffset.magnitude > 0.001f)
                throw new Exception($"MINI-045 validation: swing drifted to {swing.SwingOffset.magnitude:F4}m with a stationary anchor (expected ~0).");

            // 2. Drag the anchor sideways at a brisk, constant pace for
            // half a second (jogging speed) - the offset must grow but
            // never exceed the configured clamp.
            const float dragSpeed = 4.3f; // m/s, matches PlayerController's run speed
            float maxSeenDuringDrag = 0f;
            for (int i = 0; i < 30; i++)
            {
                anchorGo.transform.position += Vector3.right * dragSpeed * dt;
                swing.Simulate(dt, anchorGo.transform.TransformPoint(localOffset));
                if (float.IsNaN(swing.SwingOffset.x) || float.IsInfinity(swing.SwingOffset.x))
                    throw new Exception($"MINI-045 validation: swing offset went NaN/Infinity while dragging (step {i}).");
                maxSeenDuringDrag = Mathf.Max(maxSeenDuringDrag, swing.SwingOffset.magnitude);
            }
            if (maxSeenDuringDrag < 0.0005f)
                throw new Exception("MINI-045 validation: dragging the anchor produced essentially no swing at all - the drive term is likely wired wrong (sign error or zeroed coefficient).");
            if (maxSeenDuringDrag > 0.045f + 0.0001f) // maxSwingDistance default, small float-compare slack
                throw new Exception($"MINI-045 validation: swing offset reached {maxSeenDuringDrag:F4}m, exceeding the clamp (0.045m) - the clamp isn't holding.");

            // 3. Stop the anchor and let it settle - offset should decay
            // back down close to zero, not keep oscillating at similar
            // amplitude (which would mean damping is doing nothing) or
            // diverge.
            for (int i = 0; i < 180; i++) // 3 simulated seconds of standing still
            {
                swing.Simulate(dt, anchorGo.transform.TransformPoint(localOffset));
            }
            if (swing.SwingOffset.magnitude > 0.002f)
                throw new Exception($"MINI-045 validation: swing did not settle after 3s stationary - still at {swing.SwingOffset.magnitude:F4}m (expected ~0, damping may be too weak or mis-signed).");

            Debug.Log($"MINI-045 ACCESSORY SWING VALIDATION PASS: stays at rest when the anchor doesn't move, swings up to {maxSeenDuringDrag:F4}m (clamped at 0.045m) while being dragged at jogging speed, settles back to rest within 3s of the anchor stopping.");

            UnityEngine.Object.DestroyImmediate(chainGo);
            UnityEngine.Object.DestroyImmediate(anchorGo);
        }
    }
}
