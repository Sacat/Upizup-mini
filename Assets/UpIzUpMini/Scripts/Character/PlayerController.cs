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
        // Tuned to the authored pace of the Human Basic Motions clips
        // (Walk01 ~1.9 m/s, Run01 ~4.4 m/s). The previous 3.2/6.5 moved the
        // capsule far faster than the animation's stride, which is what made
        // running look wrong - the feet skated across the ground.
        [SerializeField] private float walkSpeed = 1.9f;
        [SerializeField] private float runSpeed = 4.4f;
        [SerializeField] private float turnSpeed = 12f;
        [SerializeField] private float gravity = -20f;

        [SerializeField] private Animator animator;
        [SerializeField] private string speedParam = "Speed";
        [SerializeField] private CharacterVitals vitals;

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

            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -1f;
            }
            _verticalVelocity += gravity * Time.deltaTime;

            Vector3 motion = moveDir * speed + Vector3.up * _verticalVelocity;
            _controller.Move(motion * Time.deltaTime);

            if (animator != null)
            {
                // 0 = idle, 1 = walk, 2 = run. Blend tree in Mini011PhaseBSetup's
                // generated controller expects this normalized range.
                float target = CurrentSpeed <= 0f ? 0f : (IsRunning ? 2f : 1f);
                _animSpeedBlend = Mathf.MoveTowards(_animSpeedBlend, target, 12f * Time.deltaTime);
                animator.SetFloat(speedParam, _animSpeedBlend);
            }
        }
    }
}
