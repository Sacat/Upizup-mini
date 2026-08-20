using UnityEngine;
using UpIzUpMini.Economy;
using UpIzUpMini.Navigation;
using UpIzUpMini.Character;

namespace UpIzUpMini.Interaction
{
    /// <summary>
    /// Walks an NPC back and forth along a set of waypoints, driving the
    /// same Idle/Walk/Run animator blend the player characters use so the
    /// NPC doesn't stand in a T-pose. Used for ambient residents (the
    /// dedicated police patrol/chase logic lives in PoliceOfficer).
    ///
    /// Police behaviour scales with heat (per Docs/STORY.md: "a police
    /// officer who previously spoke casually to them now reacts with
    /// suspicion"): above the alert threshold the officer patrols faster
    /// and switches to the run animation.
    ///
    /// MINI-052: waypoint steering routes through NavPathSteerer (real
    /// NavMesh path around houses + stuck recovery) instead of a straight
    /// line - previously an NPC could jitter/snag against a wall corner
    /// with no recovery at all.
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
        private float _blockedTimer;
        private float _sideSign;
        private readonly NavPathSteerer _steerer = new NavPathSteerer();

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
            _sideSign = (GetInstanceID() & 1) == 0 ? 1f : -1f;
            // Stagger identical patrols so a row of NPCs does not begin
            // walking and stopping in mechanical lockstep.
            _pauseTimer = Mathf.Abs(GetInstanceID() % 100) / 100f * pauseAtWaypointSeconds;
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
                    Vector3? relocate = _steerer.ConsumeRelocation();
                    if (relocate.HasValue) Relocate(relocate.Value);

                    Vector3 targetPos = waypoints[_target];
                    bool alert = IsAlert;
                    float speed = alert ? alertSpeed : walkSpeed;

                    Vector3? dir = _steerer.Steer(transform.position, targetPos, speed, arriveDistance, out bool arrived);

                    if (arrived)
                    {
                        _target = (_target + 1) % waypoints.Length;
                        // Pause briefly at each end so the officer reads as
                        // patrolling rather than pacing frantically.
                        _pauseTimer = alert ? 0f : pauseAtWaypointSeconds;
                    }
                    else if (dir.HasValue)
                    {
                        Vector3 safeDirection = dir.Value;
                        if (_controller != null && !LocalSteeringSafety.TryDirection(
                                transform, _controller, safeDirection, null, _sideSign, out safeDirection))
                        {
                            _blockedTimer += Time.deltaTime;
                            if (_blockedTimer >= 0.9f)
                            {
                                _sideSign *= -1f;
                                _blockedTimer = 0f;
                            }
                            ApplyAnimation(0f);
                            return;
                        }
                        _blockedTimer = 0f;

                        // Move through a CharacterController so patrols are
                        // blocked by building colliders instead of walking
                        // straight through walls.
                        if (_controller != null && _controller.enabled)
                        {
                            _controller.SimpleMove(safeDirection * speed);
                        }
                        else
                        {
                            transform.position += safeDirection * speed * Time.deltaTime;
                        }

                        transform.rotation = Quaternion.Slerp(
                            transform.rotation, Quaternion.LookRotation(safeDirection), turnSpeed * Time.deltaTime);

                        // Real m/s, matching the blend tree thresholds.
                        desiredBlend = speed;
                    }
                }
            }

            ApplyAnimation(desiredBlend);
        }

        private void ApplyAnimation(float desiredBlend)
        {
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

        /// <summary>CharacterController rejects direct transform writes
        /// while enabled, so it's briefly disabled to relocate.</summary>
        private void Relocate(Vector3 position)
        {
            if (_controller != null)
            {
                _controller.enabled = false;
                transform.position = position;
                _controller.enabled = true;
            }
            else
            {
                transform.position = position;
            }
        }
    }
}
