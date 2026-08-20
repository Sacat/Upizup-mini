using System.Collections.Generic;
using UnityEngine;
using UpIzUpMini.Character;

namespace UpIzUpMini.Interaction
{
    /// <summary>
    /// MINI-058: activates/deactivates a pool of Dog Life members by player
    /// distance to a fixed "block" centre, instead of leaving all of them
    /// active across the map - per the brief's explicit "use pooling/
    /// activation/distance logic" instruction. Modelled on
    /// PoliceReinforcementSpawner's pool (MINI-041), swapping heat-driven
    /// hysteresis for distance-driven hysteresis - the exit distance is
    /// deliberately larger than the enter distance so a member doesn't
    /// flicker active/inactive while the player stands right at the edge.
    ///
    /// The brief also says "support up to roughly ten Dog Life members in
    /// major block encounters" - this pool is deliberately smaller
    /// (maxActive below) for this pass; the pooling architecture itself is
    /// what scales, not necessarily ten authored characters on day one
    /// (see the MINI-058 handoff entry for why).
    /// </summary>
    public class RivalGangSpawner : MonoBehaviour
    {
        [SerializeField] private Vector3 blockCentre;
        [SerializeField] private float enterDistance = 26f;
        [SerializeField] private float exitDistance = 40f;

        // Real bug, caught by validation (not left for the user to find):
        // this list previously had no [SerializeField], so it built up
        // correctly in-memory during the same editor session that ran
        // Mini011PhaseBSetup.BuildScene, but held nothing at all once the
        // scene was actually saved and reloaded - exactly what happens
        // every time the real game boots. Dog Life would never have
        // appeared in an actual build.
        [SerializeField] private List<GameObject> _pool = new List<GameObject>();
        private bool _active;

        public void SetBlockCentre(Vector3 centre) => blockCentre = centre;

        public void AddMember(GameObject member)
        {
            member.SetActive(false);
            _pool.Add(member);
        }

        private void Update()
        {
            if (_pool.Count == 0) return;

            var player = CharacterSwitchManager.Instance?.Active?.root;
            if (player == null) return;

            float dist = Vector3.Distance(player.transform.position, blockCentre);

            bool shouldBeActive = _active
                ? dist <= exitDistance   // hysteresis: needs to walk further away to leave than it took to arrive
                : dist <= enterDistance;

            if (shouldBeActive == _active) return;
            _active = shouldBeActive;

            foreach (var member in _pool)
            {
                if (member == null) continue;
                if (_active)
                {
                    member.GetComponent<Combat.NpcCombatHealth>()?.ResetForRespawn();
                    member.SetActive(true);
                }
                else member.SetActive(false);
            }
        }
    }
}
