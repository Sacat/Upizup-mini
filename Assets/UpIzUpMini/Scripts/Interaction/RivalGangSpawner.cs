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
        [Tooltip("MINI-112: a member knocked out via despawnOnDefeat stays gone for at least this long, checked against NpcCombatHealth.LastDefeatedAt at the moment the player re-enters the block - not merely 'has the GameObject been reactivated'.")]
        [SerializeField] private float respawnCooldownSeconds = 100f;

        // MINI-112: "a small group may occasionally leave the block and
        // walk down Lalay together" - deliberately simple (a temporary
        // waypoint swap on the pool's own existing PatrolNPC, not a new
        // navigation system) per the brief's own "within mobile simulation
        // limits." Only ever touches members who are active, not knocked
        // out, and not IsEngaged, so it can never interrupt or be confused
        // with a fight.
        [SerializeField] private Vector3 roadDirection = Vector3.forward;
        [SerializeField] private float groupWalkDistance = 24f;
        [SerializeField] private float groupWalkDurationSeconds = 35f;
        [SerializeField] private Vector2 groupWalkIntervalRange = new Vector2(75f, 150f);
        private float _nextGroupWalkAt = -1f;
        private readonly List<GameObject> _walkingGroup = new List<GameObject>();
        private readonly Dictionary<GameObject, Vector3[]> _walkingGroupOriginalWaypoints = new Dictionary<GameObject, Vector3[]>();
        private float _groupWalkEndsAt;

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
        public void SetRoadDirection(Vector3 direction) => roadDirection = direction.normalized;

        /// <summary>
        /// MINI-119, user: "for the war story you would have to fight off
        /// and kill all dog life members to win the war story" - M16
        /// previously only required walking into the block and waiting out
        /// heat, no combat at all. True the moment every pooled member is
        /// currently knocked out (NpcCombatHealth.IsDown), regardless of
        /// ReactivateEligibleMembers' respawn-cooldown timer - that timer
        /// only governs when a downed member's GameObject reappears later
        /// for ambient world combat, it should not block a mission that
        /// only cares about the state at the moment the last one falls.
        /// False (never wins by default) if the pool is empty/unbuilt,
        /// same defensive stance as every other "instance may not exist
        /// yet" check in this codebase.
        /// </summary>
        public bool AllDefeated
        {
            get
            {
                if (_pool == null || _pool.Count == 0) return false;
                foreach (var member in _pool)
                {
                    if (member == null) continue;
                    var health = member.GetComponent<Combat.NpcCombatHealth>();
                    if (health == null || !health.IsDown) return false;
                }
                return true;
            }
        }

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

            if (shouldBeActive == _active)
            {
                // MINI-112: even while the block stays active (player
                // lingering nearby), a member defeated earlier in this same
                // visit should still reappear once their cooldown elapses,
                // not only on the next distance-triggered transition.
                if (_active)
                {
                    ReactivateEligibleMembers();
                    UpdateGroupWalk();
                }
                return;
            }
            _active = shouldBeActive;

            foreach (var member in _pool)
            {
                if (member == null) continue;
                if (!_active) member.SetActive(false);
            }
            if (_active)
            {
                ReactivateEligibleMembers();
                _nextGroupWalkAt = Time.time + Random.Range(groupWalkIntervalRange.x, groupWalkIntervalRange.y);
            }
            else
            {
                EndGroupWalk(); // block emptied out - snap any walkers back rather than leaving them stranded down the road
            }
        }

        private void UpdateGroupWalk()
        {
            if (_walkingGroup.Count > 0)
            {
                if (Time.time >= _groupWalkEndsAt) EndGroupWalk();
                return;
            }

            if (_nextGroupWalkAt < 0f || Time.time < _nextGroupWalkAt) return;
            _nextGroupWalkAt = Time.time + Random.Range(groupWalkIntervalRange.x, groupWalkIntervalRange.y);

            foreach (var member in _pool)
            {
                if (_walkingGroup.Count >= 2) break;
                if (member == null || !member.activeSelf) continue;
                var brawler = member.GetComponent<Combat.FactionBrawler>();
                if (brawler != null && brawler.IsEngaged) continue;
                var health = member.GetComponent<Combat.NpcCombatHealth>();
                if (health != null && health.IsDown) continue;
                var patrol = member.GetComponent<PatrolNPC>();
                if (patrol == null) continue;

                _walkingGroup.Add(member);
                _walkingGroupOriginalWaypoints[member] = patrol.Waypoints;
                Vector3 start = member.transform.position;
                patrol.SetWaypoints(new[] { start, start + roadDirection * groupWalkDistance });
            }

            if (_walkingGroup.Count > 0) _groupWalkEndsAt = Time.time + groupWalkDurationSeconds;
        }

        private void EndGroupWalk()
        {
            foreach (var member in _walkingGroup)
            {
                if (member == null) continue;
                var patrol = member.GetComponent<PatrolNPC>();
                if (patrol != null && _walkingGroupOriginalWaypoints.TryGetValue(member, out var original))
                    patrol.SetWaypoints(original);
            }
            _walkingGroup.Clear();
            _walkingGroupOriginalWaypoints.Clear();
        }

        /// <summary>Brings back any pooled member who is either not
        /// currently knocked out, or has been down for at least
        /// respawnCooldownSeconds - per-member, not a blanket reactivation,
        /// so a member downed seconds ago stays gone while an untouched
        /// one still shows up immediately.</summary>
        private void ReactivateEligibleMembers()
        {
            foreach (var member in _pool)
            {
                if (member == null || member.activeSelf) continue;
                var health = member.GetComponent<Combat.NpcCombatHealth>();
                bool eligible = health == null
                    || Time.time - health.LastDefeatedAt >= respawnCooldownSeconds;
                if (!eligible) continue;

                health?.ResetForRespawn();
                member.SetActive(true);
            }
        }
    }
}
