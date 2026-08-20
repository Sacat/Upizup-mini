using System;
using UpIzUpMini.Economy;
using UpIzUpMini.Progression;

namespace UpIzUpMini.Dialogue
{
    /// <summary>
    /// MINI-053: one gate a DialogueLine can require. Deliberately reads
    /// existing progression/economy state (ProgressionManager reputation
    /// and career path, EconomyManager heat) rather than introducing a
    /// parallel dialogue-only state store - the whole point of "data-driven
    /// dialogue foundation" is that later content (MINI-054's opening
    /// conversation, faction lines, story reveals) can gate on the game
    /// state that already exists.
    /// </summary>
    public enum DialogueConditionType
    {
        Always,
        MinReputation,
        MaxReputation,
        CareerPath,
        CropUnlocked,
        MinHeat,
        MaxHeat,
        // MINI-060: gates a line on ProgressionManager.DogLifeRevealed -
        // e.g. Dog Life's own dialogue turning openly hostile only after
        // Gardey Zafeh's reveal.
        DogLifeRevealed,
    }

    [Serializable]
    public class DialogueCondition
    {
        public DialogueConditionType type = DialogueConditionType.Always;
        public Faction faction;
        public int threshold;
        public CareerPath careerPath;
        public string cropId;

        public bool Matches()
        {
            switch (type)
            {
                case DialogueConditionType.Always:
                    return true;
                case DialogueConditionType.MinReputation:
                    return ProgressionManager.Instance != null
                           && ProgressionManager.Instance.GetReputation(faction) >= threshold;
                case DialogueConditionType.MaxReputation:
                    return ProgressionManager.Instance != null
                           && ProgressionManager.Instance.GetReputation(faction) <= threshold;
                case DialogueConditionType.CareerPath:
                    return ProgressionManager.Instance != null
                           && ProgressionManager.Instance.Path == careerPath;
                case DialogueConditionType.CropUnlocked:
                    return ProgressionManager.Instance != null
                           && !string.IsNullOrEmpty(cropId)
                           && ProgressionManager.Instance.IsCropUnlocked(cropId);
                case DialogueConditionType.MinHeat:
                    return EconomyManager.Instance != null && EconomyManager.Instance.Heat >= threshold;
                case DialogueConditionType.MaxHeat:
                    return EconomyManager.Instance != null && EconomyManager.Instance.Heat <= threshold;
                case DialogueConditionType.DogLifeRevealed:
                    return ProgressionManager.Instance != null && ProgressionManager.Instance.DogLifeRevealed;
                default:
                    return true;
            }
        }
    }
}
