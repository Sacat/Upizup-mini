using System.Collections.Generic;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;

namespace UpIzUpMini.Interaction
{
    /// <summary>
    /// Drives police heat from proximity rather than time, GTA-style.
    ///
    /// Heat sits at 0 while no officer is near. It climbs once the active
    /// character comes inside an officer's notice radius, faster the closer
    /// they are and faster still if they are holding weed (illegal crops or
    /// seeds). Moving away bleeds it back down. At very close range an
    /// officer also wears the player's health down.
    ///
    /// Replaces EconomyManager's old flat time decay, which meant heat
    /// existed independently of whether police were anywhere nearby.
    /// </summary>
    public class PoliceHeatController : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] private float noticeRadius = 18f;
        [SerializeField] private float closeRadius = 4f;

        [Header("Rates (heat per second)")]
        [SerializeField] private float baseGainAtContact = 6f;
        [SerializeField] private float carryingWeedMultiplier = 2.5f;
        [SerializeField] private float coolPerSecond = 9f;

        [Header("Health pressure")]
        [SerializeField] private float healthDrainPerSecond = 6f;

        [SerializeField] private CropDefinition[] illegalCrops;

        private readonly List<Transform> _officers = new List<Transform>();
        private float _rescanTimer;

        public float DistanceToNearestOfficer { get; private set; } = float.MaxValue;
        public bool PlayerCarryingContraband { get; private set; }

        private void Update()
        {
            // Officers can be spawned/despawned by the reinforcement
            // system, so refresh the list periodically rather than caching
            // once at startup.
            _rescanTimer -= Time.deltaTime;
            if (_rescanTimer <= 0f)
            {
                RescanOfficers();
                _rescanTimer = 1f;
            }

            var slot = CharacterSwitchManager.Instance?.Active;
            var player = slot?.root;
            if (player == null || EconomyManager.Instance == null) return;

            Vector3 playerPos = player.transform.position;
            DistanceToNearestOfficer = float.MaxValue;

            foreach (var officer in _officers)
            {
                if (officer == null || !officer.gameObject.activeInHierarchy) continue;
                float d = Vector3.Distance(playerPos, officer.position);
                if (d < DistanceToNearestOfficer) DistanceToNearestOfficer = d;
            }

            PlayerCarryingContraband = IsCarryingContraband() || UpIzUpMini.Combat.FirearmController.GunDrawn;   // MINI-192: a drawn gun draws police attention like contraband

            if (DistanceToNearestOfficer <= noticeRadius && PlayerCarryingContraband)
            {
                // 0 at the edge of notice, 1 at contact.
                float closeness = 1f - Mathf.Clamp01(DistanceToNearestOfficer / noticeRadius);
                float gain = baseGainAtContact * closeness;
                gain *= carryingWeedMultiplier;

                EconomyManager.Instance.AddHeat(gain * Time.deltaTime);

                if (DistanceToNearestOfficer <= closeRadius && EconomyManager.Instance.Heat > 0f)
                {
                    slot.vitals?.Damage(healthDrainPerSecond * Time.deltaTime);
                }
            }
            else
            {
                EconomyManager.Instance.AddHeat(-coolPerSecond * Time.deltaTime);
            }
        }

        private bool IsCarryingContraband()
        {
            if (illegalCrops == null || EconomyManager.Instance == null) return false;

            foreach (var crop in illegalCrops)
            {
                if (crop == null || !crop.isIllegal) continue;
                if (EconomyManager.Instance.GetCount(crop.cropId) > 0) return true;
                if (EconomyManager.Instance.GetSeeds(crop.cropId) > 0) return true;
            }
            return false;
        }

        private void RescanOfficers()
        {
            _officers.Clear();
            foreach (var npc in Object.FindObjectsByType<TownNPCInteractable>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (npc.Role == NpcRole.Police) _officers.Add(npc.transform);
            }
        }
    }
}
