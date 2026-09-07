using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UpIzUpMini.Character
{
    /// <summary>
    /// Reusable action-animation layer for any Humanoid-rigged character
    /// (player, farmhand, police, ambient NPC — nothing here is
    /// player-specific).
    ///
    /// The base locomotion layer (Speed / Grounded / Jump / FreeFall on the
    /// authored StarterAssetsThirdPerson controller, shared by every
    /// character in the scene) is left completely untouched by this
    /// component — it is already tuned and correct. This instead drives two
    /// extra Animator layers baked once into that shared controller by
    /// <see cref="UpIzUpMini.EditorTools.HumanoidAnimationLayerBuilder"/>:
    ///
    ///  - "Action": masked to the upper body only (see the builder for the
    ///    exact mask), for one-shot context moves — a melee swing, a food
    ///    item raised to the mouth — that should play while the character
    ///    keeps walking/running underneath, exactly like GTA's animation
    ///    layering.
    ///  - "FullBodyOverride": unmasked, weight 0 by default, for poses that
    ///    should replace the whole body — entering/exiting a vehicle,
    ///    sitting while driving. Held via <see cref="BeginSustainedAction"/>
    ///    rather than firing once, because those states have no natural
    ///    end on their own.
    ///
    /// A new action is a data entry — an id, a clip, and whether it needs
    /// the full body — assigned per character in the Inspector (or via
    /// <see cref="RegisterAction"/> at runtime). Adding "Eat" or "Aim"
    /// later is exactly that: an entry plus a call to
    /// <see cref="PlayAction"/> from whatever gameplay script triggers it
    /// (see <see cref="UpIzUpMini.Combat.SimpleMeleeCombat"/> for the first
    /// real caller) — no changes to this file. Calling an id a given
    /// character doesn't carry, or one whose state isn't baked into this
    /// controller yet, is a safe no-op — callers never need to know which
    /// characters support which actions.
    /// </summary>
    public class HumanoidAnimationManager : MonoBehaviour
    {
        [System.Serializable]
        public struct ActionEntry
        {
            public string id;
            public AnimationClip clip;
            [Tooltip("Full body (replaces locomotion entirely — vehicle seating, eventually) vs. upper-body-only (composes with walking/running — melee, eating).")]
            public bool fullBody;
        }

        public const string ActionLayerName = "Action";
        public const string FullBodyLayerName = "FullBodyOverride";
        /// <summary>
        /// A SECOND full-body override layer, sitting above FullBodyOverride.
        ///
        /// Exists because layer weight on FullBodyOverride itself cannot give a
        /// partial pose: lowering it blends the held pose towards the BASE
        /// locomotion layer (a standing idle), not towards the other held pose.
        /// For a rider that meant a half-weight wheelie pose mixed in standing
        /// legs - feet off the pegs. With the ride pose held at full weight on
        /// FullBodyOverride and the wheelie pose on this layer above it, the
        /// weight here is a genuine ride-pose -> wheelie-pose blend.
        /// </summary>
        public const string FullBodyBlendLayerName = "FullBodyBlend";
        public const string EmptyStateName = "Empty";

        [SerializeField] private Animator animator;
        [SerializeField] private List<ActionEntry> actions = new List<ActionEntry>();
        [SerializeField] private float blendOutSeconds = 0.15f;

        private readonly Dictionary<string, ActionEntry> _byId = new Dictionary<string, ActionEntry>();
        private int _actionLayer = -1;
        private int _fullBodyLayer = -1;
        private Coroutine _actionFade;
        private Coroutine _fullBodyFade;
        private bool _fullBodySustained;
        private float _fullBodyWeight = 1f;
        private int _blendLayer = -1;
        private bool _blendActive;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            RebuildLookup();
            ResolveLayers();
        }

        private void RebuildLookup()
        {
            _byId.Clear();
            foreach (var entry in actions)
            {
                if (!string.IsNullOrEmpty(entry.id)) _byId[entry.id] = entry;
            }
        }

        private void ResolveLayers()
        {
            if (animator == null) return;
            _actionLayer = animator.GetLayerIndex(ActionLayerName);
            _fullBodyLayer = animator.GetLayerIndex(FullBodyLayerName);
            _blendLayer = animator.GetLayerIndex(FullBodyBlendLayerName);
        }

        /// <summary>Adds or replaces an action this character can play, without needing the editor tool. Does not by itself bake the Animator state — the id must already exist in the shared controller (see the layer builder) for PlayAction to succeed.</summary>
        public void RegisterAction(string id, AnimationClip clip, bool fullBody = false)
        {
            if (string.IsNullOrEmpty(id)) return;
            var entry = new ActionEntry { id = id, clip = clip, fullBody = fullBody };
            _byId[id] = entry;
            for (int i = 0; i < actions.Count; i++)
            {
                if (actions[i].id == id) { actions[i] = entry; return; }
            }
            actions.Add(entry);
        }

        /// <summary>
        /// Plays a one-shot action once. Fades the target layer's weight in
        /// immediately and back out automatically once the clip finishes
        /// (see <see cref="blendOutSeconds"/>), so it composes with
        /// whatever locomotion is doing underneath instead of freezing the
        /// whole character. Returns false without doing anything if this
        /// character doesn't carry that action id, or the baked Animator
        /// state doesn't exist (e.g. the controller hasn't been rebuilt
        /// since the id was added) — safe to call speculatively.
        /// </summary>
        public bool PlayAction(string id, float blendIn = 0.08f, float durationOverride = -1f)
        {
            if (animator == null || string.IsNullOrEmpty(id)) return false;
            if (!_byId.TryGetValue(id, out var entry)) return false;

            int layer = entry.fullBody ? _fullBodyLayer : _actionLayer;
            if (layer < 0) return false;

            int hash = Animator.StringToHash(id);
            if (!animator.HasState(layer, hash)) return false;

            animator.SetLayerWeight(layer, 1f);
            animator.CrossFadeInFixedTime(id, blendIn, layer, 0f);

            float clipLength = entry.clip != null ? entry.clip.length : 0.5f;
            // Optional measured playback duration for accelerated kick states;
            // all existing non-melee/vehicle callers retain their old timing.
            var fade = FadeLayerAfter(layer, durationOverride > 0f ? durationOverride : clipLength + blendIn);
            if (entry.fullBody)
            {
                if (_fullBodyFade != null) StopCoroutine(_fullBodyFade);
                _fullBodyFade = StartCoroutine(fade);
            }
            else
            {
                if (_actionFade != null) StopCoroutine(_actionFade);
                _actionFade = StartCoroutine(fade);
            }
            return true;
        }

        /// <summary>
        /// Holds a full-body pose at full weight indefinitely — for states
        /// with no natural end, like sitting while driving. Call
        /// <see cref="EndSustainedAction"/> to fade back out (e.g. on
        /// exiting a vehicle). Nothing calls this yet in this build; it
        /// exists so vehicle-entry work has a stable place to plug into
        /// rather than needing this component reworked later.
        /// </summary>
        /// <param name="startTimeSeconds">
        /// MINI-079: "i want to use the latter part of cheer2 for the pillion
        /// just so he wouldnt do all that cheering before he sits". Passed
        /// straight through to Animator.CrossFadeInFixedTime's own
        /// fixedTimeOffset parameter - where IN the target clip playback
        /// begins, in seconds, not a transition-blend value. 0 (the default)
        /// is the existing behaviour for every other caller - starts at the
        /// beginning of the clip, unchanged.
        /// </param>
        public bool BeginSustainedAction(string id, float blendIn = 0.15f, float weight = 1f, float startTimeSeconds = 0f)
        {
            if (animator == null || _fullBodyLayer < 0) return false;
            if (!_byId.TryGetValue(id, out _)) return false;

            int hash = Animator.StringToHash(id);
            if (!animator.HasState(_fullBodyLayer, hash)) return false;

            if (_fullBodyFade != null) { StopCoroutine(_fullBodyFade); _fullBodyFade = null; }
            _fullBodySustained = true;
            _fullBodyWeight = Mathf.Clamp01(weight);
            animator.SetLayerWeight(_fullBodyLayer, _fullBodyWeight);
            animator.CrossFadeInFixedTime(id, blendIn, _fullBodyLayer, startTimeSeconds);
            return true;
        }

        /// <summary>
        /// Retunes the weight of the pose currently being held, WITHOUT
        /// restarting it. Needed because a held pose is only re-issued when the
        /// chosen pose changes, so a caller that wants to blend the pose in
        /// partially (and keep adjusting that blend live, e.g. from a tuner
        /// slider) has no other way to reach the layer weight.
        ///
        /// A partial weight is not a cosmetic dial: the FullBodyOverride layer
        /// overrides, so weight 0.5 means the held clip's pose is mixed half
        /// and half with whatever the locomotion layers underneath are doing.
        /// That is the point - it lets an authored pose contribute its flavour
        /// without imposing its full body angle.
        ///
        /// Ignored unless a sustained pose is actually active, so it cannot
        /// fight the fade-out coroutine on the way back down.
        /// </summary>
        public void SetSustainedWeight(float weight)
        {
            if (!_fullBodySustained || animator == null || _fullBodyLayer < 0) return;
            if (_fullBodyFade != null) return;
            _fullBodyWeight = Mathf.Clamp01(weight);
            animator.SetLayerWeight(_fullBodyLayer, _fullBodyWeight);
        }

        /// <summary>True while a pose is being held via BeginSustainedAction.</summary>
        public bool IsSustaining => _fullBodySustained;

        /// <summary>
        /// Starts holding <paramref name="id"/> on the blend layer above the
        /// sustained pose. Deliberately does NOT touch the layer weight - the
        /// caller ramps that itself via <see cref="SetBlendedWeight"/>, so the
        /// blend can both ease in and be retuned live (from a tuner slider, or
        /// tracked against a vehicle's live angle) without the clip restarting.
        /// </summary>
        public bool PlayBlendedPose(string id, float blendIn = 0.15f)
        {
            if (animator == null || _blendLayer < 0) return false;
            if (!_byId.TryGetValue(id, out _)) return false;

            int hash = Animator.StringToHash(id);
            if (!animator.HasState(_blendLayer, hash)) return false;

            _blendActive = true;
            animator.CrossFadeInFixedTime(id, blendIn, _blendLayer, 0f);
            return true;
        }

        /// <summary>
        /// Sets how much of the blended pose shows through, 0 = none (the
        /// sustained pose underneath is what you see) to 1 = fully replaced.
        /// </summary>
        public void SetBlendedWeight(float weight)
        {
            if (animator == null || _blendLayer < 0) return;
            animator.SetLayerWeight(_blendLayer, Mathf.Clamp01(weight));
        }

        /// <summary>Current blend-layer weight, so a caller ramping it does not
        /// need to keep its own duplicate of the value.</summary>
        public float BlendedWeight =>
            animator != null && _blendLayer >= 0 ? animator.GetLayerWeight(_blendLayer) : 0f;

        public bool IsBlending => _blendActive;

        /// <summary>Drops the blended pose. Expects the caller to have already
        /// ramped the weight down; the weight is zeroed here regardless so a
        /// dismount mid-blend cannot leave the pose stuck on.</summary>
        public void StopBlendedPose()
        {
            if (!_blendActive || animator == null || _blendLayer < 0) return;
            _blendActive = false;
            animator.SetLayerWeight(_blendLayer, 0f);
            if (animator.HasState(_blendLayer, Animator.StringToHash(EmptyStateName)))
            {
                animator.CrossFadeInFixedTime(EmptyStateName, 0.05f, _blendLayer, 0f);
            }
        }

        public void EndSustainedAction()
        {
            StopBlendedPose();
            if (!_fullBodySustained || animator == null) return;
            _fullBodySustained = false;
            if (_fullBodyFade != null) StopCoroutine(_fullBodyFade);
            _fullBodyFade = StartCoroutine(FadeLayerOut(_fullBodyLayer));
        }

        private IEnumerator FadeLayerAfter(int layer, float delaySeconds)
        {
            yield return new WaitForSeconds(Mathf.Max(0.05f, delaySeconds));
            yield return FadeLayerOut(layer);
        }

        private IEnumerator FadeLayerOut(int layer)
        {
            float start = animator.GetLayerWeight(layer);
            float t = 0f;
            while (t < blendOutSeconds)
            {
                t += Time.deltaTime;
                animator.SetLayerWeight(layer, Mathf.Lerp(start, 0f, t / blendOutSeconds));
                yield return null;
            }
            animator.SetLayerWeight(layer, 0f);
            if (animator.HasState(layer, Animator.StringToHash(EmptyStateName)))
            {
                animator.CrossFadeInFixedTime(EmptyStateName, 0.05f, layer, 0f);
            }
        }
    }
}
