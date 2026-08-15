using UnityEngine;
using UpIzUpMini.Economy;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.Farming
{
    public enum PlotState { Empty, PlantedDry, Growing, Ripe }

    /// <summary>
    /// One farm plot: [E] Plant (using the currently selected crop from
    /// CropSelectionController) -> [E] Water -> grows through 3 visual
    /// stages -> [E] Harvest -> back to Empty. Soil is light brown while
    /// Empty, dark brown once planted (matches the user's explicit
    /// before/after soil colour request); the crop mesh scales up and
    /// tints from unripe to ripe colour as it grows.
    /// </summary>
    public class FarmPlot : InteractableBase
    {
        [SerializeField] private Renderer soilRenderer;
        [SerializeField] private Transform cropVisualRoot;
        [SerializeField] private Renderer cropRenderer;
        [SerializeField] private Color soilEmptyColor = new Color(0.62f, 0.5f, 0.35f);
        [SerializeField] private Color soilPlantedColor = new Color(0.28f, 0.18f, 0.1f);

        private PlotState _state = PlotState.Empty;
        private CropDefinition _crop;
        private float _growTimer;
        private string _lastFeedback;

        public override string PromptLabel => _state switch
        {
            PlotState.Empty => CropSelectionController.Instance != null && CropSelectionController.Instance.Selected != null
                ? $"[ E ] Plant {CropSelectionController.Instance.Selected.displayName}"
                : "[ E ] Plant",
            PlotState.PlantedDry => "[ E ] Water",
            PlotState.Growing => "[ E ] Tend",
            PlotState.Ripe => "[ E ] Harvest",
            _ => "[ E ]"
        };

        private void Update()
        {
            if (_state != PlotState.Growing || _crop == null) return;

            _growTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_growTimer / Mathf.Max(0.1f, _crop.growDurationSeconds));
            ApplyGrowthVisual(t);

            if (t >= 1f)
            {
                _state = PlotState.Ripe;
            }
        }

        public override void Interact(GameObject interactor)
        {
            switch (_state)
            {
                case PlotState.Empty:
                    var crop = CropSelectionController.Instance != null ? CropSelectionController.Instance.Selected : null;
                    if (crop == null) return;
                    _crop = crop;
                    _state = PlotState.PlantedDry;
                    SetSoilColor(soilPlantedColor);
                    ApplyGrowthVisual(0f);
                    if (cropVisualRoot != null) cropVisualRoot.gameObject.SetActive(true);
                    _lastFeedback = $"Planted {crop.displayName}. Needs water.";
                    break;

                case PlotState.PlantedDry:
                    _state = PlotState.Growing;
                    _growTimer = 0f;
                    _lastFeedback = $"Watered. {_crop.displayName} is growing.";
                    break;

                case PlotState.Growing:
                    _lastFeedback = "Still growing.";
                    break;

                case PlotState.Ripe:
                    if (EconomyManager.Instance != null && _crop != null)
                    {
                        EconomyManager.Instance.AddCrop(_crop.cropId, 1);
                        if (_crop.isIllegal) EconomyManager.Instance.AddHeat(4f);
                    }
                    _lastFeedback = _crop != null ? $"Harvested {_crop.displayName}." : "Harvested.";
                    ResetPlot();
                    break;
            }
        }

        public override string GetInteractionFeedback() => _lastFeedback;

        private void ResetPlot()
        {
            _state = PlotState.Empty;
            _crop = null;
            _growTimer = 0f;
            SetSoilColor(soilEmptyColor);
            if (cropVisualRoot != null) cropVisualRoot.gameObject.SetActive(false);
        }

        private void ApplyGrowthVisual(float t)
        {
            if (cropVisualRoot != null)
            {
                float scale = Mathf.Lerp(0.15f, 1f, t);
                cropVisualRoot.localScale = Vector3.one * scale;
            }
            if (cropRenderer != null && _crop != null)
            {
                // Stay unripe-coloured for most of growth, only ripen in
                // the final third - gives a clear 3rd visual stage.
                float colorT = Mathf.Clamp01((t - 0.66f) / 0.34f);
                cropRenderer.material.color = Color.Lerp(_crop.unripeColor, _crop.ripeColor, colorT);
            }
        }

        private void SetSoilColor(Color color)
        {
            if (soilRenderer != null) soilRenderer.material.color = color;
        }
    }
}
