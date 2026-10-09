#if MINI206_DRAFT
// =====================================================================================================================
// MINI-206 DRAFT - written in the cloud design lane, NEVER COMPILED OR RUN. Enable with the scripting define MINI206_DRAFT.
// =====================================================================================================================
using UnityEngine;

namespace UpIzUpMini.Vehicles.Draft
{
    /// <summary>
    /// Owner request (2026-10-09): "the wheeling should be easy like how it is just more realistic", plus better leaning and
    /// road physics, on the CURRENT TMAX (TmaxBikeControllerCustom). Add to the TMAX root; untick to get the old behaviour back.
    ///
    /// 1) Ride values (MINI-206-riding.md path A), applied at Awake through the controller's public setters - no prefab edit:
    ///    extra air gravity 2500 -> 15 m/s^2 after 0.3 s airborne with a 0.4 s ramp (no more slam-and-bounce off kerbs/crests),
    ///    yaw lock 1 -> 0.15 and spin damping only above 70 deg/s (the bike can slip and carry yaw, so it stops turning like a cursor),
    ///    upright assist 16 -> 5 (x1.5 when slow) so the body can settle on cambers, hill-climb push 9 -> 3.
    ///
    /// 2) Wheelie shaping. The controller still decides WHEN a wheelie happens (hold Q, speed window - exactly as easy as now) and
    ///    how high it may go (MaxWheelieAngle). Its own lift is a constant 72 deg/s ramp held perfectly rigid. This component
    ///    runs right after it in the same physics step and re-poses the bike about the same rear contact patch with:
    ///    - a second-order spring toward the controller's angle: fast "clutch pop" (omega 7 rad/s), eases into the balance
    ///      angle with a small overshoot (zeta 0.62) instead of a straight ramp;
    ///    - throttle trims the balance angle (+-6 deg) like a real rider feathering the throttle; brake drops it;
    ///    - on release the front falls with an accelerating, gravity-like drop and a short fork-compression bump on landing
    ///      (the WheelCollider suspension does the bump);
    ///    - a bounded natural wobble (pitch +-1.2 deg at 0.55 Hz, roll +-0.8 deg) scaled by height, and counter-lean roll
    ///      into steering (up to 7 deg) so steering in a wheelie banks the bike instead of pivoting it flat;
    ///    - rear squat: the body sinks up to 3 cm onto the rear spring while up.
    ///    Crash/ejection logic, the 89 deg cap and the wheelie gates are untouched.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class TmaxRideFeel : MonoBehaviour
    {
        [Header("Ride values (path A)")]
        public bool applyRideValues = true;
        public float extraAirGravity = 15f, airborneGraceSeconds = 0.3f, airGravityRampSeconds = 0.4f;
        public float yawLockStrength = 0.15f, yawSpinThreshold = 70f, uprightAssist = 5f, uprightLowSpeedBoost = 1.5f, hillClimbAssist = 3f;

        [Header("Wheelie feel")]
        public bool shapeWheelie = true;
        public float liftOmega = 7f, liftZeta = 0.62f, fallOmega = 5.5f, fallZeta = 0.85f;
        public float throttleTrimDeg = 6f, wobblePitchDeg = 1.2f, wobbleRollDeg = 0.8f, wobbleHz = 0.55f, steerRollDeg = 7f, rearSquatM = 0.03f;

        TmaxBikeControllerCustom _c; Rigidbody _rb; WheelCollider _rear;
        float _ang, _vel, _seed, _prevYaw; bool _active;

        void Awake()
        {
            _c = GetComponent<TmaxBikeControllerCustom>(); _rb = GetComponent<Rigidbody>(); _seed = Random.value * 100f;
            foreach (var w in GetComponentsInChildren<WheelCollider>(true))
                if (_rear == null || transform.InverseTransformPoint(w.transform.position).z < transform.InverseTransformPoint(_rear.transform.position).z) _rear = w;
            if (_c == null || !applyRideValues) return;
            _c.ExtraAirGravity = extraAirGravity; _c.AirborneGraceSeconds = airborneGraceSeconds; _c.AirGravityRampSeconds = airGravityRampSeconds;
            _c.YawLockStrength = yawLockStrength; _c.YawSpinThreshold = yawSpinThreshold;
            _c.UprightAssist = uprightAssist; _c.UprightAssistLowSpeedBoost = uprightLowSpeedBoost; _c.HillClimbAssist = hillClimbAssist;
        }

        void FixedUpdate()
        {
            if (!shapeWheelie || _c == null || _rb == null || _rear == null) return;
            float goal = _c.WheelieAngle;                    // controller's linear target (0 when released / out of window)
            if (goal > 0.01f)
            {
                float throttle = Mathf.Clamp(Input.GetAxis("Vertical"), -1f, 1f);
                goal = Mathf.Clamp(goal + throttle * throttleTrimDeg * Mathf.Clamp01(goal / 20f), 0f, _c.MaxWheelieAngle);
            }
            if (goal <= 0.01f && _ang <= 0.3f) { _ang = 0f; _vel = 0f; _active = false; return; }
            bool rising = goal >= _ang;
            float w = rising ? liftOmega : fallOmega, z = rising ? liftZeta : fallZeta, dt = Time.fixedDeltaTime;
            float acc = w * w * (goal - _ang) - 2f * z * w * _vel;
            if (!rising) acc -= 60f * Mathf.Cos(_ang * Mathf.Deg2Rad);   // gravity-like extra drop when coming down (deg/s^2)
            _vel += acc * dt; _ang = Mathf.Clamp(_ang + _vel * dt, 0f, _c.MaxWheelieAngle);
            if (_ang <= 0.3f && !rising) { _ang = 0f; _vel = 0f; _active = false; return; }

            float h = Mathf.Clamp01(_ang / 45f), t = Time.time;
            float yaw = transform.eulerAngles.y;
            float yawRate = _active ? Mathf.DeltaAngle(_prevYaw, yaw) / dt : 0f; _prevYaw = yaw; _active = true;
            float pitch = _ang + wobblePitchDeg * h * Mathf.Sin(2f * Mathf.PI * wobbleHz * t + _seed);
            float roll = Mathf.Clamp(-yawRate * 0.12f, -steerRollDeg, steerRollDeg) + wobbleRollDeg * h * (Mathf.PerlinNoise(t * 0.7f, _seed) * 2f - 1f);

            Vector3 pivot = _rear.transform.position - Vector3.up * _rear.radius;
            Quaternion newRot = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(-pitch, 0f, 0f) * Quaternion.Euler(0f, 0f, roll);
            Quaternion delta = newRot * Quaternion.Inverse(_rb.rotation);
            Vector3 newPos = pivot + delta * (_rb.position - pivot) - Vector3.up * (rearSquatM * h);
            _rb.MoveRotation(newRot); _rb.MovePosition(newPos);
            _rb.angularVelocity = Vector3.zero;
        }
    }
}
#endif
