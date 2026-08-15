using UnityEngine;

namespace UpIzUpMini.Interaction
{
    /// <summary>
    /// Attached to the player. Finds the nearest registered
    /// <see cref="IInteractable"/> in range, shows a world-anchored
    /// "[ E ] ..." prompt above it, and handles the interact key.
    ///
    /// Rendered with legacy IMGUI (OnGUI) rather than a UGUI World Space
    /// canvas because this project currently has no com.unity.ugui /
    /// TextMeshPro package installed (see PROJECT-HANDOFF.md MINI-001
    /// entry). The prompt still tracks a world position via
    /// Camera.WorldToScreenPoint, so it reads as world-space to the
    /// player even though it is not a Canvas.
    /// </summary>
    public class InteractionDetector : MonoBehaviour
    {
        [SerializeField] private float interactRange = 2.75f;
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private KeyCode cloneKey = KeyCode.R;
        [SerializeField] private float feedbackDuration = 3f;

        private IInteractable _current;
        private string _feedback;
        private float _feedbackTimer;

        private GUIStyle _promptStyle;
        private GUIStyle _feedbackStyle;

        private void Update()
        {
            _current = FindNearestInRange();

            if (_current != null && Input.GetKeyDown(interactKey) && _current.CanInteract(gameObject))
            {
                _current.Interact(gameObject);
                _feedback = (_current as InteractableBase)?.GetInteractionFeedback();
                _feedbackTimer = feedbackDuration;
            }

            // R clones a ripe plant for extra seed (see FarmPlot.Clone).
            if (_current is Farming.FarmPlot plot && Input.GetKeyDown(cloneKey))
            {
                string result = plot.Clone();
                if (!string.IsNullOrEmpty(result))
                {
                    _feedback = result;
                    _feedbackTimer = feedbackDuration;
                }
            }

            if (_feedbackTimer > 0f)
            {
                _feedbackTimer -= Time.deltaTime;
            }
        }

        private IInteractable FindNearestInRange()
        {
            IInteractable best = null;
            float bestDistSqr = interactRange * interactRange;

            foreach (var interactable in InteractableBase.All)
            {
                if (interactable == null) continue;
                var mb = interactable as MonoBehaviour;
                if (mb == null) continue;

                float distSqr = (mb.transform.position - transform.position).sqrMagnitude;
                if (distSqr <= bestDistSqr)
                {
                    bestDistSqr = distSqr;
                    best = interactable;
                }
            }

            return best;
        }

        private void EnsureStyles()
        {
            // OnGUI font sizes are in raw screen pixels, not scaled with
            // resolution - on a high-res/mobile display a fixed 16px prompt
            // reads as tiny. Scale against a 1080-tall reference so it stays
            // readable across resolutions (also serves the project's
            // mobile/tablet-first requirement, not just this desktop bug).
            float scale = Mathf.Max(1f, Screen.height / 1080f);
            int promptFontSize = Mathf.RoundToInt(30 * scale);
            int feedbackFontSize = Mathf.RoundToInt(26 * scale);

            if (_promptStyle == null)
            {
                _promptStyle = new GUIStyle(GUI.skin.box)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
            }
            _promptStyle.fontSize = promptFontSize;

            if (_feedbackStyle == null)
            {
                _feedbackStyle = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    normal = { textColor = Color.white }
                };
            }
            _feedbackStyle.fontSize = feedbackFontSize;
        }

        private void OnGUI()
        {
            var cam = Camera.main;
            if (cam == null) return;

            EnsureStyles();
            float scale = Mathf.Max(1f, Screen.height / 1080f);

            if (_current != null)
            {
                Vector3 worldPos = _current.PromptAnchor != null
                    ? _current.PromptAnchor.position
                    : ((MonoBehaviour)_current).transform.position + Vector3.up * 2f;

                Vector3 screenPoint = cam.WorldToScreenPoint(worldPos);
                if (screenPoint.z > 0f)
                {
                    var guiPoint = new Vector2(screenPoint.x, Screen.height - screenPoint.y);
                    float w = 260f * scale;
                    float h = 56f * scale;
                    var rect = new Rect(guiPoint.x - w / 2f, guiPoint.y - h / 2f, w, h);
                    GUI.Box(rect, _current.PromptLabel, _promptStyle);

                    // A ripe plot also offers cloning, shown as a second line.
                    if (_current is Farming.FarmPlot ripePlot && ripePlot.CanClone)
                    {
                        var cloneRect = new Rect(rect.x, rect.y + h + 4f * scale, w, h);
                        GUI.Box(cloneRect, ripePlot.CloneLabel, _promptStyle);
                    }
                }
            }

            if (_feedbackTimer > 0f && !string.IsNullOrEmpty(_feedback))
            {
                float w = 700f * scale;
                float h = 110f * scale;
                var rect = new Rect(Screen.width / 2f - w / 2f, Screen.height - h - 30f * scale, w, h);
                GUI.Box(rect, _feedback, _feedbackStyle);
            }
        }
    }
}
