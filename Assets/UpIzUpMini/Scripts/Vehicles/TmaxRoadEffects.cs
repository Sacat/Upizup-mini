using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// Lightweight TMAX-only exhaust and rear-tyre road effects. Reuses the
    /// installed Motorbike Physics Tool's skid trail/smoke art, but reads the
    /// TMAX's own proven WheelColliders instead of re-enabling the incompatible
    /// vendor controller stack removed in MINI-127.
    /// </summary>
    public sealed class TmaxRoadEffects : MonoBehaviour
    {
        [SerializeField] private WheelCollider frontWheel;
        [SerializeField] private WheelCollider rearWheel;
        [SerializeField] private Transform skidTrailPrefab;
        [SerializeField] private ParticleSystem smokePrefab;
        [SerializeField] private float minimumMarkSpeedKmh = 6f;
        [SerializeField] private float minimumForwardSlip = 0.38f;
        [SerializeField] private float minimumSidewaysSlip = 0.32f;
        [SerializeField] private float markStopDelay = 0.18f;

        private TmaxBikeControllerCustom _controller;
        private BikeInteractable _interactable;
        private Transform _activeTrail;
        private ParticleSystem _tyreSmoke;
        private ParticleSystem _exhaustSmoke;
        private float _markStopTimer;
        private float _nextExhaustPuffAt;
        private ParticleSystem _wheelieSparks;
        private float _nextSparkAt;

        public WheelCollider FrontWheel => frontWheel;
        public WheelCollider RearWheel => rearWheel;
        public Transform SkidTrailPrefab => skidTrailPrefab;
        public ParticleSystem SmokePrefab => smokePrefab;
        public const float WheelieSparkMinimumSpeedMph = 12f;
        public const float WheelieSparkMinimumSpeedKmh = WheelieSparkMinimumSpeedMph * 1.609344f;
        public const int WheelieSparkParticleLimit = 20;
        public const float ExhaustMaximumAlpha = 0.22f;
        public int ExhaustParticleLimit => 24;

        public void Configure(
            WheelCollider front,
            WheelCollider rear,
            Transform trailPrefab,
            ParticleSystem particlePrefab)
        {
            frontWheel = front;
            rearWheel = rear;
            skidTrailPrefab = trailPrefab;
            smokePrefab = particlePrefab;
        }

        private void Awake()
        {
            _controller = GetComponent<TmaxBikeControllerCustom>();
            _interactable = GetComponent<BikeInteractable>();
            CreateExhaust();
            CreateWheelieSparks();

            if (smokePrefab != null)
            {
                _tyreSmoke = Instantiate(smokePrefab);
                _tyreSmoke.name = "TMAX_RearTyreSmoke";
                _tyreSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            Debug.Log($"MINI-128 TMAX EFFECTS: Ready at {transform.position}; exhaust cap {ExhaustParticleLimit}, rear skid art wired={skidTrailPrefab != null}.");
        }

        private void Update()
        {
            bool ridden = _interactable != null && _interactable.HasRider;
            UpdateExhaust(ridden);
            UpdateWheelieSparks(ridden);
            UpdateRearSkid(ridden);
        }

        private void CreateExhaust()
        {
            if (rearWheel == null || smokePrefab == null) return;

            var anchor = new GameObject("TMAX_ExhaustOutlet").transform;
            anchor.SetParent(transform, true);
            anchor.position = rearWheel.transform.position
                              + transform.up * 0.27f
                              + transform.right * 0.43f
                              - transform.forward * 0.44f;
            anchor.rotation = Quaternion.LookRotation(-transform.forward, transform.up);

            _exhaustSmoke = Instantiate(smokePrefab, anchor);
            _exhaustSmoke.name = "TMAX_ExhaustSmoke";
            _exhaustSmoke.transform.localPosition = Vector3.zero;
            _exhaustSmoke.transform.localRotation = Quaternion.identity;

            var main = _exhaustSmoke.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = ExhaustParticleLimit;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.28f, 0.55f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.19f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.22f, 0.22f, 0.22f, ExhaustMaximumAlpha),
                new Color(0.58f, 0.58f, 0.58f, 0.10f));

            var shape = _exhaustSmoke.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 11f;
            shape.radius = 0.025f;

            var emission = _exhaustSmoke.emission;
            emission.rateOverTime = 0f;
            _exhaustSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void UpdateExhaust(bool ridden)
        {
            if (_exhaustSmoke == null) return;

            var emission = _exhaustSmoke.emission;
            if (!ridden)
            {
                emission.rateOverTime = 0f;
                if (_exhaustSmoke.isPlaying)
                    _exhaustSmoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                return;
            }

            // Use explicit, bounded puffs rather than relying only on the
            // imported dust prefab's inherited rate curve. The first live
            // screenshot proved that curve was too faint to read behind the
            // TMAX even though the system was playing.
            emission.rateOverTime = 0f;
            if (!_exhaustSmoke.isPlaying) _exhaustSmoke.Play();
            if (Time.time >= _nextExhaustPuffAt)
            {
                float speed = _controller != null ? Mathf.Abs(_controller.SpeedKmh) : 0f;
                _nextExhaustPuffAt = Time.time + Mathf.Lerp(0.30f, 0.18f, Mathf.Clamp01(speed / 55f));
                _exhaustSmoke.Emit(1);
            }
        }

        private void CreateWheelieSparks()
        {
            if (rearWheel == null || smokePrefab == null) return;

            var anchor = new GameObject("TMAX_RearUndersideSparkPoint").transform;
            anchor.SetParent(transform, true);
            anchor.position = rearWheel.transform.position
                              + transform.up * 0.05f
                              - transform.forward * 0.38f;
            anchor.rotation = Quaternion.LookRotation(-transform.forward - transform.up * 0.2f, transform.up);

            _wheelieSparks = Instantiate(smokePrefab, anchor);
            _wheelieSparks.name = "TMAX_WheelieSparks";
            _wheelieSparks.transform.localPosition = Vector3.zero;
            _wheelieSparks.transform.localRotation = Quaternion.identity;

            var main = _wheelieSparks.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = WheelieSparkParticleLimit;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.30f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 2.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.038f);
            main.gravityModifier = 0.38f;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.86f, 0.28f, 0.92f),
                new Color(1f, 0.30f, 0.04f, 0.72f));

            var emission = _wheelieSparks.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            var shape = _wheelieSparks.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 18f;
            shape.radius = 0.018f;

            var noise = _wheelieSparks.noise;
            noise.enabled = false;
            var trails = _wheelieSparks.trails;
            trails.enabled = false;

            var renderer = _wheelieSparks.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.lengthScale = 0.30f;
                renderer.velocityScale = 0.08f;
            }

            _wheelieSparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public static bool ShouldEmitWheelieSparks(bool ridden, float wheelieDegrees, float speedKmh)
        {
            return ridden
                   && wheelieDegrees >= BikeCrashEjectionController.MaximumWheelieDegrees - 0.25f
                   && Mathf.Abs(speedKmh) >= WheelieSparkMinimumSpeedKmh;
        }

        private void UpdateWheelieSparks(bool ridden)
        {
            if (_wheelieSparks == null || _controller == null) return;

            if (!ShouldEmitWheelieSparks(ridden, _controller.WheelieAngle, _controller.SpeedKmh))
            {
                if (_wheelieSparks.isPlaying)
                    _wheelieSparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                return;
            }

            if (!_wheelieSparks.isPlaying) _wheelieSparks.Play();
            if (Time.time >= _nextSparkAt)
            {
                _nextSparkAt = Time.time + 0.075f;
                _wheelieSparks.Emit(2);
            }
        }
        private void UpdateRearSkid(bool ridden)
        {
            bool shouldMark = false;
            WheelHit hit = default;
            float speed = _controller != null ? Mathf.Abs(_controller.SpeedKmh) : 0f;

            if (ridden && rearWheel != null && speed >= minimumMarkSpeedKmh
                && rearWheel.isGrounded && rearWheel.GetGroundHit(out hit))
            {
                bool braking = Input.GetKey(KeyCode.Space);
                bool slipping = Mathf.Abs(hit.forwardSlip) >= minimumForwardSlip
                                || Mathf.Abs(hit.sidewaysSlip) >= minimumSidewaysSlip;
                shouldMark = braking || slipping;
            }

            if (shouldMark)
            {
                _markStopTimer = 0f;
                StartTrail();

                if (_tyreSmoke != null)
                {
                    _tyreSmoke.transform.position = hit.point + hit.normal * 0.025f;
                    _tyreSmoke.Emit(1);
                }
            }
            else if (_activeTrail != null)
            {
                _markStopTimer += Time.deltaTime;
                if (_markStopTimer >= markStopDelay || rearWheel == null || !rearWheel.isGrounded)
                    EndTrail();
            }
        }

        private void StartTrail()
        {
            if (_activeTrail != null || skidTrailPrefab == null || rearWheel == null) return;

            _activeTrail = Instantiate(skidTrailPrefab, rearWheel.transform);
            _activeTrail.name = "TMAX_RearSkidMark";
            _activeTrail.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _activeTrail.localPosition = -Vector3.up * (rearWheel.radius + 0.025f);
        }

        private void EndTrail()
        {
            if (_activeTrail == null) return;

            Transform finished = _activeTrail;
            _activeTrail = null;
            finished.SetParent(null, true);
            finished.rotation = Quaternion.Euler(90f, 0f, 0f);
            Destroy(finished.gameObject, 15f);
        }

        private void OnDisable()
        {
            EndTrail();
            if (_exhaustSmoke != null)
                _exhaustSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (_tyreSmoke != null)
                _tyreSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (_wheelieSparks != null)
                _wheelieSparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnDestroy()
        {
            if (_tyreSmoke != null) Destroy(_tyreSmoke.gameObject);
            if (_wheelieSparks != null) Destroy(_wheelieSparks.gameObject);
        }
    }
}
