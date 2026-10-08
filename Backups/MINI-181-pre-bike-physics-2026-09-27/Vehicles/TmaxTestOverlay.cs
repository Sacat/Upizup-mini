using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-066. Shared show/hide state for the TMAX test scene's on-screen
    /// overlays (the controls help box and the speed/wheelie readout).
    ///
    /// Shared rather than a per-component toggle so one key press hides them
    /// together - two separate hide keys for two boxes that always appear
    /// together would be worse than the problem. The wheelie tuner keeps its
    /// own separate T key, since that one is a working panel rather than a
    /// readout and is often wanted while the readouts are hidden.
    ///
    /// Guarded by frame number because several components poll this in the
    /// same frame; without that the toggle would fire once per listener and
    /// cancel itself out.
    /// </summary>
    public static class TmaxTestOverlay
    {
        public static KeyCode ToggleKey = KeyCode.H;

        private static int _lastPolledFrame = -1;

        /// <summary>True while the readout overlays should be drawn.</summary>
        public static bool Visible { get; private set; } = true;

        /// <summary>
        /// Call once per frame from any overlay component's Update. Only the
        /// first caller in a given frame actually evaluates the key.
        /// </summary>
        public static void Poll()
        {
            if (_lastPolledFrame == Time.frameCount) return;
            _lastPolledFrame = Time.frameCount;

            if (Input.GetKeyDown(ToggleKey)) Visible = !Visible;
        }

        /// <summary>A one-line reminder of how to bring the overlays back,
        /// drawn while they are hidden so the key is never lost.</summary>
        public static void DrawHiddenHint()
        {
            GUI.Label(new Rect(10, 10, 320, 22), $"[ {ToggleKey} ] show controls / readout");
        }
    }
}
