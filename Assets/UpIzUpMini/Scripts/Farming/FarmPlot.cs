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
        [SerializeField] private int harvestYield = 3;

        private PlotState _state = PlotState.Empty;
        private CropDefinition _crop;
        private CropStageVisual _activeVisual;
        private float _growTimer;
        private string _lastFeedback;

        [SerializeField] private int seedsPerClone = 2;
        [SerializeField] private float cloneCooldownSeconds = 60f;
        [Tooltip("Condition lost by the parent plant each time cuttings are taken.")]
        [SerializeField] private float cloneQualityLoss = 0.2f;
        [SerializeField] private float minCloneQuality = 0.3f;

        [Header("Freshness")]
        [Tooltip("Seconds after ripening before the crop is worth the minimum.")]
        [SerializeField] private float spoilSeconds = 120f;
        [Tooltip("Fraction of full value a fully-spoiled crop still fetches.")]
        [SerializeField] private float minFreshness = 0.35f;

        private float _cloneReadyAt;
        private float _ripeSince = -1f;
        private int _lastHarvestYield;
        private float _lastFreshness = 1f;
        private float _cloneQuality = 1f;

        /// <summary>
        /// R clones a mature plant: takes cuttings for extra seed without
        /// harvesting the crop, so the farm can be expanded from one buy.
        /// Rate-limited so a single ripe plant isn't an infinite seed tap.
        /// </summary>
        public bool CanClone => _state == PlotState.Ripe
                                && _crop != null
                                && Time.time >= _cloneReadyAt;
        public bool IsRipe => _state == PlotState.Ripe;
        public CropDefinition CurrentCrop => _crop;

        /// <summary>True while cuttings are still recovering.</summary>
        public bool IsCloning => _cloneReadyAt > 0f && Time.time < _cloneReadyAt;

        public string CloneLabel
        {
            get
            {
                if (_state != PlotState.Ripe || _crop == null) return null;
                if (CanClone) return "[ R ] Clone for seed";
                int wait = Mathf.CeilToInt(_cloneReadyAt - Time.time);
                return $"Cloning... {wait}s   (cannot harvest)";
            }
        }

        /// <summary>
        /// Crop value falls the longer it is left standing after ripening,
        /// so harvesting promptly is rewarded.
        /// </summary>
        public float Freshness
        {
            get
            {
                if (_ripeSince < 0f) return 1f;
                float overdue = Time.time - _ripeSince;
                float t = Mathf.Clamp01(overdue / Mathf.Max(1f, spoilSeconds));
                // Cloning damage compounds with age.
                return Mathf.Lerp(1f, minFreshness, t) * _cloneQuality;
            }
        }

        /// <summary>
        /// True when a farmhand could usefully act here: an empty plot to
        /// plant, dry soil to water, or ripe crop to harvest. Growing plots
        /// and plots mid-cloning are skipped.
        /// </summary>
        public bool NeedsFarmhandAttention =>
            _state == PlotState.Empty
            || _state == PlotState.PlantedDry
            || (_state == PlotState.Ripe && !IsCloning);

        public string Clone()
        {
            if (_state != PlotState.Ripe || _crop == null) return null;

            if (Time.time < _cloneReadyAt)
            {
                int wait = Mathf.CeilToInt(_cloneReadyAt - Time.time);
                _lastFeedback = $"Plant need time to recover - {wait}s.";
                return _lastFeedback;
            }

            EconomyManager.Instance?.AddSeeds(_crop.cropId, seedsPerClone);
            _cloneReadyAt = Time.time + cloneCooldownSeconds;

            // Taking cuttings costs the parent plant condition, so cloning
            // repeatedly trades crop quality for seed.
            _cloneQuality = Mathf.Max(minCloneQuality, _cloneQuality - cloneQualityLoss);

            _lastFeedback =
                $"Took cuttings - {seedsPerClone} more {_crop.displayName} seed. " +
                $"Plant quality now {Mathf.RoundToInt(_cloneQuality * 100f)}%.";
            return _lastFeedback;
        }

        /// <summary>
        /// Automated farm work represents taking the cutting as part of
        /// the harvest action. Manual R cloning keeps its 60-second plant
        /// recovery, but the assigned helper must not stand idle for a
        /// minute and appear unable to harvest.
        /// </summary>
        public bool FarmhandCloneBeforeHarvest()
        {
            if (!CanClone) return false;
            string result = Clone();
            if (string.IsNullOrEmpty(result)) return false;
            _cloneReadyAt = 0f;
            return true;
        }

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

            if (t >= 1f)
            {
                _state = PlotState.Ripe;
                if (_ripeSince < 0f) _ripeSince = Time.time;
            }
        }

        public override void Interact(GameObject interactor)
        {
            InteractInternal(interactor, null);
        }

        public void FarmhandInteract(GameObject interactor, CropDefinition assignedCrop)
        {
            InteractInternal(interactor, assignedCrop);
        }

        private void InteractInternal(GameObject interactor, CropDefinition assignedCrop)
        {
            switch (_state)
            {
                case PlotState.Empty:
                    var crop = assignedCrop != null
                        ? assignedCrop
                        : (CropSelectionController.Instance != null ? CropSelectionController.Instance.Selected : null);
                    if (crop == null) return;

                    // Planting costs a seed - buy more from the shopkeeper,
                    // or get them back by harvesting.
                    if (EconomyManager.Instance != null && !EconomyManager.Instance.TryConsumeSeed(crop.cropId))
                    {
                        _lastFeedback = $"No {crop.displayName} seed left. Buy some by the shop, nuh.";
                        return;
                    }

                    _crop = crop;
                    _state = PlotState.PlantedDry;
                    Missions.MissionSystem.Instance?.Notify(
                        Missions.ObjectiveKind.PlantCrop, crop.cropId);

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
                    Missions.MissionSystem.Instance?.Notify(
                        Missions.ObjectiveKind.WaterAny, _crop.cropId);
                    break;

                case PlotState.Growing:
                    _lastFeedback = "Still growing. Give it time.";
                    break;

                case PlotState.Ripe:
                    if (IsCloning)
                    {
                        int wait = Mathf.CeilToInt(_cloneReadyAt - Time.time);
                        _lastFeedback = $"Cuttings still taking - cannot harvest for {wait}s.";
                        return;
                    }

                    if (EconomyManager.Instance != null && _crop != null)
                    {
                        // Leaving a ripe plant standing costs yield.
                        float freshness = Freshness;
                        int yield = Mathf.Max(1, Mathf.RoundToInt(harvestYield * freshness));
                        EconomyManager.Instance.AddCrop(_crop.cropId, yield);
                        _lastHarvestYield = yield;
                        _lastFreshness = freshness;
                        // Seed comes only from taking cuttings (R), never
                        // from harvesting - so expanding the farm means
                        // cloning, which costs plant quality.
                        if (_crop.isIllegal) EconomyManager.Instance.AddHeat(4f);
                    }
                    if (_crop != null)
                    {
                        string quality = _lastFreshness > 0.85f ? "Prime."
                            : _lastFreshness > 0.6f ? "Still good."
                            : "It sit too long - worth less now.";
                        _lastFeedback =
                            $"Harvested {_lastHarvestYield} {_crop.displayName}. {quality}";

                        Missions.MissionSystem.Instance?.NotifyCount(
                            Missions.ObjectiveKind.HarvestCrop, _crop.cropId, _lastHarvestYield);
                    }
                    else
                    {
                        _lastFeedback = "Harvested.";
                    }
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
            _ripeSince = -1f;
            _cloneReadyAt = 0f;
            _cloneQuality = 1f;
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
