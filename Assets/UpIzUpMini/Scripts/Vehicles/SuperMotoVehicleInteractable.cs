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
        [Tooltip("MINI-119 follow-up: how close the OTHER main character must be, when the driver mounts, to hop on the pillion seat automatically - same convention as BikeInteractable's own pillionBoardRadius.")]
        [SerializeField] private float pillionBoardRadius = 8f;

        private VehicleSeat _seat;
        private VehicleSeat _pillionSeat;
        private SuperMotoWheelieAssist _wheelieAssist;
        private Transform _camAnchor;

        private VehicleRider _rider;
        private BikeRiderAnimation _riderAnim;
        private SuperMotoHandFootLock _handFootLock;
        private VehicleRider _pillion;
        private SuperMotoHandFootLock _pillionHandFootLock;
        private GameObject _mountedPlayer;
        private int _lockedSlotIndex = -1;

        public bool HasRider => _mountedPlayer != null;

        public void Configure(VehicleSeat seat, VehicleSeat pillionSeat, SuperMotoWheelieAssist wheelieAssist, Transform camAnchor)
        {
            _seat = seat;
            _pillionSeat = pillionSeat;
            _wheelieAssist = wheelieAssist;
            _camAnchor = camAnchor;
        }

        public override string PromptLabel =>
            HasRider ? $"[ {dismountKey} ] Get off" : $"[ {mountKey} ] Get on the bike";

        public override bool CanInteract(GameObject interactor) => !HasRider;

        public override void Interact(GameObject interactor) => Mount(interactor);

        /// <summary>MINI-119 follow-up, user: "i dont want to press F to
        /// mount... sacat is mounted on the bike [at Play start]."
        /// Deliberately NOT the same as walking up and pressing F -
        /// skips the range/IsControlled checks TryMountByKey enforces,
        /// since the player could be anywhere when the bike spawns.
        /// Same naming convention as the project's existing
        /// DevAutoPossessSuperMotoOnSpawn/DevForceMount pattern for this
        /// exact "just put them on it, unconditionally" case.</summary>
        public void DevForceMount(GameObject player) => Mount(player);

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

                // MINI-119 follow-up, user: "the hands on the handlebar
                // looks out of position since the character doesnt
                // move." VehicleRider's own blend above only translates/
                // pitches the WHOLE body - it never reshapes the arms.
                // This is the piece that actually does: the same
                // authored "BikeWheelie" pose overlay TMAX riders get,
                // blended in on top of the seated pose via
                // HumanoidAnimationManager's dedicated FullBodyBlend
                // layer - genuinely bending the arms/torso for a
                // wheelie, which gives the hand IK a much closer
                // starting point to reach the real handlebar anchors
                // from instead of stretching from an unchanged seated
                // pose.
                if (_riderAnim != null) _riderAnim.UpdateWheelieOverlay(wheelie01 > 0.05f);
            }

            // MINI-119 follow-up fix, user: "if the pillion rider is
            // following." A one-shot check at the exact moment of
            // mounting (what BikeInteractable itself does for the TMAX)
            // only works there because the player always walks up and
            // presses F, by which point the other character is already
            // nearby from normal following. AutoMountSuperMotoOnSpawn
            // mounts the driver essentially instantly, with no such
            // walk-up window, so the passenger is very likely still out
            // of range at that exact instant no matter how close they
            // eventually get. Checked every frame instead, while
            // mounted and the seat is still empty, so boarding happens
            // the moment the passenger actually arrives.
            if (_pillion == null) BoardPillion();

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
            _riderAnim = player.GetComponent<BikeRiderAnimation>();
            if (_riderAnim == null) _riderAnim = player.AddComponent<BikeRiderAnimation>();

            // MINI-119 follow-up, user: hands/feet still weren't
            // tracking through a wheelie via VehicleRider's own
            // OnAnimatorIK - see SuperMotoHandFootLock's own header for
            // the full reasoning. Layered on top, not replacing
            // VehicleRider - it still owns seating/mounting/the ride
            // animation.
            _handFootLock = player.GetComponent<SuperMotoHandFootLock>();
            if (_handFootLock == null) _handFootLock = player.AddComponent<SuperMotoHandFootLock>();
            _handFootLock.Attach(player.GetComponentInChildren<Animator>(),
                _seat.RightHandTarget, _seat.LeftHandTarget, _seat.RightFootTarget, _seat.LeftFootTarget);

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

        /// <summary>MINI-119 follow-up, user: "i want pillion rider to
        /// hop at the back... if the pillion rider is following."
        /// Verbatim the same logic as BikeInteractable.BoardPillion -
        /// the OTHER main character hops onto the pillion seat
        /// automatically when the driver mounts, if they're nearby and
        /// not off doing something else (locked away, e.g. a Guadeloupe
        /// run) or already mounted on something.</summary>
        private void BoardPillion()
        {
            if (_pillionSeat == null || _pillionSeat.IsOccupied) return;

            var switcher = CharacterSwitchManager.Instance;
            if (switcher?.Slots == null || switcher.Slots.Length < 2) return;

            int otherIndex = 1 - switcher.ActiveIndex;
            if (otherIndex < 0 || otherIndex >= switcher.Slots.Length) return;
            if (switcher.IsLocked(otherIndex)) return;

            var otherSlot = switcher.Slots[otherIndex];
            if (otherSlot?.root == null) return;

            if (Vector3.Distance(otherSlot.root.transform.position, transform.position) > pillionBoardRadius) return;

            var passenger = otherSlot.root.GetComponent<VehicleRider>();
            if (passenger == null) passenger = otherSlot.root.AddComponent<VehicleRider>();
            if (passenger.IsMounted) return;

            if (!passenger.Mount(_pillionSeat)) return;
            _pillion = passenger;

            // MINI-119 follow-up, user: "rig his hands to his side."
            // Same continuous, direct bone-override technique already
            // proven for the driver - the pillion's own hand targets
            // (PillionLeftHandPos/PillionRightHandPos, positioned at his
            // sides) pull his hands away from wherever the reused
            // "RideBike" pose naturally puts them (reaching forward for
            // a handlebar he doesn't have). No foot targets - left null,
            // same as the driver's setup gracefully skips a null goal.
            _pillionHandFootLock = otherSlot.root.GetComponent<SuperMotoHandFootLock>();
            if (_pillionHandFootLock == null) _pillionHandFootLock = otherSlot.root.AddComponent<SuperMotoHandFootLock>();
            _pillionHandFootLock.Attach(otherSlot.root.GetComponentInChildren<Animator>(),
                _pillionSeat.RightHandTarget, _pillionSeat.LeftHandTarget, null, null);
        }

        private void DismountPillion()
        {
            if (_pillion == null) return;

            Vector3 exit = transform.position - transform.right * dismountSideOffset + Vector3.up * 0.1f;
            _pillion.Dismount(exit);
            _pillion = null;
            _pillionHandFootLock?.Detach();
            _pillionHandFootLock = null;
        }

        private void Dismount()
        {
            if (!HasRider) return;

            Vector3 exitPosition = transform.position + transform.right * dismountSideOffset + Vector3.up * 0.1f;
            if (Physics.Raycast(exitPosition + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f))
                exitPosition = hit.point;

            _rider?.Dismount(exitPosition);
            _riderAnim?.ClearPose();
            _handFootLock?.Detach();
            DismountPillion();
            VehicleSpawnController.SetBikeInputEnabled(gameObject, false);

            var switcher = CharacterSwitchManager.Instance;
            if (_lockedSlotIndex >= 0 && switcher != null) switcher.SetLocked(_lockedSlotIndex, false);
            _lockedSlotIndex = -1;

            RetargetGameCamera(toBike: false);

            _mountedPlayer = null;
            _rider = null;
            _riderAnim = null;
            _handFootLock = null;
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
