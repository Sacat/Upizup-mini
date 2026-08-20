using UnityEngine;

namespace UpIzUpMini.Character
{
    /// <summary>Cheap local obstacle and ledge check for CharacterController
    /// followers. It does not replace the terrain-friendly direct follower;
    /// it stops repeated wall-pushing and tries one short side-step only when
    /// the straight route is blocked.</summary>
    public static class LocalSteeringSafety
    {
        public static bool TryDirection(
            Transform self, CharacterController controller, Vector3 desired,
            Transform target, float sideSign, out Vector3 safeDirection)
        {
            desired.y = 0f;
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

        private static bool DirectionIsSafe(
            Transform self, CharacterController controller, Vector3 direction, Transform target)
        {
            float radius = controller != null ? Mathf.Clamp(controller.radius * 0.72f, 0.18f, 0.32f) : 0.25f;
            float ahead = controller != null ? Mathf.Max(0.75f, controller.radius * 2.2f) : 0.8f;
            Vector3 chest = self.position + Vector3.up * 0.9f + direction * (radius + 0.08f);

            if (Physics.SphereCast(chest, radius, direction, out RaycastHit hit, ahead,
                    ~0, QueryTriggerInteraction.Ignore))
            {
                Transform h = hit.transform;
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
