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
        // MINI-050: per-crop visual registry so a crop is not forced onto the
        // tomato/weed shape by legality alone (banana previously rendered as a
        // tomato plant recoloured yellow). Unregistered crops fall back to the
        // legal/illegal default, preserving every existing crop's look.
        [SerializeField] private CropVisualEntry[] cropVisuals = System.Array.Empty<CropVisualEntry>();
        [SerializeField] private int harvestYield = 3;

        private PlotState _state = PlotState.Empty;
        private CropDefinition _crop;
        private CropStageVisual _activeVisual;
        private float _growTimer;
        private string _lastFeedback;

        [System.Serializable]
        public struct CropVisualEntry
        {
            public string cropId;
            public CropStageVisual visual;
            public CropVisualEntry(string id, CropStageVisual v) { cropId = id; visual = v; }
        }

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
        public bool IsEmpty => _state == PlotState.Empty;
        public bool IsDry => _state == PlotState.PlantedDry;
        public CropDefinition CurrentCrop => _crop;

        /// <summary>MINI-059: a plot is only worth stealing from if there's
        /// an actual illegal (Zeb) crop growing or ready on it - dry soil,
        /// empty plots, and legal crops (tomato/banana/carrot) are never
        /// targets.</summary>
        public bool HasStealableZeb =>
            _crop != null && _crop.isIllegal && (_state == PlotState.Growing || _state == PlotState.Ripe);

        /// <summary>
        /// MINI-059: someone volehs (steals) the crop off this plot -
        /// a full loss, not a partial one, matching "unattended Zeb can be
        /// volehed" plainly. Returns the stolen crop's display name for
        /// the theft notification, or null if there was nothing stealable.
        /// </summary>
        public string Voleh()
        {
            if (!HasStealableZeb) return null;
            string name = _crop.displayName;
            ResetPlot();
            return name;
        }

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
            // The helper keeps this plant as a mother rather than
            // harvesting it. A short action recovery prevents repeated
            // cloning in the same frame without stalling the farm loop.
            _cloneReadyAt = Time.time + 2f;
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
            HideAllVisuals();
        }

        private void Update()
        {
            if (_state != PlotState.Growing || _crop == null) return;

            _growTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_growTimer / Mathf.Max(0.1f, _crop.growDurationSeconds));

            // 4 discrete stages rather than a continuous scale, so growth
            // reads as a plant developing rather than something inflating.
            int stage = t >= 1f ? 3 : Mathf.Clamp(Mathf.FloorToInt(t * 3f), 0, 2);
            _activeVisual?.ApplyStage(stage, _crop.unripeColor, _crop.ripeColor, _crop.secondaryRipeColor);

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

                    _activeVisual = VisualFor(_crop);
                    ApplyActiveVisual(0, _crop.unripeColor, _crop.ripeColor, _crop.secondaryRipeColor);

                    _lastFeedback = $"Planted {_crop.displayName}. It need water, nuh.";
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

            SetSoilColor(_state == PlotState.PlantedDry ? DrySoilColor : WateredSoilColor);

            float t = Mathf.Clamp01(_growTimer / Mathf.Max(0.1f, _crop.growDurationSeconds));
            int stage = _state == PlotState.Ripe ? 3 : Mathf.Clamp(Mathf.FloorToInt(t * 3f), 0, 2);
            _activeVisual = VisualFor(_crop);
            ApplyActiveVisual(stage, _crop.unripeColor, _crop.ripeColor, _crop.secondaryRipeColor);
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
            HideAllVisuals();
            _activeVisual = null;
        }

        /// <summary>
        /// Resolves which visual a crop should use. The per-crop registry wins
        /// (e.g. banana -> banana tree); anything unregistered falls back to
        /// weed for illegal crops and tomato for legal ones, matching the
        /// previous hardcoded behaviour so no existing crop's look changes.
        /// </summary>
        private CropStageVisual VisualFor(CropDefinition crop)
        {
            if (crop != null && cropVisuals != null)
            {
                for (int i = 0; i < cropVisuals.Length; i++)
                {
                    if (cropVisuals[i].visual != null && cropVisuals[i].cropId == crop.cropId)
                        return cropVisuals[i].visual;
                }
            }
            return crop != null && crop.isIllegal ? weedVisual : tomatoVisual;
        }

        private void ApplyActiveVisual(int stage, Color unripe, Color ripe, Color secondary)
        {
            HideAllVisuals();
            if (_activeVisual != null) _activeVisual.SetVisible(true);
            _activeVisual?.ApplyStage(stage, unripe, ripe, secondary);
        }

        private void HideAllVisuals()
        {
            if (tomatoVisual != null) tomatoVisual.SetVisible(false);
            if (weedVisual != null) weedVisual.SetVisible(false);
            if (cropVisuals != null)
            {
                for (int i = 0; i < cropVisuals.Length; i++)
                    if (cropVisuals[i].visual != null) cropVisuals[i].visual.SetVisible(false);
            }
        }

        private void SetSoilColor(Color color)
        {
            if (soilRenderer != null) soilRenderer.material.color = color;
        }
    }
}
