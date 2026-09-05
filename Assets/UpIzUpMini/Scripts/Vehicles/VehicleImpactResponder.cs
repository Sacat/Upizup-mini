using System.Collections.Generic;
using UnityEngine;
using UpIzUpMini.Combat;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.Vehicles
{
    /// <summary>One collision adapter shared by cars, TMAX and SuperMoto.
    /// It turns real Rigidbody collision speed into NPC health damage and
    /// momentum, while leaving each vehicle's approved handling untouched.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehicleImpactResponder : MonoBehaviour
    {
        [SerializeField] float minimumImpactSpeed = 3.5f;
        [SerializeField] float fullDamageSpeed = 20f;
        [SerializeField] float minimumDamage = 10f;
        [SerializeField] float maximumDamage = 82f;
        [SerializeField] float repeatHitCooldown = .75f;

        readonly Dictionary<int, float> nextAllowedHit = new Dictionary<int, float>();
        Rigidbody body;

        public float MinimumImpactSpeed => minimumImpactSpeed;
        public float MaximumDamage => maximumDamage;

        void Awake() => body = GetComponent<Rigidbody>();

        public static VehicleImpactResponder Ensure(GameObject vehicle)
        {
            if (vehicle == null) return null;
            var responder = vehicle.GetComponent<VehicleImpactResponder>();
            return responder != null ? responder : vehicle.AddComponent<VehicleImpactResponder>();
        }

        public float DamageForSpeed(float speed)
        {
            if (speed < minimumImpactSpeed) return 0f;
            float t = Mathf.InverseLerp(minimumImpactSpeed, fullDamageSpeed, speed);
            return Mathf.Lerp(minimumDamage, maximumDamage, t);
        }

        public Vector3 ImpactVelocityFor(Vector3 momentum, float speed)
        {
            Vector3 result = Vector3.ClampMagnitude(momentum * .50f, 11f);
            result += Vector3.up * Mathf.Min(1.8f, speed * .08f);
            return result;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision == null || collision.collider == null) return;
            float speed = collision.relativeVelocity.magnitude;
            if (speed < minimumImpactSpeed) return;

            ContactPoint contact = collision.contactCount > 0
                ? collision.GetContact(0)
                : new ContactPoint();
            Vector3 point = collision.contactCount > 0
                ? contact.point
                : collision.collider.bounds.center;
            Vector3 momentum = body != null ? body.linearVelocity : -collision.relativeVelocity;
            TryApplyImpact(collision.collider, speed, momentum, point);
        }

        void OnTriggerEnter(Collider other)
        {
            if (other == null || body == null) return;
            float speed = body.linearVelocity.magnitude;
            if (speed < minimumImpactSpeed) return;
            Vector3 point = other.ClosestPoint(transform.position);
            TryApplyImpact(other, speed, body.linearVelocity, point);
        }

        public bool TryApplyImpact(Collider other, float speed, Vector3 momentum, Vector3 point)
        {
            if (other == null || other.transform.IsChildOf(transform)) return false;

            NpcCombatHealth health = other.GetComponentInParent<NpcCombatHealth>();
            TownNPCInteractable townNpc = other.GetComponentInParent<TownNPCInteractable>();
            if (health == null && townNpc != null)
            {
                if (townNpc.GetComponent<NpcRagdoll>() == null)
                    townNpc.gameObject.AddComponent<NpcRagdoll>();
                health = townNpc.GetComponent<NpcCombatHealth>();
                if (health == null) health = townNpc.gameObject.AddComponent<NpcCombatHealth>();
                health.ConfigureFromRole(townNpc);
            }
            if (health == null) return false;

            int id = health.GetInstanceID();
            if (nextAllowedHit.TryGetValue(id, out float readyAt) && Time.time < readyAt) return false;
            nextAllowedHit[id] = Time.time + repeatHitCooldown;

            if (momentum.sqrMagnitude < .01f)
                momentum = (health.transform.position - transform.position).normalized * speed;
            Vector3 impactVelocity = ImpactVelocityFor(momentum, speed);
            health.HitFromImpact(DamageForSpeed(speed), impactVelocity, point);
            return true;
        }
    }
}