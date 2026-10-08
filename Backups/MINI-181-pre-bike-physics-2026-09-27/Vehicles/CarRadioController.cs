using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-074. "add a radio for when in the range with one song that i
    /// have permission for, i want to be able to turn it on and off up is
    /// up. I have the contract to play it in the game."
    ///
    /// One track ("Up Is Up" - DeLuxe F & Quan Dan), toggled by the DRIVER
    /// only (a passenger fiddling with the radio while someone else drives
    /// would be odd), and only reachable/audible while someone is actually
    /// in the car - it starts stopped, and is force-stopped on exit so it
    /// can never keep looping out in the world after the player gets out.
    ///
    /// 2D (non-spatialised) rather than a 3D positioned source: this is a
    /// car radio the driver/passengers hear, not an ambient world sound
    /// other characters should hear you approaching with, so there is no
    /// falloff/distance behaviour to get right.
    /// </summary>
    [RequireComponent(typeof(CarInteractable))]
    public class CarRadioController : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [SerializeField] private KeyCode toggleKey = KeyCode.M;
        [SerializeField] private CarInteractable car;

        [Tooltip("MINI-078: \"when i press M should show the song playing as well\" - shown via the same mission-banner Alert() every other player-facing announcement in this project already uses.")]
        [SerializeField] private string nowPlayingLabel = "UP IS UP - DeLuxe F & Quan Dan ft. Arnan, Alyjah & Nahim";

        public bool IsPlaying => source != null && source.isPlaying;

        private void Awake()
        {
            if (car == null) car = GetComponent<CarInteractable>();
            if (source == null) source = GetComponent<AudioSource>();
            if (source != null)
            {
                source.playOnAwake = false;
                source.loop = true;
                source.spatialBlend = 0f;
            }
        }

        private void Update()
        {
            if (car == null || source == null) return;

            // Stop the instant nobody is driving - a passenger-only car (or
            // an empty parked one) should never be the one playing music.
            if (!car.HasDriver)
            {
                if (source.isPlaying) source.Stop();
                return;
            }

            if (Input.GetKeyDown(toggleKey))
            {
                if (source.isPlaying)
                {
                    source.Stop();
                    UpIzUpMini.Missions.MissionSystem.Instance?.Alert("RADIO OFF");
                }
                else
                {
                    source.Play();
                    // MINI-078: "when i press M should show the song playing
                    // as well" - reuses the same banner every other player-
                    // facing announcement in this project already fires
                    // through (purchases, mission briefings), rather than a
                    // new UI element for one line of text.
                    UpIzUpMini.Missions.MissionSystem.Instance?.Alert($"RADIO ON\n{nowPlayingLabel}");
                }
            }
        }
    }
}
