using System;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;
using UpIzUpMini.Interaction;
using UpIzUpMini.UI;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.Combat
{
    /// <summary>One shared fictional sidearm. Mouse2 aims, Mouse1 fires, R reloads.
    /// Public input methods allow future touch buttons to use the same rules.</summary>
    [RequireComponent(typeof(PlayerController))]
    public class FirearmController : MonoBehaviour, IPistolUser
    {
        public const string SidearmId = "lalay_sidearm";
        public const string AmmoId = "sidearm_rounds";
        public const int MagazineCapacity = 12;

        [SerializeField] float range = 65f;
        [SerializeField] float damage = 38f;
        [SerializeField] float shotInterval = .28f;
        [SerializeField] float reloadSeconds = 1.35f;
        [SerializeField] Transform muzzle;
        [SerializeField] Transform weaponRoot;
        [SerializeField] Transform gripAnchor;
        [SerializeField] Transform backstrapAnchor;
        [SerializeField] Renderer[] heldWeaponRenderers;
        [SerializeField] FirearmPose pose;

        PlayerController player;
        CharacterVitals vitals;
        VehicleRider rider;
        float nextShotAt;
        float reloadAt;
        bool virtualAim;
        bool queuedFire;
        bool queuedReload;
        LineRenderer tracer;
        Transform rightHand;
        Transform rightMiddleIntermediate;
        Transform rightThumbProximal;
        Transform rightIndexProximal;
        float tracerUntil;

        public bool IsAiming { get; private set; }
        public float Recoil { get; private set; }
        public int MagazineRounds => EconomyManager.Instance != null ? EconomyManager.Instance.SidearmMagazine : 0;
        public bool IsReloading => reloadAt > 0f;
        // IPistolUser (MINI-190): the body animation reads these
        public bool IsLowReady => false;
        public float ReloadProgress => reloadAt > 0f ? Mathf.Clamp01(1f - (reloadAt - Time.time) / Mathf.Max(.01f, reloadSeconds)) : 0f;
        public Vector3 AimDirection { get { var c = Camera.main; return c != null ? c.transform.forward : transform.forward; } }
        public Transform WeaponRoot => weaponRoot;

        void Awake()
        {
            player = GetComponent<PlayerController>();
            vitals = GetComponent<CharacterVitals>();
            rider = GetComponent<VehicleRider>();
            if (pose == null) pose = GetComponentInChildren<FirearmPose>(true);
            if (pose != null) pose.Controller = this;
            var animator = pose != null ? pose.GetComponent<Animator>() : null;
            if (animator != null && animator.isHuman)
            {
                rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                rightMiddleIntermediate = animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate);
                rightThumbProximal = animator.GetBoneTransform(HumanBodyBones.RightThumbProximal);
                rightIndexProximal = animator.GetBoneTransform(HumanBodyBones.RightIndexProximal);
            }
            if (gripAnchor == null && weaponRoot != null)
                gripAnchor = weaponRoot.Find("LalayTool_Visual/GripAnchor");
            if (backstrapAnchor == null && weaponRoot != null)
                backstrapAnchor = weaponRoot.Find("LalayTool_Visual/BackstrapAnchor");
            CreateTracer();
        }

        void LateUpdate()
        {
            if (weaponRoot == null || rightHand == null) return;
            var cam = Camera.main;
            if (cam == null) return;
            // MINI-190: follow the animated hand direction (recoil flip, reload tilt) once the pose system is driving the arms
            Vector3 gunDirection = pose != null && pose.AimBlend > .05f ? pose.GunDirection : cam.transform.forward;
            PlaceAtHand(weaponRoot, gripAnchor, backstrapAnchor, rightHand,
                rightMiddleIntermediate, rightThumbProximal, rightIndexProximal, gunDirection);
        }

        // The model's grip must sit inside the curled fingers while the upper
        // backstrap approaches the thumb-index web. The 25% web correction is
        // calibrated against both playable characters' sampled hand meshes.
        public static void PlaceAtHand(Transform root, Transform grip, Transform backstrap,
            Transform wrist, Transform middle, Transform thumb, Transform index, Vector3 direction)
        {
            if (grip == null || middle == null || backstrap == null || thumb == null || index == null)
            {
                PlaceAtPalm(root, grip, wrist, middle, direction);
                return;
            }
            var rotation = Quaternion.LookRotation(direction, Vector3.up);
            Vector3 gripLocal = root.InverseTransformPoint(grip.position);
            Vector3 backstrapLocal = root.InverseTransformPoint(backstrap.position);
            Vector3 fingerRoot = middle.position - rotation * gripLocal;
            Vector3 webRoot = (thumb.position + index.position) * .5f - rotation * backstrapLocal;
            root.SetPositionAndRotation(Vector3.Lerp(fingerRoot, webRoot, .25f), rotation);
        }

        // The Humanoid RightHand bone is the wrist. Align the model's grip
        // to the middle finger's curled joint, inside this rig's closed hand.
        public static void PlaceAtPalm(Transform root, Transform anchor, Transform wrist, Transform middleIntermediate, Vector3 direction)
        {
            var rotation = Quaternion.LookRotation(direction, Vector3.up);
            if (anchor == null || middleIntermediate == null)
            {
                // no finger bones (low-poly NPC hands): seat the grip at the palm centre, a hand length ahead of the wrist
                Vector3 gripLocal = anchor != null ? root.InverseTransformPoint(anchor.position) : Vector3.zero;
                root.SetPositionAndRotation(wrist.position + direction * .05f - rotation * gripLocal - Vector3.up * .01f, rotation);
                return;
            }
            Vector3 palm = middleIntermediate.position;
            Vector3 gripInRoot = root.InverseTransformPoint(anchor.position);
            root.SetPositionAndRotation(palm - rotation * gripInRoot, rotation);
        }

        void OnDisable()
        {
            IsAiming = false;
            virtualAim = false;
            queuedFire = queuedReload = false;
            if (tracer != null) tracer.enabled = false;
        }

        public void SetAimInput(bool held) => virtualAim = held;
        public void RequestFire() => queuedFire = true;
        public void RequestReload() => queuedReload = true;

        void Update()
        {
            var economy = EconomyManager.Instance;
            bool available = economy != null && economy.OwnsItem(SidearmId);
            bool canUse = available && player != null && player.IsControlled
                && (vitals == null || !vitals.IsDead) && (rider == null || !rider.IsMounted)
                && Time.timeScale > .001f && Cursor.lockState == CursorLockMode.Locked
                && !AnyShopOpen();

            IsAiming = canUse && (virtualAim || Input.GetMouseButton(1));
            bool showWeapon = pose != null ? pose.WeaponVisible : IsAiming;
            if (heldWeaponRenderers != null)
                foreach (var renderer in heldWeaponRenderers)
                    if (renderer != null) renderer.enabled = showWeapon;

            if (reloadAt > 0f && Time.time >= reloadAt)
            {
                reloadAt = 0f;
                int loaded = economy != null ? economy.ReloadSidearm(MagazineCapacity, AmmoId) : 0;
                if (loaded > 0) Missions.MissionSystem.Instance?.Notify(Missions.ObjectiveKind.ReloadTool);   // MINI-191 ammunition mission
            }

            if (canUse && (queuedReload || Input.GetKeyDown(KeyCode.R))) BeginReload();
            if (IsAiming && (queuedFire || Input.GetMouseButtonDown(0))) Fire();
            queuedFire = queuedReload = false;

            Recoil = Mathf.MoveTowards(Recoil, 0f, Time.deltaTime * 6f);
            if (tracer != null && Time.time >= tracerUntil) tracer.enabled = false;
        }

        void BeginReload()
        {
            var economy = EconomyManager.Instance;
            if (reloadAt > 0f || economy == null || economy.SidearmMagazine >= MagazineCapacity || economy.GetAmmo(AmmoId) <= 0) return;
            reloadAt = Time.time + reloadSeconds;
        }

        void Fire()
        {
            if (Time.time < nextShotAt || reloadAt > 0f) return;
            nextShotAt = Time.time + shotInterval;
            var economy = EconomyManager.Instance;
            if (economy == null || !economy.SpendSidearmRound()) { BeginReload(); return; }

            Recoil = 1f;
            if (pose != null) pose.NotifyShot();
            Camera cam = Camera.main;
            if (cam == null) return;
            Ray ray = cam.ViewportPointToRay(new Vector3(.5f, .5f));
            Vector3 end = ray.GetPoint(range);
            var hits = Physics.RaycastAll(ray, range, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform)) continue;
                end = hit.point;
                var target = hit.collider.GetComponentInParent<NpcCombatHealth>();
                var npc = hit.collider.GetComponentInParent<TownNPCInteractable>();
                if (target == null && npc != null) target = npc.gameObject.AddComponent<NpcCombatHealth>();
                if (target != null && !target.IsDown)
                {
                    target.HitFromImpact(damage, ray.direction * 2.5f, hit.point);
                    economy.AddHeat(npc != null && npc.Role == NpcRole.Police ? EconomyManager.MaxHeat : 30f);
                }
                break;
            }
            ShowTracer(muzzle != null ? muzzle.position : transform.position + Vector3.up * 1.4f, end);
            Missions.MissionSystem.Instance?.Notify(Missions.ObjectiveKind.FireTool);
        }

        static bool AnyShopOpen()
        {
            foreach (var shop in FindObjectsByType<ShopPanelController>(FindObjectsSortMode.None))
                if (shop.IsOpen) return true;
            return false;
        }

        void CreateTracer()
        {
            var go = new GameObject("Sidearm Shot Flash");
            go.transform.SetParent(transform, false);
            tracer = go.AddComponent<LineRenderer>();
            tracer.useWorldSpace = true;
            tracer.positionCount = 2;
            tracer.startWidth = .035f;
            tracer.endWidth = .006f;
            tracer.material = new Material(Shader.Find("Sprites/Default"));
            tracer.startColor = new Color(1f, .85f, .35f, .9f);
            tracer.endColor = new Color(1f, .72f, .2f, 0f);
            tracer.enabled = false;
        }

        void ShowTracer(Vector3 start, Vector3 end)
        {
            if (tracer == null) return;
            tracer.SetPosition(0, start);
            tracer.SetPosition(1, end);
            tracer.enabled = true;
            tracerUntil = Time.time + .045f;
        }

        void OnGUI()
        {
            if (player == null || !player.IsControlled || EconomyManager.Instance == null || !EconomyManager.Instance.OwnsItem(SidearmId)) return;
            if (Time.timeScale <= .001f) return;
            var label = IsReloading ? "RELOADING" : $"TOOL  {MagazineRounds}/{EconomyManager.Instance.GetAmmo(AmmoId)}";
            GUI.Label(new Rect(Screen.width - 210, Screen.height - 75, 205, 45), label + "\nR reload  •  Right click aim");
            if (!IsAiming) return;
            var box = new Rect(Screen.width * .5f - 9, Screen.height * .5f - 9, 18, 18);
            GUI.Box(box, "+");
        }
    }

}
