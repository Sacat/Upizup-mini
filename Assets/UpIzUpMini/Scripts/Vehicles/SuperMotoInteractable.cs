using UnityEngine;
using UpIzUpMini.Cameras;
using UpIzUpMini.Character;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-119, user: "i want to test this in my actual game scene to
    /// get the full gist." A minimal, standalone mount/possess/dismount
    /// interaction for the new Motorbike Physics Tool-based bike
    /// (TmaxBikeController wrapping Gadd420.RB_Controller) - deliberately
    /// separate from BikeInteractable, which stays exactly as it was for
    /// the original bike.
    ///
    /// The SuperMoto prefab carries its OWN fully-rigged, IK-driven rider
    /// (see the asset's IK.cs/RagdollManager.cs) - unlike our original
    /// bike, there is no VehicleSeat/our-own-character-mounting step here.
    /// "Possessing" it simply hides the player's on-foot character,
    /// disables their on-foot control, feeds the bike our own decoupled
    /// input, and points the camera at it - the same axes and keys
    /// BikeInteractable already uses (E for wheelie, Space for brake), so
    /// it feels consistent even though the rider model underneath is the
    /// asset's own.
    /// </summary>
    [RequireComponent(typeof(TmaxBikeController))]
    public class SuperMotoInteractable : InteractableBase
    {
        [SerializeField] private KeyCode mountKey = KeyCode.F;
        [SerializeField] private KeyCode dismountKey = KeyCode.F;
        [SerializeField] private KeyCode wheelieKey = KeyCode.E;
        [SerializeField] private float mountRange = 3.5f;
        [SerializeField] private float dismountSideOffset = 1.3f;

        private TmaxBikeController _bike;
        private GameObject _riderCharacter;
        private BikeCameraAnchor _camAnchor;

        public bool HasRider => _riderCharacter != null;

        private void Awake() => _bike = GetComponent<TmaxBikeController>();

        public override string PromptLabel =>
            HasRider ? $"[ {dismountKey} ] Get off" : $"[ {mountKey} ] Get on (test bike)";

        public override bool CanInteract(GameObject interactor) => !HasRider;

        public override void Interact(GameObject interactor)
        {
            if (HasRider || interactor == null) return;
            Possess(interactor);
        }

        private void Update()
        {
            if (!HasRider)
            {
                TryMountByKey();
                return;
            }

            // Same axes/keys BikeInteractable already uses for the
            // original bike, so switching between the two for comparison
            // feels consistent.
            float throttle = Input.GetAxis("Vertical");
            float steer = Input.GetAxis("Horizontal");
            float brake = Input.GetKey(KeyCode.Space) ? 1f : 0f;

            _bike.SetInput(throttle, steer, brake);
            _bike.SetWheelieHeld(Input.GetKey(wheelieKey));

            if (Input.GetKeyDown(dismountKey)) Dismount();
        }

        private void TryMountByKey()
        {
            if (!Input.GetKeyDown(mountKey)) return;

            var active = CharacterSwitchManager.Instance?.Active;
            if (active?.root == null) return;

            var pc = active.root.GetComponent<PlayerController>();
            if (pc != null && !pc.IsControlled) return; // already busy (on another vehicle)

            if (Vector3.Distance(active.root.transform.position, transform.position) > mountRange) return;

            Possess(active.root);
        }

        private void Possess(GameObject character)
        {
            _riderCharacter = character;

            var pc = character.GetComponent<PlayerController>();
            if (pc != null) pc.IsControlled = false;
            character.SetActive(false); // the SuperMoto's own rider model is the visible one while riding

            var cam = Object.FindFirstObjectByType<ThirdPersonFollowCamera>();
            if (cam != null)
            {
                if (_camAnchor == null)
                {
                    var go = new GameObject("SuperMotoCameraAnchor");
                    _camAnchor = go.AddComponent<BikeCameraAnchor>();
                }
                _camAnchor.Follow(transform);
                cam.SetTarget(_camAnchor.transform);
                cam.OrbitLocked = true;
            }
        }

        private void Dismount()
        {
            if (_riderCharacter == null) return;

            Vector3 exit = transform.position + transform.right * dismountSideOffset + Vector3.up * 0.1f;
            _riderCharacter.transform.position = exit;
            _riderCharacter.SetActive(true);

            var pc = _riderCharacter.GetComponent<PlayerController>();
            if (pc != null) pc.IsControlled = true;

            _bike.SetInput(0f, 0f, 1f);
            _bike.SetWheelieHeld(false);

            var cam = Object.FindFirstObjectByType<ThirdPersonFollowCamera>();
            if (cam != null)
            {
                cam.SetTarget(_riderCharacter.transform);
                cam.OrbitLocked = false;
            }
            if (_camAnchor != null) Destroy(_camAnchor.gameObject);
            _camAnchor = null;

            _riderCharacter = null;
        }
    }
}
