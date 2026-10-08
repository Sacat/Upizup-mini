using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// Mobile-bounded Range Rover tyre marks, tyre dust and tailpipe puffs.
    /// Marks appear only while a driven rear wheel is braking or slipping.
    /// </summary>
    public sealed class RangeRoverRoadEffects : MonoBehaviour
    {
        [SerializeField] private WheelCollider rearLeft;
        [SerializeField] private WheelCollider rearRight;
        [SerializeField] private Transform exhaustOutlet;
        [SerializeField] private Transform skidTrailPrefab;
        [SerializeField] private ParticleSystem smokePrefab;
        [SerializeField] private float minimumMarkSpeedKmh = 7f;
        [SerializeField] private float minimumForwardSlip = 0.42f;
        [SerializeField] private float minimumSidewaysSlip = 0.35f;
        [SerializeField] private float markStopDelay = 0.16f;

        private readonly Transform[] _trails = new Transform[2];
        private readonly ParticleSystem[] _tyreSmoke = new ParticleSystem[2];
        private readonly float[] _stopTimers = new float[2];
        private CarController _controller;
        private CarInteractable _interactable;
        private ParticleSystem _exhaustSmoke;
        private float _nextExhaustPuffAt;

        public const int ExhaustParticleLimit = 28;
        public const int TyreParticleLimit = 16;
        public WheelCollider RearLeft => rearLeft;
        public WheelCollider RearRight => rearRight;
        public Transform ExhaustOutlet => exhaustOutlet;
        public Transform SkidTrailPrefab => skidTrailPrefab;
        public ParticleSystem SmokePrefab => smokePrefab;

        private void Awake()
        {
            _controller = GetComponent<CarController>();
            _interactable = GetComponent<CarInteractable>();
            CreateParticles();
        }

        private void Update()
        {
            bool driven = _interactable != null && _interactable.HasDriver;
            UpdateExhaust(driven);
            UpdateRearWheel(0, rearLeft, driven);
            UpdateRearWheel(1, rearRight, driven);
        }

        private void CreateParticles()
        {
            if (smokePrefab == null) return;

            if (exhaustOutlet != null)
            {
                _exhaustSmoke = Instantiate(smokePrefab, exhaustOutlet);
                _exhaustSmoke.name = "RangeRover_ExhaustSmoke";
                _exhaustSmoke.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                ConfigureSmoke(_exhaustSmoke, ExhaustParticleLimit, 0.12f, 0.24f);
            }

            for (int i = 0; i < _tyreSmoke.Length; i++)
            {
                _tyreSmoke[i] = Instantiate(smokePrefab);
                _tyreSmoke[i].name = i == 0 ? "RangeRover_LeftTyreSmoke" : "RangeRover_RightTyreSmoke";
                ConfigureSmoke(_tyreSmoke[i], TyreParticleLimit, 0.10f, 0.20f);
            }
        }

        private static void ConfigureSmoke(ParticleSystem particles, int limit, float minSize, float maxSize)
        {
            var main = particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = limit;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.75f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.22f, 0.48f);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.18f, 0.18f, 0.18f, 0.42f),
                new Color(0.58f, 0.58f, 0.58f, 0.22f));
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void UpdateExhaust(bool driven)
        {
            if (_exhaustSmoke == null) return;
            if (!driven)
            {
                if (_exhaustSmoke.isPlaying)
                    _exhaustSmoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                return;
            }

            if (!_exhaustSmoke.isPlaying) _exhaustSmoke.Play();
            if (Time.time < _nextExhaustPuffAt) return;

            float speed = _controller != null ? Mathf.Abs(_controller.SpeedKmh) : 0f;
            _nextExhaustPuffAt = Time.time + Mathf.Lerp(0.22f, 0.12f, Mathf.Clamp01(speed / 70f));
            _exhaustSmoke.Emit(1);
        }

        private void UpdateRearWheel(int index, WheelCollider wheel, bool driven)
        {
            bool shouldMark = false;
            WheelHit hit = default;
            float speed = _controller != null ? Mathf.Abs(_controller.SpeedKmh) : 0f;

            if (driven && wheel != null && speed >= minimumMarkSpeedKmh
                && wheel.isGrounded && wheel.GetGroundHit(out hit))
            {
                bool braking = Input.GetKey(KeyCode.Space);
                bool slipping = Mathf.Abs(hit.forwardSlip) >= minimumForwardSlip
                                || Mathf.Abs(hit.sidewaysSlip) >= minimumSidewaysSlip;
                shouldMark = braking || slipping;
            }

            if (shouldMark)
            {
                _stopTimers[index] = 0f;
                StartTrail(index, wheel);
                if (_tyreSmoke[index] != null)
                {
                    _tyreSmoke[index].transform.position = hit.point + hit.normal * 0.025f;
                    _tyreSmoke[index].Emit(1);
                }
            }
            else if (_trails[index] != null)
            {
                _stopTimers[index] += Time.deltaTime;
                if (_stopTimers[index] >= markStopDelay || wheel == null || !wheel.isGrounded)
                    EndTrail(index);
            }
        }

        private void StartTrail(int index, WheelCollider wheel)
        {
            if (_trails[index] != null || skidTrailPrefab == null || wheel == null) return;
            _trails[index] = Instantiate(skidTrailPrefab, wheel.transform);
            _trails[index].name = index == 0 ? "RangeRover_LeftSkidMark" : "RangeRover_RightSkidMark";
            _trails[index].localRotation = Quaternion.Euler(90f, 0f, 0f);
            _trails[index].localPosition = -Vector3.up * (wheel.radius + 0.025f);
        }

        private void EndTrail(int index)
        {
            if (_trails[index] == null) return;
            Transform finished = _trails[index];
            _trails[index] = null;
            finished.SetParent(null, true);
            finished.rotation = Quaternion.Euler(90f, 0f, 0f);
            Destroy(finished.gameObject, 14f);
        }

        private void OnDisable()
        {
            EndTrail(0);
            EndTrail(1);
            if (_exhaustSmoke != null)
                _exhaustSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            for (int i = 0; i < _tyreSmoke.Length; i++)
                if (_tyreSmoke[i] != null)
                    _tyreSmoke[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _tyreSmoke.Length; i++)
                if (_tyreSmoke[i] != null) Destroy(_tyreSmoke[i].gameObject);
        }

#if UNITY_EDITOR
        public void Configure(
            WheelCollider left,
            WheelCollider right,
            Transform outlet,
            Transform trail,
            ParticleSystem particles)
        {
            rearLeft = left;
            rearRight = right;
            exhaustOutlet = outlet;
            skidTrailPrefab = trail;
            smokePrefab = particles;
        }
#endif
    }
}
