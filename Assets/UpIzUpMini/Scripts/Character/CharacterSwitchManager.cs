using System;
using UnityEngine;
using UpIzUpMini.Cameras;
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

        public int ActiveIndex { get; private set; }
        public CharacterSlot Active => slots[ActiveIndex];

        public event Action<CharacterSlot> OnActiveChanged;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            ApplyActive(ActiveIndex);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab) && slots.Length > 1)
            {
                SwitchTo(1 - ActiveIndex);
            }
        }

        public void SwitchTo(int index)
        {
            if (index < 0 || index >= slots.Length || index == ActiveIndex) return;
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
        }
    }
}
