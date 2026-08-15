using UnityEngine;

namespace UpIzUpMini.Character
{
    /// <summary>
    /// Simple follow behaviour for the currently-inactive boy (per
    /// Docs/STORY.md: "the inactive boy must remain a real world
    /// character... he follows the active boy at a sensible distance").
    /// Deliberately simple - direct steering toward the target with
    /// CharacterController.SimpleMove, not a NavMesh agent. Good enough for
    /// open ground; may cut corners around obstacles. Only active while
    /// this character is not the player-controlled one.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FollowController : MonoBehaviour
    {
        [SerializeField] private float followDistance = 3.5f;
        // Matches the walk clip's tuned speed (StarterAssets MoveSpeed) so
        // the companion's stride reads correctly.
        [SerializeField] private float moveSpeed = 2.0f;
        [SerializeField] private float turnSpeed = 8f;
        [SerializeField] private Animator animator;
        [SerializeField] private string speedParam = "Speed";

        private CharacterController _controller;
        private float _animSpeedBlend;

        public Transform FollowTarget { get; set; }
        public bool FollowingEnabled { get; set; } = true;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            if (!FollowingEnabled || FollowTarget == null) return;

            Vector3 toTarget = FollowTarget.position - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            // Speed is fed to the animator in real m/s to match the blend
            // tree thresholds (0 / 2.0 / 5.335).
            float speedBlend = 0f;
            if (dist > followDistance)
            {
                Vector3 dir = toTarget.normalized;
                // Break into a run if we've fallen well behind, so the
                // companion can actually catch up.
                float speed = dist > followDistance * 3f ? moveSpeed * 2.4f : moveSpeed;
                _controller.SimpleMove(dir * speed);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
                speedBlend = speed;
            }

            if (animator != null)
            {
                _animSpeedBlend = Mathf.Lerp(_animSpeedBlend, speedBlend, 10f * Time.deltaTime);
                if (_animSpeedBlend < 0.01f) _animSpeedBlend = 0f;
                animator.SetFloat(speedParam, _animSpeedBlend);
            }
        }
    }
}
