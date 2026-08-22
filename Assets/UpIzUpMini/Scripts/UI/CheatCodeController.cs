using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;
using UpIzUpMini.Interaction;
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

        // MINI-060 follow-up-2: a second cheat, six consecutive "0" presses
        // -> invincible, $100,000, stamina stays full, heat locked at 0,
        // and a 600s time-skip applied to the two currently-live timed
        // systems (the Boat Man's away timer, any in-progress
        // GuadeloupeTrade run) - per the user's "press '0' 6 times i am
        // invincible with $100,000 heat stays 0 which skips time by 600s."
        // Matched independently via EndsWith rather than an exact-length
        // buffer compare, since it's a different length to Code and both
        // need to keep matching off the same rolling buffer.
        //
        // Toggle, not one-shot: pressing "000000" again turns invincibility/
        // full-stamina/the heat lock back off, per the user's explicit
        // "can be disabled by pressing 0 6 times again." Money already
        // granted and time already skipped are one-time effects that don't
        // (and can't sensibly) reverse - there's no "undo" for cash spent
        // or a trip that already returned early.
        private const string HeatCode = "000000";
        private const string MissionCode = "111111";
        private const float HeatCheatTimeSkip = 600f;

        private bool _heatCheatActive;
        private string _buffer = string.Empty;
        private int MaxCodeLength => Mathf.Max(Code.Length, Mathf.Max(HeatCode.Length, MissionCode.Length));

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
            if (_buffer.Length > MaxCodeLength)
            {
                _buffer = _buffer.Substring(_buffer.Length - MaxCodeLength);
            }

            if (_buffer.EndsWith(Code))
            {
                Activate();
                _buffer = string.Empty;
            }
            else if (_buffer.EndsWith(HeatCode))
            {
                ToggleHeatCheat();
                _buffer = string.Empty;
            }
            else if (_buffer.EndsWith(MissionCode))
            {
                MissionSystem.Instance?.CompleteCurrentMissionCheat();
                MissionSystem.Instance?.Alert("MISSION SKIPPED\nCheat 111111 completed the current mission.");
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

        private void ToggleHeatCheat()
        {
            if (!_heatCheatActive)
            {
                _heatCheatActive = true;
                CharacterVitals.GlobalInvincible = true;
                CharacterVitals.GlobalUnlimitedStamina = true;
                EconomyManager.Instance?.SetMoney(CheatMoney);
                EconomyManager.Instance?.LockHeatAtZero();

                var boatManGo = GameObject.Find("NPC_BoatMan");
                boatManGo?.GetComponent<TownNPCInteractable>()?.SkipTravelTime(HeatCheatTimeSkip);
                GuadeloupeTrade.Instance?.SkipTime(HeatCheatTimeSkip);

                MissionSystem.Instance?.Alert(
                    $"CHEAT ACTIVATED\nInvincible. Stamina full. ${CheatMoney:N0} in hand. Heat locked at 0. Time skipped {HeatCheatTimeSkip:N0}s.");
                Debug.Log("MINI-060 follow-up-2: heat cheat code activated.");
            }
            else
            {
                _heatCheatActive = false;
                CharacterVitals.GlobalInvincible = false;
                CharacterVitals.GlobalUnlimitedStamina = false;
                EconomyManager.Instance?.UnlockHeat();

                MissionSystem.Instance?.Alert("CHEAT DEACTIVATED\nInvincibility, full stamina, and the heat lock are off.");
                Debug.Log("MINI-060 follow-up-2: heat cheat code deactivated.");
            }
        }
    }
}
