using Gadd420;
using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-119 follow-up, user: "f respawns the bike only on crash but i
    /// want it to respawn anytime." The stock Gadd420.KeyBoardShortCuts
    /// gates its own F handler on `rbController.isCrashed` - this
    /// replaces that component's F handling entirely so the reset works
    /// whenever pressed, same underlying recovery steps
    /// (TmaxBikeController.ResetUpright already proved this exact recipe
    /// for the mapped bike; mirrored here since this is the raw stock rig
    /// with no TmaxBikeController facade in the loop): reposition upright
    /// in place, zero both velocities, re-arm the ragdoll via
    /// RagdollManager.resetRider (which itself clears isCrashed and puts
    /// the rider's bones back to kinematic/trigger). R is kept for a full
    /// scene reload, matching the stock KeyBoardShortCuts convention this
    /// replaces.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(RB_Controller))]
    public class SuperMotoAnytimeReset : MonoBehaviour
    {
        [SerializeField] private KeyCode resetKey = KeyCode.F;
        [SerializeField] private KeyCode reloadSceneKey = KeyCode.R;

        private Rigidbody _rb;
        private RB_Controller _gadd;
        private RagdollManager _ragdoll;
        private CrashController _crash;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _gadd = GetComponent<RB_Controller>();
            _ragdoll = GetComponentInChildren<RagdollManager>(true);
            _crash = GetComponent<CrashController>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(resetKey)) ResetUpright();
            if (Input.GetKeyDown(reloadSceneKey))
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        /// <summary>MINI-119 follow-up, user: "so why cant the spawn be
        /// [like] the F behaviour." Exactly - made public so
        /// VehicleSpawnController can call this once, immediately after
        /// spawning, as a guaranteed clean start regardless of any
        /// remaining spawn-placement edge case, on top of (not instead
        /// of) placing it correctly in the first place.</summary>
        public void ResetUpright()
        {
            if (_rb == null) return;

            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(
                transform.position + Vector3.up * 1f,
                Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
            Physics.SyncTransforms();

            if (_ragdoll != null) _ragdoll.resetRider = true;
            if (_gadd != null) _gadd.isCrashed = false;
            if (_crash != null) { _crash.rbSpeed = 0f; _crash.lateRbSpeed = 0f; }
        }
    }
}
