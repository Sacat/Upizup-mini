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
    /// held, the second if-check wins) - E stands in for LeftControl
    /// (wheelieInput = -1), Q stands in for LeftShift (wheelieInput = 1).
    /// If that direction feels backwards in play, swap which key sets
    /// which sign here - it's a one-line change either way.
    /// </summary>
    public class SuperMotoWheelieKeyRemap : Input_Manager
    {
        protected override void GetLeanBackValue()
        {
            if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Q))
            {
                if (Input.GetKey(KeyCode.E)) wheelieInput = -1;
                if (Input.GetKey(KeyCode.Q)) wheelieInput = 1;
            }
            else
            {
                wheelieInput = 0;
            }
        }
    }
}
