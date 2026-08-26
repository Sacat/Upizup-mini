using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-066. Marks one seat on a vehicle and carries everything a rider
    /// needs to be placed and posed on it.
    ///
    /// Deliberately a small data component rather than bike-specific logic,
    /// because the user's own instruction is that entry differs per vehicle
    /// type ("remember bike entry and car entry is different okay"). A bike
    /// seat is straddled - the rider swings a leg over from the side, ends up
    /// astride with both hands on the bars and both feet on pegs. A car seat
    /// is entered through a door and the occupant sits enclosed. Those are
    /// different animations AND different IK targets, so <see cref="style"/>
    /// lets one rider component drive both without either becoming a special
    /// case buried in an if-chain. Cars are explicitly later work (see the
    /// brief's Vehicle Architecture section); this exists so adding them
    /// doesn't mean rewriting the rider.
    ///
    /// The anchors themselves are already built onto TMAX_560 by
    /// Mini064TmaxAssetPrep (Seat, HandlebarLeft/Right, LeftFootTarget,
    /// RightFootTarget) - measured from the real mesh rather than guessed,
    /// so the IK targets line up with the actual model.
    /// </summary>
    public class VehicleSeat : MonoBehaviour
    {
        public enum SeatRole
        {
            Driver,
            Passenger,
        }

        /// <summary>How a rider gets on/off. Bike and car are genuinely
        /// different motions, not a shared "enter vehicle" clip.</summary>
        public enum EntryStyle
        {
            /// <summary>Swing a leg over and straddle - motorcycles,
            /// scooters, the TMAX.</summary>
            Straddle,
            /// <summary>Open a door and sit enclosed - cars, vans. Not used
            /// yet; here so car support doesn't require reshaping this.</summary>
            DoorSeated,
        }

        [SerializeField] private SeatRole role = SeatRole.Driver;
        [SerializeField] private EntryStyle style = EntryStyle.Straddle;

        [Header("Placement")]
        [Tooltip("Where the rider's root sits once mounted. Usually the vehicle's own Seat marker.")]
        [SerializeField] private Transform seatAnchor;

        [Header("IK targets (leave any unset to skip that constraint)")]
        [Tooltip("Where the LEFT hand is pinned - a handlebar grip for a driver, a grab handle or the driver's waist for a passenger.")]
        [SerializeField] private Transform leftHandTarget;
        [SerializeField] private Transform rightHandTarget;
        [SerializeField] private Transform leftFootTarget;
        [SerializeField] private Transform rightFootTarget;

        // MINI-119 follow-up, user found these exact numbers by hand,
        // live, on the SuperMoto: "so can you fix how the rider fits on
        // the bike... tell me how you did this." VehicleRider is SHARED
        // across every bike (TMAX and SuperMoto both add it to whoever
        // mounts), so its own seatedOffset/wheelieOffset fields can't be
        // repointed per-bike without breaking the TMAX's own already-
        // tuned seating - moved the DATA here instead, onto the
        // per-vehicle VehicleSeat, defaulting to VehicleRider's own
        // previous hardcoded values so the TMAX's behaviour is
        // completely unchanged unless a seat explicitly overrides it
        // (see ConfigureSeatedPose/ConfigureWheeliePose). VehicleRider.
        // Mount() now copies these onto itself at the moment of
        // mounting - one rider script, per-bike keyframes.
        [Header("Keyframed seating - SEATED (see VehicleRider for what these mean)")]
        [SerializeField] private Vector3 seatedOffset = new Vector3(0.11f, -0.01f, 0.32f);
        [SerializeField] private float seatedPitch = 0f;

        [Header("Keyframed seating - WHEELIE")]
        [SerializeField] private Vector3 wheelieOffset = new Vector3(0f, -0.40f, 0.28f);
        [SerializeField] private float wheeliePitch = 22f;

        public Vector3 SeatedOffset => seatedOffset;
        public float SeatedPitch => seatedPitch;
        public Vector3 WheelieOffset => wheelieOffset;
        public float WheeliePitch => wheeliePitch;

        public void ConfigureSeatedPose(Vector3 offset, float pitch)
        {
            seatedOffset = offset;
            seatedPitch = pitch;
        }

        public void ConfigureWheeliePose(Vector3 offset, float pitch)
        {
            wheelieOffset = offset;
            wheeliePitch = pitch;
        }

        [Header("Animation")]
        [Tooltip("HumanoidAnimationManager action id held for the whole time this seat is occupied (a sustained full-body pose, not a one-shot).")]
        [SerializeField] private string ridePoseActionId = "RideBike";
        [Tooltip("One-shot action id played while getting on. Dismount reuses this clip reversed - the user's own suggestion, and cheaper than authoring a second clip.")]
        [SerializeField] private string mountActionId = "MountBike";
        [Tooltip("MINI-079: where IN the ride pose clip playback starts, in seconds. 0 = the clip's own beginning. Exists for seats whose clip has an unwanted lead-in - the pillion's borrowed 'cheer' clip cheers before settling, so this skips straight to the settled part.")]
        [SerializeField] private float ridePoseStartTimeSeconds = 0f;

        public SeatRole Role => role;
        public EntryStyle Style => style;
        public Transform SeatAnchor => seatAnchor != null ? seatAnchor : transform;
        public Transform LeftHandTarget => leftHandTarget;
        public Transform RightHandTarget => rightHandTarget;
        public Transform LeftFootTarget => leftFootTarget;
        public Transform RightFootTarget => rightFootTarget;
        public string RidePoseActionId => ridePoseActionId;
        public string MountActionId => mountActionId;
        public float RidePoseStartTimeSeconds => ridePoseStartTimeSeconds;

        /// <summary>The rider currently in this seat, or null. Set by
        /// VehicleRider - a seat never mounts anyone itself, it only
        /// describes where and how.</summary>
        public GameObject Occupant { get; set; }

        public bool IsOccupied => Occupant != null;

        /// <summary>MINI-119 follow-up: SuperMoto's own seat isn't baked
        /// onto a prefab the way TMAX's is (Mini064TmaxAssetPrep authors
        /// it at edit time) - it's spawned fresh from the raw vendor
        /// prefab at runtime, so its anchors have to be wired the same
        /// way, in code, after spawn.</summary>
        public void Configure(Transform seat, Transform leftHand, Transform rightHand,
            Transform leftFoot, Transform rightFoot, string mountAction, string ridePoseAction)
        {
            seatAnchor = seat;
            leftHandTarget = leftHand;
            rightHandTarget = rightHand;
            leftFootTarget = leftFoot;
            rightFootTarget = rightFoot;
            mountActionId = mountAction;
            ridePoseActionId = ridePoseAction;
        }
    }
}
