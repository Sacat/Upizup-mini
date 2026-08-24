using UnityEngine;
using UpIzUpMini.Cameras;
using UpIzUpMini.Character;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-119 follow-up, user: "i want the bike spawned in the same
    /// way, i want to be able to walk to the bike and press f to get on
    /// the bike." F-to-mount/dismount for the stock demo SuperMoto,
    /// mirroring BikeInteractable's own proven TryMountByKey pattern
    /// (same key, same range/IsControlled checks) - just driving
    /// SuperMotoRagdollRider's Mount/Dismount instead of TmaxBikeController/
    /// VehicleRider, since the bike itself needs no per-frame input relay
    /// at all (RB_Controller and SuperMotoWheelieKeyRemap already read
    /// the keyboard directly).
    ///
    /// Also the fix for "when i pressed tab twice to switch character it
    /// showed the character falling under the map": the mounted
    /// character's CharacterSwitchManager slot is LOCKED for the
    /// duration of the ride (the same lock mechanism already used for
    /// "away on the Guadeloupe run"), so TAB simply can't switch away
    /// from a half-ragdoll rider mid-ride and leave them uncontrolled.
    /// </summary>
    public class SuperMotoStockInteractable : InteractableBase
    {
        [SerializeField] private KeyCode mountKey = KeyCode.F;
        [SerializeField] private KeyCode dismountKey = KeyCode.F;
        [SerializeField] private float mountRange = 3.5f;
        [SerializeField] private float dismountSideOffset = 1.3f;

        private Vector3 _seatLocalPos;
        private Transform _rightHandTarget, _leftHandTarget, _rightFootTarget, _leftFootTarget;
        private Transform _camAnchor;

        private GameObject _mountedPlayer;
        private SuperMotoKinematicRider _rider;
        private GameObject _seatGo;
        private int _lockedSlotIndex = -1;

        public bool HasRider => _mountedPlayer != null;

        public void Configure(Vector3 seatLocalPos,
            Transform rightHandTarget, Transform leftHandTarget, Transform rightFootTarget, Transform leftFootTarget,
            Transform camAnchor)
        {
            _seatLocalPos = seatLocalPos;
            _rightHandTarget = rightHandTarget;
            _leftHandTarget = leftHandTarget;
            _rightFootTarget = rightFootTarget;
            _leftFootTarget = leftFootTarget;
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

            // MINI-119 follow-up, user: "put some quick controls on the
            // screen i want to test it using my phone." SuperMotoTouchInputState's
            // mount button is edge-triggered the same way Input.GetKeyDown
            // is - ConsumeMountPressed() must be called even if the key
            // check already short-circuits, or a tap while mounted would
            // sit "pressed" and fire the moment you dismount.
            bool touchPressed = SuperMotoTouchInputState.ConsumeMountPressed();
            if (Input.GetKeyDown(dismountKey) || touchPressed) Dismount();
        }

        private void TryMountByKey()
        {
            bool touchPressed = SuperMotoTouchInputState.ConsumeMountPressed();
            if (!Input.GetKeyDown(mountKey) && !touchPressed) return;

            var active = CharacterSwitchManager.Instance?.Active;
            if (active?.root == null) return;

            var pc = active.root.GetComponent<PlayerController>();
            if (pc != null && !pc.IsControlled) return; // already busy (driving something else etc.)

            if (Vector3.Distance(active.root.transform.position, transform.position) > mountRange) return;

            Mount(active.root);
        }

        private void Mount(GameObject player)
        {
            if (HasRider || player == null) return;

            // MINI-119 follow-up fix, user: "the bike just flips up high
            // into the air when trying to mount." Real cause: the
            // CharacterController (a live physics capsule) used to only
            // get disabled deep inside SuperMotoKinematicRider.BuildRig,
            // which waits several frames for animator.avatar to be ready
            // before running. In that gap the player was ALREADY parented
            // and teleported directly onto/inside the bike's seat
            // geometry below with a still-active collider - a sudden deep
            // overlap the physics solver resolves by shoving the much
            // lighter bike Rigidbody violently away on its next
            // FixedUpdate. Disabled here, synchronously, before any
            // parenting/teleporting happens - no gap for the collider to
            // be live while overlapping.
            var characterController = player.GetComponent<CharacterController>();
            if (characterController != null) characterController.enabled = false;
            var playerController = player.GetComponent<Character.PlayerController>();
            if (playerController != null) playerController.IsControlled = false;

            // MINI-119 follow-up fix, user: "just the walking animation
            // is going on." Real cause: PlayerController.Update() returns
            // immediately whenever IsControlled is false (see its own
            // header comment - "only processes input while IsControlled
            // is true") - it never touches the Animator's own Speed/
            // MotionSpeed parameters again once that happens, so whatever
            // value they held the instant before mounting (nonzero, since
            // you were walking TO the bike) stays stuck forever, looping
            // the walk cycle the whole ride. Frozen to a calm standing
            // idle here, once, right as control is taken away - the IK
            // constraints (SuperMotoKinematicRider, already running every
            // frame once built) then bend arms/legs from that idle base
            // pose onto the bike's own hand/foot anchors.
            var animator = player.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetFloat("Speed", 0f);
                animator.SetFloat("MotionSpeed", 0f);
                animator.SetBool("Grounded", true);
                animator.SetBool("Jump", false);
                animator.SetBool("FreeFall", false);
                // MINI-119 follow-up, user: "the sitting pose is already
                // a freeze animation." Zeroing the blend parameters alone
                // still lets the idle clip's own internal loop (weight
                // shift/breathing) play out frame to frame - genuinely
                // freezing playback (not just blending toward idle) is
                // what stops that outright. Animator.speed only halts the
                // state's own internal clock; the blend tree's weighting
                // among sub-motions is still driven by the Speed/
                // MotionSpeed parameters above regardless of speed=0, so
                // this lands on - and holds - the idle pose, not a
                // mid-walk-cycle frame. Restored to 1 in Dismount().
                animator.speed = 0f;
            }

            var seatGo = new GameObject("PlayerSeat");
            _seatGo = seatGo;
            seatGo.transform.SetParent(transform, false);
            seatGo.transform.localPosition = _seatLocalPos;
            seatGo.transform.localRotation = Quaternion.identity;

            player.transform.SetParent(seatGo.transform, false);
            // Reusing the user's own manually-placed numbers from earlier
            // this session as the one-time starting pose the ragdoll
            // poses limbs from - see SuperMotoRagdollRider's own header.
            player.transform.localPosition = new Vector3(0.022f, -0.925f, 0.154f);
            player.transform.localRotation = Quaternion.Euler(-7.529f, 0f, 0f);
            Vector3 parentScale = seatGo.transform.lossyScale;
            player.transform.localScale = new Vector3(
                player.transform.localScale.x / Mathf.Max(0.0001f, parentScale.x),
                player.transform.localScale.y / Mathf.Max(0.0001f, parentScale.y),
                player.transform.localScale.z / Mathf.Max(0.0001f, parentScale.z));

            _rider = player.GetComponent<SuperMotoKinematicRider>();
            if (_rider == null) _rider = player.AddComponent<SuperMotoKinematicRider>();
            _rider.Configure(player, seatGo.transform, _rightHandTarget, _leftHandTarget, _rightFootTarget, _leftFootTarget);

            // MINI-119 follow-up, user: "my character moves wit the bike
            // controls same time so when i start the game the bike
            // automatically starts moving." Real bug, confirmed in
            // RB_Controller.cs: it reads player keyboard input every
            // single frame regardless of whether anyone's mounted, so a
            // parked bike drove itself the instant the player pressed the
            // same WASD keys to walk. These only get switched on for the
            // duration of the ride now.
            VehicleSpawnController.SetBikeInputEnabled(gameObject, true);

            _mountedPlayer = player;

            // Lock the mounted character's slot so TAB can't switch away
            // mid-ride and leave a half-ragdoll body with no control and
            // no CharacterController - the fall-through bug reported.
            var switcher = CharacterSwitchManager.Instance;
            if (switcher?.Slots != null)
            {
                for (int i = 0; i < switcher.Slots.Length; i++)
                {
                    if (switcher.Slots[i]?.root == player) { _lockedSlotIndex = i; switcher.SetLocked(i, true); break; }
                }
            }

            RetargetGameCamera(toBike: true);
        }

        private void Dismount()
        {
            if (!HasRider) return;

            Vector3 exitPosition = transform.position + transform.right * dismountSideOffset + Vector3.up * 0.1f;
            if (Physics.Raycast(exitPosition + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f))
                exitPosition = hit.point;

            if (_rider != null) _rider.Dismount(exitPosition);
            VehicleSpawnController.SetBikeInputEnabled(gameObject, false);

            var dismountAnimator = _mountedPlayer.GetComponent<Animator>();
            if (dismountAnimator != null) dismountAnimator.speed = 1f; // undo the mount-time freeze

            if (_seatGo != null) Destroy(_seatGo);
            _seatGo = null;

            var switcher = CharacterSwitchManager.Instance;
            if (_lockedSlotIndex >= 0 && switcher != null) switcher.SetLocked(_lockedSlotIndex, false);
            _lockedSlotIndex = -1;

            var pc = _mountedPlayer.GetComponent<PlayerController>();
            if (pc != null) pc.IsControlled = true;

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
