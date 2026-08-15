using UnityEngine;

namespace UpIzUpMini.Character
{
    /// <summary>
    /// Camera-relative walk/run controller. Uses the legacy Input Manager
    /// (Horizontal/Vertical, Left Shift to run) since no Input System
    /// package is installed in this project yet. Only processes input
    /// while <see cref="IsControlled"/> is true - CharacterSwitchManager
    /// flips this when the player switches between Smart and Strong, and
    /// hands movement of the inactive character to FollowController
    /// instead.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        // Locomotion now uses the StarterAssets/Mixamo clips taken from the
        // larger Up Iz Up project. Those are in-place clips with no root
        // motion, so the correct speeds are the ones Unity tuned them for
        // in ThirdPersonController: MoveSpeed 2.0, SprintSpeed 5.335,
        // JumpHeight 1.2, Gravity -15. Using the animation's own numbers
        // is what stops the feet skating.
        [SerializeField] private float walkSpeed = 2.0f;
        [SerializeField] private float runSpeed = 5.335f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float speedChangeRate = 10f;
        [SerializeField] private float turnSpeed = 12f;
        [SerializeField] private float gravity = -15f;

        [SerializeField] private Animator animator;
        [SerializeField] private string speedParam = "Speed";
        [SerializeField] private string jumpParam = "Jump";
        [SerializeField] private string groundedParam = "Grounded";
        [SerializeField] private CharacterVitals vitals;

        public bool IsJumping { get; private set; }

        private CharacterController _controller;
        private float _verticalVelocity;
        private float _animSpeedBlend;

        public bool IsControlled { get; set; } = true;
        public bool IsRunning { get; private set; }
        public float CurrentSpeed { get; private set; }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
            if (vitals == null)
            {
                vitals = GetComponent<CharacterVitals>();
            }
        }

        private void Update()
        {
            if (!IsControlled)
            {
                // Still apply gravity so a paused-control character doesn't
                // float if it was mid-air, but no input/animation.
                if (_controller.enabled)
                {
                    if (_controller.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -1f;
                    _verticalVelocity += gravity * Time.deltaTime;
                    _controller.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
                }
                return;
            }

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 inputDir = new Vector3(h, 0f, v);
            if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();

            Transform cam = Camera.main != null ? Camera.main.transform : transform;
            Vector3 camForward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
            Vector3 moveDir = camForward * inputDir.z + camRight * inputDir.x;

            bool wantsRun = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (vitals != null)
            {
                vitals.SetRunning(wantsRun && moveDir.sqrMagnitude > 0.001f);
                // Running out of stamina drops the player to a walk - it
                // must never bring them to a complete stop.
                wantsRun = wantsRun && vitals.CanRun;
            }
            IsRunning = wantsRun;

            float speed = IsRunning ? runSpeed : walkSpeed;
            CurrentSpeed = moveDir.sqrMagnitude > 0.001f ? speed : 0f;

            if (moveDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            }

            bool grounded = _controller.isGrounded;
            if (grounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -1f;
            }

            if (grounded && Input.GetKeyDown(KeyCode.Space))
            {
                // v = sqrt(2 * g * h) for the requested apex height.
                _verticalVelocity = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
                IsJumping = true;
                animator?.SetTrigger(jumpParam);
            }

            if (grounded && _verticalVelocity <= 0f) IsJumping = false;

            _verticalVelocity += gravity * Time.deltaTime;

            Vector3 motion = moveDir * speed + Vector3.up * _verticalVelocity;
            _controller.Move(motion * Time.deltaTime);

            if (animator != null)
            {
                // Speed is in real m/s to match the blend tree thresholds
                // (0 / 2.0 / 5.335), the same convention StarterAssets uses
                // for these clips.
                _animSpeedBlend = Mathf.Lerp(_animSpeedBlend, CurrentSpeed, speedChangeRate * Time.deltaTime);
                if (_animSpeedBlend < 0.01f) _animSpeedBlend = 0f;
                animator.SetFloat(speedParam, _animSpeedBlend);
                animator.SetBool(groundedParam, grounded);
            }
        }
    }
}
