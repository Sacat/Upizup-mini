using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-119 follow-up, user: "there is some type of bycicle animation
    /// going on when i click on play... looks like he is ridding a
    /// bycicle and not supermoto." Real cause: SuperMotoStockInteractable.
    /// Mount() freezes the Animator (Speed/MotionSpeed=0, animator.speed=0)
    /// via plain runtime calls - exactly like PlayerController.IsControlled
    /// before its own fix, THIS state is not Unity-serializable data at
    /// all (it lives in the Animator's PlayableGraph, rebuilt fresh every
    /// Play session) - it can never survive a scene save no matter how
    /// it's set. Without it, Play falls back to whatever Speed default is
    /// baked into the Animator Controller asset itself, which combined
    /// with legs already IK-posed toward the bike's foot pegs (bent,
    /// pedal-height) reads as riding a bicycle.
    ///
    /// The correct fix, not a workaround: persist a COMPONENT instead of
    /// runtime state. A component's Awake() is a real Unity lifecycle
    /// guarantee - it runs automatically every single Play session,
    /// completely independent of Editor save/reload cycles, unlike any
    /// value set by a one-off script. Add this once (saved with the
    /// scene) to any character parked in a frozen/mounted pose and the
    /// freeze re-applies itself every time Play starts, permanently.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class MountedAnimatorFreeze : MonoBehaviour
    {
        private Animator _animator;

        private void Awake() => _animator = GetComponent<Animator>();

        // MINI-119 follow-up fix, user: "there is a weird animation as i
        // am mounted... looks like a falling or cycling animation."
        // Setting animator.speed=0 in Awake() (the very first frame,
        // before the state machine has had any chance to settle into an
        // actual idle state) could freeze him mid-transition, stuck
        // showing whatever the controller's default/entry state happens
        // to be (a jump/fall pose, in this case) forever. Fixed by never
        // touching animator.speed at all - the state machine transitions
        // normally at its own pace, and this just keeps re-pinning the
        // parameters that determine WHICH state it settles into every
        // frame (Speed/MotionSpeed=0, Grounded=true, Jump/FreeFall=false),
        // so it always lands on and stays in Idle, no freeze/stuck-frame
        // risk at all.
        private void Update()
        {
            if (_animator == null) return;

            _animator.SetFloat("Speed", 0f);
            _animator.SetFloat("MotionSpeed", 0f);
            _animator.SetBool("Grounded", true);
            _animator.SetBool("Jump", false);
            _animator.SetBool("FreeFall", false);
        }
    }
}
