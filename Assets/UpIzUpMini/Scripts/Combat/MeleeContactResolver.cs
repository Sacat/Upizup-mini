using System;
using System.Collections.Generic;
using UnityEngine;

namespace UpIzUpMini.Combat
{
    /// <summary>Small data packet describing one melee swing's timing and
    /// forward contact volume. Components can tune damage separately while
    /// sharing contact truth.</summary>
    [Serializable]
    public struct MeleeAttackProfile
    {
        public float windupSeconds;
        public float activeSeconds;
        public float recoverySeconds;
        public float forwardOffset;
        public float reach;
        public float radius;
        public float arcDegrees;

        public MeleeAttackProfile(
            float windupSeconds, float activeSeconds, float recoverySeconds,
            float forwardOffset, float reach, float radius, float arcDegrees)
        {
            this.windupSeconds = Mathf.Max(0f, windupSeconds);
            this.activeSeconds = Mathf.Max(0.01f, activeSeconds);
            this.recoverySeconds = Mathf.Max(0f, recoverySeconds);
            this.forwardOffset = Mathf.Max(0f, forwardOffset);
            this.reach = Mathf.Max(this.forwardOffset + 0.05f, reach);
            this.radius = Mathf.Max(0.05f, radius);
            this.arcDegrees = Mathf.Clamp(arcDegrees, 1f, 180f);
        }

        public float TotalSeconds => windupSeconds + activeSeconds + recoverySeconds;
    }

    /// <summary>Deterministic windup/active/recovery state. Damage resolves
    /// once when the active window begins, never at button/proximity start.</summary>
    public sealed class MeleeSwingTimeline
    {
        private float _elapsed = -1f;
        private bool _resolved;
        private MeleeAttackProfile _profile;

        public bool IsRunning => _elapsed >= 0f;
        public float Elapsed => Mathf.Max(0f, _elapsed);

        public bool Begin(MeleeAttackProfile profile)
        {
            if (IsRunning) return false;
            _profile = profile;
            _elapsed = 0f;
            _resolved = false;
            return true;
        }

        /// <summary>Returns true exactly once when the swing enters its active
        /// phase. A large test/headless delta still resolves once, then the
        /// timeline completes safely.</summary>
        public bool Advance(float deltaSeconds)
        {
            if (!IsRunning) return false;
            _elapsed += Mathf.Max(0f, deltaSeconds);

            bool resolveNow = !_resolved && _elapsed >= _profile.windupSeconds;
            if (resolveNow) _resolved = true;

            if (_elapsed >= _profile.TotalSeconds) _elapsed = -1f;
            return resolveNow;
        }

        public void Cancel()
        {
            _elapsed = -1f;
            _resolved = false;
        }
    }

    /// <summary>
    /// Shared, allocation-free forward capsule query for player, companion,
    /// and faction melee. The capsule approximates the path of a fist from
    /// near the chest out toward full extension, then an angle and visibility
    /// test reject side/behind/wall contacts.
    /// </summary>
    public static class MeleeContactResolver
    {
        private static readonly RaycastHit[] SightHits = new RaycastHit[24];

        public static bool TryFindNearest<T>(
            Transform attacker, MeleeAttackProfile profile, IReadOnlyList<T> candidates,
            Predicate<T> isEligible, out T target)
            where T : Component
        {
            target = null;
            if (attacker == null || candidates == null) return false;

            Vector3 forward = Vector3.ProjectOnPlane(attacker.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            Vector3 chest = attacker.position + Vector3.up * 1.05f;

            float nearest = float.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                T candidate = candidates[i];
                if (candidate == null || candidate.transform == attacker || candidate.transform.IsChildOf(attacker)) continue;
                if (isEligible != null && !isEligible(candidate)) continue;
                if (!IsInsideForwardContact(attacker, candidate.transform, profile)) continue;
                if (!HasLineOfSight(attacker, candidate.transform, chest)) continue;

                float distance = Vector3.Distance(attacker.position, candidate.transform.position);
                if (distance >= nearest) continue;

                nearest = distance;
                target = candidate;
            }

            return target != null;
        }

        /// <summary>Pure geometric half of contact truth, exposed so it can
        /// be validated deterministically even where Edit Mode's PhysX
        /// broadphase does not register freshly-created colliders.</summary>
        public static bool IsInsideForwardContact(
            Transform attacker, Transform target, MeleeAttackProfile profile)
        {
            if (attacker == null || target == null) return false;
            Vector3 forward = Vector3.ProjectOnPlane(attacker.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;

            Vector3 flatToTarget = target.position - attacker.position;
            flatToTarget.y = 0f;
            float flatDistance = flatToTarget.magnitude;
            if (flatDistance < 0.001f) return false;
            if (Vector3.Angle(forward, flatToTarget / flatDistance) > profile.arcDegrees * 0.5f) return false;

            Vector3 chest = attacker.position + Vector3.up * 1.05f;
            Vector3 start = chest + forward * profile.forwardOffset;
            Vector3 end = chest + forward * profile.reach;
            Vector3 bodyPoint = target.position + Vector3.up;

            // Contact radius plus a modest body radius approximates a fist
            // meeting a character capsule, without relying on target collider
            // topology or an omnidirectional overlap sphere.
            return DistancePointToSegment(bodyPoint, start, end) <= profile.radius + 0.35f;
        }

        /// <summary>Complete single-target contact check used when an AI has
        /// already selected its intended victim.</summary>
        public static bool CanHitTarget(
            Transform attacker, Transform target, MeleeAttackProfile profile)
        {
            if (!IsInsideForwardContact(attacker, target, profile)) return false;
            return HasLineOfSight(attacker, target, attacker.position + Vector3.up * 1.05f);
        }

        private static float DistancePointToSegment(Vector3 point, Vector3 start, Vector3 end)
        {
            Vector3 segment = end - start;
            float lengthSq = segment.sqrMagnitude;
            if (lengthSq < 0.0001f) return Vector3.Distance(point, start);
            float t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / lengthSq);
            return Vector3.Distance(point, start + segment * t);
        }

        private static bool HasLineOfSight(Transform attacker, Transform target, Vector3 from)
        {
            Vector3 to = target.position + Vector3.up;
            Vector3 ray = to - from;
            float distance = ray.magnitude;
            if (distance < 0.01f) return true;

            int count = Physics.RaycastNonAlloc(
                from, ray / distance, SightHits, distance + 0.15f, ~0, QueryTriggerInteraction.Ignore);
            float targetDistance = float.MaxValue;
            float blockerDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = SightHits[i];
                SightHits[i] = default;
                Transform seen = hit.transform;
                if (seen == null) continue;
                if (seen == attacker || seen.IsChildOf(attacker)) continue;

                bool isTarget = seen == target || seen.IsChildOf(target) || target.IsChildOf(seen);
                if (isTarget) targetDistance = Mathf.Min(targetDistance, hit.distance);
                else blockerDistance = Mathf.Min(blockerDistance, hit.distance);
            }

            // The overlap query already proved the target owns a collider in
            // the contact volume. If this ray happens to pass through a gap in
            // that collider, no blocker still means clear sight.
            return blockerDistance == float.MaxValue || targetDistance <= blockerDistance;
        }
    }
}
