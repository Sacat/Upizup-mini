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
    /// </summary>
    public class CropStageVisual : MonoBehaviour
    {
        [SerializeField] private Transform plantRoot;
        [SerializeField] private Renderer plantRenderer;
        [SerializeField] private Renderer[] fruitRenderers;
        [SerializeField] private float fullScale = 1f;

        private MaterialPropertyBlock _block;

        /// <summary>Stage 0..3: seedling, small, grown (green fruit), ripe (red fruit).</summary>
        public void ApplyStage(int stage, Color unripeColor, Color ripeColor)
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
            Color fruitColor = stage >= 3 ? ripeColor : unripeColor;

            if (fruitRenderers == null) return;
            foreach (var r in fruitRenderers)
            {
                if (r == null) continue;
                r.gameObject.SetActive(showFruit);
                if (!showFruit) continue;

                _block ??= new MaterialPropertyBlock();
                r.GetPropertyBlock(_block);
                _block.SetColor("_Color", fruitColor);
                _block.SetColor("_BaseColor", fruitColor);
                r.SetPropertyBlock(_block);
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
