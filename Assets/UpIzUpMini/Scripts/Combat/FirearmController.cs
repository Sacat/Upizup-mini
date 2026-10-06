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
        public bool IsLowReady => gunDrawn && !IsAiming && canUseNow && !IsReloading;

        /// <summary>MINI-192: true while the active character carries the sidearm in hand (low ready or aiming). Police react to it.</summary>
        public static bool GunDrawn { get; private set; }
        /// <summary>MINI-192: true when the player owns the sidearm at all (holstered or not).</summary>
        public static bool PlayerOwnsGun { get { return EconomyManager.Instance != null && EconomyManager.Instance.OwnsItem(SidearmId); } }
        bool gunDrawn = true, canUseNow;
        /// <summary>Batch-mode proofs cannot lock the cursor; they set this instead.</summary>
        public static bool TestForceUsable;
        Cameras.ThirdPersonFollowCamera followCamera;
        Transform shotOrigin;
        float hudEmptyFlash;
        public float ReloadProgress => reloadAt > 0f ? Mathf.Clamp01(1f - (reloadAt - Time.time) / Mathf.Max(.01f, reloadSeconds)) : 0f;
        // MINI-195: aimed shots follow the camera; the lowered ready pose follows the BODY, so the arms never twist across the chest toward a camera
        // that is looking somewhere else than the character is facing.
        public Vector3 AimDirection { get { return IsAiming ? CrosshairAimDirection() : transform.forward; } }

        // MINI-195: the third-person camera looks steeply down at the character, so the raw camera forward would point the gun at the ground.
        // Aim the gun from the chest to the point under the crosshair instead, with the pitch limited so the arms stay natural.
        int aimCacheFrame = -1; Vector3 aimCache = Vector3.forward;
        Vector3 CrosshairAimDirection()
        {
            if (aimCacheFrame == Time.frameCount) return aimCache;
            aimCacheFrame = Time.frameCount;
            var c = Camera.main; if (c == null) { aimCache = transform.forward; return aimCache; }
            Vector3 chest = transform.position + Vector3.up * 1.35f;
            var ray = new Ray(c.transform.position, c.transform.forward);
            Vector3 point = ray.GetPoint(60f);
            var hits = Physics.RaycastAll(ray, 60f, ~0, QueryTriggerInteraction.Ignore); float best = float.MaxValue;
            foreach (var h in hits) { if (h.collider.transform.IsChildOf(transform) || h.distance < 1f) continue; if (h.distance < best) { best = h.distance; point = h.point; } }
            Vector3 d = point - chest; if (d.sqrMagnitude < .01f) d = c.transform.forward;
            d.Normalize();
            float pitch = Mathf.Asin(Mathf.Clamp(d.y, -1f, 1f)) * Mathf.Rad2Deg;
            Vector3 flat = new Vector3(d.x, 0f, d.z); if (flat.sqrMagnitude < .0001f) flat = transform.forward; flat.Normalize();
            pitch = Mathf.Clamp(pitch, -32f, 48f);
            aimCache = Quaternion.AngleAxis(-pitch, Vector3.Cross(Vector3.up, flat).normalized) * flat;
            return aimCache;
        }
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
            if (IsAiming && player != null && player.IsControlled)
            {
                // MINI-195: while aiming the body turns to face where the camera points (third-person shooter convention)
                Vector3 flat = cam.transform.forward; flat.y = 0f;
                if (flat.sqrMagnitude > .001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat.normalized, Vector3.up), 1f - Mathf.Exp(-18f * Time.deltaTime));
            }
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
            // MINI-192: the player starts with the sidearm in hand (a full magazine, no reserve rounds - the ammunition mission teaches buying more)
            if (economy != null && player != null && player.IsControlled) economy.GrantStarterItem(SidearmId, MagazineCapacity);
            bool available = economy != null && economy.OwnsItem(SidearmId);
            bool canUse = available && player != null && player.IsControlled
                && (vitals == null || !vitals.IsDead) && (rider == null || !rider.IsMounted)
                && Time.timeScale > .001f && (Cursor.lockState == CursorLockMode.Locked || TestForceUsable)
                && !AnyShopOpen();

            canUseNow = canUse;
            if (canUse && Input.GetKeyDown(KeyCode.H)) gunDrawn = !gunDrawn;
            IsAiming = canUse && (virtualAim || Input.GetMouseButton(1));
            if (IsAiming) gunDrawn = true;
            if (player != null && player.IsControlled) GunDrawn = canUse && gunDrawn;
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

            // starter gun means the "buy the Tool" step is already satisfied
            if (available && Missions.MissionSystem.Instance != null)
            {
                var objective = Missions.MissionSystem.Instance.CurrentObjective;
                if (objective != null && objective.kind == Missions.ObjectiveKind.BuyItem
                    && string.Equals(objective.targetId, SidearmId, StringComparison.OrdinalIgnoreCase))
                    Missions.MissionSystem.Instance.Notify(Missions.ObjectiveKind.BuyItem, SidearmId);
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
            if (economy == null || !economy.SpendSidearmRound()) { hudEmptyFlash = Time.time + .6f; BeginReload(); return; }

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
            if (shotOrigin == null && weaponRoot != null) shotOrigin = weaponRoot.Find("LalayTool_Visual/ShotOrigin");
            Vector3 muzzlePos = shotOrigin != null ? shotOrigin.position : (muzzle != null ? muzzle.position : transform.position + Vector3.up * 1.4f);
            ShowTracer(muzzlePos, end);
            Vector3 fireDir = pose != null ? pose.GunDirection : (end - muzzlePos).normalized;
            MuzzleFlashFx.Play(shotOrigin != null ? shotOrigin : transform, fireDir);
            if (followCamera == null) followCamera = FindFirstObjectByType<Cameras.ThirdPersonFollowCamera>();
            if (followCamera != null) followCamera.Kick(1.7f);
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
            tracer.startWidth = .018f;
            tracer.endWidth = .004f;
            tracer.material = MuzzleFlashFx.FlashMaterial != null ? MuzzleFlashFx.FlashMaterial : new Material(Shader.Find("Sprites/Default"));
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

        GUIStyle bigStyle, smallStyle;
        void OnGUI()
        {
            if (player == null || !player.IsControlled || EconomyManager.Instance == null || !EconomyManager.Instance.OwnsItem(SidearmId)) return;
            if (Time.timeScale <= .001f) return;
            float s = Mathf.Max(.75f, Screen.height / 900f);
            if (bigStyle == null) { bigStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft }; smallStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft }; }
            bigStyle.fontSize = Mathf.RoundToInt(44 * s); smallStyle.fontSize = Mathf.RoundToInt(15 * s);
            int mag = MagazineRounds, reserve = EconomyManager.Instance.GetAmmo(AmmoId);
            float w = 300 * s, h = 112 * s, x = Screen.width - w - 22 * s, y = Screen.height - h - 22 * s;
            var old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, .62f); GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
            GUI.color = new Color(1f, .78f, .25f, 1f); GUI.DrawTexture(new Rect(x, y, 4 * s, h), Texture2D.whiteTexture);
            bool empty = mag <= 0, lowMag = mag > 0 && mag <= 3, flashing = Time.time < hudEmptyFlash && (int)(Time.time * 10) % 2 == 0;
            GUI.color = IsReloading ? new Color(1f, .8f, .3f) : (empty || flashing ? new Color(1f, .3f, .25f) : (lowMag ? new Color(1f, .6f, .25f) : Color.white));
            GUI.Label(new Rect(x + 16 * s, y + 6 * s, 170 * s, 56 * s), IsReloading ? "RELOAD" : mag.ToString(), bigStyle);
            GUI.color = new Color(.85f, .85f, .85f, 1f);
            GUI.Label(new Rect(x + 150 * s, y + 6 * s, 140 * s, 56 * s), IsReloading ? "" : "/ " + MagazineCapacity + "    x " + reserve, smallStyle);
            // one pip per round in the magazine
            float pip = (w - 32 * s) / MagazineCapacity;
            for (int i = 0; i < MagazineCapacity; i++)
            {
                GUI.color = i < mag ? new Color(1f, .82f, .3f, 1f) : new Color(1f, 1f, 1f, .16f);
                GUI.DrawTexture(new Rect(x + 16 * s + i * pip, y + 66 * s, pip - 4 * s, 14 * s), Texture2D.whiteTexture);
            }
            if (IsReloading) { GUI.color = new Color(1f, .8f, .3f, 1f); GUI.DrawTexture(new Rect(x + 16 * s, y + 88 * s, (w - 32 * s) * ReloadProgress, 6 * s), Texture2D.whiteTexture); }
            GUI.color = new Color(.9f, .9f, .9f, .85f);
            string hint = !gunDrawn ? "H  draw gun" : (empty ? (reserve > 0 ? "R  reload  -  empty" : "OUT OF AMMO  -  buy rounds from the trader") : "R reload   H holster   RMB aim");
            GUI.Label(new Rect(x + 16 * s, y + 90 * s, w - 20 * s, 20 * s), hint, smallStyle);
            GUI.color = old;
            if (!IsAiming) return;
            float cx = Screen.width * .5f, cy = Screen.height * .5f, gap = 6 * s, len = 10 * s, th = 2 * s;
            GUI.color = new Color(1f, 1f, 1f, .9f);
            GUI.DrawTexture(new Rect(cx - th * .5f, cy - gap - len, th, len), Texture2D.whiteTexture); GUI.DrawTexture(new Rect(cx - th * .5f, cy + gap, th, len), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - gap - len, cy - th * .5f, len, th), Texture2D.whiteTexture); GUI.DrawTexture(new Rect(cx + gap, cy - th * .5f, len, th), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - th, cy - th, th * 2, th * 2), Texture2D.whiteTexture);
            GUI.color = old;
        }
    }

}
