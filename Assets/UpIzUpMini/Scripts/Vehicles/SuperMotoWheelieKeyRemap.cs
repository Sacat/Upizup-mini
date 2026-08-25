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
        // MINI-119 follow-up fix, user: "it still doesnt wheelie... i
        // didnt see the screen read out if E was held or not."
        //
        // THE most likely reason the wheelie never worked in the real
        // game while every batch test passed: Input_Manager.Update() -
        // the method that actually reads the keyboard and sets
        // wheelieInput - is declared PRIVATE in the vendor's base class.
        // Unity's message dispatch finding a private Update() declared on
        // a BASE class (when the live component is this subclass) is a
        // known Unity gotcha and cannot be relied on. If it doesn't fire,
        // wheelieInput stays 0 forever, no key is ever registered, and no
        // amount of physics work downstream can produce a wheelie.
        //
        // That matches the evidence exactly: every batch test in this task
        // set wheelieInput DIRECTLY via reflection (bypassing Update
        // entirely) and always passed, while the real game - which has
        // nothing but this Update to read the E key - never once
        // wheelied. Declaring Update() here removes the ambiguity
        // completely: Unity always dispatches to the most-derived
        // Update(), so this is guaranteed to run. All five input readers
        // are called explicitly, exactly as the base did, so throttle/
        // steering/lean/brake behave identically to before.
        private void Update()
        {
            VerticalInput();
            HZInput();
            GetLeanValue();
            GetLeanBackValue();
            FrontBreak();
        }

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

        // MINI-119 follow-up, user: "i want the arrow keys to be apart of
        // the controls as well for the [ride] and wheelie. just map
        // them." Up/Down/Left/Right now work as full alternates for W/S/
        // A/D - either set works, and both can be held together. Since
        // holding throttle + E is what starts a wheelie, Up+E works
        // exactly like W+E already did, with no separate wheelie-specific
        // arrow binding needed. The base class's own timer fields
        // (vInputTime/hzInputTime) are private, not protected, so this
        // duplicates the exact same smoothing curve with its own timers
        // rather than reusing them - same feel, arrow keys included.
        private float _vInputTime;
        private float _hzInputTime;

        protected override void VerticalInput()
        {
            _vInputTime = Mathf.Clamp(_vInputTime, 0, inputSmoothingTime);

            bool up = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
            bool down = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

            if (up || down)
            {
                if (up)
                {
                    if (vInput < 0)
                    {
                        _vInputTime -= 2 * Time.deltaTime;
                        vInput = -Mathf.InverseLerp(0, inputSmoothingTime, _vInputTime);
                    }
                    else
                    {
                        _vInputTime += 1 * Time.deltaTime;
                        vInput = Mathf.InverseLerp(0, inputSmoothingTime, _vInputTime);
                    }
                }
                if (down)
                {
                    if (vInput > 0.01f)
                    {
                        _vInputTime -= 2 * Time.deltaTime;
                        vInput = Mathf.InverseLerp(0, inputSmoothingTime, _vInputTime);
                    }
                    else
                    {
                        _vInputTime += 1 * Time.deltaTime;
                        vInput = -Mathf.InverseLerp(0, inputSmoothingTime, _vInputTime);
                    }
                }
            }
            else
            {
                if (_vInputTime > 0.01f)
                {
                    _vInputTime -= 1 * Time.deltaTime;
                    if (vInput < 0) vInput = -Mathf.InverseLerp(0, inputSmoothingTime, _vInputTime);
                    if (vInput > 0) vInput = Mathf.InverseLerp(0, inputSmoothingTime, _vInputTime);
                }
                else
                {
                    _vInputTime = 0;
                    vInput = 0;
                }
            }
        }

        protected override void HZInput()
        {
            _hzInputTime = Mathf.Clamp(_hzInputTime, 0, inputSmoothingTime);

            bool right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
            bool left = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);

            if (right || left)
            {
                if (right)
                {
                    if (hzInput < 0)
                    {
                        _hzInputTime -= 2 * Time.deltaTime;
                        hzInput = -Mathf.InverseLerp(0, inputSmoothingTime, _hzInputTime);
                    }
                    else
                    {
                        _hzInputTime += 1 * Time.deltaTime;
                        hzInput = Mathf.InverseLerp(0, inputSmoothingTime, _hzInputTime);
                    }
                }
                if (left)
                {
                    if (hzInput > 0.01f)
                    {
                        _hzInputTime -= 2 * Time.deltaTime;
                        hzInput = Mathf.InverseLerp(0, inputSmoothingTime, _hzInputTime);
                    }
                    else
                    {
                        _hzInputTime += 1 * Time.deltaTime;
                        hzInput = -Mathf.InverseLerp(0, inputSmoothingTime, _hzInputTime);
                    }
                }
            }
            else
            {
                if (_hzInputTime > 0.01f)
                {
                    _hzInputTime -= 1 * Time.deltaTime;
                    if (hzInput < 0) hzInput = -Mathf.InverseLerp(0, inputSmoothingTime, _hzInputTime);
                    if (hzInput > 0) hzInput = Mathf.InverseLerp(0, inputSmoothingTime, _hzInputTime);
                }
                else
                {
                    _hzInputTime = 0;
                    hzInput = 0;
                }
            }
        }
    }
}
