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
    /// MINI-119 follow-up fix, user: "Q leans back and E leans forward. i
    /// want E as the wheelie." The BIKE's own real wheelie (this project's
    /// SuperMotoWheelieAssist, since the kinematic-port rewrite) no longer
    /// cares which sign either key sends - either one triggers the same
    /// nose-up lift. But there's a SEPARATE, purely cosmetic script in the
    /// pack itself (Gadd420.PlayerLeaning, on the rider) that shifts the
    /// rider's own local position forward/back straight off
    /// Input_Manager.WheelieInput's raw sign, with no gating at all:
    /// positive leans the rider forward, negative leans them back. That's
    /// exactly what the user was seeing and is what this remap's sign
    /// actually controls now. Swapped so E sends the sign PlayerLeaning
    /// reads as "back" (the correct wheelie-prep visual - a rider leans
    /// their weight BACK as the front comes up, not forward) - the real
    /// physics is unaffected either way since it no longer reads sign.
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
