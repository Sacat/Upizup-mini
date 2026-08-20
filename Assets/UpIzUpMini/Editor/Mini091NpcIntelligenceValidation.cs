using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.EditorTools
{
    public static class Mini091NpcIntelligenceValidation
    {
        [MenuItem("Up Iz Up Mini/MINI-091/Validate NPC Intelligence")]
        public static void Validate()
        {
            ValidateFormationAndHysteresis();
            ValidateSeparation();
            ValidatePoliceStates();
            ValidateIntegrationAndExistingGangLifecycle();
            Debug.Log("MINI-091 NPC INTELLIGENCE PASS: formation slots, stop/resume hysteresis, dynamic separation, police state configuration, patrol safety, and rival retreat/despawn/respawn wiring validated.");
        }

        private static void ValidateFormationAndHysteresis()
        {
            var leader = new GameObject("MINI091_Leader");
            try
            {
                leader.transform.position = new Vector3(10f, 0f, 5f);
                leader.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                Vector3 slot = LocalSteeringSafety.TrailingSlot(leader.transform, 3.5f, 1.15f, 1f);
                Vector3 expected = leader.transform.position - leader.transform.forward * 3.5f + leader.transform.right * 1.15f;
                Expect(Vector3.Distance(slot, expected) < 0.001f, "Trailing slot is not leader-relative.");

                Expect(!LocalSteeringSafety.ShouldMoveToSlot(1.0f, false, 0.65f, 1.25f),
                    "Idle follower should not restart inside resume distance.");
                Expect(LocalSteeringSafety.ShouldMoveToSlot(1.3f, false, 0.65f, 1.25f),
                    "Idle follower should resume beyond resume distance.");
                Expect(LocalSteeringSafety.ShouldMoveToSlot(0.8f, true, 0.65f, 1.25f),
                    "Moving follower should continue until stop distance.");
                Expect(!LocalSteeringSafety.ShouldMoveToSlot(0.6f, true, 0.65f, 1.25f),
                    "Moving follower should stop inside stop distance.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(leader);
            }
        }

        private static void ValidateSeparation()
        {
            var self = new GameObject("MINI091_Self");
            var other = new GameObject("MINI091_Other");
            try
            {
                self.AddComponent<CharacterController>();
                other.AddComponent<CharacterController>();
                self.transform.position = Vector3.zero;
                other.transform.position = new Vector3(0.45f, 0f, 0f);
                Physics.SyncTransforms();

                Vector3 separated = LocalSteeringSafety.ApplyCharacterSeparation(
                    self.transform, Vector3.forward, null);
                Expect(separated.x < -0.05f, "Nearby character did not steer the desired direction apart.");
                Expect(Mathf.Abs(separated.magnitude - 1f) < 0.01f, "Separated direction is not normalized.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(self);
                UnityEngine.Object.DestroyImmediate(other);
            }
        }

        private static void ValidatePoliceStates()
        {
            var go = new GameObject("MINI091_Police");
            try
            {
                go.AddComponent<CharacterController>();
                var officer = go.AddComponent<PoliceOfficer>();
                var so = new SerializedObject(officer);
                Expect(so.FindProperty("maxStamina").floatValue > 0f, "Police stamina is not configured.");
                Expect(so.FindProperty("chaseDrainPerSecond").floatValue > 0f, "Police chase drain is not configured.");
                Expect(so.FindProperty("minimumResumeStamina").floatValue > 0f, "Police recovery threshold is not configured.");
                Expect(so.FindProperty("searchSeconds").floatValue >= 3f, "Police search window is too short or missing.");
                Expect(officer.CurrentState == PoliceMovementState.Patrol, "Police must start in Patrol state.");
                Expect(Enum.GetValues(typeof(PoliceMovementState)).Length == 5, "Police state set is incomplete.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static void ValidateIntegrationAndExistingGangLifecycle()
        {
            string follow = Read("Assets/UpIzUpMini/Scripts/Character/FollowController.cs");
            string recruit = Read("Assets/UpIzUpMini/Scripts/Character/GangMemberController.cs");
            string patrol = Read("Assets/UpIzUpMini/Scripts/Interaction/PatrolNPC.cs");
            string police = Read("Assets/UpIzUpMini/Scripts/Interaction/PoliceOfficer.cs");
            string brawler = Read("Assets/UpIzUpMini/Scripts/Combat/FactionBrawler.cs");
            string pool = Read("Assets/UpIzUpMini/Scripts/Interaction/RivalGangSpawner.cs");

            Expect(follow.Contains("TrailingSlot") && follow.Contains("ShouldMoveToSlot"),
                "Main companion does not use formation/hysteresis.");
            Expect(recruit.Contains("TrailingSlot") && recruit.Contains("ShouldMoveToSlot"),
                "Recruits do not use formation/hysteresis.");
            Expect(patrol.Contains("LocalSteeringSafety.TryDirection"),
                "Patrol NPCs do not use dynamic local safety.");
            Expect(police.Contains("PoliceMovementState.Search") && police.Contains("HasLineOfSight"),
                "Police search/visibility transition is not wired.");
            Expect(brawler.Contains("RetreatFromLastEnemy") && brawler.Contains("gameObject.SetActive(false)"),
                "Last-rival retreat/despawn behavior was lost.");
            Expect(pool.Contains("ResetForRespawn") && pool.Contains("member.SetActive(true)"),
                "Distance-pool rival respawn behavior was lost.");
        }

        private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path));

        private static void Expect(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException($"MINI-091 NPC INTELLIGENCE FAIL: {message}");
        }
    }
}
