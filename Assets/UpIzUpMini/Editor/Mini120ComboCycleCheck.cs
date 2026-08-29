using UnityEditor;
using UnityEngine;
using UpIzUpMini.Combat;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-120 combo request: verifies SimpleMeleeCombat.Attack()
    /// actually cycles Jab -> Hook -> Right Hook -> Jab and that waiting
    /// past comboResetSeconds drops the chain back to the jab. Delete
    /// after use.</summary>
    public static class Mini120ComboCycleCheck
    {
        [MenuItem("Up Iz Up Mini/MINI-120/Check Combo Cycle (one-off)")]
        public static void Run()
        {
            var go = new GameObject("Mini120ComboTestDummy");
            var combat = go.AddComponent<SimpleMeleeCombat>();
            // No CharacterVitals attached - Attack()'s stamina check is
            // skipped whenever vitals is null, so this exercises the pure
            // combo-cycling logic without needing a full character.

            Debug.Log($"MINI-120 COMBO CHECK: ComboStep before any attack = {combat.ComboStep} (expect 0).");

            combat.Attack();
            Debug.Log($"MINI-120 COMBO CHECK: after attack #1, ComboStep = {combat.ComboStep} (expect 1, played {MeleeMoveLibrary.Chain[0].id}).");

            // Simulate the swing finishing so the next Attack() isn't
            // blocked by its own cooldown/IsRunning guard.
            combat.AdvanceAttack(MeleeMoveLibrary.Chain[0].TotalSeconds + 0.05f);

            combat.Attack();
            Debug.Log($"MINI-120 COMBO CHECK: after attack #2, ComboStep = {combat.ComboStep} (expect 2, played {MeleeMoveLibrary.Chain[1].id}).");
            combat.AdvanceAttack(MeleeMoveLibrary.Chain[1].TotalSeconds + 0.05f);

            combat.Attack();
            Debug.Log($"MINI-120 COMBO CHECK: after attack #3, ComboStep = {combat.ComboStep} (expect 0 - wrapped around, played {MeleeMoveLibrary.Chain[2].id}).");
            combat.AdvanceAttack(MeleeMoveLibrary.Chain[2].TotalSeconds + 0.05f);

            Debug.Log("MINI-120 COMBO CHECK: waiting past the combo reset window...");
            combat.AdvanceAttack(2.0f); // well past comboResetSeconds (1.0s default)
            Debug.Log($"MINI-120 COMBO CHECK: ComboStep after idling 2s = {combat.ComboStep} (expect 0, already was after the wraparound - re-verify with a mid-chain reset instead).");

            // A cleaner reset test: attack once (step->1), then idle past
            // the reset window WITHOUT attacking again, and confirm it
            // drops back to 0 instead of staying at 1.
            var go2 = new GameObject("Mini120ComboResetTestDummy");
            var combat2 = go2.AddComponent<SimpleMeleeCombat>();
            combat2.Attack();
            Debug.Log($"MINI-120 COMBO CHECK: [reset test] ComboStep right after one attack = {combat2.ComboStep} (expect 1).");
            combat2.AdvanceAttack(MeleeMoveLibrary.Chain[0].TotalSeconds + 2.0f); // past both the swing AND the combo window
            Debug.Log($"MINI-120 COMBO CHECK: [reset test] ComboStep after idling well past comboResetSeconds = {combat2.ComboStep} (expect 0 - dropped back to jab).");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(go2);
            Debug.Log("MINI-120 COMBO CHECK: done.");
        }
    }
}
