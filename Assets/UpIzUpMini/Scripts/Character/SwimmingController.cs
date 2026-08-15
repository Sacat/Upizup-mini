using UnityEngine;

namespace UpIzUpMini.Character
{
    /// <summary>
    /// Lets a character enter the sea and float/swim instead of sinking or
    /// being blocked. Deliberately simple: when the character's feet drop
    /// below the water plane, buoyancy holds them near the surface and
    /// gravity is suppressed. Works alongside PlayerController rather than
    /// replacing it - PlayerController still handles input and horizontal
    /// movement; this only overrides vertical behaviour while submerged.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class SwimmingController : MonoBehaviour
    {
        [SerializeField] private float waterLevelY = 1.2f;
        [SerializeField] private float floatOffset = 1.1f;
        [SerializeField] private float bobAmplitude = 0.06f;
        [SerializeField] private float bobSpeed = 1.5f;
        [SerializeField] private float verticalLerp = 3f;

        private CharacterController _controller;

        public bool IsSwimming { get; private set; }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void LateUpdate()
        {
            float feetY = transform.position.y;
            IsSwimming = feetY < waterLevelY;
            if (!IsSwimming) return;

            // Hold the character at the surface with a gentle bob, instead
            // of letting PlayerController's gravity pull them under.
            float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            float targetY = waterLevelY - floatOffset + bob;

            Vector3 pos = transform.position;
            float newY = Mathf.Lerp(pos.y, targetY, verticalLerp * Time.deltaTime);
            _controller.enabled = false;
            transform.position = new Vector3(pos.x, newY, pos.z);
            _controller.enabled = true;
        }

        public void SetWaterLevel(float y) => waterLevelY = y;
    }
}
