using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UpIzUpMini.Missions;
using UpIzUpMini.InputSystem;

namespace UpIzUpMini.Dialogue
{
    /// <summary>
    /// MINI-054: the opening conversation between Franki and Sacat, per the
    /// roadmap brief's beats - kicked out of school, hungry and struggling,
    /// need money, one suggests Zion/Zeb, the other is hesitant, they
    /// settle on starting with normal crops. Plays as a scripted, ordered
    /// sequence of lines (NOT the conditional DialogueSet selection from
    /// MINI-053 - this is a one-time linear scene, so DialogueLine is reused
    /// here purely as a data shape for speaker+text, with conditions left
    /// empty/unused) on the same banner MissionHUD already renders.
    ///
    /// Reading-time formula matches MissionHUD/InteractionDetector's own
    /// MINI-053 fix (a longer line holds longer) - duplicated as a small
    /// local constant set rather than factored into a shared helper, since
    /// all three already-verified call sites would need touching for a
    /// three-line formula.
    /// </summary>
    public class OpeningConversationController : MonoBehaviour
    {
        private const float HoldMin = 3.2f;
        private const float HoldPerChar = 0.045f;
        private const float HoldMax = 9f;

        [SerializeField] private List<DialogueLine> lines = new List<DialogueLine>();

        /// <summary>Total playback time - Mini011PhaseBSetup reads this at
        /// scene-build time to set MissionSystem.firstBriefingDelay so M1's
        /// own briefing appears right after this finishes, not on top of it.</summary>
        public float TotalDuration
        {
            get
            {
                float total = 0f;
                if (lines != null)
                {
                    foreach (var line in lines) total += ReadingHold(line?.text);
                }
                return total;
            }
        }

        private void Start()
        {
            if (lines == null || lines.Count == 0) return;
            StartCoroutine(PlaySequence());
        }

        private IEnumerator PlaySequence()
        {
            foreach (var line in lines)
            {
                if (line == null || string.IsNullOrEmpty(line.text)) continue;

                string shown = string.IsNullOrEmpty(line.speaker) ? line.text : $"{line.speaker}: {line.text}";
                MissionSystem.Instance?.Alert(shown);

                float hold = ReadingHold(line.text);
                float elapsed = 0f;
                while (elapsed < hold)
                {
                    if (GameInput.WasPressed(GameAction.Interact)) break;
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
            MissionSystem.Instance?.FinishOpeningConversation();
        }

        private static float ReadingHold(string text)
        {
            int len = string.IsNullOrEmpty(text) ? 0 : text.Length;
            return Mathf.Clamp(HoldMin + len * HoldPerChar, HoldMin, HoldMax);
        }
    }
}
