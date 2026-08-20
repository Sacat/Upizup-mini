using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.InputSystem;

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
        // MINI-053: "mission details must not disappear before the player
        // can read them" applies just as much to NPC dialogue - a fixed 3s
        // box could cut off a longer boss/shopkeeper line. Duration now
        // scales with the shown text's length (min/max keep it sane).
        // MINI-073: "make the dialogue fade box smaller and fade away
        // quicker without pressing E just once you walk it fades from the
        // shops" - shortened hold considerably (was 3-9s), and the box no
        // longer needs to be dismissed with E to feel snappy; walking away
        // (already handled below) is the main way it goes, this timer is
        // just how long it lingers if you stand still and read it.
        [SerializeField] private float feedbackDurationMin = 1.4f;
        [SerializeField] private float feedbackDurationPerChar = 0.022f;
        [SerializeField] private float feedbackDurationMax = 4.5f;
        // MINI-073 follow-up: "the dialogue box should fade as walk motion
        // in done" - the box's opacity now tracks physical distance walked
        // since it appeared, not a flat timed ease. feedbackFadeSeconds is
        // kept as a backstop only, so it still eventually goes if the player
        // just stands still reading it.
        [SerializeField] private float feedbackFadeSeconds = 0.35f;
        [Tooltip("Metres of WALKING that fully fades the box out, once its reading hold has ended.")]
        [SerializeField] private float feedbackFadeWalkDistance = 2.2f;

        private IInteractable _current;
        private string _feedback;
        private float _feedbackTimer;
        private Vector3 _feedbackHoldEndPos;

        private GUIStyle _promptStyle;
        private GUIStyle _feedbackStyle;

        // MINI-080: "disable interaction when on a vehicle" - IsControlled
        // is exactly the right signal here (unlike the INACTIVE-character
        // trap fixed elsewhere this session): for the character THIS
        // component is actually attached to, IsControlled genuinely means
        // "not currently taken by a bike or car".
        private PlayerController _playerController;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
        }

        /// <summary>True while riding a vehicle - both Update (world
        /// interaction) and OnGUI (prompt/feedback box) skip entirely.</summary>
        private bool IsRidingVehicle => _playerController != null && !_playerController.IsControlled;

        private void Update()
        {
            if (IsRidingVehicle)
            {
                // Drop anything mid-flight rather than leaving a stale prompt
                // or feedback box hanging while mounted.
                _current = null;
                _feedback = null;
                _feedbackTimer = 0f;
                return;
            }

            var previous = _current;
            _current = FindNearestInRange();

            // Start the fade-out as soon as the player walks away, rather
            // than leaving the box hanging for the full timer - but keep the
            // TEXT so it can fade smoothly (see FeedbackAlpha) instead of
            // vanishing instantly, which read as an abrupt pop rather than a
            // fade per the user's "once you walk it fades" ask.
            if (previous != null && _current == null && _feedbackTimer > 0f)
            {
                _feedbackTimer = 0f;
                _feedbackHoldEndPos = transform.position;
            }

            // While a dialogue box is up, E is consumed dismissing it -
            // stops the player re-triggering a seller mid-sentence.
            if (_feedbackTimer > 0f && GameInput.WasPressed(GameAction.Interact))
            {
                _feedbackTimer = 0f;   // keep the text; let it fade rather than pop
                _feedbackHoldEndPos = transform.position;
                return;
            }

            if (_current != null && GameInput.WasPressed(GameAction.Interact) && _current.CanInteract(gameObject))
            {
                _current.Interact(gameObject);
                _feedback = (_current as InteractableBase)?.GetInteractionFeedback();
                _feedbackTimer = ReadingHold(_feedback);
            }

            if (GameInput.WasPressed(GameAction.AssignFarmhand))
            {
                var switcher = Character.CharacterSwitchManager.Instance;
                var slots = switcher?.Slots;
                if (slots != null)
                {
                    int other = 1 - switcher.ActiveIndex;
                    if (other >= 0 && other < slots.Length && slots[other]?.root != null)
                    {
                        var hand = slots[other].root.GetComponent<Character.FarmhandController>();
                        if (hand != null)
                        {
                            var crop = Economy.CropSelectionController.Instance != null
                                ? Economy.CropSelectionController.Instance.Selected : null;
                            hand.ToggleWorking(crop);
                            _feedback = hand.IsWorking
                                ? $"{slots[other].displayName} staying to plant and tend {crop?.displayName ?? "the selected crop"}."
                                : $"{slots[other].displayName} following again.";
                            if (hand.IsWorking)
                                Missions.MissionSystem.Instance?.Notify(Missions.ObjectiveKind.AssignFarmhand, crop?.cropId);
                            _feedbackTimer = ReadingHold(_feedback);
                        }
                    }
                }
            }

            // R clones a ripe plant for extra seed (see FarmPlot.Clone).
            if (_current is Farming.FarmPlot plot && GameInput.WasPressed(GameAction.SecondaryInteract))
            {
                string result = plot.Clone();
                if (!string.IsNullOrEmpty(result))
                {
                    _feedback = result;
                    _feedbackTimer = ReadingHold(_feedback);
                }
            }

            // R also asks the Boat Man about Gardey Zafeh - a second,
            // explicit choice alongside E's produce trade, same dual-key
            // pattern as FarmPlot's Harvest/Clone above.
            if (_current is TownNPCInteractable boatMan && GameInput.WasPressed(GameAction.SecondaryInteract))
            {
                string result = boatMan.InteractGardeyZafeh();
                if (!string.IsNullOrEmpty(result))
                {
                    _feedback = result;
                    _feedbackTimer = ReadingHold(_feedback);
                }
            }

            if (_feedbackTimer > 0f)
            {
                float previousTimer = _feedbackTimer;
                _feedbackTimer -= Time.deltaTime;
                if (_feedbackTimer <= 0f && previousTimer > 0f)
                {
                    _feedbackHoldEndPos = transform.position;
                }
            }
        }

        /// <summary>
        /// 0..1 fade-out multiplier for the current feedback box. Full opacity
        /// while the reading hold runs; once it ends, opacity is driven by how
        /// far the player has WALKED since - the position is captured the
        /// instant the hold ends (see the two call sites that zero
        /// _feedbackTimer) rather than at any fixed interval, so a player who
        /// keeps standing still keeps seeing it at full strength from that
        /// point, and a player who immediately walks away sees it go
        /// proportionally to the steps taken. A small time-based ease is
        /// blended in underneath purely as a backstop (see feedbackFadeSeconds)
        /// so the box does not linger forever for someone who never moves.
        /// </summary>
        private float FeedbackAlpha()
        {
            if (_feedbackTimer > 0.001f) return 1f;

            float walked = Vector3.Distance(transform.position, _feedbackHoldEndPos);
            float walkAlpha = Mathf.Clamp01(1f - walked / Mathf.Max(0.05f, feedbackFadeWalkDistance));

            float sinceExpired = -_feedbackTimer;
            float timeAlpha = Mathf.Clamp01(1f - sinceExpired / Mathf.Max(0.05f, feedbackFadeSeconds * 6f));

            return Mathf.Min(walkAlpha, timeAlpha);
        }

        private float ReadingHold(string text)
        {
            int len = string.IsNullOrEmpty(text) ? 0 : text.Length;
            return Mathf.Clamp(
                feedbackDurationMin + len * feedbackDurationPerChar, feedbackDurationMin, feedbackDurationMax);
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
                // Ignore the active boy's own CompanionInteractable (and
                // any other temporarily invalid target). Previously that
                // self target was always distance zero, so it hid the
                // inactive boy and made "Send to farm" appear broken.
                if (!interactable.CanInteract(gameObject)) continue;

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
            int feedbackFontSize = Mathf.RoundToInt(20 * scale);

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
                    // Sized from the text so longer prompts (e.g. the
                    // safehouse menu) aren't clipped.
                    var content = new GUIContent(_current.PromptLabel);
                    Vector2 size = _promptStyle.CalcSize(content);
                    float w = Mathf.Max(260f * scale, size.x + 40f * scale);
                    float h = Mathf.Max(56f * scale, size.y + 20f * scale);
                    var rect = new Rect(guiPoint.x - w / 2f, guiPoint.y - h / 2f, w, h);
                    GUI.Box(rect, _current.PromptLabel, _promptStyle);

                    // A ripe plot also offers cloning, shown as a second line.
                    if (_current is Farming.FarmPlot plotWithClone)
                    {
                        string cloneLabel = plotWithClone.CloneLabel;
                        if (!string.IsNullOrEmpty(cloneLabel))
                        {
                            var cloneContent = new GUIContent(cloneLabel);
                            Vector2 cloneSize = _promptStyle.CalcSize(cloneContent);
                            float cw = Mathf.Max(w, cloneSize.x + 40f * scale);
                            var cloneRect = new Rect(
                                guiPoint.x - cw / 2f, rect.y + h + 4f * scale, cw, h);
                            GUI.Box(cloneRect, cloneLabel, _promptStyle);
                        }
                    }

                    // The Boat Man also offers Gardey Zafeh, shown as a
                    // second line - the explicit E/R choice the user asked
                    // for instead of the game silently picking one.
                    if (_current is TownNPCInteractable boatManPrompt)
                    {
                        string gzLabel = boatManPrompt.GardeyZafehLabel;
                        if (!string.IsNullOrEmpty(gzLabel))
                        {
                            var gzContent = new GUIContent(gzLabel);
                            Vector2 gzSize = _promptStyle.CalcSize(gzContent);
                            float gw = Mathf.Max(w, gzSize.x + 40f * scale);
                            var gzRect = new Rect(
                                guiPoint.x - gw / 2f, rect.y + h + 4f * scale, gw, h);
                            GUI.Box(gzRect, gzLabel, _promptStyle);
                        }
                    }
                }
            }

            // MINI-073: smaller box (was 900x130) and fades out over
            // feedbackFadeSeconds instead of vanishing at the exact instant
            // the timer expires - both per the user's "smaller ... fade away
            // quicker" ask.
            float feedbackAlpha = FeedbackAlpha();
            if (feedbackAlpha > 0f && !string.IsNullOrEmpty(_feedback))
            {
                float w = 620f * scale;
                float h = 92f * scale;
                var rect = new Rect(Screen.width / 2f - w / 2f, Screen.height - h - 26f * scale, w, h);

                Color prevColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, feedbackAlpha);
                GUI.Box(rect, _feedback, _feedbackStyle);
                GUI.color = prevColor;
            }
        }
    }
}
