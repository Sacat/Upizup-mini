using UnityEngine;

namespace UpIzUpMini.Character
{
    /// <summary>
    /// Camera-relative walk/run controller.
    ///
    /// The movement and animation driving here deliberately mirrors the
    /// larger Up Iz Up project's `ThirdPersonController.cs`, because that
    /// is the combination its locomotion clips were authored and tuned
    /// against. Two details matter and were previously missing, which is
    /// why walking/running looked wrong:
    ///
    /// 1. **MotionSpeed.** The StarterAssets controller scales animation
    ///    playback by a `MotionSpeed` parameter. Without it the clip plays
    ///    at a fixed rate no matter how fast the character actually moves,
    ///    so the feet never agree with the ground.
    /// 2. **Smoothed speed.** Both the movement speed and the animator's
    ///    `Speed` value are lerped toward their target at
    ///    `SpeedChangeRate`, rather than snapping. The clips' blends assume
    ///    that easing.
    ///
    /// Only processes input while <see cref="IsControlled"/> is true;
    /// CharacterSwitchManager flips this when switching between Franki and
    /// Sacat, handing the other to FollowController.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        // Values taken from the larger project's ThirdPersonController, as
        // the clips are in-place and carry no root motion of their own.
        [SerializeField] private float walkSpeed = 2.0f;
        [SerializeField] private float runSpeed = 5.335f;
        [SerializeField] private float speedChangeRate = 10.0f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -15.0f;
        [SerializeField] private float turnSpeed = 12f;

        [SerializeField] private Animator animator;
        [SerializeField] private CharacterVitals vitals;

        private CharacterController _controller;
        private float _verticalVelocity;
        private float _speed;
        private float _animationBlend;

        // Matches the StarterAssetsThirdPerson controller's parameters.
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        public bool IsControlled { get; set; } = true;
        public bool IsRunning { get; private set; }
        public float CurrentSpeed { get; private set; }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (vitals == null) vitals = GetComponent<CharacterVitals>();

            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void Update()
        {
            if (!IsControlled)
            {
                if (_controller.enabled)
                {
                    if (_controller.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
                    _verticalVelocity += gravity * Time.deltaTime;
                    _controller.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
                }
                return;
            }

            bool grounded = _controller.isGrounded;

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 inputDir = new Vector3(h, 0f, v);
            bool hasInput = inputDir.sqrMagnitude > 0.001f;
            if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();

            Transform cam = Camera.main != null ? Camera.main.transform : transform;
            Vector3 camForward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
            Vector3 moveDir = (camForward * inputDir.z + camRight * inputDir.x).normalized;

            bool wantsRun = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (vitals != null)
            {
                vitals.SetRunning(wantsRun && hasInput);
                // Exhaustion only removes the run option - it must never
                // bring the player to a complete stop.
                wantsRun = wantsRun && vitals.CanRun;
            }
            IsRunning = wantsRun && hasInput;

            float targetSpeed = hasInput ? (wantsRun ? runSpeed : walkSpeed) : 0f;

            // Ease toward the target rather than snapping, as the clips'
            // blending assumes (StarterAssets does the same).
            Vector3 horizontalVelocity = _controller.velocity;
            horizontalVelocity.y = 0f;
            float currentHorizontal = horizontalVelocity.magnitude;

            if (Mathf.Abs(currentHorizontal - targetSpeed) > 0.1f)
            {
                _speed = Mathf.Lerp(currentHorizontal, targetSpeed, Time.deltaTime * speedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * speedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            CurrentSpeed = _speed;

            if (hasInput)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            }

            if (grounded)
            {
                if (_verticalVelocity < 0f) _verticalVelocity = -2f;

                if (Input.GetKeyDown(KeyCode.Space))
                {
                    _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    animator?.SetBool(_animIDJump, true);
                }
                else
                {
                    animator?.SetBool(_animIDJump, false);
                }
                animator?.SetBool(_animIDFreeFall, false);
            }
            else
            {
                animator?.SetBool(_animIDJump, false);
                if (_verticalVelocity < -2f) animator?.SetBool(_animIDFreeFall, true);
            }

            _verticalVelocity += gravity * Time.deltaTime;

            _controller.Move(moveDir * (_speed * Time.deltaTime)
                             + Vector3.up * (_verticalVelocity * Time.deltaTime));

            if (animator != null)
            {
                animator.SetBool(_animIDGrounded, grounded);
                animator.SetFloat(_animIDSpeed, _animationBlend);
                // Scales clip playback so the stride matches the distance
                // actually travelled. This was the missing piece.
                animator.SetFloat(_animIDMotionSpeed, hasInput ? 1f : 0f);
            }
        }
    }
}
