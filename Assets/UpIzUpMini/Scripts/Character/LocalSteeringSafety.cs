using UnityEngine;

namespace UpIzUpMini.Character
{
    /// <summary>Cheap local obstacle and ledge check for CharacterController
    /// followers. It does not replace the terrain-friendly direct follower;
    /// it stops repeated wall-pushing and tries one short side-step only when
    /// the straight route is blocked.</summary>
    public static class LocalSteeringSafety
    {
        private static readonly Collider[] NearbyCharacters = new Collider[16];
        private static readonly RaycastHit[] ForwardHits = new RaycastHit[16];

        /// <summary>Stable point behind and slightly beside a moving leader.
        /// Followers target different slots rather than the leader's exact
        /// position, preventing the constant shoulder-to-shoulder bounce.</summary>
        public static Vector3 TrailingSlot(
            Transform leader, float trailDistance, float sideDistance, float sideSign)
        {
            if (leader == null) return Vector3.zero;
            Vector3 forward = Vector3.ProjectOnPlane(leader.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(leader.right, Vector3.up).normalized;
            return leader.position - forward * Mathf.Max(0f, trailDistance)
                   + right * sideDistance * Mathf.Sign(sideSign == 0f ? 1f : sideSign);
        }

        /// <summary>Start and stop distances are deliberately different, so
        /// a follower does not alternate between one moving frame and one idle
        /// frame while hovering on the edge of its slot.</summary>
        public static bool ShouldMoveToSlot(float distance, bool wasMoving, float stopDistance, float resumeDistance)
        {
            return wasMoving ? distance > stopDistance : distance > Mathf.Max(stopDistance, resumeDistance);
        }

        public static bool TryDirection(
            Transform self, CharacterController controller, Vector3 desired,
            Transform target, float sideSign, out Vector3 safeDirection)
        {
            desired.y = 0f;
            desired = ApplyCharacterSeparation(self, desired, target);
            safeDirection = desired.sqrMagnitude > 0.001f ? desired.normalized : Vector3.zero;
            if (safeDirection == Vector3.zero) return false;

            if (DirectionIsSafe(self, controller, safeDirection, target)) return true;

            Vector3 first = Quaternion.Euler(0f, 58f * sideSign, 0f) * safeDirection;
            if (DirectionIsSafe(self, controller, first, target))
            {
                safeDirection = first;
                return true;
            }

            Vector3 second = Quaternion.Euler(0f, -58f * sideSign, 0f) * safeDirection;
            if (DirectionIsSafe(self, controller, second, target))
            {
                safeDirection = second;
                return true;
            }

            safeDirection = Vector3.zero;
            return false;
        }

        /// <summary>
        /// Cheap, bounded crowd separation for the small number of active
        /// mobile NPCs. Only CharacterControllers contribute, so scenery does
        /// not distort the requested path; walls and ledges remain the job of
        /// the safety probes below.
        /// </summary>
        public static Vector3 ApplyCharacterSeparation(Transform self, Vector3 desired, Transform target)
        {
            if (self == null) return desired;

            const float separationRadius = 1.35f;
            const float separationWeight = 0.9f;
            int count = Physics.OverlapSphereNonAlloc(
                self.position + Vector3.up * 0.8f, separationRadius, NearbyCharacters,
                ~0, QueryTriggerInteraction.Ignore);

            Vector3 separation = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                Collider nearby = NearbyCharacters[i];
                NearbyCharacters[i] = null;
                if (nearby == null) continue;

                CharacterController other = nearby.GetComponentInParent<CharacterController>();
                if (other == null) continue;
                Transform otherTransform = other.transform;
                if (otherTransform == self || otherTransform.IsChildOf(self)) continue;
                if (target != null && (otherTransform == target || otherTransform.IsChildOf(target))) continue;

                Vector3 away = self.position - otherTransform.position;
                away.y = 0f;
                float distance = away.magnitude;
                if (distance < 0.001f || distance >= separationRadius) continue;
                separation += away.normalized * (1f - distance / separationRadius);
            }

            if (separation.sqrMagnitude < 0.0001f) return desired;
            Vector3 blended = desired.normalized + Vector3.ClampMagnitude(separation, 1f) * separationWeight;
            return blended.sqrMagnitude > 0.0001f ? blended.normalized : desired;
        }

        private static bool DirectionIsSafe(
            Transform self, CharacterController controller, Vector3 direction, Transform target)
        {
            float radius = controller != null ? Mathf.Clamp(controller.radius * 0.72f, 0.18f, 0.32f) : 0.25f;
            float ahead = controller != null ? Mathf.Max(0.75f, controller.radius * 2.2f) : 0.8f;
            Vector3 chest = self.position + Vector3.up * 0.9f + direction * (radius + 0.08f);

            int hitCount = Physics.SphereCastNonAlloc(
                chest, radius, direction, ForwardHits, ahead, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = ForwardHits[i];
                ForwardHits[i] = default;
                Transform h = hit.transform;
                if (h == null) continue;
                bool isSelf = h == self || h.IsChildOf(self);
                bool isTarget = target != null && (h == target || h.IsChildOf(target));
                if (!isSelf && !isTarget) return false;
            }

            // Do not step into the sea/off a cliff. A shallow kerb or slope is
            // still found by this downward probe; a real ledge is not.
            Vector3 groundProbe = self.position + direction * (ahead + 0.35f) + Vector3.up * 1.2f;
            return Physics.Raycast(groundProbe, Vector3.down, 2.4f, ~0, QueryTriggerInteraction.Ignore);
        }
    }
}
