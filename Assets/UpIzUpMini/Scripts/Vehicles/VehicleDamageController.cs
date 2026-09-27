using UnityEngine;
using UpIzUpMini.Combat;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// Shared lightweight durability and crash recovery. It does not change a
    /// vehicle's handling tune; severe accumulated damage briefly disables and
    /// safely rights the vehicle with partial health.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehicleDamageController : MonoBehaviour
    {
        [SerializeField] private float maximumHealth = 100f;
        [SerializeField] private float minimumDamageSpeed = 6f;
        [SerializeField] private float fullDamageSpeed = 20f;
        [SerializeField] private float maximumCollisionDamage = 62f;
        [SerializeField] private float recoveryDelay = 4f;
        [SerializeField] private float recoveryHealth = 45f;
        [SerializeField] private float repeatCollisionCooldown = 0.35f;

        private Rigidbody _body;
        private float _health;
        private float _recoverAt;
        private float _nextDamageAt;

        public float Health => _health;
        public float MaximumHealth => maximumHealth;
        public bool IsDisabled { get; private set; }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _health = maximumHealth;
        }

        public static VehicleDamageController Ensure(GameObject vehicle)
        {
            if (vehicle == null) return null;
            var result = vehicle.GetComponent<VehicleDamageController>();
            return result != null ? result : vehicle.AddComponent<VehicleDamageController>();
        }

        public float DamageForSpeed(float speed)
        {
            if (speed < minimumDamageSpeed) return 0f;
            float t = Mathf.InverseLerp(minimumDamageSpeed, fullDamageSpeed, speed);
            return Mathf.Lerp(4f, maximumCollisionDamage, t * t);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (IsDisabled || collision == null || Time.time < _nextDamageAt) return;
            if (collision.collider != null
                && collision.collider.GetComponentInParent<NpcCombatHealth>() != null) return;

            // MINI-181: road/ground contacts (landings, bumps, a wheelie's tail
            // scraping) never wear the bike down to a forced ejection, and
            // nothing damages it mid-wheelie. Walls and cars still do.
            if (collision.contactCount > 0 && collision.GetContact(0).normal.y > 0.55f) return;
            var crashCtl = GetComponent<BikeCrashEjectionController>();
            if (crashCtl != null && crashCtl.IsWheelieActive) return;

            float damage = DamageForSpeed(collision.relativeVelocity.magnitude);
            if (damage <= 0f) return;
            _nextDamageAt = Time.time + repeatCollisionCooldown;
            ApplyDamage(damage);
        }

        public void ApplyDamage(float amount)
        {
            if (IsDisabled || amount <= 0f) return;
            _health = Mathf.Max(0f, _health - amount);
            if (_health > 0f) return;

            IsDisabled = true;
            _recoverAt = Time.time + recoveryDelay;
            if (_body != null)
            {
                _body.linearVelocity *= 0.15f;
                _body.angularVelocity *= 0.15f;
            }
            GetComponent<BikeCrashEjectionController>()?.ForceEject(
                _body != null ? _body.linearVelocity : Vector3.zero,
                transform.position + Vector3.up);
        }

        private void Update()
        {
            if (!IsDisabled || Time.time < _recoverAt) return;
            RecoverVehicle();
        }

        public void RecoverVehicle()
        {
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            Vector3 position = transform.position;
            if (Physics.Raycast(position + Vector3.up * 4f, Vector3.down,
                out RaycastHit hit, 12f, ~0, QueryTriggerInteraction.Ignore))
                position = hit.point + Vector3.up * 0.35f;

            transform.SetPositionAndRotation(position,
                Quaternion.LookRotation(forward.normalized, Vector3.up));
            if (_body != null)
            {
                _body.linearVelocity = Vector3.zero;
                _body.angularVelocity = Vector3.zero;
                _body.WakeUp();
            }
            _health = Mathf.Clamp(recoveryHealth, 1f, maximumHealth);
            IsDisabled = false;
        }
    }
}
