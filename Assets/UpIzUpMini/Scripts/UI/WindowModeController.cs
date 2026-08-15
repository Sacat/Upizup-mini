using UnityEngine;

namespace UpIzUpMini.UI
{
    /// <summary>
    /// Runs the game windowed by default and lets the player toggle
    /// fullscreen with F11 (or Alt+Enter), so the window can be moved,
    /// resized and minimised like a normal application. Exclusive
    /// fullscreen is avoided because it prevents minimising cleanly.
    /// </summary>
    public class WindowModeController : MonoBehaviour
    {
        [SerializeField] private int windowedWidth = 1600;
        [SerializeField] private int windowedHeight = 900;
        [SerializeField] private bool startWindowed = true;

        private void Start()
        {
            if (startWindowed && Screen.fullScreen)
            {
                SetWindowed();
            }
        }

        private void Update()
        {
            bool altEnter = (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                            && Input.GetKeyDown(KeyCode.Return);

            if (Input.GetKeyDown(KeyCode.F11) || altEnter)
            {
                if (Screen.fullScreen) SetWindowed();
                else SetFullscreen();
            }
        }

        private void SetWindowed()
        {
            Screen.SetResolution(windowedWidth, windowedHeight, FullScreenMode.Windowed);
        }

        private void SetFullscreen()
        {
            // Borderless rather than exclusive, so alt-tab and minimise
            // behave normally.
            var res = Screen.currentResolution;
            Screen.SetResolution(res.width, res.height, FullScreenMode.FullScreenWindow);
        }
    }
}
