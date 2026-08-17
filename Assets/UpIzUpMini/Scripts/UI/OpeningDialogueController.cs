using UnityEngine;
using UnityEngine.UI;
using UpIzUpMini.Character;

namespace UpIzUpMini.UI
{
    /// <summary>
    /// MINI-055. The game-opening "kicked out of school" dialogue cutscene.
    /// The two protagonists (Sacat and Franki) talk on screen before normal
    /// control takes over, using the user's exact script vocabulary:
    ///   - "boi they kick you out of school"
    ///   - "boi I hungry, me self doe even think i can go down diah"
    ///   - "We need to make ah money wii gasah"
    ///   - "boii let us go zion"
    ///   - "boi i doe really want to plant zeb yea but we go see"
    ///   - "we will plant normal crops and we will see how dat go"
    ///   - "Boi i scrub wii fadah"
    ///
    /// Advances on E. While active it locks the controllable protagonists
    /// (playerController.IsControlled = false) so the player can't move or
    /// punch during the intro, then unlocks them at the end. Reuses the
    /// existing Screen Space - Overlay canvas (built into the GameplayUICanvas
    /// by the scene builder), so it matches the rest of the HUD.
    /// </summary>
    public class OpeningDialogueController : MonoBehaviour
    {
        [System.Serializable]
        public struct Line
        {
            public string speaker;   // displayed name
            public string text;      // the spoken line
        }

        [SerializeField] private Line[] lines = new Line[0];
        [SerializeField] private GameObject panel;
        [SerializeField] private Text bodyText;
        [SerializeField] private float holdAfterLast = 1.2f;

        private int _index;
        private bool _finished;
        private bool _controlRestored;
        private CharacterSwitchManager _switcher;

        private void Start()
        {
            _switcher = CharacterSwitchManager.Instance;
            // Lock both protagonists while the intro plays.
            LockControl(true);
            _index = 0;
            ShowCurrent();
        }

        private void Update()
        {
            if (_finished)
            {
                // Brief pause after the last line, then hand over control.
                if (!_controlRestored)
                {
                    _restoreAt = Time.time + holdAfterLast;
                    _controlRestored = true;
                }
                if (Time.time >= _restoreAt)
                {
                    LockControl(false);
                    if (panel != null) panel.SetActive(false);
                    enabled = false;
                }
                return;
            }

            // E advances to the next line (or finishes).
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (_index < lines.Length - 1)
                {
                    _index++;
                    ShowCurrent();
                }
                else
                {
                    _finished = true;
                    if (bodyText != null && panel != null)
                    {
                        // Keep last line visible until control is freed.
                    }
                }
            }
        }

        private void ShowCurrent()
        {
            if (bodyText == null || _index >= lines.Length) return;
            var line = lines[_index];
            string speaker = string.IsNullOrEmpty(line.speaker) ? "" : $"<b>{line.speaker}</b>\n";
            bodyText.text = speaker + line.text;
            if (panel != null) panel.SetActive(true);
        }

        private void LockControl(bool locked)
        {
            if (_switcher?.Slots == null) return;
            foreach (var slot in _switcher.Slots)
            {
                if (slot?.playerController != null) slot.playerController.IsControlled = !locked;
            }
        }

        private float _restoreAt;
    }
}
