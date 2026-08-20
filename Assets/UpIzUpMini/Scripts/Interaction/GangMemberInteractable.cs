using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Progression;

namespace UpIzUpMini.Interaction
{
    /// <summary>
    /// MINI-058: lets the player cycle a recruited gang member's
    /// assignment face-to-face - Follow -> Guard Plantation -> Stay at
    /// Home -> Follow. Refuses while Unavailable (really knocked down in
    /// combat, see GangMemberController) rather than letting the player
    /// reassign someone who's currently lying on the ground.
    ///
    /// Follow-up (per the user): one member (Chevy) isn't part of the
    /// paid recruiter's pool at all - he stands in the world near Boss C
    /// from the start, and talking to him directly recruits him for free
    /// once GrandBayGangs reputation ("respect") crosses a threshold,
    /// money never enters into it. recruitViaReputation switches this
    /// component into that pre-recruitment mode; every other member
    /// leaves it false and is untouched by this change.
    /// </summary>
    public class GangMemberInteractable : InteractableBase
    {
        [SerializeField] private GangMemberController member;
        [SerializeField] private bool recruitViaReputation;
        [SerializeField] private int recruitReputationThreshold = 20;
        private string _feedback;

        public override bool CanInteract(GameObject interactor)
            => member != null && (member.IsRecruited || recruitViaReputation);

        public override string PromptLabel
        {
            get
            {
                if (member == null) return "[ E ] Talk";

                if (!member.IsRecruited)
                {
                    return recruitViaReputation ? $"[ E ] Talk to {member.MemberName}" : "[ E ] Talk";
                }

                return member.Assignment == GangAssignment.Unavailable
                    ? $"[ E ] {member.MemberName} (hurt - resting)"
                    : $"[ E ] {member.MemberName}: {DescribeAssignment(member.Assignment)}";
            }
        }

        public override void Interact(GameObject interactor)
        {
            if (member == null) return;

            if (!member.IsRecruited)
            {
                if (!recruitViaReputation) return; // handled by the paid recruiter NPC instead

                if (!ProgressionGate.CanRecruit)
                {
                    int current = ProgressionManager.Instance != null
                        ? ProgressionManager.Instance.GetReputation(Faction.GrandBayGangs)
                        : 0;
                    _feedback = $"{member.MemberName}: Build your name first, boss. Street rep at {current}%.";
                    return;
                }

                var progression = ProgressionManager.Instance;
                int reputation = progression != null ? progression.GetReputation(Faction.GrandBayGangs) : 0;
                if (reputation < recruitReputationThreshold)
                {
                    _feedback = $"{member.MemberName}: You nuh have di respect yet. Build your name more, nuh.";
                    return;
                }

                var activeRoot = CharacterSwitchManager.Instance?.Active?.root;
                member.Recruit(activeRoot != null ? activeRoot.transform : transform);
                _feedback = $"{member.MemberName}: Respect. I riding with allu now.";
                return;
            }

            if (member.Assignment == GangAssignment.Unavailable)
            {
                _feedback = $"{member.MemberName}: Gimme a minute, I still hurting from dat, nuh.";
                return;
            }

            member.CycleAssignment();
            _feedback = $"{member.MemberName}: {DescribeAssignment(member.Assignment)}";
        }

        public override string GetInteractionFeedback() => _feedback;

        private static string DescribeAssignment(GangAssignment assignment) => assignment switch
        {
            GangAssignment.Follow => "Yea wii, I riding with you.",
            GangAssignment.GuardPlantation => "Say no more - I holding down the plantation.",
            GangAssignment.StayAtHome => "Alright, I staying back at the block.",
            _ => "..."
        };
    }
}
