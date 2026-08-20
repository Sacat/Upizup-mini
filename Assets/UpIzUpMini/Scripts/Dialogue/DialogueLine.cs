using System;
using System.Collections.Generic;
using UnityEngine;

namespace UpIzUpMini.Dialogue
{
    /// <summary>
    /// MINI-053: the category list is exactly what the roadmap brief asked
    /// the foundation to support later - normal dialogue, inner thoughts,
    /// conditional lines (via DialogueCondition, not a category of its
    /// own), mission dialogue, shop dialogue, faction dialogue, and story
    /// revelations. Nothing here decides HOW a category is displayed
    /// (italic for inner thought, no speaker name, etc.) - that stays a UI
    /// concern for whoever renders the line.
    /// </summary>
    public enum DialogueCategory
    {
        Normal,
        InnerThought,
        Mission,
        Shop,
        Faction,
        StoryReveal,
    }

    [Serializable]
    public class DialogueLine
    {
        public string speaker;
        public DialogueCategory category = DialogueCategory.Normal;
        [TextArea(1, 3)] public string text;

        // ALL conditions must match (AND). An empty list means "always
        // eligible" - existing unconditional lines need no authoring
        // changes to sit in a DialogueSet.
        public List<DialogueCondition> conditions = new List<DialogueCondition>();

        public bool IsEligible()
        {
            if (conditions == null) return true;
            for (int i = 0; i < conditions.Count; i++)
            {
                if (conditions[i] != null && !conditions[i].Matches()) return false;
            }
            return true;
        }
    }
}
