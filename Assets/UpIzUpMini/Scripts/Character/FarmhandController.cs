using System.Collections.Generic;
using UnityEngine;
using UpIzUpMini.Farming;
using UpIzUpMini.Economy;

namespace UpIzUpMini.Character
{
    /// <summary>
    /// Lets the inactive boy be left at the farm to work it.
    ///
    /// Press G near the farm to leave your companion behind; he stops
    /// following and instead walks between plots, planting, watering and
    /// harvesting on his own. Press G again (or get close) to pick him back
    /// up. This is the "leave one at the farm" behaviour - it does not
    /// clone, so seed still only comes from the player taking cuttings.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FarmhandController : MonoBehaviour
    {
        [SerializeField] private float workSpeed = 2.0f;
        [SerializeField] private float reachDistance = 1.8f;
        [SerializeField] private float actionCooldown = 1.6f;
        [SerializeField] private float turnSpeed = 8f;
        [SerializeField] private Animator animator;

        private CharacterController _controller;
        private FollowController _follow;
        private FarmPlot _target;
        private float _cooldown;
        private float _animBlend;
        private readonly List<FarmPlot> _workPlots = new List<FarmPlot>(3);
        private FarmPlot _cloneMother;

        /// <summary>True while stationed at the farm instead of following.</summary>
        public bool IsWorking { get; private set; }
        public CropDefinition AssignedCrop { get; private set; }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _follow = GetComponent<FollowController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        public void SetWorking(bool working, CropDefinition crop = null)
        {
            bool newAssignment = working && (!IsWorking || (crop != null && crop != AssignedCrop));
            IsWorking = working;
            if (working && crop != null) AssignedCrop = crop;
            if (newAssignment)
            {
                _workPlots.Clear();
                _cloneMother = null;
                _target = null;
            }
            // Working and following are mutually exclusive.
            if (_follow != null) _follow.FollowingEnabled = !working;
            if (!working)
            {
                _target = null;
                _workPlots.Clear();
                _cloneMother = null;
            }
        }

        public void ToggleWorking(CropDefinition crop = null) => SetWorking(!IsWorking, crop);

        private void Update()
        {
            if (!IsWorking) return;

            if (_cooldown > 0f) _cooldown -= Time.deltaTime;

            if (_target == null || !NeedsWork(_target))
            {
                _target = FindPlotNeedingWork();
            }

            if (_target == null)
            {
                Animate(0f);
                return;
            }

            Vector3 toPlot = _target.transform.position - transform.position;
            toPlot.y = 0f;

            if (toPlot.magnitude > reachDistance)
            {
                Vector3 dir = toPlot.normalized;
                _controller.SimpleMove(dir * workSpeed);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
                Animate(workSpeed);
                return;
            }

            Animate(0f);

            if (_cooldown <= 0f)
            {
                // Preserve the crop cycle: when only one matching seed is
                // left, take cuttings before harvesting. Clone adds seed
                // and temporarily protects the parent from harvest; once
                // it recovers, the farmhand harvests and replants.
                bool isMother = _target == _cloneMother;
                if (isMother && _target.IsRipe && _target.CanClone && _target.CurrentCrop != null
                    && AssignedCrop != null
                    && _target.CurrentCrop.cropId == AssignedCrop.cropId
                    && EconomyManager.Instance != null
                    && EconomyManager.Instance.GetSeeds(AssignedCrop.cropId) <= 1)
                {
                    _target.FarmhandCloneBeforeHarvest();
                    _target = null;
                    _cooldown = actionCooldown;
                    return;
                }

                // The two production plots are harvested normally. The
                // third plot is never harvested; it remains the mother.
                _target.FarmhandInteract(gameObject, AssignedCrop);
                _target = null;
                _cooldown = actionCooldown;
            }
        }

        private static bool NeedsWork(FarmPlot plot)
        {
            if (plot == null || !plot.isActiveAndEnabled) return false;
            // Growing plots need nothing, and a cloning plot can't be
            // harvested - skip both.
            return plot.NeedsFarmhandAttention;
        }

        private FarmPlot FindPlotNeedingWork()
        {
            EnsureThreePlots();
            if (_workPlots.Count == 0) return null;

            // Water first so every planted plot progresses.
            foreach (var plot in _workPlots) if (plot != null && plot.IsDry) return plot;

            // Harvest the first two as soon as each becomes ripe.
            foreach (var plot in _workPlots)
            {
                if (plot != null && plot != _cloneMother && plot.IsRipe && !plot.IsCloning) return plot;
            }

            // The third plant is the permanent seed mother. Clone it when
            // the two replacement plots need seed, but never harvest it.
            if (_cloneMother != null && _cloneMother.IsRipe && _cloneMother.CanClone
                && AssignedCrop != null && EconomyManager.Instance != null
                && EconomyManager.Instance.GetSeeds(AssignedCrop.cropId) <= 1)
                return _cloneMother;

            // Replant only the two production plots, keeping exactly three
            // plants in the assignment rather than spreading endlessly.
            if (AssignedCrop != null && EconomyManager.Instance != null
                && EconomyManager.Instance.GetSeeds(AssignedCrop.cropId) > 0)
            {
                foreach (var plot in _workPlots)
                    if (plot != null && plot != _cloneMother && plot.IsEmpty) return plot;
                if (_cloneMother != null && _cloneMother.IsEmpty) return _cloneMother;
            }
            return null;
        }

        private void EnsureThreePlots()
        {
            _workPlots.RemoveAll(p => p == null || !p.isActiveAndEnabled);
            if (_workPlots.Count >= 3)
            {
                if (_cloneMother == null) _cloneMother = _workPlots[2];
                return;
            }

            var all = Object.FindObjectsByType<FarmPlot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            System.Array.Sort(all, (a, b) =>
                Vector3.SqrMagnitude(a.transform.position - transform.position)
                    .CompareTo(Vector3.SqrMagnitude(b.transform.position - transform.position)));
            foreach (var plot in all)
            {
                if (plot == null || !plot.isActiveAndEnabled || _workPlots.Contains(plot)) continue;
                _workPlots.Add(plot);
                if (_workPlots.Count == 3) break;
            }
            if (_workPlots.Count >= 3) _cloneMother = _workPlots[2];
        }

        private void Animate(float speed)
        {
            if (animator == null) return;
            _animBlend = Mathf.Lerp(_animBlend, speed, 10f * Time.deltaTime);
            if (_animBlend < 0.01f) _animBlend = 0f;
            animator.SetFloat("Speed", _animBlend);
            animator.SetFloat("MotionSpeed", speed > 0.01f ? 1f : 0f);
            animator.SetBool("Grounded", true);
        }
    }
}
