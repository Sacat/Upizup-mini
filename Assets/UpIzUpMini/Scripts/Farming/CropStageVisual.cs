using UnityEngine;

namespace UpIzUpMini.Farming
{
    /// <summary>
    /// Visual for a growing crop: a real decimated plant mesh (from the
    /// larger project's photogrammetry scans - see
    /// Mini012CropAssetBuilder) that scales through discrete growth
    /// stages, plus separate fruit spheres that ripen green -> red.
    ///
    /// The source scan is a single submesh, so the fruit built into the
    /// mesh can't be tinted independently of the leaves. Rather than tint
    /// the whole plant red (which would recolour the foliage too), ripeness
    /// is shown with dedicated fruit objects layered on the plant. Those
    /// are hidden until the plant is grown enough to bear fruit.
    ///
    /// MINI-051: for Zeb (weed), "fruit" means bud/cola clusters, not a
    /// single tinted sphere. Added two more optional layers so a strain's
    /// designated colour actually reads: pistil accents (the hair-like
    /// strands real buds carry - cream while flowering, rust-orange once
    /// mature, independent of the strain's own bud colour) and an optional
    /// frost/trichome tint applied to the bud colour itself at full
    /// ripeness (a slight lighten + a glossier surface, standing in for
    /// the frosted-crystal look of a ripe cola without needing a custom
    /// shader). Both are opt-in via serialized arrays/flag so tomato/
    /// banana/carrot, which don't set them, are visually unchanged.
    /// </summary>
    public class CropStageVisual : MonoBehaviour
    {
        [SerializeField] private Transform plantRoot;
        [SerializeField] private Renderer plantRenderer;
        [SerializeField] private Renderer[] fruitRenderers;
        [SerializeField] private float fullScale = 1f;

        [Header("MINI-051 bud detail (weed only - leave empty elsewhere)")]
        [Tooltip("Pistil-hair accents. Cream while flowering, rust-orange once ripe, independent of the strain's own bud colour.")]
        [SerializeField] private Renderer[] pistilRenderers;
        [Tooltip("Lightens ripe bud colour slightly and adds a glossier surface, standing in for a frosted/trichome look.")]
        [SerializeField] private bool applyFrostEffect;

        private static readonly Color PistilCream = new Color(0.90f, 0.85f, 0.72f);
        private static readonly Color PistilMature = new Color(0.74f, 0.36f, 0.12f);
        private const float FrostBlend = 0.22f;
        private const float FrostSmoothness = 0.62f;

        private MaterialPropertyBlock _block;

        /// <summary>Stage 0..3: seedling, small, grown (green fruit), ripe (red fruit).</summary>
        public void ApplyStage(int stage, Color unripeColor, Color ripeColor)
            => ApplyStage(stage, unripeColor, ripeColor, default);

        /// <summary>
        /// MINI-047 overload. When <paramref name="secondaryRipeColor"/> has
        /// alpha > 0 (CropDefinition.HasSecondaryRipeColor), ripe fruit
        /// alternates between <paramref name="ripeColor"/> and it by index
        /// instead of a single flat colour - Purple Black's orange+purple
        /// buds, showing both parent strains at once rather than a blended
        /// third colour.
        /// </summary>
        public void ApplyStage(int stage, Color unripeColor, Color ripeColor, Color secondaryRipeColor)
        {
            if (plantRoot != null)
            {
                float t = stage switch
                {
                    0 => 0.28f,
                    1 => 0.55f,
                    2 => 0.85f,
                    _ => 1f
                };
                plantRoot.localScale = Vector3.one * (fullScale * t);
            }

            bool showFruit = stage >= 2;
            bool ripe = stage >= 3;
            bool twoTone = ripe && secondaryRipeColor.a > 0.001f;

            _block ??= new MaterialPropertyBlock();

            if (fruitRenderers != null)
            {
                for (int i = 0; i < fruitRenderers.Length; i++)
                {
                    var r = fruitRenderers[i];
                    if (r == null) continue;
                    r.gameObject.SetActive(showFruit);
                    if (!showFruit) continue;

                    Color fruitColor = !ripe
                        ? unripeColor
                        : (twoTone && i % 2 == 1 ? secondaryRipeColor : ripeColor);

                    if (ripe && applyFrostEffect)
                        fruitColor = Color.Lerp(fruitColor, Color.white, FrostBlend);

                    r.GetPropertyBlock(_block);
                    _block.SetColor("_Color", fruitColor);
                    _block.SetColor("_BaseColor", fruitColor);
                    if (ripe && applyFrostEffect)
                        _block.SetFloat("_Glossiness", FrostSmoothness);
                    r.SetPropertyBlock(_block);
                }
            }

            if (pistilRenderers != null)
            {
                Color pistilColor = ripe ? PistilMature : PistilCream;
                for (int i = 0; i < pistilRenderers.Length; i++)
                {
                    var r = pistilRenderers[i];
                    if (r == null) continue;
                    r.gameObject.SetActive(showFruit);
                    if (!showFruit) continue;

                    r.GetPropertyBlock(_block);
                    _block.SetColor("_Color", pistilColor);
                    _block.SetColor("_BaseColor", pistilColor);
                    r.SetPropertyBlock(_block);
                }
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
