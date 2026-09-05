using UnityEngine;
using UpIzUpMini.Character;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-066/067. Picks which riding pose the mounted rider should be
    /// holding, from the bike's own live state.
    ///
    /// Clip identification was NOT inferred from the pack's file names - the
    /// names are misleading (the wheelie clip ships called "cheer01"). The
    /// user identified the poses from the vendor's preview video, and each
    /// one was then confirmed numerically by measuring torso angles straight
    /// out of the FBX skeletons in Blender:
    ///
    ///   riding  (MOTOIdle02)  torso -59.8deg from vertical (over the bars)
    ///   stopped (MOTOIdle01)  torso -38.8deg              (sat upright)
    ///   wheelie (cheer01)     torso  +1.7deg              (bolt upright)
    ///   lean L/R (Left/Right02) torso side +/-6.6deg, pitch unchanged
    ///
    /// Poses are held via BeginSustainedAction rather than fired once,
    /// because "sitting on a moving bike" has no natural end - the same
    /// distinction HumanoidAnimationManager's FullBodyOverride layer exists
    /// for. Only re-issued when the chosen pose actually changes, so the
    /// clip is not restarted every frame.
    /// </summary>
    public class BikeRiderAnimation : MonoBehaviour
    {
        [Tooltip("Below this speed the rider uses the upright stopped pose rather than the leaned-forward riding pose.")]
        [SerializeField] private float movingSpeedKmh = 3f;
        [Tooltip("How much steering input counts as a deliberate lean.")]
        [SerializeField] private float leanInputThreshold = 0.55f;
        [Tooltip("Once leaning, steering must fall below this smaller threshold before a new lean accent can play. Prevents rapid left/right pose flicker.")]
        [SerializeField] private float leanExitThreshold = 0.18f;
        [Tooltip("Seconds to smooth the steering value the lean decision is made from. Raised per the user's \"just making bend slower\" - real keyboard input is instant on/off, so without this the rider snapped between lean poses the moment a key was tapped.")]
        [SerializeField] private float leanInputSmoothing = 0.45f;
        [Tooltip("Return-to-centre is deliberately faster than entering a lean, so the rider stops swinging as soon as the turn is released.")]
        [SerializeField] private float leanReturnSmoothing = 0.14f;
        [Tooltip("Crossfade time into the WHEELIE pose - how fast the character angles up into it. Was sharing the generic 0.15s default, i.e. a near-snap, which is what the user meant by \"the character is angling too fast\". Separate from the bike's own rise rate and from the seated<->wheelie position blend.")]
        [SerializeField] private float wheelieBlendSeconds = 0.5f;

        [Tooltip("Crossfade time into a lean pose. Longer than the default action blend so leaning reads as the rider easing over rather than switching pose.")]
        [SerializeField] private float leanBlendSeconds = 0.24f;
        [Tooltip("The authored lean clip is only a short turn accent. After this time the normal ride pose resumes while restrained procedural roll keeps the body with the bike.")]
        [SerializeField] private float leanPoseMaxSeconds = 0.38f;

        [Tooltip("ON (user's choice - \"i like the cheer animation so use it\"): play the pack's own wheelie pose. OFF: hold the ordinary riding pose through the wheelie instead. With partial blending below, OFF is now equivalent to a blend of 0.")]
        [SerializeField] private bool useWheelieClip = true;

        [Tooltip("How much of the wheelie pose is mixed OVER the riding pose. 0 = pure riding pose (rider stays over the bars, angle driven only by the bike + the tuner's pitch offset), 1 = the full authored wheelie pose. The authored pose is bolt upright (+1.7deg torso vs -59.8deg riding), so it leans the rider back ~61deg on its own, independently of anything numeric - which is why full weight could never be angle-synced to the bike. A partial value keeps the pose's character while leaving the bike's live angle in charge of how far back he actually goes.")]
        [Range(0f, 1f)][SerializeField] private float wheelieClipWeight = 0.45f;

        [Tooltip("How long the bike must sit still before the rider relaxes into the resting pose (the pack's MOTOIdle01->Idle move).")]
        [SerializeField] private float restAfterSeconds = 2.5f;

        private bool _wasMoving;
        private float _stoppedSeconds;
        private bool _restPlayed;

        private float _smoothedSteer;
        private int _activeLeanDirection;
        private float _leanPoseSeconds;

        private HumanoidAnimationManager _anim;
        private string _current;

        private float _blendWeight;
        private bool _blendPlaying;

        private void Awake()
        {
            _anim = GetComponent<HumanoidAnimationManager>();
        }

        /// <summary>
        /// Called every frame by BikeInteractable while this character is in
        /// the driver's seat. Takes the bike rather than finding it, so the
        /// rider component stays usable for a pillion or an AI rider that has
        /// no control over the vehicle at all.
        /// </summary>
        /// <summary>Smoothed steering, exposed so the caller can scale the
        /// rider's counter-roll by the same value the pose decision uses -
        /// otherwise the trim would jump while the pose eased.</summary>
        public float WheelieBlendSeconds { get => wheelieBlendSeconds; set => wheelieBlendSeconds = value; }
        public float WheelieClipWeight
        {
            get => wheelieClipWeight;
            set => wheelieClipWeight = Mathf.Clamp01(value);
        }
        public float LeanBlendSeconds { get => leanBlendSeconds; set => leanBlendSeconds = value; }

        public float SmoothedSteer => _smoothedSteer;

        public void UpdatePose(TmaxBikeControllerCustom bike, float steerInput)
        {
            if (_anim == null || bike == null) return;

            // Ease the steering value the lean is chosen from. Keyboard input
            // is a hard 0/1, so reacting to it directly made the rider snap
            // between lean poses on every tap.
            bool returningToCentre = Mathf.Abs(steerInput) < Mathf.Abs(_smoothedSteer)
                && (Mathf.Approximately(steerInput, 0f)
                    || Mathf.Sign(steerInput) == Mathf.Sign(_smoothedSteer));
            float smoothing = returningToCentre ? leanReturnSmoothing : leanInputSmoothing;
            float k = smoothing > 0.001f
                ? Time.deltaTime / smoothing
                : 1f;
            _smoothedSteer = Mathf.MoveTowards(_smoothedSteer, steerInput, k);
            steerInput = _smoothedSteer;

            bool wheelieing = bike.IsWheelieing || bike.WheelieAngle > 5f;
            bool moving = bike.SpeedKmh >= movingSpeedKmh;

            UpdateWheelieOverlay(wheelieing);

            // --- Stopped/rest/pull-away sequence -------------------------
            // The user's own reading of the pack's preview video: coming to a
            // stop should play 02->01 to settle onto the stopped pose, then
            // 01->Idle if the rider just sits there, and 01->02 to pull away
            // again. Those are one-shot TRANSITIONS between poses, so they are
            // fired on the edges rather than held - holding a transition would
            // freeze the rider mid-move.
            if (!wheelieing)
            {
                if (moving && !_wasMoving)
                {
                    // Pulling away from a standstill.
                    _anim.PlayAction(Mini011Ids.PullAway);
                    _restPlayed = false;
                    _stoppedSeconds = 0f;
                }
                else if (!moving && _wasMoving)
                {
                    // Just come to a rest.
                    _anim.PlayAction(Mini011Ids.StopSettle);
                    _restPlayed = false;
                    _stoppedSeconds = 0f;
                }

                if (!moving)
                {
                    _stoppedSeconds += Time.deltaTime;

                    // Sat still long enough - relax into the resting pose.
                    // Deliberately a sustained action: the clip does not loop,
                    // so Mecanim holds its last frame, which is exactly the
                    // "still sitting there" look wanted.
                    if (!_restPlayed && _stoppedSeconds >= restAfterSeconds)
                    {
                        _restPlayed = true;
                        _current = Mini011Ids.Rest;
                        _anim.BeginSustainedAction(Mini011Ids.Rest, 0.3f);
                        _wasMoving = false;
                        return;
                    }
                }
                else
                {
                    _stoppedSeconds = 0f;
                    _restPlayed = false;
                }
            }

            _wasMoving = moving;

            // Once resting, stay there until the bike actually moves again -
            // otherwise the held pose would be re-issued every frame and
            // restart the clip.
            if (_restPlayed && !moving && !wheelieing) return;

            if (_activeLeanDirection == 0)
            {
                if (steerInput <= -leanInputThreshold)
                {
                    _activeLeanDirection = -1;
                    _leanPoseSeconds = 0f;
                }
                else if (steerInput >= leanInputThreshold)
                {
                    _activeLeanDirection = 1;
                    _leanPoseSeconds = 0f;
                }
            }
            else if (Mathf.Abs(steerInput) <= leanExitThreshold)
            {
                _activeLeanDirection = 0;
                _leanPoseSeconds = 0f;
            }
            else if (steerInput <= -leanInputThreshold && _activeLeanDirection > 0)
            {
                _activeLeanDirection = -1;
                _leanPoseSeconds = 0f;
            }
            else if (steerInput >= leanInputThreshold && _activeLeanDirection < 0)
            {
                _activeLeanDirection = 1;
                _leanPoseSeconds = 0f;
            }

            string want;

            if (wheelieing)
            {
                // The riding pose stays the base even through a wheelie. The
                // pack's own wheelie pose ("cheer01") is layered OVER it by
                // UpdateWheelieOverlay at wheelieClipWeight, rather than
                // replacing it outright - see that method for why.
                want = Mini011Ids.Ride;
            }
            else if (!moving)
            {
                want = Mini011Ids.Stopped;
            }
            else if (_activeLeanDirection < 0 && _leanPoseSeconds < leanPoseMaxSeconds)
            {
                want = Mini011Ids.LeanLeft;
                _leanPoseSeconds += Time.deltaTime;
            }
            else if (_activeLeanDirection > 0 && _leanPoseSeconds < leanPoseMaxSeconds)
            {
                want = Mini011Ids.LeanRight;
                _leanPoseSeconds += Time.deltaTime;
            }
            else
            {
                want = Mini011Ids.Ride;
            }

            if (want == _current) return;
            _current = want;

            // Leans get a longer crossfade than other pose changes, so the
            // rider eases into the lean instead of popping into it.
            bool isLean = want == Mini011Ids.LeanLeft || want == Mini011Ids.LeanRight;
            // Three separate speeds, deliberately not shared: leans ease over
            // leanBlendSeconds, the wheelie pose angles up over
            // wheelieBlendSeconds, and everything else uses a short default.
            float blend =
                isLean ? leanBlendSeconds
                : want == Mini011Ids.Wheelie ? wheelieBlendSeconds
                : 0.15f;
            _anim.BeginSustainedAction(want, blend);
        }

        /// <summary>
        /// Ramps the wheelie pose in and out as an OVERLAY on the blend layer,
        /// over the riding pose held underneath.
        ///
        /// Why an overlay rather than swapping the held pose: the authored
        /// wheelie clip is bolt upright, so playing it outright forces a ~61deg
        /// back-lean that no numeric offset is really in charge of - which is
        /// exactly the "the character angles further back when wheelieing"
        /// problem, and why it could not be synced to the bike. As an overlay,
        /// weight w means the rider sits w of the way from the riding pose
        /// towards the wheelie pose, and the bike's live angle (plus the
        /// tuner's pitch offset) still decides the rest.
        ///
        /// Ramped here frame by frame rather than by a fade coroutine so the
        /// tuner slider retunes it live, mid-wheelie, without restarting the
        /// clip.
        /// </summary>
        // MINI-119 follow-up, user: "the character stays in the same
        // position so the hands on the handlebar looks out of position
        // since the character doesnt move." Made public so
        // SuperMotoVehicleInteractable can drive the SAME authored
        // wheelie-pose overlay TMAX riders already get, without needing
        // the whole TMAX-specific UpdatePose (lean/rest/pull-away state
        // machine, which needs a concrete TmaxBikeController this bike
        // doesn't have) - just the one piece that actually reshapes the
        // arms/torso for a wheelie instead of only translating the body.
        public void UpdateWheelieOverlay(bool wheelieing)
        {
            float target = wheelieing && useWheelieClip ? wheelieClipWeight : 0f;

            if (target > 0f && !_blendPlaying)
            {
                // Crossfade in at 0 weight, so the pose is already the right
                // one by the time the ramp starts revealing it.
                _blendPlaying = _anim.PlayBlendedPose(Mini011Ids.Wheelie, wheelieBlendSeconds);
                if (!_blendPlaying) return;   // no state for it - nothing to blend
            }

            if (!_blendPlaying) return;

            float rate = wheelieBlendSeconds > 0.001f
                ? Time.deltaTime / wheelieBlendSeconds
                : 1f;
            _blendWeight = Mathf.MoveTowards(_blendWeight, target, rate);
            _anim.SetBlendedWeight(_blendWeight);

            // Fully back down - drop the overlay so the layer is genuinely
            // idle rather than sitting at weight 0 holding a pose.
            if (target <= 0f && _blendWeight <= 0.001f)
            {
                _anim.StopBlendedPose();
                _blendPlaying = false;
            }
        }

        /// <summary>Clears the held pose - called on dismount so the rider
        /// does not walk around still sitting.</summary>
        public void ClearPose()
        {
            _blendWeight = 0f;
            _blendPlaying = false;
            _current = null;
            _wasMoving = false;
            _stoppedSeconds = 0f;
            _restPlayed = false;
            _activeLeanDirection = 0;
            _leanPoseSeconds = 0f;
            _smoothedSteer = 0f;
            if (_anim != null) _anim.EndSustainedAction();
        }

        /// <summary>
        /// Action ids, duplicated from Mini011PhaseBSetup's constants because
        /// that lives in an Editor-only assembly this runtime script cannot
        /// reference. Mini065TmaxValidation checks every id here really has a
        /// state on the controller's full-body layers, so a rename on either
        /// side fails a test rather than silently leaving the rider posed
        /// wrong.
        /// </summary>
        public static class Mini011Ids
        {
            public const string Ride = "RideBike";
            public const string Stopped = "BikeStopped";
            public const string Wheelie = "BikeWheelie";
            public const string LeanLeft = "BikeLeanLeft";
            public const string LeanRight = "BikeLeanRight";
            public const string StopSettle = "BikeStopSettle";
            public const string Rest = "BikeRest";
            public const string PullAway = "BikePullAway";
            public const string Pillion = "RidePillion";
        }
    }
}
