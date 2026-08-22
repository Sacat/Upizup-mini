using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;
using UpIzUpMini.Interaction;
using UpIzUpMini.Missions;
using UpIzUpMini.Progression;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-058: proves recruitment (paid-pool cap enforcement/cost/Follow
    /// default, plus Chevy's separate respect-gated recruit), assignment
    /// cycling (including the Unavailable refusal), and the Dog Life
    /// spawner's distance-based pooling - against the real built scene's
    /// objects, not a duplicate in-memory setup.
    ///
    /// Follow-up (per the user): Chevy moved out of the paid pool (now 3:
    /// Deluxe/Draco/Rio) to his own respect-gated recruit near Boss C -
    /// updated here to match, plus a new check proving money alone can't
    /// recruit him and reputation can.
    /// </summary>
    public static class Mini058FactionsValidation
    {
        [MenuItem("Up Iz Up Mini/MINI-058/Validate Factions")]
        public static void Validate()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            bool pass = true;
            string fail = null;

            var recruiterGo = GameObject.Find("NPC_GangRecruiter");
            var economy = GameObject.Find("EconomyManager")?.GetComponent<EconomyManager>();
            var progression = GameObject.Find("ProgressionManager")?.GetComponent<ProgressionManager>();
            var switcher = Object.FindFirstObjectByType<CharacterSwitchManager>();
            var spawnerGo = GameObject.Find("DogLifeSpawner");
            var chevyGo = GameObject.Find("NotAhWord_Zoomy");
            var missionSystem = Object.FindFirstObjectByType<MissionSystem>();

            if (recruiterGo == null || economy == null || progression == null || switcher == null || spawnerGo == null || chevyGo == null || missionSystem == null)
            {
                Debug.LogError("MINI-058 VALIDATION FAIL: one of NPC_GangRecruiter/EconomyManager/ProgressionManager/CharacterSwitchManager/DogLifeSpawner/NotAhWord_Zoomy/MissionSystem not found in the built scene.");
                return;
            }

            var recruiter = recruiterGo.GetComponent<TownNPCInteractable>();
            InvokeMethod(economy, "Awake");
            // MINI-060 follow-up-2: recruitCost raised 150 -> 2000/member
            // per the user's explicit ask - 3 recruits now costs 6000.
            economy.AddMoney(7000);
            InvokeMethod(progression, "Awake");
            InvokeMethod(switcher, "Awake");
            InvokeMethod(missionSystem, "Awake");

            // MINI-117: the recruiter is gated on ProgressionGate.
            // IsMissionReached("M15") (the mission that literally sends the
            // player to him), which reads MissionSystem.Instance. This test
            // never invoked MissionSystem.Awake() before, so Instance stayed
            // null and the gate refused every attempt regardless of
            // anything else - a validator gap, not a broken recruiter.
            // Advance the simulated mission index to M15's own position
            // (found by id, not hardcoded, since MINI-110/111 inserted
            // several missions before it) so the test reflects a player who
            // has actually reached that point in the story.
            var missionsField = typeof(MissionSystem).GetField("missions", BindingFlags.NonPublic | BindingFlags.Instance);
            var missionIndexField = typeof(MissionSystem).GetField("_missionIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            var missions = (System.Collections.Generic.List<Mission>)missionsField.GetValue(missionSystem);
            int m15Index = missions.FindIndex(m => m.missionId == "M15");
            if (m15Index < 0)
            {
                Debug.LogError("MINI-058 VALIDATION FAIL: M15 not found in the built mission list.");
                return;
            }
            missionIndexField.SetValue(missionSystem, m15Index);

            // CanUseCurrentRole's GangRecruiter case also requires
            // GangReputation >= 20 (the same threshold GangMemberInteractable
            // uses for Chevy's separate respect-gated path below) - give the
            // paid-recruit test that too, since a player who has genuinely
            // reached M15 would plausibly have built at least this much rep.
            progression.AddReputation(Faction.GrandBayGangs, 20);

            var actor = new GameObject("Actor");

            // MINI-117: M15's own objective is "TalkTo GangRecruiter" -
            // the FIRST paid-recruit interaction (now that MissionSystem is
            // actually alive) also completes M15 itself via the generic
            // end-of-Interact TalkTo notify every NPC fires, paying out its
            // rewardMoney in the same call. Read the real value rather than
            // hardcoding it, so this stays correct if the reward is ever
            // retuned.
            int m15Reward = missions[m15Index].rewardMoney;

            // --- 1) Paid pool: recruit exactly 3, the 4th must fail. ---
            for (int i = 0; i < 3 && pass; i++)
            {
                int moneyBefore = economy.Money;
                recruiter.Interact(actor);
                string fb = recruiter.GetInteractionFeedback();
                Check(ref pass, ref fail, fb.Contains("in now") && fb.Contains("$2000"),
                    $"paid recruit #{i + 1}: expected a successful $2000 recruit line, got '{fb}'.");
                int expectedDrop = i == 0 ? 2000 - m15Reward : 2000;
                Check(ref pass, ref fail, economy.Money == moneyBefore - expectedDrop,
                    $"paid recruit #{i + 1}: expected money to drop by exactly {expectedDrop} (recruit #1 also completes M15's own {m15Reward} reward), got a drop of {moneyBefore - economy.Money}.");
            }
            if (pass)
            {
                recruiter.Interact(actor);
                string fb = recruiter.GetInteractionFeedback();
                Check(ref pass, ref fail, fb.Contains("full team"),
                    $"paid recruit #4 (over the 3-slot pool): expected a 'full team' refusal, got '{fb}'.");
            }

            // --- 2) Assignment cycling on a paid recruit (Deluxe). ---
            if (pass)
            {
                var redsGo = GameObject.Find("NotAhWord_Deluxe");
                var redsMember = redsGo?.GetComponent<GangMemberController>();
                var redsInteractable = redsGo?.GetComponent<GangMemberInteractable>();
                if (redsMember == null || redsInteractable == null)
                {
                    pass = false; fail = "NotAhWord_Deluxe or its GangMemberController/GangMemberInteractable not found after recruiting.";
                }
                else
                {
                    Check(ref pass, ref fail, redsMember.Assignment == GangAssignment.Follow,
                        $"expected Deluxe to start on Follow after recruiting, got {redsMember.Assignment}.");

                    if (pass)
                    {
                        redsInteractable.Interact(actor);
                        Check(ref pass, ref fail, redsMember.Assignment == GangAssignment.GuardPlantation,
                            $"expected Follow -> GuardPlantation, got {redsMember.Assignment}.");
                    }
                    if (pass)
                    {
                        redsInteractable.Interact(actor);
                        Check(ref pass, ref fail, redsMember.Assignment == GangAssignment.StayAtHome,
                            $"expected GuardPlantation -> StayAtHome, got {redsMember.Assignment}.");
                    }
                    if (pass)
                    {
                        redsInteractable.Interact(actor);
                        Check(ref pass, ref fail, redsMember.Assignment == GangAssignment.Follow,
                            $"expected StayAtHome -> Follow (wraps around), got {redsMember.Assignment}.");
                    }
                }
            }

            // MINI-117: Chevy's own test below needs GangReputation to
            // start at 0 (it proves money-only fails, then that +20 rep
            // succeeds) - undo the +20 given to the paid-recruiter section
            // above so the two sections don't interfere with each other.
            progression.AddReputation(Faction.GrandBayGangs, -20);

            // --- 3) Chevy: money alone must NOT recruit him; respect must. ---
            if (pass)
            {
                var chevyMember = chevyGo.GetComponent<GangMemberController>();
                var chevyInteractable = chevyGo.GetComponent<GangMemberInteractable>();
                if (chevyMember == null || chevyInteractable == null)
                {
                    pass = false; fail = "NotAhWord_Zoomy has no GangMemberController/GangMemberInteractable.";
                }
                else
                {
                    Check(ref pass, ref fail, !chevyMember.IsRecruited,
                        "expected Chevy to start unrecruited.");

                    if (pass)
                    {
                        // Plenty of money, zero reputation - must be refused.
                        int moneyBefore = economy.Money;
                        chevyInteractable.Interact(actor);
                        string fb = chevyInteractable.GetInteractionFeedback();
                        Check(ref pass, ref fail, !chevyMember.IsRecruited && fb.Contains("respect"),
                            $"expected money-only to fail to recruit Chevy with a 'respect' refusal, got IsRecruited={chevyMember.IsRecruited}, feedback='{fb}'.");
                        Check(ref pass, ref fail, economy.Money == moneyBefore,
                            $"expected a refused recruit attempt to charge no money, but it changed from {moneyBefore} to {economy.Money}.");
                    }

                    if (pass)
                    {
                        progression.AddReputation(Faction.GrandBayGangs, 20);
                        int moneyBefore = economy.Money;
                        chevyInteractable.Interact(actor);
                        string fb = chevyInteractable.GetInteractionFeedback();
                        Check(ref pass, ref fail, chevyMember.IsRecruited && fb.Contains("Respect"),
                            $"expected 20 GrandBayGangs reputation to recruit Chevy for free, got IsRecruited={chevyMember.IsRecruited}, feedback='{fb}'.");
                        Check(ref pass, ref fail, economy.Money == moneyBefore,
                            $"expected the respect-gated recruit to cost no money, but it changed from {moneyBefore} to {economy.Money}.");
                    }
                }
            }

            // --- 4) Dog Life spawner: far away -> inactive, close -> active. ---
            if (pass)
            {
                var player = switcher.Active.root;
                var spawner = spawnerGo.GetComponent<RivalGangSpawner>();
                // GameObject.Find only searches ACTIVE objects, and the
                // whole point of pooling is that these start inactive -
                // read the spawner's own pool via reflection instead.
                var poolField = typeof(RivalGangSpawner).GetField("_pool", BindingFlags.Instance | BindingFlags.NonPublic);
                var pool = poolField?.GetValue(spawner) as System.Collections.Generic.List<GameObject>;
                var dogLife0 = pool != null && pool.Count > 0 ? pool[0] : null;
                if (dogLife0 == null)
                {
                    pass = false; fail = "Dog Life pool is empty (RivalGangSpawner._pool has no members).";
                }
                else
                {
                    player.transform.position = dogLife0.transform.position + Vector3.forward * 200f;
                    InvokeMethod(spawner, "Update");
                    Check(ref pass, ref fail, !dogLife0.activeSelf,
                        "expected Dog Life pool inactive while the player is far away (200m).");

                    if (pass)
                    {
                        player.transform.position = dogLife0.transform.position + Vector3.forward * 5f;
                        InvokeMethod(spawner, "Update");
                        Check(ref pass, ref fail, dogLife0.activeSelf,
                            "expected Dog Life pool active once the player is close (5m).");
                    }
                }
            }

            Object.DestroyImmediate(actor);

            if (pass)
            {
                Debug.Log("MINI-058 FACTIONS VALIDATION PASS: exactly 3 paid recruits can join Not Ah Word (a 4th is refused), a new recruit starts on Follow and cycles Follow->Guard->Home->Follow correctly, Chevy cannot be bought with money but is recruited for free once GrandBayGangs reputation reaches 20, and the Dog Life pool activates/deactivates by real player distance.");
            }
            else
            {
                Debug.LogError($"MINI-058 FACTIONS VALIDATION FAIL: {fail}");
            }
        }

        private static void Check(ref bool pass, ref string fail, bool condition, string message)
        {
            if (pass && !condition) { pass = false; fail = message; }
        }

        private static void InvokeMethod(object target, string name)
        {
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);
        }
    }
}
