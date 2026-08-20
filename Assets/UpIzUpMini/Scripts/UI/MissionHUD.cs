using UnityEngine;
using UnityEngine.UI;
using UpIzUpMini.Character;
using UpIzUpMini.Missions;

namespace UpIzUpMini.UI
{
    /// <summary>
    /// Mission UI: a persistent objective card (mission name, current
    /// instruction, progress count, distance to the marker) and a large
    /// transient banner for briefings and completions.
    ///
    /// MINI-053: "mission details must not disappear before the player can
    /// read them" - the banner previously held for a fixed 3.2s regardless
    /// of how much text was in it, so a longer briefing (a full title +
    /// multi-line description) could fade before it was actually read.
    /// Hold time now scales with the banner's own text length.
    /// </summary>
    public class MissionHUD : MonoBehaviour
    {
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text bannerText;
        [SerializeField] private GameObject objectivePanel;
        [Tooltip("MINI-073: the objective card's background Image, resized every frame to fit the current text - see ResizeObjectiveCard. The fixed-height box previously TRUNCATED longer objectives (a title plus a wrapped instruction plus a distance line routinely exceeded it) with no visual sign anything was missing.")]
        [SerializeField] private RectTransform objectivePanelRect;
        [SerializeField] private float objectivePanelMinHeight = 90f;
        [SerializeField] private float objectivePanelPadding = 24f;
        [SerializeField] private float bannerHoldMin = 3.2f;
        [Tooltip("Extra hold seconds per character, on top of bannerHoldMin - an average adult reads roughly 200-250 wpm, this is a conservative slower estimate so slang-heavy lines aren't rushed.")]
        [SerializeField] private float bannerHoldPerChar = 0.045f;
        [SerializeField] private float bannerHoldMax = 9f;
        [SerializeField] private float bannerFade = 1.2f;

        private void Update()
        {
            var ms = MissionSystem.Instance;
            if (ms == null) return;

            var mission = ms.Current;
            var objective = ms.CurrentObjective;

            if (objectivePanel != null) objectivePanel.SetActive(objective != null);

            if (objective != null && objectiveText != null)
            {
                // Mission name first, per the brief's explicit "persistent
                // small objective card" requirement list - previously only
                // the objective's own instruction text was shown, with no
                // indication of which mission it belonged to.
                string line = mission != null && !string.IsNullOrEmpty(mission.title)
                    ? $"<b>{mission.title}</b>\n{objective.instruction}"
                    : objective.instruction;

                if (objective.requiredCount > 1)
                {
                    line += $"   ({objective.progress}/{objective.requiredCount})";
                }

                if (objective.hasMarker && objective.markerPosition != Vector3.zero)
                {
                    var player = CharacterSwitchManager.Instance?.Active?.root;
                    if (player != null)
                    {
                        float d = Vector3.Distance(player.transform.position, objective.markerPosition);
                        line += $"\n<size=22>{d:F0} m</size>";
                    }
                }

                objectiveText.text = line;
                ResizeObjectiveCard();
            }

            ResizeObjectiveCard();

            if (bannerText != null)
            {
                string banner = ms.Banner ?? string.Empty;
                float hold = Mathf.Clamp(
                    bannerHoldMin + banner.Length * bannerHoldPerChar, bannerHoldMin, bannerHoldMax);

                float age = Time.time - ms.BannerTime;
                float alpha =
                    age < hold ? 1f :
                    age < hold + bannerFade ? 1f - (age - hold) / bannerFade : 0f;

                bannerText.text = banner;
                var c = bannerText.color;
                c.a = alpha;
                bannerText.color = c;
            }
        }

        /// <summary>
        /// Grows the objective card's own background to fit whatever text is
        /// currently in it, using UGUI's standard preferred-height measurement
        /// (the same technique a ContentSizeFitter uses internally) rather than
        /// a component, since the panel's Image and the text's RectTransform
        /// are two different objects here. Called every frame the panel is
        /// visible, and cheap enough to do so - it is one string measurement,
        /// not a layout pass over the whole canvas.
        /// </summary>
        private void ResizeObjectiveCard()
        {
            if (objectivePanelRect == null || objectiveText == null) return;
            if (!objectivePanel.activeSelf) return;

            float width = objectivePanelRect.rect.width;
            var settings = objectiveText.GetGenerationSettings(new Vector2(width - 72f, 0f));
            float preferred = objectiveText.cachedTextGeneratorForLayout.GetPreferredHeight(objectiveText.text, settings);

            float height = Mathf.Max(objectivePanelMinHeight, preferred + objectivePanelPadding);
            var size = objectivePanelRect.sizeDelta;
            if (!Mathf.Approximately(size.y, height))
            {
                objectivePanelRect.sizeDelta = new Vector2(size.x, height);
            }
        }
    }
}
