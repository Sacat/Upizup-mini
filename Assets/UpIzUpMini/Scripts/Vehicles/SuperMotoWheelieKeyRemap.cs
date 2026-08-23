using Gadd420;
using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-119 follow-up, user: "the E button to wheelie instead of left
    /// control and the Q button will be used instead of the left shift."
    /// Surgical remap, not a full input takeover like GaddInputAdapter -
    /// overrides ONLY the one virtual method that reads the wheelie keys
    /// (Input_Manager.GetLeanBackValue), leaving Vertical/HZ/Lean/
    /// FrontBreak exactly as the stock asset already reads them (W/S, A/D,
    /// mouse buttons, Space), per the user's own "use what is there and
    /// works... we tweak it."
    ///
    /// Same left-to-right precedence as the base method (if both keys are
    /// held, the second if-check wins).
    ///
    /// MINI-119 follow-up fix, user: "get the lean fully solved... try
    /// with max effort." E and Q originally mapped to -1/+1 respectively,
    /// guessed from reading RB_Controller.Stoppies() (gated on WheelieInput
    /// == 1, which looked like it should be the OTHER, non-wheelie
    /// direction). A direct empirical test proved that guess backwards:
    /// wheelieInput = -1 consistently, cleanly pitched the nose DOWN over
    /// a real 15-second sustained hold; wheelieInput = +1 pitched it UP
    /// (until it overshot the OLD 90deg cap and hit a separate asin-
    /// singularity bug, since fixed - see SuperMotoWheelieAssist.
    /// rampCeilingDeg's own comment). Swapped to match reality: E (the
    /// key the user actually wants to lift the nose) now sends +1, Q
    /// sends -1.
    /// </summary>
    public class SuperMotoWheelieKeyRemap : Input_Manager
    {
        protected override void GetLeanBackValue()
        {
            if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Q))
            {
                if (Input.GetKey(KeyCode.E)) wheelieInput = 1;
                if (Input.GetKey(KeyCode.Q)) wheelieInput = -1;
            }
            else
            {
                wheelieInput = 0;
            }
        }
    }
}
