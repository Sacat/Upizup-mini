using UnityEngine;
using UpIzUpMini.Economy;

namespace UpIzUpMini.Character
{
    /// <summary>
    /// MINI-040. Press Q to call whichever boy isn't currently controlled -
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

        [SerializeField] private KeyCode callKey = KeyCode.Q;
        [SerializeField] private float callCooldownSeconds = 15f;
        [Tooltip("MINI-069: recruited gang members are summoned too, spread on a short arc behind the player rather than stacked on one point.")]
        [SerializeField] private float recruitSpacing = 1.3f;
        [SerializeField] private float arriveOffsetDistance = 2f;

        private float _nextCallAt;

        private void Update()
        {
            // MINI-069: moved from C to Q at the user's request ("i also want
            // to be able to call them from the cell phone to come and meet me so
            // make Q do this"). Q is otherwise free in the field - the pause
            // menu's Q only reads while paused.
            if (!Input.GetKeyDown(callKey)) return;

            // MINI-140: Q is the bike wheelie key while riding ("E for
            // mount/enter vehicles, Q for wheelie"). If the active character
            // is currently on a vehicle, a Q tap is a wheelie input - do not
            // also summon the partner/gang. VehicleRider.Mount and
            // CarInteractable.Enter both clear PlayerController.IsControlled,
            // so this covers the TMAX, the SuperMoto and the car. On foot
            // IsControlled is true and the phone call works unchanged.
            var callSwitcher = CharacterSwitchManager.Instance;
            if (callSwitcher?.Slots != null && callSwitcher.Slots.Length > 0)
            {
                var activeSlot = callSwitcher.Slots[
                    Mathf.Clamp(callSwitcher.ActiveIndex, 0, callSwitcher.Slots.Length - 1)];
                var activePc = activeSlot?.root != null
                    ? activeSlot.root.GetComponent<PlayerController>()
                    : null;
                if (activePc != null && !activePc.IsControlled) return;
            }

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
                return "NO SIGNAL\nThey're away on the Guadeloupe run - can't reach them.";
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

            int recruits = SummonRecruits(activeSlot.root.transform);

            return recruits > 0
                ? $"{otherSlot.displayName.ToUpperInvariant()}\nOn my way - bringing {recruits} of the crew."
                : $"{otherSlot.displayName.ToUpperInvariant()}\nOn my way!";
        }

        /// <summary>
        /// MINI-069: the same call also pulls in every recruited Not Ah Word
        /// member, per "i also want to be able to call them from the cell phone
        /// to come and meet me".
        ///
        /// They are placed behind the player on a short arc and switched back
        /// to Follow, so calling doubles as "regroup on me" - otherwise a
        /// member left on plantation guard duty would be summoned and then
        /// immediately walk back to his post. Teleported rather than pathed,
        /// matching the existing companion call: there is no navmesh route
        /// this could rely on.
        /// </summary>
        private int SummonRecruits(Transform anchor)
        {
            var all = GangMemberController.All;
            if (all == null || all.Count == 0) return 0;

            int placed = 0;
            for (int i = 0; i < all.Count; i++)
            {
                var member = all[i];
                if (member == null || !member.IsRecruited || !member.gameObject.activeInHierarchy) continue;

                // Fan out behind the player, alternating left/right, so a full
                // crew does not end up in one long line trailing off.
                int step = placed / 2 + 1;
                float side = (placed % 2 == 0) ? -1f : 1f;
                Vector3 spot = anchor.position
                    - anchor.forward * (recruitSpacing * 0.8f)
                    + anchor.right * side * recruitSpacing * step;

                var cc = member.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                member.transform.position = spot;
                member.transform.rotation = anchor.rotation;
                if (cc != null) cc.enabled = true;

                member.FollowTarget = anchor;
                member.SetAssignmentFollow();

                placed++;
            }

            // MINI-073: "you can also call gang on phone to come on your
            // range as well" - if the player is currently driving, whoever
            // was just teleported in climbs straight into an empty seat
            // instead of standing next to a moving car.
            UpIzUpMini.Vehicles.CarInteractable.ActiveDriven?.FillEmptySeats();

            return placed;
        }
    }
}
