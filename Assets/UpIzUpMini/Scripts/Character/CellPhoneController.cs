using UnityEngine;
using UpIzUpMini.Economy;

namespace UpIzUpMini.Character
{
    /// <summary>
    /// MINI-040. Press C to call whichever boy isn't currently controlled -
    /// including pulling him straight off farm work - once the player owns
    /// "phone_basic" (Farm Shop). Per the user's ask: "buy cell phone to
    /// call other partner to come to you if he is working on the farm."
    ///
    /// Uses the same teleport-next-to-the-active-character pattern already
    /// established by GuadeloupeTrade.CompleteTrip, rather than pathfinding
    /// the companion over - there is no navmesh in this project, and a
    /// phone call summoning someone instantly is the honest read of "come
    /// to you" anyway.
    /// </summary>
    public class CellPhoneController : MonoBehaviour
    {
        public const string PhoneItemId = "phone_basic";

        [SerializeField] private float callCooldownSeconds = 15f;
        [SerializeField] private float arriveOffsetDistance = 2f;

        private float _nextCallAt;

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.C)) return;

            var economy = EconomyManager.Instance;
            // No phone yet - C does nothing, silently, rather than nagging
            // the player about an item they haven't been offered.
            if (economy == null || !economy.OwnsItem(PhoneItemId)) return;

            string message = TryCall(economy);
            if (message != null) Missions.MissionSystem.Instance?.Alert(message);
        }

        private string TryCall(EconomyManager economy)
        {
            if (Time.time < _nextCallAt)
            {
                return $"LINE BUSY\nGive it a moment before calling again.";
            }

            var switcher = CharacterSwitchManager.Instance;
            if (switcher?.Slots == null || switcher.Slots.Length < 2) return null;

            int inactive = 1 - switcher.ActiveIndex;
            if (switcher.IsLocked(inactive))
            {
                return "NO SIGNAL\nThey away on the Gwada run - can't reach them.";
            }

            var activeSlot = switcher.Active;
            var otherSlot = switcher.Slots[inactive];
            if (activeSlot?.root == null || otherSlot?.root == null) return null;

            _nextCallAt = Time.time + callCooldownSeconds;

            // The whole point is being able to pull them off farm work.
            otherSlot.root.GetComponent<FarmhandController>()?.SetWorking(false);

            var cc = otherSlot.root.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            otherSlot.root.transform.position = activeSlot.root.transform.position
                + activeSlot.root.transform.right * arriveOffsetDistance;
            if (cc != null) cc.enabled = true;

            return $"{otherSlot.displayName.ToUpperInvariant()}\nOn my way!";
        }
    }
}
