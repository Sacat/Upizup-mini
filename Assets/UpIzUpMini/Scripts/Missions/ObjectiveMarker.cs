using UnityEngine;
using UpIzUpMini.Character;

namespace UpIzUpMini.Missions
{
    /// <summary>
    /// GTA-style objective marker: a bobbing arrow pointing down at the
    /// current objective's location, plus a ground ring. Moves itself to
    /// whatever the active objective is, and hides when there's nothing to
    /// point at. Uses generated geometry rather than an imported model so
    /// it has no asset dependency.
    /// </summary>
    public class ObjectiveMarker : MonoBehaviour
    {
        [SerializeField] private Transform arrow;
        [SerializeField] private Transform ring;
        [SerializeField] private float bobHeight = 0.35f;
        [SerializeField] private float bobSpeed = 2.2f;
        [SerializeField] private float spinSpeed = 55f;
        [SerializeField] private float hideWithinDistance = 1.25f;

        private void LateUpdate()
        {
            var objective = MissionSystem.Instance != null ? MissionSystem.Instance.CurrentObjective : null;
            bool show = objective != null && objective.hasMarker && objective.markerPosition != Vector3.zero;

            if (show)
            {
                var player = CharacterSwitchManager.Instance?.Active?.root;
                if (player != null &&
                    Vector3.Distance(player.transform.position, objective.markerPosition) < hideWithinDistance)
                {
                    // Close enough that the marker would obscure the target.
                    show = false;
                }
            }

            if (arrow != null) arrow.gameObject.SetActive(show);
            if (ring != null) ring.gameObject.SetActive(show);
            if (!show) return;

            transform.position = objective.markerPosition;

            if (arrow != null)
            {
                float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
                arrow.localPosition = new Vector3(0f, 2.6f + bob, 0f);
                arrow.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
                float pulse = 0.90f + Mathf.PingPong(Time.unscaledTime * 1.8f, 0.22f);
                arrow.localScale = Vector3.one * pulse;
            }
        }
    }
}
