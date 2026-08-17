using UnityEditor;
using UnityEngine;
using UpIzUpMini.Character;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-052 validation: proves the anti-stuck steering logic actually
    /// detects a blocked character and produces an evasion direction that
    /// would move it around an obstacle, rather than just "compiles".
    ///
    /// AntiStuckSteering is a MonoBehaviour driven by Tick()/GetEvasionDirection(),
    /// so it can be exercised outside Play mode by invoking those methods
    /// directly with a fixed dt in place of Time.deltaTime (which is 0
    /// outside Play - same pattern as Mini045AccessorySwingValidation).
    /// </summary>
    public static class Mini052AntiStuckValidation
    {
        public static void Run()
        {
            var go = new GameObject("AntiStuckTest");
            var anti = go.AddComponent<AntiStuckSteering>();
            // Drive the private timing via a fixed simulated dt. Tick uses
            // Time.deltaTime internally, which is 0 in edit mode, so we have
            // to simulate enough "frames" that stationary time accumulates.
            // We simulate by repeatedly calling Tick with the transform not
            // moving.

            // --- Phase 1: stationary-with-speed triggers evasion after timeout.
            // Tick with a fixed 0.05s dt (Time.deltaTime is 0 outside Play mode).
            // stuckTimeout=1.2s so ~24+ frames of no progress should trip it.
            bool evaded = false;
            for (int i = 0; i < 60 && !evaded; i++) // up to 3s simulated
            {
                anti.Tick(2.0f, 0.05f); // trying to walk, but not moving
                if (anti.IsEvading) evaded = true;
            }

            Debug.Log(evaded
                ? "MINI052: PASS - blocked character enters evasion after timeout."
                : "MINI052: FAIL - blocked character never entered evasion.");

            // --- Phase 2: evasion direction is perpendicular to facing.
            Vector3 facing = new Vector3(1f, 0f, 0f);
            Vector3 evade = anti.GetEvasionDirection(facing);
            bool perpendicular = Mathf.Abs(Vector3.Dot(evade.normalized, facing)) < 0.1f
                                 && evade.sqrMagnitude > 0.0001f;
            Debug.Log(perpendicular
                ? "MINI052: PASS - evasion direction is perpendicular to facing."
                : "MINI052: FAIL - evasion direction looks wrong.");

            // --- Phase 3: real progress cancels evasion.
            // Simulate the character finally moving (0.1m per 0.05s tick =
            // well above the 0.05m stuck threshold), which must clear evasion.
            for (int i = 0; i < 60 && anti.IsEvading; i++)
            {
                go.transform.position += new Vector3(0.1f, 0f, 0f);
                anti.Tick(2.0f, 0.05f);
            }
            bool progressed = !anti.IsEvading;
            Debug.Log(progressed
                ? "MINI052: PASS - real movement cancels evasion."
                : "MINI052: FAIL - evasion did not clear after real progress.");

            // --- Phase 4: Clear() resets state.
            anti.Clear();
            bool cleared = !anti.IsEvading;
            Debug.Log(cleared
                ? "MINI052: PASS - Clear() resets evasion state."
                : "MINI052: FAIL - Clear() did not reset evasion state.");

            Object.DestroyImmediate(go);

            bool overall = evaded && perpendicular && progressed && cleared;
            Debug.Log(overall
                ? "MINI052 ANTI-STUCK VALIDATION PASS"
                : "MINI052 ANTI-STUCK VALIDATION FAIL");

            if (Application.isBatchMode)
                EditorApplication.Exit(overall ? 0 : 1);
        }
    }
}
