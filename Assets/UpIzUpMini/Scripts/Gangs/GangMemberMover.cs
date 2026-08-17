using UnityEngine;
using UpIzUpMini.Character;

namespace UpIzUpMini.Gangs
{
    /// <summary>
    /// MINI-054. Per-member movement for the Dog Life rival gang. Each
    /// member wanders within its block using a CharacterController (so they
    /// collide with buildings), uses AntiStuckSteering so they do not grind
    /// into walls, and carries NpcCombatHealth so the player can fight them.
    /// The scene builder wires the visual char + Animator; this drives
    /// motion and reports back to the player's melee via the shared
    /// NpcCombatHealth component.
    /// </summary>
    public class GangMemberMover : MonoBehaviour
    {
        private CharacterController _controller;
        private AntiStuckSteering _antiStuck;
        private Vector3 _wanderTarget;
        private float _retargetAt;
        private Animator _animator;
        private Combat.NpcCombatHealth _health;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _antiStuck = GetComponent<AntiStuckSteering>();
            _animator = GetComponentInChildren<Animator>();
            _health = GetComponent<Combat.NpcCombatHealth>();
        }

        /// <summary>Drives a member's behaviour for one frame. Called by
        /// RivalGangController (kept out of Update so the controller owns
        /// the tick, and dt can be injected by tests later).</summary>
        public void Tick(Vector3 blockCenter, float blockRadius, float speed)
        {
            if (_health != null && _health.IsDown)
            {
                // Knocked out - do not steer while lying down.
                SetAnim(0f);
                return;
            }

            if (_controller == null) return;
            if (_controller.enabled == false) { SetAnim(0f); return; }

            // Pick a fresh wander target periodically or when still.
            if (Time.time >= _retargetAt ||
                Vector3.Distance(transform.position, _wanderTarget) < 0.8f)
            {
                _wanderTarget = blockCenter + RandomOnDisc(blockRadius * 0.7f);
                _retargetAt = Time.time + Random.Range(3f, 6f);
                if (_antiStuck != null) _antiStuck.Clear();
            }

            Vector3 dir = (_wanderTarget - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f) dir.Normalize();

            // Anti-stuck evasion: if blocked, take the evasion direction.
            if (_antiStuck != null)
            {
                _antiStuck.Tick(speed);
                if (_antiStuck.IsEvading) dir = _antiStuck.GetEvasionDirection(transform.forward);
            }

            // Face travel direction, then move.
            if (dir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(dir, Vector3.up), Time.deltaTime * 6f);
                _controller.SimpleMove(dir * speed);
                SetAnim(speed);
            }
            else SetAnim(0f);
        }

        private void SetAnim(float speed)
        {
            if (_animator != null && _animator.isActiveAndEnabled)
                _animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
        }

        private static Vector3 RandomOnDisc(float radius)
        {
            Vector2 p = Random.insideUnitCircle * radius;
            return new Vector3(p.x, 0f, p.y);
        }
    }
}
