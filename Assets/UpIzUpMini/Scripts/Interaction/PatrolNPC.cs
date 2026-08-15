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
        [SerializeField] private float walkSpeed = 1.5f;
        [SerializeField] private float alertSpeed = 4.2f;
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

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
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

                        transform.position += dir * speed * Time.deltaTime;
                        transform.rotation = Quaternion.Slerp(
                            transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);

                        desiredBlend = alert ? 2f : 1f;
                    }
                }
            }

            if (animator != null)
            {
                _animBlend = Mathf.MoveTowards(_animBlend, desiredBlend, 8f * Time.deltaTime);
                animator.SetFloat(speedParam, _animBlend);
            }
        }
    }
}
