using UnityEngine;

namespace UpIzUpMini.Character
{
    /// <summary>
    /// Follow behaviour for the currently-inactive boy (per Docs/STORY.md:
    /// "the inactive boy must remain a real world character... he follows
    /// the active boy at a sensible distance"). Only active while this
    /// character is not the player-controlled one.
    ///
    /// Reverted from MINI-052's NavPathSteerer-based steering back to
    /// simple direct steering, per the user's explicit report: "characters
    /// not following me properly, they are glitching especially by a hill
    /// i prefer the old follow system." The NavMesh-based path/stuck-
    /// recovery logic was producing worse real-world behaviour near sloped
    /// terrain than the straight-line approach it replaced, not better -
    /// simplicity wins here. Deliberately simple - direct steering toward
    /// the target with CharacterController.SimpleMove, not pathfinding.
    /// Good enough for open ground; may cut corners around obstacles.
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
        private float _blockedFor;
        private float _sideSign;

        public Transform FollowTarget { get; set; }
        public bool FollowingEnabled { get; set; } = true;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _sideSign = (GetInstanceID() & 1) == 0 ? 1f : -1f;
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
            bool moving = false;
            if (dist > followDistance)
            {
                Vector3 dir = toTarget.normalized;
                if (!LocalSteeringSafety.TryDirection(transform, _controller, dir, FollowTarget, _sideSign, out dir))
                {
                    _blockedFor += Time.deltaTime;
                    if (_blockedFor > 1.2f) _sideSign *= -1f;
                    Animate(speedBlend, moving);
                    return;
                }
                _blockedFor = 0f;
                // Break into a run if we've fallen well behind, so the
                // companion can actually catch up.
                float speed = dist > followDistance * 3f ? 5.335f : moveSpeed;
                _controller.SimpleMove(dir * speed);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
                speedBlend = speed;
                moving = true;
            }

            Animate(speedBlend, moving);
        }

        private void Animate(float speedBlend, bool moving)
        {
            if (animator != null)
            {
                _animSpeedBlend = Mathf.Lerp(_animSpeedBlend, speedBlend, 10f * Time.deltaTime);
                if (_animSpeedBlend < 0.01f) _animSpeedBlend = 0f;
                animator.SetFloat(speedParam, _animSpeedBlend);
                // The authored controller needs these too, or the clip
                // plays at a fixed rate and the feet skate.
                animator.SetFloat("MotionSpeed", moving ? 1f : 0f);
                animator.SetBool("Grounded", true);
            }
        }
    }
}
