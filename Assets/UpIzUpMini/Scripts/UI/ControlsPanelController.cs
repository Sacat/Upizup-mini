using UnityEngine;
using UnityEngine.UI;

namespace UpIzUpMini.UI
{
    /// <summary>H toggles a controls reference overlay.</summary>
    public class ControlsPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text bodyText;

        private const string Controls =
            "<b>CONTROLS</b>\n\n" +
            "WASD / Arrows      Move\n" +
            "Left Shift         Run\n" +
            "Space              Jump\n" +
            "Mouse              Look around\n" +
            "Tab                Switch between Franki and Sacat\n" +
            "E                  Talk / Buy / Sell / Plant / Water / Harvest\n" +
            "R                  Clone a ripe plant for extra seed\n" +
            "1 - 4              Choose crop to plant\n" +
            "F5 / F9            Save / Load\n" +
            "F11 / Alt+Enter    Fullscreen or windowed\n" +
            "H                  Show or hide this list\n" +
            "Esc                Pause menu (Q to quit)\n\n" +
            "Walk into the sea to swim.\n" +
            "Running out of stamina drops you to a walk, it never stops you.";

        private void Awake()
        {
            if (bodyText != null) bodyText.text = Controls;
            if (panel != null) panel.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.H) && panel != null)
            {
                panel.SetActive(!panel.activeSelf);
            }
        }
    }
}
