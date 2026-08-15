using UnityEngine;
using UpIzUpMini.Economy;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.Farming
{
    public enum PlotState { Empty, PlantedDry, Growing, Ripe }

    /// <summary>
    /// One farm plot: [E] Plant (using the crop selected with keys 1-4)
    /// -> [E] Water -> grows through 4 visible stages -> [E] Harvest.
    ///
    /// Soil colours are taken from the larger Up Iz Up project's
    /// CropPatch.cs so the two games match: dry soil is light brown, and
    /// watering visibly darkens it.
    /// </summary>
    public class FarmPlot : InteractableBase
    {
        // Matches CropPatch.DrySoilColor / WateredSoilColor in E:\Unity\Up iz up.
        private static readonly Color DrySoilColor = new Color(0.52f, 0.32f, 0.16f, 1f);
        private static readonly Color WateredSoilColor = new Color(0.23f, 0.105f, 0.045f, 1f);

        [SerializeField] private Renderer soilRenderer;
        [SerializeField] private CropStageVisual tomatoVisual;
        [SerializeField] private CropStageVisual weedVisual;

        private PlotState _state = PlotState.Empty;
        private CropDefinition _crop;
        private CropStageVisual _activeVisual;
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

        private void Start()
        {
            SetSoilColor(DrySoilColor);
            if (tomatoVisual != null) tomatoVisual.SetVisible(false);
            if (weedVisual != null) weedVisual.SetVisible(false);
        }

        private void Update()
        {
            if (_state != PlotState.Growing || _crop == null) return;

            _growTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_growTimer / Mathf.Max(0.1f, _crop.growDurationSeconds));

            // 4 discrete stages rather than a continuous scale, so growth
            // reads as a plant developing rather than something inflating.
            int stage = t >= 1f ? 3 : Mathf.Clamp(Mathf.FloorToInt(t * 3f), 0, 2);
            _activeVisual?.ApplyStage(stage, _crop.unripeColor, _crop.ripeColor);

            if (t >= 1f) _state = PlotState.Ripe;
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

                    _activeVisual = crop.isIllegal ? weedVisual : tomatoVisual;
                    if (tomatoVisual != null) tomatoVisual.SetVisible(_activeVisual == tomatoVisual);
                    if (weedVisual != null) weedVisual.SetVisible(_activeVisual == weedVisual);
                    _activeVisual?.ApplyStage(0, crop.unripeColor, crop.ripeColor);

                    _lastFeedback = $"Planted {crop.displayName}. It need water, nuh.";
                    break;

                case PlotState.PlantedDry:
                    _state = PlotState.Growing;
                    _growTimer = 0f;
                    SetSoilColor(WateredSoilColor);
                    _lastFeedback = $"Watered. {_crop.displayName} growing now.";
                    break;

                case PlotState.Growing:
                    _lastFeedback = "Still growing. Give it time.";
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

        /// <summary>Save/load support - see SaveLoadSystem.</summary>
        public int GetSaveState() => (int)_state;
        public string GetSaveCropId() => _crop != null ? _crop.cropId : string.Empty;
        public float GetSaveTimer() => _growTimer;

        public void LoadState(int state, CropDefinition crop, float timer)
        {
            _crop = crop;
            _state = (PlotState)state;
            _growTimer = timer;

            if (_crop == null || _state == PlotState.Empty)
            {
                ResetPlot();
                return;
            }

            _activeVisual = _crop.isIllegal ? weedVisual : tomatoVisual;
            if (tomatoVisual != null) tomatoVisual.SetVisible(_activeVisual == tomatoVisual);
            if (weedVisual != null) weedVisual.SetVisible(_activeVisual == weedVisual);

            SetSoilColor(_state == PlotState.PlantedDry ? DrySoilColor : WateredSoilColor);

            float t = Mathf.Clamp01(_growTimer / Mathf.Max(0.1f, _crop.growDurationSeconds));
            int stage = _state == PlotState.Ripe ? 3 : Mathf.Clamp(Mathf.FloorToInt(t * 3f), 0, 2);
            _activeVisual?.ApplyStage(stage, _crop.unripeColor, _crop.ripeColor);
        }

        private void ResetPlot()
        {
            _state = PlotState.Empty;
            _crop = null;
            _growTimer = 0f;
            SetSoilColor(DrySoilColor);
            if (tomatoVisual != null) tomatoVisual.SetVisible(false);
            if (weedVisual != null) weedVisual.SetVisible(false);
            _activeVisual = null;
        }

        private void SetSoilColor(Color color)
        {
            if (soilRenderer != null) soilRenderer.material.color = color;
        }
    }
}
