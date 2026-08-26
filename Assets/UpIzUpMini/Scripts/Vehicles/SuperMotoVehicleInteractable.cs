using UnityEngine;
using UpIzUpMini.Cameras;
using UpIzUpMini.Character;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-119 follow-up, user: "use the old mounting system you had
    /// for the TMAX. when i had to put him in the fixed position and
    /// when i could adjust the character for the wheelie position at
    /// normal riding position and at wheelie position." Replaces
    /// SuperMotoKinematicRider/SuperMotoStockInteractable's Animation-
    /// Rigging approach with the same proven system MINI-066/BikeInteractable
    /// already uses for the TMAX: VehicleRider's keyframed seated/wheelie
    /// poses (plain tunable offsets, blended by how deep the wheelie is)
    /// plus Unity's built-in humanoid goal IK via OnAnimatorIK for hands/
    /// feet - no runtime-built RigBuilder graph, no ad-hoc Animator
    /// parameter freezing (both of which kept causing save/reload-only
    /// bugs this session). VehicleRider already disables CharacterController
    /// BEFORE reparenting (the exact "bike flips into the air" bug fixed
    /// earlier this task) and plays a REAL sustained animation state
    /// ("RideBike", already baked into Sacat's shared controller for the
    /// TMAX) instead of forcing Animator parameters by hand - so the
    /// whole family of walking/bicycle/falling-animation bugs from the
    /// ad-hoc approach doesn't apply here at all.
    ///
    /// Deliberately leaner than BikeInteractable: the SuperMoto's own
    /// vendor scripts (Gadd420.RB_Controller / Input_Manager) already
    /// read the keyboard directly, so there's no throttle/steer relay to
    /// do here - just mount/dismount and feeding the wheelie blend.
    /// </summary>
    public class SuperMotoVehicleInteractable : InteractableBase
    {
        [SerializeField] private KeyCode mountKey = KeyCode.F;
        [SerializeField] private KeyCode dismountKey = KeyCode.F;
        [SerializeField] private float mountRange = 3.5f;
        [SerializeField] private float dismountSideOffset = 1.3f;

        private VehicleSeat _seat;
        private SuperMotoWheelieAssist _wheelieAssist;
        private Transform _camAnchor;

        private VehicleRider _rider;
        private GameObject _mountedPlayer;
        private int _lockedSlotIndex = -1;

        public bool HasRider => _mountedPlayer != null;

        public void Configure(VehicleSeat seat, SuperMotoWheelieAssist wheelieAssist, Transform camAnchor)
        {
            _seat = seat;
            _wheelieAssist = wheelieAssist;
            _camAnchor = camAnchor;
        }

        public override string PromptLabel =>
            HasRider ? $"[ {dismountKey} ] Get off" : $"[ {mountKey} ] Get on the bike";

        public override bool CanInteract(GameObject interactor) => !HasRider;

        public override void Interact(GameObject interactor) => Mount(interactor);

        private void Update()
        {
            if (!HasRider)
            {
                TryMountByKey();
                return;
            }

            // Feed the rider's keyframed seated<->wheelie blend from the
            // bike's OWN live wheelie progress - same technique
            // BikeInteractable already uses against TmaxBikeController's
            // WheelieAngle/MaxWheelieAngle, just against SuperMoto's own
            // SuperMotoWheelieAssist instead.
            if (_wheelieAssist != null && _rider != null)
            {
                float wheelie01 = _wheelieAssist.rampCeilingDeg > 0.01f
                    ? Mathf.Clamp01(_wheelieAssist.CurrentRampDeg / _wheelieAssist.rampCeilingDeg)
                    : 0f;
                _rider.SetWheelieBlend(wheelie01);
            }

            if (Input.GetKeyDown(dismountKey)) Dismount();
        }

        private void TryMountByKey()
        {
            if (!Input.GetKeyDown(mountKey)) return;

            var active = CharacterSwitchManager.Instance?.Active;
            if (active?.root == null) return;

            var pc = active.root.GetComponent<PlayerController>();
            if (pc != null && !pc.IsControlled) return;

            if (Vector3.Distance(active.root.transform.position, transform.position) > mountRange) return;

            Mount(active.root);
        }

        private void Mount(GameObject player)
        {
            if (HasRider || player == null || _seat == null) return;

            var rider = player.GetComponent<VehicleRider>();
            if (rider == null) rider = player.AddComponent<VehicleRider>();

            if (!rider.Mount(_seat)) return;

            _rider = rider;
            _mountedPlayer = player;

            var switcher = CharacterSwitchManager.Instance;
            if (switcher?.Slots != null)
            {
                for (int i = 0; i < switcher.Slots.Length; i++)
                {
                    if (switcher.Slots[i]?.root == player) { _lockedSlotIndex = i; switcher.SetLocked(i, true); break; }
                }
            }

            VehicleSpawnController.SetBikeInputEnabled(gameObject, true);
            RetargetGameCamera(toBike: true);
        }

        private void Dismount()
        {
            if (!HasRider) return;

            Vector3 exitPosition = transform.position + transform.right * dismountSideOffset + Vector3.up * 0.1f;
            if (Physics.Raycast(exitPosition + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f))
                exitPosition = hit.point;

            _rider?.Dismount(exitPosition);
            VehicleSpawnController.SetBikeInputEnabled(gameObject, false);

            var switcher = CharacterSwitchManager.Instance;
            if (_lockedSlotIndex >= 0 && switcher != null) switcher.SetLocked(_lockedSlotIndex, false);
            _lockedSlotIndex = -1;

            RetargetGameCamera(toBike: false);

            _mountedPlayer = null;
            _rider = null;
        }

        private void RetargetGameCamera(bool toBike)
        {
            var cam = Object.FindFirstObjectByType<ThirdPersonFollowCamera>();
            if (cam == null) return;

            if (!toBike)
            {
                var active = CharacterSwitchManager.Instance?.Active;
                if (active?.root != null) cam.SetTarget(active.root.transform);
                cam.OrbitLocked = false;
                return;
            }

            if (_camAnchor != null)
            {
                cam.SetTarget(_camAnchor);
                cam.OrbitLocked = true;
            }
        }
    }
}
