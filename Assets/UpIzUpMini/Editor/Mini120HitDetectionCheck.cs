using UnityEditor;
using UnityEngine;
using UpIzUpMini.Combat;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-120 (combat bug-fix pass): reproduces the user's
    /// "I can punch the air close to the character and the character
    /// gets hurt" report as a real geometric measurement, before touching
    /// any numbers. Checks IsInsideForwardContact directly at a range of
    /// distances straight ahead of a stand-in attacker. Delete after
    /// use.</summary>
    public static class Mini120HitDetectionCheck
    {
        [MenuItem("Up Iz Up Mini/MINI-120/Check Hit Detection Reach - OLD Defaults (one-off, read-only)")]
        public static void RunOld()
        {
            var oldProfile = new MeleeAttackProfile(
                windupSeconds: 0.16f, activeSeconds: 0.12f, recoverySeconds: 0.37f,
                forwardOffset: 0.3f, reach: 1.65f, radius: 0.38f, arcDegrees: 80f);
            Measure("OLD (range=1.65, radius=0.38, bonus=0.35 default)", oldProfile);
        }

        [MenuItem("Up Iz Up Mini/MINI-120/Check Hit Detection Reach - New Combo Moves (one-off, read-only)")]
        public static void RunNew()
        {
            foreach (var move in MeleeMoveLibrary.GetChainFor("Sacat"))
            {
                Measure($"{move.id} (reach={move.reach}, radius={move.radius}, bonus={move.bodyRadiusBonus})", move.BuildProfile());
            }
        }

        private static void Measure(string label, MeleeAttackProfile profile)
        {
            var attackerGo = new GameObject("Mini120Attacker");
            attackerGo.transform.position = Vector3.zero;
            attackerGo.transform.rotation = Quaternion.identity; // facing +Z

            var targetGo = new GameObject("Mini120Target");

            Debug.Log($"MINI-120 HIT CHECK [{label}]: straight ahead (0 deg), varying distance:");
            float maxHitDistance = 0f;
            for (float d = 0.3f; d <= 2.4f; d += 0.1f)
            {
                targetGo.transform.position = new Vector3(0f, 0f, d);
                bool hit = MeleeContactResolver.IsInsideForwardContact(attackerGo.transform, targetGo.transform, profile);
                if (hit) maxHitDistance = d;
            }
            Debug.Log($"MINI-120 HIT CHECK [{label}]: max distance that still registers a hit = {maxHitDistance:F1}m");

            float maxLateral = 0f;
            for (float x = 0f; x <= 1.2f; x += 0.05f)
            {
                targetGo.transform.position = new Vector3(x, 0f, 0.8f);
                bool hit = MeleeContactResolver.IsInsideForwardContact(attackerGo.transform, targetGo.transform, profile);
                if (hit) maxLateral = x;
            }
            Debug.Log($"MINI-120 HIT CHECK [{label}]: max lateral offset @ 0.8m ahead that still registers = {maxLateral:F2}m");

            Object.DestroyImmediate(attackerGo);
            Object.DestroyImmediate(targetGo);
        }
    }
}
