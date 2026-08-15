using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UpIzUpMini.UI
{
    /// <summary>
    /// Esc opens/closes a pause menu (Resume / Quit, mouse-clickable and
    /// keyboard-navigable via Unity's default UI navigation). While paused,
    /// Q quits immediately. Also owns the game's default cursor-lock state
    /// (locked+hidden while playing, unlocked+visible while paused) since
    /// ThirdPersonFollowCamera's mouse-look reads that same lock state.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button quitButton;

        private bool _paused;

        private void Start()
        {
            SetPaused(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetPaused(!_paused);
            }

            if (_paused && Input.GetKeyDown(KeyCode.Q))
            {
                QuitGame();
            }
        }

        public void SetPaused(bool paused)
        {
            _paused = paused;
            if (panel != null) panel.SetActive(paused);

            Time.timeScale = paused ? 0f : 1f;
            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = paused;

            if (paused && resumeButton != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
            }
        }

        public void ResumeGame() => SetPaused(false);

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
