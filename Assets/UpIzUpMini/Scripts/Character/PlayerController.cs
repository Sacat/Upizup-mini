using UnityEngine;

namespace UpIzUpMini.Character
{
    /// <summary>
    /// Camera-relative walk/run controller for the MINI-001 proof capsule.
    /// Uses the legacy Input Manager (Horizontal/Vertical, Left Shift to
    /// run) since no Input System package is installed in this project yet.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float walkSpeed = 3.2f;
        [SerializeField] private float runSpeed = 6.5f;
        [SerializeField] private float turnSpeed = 12f;
        [SerializeField] private float gravity = -20f;

        [SerializeField] private Animator animator;
        [SerializeField] private string speedParam = "Speed";

        private CharacterController _controller;
        private float _verticalVelocity;
        private float _animSpeedBlend;

        public bool IsRunning { get; private set; }
        public float CurrentSpeed { get; private set; }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        private void Update()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 inputDir = new Vector3(h, 0f, v);
            if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();

            Transform cam = Camera.main != null ? Camera.main.transform : transform;
            Vector3 camForward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
            Vector3 moveDir = camForward * inputDir.z + camRight * inputDir.x;

            IsRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
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
