using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;
using UpIzUpMini.Missions;
using UpIzUpMini.Progression;

namespace UpIzUpMini.UI
{
    /// <summary>
    /// MINI-049. Type "C#0W@" anywhere in-game (not while a text field has
    /// focus - there aren't any in this project, so no conflict) to set
    /// money to $100,000, unlock every strain/route, and make both boys
    /// unstoppable (can't take damage, stamina never runs out).
    ///
    /// Driven by Input.inputString rather than per-key KeyCode checks -
    /// '#' and '@' aren't their own physical keys on most layouts (they're
    /// Shift+digit combinations that vary by keyboard layout), and
    /// inputString already resolves whatever the OS/layout produced for
    /// the keys actually pressed, the same way a text field would receive
    /// them. Deliberately undocumented in the H-controls overlay - it's a
    /// cheat code, not a feature.
    /// </summary>
    public class CheatCodeController : MonoBehaviour
    {
        private const string Code = "C#0W@";
        private const int CheatMoney = 100000;

        private string _buffer = string.Empty;

        private void Update()
        {
            if (string.IsNullOrEmpty(Input.inputString)) return;
            FeedTypedCharacters(Input.inputString);
        }

        /// <summary>
        /// The buffer/match logic, split out from Update() so it can be
        /// driven directly by a test harness with an arbitrary typed
        /// string - Input.inputString reflects real OS key events and
        /// can't be faked from an edit-mode script the way KeyCode/Time
        /// values sometimes can.
        /// </summary>
        public void FeedTypedCharacters(string typed)
        {
            _buffer += typed;
            if (_buffer.Length > Code.Length)
            {
                _buffer = _buffer.Substring(_buffer.Length - Code.Length);
            }

            if (_buffer == Code)
            {
                Activate();
                _buffer = string.Empty;
            }
        }

        private void Activate()
        {
            EconomyManager.Instance?.SetMoney(CheatMoney);
            ProgressionManager.Instance?.UnlockEverything();
            CharacterVitals.GlobalInvincible = true;
            CharacterVitals.GlobalUnlimitedStamina = true;

            MissionSystem.Instance?.Alert(
                $"CHEAT ACTIVATED\n${CheatMoney:N0} in hand. Every strain and route unlocked. Unstoppable.");
            Debug.Log("MINI-049: cheat code activated.");
        }
    }
}
