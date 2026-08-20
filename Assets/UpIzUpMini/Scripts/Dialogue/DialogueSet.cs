using System.Collections.Generic;
using UnityEngine;

namespace UpIzUpMini.Dialogue
{
    /// <summary>
    /// MINI-053: a data-driven dialogue foundation - an ordered, authored
    /// list of DialogueLine, each optionally gated by DialogueCondition.
    /// A ScriptableObject asset, matching this project's existing
    /// data-definition pattern (CropDefinition, ShopItemDefinition), so
    /// content authors edit lines in the Inspector, not code.
    ///
    /// SelectLine() picks the MOST SPECIFIC eligible line (the one with the
    /// most conditions), so a conditional line always beats a generic
    /// fallback when both are eligible. Among lines tied for specificity
    /// (e.g. several unconditional "Always" lines), it cycles through them
    /// in authoring order - this is what preserves the existing
    /// "villager says something different each time you talk" behaviour
    /// for content with no conditions at all, so migrating a plain array
    /// of lines onto this system doesn't change how it plays.
    /// </summary>
    [CreateAssetMenu(menuName = "Up Iz Up Mini/Dialogue Set", fileName = "NewDialogueSet")]
    public class DialogueSet : ScriptableObject
    {
        [Tooltip("Authoring order only matters as the tie-break cycle order within a specificity tier - see SelectLine().")]
        public List<DialogueLine> lines = new List<DialogueLine>();

        private int _cycleIndex;

        public DialogueLine SelectLine()
        {
            if (lines == null || lines.Count == 0) return null;

            int bestSpecificity = -1;
            var tied = new List<DialogueLine>();

            foreach (var line in lines)
            {
                if (line == null || !line.IsEligible()) continue;

                int specificity = line.conditions?.Count ?? 0;
                if (specificity > bestSpecificity)
                {
                    bestSpecificity = specificity;
                    tied.Clear();
                    tied.Add(line);
                }
                else if (specificity == bestSpecificity)
                {
                    tied.Add(line);
                }
            }

            if (tied.Count == 0) return null;
            if (tied.Count == 1) return tied[0];

            var pick = tied[_cycleIndex % tied.Count];
            _cycleIndex++;
            return pick;
        }
    }
}
