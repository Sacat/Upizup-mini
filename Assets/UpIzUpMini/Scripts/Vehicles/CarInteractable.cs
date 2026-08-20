using UnityEngine;
using UpIzUpMini.Cameras;
using UpIzUpMini.Character;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-071. Getting in and out of a car, and driving it.
    ///
    /// Car entry is deliberately NOT the bike's - the user flagged that early
    /// ("remember bike entry and car entry is different okay"), and VehicleSeat
    /// has carried a `DoorSeated` style since MINI-066 waiting for this. The
    /// difference that matters here: you walk to the DRIVER'S DOOR rather than
    /// anywhere near the vehicle, and you end up seated inside rather than
    /// straddling. There is no door-opening animation - the project has no clip
    /// for it, and inventing one was explicitly out of scope.
    ///
    /// Kept separate from BikeInteractable rather than shared: the bike's
    /// version carries wheelie input, rider lean sync and IK grip handling that
    /// a car has no use for.
    /// </summary>
    // MINI-078: without this, a second instance can be added silently -
    // exactly what happened when CarRadioController's [RequireComponent]
    // auto-added a blank one before the real, wired one existed. Now a
    // second AddComponent<CarInteractable>() is refused outright, so any
    // future ordering mistake fails loudly in the Console instead of
    // quietly duplicating the whole mount/drive/passenger system.
    [DisallowMultipleComponent]
    public class CarInteractable : InteractableBase
    {
        [SerializeField] private CarController car;
        [SerializeField] private Transform driverDoor;
        [SerializeField] private Transform seatAnchor;
        [SerializeField] private Transform cameraTarget;

        [Tooltip("MINI-073: passenger seats. Any recruited Not Ah Word member on Follow, within boardRadius when the driver gets in, is seated here automatically - 'the recruited members should be able to go in the vehicle'.")]
        [SerializeField] private Transform[] passengerSeats = new Transform[0];
        [SerializeField] private float boardRadius = 9f;

        [Tooltip("How close to the DRIVER'S DOOR the player must be. Measured at the door rather than the car body, so you cannot climb in through the boot.")]
        [SerializeField] private float doorRange = 2.6f;
        [SerializeField] private KeyCode enterKey = KeyCode.F;
        [SerializeField] private KeyCode exitKey = KeyCode.F;

        [Tooltip("Held pose while seated. The pack has no car-sitting clip, so the bike's upright seated pose stands in - it reads acceptably through glass.")]
        [SerializeField] private string seatedPoseId = "BikeStopped";

        [Tooltip("MINI-075: while true, driver and passengers are simply made invisible for the ride instead of being posed inside the car. TEMPORARY WORKAROUND per the user - the seat anchor is placing riders sitting ON TOP of the roof instead of inside, and passengers were not boarding visibly either. Does NOT touch the underlying seating/parenting/IK system at all (left fully intact for later - see VehicleSeat/VehicleRider-style work whenever the real seat placement is revisited); this only hides the renderers of whoever is already being parented/seated by the existing code, so the illusion is 'they got in' rather than 'a character is glued to the car roof'.")]
        [SerializeField] private bool hideRidersInsteadOfPosing = true;

        [Tooltip("MINI-077: how close the OTHER main character (not the one driving) must be, while this car is being driven, to auto-board it - or take the bike instead if the car is full.")]
        [SerializeField] private float otherMainBoardRadius = 8f;

        private GameObject _driver;
        private CharacterController _driverCc;
        private PlayerController _driverPc;
        private HumanoidAnimationManager _driverAnim;
        private BikeCameraAnchor _camAnchor;
        private Transform _driverOriginalParent;
        private readonly System.Collections.Generic.List<Character.GangMemberController> _passengers =
            new System.Collections.Generic.List<Character.GangMemberController>();

        // MINI-077: "the other character still cannot going inside the
        // vehivce... once he is close and the main character is in then he
        // goes in." Tracked separately from _passengers (which is
        // GangMemberController-typed, recruits only) since the other main
        // character is a normal PlayerController-driven root, not a recruit.
        private GameObject _otherMain;
        private CharacterController _otherMainCc;
        private HumanoidAnimationManager _otherMainAnim;
        private Transform _otherMainSeat;

        public bool HasDriver => _driver != null;

        /// <summary>MINI-073: the car the player is CURRENTLY DRIVING, if any -
        /// lets other systems (the phone call) find it without a scene search.
        /// Null whenever nobody is driving.</summary>
        public static CarInteractable ActiveDriven { get; private set; }

        private void Awake()
        {
            if (car == null) car = GetComponent<CarController>();
        }

        public override string PromptLabel =>
            HasDriver ? $"[ {exitKey} ] Get out" : $"[ {enterKey} ] Get in";

        public override bool CanInteract(GameObject interactor) => !HasDriver;

        public override void Interact(GameObject interactor) => Enter(interactor);

        // MINI-074: told once on getting in, same pattern as the bike's own
        // "F to get off" feedback line - a driving control never gets a
        // persistent on-screen hint elsewhere in this project.
        public override string GetInteractionFeedback() =>
            HasDriver ? $"Yuh driving now - {exitKey} to get out. [ M ] Radio." : null;

        private void Update()
        {
            if (!HasDriver)
            {
                TryEnter();
                return;
            }

            if (Input.GetKeyDown(exitKey)) { Exit(); return; }

            TryBoardOtherMainCharacter();

            float throttle = (Input.GetKey(KeyCode.W) ? 1f : 0f) + (Input.GetKey(KeyCode.S) ? -1f : 0f);
            float steer = (Input.GetKey(KeyCode.D) ? 1f : 0f) + (Input.GetKey(KeyCode.A) ? -1f : 0f);
            float brake = Input.GetKey(KeyCode.Space) ? 1f : 0f;
            car?.SetInput(throttle, steer, brake);
        }

        private void TryEnter()
        {
            if (!Input.GetKeyDown(enterKey)) return;

            var active = CharacterSwitchManager.Instance?.Active;
            if (active?.root == null) return;

            // MINI-076: "my main characters cannot get on the Tnax... other
            // character cannot get on the range rover" - traced to the
            // parked TMAX and parked Range Rover sitting only ~5.6m apart at
            // the farm safehouse (mountRange 3.2m / doorRange 2.6m), which
            // left a real, if narrow, overlap zone where a single F press
            // could fire BOTH BikeInteractable.Interact() and
            // CarInteractable.Enter() on the same character in the same
            // frame - parenting them onto two seats at once, which leaves no
            // visibly-working mount at all. IsControlled goes false the
            // instant EITHER vehicle takes them, so checking it here is a
            // correct, general "already busy" guard, not specific to this one
            // overlap - it stops mounting a second vehicle from any state
            // where the player has already been taken by something else.
            var pc = active.root.GetComponent<PlayerController>();
            if (pc != null && !pc.IsControlled) return;

            Transform door = driverDoor != null ? driverDoor : transform;
            if (Vector3.Distance(active.root.transform.position, door.position) > doorRange) return;

            Enter(active.root);
        }

        private void Enter(GameObject interactor)
        {
            if (HasDriver || interactor == null || seatAnchor == null) return;

            _driver = interactor;
            _driverOriginalParent = interactor.transform.parent;

            // Movement off first: a live CharacterController fights being parented
            // to a moving vehicle and jitters the character around the cabin.
            _driverCc = interactor.GetComponent<CharacterController>();
            if (_driverCc != null) _driverCc.enabled = false;

            _driverPc = interactor.GetComponent<PlayerController>();
            if (_driverPc != null) _driverPc.IsControlled = false;

            interactor.transform.SetParent(seatAnchor, worldPositionStays: false);
            interactor.transform.localPosition = Vector3.zero;
            interactor.transform.localRotation = Quaternion.identity;

            _driverAnim = interactor.GetComponent<HumanoidAnimationManager>();
            if (_driverAnim == null) _driverAnim = interactor.GetComponentInChildren<HumanoidAnimationManager>();
            _driverAnim?.BeginSustainedAction(seatedPoseId, 0.25f);

            if (hideRidersInsteadOfPosing) SetRendererVisible(interactor, false);

            RetargetCamera(toCar: true);
            ActiveDriven = this;
            BoardNearbyRecruits();
        }

        /// <summary>MINI-075 workaround helper. Toggles just the RENDERERS
        /// (mesh + skinned mesh), not the GameObject itself - the character
        /// stays fully active so its Animator/HumanoidAnimationManager/
        /// VehicleRider-style components keep working normally under the
        /// hood, they just draw nothing while riding.</summary>
        private static void SetRendererVisible(GameObject character, bool visible)
        {
            if (character == null) return;
            foreach (var r in character.GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = visible;
            }
        }

        /// <summary>
        /// MINI-073: seats any recruited, currently-following crew member who
        /// is near the car when the driver gets in - up to however many
        /// passenger seats the vehicle has. Only members on Follow are
        /// eligible: someone left on plantation guard or home duty is doing
        /// that on purpose and should not be swept into the car.
        /// </summary>
        private void BoardNearbyRecruits() => FillEmptySeats();

        /// <summary>
        /// MINI-073 follow-up: "you can also call gang on phone to come on
        /// your range as well" - CellPhoneController calls this after
        /// teleporting the crew in, so anyone summoned while the player is
        /// already driving climbs straight into an empty seat instead of
        /// just standing next to a moving car.
        ///
        /// Deliberately does NOT clear _passengers first (the original,
        /// enter-only version did) - doing so while already driving would
        /// forget about whoever is already seated, and DropPassengers() on
        /// exit only knows about what is IN that list, so it would leave
        /// them stuck parented to the car forever. This only ever ADDS.
        /// </summary>
        public void FillEmptySeats()
        {
            var all = Character.GangMemberController.All;
            if (all == null || all.Count == 0) return;

            for (int i = 0; i < all.Count && _passengers.Count < passengerSeats.Length; i++)
            {
                var member = all[i];
                if (member == null || !member.IsRecruited || member.IsRiding) continue;
                if (!member.gameObject.activeInHierarchy) continue;
                if (member.Assignment != Character.GangAssignment.Follow) continue;

                float d = Vector3.Distance(member.transform.position, transform.position);
                if (d > boardRadius) continue;

                Transform seat = FindFreeSeat();
                if (seat == null) break;   // no free seat left

                var mcc = member.GetComponent<CharacterController>();
                if (mcc != null) mcc.enabled = false;

                member.transform.SetParent(seat, worldPositionStays: false);
                member.transform.localPosition = Vector3.zero;
                member.transform.localRotation = Quaternion.identity;
                member.IsRiding = true;

                if (hideRidersInsteadOfPosing) SetRendererVisible(member.gameObject, false);

                var memberAnim = member.GetComponent<HumanoidAnimationManager>();
                memberAnim?.BeginSustainedAction(seatedPoseId, 0.25f);

                _passengers.Add(member);
            }
        }

        /// <summary>
        /// MINI-077: a seat is free if nothing is parented to it - checked by
        /// TRANSFORM, not by scanning `_passengers`. `_passengers` only knows
        /// about recruits, and now the other main character can occupy a seat
        /// too (see BoardOtherMainCharacter) - a childCount check is correct
        /// regardless of WHO is sitting there, so recruits and the other main
        /// character can never be double-booked into the same seat.
        /// </summary>
        private Transform FindFreeSeat()
        {
            for (int s = 0; s < passengerSeats.Length; s++)
            {
                if (passengerSeats[s] != null && passengerSeats[s].childCount == 0) return passengerSeats[s];
            }
            return null;
        }

        /// <summary>
        /// MINI-077: "the other character still cannot going inside the
        /// vehivce put it in such a way once he is close and the main
        /// character is in then he goes in. and also if the vehicle is full
        /// he can take the bike to follow."
        ///
        /// Polled every frame while driving (see Update) rather than only at
        /// Enter() - the user's own wording ("once he is close... he goes
        /// in") describes an ongoing condition, not a one-time check the
        /// instant you get in, since the other character is usually still
        /// walking over at that moment.
        /// </summary>
        private void TryBoardOtherMainCharacter()
        {
            if (_otherMain != null) return;   // already boarded

            var switcher = CharacterSwitchManager.Instance;
            if (switcher?.Slots == null || switcher.Slots.Length < 2) return;

            int otherIndex = 1 - switcher.ActiveIndex;
            if (otherIndex < 0 || otherIndex >= switcher.Slots.Length) return;
            if (switcher.IsLocked(otherIndex)) return;   // away on the Guadeloupe run

            var otherSlot = switcher.Slots[otherIndex];
            if (otherSlot?.root == null) return;

            // MINI-079: real bug fixed here - this used to check
            // PlayerController.IsControlled, copied from the DRIVER's own
            // "already busy" guard. That is wrong for the INACTIVE
            // character: CharacterSwitchManager sets IsControlled = isActive
            // on every switch (see ApplyActive), so the inactive slot's
            // IsControlled is ALWAYS false simply because they are not
            // selected, regardless of whether they are riding anything at
            // all - the guard was unconditionally true and silently blocked
            // every attempt, which is why "the side character still wont get
            // into the test vehicle" no matter what. BikeInteractable's own
            // BoardPillion() (which boards this same inactive character onto
            // the bike) got this right already, checking VehicleRider.
            // IsMounted instead - mirrored here.
            var existingRider = otherSlot.root.GetComponent<VehicleRider>();
            if (existingRider != null && existingRider.IsMounted) return;   // already on the bike

            if (Vector3.Distance(otherSlot.root.transform.position, transform.position) > otherMainBoardRadius) return;

            var seat = FindFreeSeat();
            if (seat == null)
            {
                // MINI-077: "if the vehicle is full he can take the bike to
                // follow" - not full AI pathing (there is no navmesh route for
                // a vehicle to autonomously chase another in this project),
                // but a real, honest way out: if they happen to be within
                // mount range of the parked bike right now, mount them onto
                // it as DRIVER so the player can Tab-switch and ride it
                // themselves. If the bike isn't in range either, this is a
                // no-op - there is nothing sensible to force from here.
                var bike = Object.FindFirstObjectByType<BikeInteractable>();
                bike?.TryMountFor(otherSlot.root);
                return;
            }

            BoardOtherMain(otherSlot.root, seat);
        }

        private void BoardOtherMain(GameObject character, Transform seat)
        {
            _otherMain = character;
            _otherMainSeat = seat;

            _otherMainCc = character.GetComponent<CharacterController>();
            if (_otherMainCc != null) _otherMainCc.enabled = false;

            // MINI-079: deliberately NOT touching IsControlled here (unlike
            // the driver's own board/drop, where it correctly means "is this
            // vehicle taking control away"). For the inactive character,
            // IsControlled is owned entirely by CharacterSwitchManager and
            // means "is this the selected slot" - writing to it here would
            // fight that ownership. See TryBoardOtherMainCharacter's own
            // comment on the same trap in reverse (reading it as an
            // eligibility check, which was the actual reported bug).

            character.transform.SetParent(seat, worldPositionStays: false);
            character.transform.localPosition = Vector3.zero;
            character.transform.localRotation = Quaternion.identity;

            if (hideRidersInsteadOfPosing) SetRendererVisible(character, false);

            _otherMainAnim = character.GetComponent<HumanoidAnimationManager>();
            if (_otherMainAnim == null) _otherMainAnim = character.GetComponentInChildren<HumanoidAnimationManager>();
            _otherMainAnim?.BeginSustainedAction(seatedPoseId, 0.25f);
        }

        private void DropOtherMain()
        {
            if (_otherMain == null) return;

            _otherMainAnim?.EndSustainedAction();
            if (hideRidersInsteadOfPosing) SetRendererVisible(_otherMain, true);

            _otherMain.transform.SetParent(null, worldPositionStays: true);

            Vector3 exitPos = transform.position + transform.right * -1.6f + transform.forward * 1.5f;
            if (Physics.Raycast(exitPos + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 8f))
                exitPos = hit.point;
            _otherMain.transform.position = exitPos;

            if (_otherMainCc != null) _otherMainCc.enabled = true;
            // IsControlled deliberately untouched - see BoardOtherMain's comment.

            _otherMain = null;
            _otherMainSeat = null;
            _otherMainAnim = null;
        }

        /// <summary>Drops every boarded passenger out beside the car, mirroring
        /// how the driver exits.</summary>
        private void DropPassengers()
        {
            for (int i = 0; i < _passengers.Count; i++)
            {
                var member = _passengers[i];
                if (member == null) continue;

                member.GetComponent<HumanoidAnimationManager>()?.EndSustainedAction();

                if (hideRidersInsteadOfPosing) SetRendererVisible(member.gameObject, true);

                member.transform.SetParent(null, worldPositionStays: true);

                Vector3 exitPos = transform.position + transform.right * (1.2f + i * 0.7f) + transform.forward * 1.5f;
                if (Physics.Raycast(exitPos + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 8f))
                    exitPos = hit.point;
                member.transform.position = exitPos;

                var mcc = member.GetComponent<CharacterController>();
                if (mcc != null) mcc.enabled = true;

                member.IsRiding = false;
            }
            _passengers.Clear();
        }

        private void Exit()
        {
            if (!HasDriver) return;

            var driver = _driver;
            _driver = null;

            _driverAnim?.EndSustainedAction();

            if (hideRidersInsteadOfPosing) SetRendererVisible(driver, true);

            driver.transform.SetParent(_driverOriginalParent, worldPositionStays: true);

            // Step out beside the driver's door rather than materialising inside
            // the bodywork, which is what dropping them at the seat position does.
            Transform door = driverDoor != null ? driverDoor : transform;
            Vector3 exitPos = door.position + door.right * 0.9f;
            if (Physics.Raycast(exitPos + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 8f))
                exitPos = hit.point;
            driver.transform.position = exitPos;

            if (_driverCc != null) _driverCc.enabled = true;
            if (_driverPc != null) _driverPc.IsControlled = true;

            DropPassengers();
            DropOtherMain();
            if (ActiveDriven == this) ActiveDriven = null;

            car?.SetInput(0f, 0f, 1f);   // leave it braked, not rolling away
            RetargetCamera(toCar: false);
        }

        private void RetargetCamera(bool toCar)
        {
            var cam = Object.FindFirstObjectByType<ThirdPersonFollowCamera>();
            if (cam == null) return;

            if (!toCar)
            {
                var active = CharacterSwitchManager.Instance?.Active;
                if (active?.root != null) cam.SetTarget(active.root.transform);
                cam.OrbitLocked = false;
                if (_camAnchor != null) Destroy(_camAnchor.gameObject);
                _camAnchor = null;
                return;
            }

            // Reuses the bike's levelled chase anchor: it already solves the
            // "camera should not roll or pitch with the vehicle body" problem,
            // and that is just as true for a car.
            if (_camAnchor == null)
            {
                var go = new GameObject("CarCameraAnchor");
                _camAnchor = go.AddComponent<BikeCameraAnchor>();
            }
            _camAnchor.Follow(cameraTarget != null ? cameraTarget : transform);
            cam.SetTarget(_camAnchor.transform);
            cam.OrbitLocked = true;
        }
    }
}
