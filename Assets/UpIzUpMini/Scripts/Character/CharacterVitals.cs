using UnityEngine;

namespace UpIzUpMini.Character
{
    /// <summary>
    /// Per-character health and stamina - kept separate per Docs/STORY.md
    /// ("retain separate health, stamina, position, and abilities").
    /// Stamina drains while the owning PlayerController reports running,
    /// and regenerates otherwise; both work whether or not this character
    /// is the currently-controlled one (FollowController also queries
    /// IsRunning-equivalent state via SetRunning).
    /// </summary>
    public class CharacterVitals : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float staminaDrainPerSecond = 18f;
        [SerializeField] private float staminaRegenPerSecond = 12f;

        public float Health { get; private set; }
        public float Stamina { get; private set; }
        public float MaxHealth => maxHealth;
        public float MaxStamina => maxStamina;

        private bool _running;

        private void Awake()
        {
            Health = maxHealth;
            Stamina = maxStamina;
        }

        private void Update()
        {
            Stamina = _running
                ? Mathf.Max(0f, Stamina - staminaDrainPerSecond * Time.deltaTime)
                : Mathf.Min(maxStamina, Stamina + staminaRegenPerSecond * Time.deltaTime);
        }

        /// <summary>True while moving fast enough to be "running" - also
        /// prevents actually running once stamina is spent.</summary>
        public bool CanRun => Stamina > 1f;

        public void SetRunning(bool running) => _running = running && CanRun;
    }
}
