using UnityEngine;
using UpIzUpMini.Character;

namespace UpIzUpMini.UI
{
    /// <summary>
    /// MINI-194: the cursor used to start unlocked, so on a fresh launch the camera could not orbit and the sidearm stayed holstered/unusable
    /// (FirearmController and the orbit camera both require a locked cursor) until the player had opened and closed a menu. Locks it once the
    /// playable scene is running and re-locks when the window regains focus after the game itself had locked it. UI panels still unlock it
    /// through their own code; this never fights them.
    /// </summary>
    public class StartupCursorLock : MonoBehaviour
    {
        float lockAt;
        bool done, lockedByGame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (!Application.isPlaying) return;
            var go = new GameObject("StartupCursorLock"); go.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(go); go.AddComponent<StartupCursorLock>();
        }

        void Start() { lockAt = Time.unscaledTime + 0.75f; }

        void Update()
        {
            if (!done && Time.unscaledTime >= lockAt)
            {
                done = true;
                if (FindFirstObjectByType<PlayerController>() != null && Time.timeScale > 0.001f)
                {
                    Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; lockedByGame = true;
                }
            }
            else if (done && Cursor.lockState == CursorLockMode.Locked) lockedByGame = true;
        }

        void OnApplicationFocus(bool focus)
        {
            // regained focus after alt-tab: restore the lock unless a panel left the cursor visible on purpose
            if (focus && done && lockedByGame && Time.timeScale > 0.001f && !Cursor.visible) Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
