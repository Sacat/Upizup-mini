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
        [SerializeField] private float moveSpeed = 3.4f;
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

            float speedBlend = 0f;
            if (dist > followDistance)
            {
                Vector3 dir = toTarget.normalized;
                _controller.SimpleMove(dir * moveSpeed);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
                speedBlend = 1f;
            }

            if (animator != null)
            {
                _animSpeedBlend = Mathf.MoveTowards(_animSpeedBlend, speedBlend, 12f * Time.deltaTime);
                animator.SetFloat(speedParam, _animSpeedBlend);
            }
        }
    }
}
