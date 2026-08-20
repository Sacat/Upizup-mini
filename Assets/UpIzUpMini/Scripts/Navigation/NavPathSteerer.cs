using UnityEngine;
using UnityEngine.AI;

namespace UpIzUpMini.Navigation
{
    /// <summary>
    /// MINI-052: reusable path-follow + stuck-recovery helper for the
    /// project's existing CharacterController-driven NPCs (companion,
    /// villagers, police). Deliberately does NOT replace CharacterController
    /// with a NavMeshAgent - a dozen other systems (combat, save/load, the
    /// cell phone recall, farmhand assignment...) already reference these
    /// characters' CharacterController, and swapping the movement backbone
    /// project-wide is a bigger architectural change than "improve the
    /// steering" calls for. Instead this samples the NavMesh baked by
    /// Mini011PhaseBSetup (terrain + building colliders, via
    /// com.unity.ai.navigation - already an installed package, not a new
    /// dependency) with the static <see cref="NavMesh"/> API to compute a
    /// real path around houses, and hands back a steer direction the
    /// caller feeds into its own CharacterController.SimpleMove - the
    /// caller's speed/animation/heat/stamina logic is untouched.
    ///
    /// Stuck recovery follows the brief's exact order: recompute the path,
    /// then try a nearby valid path point, then rotate/recover in place,
    /// and only after repeated failure, relocate to a nearby valid NavMesh
    /// position. "Repeated failure" means the ladder has to fail at every
    /// earlier rung (each one resets the stuck timer without curing the
    /// stuck condition) before relocation fires - not a single bad frame.
    /// </summary>
    public class NavPathSteerer
    {
        private const float RepathInterval = 0.6f;
        private const float TargetMoveRepathThreshold = 1.5f;
        private const float ArriveCornerDistance = 0.5f;

        // Stuck-timer thresholds - each rung must fail for this long before
        // escalating to the next, so a brief snag doesn't trigger a relocate.
        private const float RecomputeAfterSeconds = 0.6f;
        private const float NearbyPointAfterSeconds = 1.5f;
        private const float RotateRecoverAfterSeconds = 3.0f;
        private const float RelocateAfterSeconds = 5.0f;
        private const float RotateRecoverDuration = 0.5f;

        // Not constructed inline: NavPathSteerer instances are typically
        // created as field initializers on MonoBehaviours, which runs
        // during the MonoBehaviour's own construction - Unity forbids
        // constructing a NavMeshPath there ("InitializeNavMeshPath is not
        // allowed to be called from a MonoBehaviour constructor"). Built
        // lazily on first real use (Awake/Update has definitely run by then).
        private NavMeshPath _path;
        private NavMeshPath Path => _path ??= new NavMeshPath();
        private Vector3 _target;
        private Vector3? _detour;
        private int _cornerIndex;
        private float _repathCooldown;
        private float _stuckSeconds;
        private float _rotateRecoverSeconds;
        private Vector3 _lastPos;
        private bool _havePos;
        private bool _haveTarget;

        /// <summary>Set by Relocate() when every earlier recovery rung has
        /// failed; the caller must actually move its transform (this class
        /// doesn't own it) and then call <see cref="ConsumeRelocation"/>.</summary>
        public Vector3? PendingRelocation { get; private set; }

        /// <summary>
        /// Call once per frame instead of "(target - currentPos).normalized".
        /// Returns a flat, normalized steer direction already routed around
        /// baked-in obstacles, or null once arrived (no active detour) or if
        /// no path/NavMesh coverage is available at all (caller should fall
        /// back to a straight line in that case, same as before this task).
        /// </summary>
        public Vector3? Steer(Vector3 currentPos, Vector3 target, float desiredSpeed, float arriveDistance, out bool arrived)
        {
            arrived = false;

            if (!_havePos) { _lastPos = currentPos; _havePos = true; }

            if (!_haveTarget || (target - _target).sqrMagnitude > TargetMoveRepathThreshold * TargetMoveRepathThreshold)
            {
                _target = target;
                _haveTarget = true;
                Repath(currentPos);
            }
            else
            {
                _target = target;
            }

            _repathCooldown -= Time.deltaTime;
            if (_repathCooldown <= 0f)
            {
                _repathCooldown = RepathInterval;
                if (Path.status != NavMeshPathStatus.PathComplete || Path.corners.Length == 0)
                    Repath(currentPos);
            }

            UpdateStuckTimer(currentPos, desiredSpeed);

            if (_rotateRecoverSeconds > 0f)
            {
                _rotateRecoverSeconds -= Time.deltaTime;
                Vector3 fallback = FlatDir(currentPos, _detour ?? _target);
                if (fallback == Vector3.zero) fallback = Vector3.forward;
                return Quaternion.Euler(0f, 70f, 0f) * fallback;
            }

            RunRecoveryLadder(currentPos);

            if (_detour.HasValue)
            {
                if (Vector3.Distance(FlatPos(currentPos), FlatPos(_detour.Value)) <= arriveDistance)
                {
                    _detour = null;
                }
                else
                {
                    return FlatDir(currentPos, _detour.Value);
                }
            }

            float distToFinal = Vector3.Distance(FlatPos(currentPos), FlatPos(_target));
            if (distToFinal <= arriveDistance)
            {
                arrived = true;
                return null;
            }

            Vector3 corner = NextCorner(currentPos);
            Vector3 dir = FlatDir(currentPos, corner);
            return dir == Vector3.zero ? (Vector3?)null : dir;
        }

        /// <summary>Reads and clears a pending relocation. Caller must move
        /// its own transform (and briefly disable its CharacterController,
        /// which does not allow direct position writes while enabled).</summary>
        public Vector3? ConsumeRelocation()
        {
            var r = PendingRelocation;
            PendingRelocation = null;
            return r;
        }

        private void UpdateStuckTimer(Vector3 currentPos, float desiredSpeed)
        {
            float moved = Vector3.Distance(currentPos, _lastPos);
            _lastPos = currentPos;

            // "Low displacement while desired movement remains high" - only
            // counts as stuck while the caller is actually trying to move.
            bool tryingToMove = desiredSpeed > 0.05f;
            float expectedMove = desiredSpeed * Time.deltaTime;
            if (tryingToMove && moved < expectedMove * 0.15f)
                _stuckSeconds += Time.deltaTime;
            else
                _stuckSeconds = 0f;
        }

        private void RunRecoveryLadder(Vector3 currentPos)
        {
            if (_stuckSeconds >= RelocateAfterSeconds)
            {
                Relocate(currentPos);
                _stuckSeconds = 0f;
            }
            else if (_stuckSeconds >= RotateRecoverAfterSeconds)
            {
                _rotateRecoverSeconds = RotateRecoverDuration;
                _stuckSeconds = 0f;
            }
            else if (_stuckSeconds >= NearbyPointAfterSeconds && !_detour.HasValue)
            {
                TryNearbyDetour(currentPos);
                _stuckSeconds = 0f;
            }
            else if (_stuckSeconds >= RecomputeAfterSeconds)
            {
                Repath(currentPos);
                _stuckSeconds = 0f;
            }
        }

        private void Repath(Vector3 from)
        {
            _cornerIndex = 0;
            if (!NavMesh.SamplePosition(from, out var fromHit, 3f, NavMesh.AllAreas)) return;
            if (!NavMesh.SamplePosition(_target, out var toHit, 3f, NavMesh.AllAreas)) return;
            NavMesh.CalculatePath(fromHit.position, toHit.position, NavMesh.AllAreas, Path);
        }

        private Vector3 NextCorner(Vector3 currentPos)
        {
            var corners = Path.corners;
            if (corners == null || corners.Length < 2) return _target;

            if (_cornerIndex >= corners.Length) _cornerIndex = corners.Length - 1;
            while (_cornerIndex < corners.Length - 1 &&
                   Vector3.Distance(FlatPos(currentPos), FlatPos(corners[_cornerIndex])) < ArriveCornerDistance)
            {
                _cornerIndex++;
            }
            return corners[_cornerIndex];
        }

        /// <summary>Rung 2: a nearby valid NavMesh point, tried in a ring
        /// around the current position - not the final destination, just
        /// somewhere reachable to break out of whatever local snag this is.</summary>
        private void TryNearbyDetour(Vector3 currentPos)
        {
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f;
                Vector3 probe = currentPos + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 2.2f;
                if (NavMesh.SamplePosition(probe, out var hit, 1.5f, NavMesh.AllAreas))
                {
                    _detour = hit.position;
                    return;
                }
            }
        }

        /// <summary>Rung 4, last resort: teleport to a nearby valid NavMesh
        /// position after rungs 1-3 all failed to unstick this character.</summary>
        private void Relocate(Vector3 currentPos)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                Vector3 probe = currentPos + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 4f;
                if (NavMesh.SamplePosition(probe, out var hit, 3f, NavMesh.AllAreas))
                {
                    _detour = null;
                    Path.ClearCorners();
                    PendingRelocation = hit.position;
                    return;
                }
            }
        }

        private static Vector3 FlatPos(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private static Vector3 FlatDir(Vector3 from, Vector3 to)
        {
            Vector3 d = to - from;
            d.y = 0f;
            return d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.zero;
        }
    }
}
