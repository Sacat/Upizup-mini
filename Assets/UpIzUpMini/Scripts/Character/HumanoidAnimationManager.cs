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
        public bool PlayAction(string id, float blendIn = 0.08f)
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
            var fade = FadeLayerAfter(layer, clipLength + blendIn);
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
        public bool BeginSustainedAction(string id, float blendIn = 0.15f)
        {
            if (animator == null || _fullBodyLayer < 0) return false;
            if (!_byId.TryGetValue(id, out _)) return false;

            int hash = Animator.StringToHash(id);
            if (!animator.HasState(_fullBodyLayer, hash)) return false;

            if (_fullBodyFade != null) { StopCoroutine(_fullBodyFade); _fullBodyFade = null; }
            _fullBodySustained = true;
            animator.SetLayerWeight(_fullBodyLayer, 1f);
            animator.CrossFadeInFixedTime(id, blendIn, _fullBodyLayer, 0f);
            return true;
        }

        public void EndSustainedAction()
        {
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
