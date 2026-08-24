namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-119 follow-up, user: "put some quick controls on the screen
    /// i want to test it using my phone" (via remote desktop/screen
    /// mirroring - taps arrive as ordinary mouse clicks on a UI Button).
    /// Plain static hold-state that on-screen buttons set/clear via
    /// EventTrigger PointerDown/PointerUp - SuperMotoWheelieKeyRemap ORs
    /// these in right alongside the real keyboard checks, so keyboard
    /// and on-screen buttons both just work through the exact same code
    /// path, nothing duplicated.
    /// </summary>
    public static class SuperMotoTouchInputState
    {
        public static bool Throttle;
        public static bool ReverseBrake;
        public static bool SteerLeft;
        public static bool SteerRight;
        public static bool LeanLeft;
        public static bool LeanRight;
        public static bool WheelieE;
        public static bool WheelieQ;
        public static bool FrontBrake;

        private static bool _mountPressed;
        public static void PressMount() => _mountPressed = true;

        /// <summary>Edge-triggered, same as Input.GetKeyDown - true only
        /// once per press, consumed on read.</summary>
        public static bool ConsumeMountPressed()
        {
            if (!_mountPressed) return false;
            _mountPressed = false;
            return true;
        }
    }
}
