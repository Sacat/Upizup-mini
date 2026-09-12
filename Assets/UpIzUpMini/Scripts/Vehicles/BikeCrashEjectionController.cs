using UnityEngine;
using UpIzUpMini.Combat;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// Shared TMAX/SuperMoto crash detector. It observes real collision speed,
    /// then asks the active interactable to release both riders into the shared
    /// player ragdoll. Wheelie angle never causes ejection.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class BikeCrashEjectionController : MonoBehaviour
    {
        public const float MaximumWheelieDegrees = 89f;

        public static float ClampWheelieDegrees(float requestedDegrees) =>
            Mathf.Clamp(requestedDegrees, 0f, MaximumWheelieDegrees);
        // MINI-167: user reported bikes crash too easily - raised 50%
        // (16.5 -> 24.75 m/s), then still too easy - raised a further 75%
        // on top of that (24.75 -> 43.31 m/s, ~156 km/h). NPC-hit (1.5x)
        // and wheelie (1.15x) gates are both ratios of this same base, so
        // they scale with it automatically.
        [SerializeField] private float hardImpactSpeed = 43.31f;
        // MINI-167: "make it 90% harder to crash when wheelieing" - raised
        // 1.15 -> 2.185 (+90%). Combined with hardImpactSpeed=43.31, the
        // wheelie impact-speed gate is now ~94.6 m/s (340 km/h) - on top of
        // the separate, already-strict linearSpeed>=hardImpactSpeed gate
        // below (itself already above the TMAX's own top speed). Wheelie
        // crashes from a wall hit are now extremely rare by design.
        [SerializeField] private float wheelieImpactMultiplier = 2.185f;
        [SerializeField] private float mountGraceSeconds = 0.45f;
        [SerializeField] private float repeatCooldown = 2.5f;

        private Rigidbody _body;
        private BikeInteractable _tmax;
        private SuperMotoVehicleInteractable _superMoto;
        private TmaxBikeControllerCustom _tmaxController;
        private SuperMotoWheelieAssist _superMotoWheelie;
        private float _riderDetectedAt;
        private float _nextAllowedEjection;
        private bool _hadRider;

        public float HardImpactSpeed => hardImpactSpeed;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _tmax = GetComponent<BikeInteractable>();
            _superMoto = GetComponent<SuperMotoVehicleInteractable>();
            _tmaxController = GetComponent<TmaxBikeControllerCustom>();
            _superMotoWheelie = GetComponent<SuperMotoWheelieAssist>();
        }

        public static BikeCrashEjectionController Ensure(GameObject bike)
        {
            if (bike == null) return null;
            var result = bike.GetComponent<BikeCrashEjectionController>();
            return result != null ? result : bike.AddComponent<BikeCrashEjectionController>();
        }

        private bool HasRider => (_tmax != null && _tmax.HasRider)
                                 || (_superMoto != null && _superMoto.HasRider);

        private void Update()
        {
            bool hasRider = HasRider;
            if (hasRider && !_hadRider) _riderDetectedAt = Time.time;
            _hadRider = hasRider;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!HasRider || collision == null || Time.time < _nextAllowedEjection
                || Time.time - _riderDetectedAt < mountGraceSeconds) return;

            // Hitting a person is handled by VehicleImpactResponder. A rider
            // ejects from that only at a much higher speed; normal NPC contact
            // must not throw the player off every time.
            bool hitNpc = collision.collider != null
                          && (collision.collider.GetComponentInParent<NpcCombatHealth>() != null
                              || collision.collider.GetComponentInParent<TownNPCInteractable>() != null);
            Vector3 normal = collision.contactCount > 0
                ? collision.GetContact(0).normal
                : Vector3.zero;
            float linearSpeed = _body != null
                ? _body.linearVelocity.magnitude
                : collision.relativeVelocity.magnitude;
            if (!ShouldEjectForImpact(
                    collision.relativeVelocity, normal, linearSpeed, hitNpc, IsWheelieActive)) return;

            Vector3 velocity = _body != null ? _body.linearVelocity : -collision.relativeVelocity;
            Vector3 point = collision.contactCount > 0
                ? collision.GetContact(0).point
                : transform.position + Vector3.up;
            ForceEject(velocity, point);
        }

        public bool ForceEject(Vector3 impactVelocity, Vector3 impactPoint)
        {
            if (!HasRider || Time.time < _nextAllowedEjection) return false;
            _nextAllowedEjection = Time.time + repeatCooldown;
            impactVelocity = Vector3.ClampMagnitude(impactVelocity, 14f) + Vector3.up * 1.2f;

            bool ejected = _tmax != null && _tmax.CrashEject(impactVelocity, impactPoint);
            if (!ejected && _superMoto != null)
                ejected = _superMoto.CrashEject(impactVelocity, impactPoint);
            return ejected;
        }

        /// <summary>Wheelie/tilt angle alone never ejects a rider.</summary>
        public bool ShouldEjectForTilt(float tiltDegrees, float speed) => false;
        public bool IsWheelieActive =>
            (_tmaxController != null && _tmaxController.WheelieAngle > 8f)
            || (_superMotoWheelie != null && _superMotoWheelie.CurrentRampDeg > 8f);

        public float ImpactThreshold(bool hitNpc, bool wheelieActive)
        {
            float threshold = hitNpc ? hardImpactSpeed * 1.5f : hardImpactSpeed;
            return wheelieActive ? threshold * wheelieImpactMultiplier : threshold;
        }

        public bool ShouldEjectForImpact(
            Vector3 relativeVelocity,
            Vector3 contactNormal,
            float linearSpeed,
            bool hitNpc,
            bool wheelieActive)
        {
            // MINI-167: "just remove crashing from wheelieing for now" -
            // no ejection at all while a wheelie is active, full stop.
            // wheelieImpactMultiplier is now unused for gating (kept, not
            // deleted, in case wheelie crashes are reinstated later at a
            // tuned value instead of fully disabled).
            if (wheelieActive) return false;
            if (ShouldIgnoreGroundContact(contactNormal)) return false;
            if (ImpactSpeedFor(relativeVelocity, contactNormal)
                < ImpactThreshold(hitNpc, wheelieActive)) return false;

            // The forced lift can give the tail a large contact-relative
            // velocity even when the bike itself is travelling slowly. A
            // wheelie crash therefore also needs genuine Rigidbody speed.
            return !wheelieActive || linearSpeed >= hardImpactSpeed;
        }

        public float ImpactSpeedFor(Vector3 relativeVelocity, Vector3 contactNormal) =>
            contactNormal.sqrMagnitude > 0.01f
                ? Mathf.Abs(Vector3.Dot(relativeVelocity, contactNormal.normalized))
                : relativeVelocity.magnitude;

        public bool ShouldIgnoreGroundContact(Vector3 contactNormal) =>
            contactNormal.sqrMagnitude > 0.01f && Vector3.Dot(contactNormal.normalized, Vector3.up) > 0.55f;

    }
}