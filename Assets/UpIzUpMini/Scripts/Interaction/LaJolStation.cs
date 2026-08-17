using UnityEngine;

namespace UpIzUpMini.Interaction
{
    /// <summary>
    /// MINI-054. La Jol - the Gwa Bay police station. When the player gets
    /// busted by police (not killed outright), they are brought here and
    /// respawned, instead of back at the safehouse. Also serves as the
    /// respawn/relocation target for an arrest.
    /// </summary>
    public class LaJolStation : MonoBehaviour
    {
        public static LaJolStation Instance { get; private set; }

        [SerializeField] private Vector3 stationSpawn;

        public Vector3 SpawnPosition => stationSpawn;

        private void Awake()
        {
            // Keep a single active instance.
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            stationSpawn = transform.position + transform.forward * -2.5f + Vector3.up * 0.2f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
