using System.Collections.Generic;
using UnityEngine;
using UpIzUpMini.Combat;

namespace UpIzUpMini.Gangs
{
    /// <summary>
    /// MINI-054. The rival gang "Dog Life" (brown bandanas/scarfs, black
    /// shirt, brown pants - set by the scene builder recolouring the model
    /// material slots). They run Lalay. Up to <see cref="maxMembers"/> of
    /// them spawn on their block at once and are FIGHTABLE - each carries
    /// NpcCombatHealth (isOfficer = false, so fighting them does not spike
    /// police heat). They wander their block, and once gang rivalry begins
    /// they can raid the player's unattended plots (steal zeb), which the
    /// story reveals only later via the Gardey Zafeh / Guadeloupe dispatch.
    ///
    /// Scope for this pass: spawn + fightable + basic block behaviour. The
    /// story-driven theft reveal and mission wiring land in MINI missions.
    /// </summary>
    public class RivalGangController : MonoBehaviour
    {
        public const int MaxMembers = 10;

        [SerializeField] private int maxMembers = MaxMembers;
        [SerializeField] private float blockRadius = 14f;
        [SerializeField] private float wanderSpeed = 1.4f;
        [SerializeField] private Vector3 blockCenter;

        private readonly List<GameObject> _members = new List<GameObject>();

        public IReadOnlyList<GameObject> Members => _members;
        public bool RivalryActive { get; set; }

        // Members are fully assembled by the scene builder (model, animator,
        // movement, combat health) as children of this controller - this
        // runtime piece registers them and drives their behaviour. No
        // auto-spawn here to avoid double-building.
        public void RegisterMember(GameObject member)
        {
            if (member != null && !_members.Contains(member)) _members.Add(member);
        }

        private void Awake()
        {
            foreach (Transform child in transform)
            {
                if (child.gameObject.activeSelf) RegisterMember(child.gameObject);
            }
        }

        private void Update()
        {
            // Behaviour update: wandering members drift within their block.
            // (Full pursuit/theft logic is mission-scoped; this keeps them
            // alive, animated and fightable on the block.)
            foreach (var member in _members)
            {
                if (member == null) continue;
                var mover = member.GetComponent<GangMemberMover>();
                if (mover != null) mover.Tick(blockCenter, blockRadius, wanderSpeed);
            }
        }
    }
}
