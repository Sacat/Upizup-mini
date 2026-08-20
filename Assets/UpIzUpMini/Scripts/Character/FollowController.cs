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
        [SerializeField] private float sideOffset = 1.15f;
        [SerializeField] private float slotStopDistance = 0.65f;
        [SerializeField] private float slotResumeDistance = 1.25f;
        [SerializeField] private float blockedRetrySeconds = 0.3f;
        [SerializeField] private Animator animator;
        [SerializeField] private string speedParam = "Speed";

        private CharacterController _controller;
        private float _animSpeedBlend;
        private float _blockedFor;
        private float _sideSign;
        private float _retryAt;
        private bool _movingToSlot;

        public Transform FollowTarget { get; set; }
        public bool FollowingEnabled { get; set; } = true;
        public bool IsWaitingForPath { get; private set; }
        public Vector3 CurrentFormationTarget { get; private set; }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _sideSign = (GetInstanceID() & 1) == 0 ? 1f : -1f;
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            if (!FollowingEnabled || FollowTarget == null) return;

            CurrentFormationTarget = LocalSteeringSafety.TrailingSlot(
                FollowTarget, followDistance, sideOffset, _sideSign);
            Vector3 toTarget = CurrentFormationTarget - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;
            _movingToSlot = LocalSteeringSafety.ShouldMoveToSlot(
                dist, _movingToSlot, slotStopDistance, slotResumeDistance);

            // Speed is fed to the animator in real m/s to match the blend
            // tree thresholds (0 / 2.0 / 5.335).
            float speedBlend = 0f;
            bool moving = false;
            if (_movingToSlot)
            {
                if (Time.time < _retryAt)
                {
                    IsWaitingForPath = true;
                    Animate(speedBlend, moving);
                    return;
                }

                Vector3 dir = toTarget.normalized;
                if (!LocalSteeringSafety.TryDirection(transform, _controller, dir, FollowTarget, _sideSign, out dir))
                {
                    _blockedFor += Time.deltaTime;
                    _retryAt = Time.time + blockedRetrySeconds;
                    IsWaitingForPath = true;
                    if (_blockedFor > 1.2f)
                    {
                        _sideSign *= -1f;
                        _blockedFor = 0f;
                    }
                    Animate(speedBlend, moving);
                    return;
                }
                _blockedFor = 0f;
                IsWaitingForPath = false;
                // Break into a run if we've fallen well behind, so the
                // companion can actually catch up.
                float speed = dist > followDistance * 2.5f ? 5.335f : moveSpeed;
                _controller.SimpleMove(dir * speed);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
                speedBlend = speed;
                moving = true;
            }
            else
            {
                IsWaitingForPath = false;
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
