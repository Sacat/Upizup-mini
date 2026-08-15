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
            if (_promptStyle == null)
            {
                _promptStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
            }

            if (_feedbackStyle == null)
            {
                _feedbackStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 15,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    normal = { textColor = Color.white }
                };
            }
        }

        private void OnGUI()
        {
            var cam = Camera.main;
            if (cam == null) return;

            EnsureStyles();

            if (_current != null)
            {
                Vector3 worldPos = _current.PromptAnchor != null
                    ? _current.PromptAnchor.position
                    : ((MonoBehaviour)_current).transform.position + Vector3.up * 2f;

                Vector3 screenPoint = cam.WorldToScreenPoint(worldPos);
                if (screenPoint.z > 0f)
                {
                    var guiPoint = new Vector2(screenPoint.x, Screen.height - screenPoint.y);
                    var rect = new Rect(guiPoint.x - 60f, guiPoint.y - 15f, 120f, 30f);
                    GUI.Box(rect, _current.PromptLabel, _promptStyle);
                }
            }

            if (_feedbackTimer > 0f && !string.IsNullOrEmpty(_feedback))
            {
                var rect = new Rect(Screen.width / 2f - 220f, Screen.height - 90f, 440f, 60f);
                GUI.Box(rect, _feedback, _feedbackStyle);
            }
        }
    }
}
