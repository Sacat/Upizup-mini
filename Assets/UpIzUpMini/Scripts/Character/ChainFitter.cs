using UnityEngine;

namespace UpIzUpMini.Character
{
    /// <summary>
    /// MINI-067 fitting aid. Drives an accessory's placement from four numbers
    /// expressed in the CHARACTER's frame - forward / up / side / tilt - and
    /// applies them live in the Editor, so the chain can be fitted with
    /// Inspector sliders instead of fought with move handles.
    ///
    /// Why sliders and not just dragging the transform: the object is parented
    /// to the chest BONE, whose rest orientation is not world-aligned on this
    /// rig, so the Inspector's local position/rotation values are in a skewed
    /// frame and mean nothing useful. These four do mean something, and they
    /// are exactly the constants CharacterEquipment applies at runtime - so
    /// whatever is dialled in here can be baked verbatim.
    ///
    /// Reference geometry (real puffed mariner / "Gucci link" chains run
    /// 20-30in with 7-11mm links): this model's loop is ~1.0m around at 0.27m
    /// wide, i.e. roughly a 39in chain - deliberately oversized. Worn properly
    /// its top arc passes behind the NAPE and the bottom hangs at mid-chest,
    /// which over a ~0.36m drop forces a tilt near 50deg. That is why the
    /// defaults below start there rather than at zero.
    /// </summary>
    [ExecuteAlways]
    public class ChainFitter : MonoBehaviour
    {
        [Header("Drag these - live in the Editor")]
        [Tooltip("Forward of the chest bone, in metres. NEGATIVE puts the top of the loop behind the neck, at the nape.")]
        [Range(-0.40f, 0.40f)] public float forward = -0.10f;

        [Tooltip("Above the chest bone, in metres. This is where the TOP of the loop pins; the chain hangs down from it.")]
        [Range(-0.20f, 0.70f)] public float up = 0.34f;

        [Tooltip("Sideways, in metres. Should normally stay 0.")]
        [Range(-0.20f, 0.20f)] public float side = 0f;

        [Tooltip("Pitch about the character's right axis. POSITIVE swings the bottom of the loop forward onto the chest - needed because the model is a flat loop and the chest curves outward.")]
        [Range(-90f, 90f)] public float tilt = 50f;

        [Tooltip("Overall width of the chain across the body, in metres. 0.27 is roughly a 39in chain on these ~1.8m characters; 0.18 is nearer a real 26in.")]
        [Range(0.05f, 0.60f)] public float width = 0.27f;

        [Header("Auto-found if left empty")]
        [SerializeField] private Transform characterRoot;
        [SerializeField] private Transform chestBone;

        private float _authoredWidth = -1f;

        private void OnEnable() => Resolve();
        private void OnValidate() => Resolve();

        private void Resolve()
        {
            if (chestBone == null) chestBone = transform.parent;

            if (characterRoot == null && chestBone != null)
            {
                // Walk up to the object carrying the Animator - that is the
                // character, and its rotation is the sane frame to work in.
                var anim = chestBone.GetComponentInParent<Animator>();
                if (anim != null) characterRoot = anim.transform.root;
            }

            // Capture the prefab's authored width once, so the width slider is
            // an absolute size rather than a multiplier that drifts every time
            // the value is nudged.
            if (_authoredWidth <= 0f)
            {
                var r = GetComponentInChildren<Renderer>();
                if (r != null && transform.localScale.x > 0.0001f)
                {
                    _authoredWidth = r.bounds.size.x / transform.lossyScale.x;
                }
            }
        }

        private void LateUpdate() => Apply();

        /// <summary>
        /// Placed in LateUpdate so it wins against the animator posing the
        /// chest bone - otherwise the chain would lag a frame behind the body
        /// whenever the character moves.
        /// </summary>
        public void Apply()
        {
            if (characterRoot == null || chestBone == null) return;

            transform.rotation = characterRoot.rotation * Quaternion.Euler(tilt, 0f, 0f);
            transform.position = chestBone.position
                + characterRoot.forward * forward
                + characterRoot.up * up
                + characterRoot.right * side;

            if (_authoredWidth > 0.0001f)
            {
                float s = width / _authoredWidth;
                // Divide out the bone's own scale, so a scaled character does
                // not stretch the chain along with it.
                Vector3 parentScale = chestBone.lossyScale;
                transform.localScale = new Vector3(
                    parentScale.x > 0.0001f ? s / parentScale.x : s,
                    parentScale.y > 0.0001f ? s / parentScale.y : s,
                    parentScale.z > 0.0001f ? s / parentScale.z : s);
            }
        }
    }
}
