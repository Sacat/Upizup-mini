using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.UI
{
    /// <summary>
    /// MINI-119 follow-up, user: "put some quick controls on the screen
    /// i want to test it using my phone" (via remote desktop/screen
    /// mirroring, per the user's own confirmed test method - taps arrive
    /// as ordinary mouse clicks). Self-bootstraps on scene load, no scene
    /// wiring needed - builds a plain runtime UI Canvas with hold-buttons
    /// for throttle/brake/steer/lean/wheelie and a tap-button for mount/
    /// dismount, all just setting UpIzUpMini.Vehicles.SuperMotoTouchInputState's
    /// static flags. SuperMotoWheelieKeyRemap ORs those flags in
    /// alongside the real keyboard, so this is purely additive - nothing
    /// about keyboard/mouse play changes.
    ///
    /// Deliberately bike-only for this quick pass - on-foot movement
    /// (PlayerController) isn't wired here, that's a separate system.
    /// </summary>
    public class SuperMotoTouchControlsOverlay : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<SuperMotoTouchControlsOverlay>() != null) return;
            var go = new GameObject("SuperMotoTouchControlsOverlay");
            go.AddComponent<SuperMotoTouchControlsOverlay>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
                DontDestroyOnLoad(esGo);
            }

            var canvasGo = new GameObject("SuperMotoTouchCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            var rt = canvasGo.GetComponent<RectTransform>();

            // Bottom-left cluster: throttle/reverse/steer.
            HoldButton(rt, "W", new Vector2(140f, 220f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                () => SuperMotoTouchInputState.Throttle = true, () => SuperMotoTouchInputState.Throttle = false);
            HoldButton(rt, "S", new Vector2(140f, 120f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                () => SuperMotoTouchInputState.ReverseBrake = true, () => SuperMotoTouchInputState.ReverseBrake = false);
            HoldButton(rt, "A", new Vector2(60f, 170f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                () => SuperMotoTouchInputState.SteerLeft = true, () => SuperMotoTouchInputState.SteerLeft = false);
            HoldButton(rt, "D", new Vector2(220f, 170f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                () => SuperMotoTouchInputState.SteerRight = true, () => SuperMotoTouchInputState.SteerRight = false);

            // Bottom-right cluster: lean/wheelie/brake.
            HoldButton(rt, "LEAN\nL", new Vector2(-260f, 170f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                () => SuperMotoTouchInputState.LeanLeft = true, () => SuperMotoTouchInputState.LeanLeft = false);
            HoldButton(rt, "LEAN\nR", new Vector2(-140f, 170f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                () => SuperMotoTouchInputState.LeanRight = true, () => SuperMotoTouchInputState.LeanRight = false);
            HoldButton(rt, "E\nWHEELIE", new Vector2(-260f, 280f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                () => SuperMotoTouchInputState.WheelieE = true, () => SuperMotoTouchInputState.WheelieE = false);
            HoldButton(rt, "Q\nWHEELIE", new Vector2(-140f, 280f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                () => SuperMotoTouchInputState.WheelieQ = true, () => SuperMotoTouchInputState.WheelieQ = false);
            HoldButton(rt, "BRAKE", new Vector2(-200f, 60f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                () => SuperMotoTouchInputState.FrontBrake = true, () => SuperMotoTouchInputState.FrontBrake = false);

            // Mount/Dismount - a tap, not a hold.
            TapButton(rt, "F\nMOUNT/OFF", new Vector2(0f, 60f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                () => SuperMotoTouchInputState.PressMount());

            Debug.Log("MINI-119 TOUCH CONTROLS: on-screen bike controls added (throttle/brake/steer/lean/wheelie/mount). Keyboard still works exactly as before - this is purely additive.");
        }

        private static void HoldButton(RectTransform canvasRt, string label, Vector2 anchoredPos, Vector2 anchorMin, Vector2 anchorMax,
            System.Action onDown, System.Action onUp)
        {
            var button = CreateButtonBase(canvasRt, label, anchoredPos, anchorMin, anchorMax);
            var trigger = button.gameObject.AddComponent<EventTrigger>();

            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener(_ => onDown());
            trigger.triggers.Add(down);

            var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            up.callback.AddListener(_ => onUp());
            trigger.triggers.Add(up);

            // PointerExit too - a finger/mouse dragging off the button
            // while still "down" must not leave the input stuck true.
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => onUp());
            trigger.triggers.Add(exit);
        }

        private static void TapButton(RectTransform canvasRt, string label, Vector2 anchoredPos, Vector2 anchorMin, Vector2 anchorMax,
            System.Action onTap)
        {
            var button = CreateButtonBase(canvasRt, label, anchoredPos, anchorMin, anchorMax);
            button.onClick.AddListener(() => onTap());
        }

        private static Button CreateButtonBase(RectTransform canvasRt, string label, Vector2 anchoredPos, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject($"Btn_{label.Replace("\n", "_")}");
            go.transform.SetParent(canvasRt, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(90f, 90f);
            rt.anchoredPosition = anchoredPos;

            var image = go.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.35f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            return button;
        }
    }
}
