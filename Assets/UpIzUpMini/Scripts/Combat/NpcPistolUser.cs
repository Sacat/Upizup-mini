using UnityEngine;

namespace UpIzUpMini.Combat
{
    /// <summary>
    /// MINI-190: lets ANY humanoid NPC hold and use the sidearm with the same body animation as the playable characters.
    /// Pure animation/visual layer: it never deals damage or changes AI. Gameplay code (police, brawlers, missions) sets <see cref="Stance"/>,
    /// <see cref="AimTarget"/>, and calls <see cref="TriggerShot"/> / <see cref="BeginReload"/>.
    /// The weapon visual is instantiated from <see cref="weaponVisualPrefab"/> (grip / backstrap / shot-origin / support anchors included).
    /// </summary>
    [DisallowMultipleComponent]
    public class NpcPistolUser : MonoBehaviour, IPistolUser
    {
        public enum StanceKind { Holstered, LowReady, Aiming }

        [SerializeField] GameObject weaponVisualPrefab;
        [SerializeField] float reloadSeconds = 1.6f;
        [SerializeField] StanceKind stance = StanceKind.Holstered;

        public StanceKind Stance { get { return stance; } set { stance = value; } }
        public Transform AimTarget { get; set; }
        public Vector3 AimPointOffset = new Vector3(0f, 1.2f, 0f);

        Animator animator;
        FirearmPose pose;
        Transform weaponRoot, visual, rightHand, middle, thumb, index, grip, backstrap, shotOrigin;
        float reloadStart = -1f;
        Renderer[] renderers;
        LineRenderer flash;
        float flashUntil;

        public bool IsAiming { get { return stance == StanceKind.Aiming; } }
        public bool IsLowReady { get { return stance == StanceKind.LowReady; } }
        public bool IsReloading { get { return reloadStart >= 0f; } }
        public float ReloadProgress { get { return reloadStart < 0f ? 0f : Mathf.Clamp01((Time.time - reloadStart) / reloadSeconds); } }
        public Transform WeaponRoot { get { return weaponRoot; } }
        public Vector3 AimDirection
        {
            get
            {
                if (AimTarget != null && rightHand != null) return ((AimTarget.position + AimPointOffset) - rightHand.position).normalized;
                return transform.forward;
            }
        }

        public void Configure(GameObject visualPrefab) { weaponVisualPrefab = visualPrefab; if (weaponRoot == null && animator != null) BuildWeapon(); }
        public void BeginReload() { if (reloadStart < 0f) reloadStart = Time.time; }
        public void TriggerShot()
        {
            if (pose != null) pose.NotifyShot();
            MuzzleFlashFx.Play(shotOrigin, AimDirection);
            if (flash != null && shotOrigin != null) { flash.SetPosition(0, shotOrigin.position); flash.SetPosition(1, shotOrigin.position + AimDirection * .6f); flash.enabled = true; flashUntil = Time.time + .045f; }
        }

        void Awake()
        {
            animator = GetComponentInChildren<Animator>(true);
            if (animator == null || !animator.isHuman) { enabled = false; return; }
            pose = animator.GetComponent<FirearmPose>() ?? animator.gameObject.AddComponent<FirearmPose>();
            pose.User = this;
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            middle = animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate);
            thumb = animator.GetBoneTransform(HumanBodyBones.RightThumbProximal);
            index = animator.GetBoneTransform(HumanBodyBones.RightIndexProximal);
            BuildWeapon();
            var go = new GameObject("NPC Shot Flash"); go.transform.SetParent(transform, false);
            flash = go.AddComponent<LineRenderer>(); flash.useWorldSpace = true; flash.positionCount = 2; flash.startWidth = .05f; flash.endWidth = .01f;
            flash.material = new Material(Shader.Find("Sprites/Default")); flash.startColor = new Color(1f, .85f, .35f, .95f); flash.endColor = new Color(1f, .7f, .2f, 0f); flash.enabled = false;
        }

        void BuildWeapon()
        {
            if (weaponVisualPrefab != null)
            {
                var root = new GameObject("Sidearm (NPC)"); root.transform.SetParent(transform, false); weaponRoot = root.transform;
                var inst = Instantiate(weaponVisualPrefab, weaponRoot, false); inst.name = "LalayTool_Visual"; visual = inst.transform;
                grip = visual.Find("GripAnchor"); backstrap = visual.Find("BackstrapAnchor"); shotOrigin = visual.Find("ShotOrigin");
                renderers = weaponRoot.GetComponentsInChildren<Renderer>(true);
            }
        }

        void Update()
        {
            if (reloadStart >= 0f && Time.time - reloadStart >= reloadSeconds) reloadStart = -1f;
            if (flash != null && flash.enabled && Time.time >= flashUntil) flash.enabled = false;
            bool visible = pose != null && pose.WeaponVisible;
            if (renderers != null) foreach (var r in renderers) if (r != null) r.enabled = visible;
        }

        void LateUpdate()
        {
            if (weaponRoot == null || rightHand == null || pose == null || pose.AimBlend < .02f) return;
            FirearmController.PlaceAtHand(weaponRoot, grip, backstrap, rightHand, middle, thumb, index, pose.GunDirection);
        }
    }
}
