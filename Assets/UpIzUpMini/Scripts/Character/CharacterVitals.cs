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
        [SerializeField] private float staminaDrainPerSecond = 14f;
        [Tooltip("Recovery while walking.")]
        [SerializeField] private float staminaRegenWalking = 8f;
        [Tooltip("Recovery while standing still - deliberately faster.")]
        [SerializeField] private float staminaRegenIdle = 22f;

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
            // Expire any temporary enhancement.
            if (_boostEndsAt > 0f && Time.time >= _boostEndsAt)
            {
                maxStamina -= _boostExtraStamina;
                Stamina = Mathf.Min(Stamina, maxStamina);
                _boostExtraStamina = 0f;
                _boostRegenMultiplier = 1f;
                _boostEndsAt = -1f;
            }

            if (_running)
            {
                Stamina = Mathf.Max(0f, Stamina - staminaDrainPerSecond * Time.deltaTime);
            }
            else
            {
                // Recovers while walking, and noticeably faster at a stop.
                float regen = _moving ? staminaRegenWalking : staminaRegenIdle;
                Stamina = Mathf.Min(maxStamina, Stamina + regen * _boostRegenMultiplier * Time.deltaTime);
            }
        }

        /// <summary>True while moving fast enough to be "running" - also
        /// prevents actually running once stamina is spent.</summary>
        public bool CanRun => Stamina > 1f;

        public void SetRunning(bool running) => _running = running && CanRun;

        /// <summary>Lets stamina recover faster when standing still.</summary>
        public void SetMoving(bool moving) => _moving = moving;

        private bool _moving;

        /// <summary>Full restore - used when resting at a safehouse.</summary>
        public void Restore()
        {
            Health = maxHealth;
            Stamina = maxStamina;
        }

        public void Heal(float amount)
        {
            Health = Mathf.Min(maxHealth, Health + amount);
        }

        public void RestoreStamina(float amount)
        {
            Stamina = Mathf.Min(maxStamina, Stamina + amount);
        }

        public void Damage(float amount)
        {
            Health = Mathf.Max(0f, Health - amount);
        }

        /// <summary>
        /// Temporary boost from an enhancement item - raises the ceiling and
        /// tops the character up, so the effect is visible on the meters.
        /// </summary>
        public void ApplyBoost(float extraMaxStamina, float regenMultiplier, float seconds)
        {
            maxStamina += extraMaxStamina;
            Stamina = maxStamina;
            _boostRegenMultiplier = regenMultiplier;
            _boostEndsAt = Time.time + seconds;
            _boostExtraStamina = extraMaxStamina;
        }

        private float _boostEndsAt = -1f;
        private float _boostExtraStamina;
        private float _boostRegenMultiplier = 1f;
    }
}
