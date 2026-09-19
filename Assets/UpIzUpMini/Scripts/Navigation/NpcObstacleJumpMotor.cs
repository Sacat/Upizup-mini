using UnityEngine;

namespace UpIzUpMini.Navigation
{
    public sealed class NpcObstacleJumpMotor
    {
        private float _verticalVelocity;
        private float _cooldown;
        public bool IsAirborne { get; private set; }

        public Vector3 Step(Transform actor, CharacterController controller, Vector3 direction, bool mayJump, float deltaTime)
            => Step(actor, controller, direction, mayJump, false, deltaTime);

        public Vector3 Step(Transform actor, CharacterController controller, Vector3 direction, bool mayJump, bool stuck, float deltaTime)
        {
            if (controller == null || !controller.enabled) return Vector3.zero;
            _cooldown = Mathf.Max(0f, _cooldown - deltaTime);
            bool grounded = controller.isGrounded;
            if (grounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
            if (mayJump && grounded && _cooldown <= 0f && direction.sqrMagnitude > 0.1f
                && (HasLowObstacle(actor, controller, direction.normalized)
                    || stuck && HasJumpClearance(actor, controller, direction.normalized)))
            {
                _verticalVelocity = Mathf.Sqrt(1.15f * 2f * 18f);
                _cooldown = 0.8f;
                grounded = false;
            }
            _verticalVelocity -= 18f * deltaTime;
            IsAirborne = !grounded || _verticalVelocity > 0f;
            return Vector3.up * (_verticalVelocity * deltaTime);
        }

        private static bool HasJumpClearance(Transform actor, CharacterController controller, Vector3 direction)
        {
            float distance = controller.radius + 0.75f;
            Vector3 chest = actor.position + Vector3.up * 1.45f + direction * (controller.radius + 0.05f);
            bool bodyClear = !Physics.Raycast(chest, direction, distance, ~0, QueryTriggerInteraction.Ignore);
            Vector3 landingProbe = actor.position + direction * (controller.radius + 0.9f) + Vector3.up * 1.5f;
            bool hasLanding = Physics.Raycast(landingProbe, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore);
            return bodyClear && hasLanding;
        }

        private static bool HasLowObstacle(Transform actor, CharacterController controller, Vector3 direction)
        {
            float distance = controller.radius + 0.75f;
            Vector3 origin = actor.position + Vector3.up * 0.3f + direction * (controller.radius + 0.05f);
            bool lowBlocked = Physics.Raycast(origin, direction, distance, ~0, QueryTriggerInteraction.Ignore);
            bool bodyClear = !Physics.Raycast(origin + Vector3.up * 1.15f, direction, distance, ~0, QueryTriggerInteraction.Ignore);
            return lowBlocked && bodyClear;
        }
    }
}
