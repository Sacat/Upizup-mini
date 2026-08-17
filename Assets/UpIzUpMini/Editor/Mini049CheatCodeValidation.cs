using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;
using UpIzUpMini.Progression;
using UpIzUpMini.UI;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-049. Exercises the real cheat-code buffer/match logic and its
    /// effects against real EconomyManager/ProgressionManager/CharacterVitals
    /// code, via FeedTypedCharacters(string) rather than Input.inputString
    /// (which reflects real OS key events and can't be faked from an
    /// edit-mode harness).
    /// </summary>
    public static class Mini049CheatCodeValidation
    {
        [MenuItem("Up Iz Up Mini/MINI-049/Run Cheat Code Validation")]
        public static void Run()
        {
            // Static flags - always start and end this run clean so a
            // failed/aborted run doesn't leave them set for anything else
            // in the same editor session.
            CharacterVitals.GlobalInvincible = false;
            CharacterVitals.GlobalUnlimitedStamina = false;

            try
            {
                var economyGo = new GameObject("TestEconomy");
                var economy = economyGo.AddComponent<EconomyManager>();
                InvokeMethod(economy, "Awake");
                economy.SetMoney(37); // an arbitrary starting amount, not 0 and not the cheat amount

                var progGo = new GameObject("TestProgression");
                var progression = progGo.AddComponent<ProgressionManager>();
                InvokeMethod(progression, "Awake");

                var cheatGo = new GameObject("TestCheat");
                var cheat = cheatGo.AddComponent<CheatCodeController>();

                // --- A near-miss must not trigger anything. ---
                cheat.FeedTypedCharacters("C#0WX");
                if (economy.Money != 37)
                    throw new Exception($"MINI-049 validation: a near-miss code changed Money to {economy.Money} (expected untouched 37).");
                if (CharacterVitals.GlobalInvincible)
                    throw new Exception("MINI-049 validation: a near-miss code set GlobalInvincible.");

                // --- Junk typed before the real code, then the real code
                // (character by character, like a real player typing) -
                // the rolling buffer must still catch it. ---
                cheat.FeedTypedCharacters("hello world ");
                foreach (char c in "C#0W@") cheat.FeedTypedCharacters(c.ToString());

                if (economy.Money != 100000)
                    throw new Exception($"MINI-049 validation: Money is {economy.Money} after the real code, expected exactly 100000.");
                if (!progression.PurpleCheeseUnlocked || !progression.BlueCheeseUnlocked || !progression.PurpleBlackUnlocked
                    || !progression.BlackSugarUnlocked || !progression.PurpleUnlocked || !progression.SugarCheeseUnlocked)
                    throw new Exception("MINI-049 validation: not every strain unlock is true after the cheat.");
                if (!progression.GrandBayWeedRouteEstablished || !progression.GuadeloupeCharacterCourierUnlocked)
                    throw new Exception("MINI-049 validation: route unlocks (Grand Bay weed route / Guadeloupe character courier) not both true after the cheat.");
                if (progression.Path == CareerPath.Undecided)
                    throw new Exception("MINI-049 validation: career Path still Undecided after the cheat - farm assignment would stay locked.");
                if (!CharacterVitals.GlobalInvincible || !CharacterVitals.GlobalUnlimitedStamina)
                    throw new Exception("MINI-049 validation: invincibility/unlimited-stamina flags not both set after the cheat.");

                // --- Invincibility must actually block damage, not just flip a flag nothing reads. ---
                var charGo = new GameObject("TestChar");
                var vitals = charGo.AddComponent<CharacterVitals>();
                InvokeMethod(vitals, "Awake");
                vitals.Damage(9999f);
                if (vitals.IsDead || vitals.Health < vitals.MaxHealth - 0.01f)
                    throw new Exception($"MINI-049 validation: a character took damage (health {vitals.Health}/{vitals.MaxHealth}) while GlobalInvincible was true.");

                Debug.Log("MINI-049 CHEAT CODE VALIDATION PASS: a near-miss does nothing, junk typed before the real code doesn't break the rolling-buffer match, and the real code sets money to exactly $100,000, unlocks every strain/route, picks a career path if undecided, and makes damage a genuine no-op.");

                UnityEngine.Object.DestroyImmediate(charGo);
                UnityEngine.Object.DestroyImmediate(cheatGo);
                UnityEngine.Object.DestroyImmediate(progGo);
                UnityEngine.Object.DestroyImmediate(economyGo);
            }
            finally
            {
                CharacterVitals.GlobalInvincible = false;
                CharacterVitals.GlobalUnlimitedStamina = false;
            }
        }

        private static void InvokeMethod(object target, string name)
        {
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);
        }
    }
}
