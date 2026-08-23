using Gadd420;
using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-119: the imported Motorbike Physics Tool's own Input_Manager
    /// reads Input.GetKey directly inside Update() - exactly the coupling
    /// this project's own architecture note (see TmaxBikeControllerCustom's
    /// header) explicitly avoids, since it means nothing but a literal
    /// keyboard can ever drive it (no mobile buttons, no AI, no swapped
    /// key bindings without editing the asset's own file).
    ///
    /// This subclass overrides every one of its input-reading methods to
    /// do nothing, and instead exposes a single method (SetRawInputs) that
    /// writes straight into the SAME protected fields the base class
    /// already exposes via its public HzInput/VInput/LeanInput/
    /// WheelieInput/FrontBreakInput getters - so Gadd420.RB_Controller
    /// (which only ever reads those getters, never anything else) behaves
    /// identically whether the values came from a real keyboard or from
    /// our own decoupled SetInput/SetWheelieHeld pipeline.
    /// </summary>
    public class GaddInputAdapter : Input_Manager
    {
        protected override void VerticalInput() { }
        protected override void HZInput() { }
        protected override void GetLeanValue() { }
        protected override void GetLeanBackValue() { }
        protected override void FrontBreak() { }

        /// <summary>
        /// throttle/steer: -1..1, matching TmaxBikeController.SetInput's own
        /// contract. wheelieHeld: true while our own wheelie key (E, see
        /// BikeInteractable.wheelieKey) is held. brake: 0..1.
        /// </summary>
        public void SetRawInputs(float throttle, float steer, float brake, bool wheelieHeld)
        {
            vInput = Mathf.Clamp(throttle, -1f, 1f);
            hzInput = Mathf.Clamp(steer, -1f, 1f);
            frontBreakInput = Mathf.Clamp01(brake);
            // The base controller's own wheelie mechanic reads a signed
            // WheelieInput (+1 lifts the front, -1 is its stoppie/brake-lean
            // case - see RB_Controller's own Stoppies()/AddTorque()) - our
            // single wheelieHeld bool maps to the lift case only, +1.
            wheelieInput = wheelieHeld ? 1f : 0f;
            // Lean input (cornering lean assist) mirrors steer, the same
            // "combineLeanAndSteering" behaviour the base class already
            // supports natively - one input source, not two to keep in sync.
            leanInput = hzInput;
        }
    }
}
