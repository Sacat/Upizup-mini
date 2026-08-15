using UnityEngine;
using UnityEngine.UI;
using UpIzUpMini.Character;
using UpIzUpMini.Missions;

namespace UpIzUpMini.UI
{
    /// <summary>
    /// Mission UI: a persistent objective card (current instruction plus
    /// distance to the marker) and a large transient banner for briefings
    /// and completions.
    /// </summary>
    public class MissionHUD : MonoBehaviour
    {
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text bannerText;
        [SerializeField] private GameObject objectivePanel;
        [SerializeField] private float bannerHold = 3.2f;
        [SerializeField] private float bannerFade = 1.2f;

        private void Update()
        {
            var ms = MissionSystem.Instance;
            if (ms == null) return;

            var objective = ms.CurrentObjective;

            if (objectivePanel != null) objectivePanel.SetActive(objective != null);

            if (objective != null && objectiveText != null)
            {
                string line = objective.instruction;

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
            }

            if (bannerText != null)
            {
                float age = Time.time - ms.BannerTime;
                float alpha =
                    age < bannerHold ? 1f :
                    age < bannerHold + bannerFade ? 1f - (age - bannerHold) / bannerFade : 0f;

                bannerText.text = ms.Banner ?? string.Empty;
                var c = bannerText.color;
                c.a = alpha;
                bannerText.color = c;
            }
        }
    }
}
