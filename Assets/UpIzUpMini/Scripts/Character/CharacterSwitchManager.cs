using System;
using System.Collections.Generic;
using UnityEngine;
using UpIzUpMini.Cameras;
using UpIzUpMini.InputSystem;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.Character
{
    [Serializable]
    public class CharacterSlot
    {
        public string displayName;
        public GameObject root;
        public PlayerController playerController;
        public FollowController followController;
        public CharacterVitals vitals;
        public InteractionDetector interactionDetector;
    }

    /// <summary>
    /// Tab switches which of the two boys the player controls. The other
    /// becomes a FollowController-driven companion. Shared state (money,
    /// inventory, heat) lives in EconomyManager and is untouched by
    /// switching; only control, camera target, and per-character vitals
    /// display follow the active slot - see Docs/STORY.md.
    /// </summary>
    public class CharacterSwitchManager : MonoBehaviour
    {
        public static CharacterSwitchManager Instance { get; private set; }

        [SerializeField] private CharacterSlot[] slots = new CharacterSlot[2];
        [SerializeField] private ThirdPersonFollowCamera followCamera;
        [SerializeField] private Vector3 safehouseSpawn;
        [Tooltip("Owed to the health center every time health hits 0 - per the user's explicit ask. AddMoney has no floor, so this can put Money negative (a debt) rather than being capped at what's on hand.")]
        [SerializeField] private int deathFee = 500;

        public int ActiveIndex { get; private set; }
        public CharacterSlot Active => slots[ActiveIndex];
        public CharacterSlot[] Slots => slots;

        public event Action<CharacterSlot> OnActiveChanged;

        // MINI-062: "selectable respawn" - a house's SafehouseInteractable
        // calls this ([4] Set Respawn) to make itself where the player
        // reappears after death or falling out of the world, instead of
        // that always being the fixed Highland farm safehouse baked into
        // `safehouseSpawn` at scene-build time. Falls back to
        // `safehouseSpawn` until the player ever picks one explicitly.
        private bool _hasSelectedRespawn;
        private Vector3 _selectedRespawn;
        public string RespawnLabel { get; private set; } = "Farm Safehouse";
        public Vector3 CurrentRespawnPoint => _hasSelectedRespawn ? _selectedRespawn : safehouseSpawn;

        public void SetRespawnPoint(Vector3 point, string label)
        {
            _hasSelectedRespawn = true;
            _selectedRespawn = point;
            RespawnLabel = label;
        }

        /// <summary>Restores a respawn selection from a save file - distinct
        /// from SetRespawnPoint only in that a zero vector (never saved, or
        /// an old save predating this feature) is treated as "no selection
        /// yet" rather than a real point to respawn at.</summary>
        public void LoadRespawnPoint(Vector3 point, string label)
        {
            if (point == Vector3.zero) return;
            SetRespawnPoint(point, label);
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            foreach (var slot in slots)
            {
                if (slot?.vitals != null) slot.vitals.OnDied += HandleDeath;
            }
            ApplyActive(ActiveIndex);
            IgnoreHeroBodyCollision();
        }

        private void IgnoreHeroBodyCollision()
        {
            if (slots == null || slots.Length < 2) return;
            var a = slots[0]?.root != null ? slots[0].root.GetComponent<CharacterController>() : null;
            var b = slots[1]?.root != null ? slots[1].root.GetComponent<CharacterController>() : null;
            if (a != null && b != null) Physics.IgnoreCollision(a, b, true);
        }

        private void OnDestroy()
        {
            foreach (var slot in slots)
            {
                if (slot?.vitals != null) slot.vitals.OnDied -= HandleDeath;
            }
        }

        private void HandleDeath(CharacterVitals deadVitals)
        {
            var dead = Array.Find(slots, s => s != null && s.vitals == deadVitals);
            // Health-zero death specifically owes the health center a fee -
            // WorldSafetyController's out-of-bounds respawn goes through
            // the same RespawnAtSafehouse but isn't a medical event, so the
            // charge lives here rather than in that shared method.
            Economy.EconomyManager.Instance?.AddMoney(-deathFee);
            RespawnAtSafehouse($"{dead?.displayName ?? "Player"} dead. ${deathFee} owed to the health center.");
        }

        public void RespawnAtSafehouse(string reason)
        {
            Missions.MissionSystem.Instance?.FailCurrentMission(reason);
            Economy.EconomyManager.Instance?.AddHeat(-Economy.EconomyManager.MaxHeat);

            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot?.root == null) continue;
                var cc = slot.root.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                slot.root.transform.position = CurrentRespawnPoint + new Vector3(i * 1.25f, 0.15f, 0f);
                if (cc != null) cc.enabled = true;
                slot.vitals?.Restore();
                slot.root.GetComponent<FarmhandController>()?.SetWorking(false);
            }
        }

        private void Update()
        {
            if (GameInput.WasPressed(GameAction.SwitchCharacter) && slots.Length > 1)
            {
                SwitchTo(1 - ActiveIndex);
            }
        }

        // A character can be temporarily unavailable - e.g. away on the
        // Guadeloupe run (DECISIONS.md D-007).
        private readonly HashSet<int> _locked = new HashSet<int>();

        public void SetLocked(int index, bool locked)
        {
            if (locked) _locked.Add(index);
            else _locked.Remove(index);
        }

        public bool IsLocked(int index) => _locked.Contains(index);

        public void SwitchTo(int index)
        {
            if (index < 0 || index >= slots.Length) return;
            if (_locked.Contains(index)) return;
            ApplyActive(index);
        }

        private void ApplyActive(int index)
        {
            ActiveIndex = index;
            for (int i = 0; i < slots.Length; i++)
            {
                bool isActive = i == index;
                var slot = slots[i];
                if (slot?.playerController != null) slot.playerController.IsControlled = isActive;
                if (slot?.interactionDetector != null) slot.interactionDetector.enabled = isActive;

                if (slot?.followController != null)
                {
                    slot.followController.FollowingEnabled = !isActive;
                    slot.followController.FollowTarget = isActive ? null : slots[index].root.transform;
                }
            }

            if (followCamera != null && slots[index]?.root != null)
            {
                followCamera.SetTarget(slots[index].root.transform);
            }

            OnActiveChanged?.Invoke(Active);

            if (_hasSwitchedOnce)
            {
                Missions.MissionSystem.Instance?.Notify(Missions.ObjectiveKind.Switch);
            }
            _hasSwitchedOnce = true;
        }

        // Start() applies the initial active character, which must not count
        // as the player performing a switch.
        private bool _hasSwitchedOnce;
    }
}
