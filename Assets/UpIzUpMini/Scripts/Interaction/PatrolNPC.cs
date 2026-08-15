using UnityEngine;
using UpIzUpMini.Economy;

namespace UpIzUpMini.Interaction
{
    /// <summary>
    /// Walks an NPC back and forth along a set of waypoints, driving the
    /// same Idle/Walk/Run animator blend the player characters use so the
    /// NPC doesn't stand in a T-pose. Used for the police officer and for
    /// ambient residents.
    ///
    /// Police behaviour scales with heat (per Docs/STORY.md: "a police
    /// officer who previously spoke casually to them now reacts with
    /// suspicion"): above the alert threshold the officer patrols faster
    /// and switches to the run animation.
    /// </summary>
    public class PatrolNPC : MonoBehaviour
    {
        [SerializeField] private Vector3[] waypoints;
        // Match the locomotion clips' tuned speeds so NPC feet don't skate
        // (StarterAssets MoveSpeed 2.0 / SprintSpeed 5.335).
        [SerializeField] private float walkSpeed = 2.0f;
        [SerializeField] private float alertSpeed = 5.335f;
        [SerializeField] private float turnSpeed = 6f;
        [SerializeField] private float arriveDistance = 0.6f;
        [SerializeField] private Animator animator;
        [SerializeField] private string speedParam = "Speed";
        [SerializeField] private bool reactsToHeat;
        [SerializeField] private float heatAlertThreshold = 40f;
        [SerializeField] private float pauseAtWaypointSeconds = 1.5f;

        private int _target;
        private float _animBlend;
        private float _pauseTimer;

        public bool IsAlert => reactsToHeat
            && EconomyManager.Instance != null
            && EconomyManager.Instance.Heat >= heatAlertThreshold;

        private CharacterController _controller;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();

            // Added by the scene builder so patrols are blocked by
            // building colliders instead of walking through walls.
            _controller = GetComponent<CharacterController>();
        }

        public void SetWaypoints(Vector3[] points)
        {
            waypoints = points;
            _target = 0;
        }

        private void Update()
        {
            float desiredBlend = 0f;

            if (waypoints != null && waypoints.Length >= 2)
            {
                if (_pauseTimer > 0f)
                {
                    _pauseTimer -= Time.deltaTime;
                }
                else
                {
                    Vector3 targetPos = waypoints[_target];
                    Vector3 toTarget = targetPos - transform.position;
                    toTarget.y = 0f;

                    if (toTarget.magnitude <= arriveDistance)
                    {
                        _target = (_target + 1) % waypoints.Length;
                        // Pause briefly at each end so the officer reads as
                        // patrolling rather than pacing frantically.
                        _pauseTimer = IsAlert ? 0f : pauseAtWaypointSeconds;
                    }
                    else
                    {
                        bool alert = IsAlert;
                        float speed = alert ? alertSpeed : walkSpeed;
                        Vector3 dir = toTarget.normalized;

                        // Move through a CharacterController so patrols are
                        // blocked by building colliders instead of walking
                        // straight through walls.
                        if (_controller != null && _controller.enabled)
                        {
                            _controller.SimpleMove(dir * speed);
                        }
                        else
                        {
                            transform.position += dir * speed * Time.deltaTime;
                        }

                        transform.rotation = Quaternion.Slerp(
                            transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);

                        // Real m/s, matching the blend tree thresholds.
                        desiredBlend = speed;
                    }
                }
            }

            if (animator != null)
            {
                _animBlend = Mathf.Lerp(_animBlend, desiredBlend, 10f * Time.deltaTime);
                if (_animBlend < 0.01f) _animBlend = 0f;
                animator.SetFloat(speedParam, _animBlend);
                // The authored StarterAssets controller scales playback by
                // MotionSpeed and gates on Grounded; without these the clip
                // plays at a fixed rate and the feet skate.
                animator.SetFloat("MotionSpeed", desiredBlend > 0.01f ? 1f : 0f);
                animator.SetBool("Grounded", true);
            }
        }
    }
}
