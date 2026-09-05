using UnityEngine;
using UpIzUpMini.Cameras;
using UpIzUpMini.Character;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-066. The "get on the bike" world interaction, and the thing that
    /// drives the bike while someone is on it.
    ///
    /// Deliberately built on the project's existing InteractableBase/
    /// InteractionDetector prompt system rather than a bespoke key check, so
    /// the bike shows the same [ E ] prompt as every shop, safehouse and NPC
    /// and needs no new input plumbing.
    ///
    /// Bike entry is intentionally NOT modelled as generic "enter vehicle" -
    /// per the user's explicit note that bike and car entry differ, the seat
    /// itself carries an EntryStyle (see VehicleSeat) and the rider straddles
    /// rather than opening a door. When cars arrive they get their own style
    /// and their own mount clip without touching this file's control logic.
    ///
    /// While mounted this component reads the same movement axes the on-foot
    /// controller uses and feeds them into TmaxBikeControllerCustom.SetInput, which
    /// is exactly the decoupling that controller was built for (it never
    /// reads Input.* itself, so mobile buttons or AI can drive it later
    /// without changes here).
    /// </summary>
    [RequireComponent(typeof(TmaxBikeControllerCustom))]
    public class BikeInteractable : InteractableBase
    {
        private const float RiderLeanFollowSafetyLimit = 0.52f;
        private const float RiderMaxLeanSafetyLimit = 9f;
        public const float SuperMotoWheelieClipWeight = 0.22f;

        [SerializeField] private VehicleSeat driverSeat;
        [SerializeField] private VehicleSeat pillionSeat;

        [Tooltip("Where a rider is placed when they get off - to the left of the bike, clear of it, so they don't dismount inside the collider.")]
        [SerializeField] private float dismountSideOffset = 1.1f;

        // Set to 0 per the user's final call: "for the wheelie, can you make
        // the character move in the same angle of the bike at the same
        // speed". Any non-zero value rotates the rider AGAINST the bike, and
        // at a 90-degree wheelie that read as the rider peeling off the back
        // rather than riding it. Zero means the rider is carried by the seat
        // transform alone - exactly the bike's angle, exactly its rate, no
        // lag. Kept as a field rather than deleted because the earlier
        // "goes too much back" complaint may return once the wheelie's own
        // maximum angle is retuned.
        // Back to 0 after seeing it in Play Mode. Any counter-rotation makes
        // the rider swing away from the bike about his own pinned hands - a
        // pendulum off the handlebars - which is exactly the "character is
        // back", off-the-seat look in the user's wheelie screenshot. At 0 he
        // simply rotates WITH the bike, staying planted on the seat in the
        // same posture he rides in, which is what was actually asked for:
        // "put the character in the first position when the bike was idle
        // and use this one where he is wheeling".
        [Tooltip("How much of the bike's wheelie pitch is cancelled out. 0 = rider stays seated and rotates with the bike (current setting). Above 0 he leans against the bike's rotation and will lift off the seat.")]
        [Range(0f, 1f)][SerializeField] private float wheelieCounterLean = 0f;
        [Tooltip("How closely the rider tracks the bike's LIVE wheelie angle as it rises and falls. 1 = moves in sync with the bike (and so with the throttle/R); below 1 he only partly follows it up; above 1 he exaggerates it. Pair with CHARACTER rise time - this sets how FAR he follows, that sets how QUICKLY.")]
        [Range(0f, 2f)][SerializeField] private float wheelieFollowAmount = 1f;


        [Tooltip("How much of the bike body's cosmetic lean the rider copies. 1 = leans exactly with the bike (they stay visually attached); 0 = rider stays upright while the bike leans away from him.")]
        [Range(0f, 1.2f)][SerializeField] private float riderLeanFollow = 0.81f;
        [Tooltip("Extra lean given to the RIDER at full steering, on top of whatever he copies from the bike. Lets the character lean more (or less) than the machine does - a separate control from riderLeanFollow, which only scales the bike's own lean.")]
        [SerializeField] private float riderExtraLeanDegrees = 0f;

        [Tooltip("MINI-072: hard ceiling on how far the RIDER leans, in degrees, whatever the bike underneath is doing. The user's own figure - \"make it subtle like a 15 degree angle\". A ceiling rather than a smaller follow factor, so the rider still tracks the bike's lean naturally at normal cornering and only stops short at the extremes.")]
        [Range(0f, 60f)][SerializeField] private float riderMaxLeanDegrees = 15f;


        [Tooltip("Degrees of counter-roll applied at full steering, trimming how far the lean clips tip the rider. Positive values reduce the visible lean.")]
        [SerializeField] private float leanTrimDegrees = 3.5f;

        [Tooltip("Hand IK multiplier while wheelieing - the wheelie clip lifts an arm off the bars, so the grip needs forcing past its normal strength.")]
        [SerializeField] private float wheelieHandGrip = 1.6f;

        // MINI-140 control scheme, set by the user directly: "E for mount/enter
        // vehicles, Q for wheelie". Mounting now uses the SAME key as every
        // other world interaction (E / GameAction.Interact) instead of a
        // bespoke F check - you walk up and press E to get on, press E again to
        // get off. F goes back to being purely the melee Attack key. The
        // wheelie moves off E (which is now the get-on/get-off key) onto Q.
        // Q also calls the partner/gang on the phone when ON FOOT - that path
        // is suppressed while mounted (see CellPhoneController, MINI-140) so a
        // Q tap mid-wheelie doesn't also summon anyone.
        //
        // Superseded history for context: MINI-069 had F=mount / E=wheelie;
        // before that "bike will be F" / "wheelie would have to be R". The
        // look-left/look-right fields that once lived here were deleted long
        // ago (declared, never read).
        [SerializeField] private KeyCode wheelieKey = KeyCode.Q;
        [SerializeField] private KeyCode dismountKey = KeyCode.E;
        [Tooltip("MINI-140: same key as dismount and as the world interact key - E is simply 'get on / get off'. The interactable/prompt still announces the bike when you walk up; this key check just keeps the bike's own wider mount range.")]
        [SerializeField] private KeyCode mountKey = KeyCode.E;
        [Tooltip("How close the active character must be for the mount key to work.")]
        [SerializeField] private float mountRange = 3.2f;
        private const float MinimumMountRange = 4.25f;

        [Tooltip("MINI-073: how close the other main character must be, when the driver mounts, to hop on the pillion seat automatically.")]
        [SerializeField] private float pillionBoardRadius = 8f;

        private TmaxBikeControllerCustom _bike;
        private Rigidbody _body;
        private bool _parkingLockLogged;
        private VehicleRider _driver;
        private VehicleRider _pillion;
        private BikeRiderAnimation _driverAnim;
        private float _savedWheelieClipWeight = -1f;
        private BikeCameraAnchor _camAnchor;
        // MINI-140: E now mounts AND dismounts. Record the frame a mount
        // happened so the same key-down that got the rider on cannot be read
        // again as a dismount later in that same frame (script execution order
        // between InteractionDetector and this component is not fixed).
        private int _mountedFrame = -1;
        public float RiderLeanFollow { get => riderLeanFollow; set => riderLeanFollow = value; }
        public float RiderExtraLean { get => riderExtraLeanDegrees; set => riderExtraLeanDegrees = value; }
        public float RiderMaxLean { get => riderMaxLeanDegrees; set => riderMaxLeanDegrees = value; }
        public float WheelieFollowAmount { get => wheelieFollowAmount; set => wheelieFollowAmount = value; }
        public float RiderSeatPitch { get => leanTrimDegrees; set => leanTrimDegrees = value; }


        public bool HasRider => _driver != null;

        private void Awake()
        {
            _bike = GetComponent<TmaxBikeControllerCustom>();
            _body = GetComponent<Rigidbody>();
            VehicleImpactResponder.Ensure(gameObject);
            VehicleDamageController.Ensure(gameObject);
            BikeCrashEjectionController.Ensure(gameObject);
            if (driverSeat == null || pillionSeat == null)
            {
                // Seats are wired at build time (Mini065TmaxPhysicsTest.
                // WireSeat) but fall back to a search so a hand-placed bike
                // in a scene still works.
                foreach (var s in GetComponentsInChildren<VehicleSeat>(true))
                {
                    if (s.Role == VehicleSeat.SeatRole.Driver && driverSeat == null) driverSeat = s;
                    else if (s.Role == VehicleSeat.SeatRole.Passenger && pillionSeat == null) pillionSeat = s;
                }
            }
        }

        public override string PromptLabel =>
            HasRider ? $"[ {dismountKey} ] Get off" : $"[ {mountKey} ] Get on the TNAX";

        public override bool CanInteract(GameObject interactor) => !HasRider;

        public override void Interact(GameObject interactor)
        {
            if (HasRider || driverSeat == null || interactor == null) return;

            // Release the empty-bike parking lock immediately before the
            // rider takes control. Kinematic parking is stronger and more
            // reliable than WheelCollider brake torque on Lalay's incline.
            if (_body != null && _body.isKinematic)
            {
                _body.isKinematic = false;
                _body.WakeUp();
            }

            var rider = interactor.GetComponent<VehicleRider>();
            if (rider == null) rider = interactor.AddComponent<VehicleRider>();

            if (rider.Mount(driverSeat))
            {
                _driver = rider;
                _mountedFrame = Time.frameCount;
                _driverAnim = interactor.GetComponent<BikeRiderAnimation>();
                if (_driverAnim == null) _driverAnim = interactor.AddComponent<BikeRiderAnimation>();
                // Match the exact authored lift pose used by the original SuperMoto rider.
                _savedWheelieClipWeight = _driverAnim.WheelieClipWeight;
                _driverAnim.WheelieClipWeight = SuperMotoWheelieClipWeight;
                RetargetGameCamera(toBike: true);

                SetCompanionFollowing(false);
                BoardPillion();
            }
        }

        /// <summary>
        /// MINI-073: "the second rider coming at the back of the bike" - the
        /// OTHER main character (not the one driving) hops on the pillion seat
        /// automatically when the driver mounts, if they are nearby and not
        /// off doing something else (a Guadeloupe run) or already mounted.
        ///
        /// Deliberately does NOT run them through BikeRiderAnimation - that
        /// class exists to decide the DRIVER's pose from the bike's throttle/
        /// wheelie/lean state, none of which the passenger has any say over.
        /// The pillion just holds one sustained pose for as long as they ride.
        /// </summary>
        private void BoardPillion()
        {
            if (pillionSeat == null || pillionSeat.IsOccupied) return;

            var switcher = CharacterSwitchManager.Instance;
            if (switcher?.Slots == null || switcher.Slots.Length < 2) return;

            int otherIndex = 1 - switcher.ActiveIndex;
            if (otherIndex < 0 || otherIndex >= switcher.Slots.Length) return;
            if (switcher.IsLocked(otherIndex)) return;   // away on the Guadeloupe run

            var otherSlot = switcher.Slots[otherIndex];
            if (otherSlot?.root == null) return;

            // Only board if they are actually nearby - normally true since they
            // follow the active character everywhere, but this stops someone
            // left behind (e.g. mid farm-hand work) from teleporting onto the
            // back of the bike from across the map.
            if (Vector3.Distance(otherSlot.root.transform.position, transform.position) > pillionBoardRadius) return;

            var passenger = otherSlot.root.GetComponent<VehicleRider>();
            if (passenger == null) passenger = otherSlot.root.AddComponent<VehicleRider>();
            if (passenger.IsMounted) return;

            // Mount() itself plays the seat's own configured RidePoseActionId
            // ("RidePillion" - see Mini065TmaxPhysicsTest.WireSeat), so no
            // separate pose call is needed here.
            if (!passenger.Mount(pillionSeat)) return;
            _pillion = passenger;
        }

        private void DismountPillion()
        {
            if (_pillion == null) return;

            // Dismount() itself ends the held pose and hands control back -
            // symmetric with what Mount() already does on its own.
            Vector3 exit = transform.position
                           + transform.right * dismountSideOffset
                           + Vector3.up * 0.1f;
            _pillion.Dismount(exit);
            _pillion = null;
        }

        public override string GetInteractionFeedback() =>
            HasRider ? $"Yuh riding now - {dismountKey} to get off." : null;

        /// <summary>
        /// True on the frame the bike was mounted with the mount key, so the
        /// player's own melee swing can skip that frame - F is also the punch
        /// key, and without this you throw a punch every time you get on.
        /// </summary>
        public static bool ConsumedMountKeyThisFrame { get; private set; }

        private void LateUpdate() => ConsumedMountKeyThisFrame = false;

        /// <summary>
        /// Mounting is driven from here rather than through InteractionDetector
        /// because the user wants F, while E remains the world's general
        /// interact key. The interactable/prompt is kept so the bike still
        /// announces itself when you walk up to it.
        /// </summary>
        private void TryMountByKey()
        {
            if (HasRider || driverSeat == null) return;
            if (!Input.GetKeyDown(mountKey)) return;

            var active = CharacterSwitchManager.Instance?.Active;
            if (active?.root == null) return;

            // MINI-076: same fix as CarInteractable.TryEnter() - the parked
            // TMAX and parked Range Rover sit close enough (~5.6m, at the
            // farm safehouse) that their mount ranges overlap, so one F press
            // could fire both mount paths on the same character in the same
            // frame. IsControlled is false the instant either vehicle takes
            // control, so this is a correct general "already busy" guard.
            var pc = active.root.GetComponent<PlayerController>();
            if (pc != null && !pc.IsControlled) return;

            if (Vector3.Distance(active.root.transform.position, transform.position) > Mathf.Max(mountRange, MinimumMountRange)) return;

            ConsumedMountKeyThisFrame = true;
            Interact(active.root);
        }

        /// <summary>
        /// MINI-077: "if the vehicle is full he can take the bike to follow" -
        /// called from CarInteractable when the OTHER main character has
        /// nowhere left to sit in a full car. Same eligibility rules as the
        /// player's own F-to-mount (not already riding, in range), just for
        /// an arbitrary character rather than whoever is currently active -
        /// this lets the currently-INACTIVE character mount the bike, which
        /// the normal key-driven path can never do (it only ever reads the
        /// active slot).
        /// </summary>
        public bool TryMountFor(GameObject character)
        {
            if (HasRider || driverSeat == null || character == null) return false;

            var pc = character.GetComponent<PlayerController>();
            if (pc != null && !pc.IsControlled) return false;

            if (Vector3.Distance(character.transform.position, transform.position) > Mathf.Max(mountRange, MinimumMountRange)) return false;

            Interact(character);
            return HasRider;
        }

        private void Update()
        {
            TryMountByKey();

            if (!HasRider)
            {
                // MINI-128: the temporary market-road TMAX could free-roll
                // down Lalay before the player reached it, making a successful
                // spawn look like no spawn at all. Treat an empty bike as
                // parked: brake both wheels and clear any stale wheelie input.
                _bike?.SetInput(0f, 0f, 1f);
                _bike?.SetWheelieHeld(false);
                if (_body != null && !_body.isKinematic)
                {
                    _body.linearVelocity = Vector3.zero;
                    _body.angularVelocity = Vector3.zero;
                    _body.isKinematic = true;
                }
                if (!_parkingLockLogged)
                {
                    _parkingLockLogged = true;
                    Debug.Log($"MINI-128 TMAX PARKED: Empty bike locked at {transform.position} until F mounts it.");
                }
                return;
            }

            // Same axes the on-foot controller reads, so riding feels
            // continuous with walking rather than a different control scheme.
            float throttle = Input.GetAxis("Vertical");
            float steer = Input.GetAxis("Horizontal");
            float brake = Input.GetKey(KeyCode.Space) ? 1f : 0f;

            _bike.SetInput(throttle, steer, brake);
            _bike.SetWheelieHeld(Input.GetKey(wheelieKey));

            // Rider pose follows the bike's real state (speed, wheelie, lean)
            // rather than being driven from input directly, so the animation
            // matches what the bike is actually doing.
            if (_driverAnim != null)
                _driverAnim.UpdatePose(_bike, steer);

            // Counter-lean during a wheelie. The rider is parented to the
            // bike, so a 90-degree wheelie carries them 90 degrees back on
            // its own - and the wheelie CLIP leans them back further still,
            // which compounded into the user's "the wheeling goes too much
            // back". Leaning them forward by a fraction of the bike's own
            // pitch keeps them nearer upright in world space, which is also
            // what a real rider does to counterbalance.
            if (_driver != null)
            {
                // Keyframed seating: feed the rider how deep the wheelie is
                // and let it blend between its two authored poses. Measured
                // against the bike's own max angle so the blend is complete
                // exactly when the wheelie is, and - because this is the LIVE
                // angle - the return to the seated pose happens automatically
                // as the bike comes down, with no separate reverse logic to
                // fall out of sync.
                float wheelie01 = _bike.MaxWheelieAngle > 0.01f
                    ? Mathf.Clamp01(_bike.WheelieAngle / _bike.MaxWheelieAngle)
                    : 0f;
                _driver.SetWheelieBlend(wheelie01 * wheelieFollowAmount);

                // Left available but normally 0 - the keyframes carry the
                // lean themselves now.
                _driver.SetExtraPitch(-_bike.CurrentPitchAngle * wheelieCounterLean);

                // Roll the rider WITH the bike body.


                //


                // The rider sits on Seat, which is a SIBLING of VisualLeanRoot, not a


                // child - so the bike body leans into a corner while the rider stays


                // bolt upright, and his legs and hands visibly come away from the


                // bike (exactly what the user reported in-game). Copying the body.s


                // own lean onto the rider keeps them together. Deliberately NOT fixed


                // by reparenting Seat under VisualLeanRoot: the seat anchor is also


                // what the IK targets and keyframes are measured against, and putting


                // it under a cosmetically-rotating transform would make every one of


                // those measurements lean too.


                // MINI-131: the authored lean clip already adds a visible
                // torso swing. Following 81% of a 15-degree bike lean and
                // then adding that clip compounded into the rider sweeping
                // too far across the scooter. Preserve prefab tuning as an
                // upper request, but apply a restrained runtime safety limit.
                float effectiveLeanFollow = Mathf.Min(riderLeanFollow, RiderLeanFollowSafetyLimit);
                float effectiveMaxLean = Mathf.Min(riderMaxLeanDegrees, RiderMaxLeanSafetyLimit);
                float riderRoll =
                    (_bike.CurrentVisualLean * effectiveLeanFollow)
                    + (_driverAnim != null ? _driverAnim.SmoothedSteer * riderExtraLeanDegrees : 0f);
                // Clamped, not scaled down: the rider keeps tracking the bike
                // one-for-one through ordinary cornering and simply stops short
                // of the extreme angles that read as him falling off.
                _driver.SetExtraRoll(Mathf.Clamp(riderRoll, -effectiveMaxLean, effectiveMaxLean));

                // Hands locked to the grips for the WHOLE wheelie, including
                // the way back down ("make it stay there until the wheelie is
                // done"). Threshold is deliberately low (1 degree, not 5) so
                // the grip is already firm before the front starts to rise
                // and does not release until the bike is genuinely back down.
                bool wheelieing = _bike.WheelieAngle > 1f || !_bike.IsFrontWheelGrounded;
                _driver.SetHandIkBoost(wheelieing ? wheelieHandGrip : 1f);
            }

            // MINI-077: "the pillion is leaning way to back" - mirrors the
            // DRIVER's own counter-pitch/roll treatment above verbatim, per
            // the user's own diagnosis ("couldnt you mimic the pillion pitch
            // degree because the animation is from the same pack"). Both
            // riders are from the same mocap pack and sit on the same bike
            // body, so applying the identical numbers is not just convenient,
            // it is literally what "bend in sync" means - the SAME computed
            // angle at the SAME instant, not two independently-tuned values
            // that happen to look similar. Deliberately does NOT get
            // SetWheelieBlend (the pillion has no separate seated<->wheelie
            // pose to blend between the way the driver does) - just the
            // counter-pitch and the clamped roll, which is what actually
            // caused the "leaning way too back" complaint.
            if (_pillion != null)
            {
                _pillion.SetExtraPitch(-_bike.CurrentPitchAngle * wheelieCounterLean);

                float effectiveLeanFollow = Mathf.Min(riderLeanFollow, RiderLeanFollowSafetyLimit);
                float effectiveMaxLean = Mathf.Min(riderMaxLeanDegrees, RiderMaxLeanSafetyLimit);
                float pillionRoll =
                    (_bike.CurrentVisualLean * effectiveLeanFollow)
                    + (_driverAnim != null ? _driverAnim.SmoothedSteer * riderExtraLeanDegrees : 0f);
                _pillion.SetExtraRoll(Mathf.Clamp(pillionRoll, -effectiveMaxLean, effectiveMaxLean));
            }

            // Guard the mount frame: the E press that just mounted must not
            // also be read as a dismount (see _mountedFrame).
            if (Input.GetKeyDown(dismountKey) && Time.frameCount != _mountedFrame)
                DismountDriver();
        }

        /// <summary>
        /// Points the real game's chase camera at the bike while riding, and
        /// back at the rider when they get off (user: "the camera should be
        /// at the back of the bike in the real game").
        ///
        /// It deliberately does NOT follow the bike transform directly, nor
        /// the rider - both pitch up to 90 degrees during a wheelie, which
        /// would roll the whole camera over with them. Instead a small anchor
        /// tracks the bike's POSITION and HEADING only, staying level, so the
        /// camera sits steadily behind the bike throughout. That also keeps
        /// the wheelie framed from behind rather than side-on, which is the
        /// angle its remaining rough edges show from.
        /// </summary>
        private void RetargetGameCamera(bool toBike)
        {
            var cam = Object.FindFirstObjectByType<ThirdPersonFollowCamera>();
            if (cam == null) return; // test scene uses its own camera

            if (!toBike)
            {
                var active = CharacterSwitchManager.Instance?.Active;
                if (active?.root != null) cam.SetTarget(active.root.transform);
                cam.OrbitLocked = false;
                if (_camAnchor != null) Destroy(_camAnchor.gameObject);
                _camAnchor = null;
                return;
            }

            if (_camAnchor == null)
            {
                var go = new GameObject("BikeCameraAnchor");
                _camAnchor = go.AddComponent<BikeCameraAnchor>();
            }
            _camAnchor.Follow(transform);
            cam.SetTarget(_camAnchor.transform);
            cam.OrbitLocked = true;
        }


        /// <summary>
        /// Stops the companion trying to follow while the player is on the
        /// bike, and lets them resume on dismount.
        ///
        /// A companion on foot cannot keep up with a 60km/h bike, so it ends
        /// up permanently sprinting and hitting its own stuck-recovery, which
        /// both looks wrong and costs frames - the user suspected exactly this
        /// ("not sure if its because the other character is following"). They
        /// simply wait where they are instead; carrying a passenger is the
        /// pillion seat's job, not the follower's.
        /// </summary>
        private void SetCompanionFollowing(bool following)
        {
            var mgr = CharacterSwitchManager.Instance;
            if (mgr?.Slots == null) return;

            foreach (var slot in mgr.Slots)
            {
                if (slot?.followController == null) continue;
                if (slot.root == _driver?.gameObject) continue; // the rider itself
                slot.followController.FollowingEnabled = following;
            }
        }

        public bool CrashEject(Vector3 impactVelocity, Vector3 impactPoint)
        {
            if (_driver == null) return false;

            GameObject driverObject = _driver.gameObject;
            GameObject pillionObject = _pillion != null ? _pillion.gameObject : null;
            DismountDriver();

            PlayerCrashRagdoll.Trigger(driverObject, impactVelocity, impactPoint);
            if (pillionObject != null)
                PlayerCrashRagdoll.Trigger(
                    pillionObject,
                    impactVelocity * 0.85f + transform.right * 0.7f,
                    impactPoint - transform.forward * 0.45f);
            return true;
        }
        private void DismountDriver()
        {
            if (_driver == null) return;

            // Step off to the left, level with the seat, and let gravity/the
            // CharacterController settle them - putting them at the bike's
            // own origin would drop them inside its collider.
            Vector3 exit = transform.position
                           - transform.right * dismountSideOffset
                           + Vector3.up * 0.1f;

            // Cut the throttle before handing control back, otherwise the
            // bike keeps whatever input was held at the moment of dismount.
            _bike.SetInput(0f, 0f, 1f);
            _bike.SetWheelieHeld(false);

            if (_driverAnim != null)
            {
                _driverAnim.ClearPose();
                if (_savedWheelieClipWeight >= 0f)
                    _driverAnim.WheelieClipWeight = _savedWheelieClipWeight;
                _savedWheelieClipWeight = -1f;
                _driverAnim = null;
            }

            _driver.Dismount(exit);
            _driver = null;

            DismountPillion();

            // Hand the camera back to the character before clearing state,
            // so it doesn't spend a frame following a destroyed anchor.
            SetCompanionFollowing(true);

            RetargetGameCamera(toBike: false);
        }
    }
}
