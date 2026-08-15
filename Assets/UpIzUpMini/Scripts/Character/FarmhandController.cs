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
            IsWorking = working;
            if (working && crop != null) AssignedCrop = crop;
            // Working and following are mutually exclusive.
            if (_follow != null) _follow.FollowingEnabled = !working;
            if (!working) _target = null;
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
                // Plant / water / harvest, whichever the plot needs next.
                _target.FarmhandInteract(gameObject, AssignedCrop);
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
            FarmPlot best = null;
            float bestDist = float.MaxValue;

            foreach (var plot in Object.FindObjectsByType<FarmPlot>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!NeedsWork(plot)) continue;
                float d = Vector3.Distance(transform.position, plot.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = plot;
                }
            }

            return best;
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
