using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.EditorTools
{
    public static class Mini093PoliceCombatValidation
    {
        [MenuItem("Up Iz Up Mini/MINI-093/Validate Police Combat")]
        public static void Validate()
        {
            ValidateSavedSceneDefaults();
            var officerGo = new GameObject("MINI093_Officer");
            var victimGo = new GameObject("MINI093_Victim");
            try
            {
                officerGo.AddComponent<CharacterController>();
                var officer = officerGo.AddComponent<PoliceOfficer>();
                Invoke(officer, "Awake");

                victimGo.AddComponent<CharacterController>();
                var victim = victimGo.AddComponent<CharacterVitals>();
                Invoke(victim, "Awake");
                victimGo.transform.position = new Vector3(0f, 0f, 1.2f);

                float startingHealth = victim.Health;
                Expect(BeginStrike(officer, victimGo), "Police strike did not begin against a live close target.");
                Expect(Mathf.Abs(victim.Health - startingHealth) < 0.01f,
                    "Police damage happened immediately instead of after windup.");

                officer.AdvancePoliceAttack(0.1f);
                Expect(Mathf.Abs(victim.Health - startingHealth) < 0.01f,
                    "Police damage happened before windup completed.");
                officer.AdvancePoliceAttack(0.13f);
                float afterFirstHit = victim.Health;
                Expect(afterFirstHit < startingHealth, "Police active contact did not reduce player health.");
                officer.AdvancePoliceAttack(0.05f);
                Expect(Mathf.Abs(victim.Health - afterFirstHit) < 0.01f,
                    "One police swing damaged the player more than once.");

                officer.AdvancePoliceAttack(1f);
                SetField(officer, "_nextStrike", -1f);
                victimGo.transform.position = new Vector3(0f, 0f, -1.2f);
                Expect(BeginStrike(officer, victimGo), "Behind-target test strike did not begin.");
                officer.AdvancePoliceAttack(0.23f);
                Expect(Mathf.Abs(victim.Health - afterFirstHit) < 0.01f,
                    "Police damaged a target behind the officer.");

                officer.AdvancePoliceAttack(1f);
                victimGo.transform.position = new Vector3(0f, 0f, 1.2f);
                int deathEvents = 0;
                victim.OnDied += _ => deathEvents++;
                for (int i = 0; i < 12 && !victim.IsDead; i++)
                {
                    SetField(officer, "_nextStrike", -1f);
                    Expect(BeginStrike(officer, victimGo), $"Repeated police strike {i + 1} did not begin.");
                    officer.AdvancePoliceAttack(0.23f);
                    officer.AdvancePoliceAttack(1f);
                }

                Expect(victim.IsDead && victim.Health <= 0f,
                    "Repeated police contact could not reduce player health to zero.");
                Expect(deathEvents == 1, $"Expected one death event, received {deathEvents}.");

                var so = new SerializedObject(officer);
                Expect(so.FindProperty("strikeDamage").floatValue > 0f, "Police strike damage is not configured.");
                Expect(so.FindProperty("strikeCooldown").floatValue >= 0.8f, "Police strike cooldown is too short.");

                string source = File.ReadAllText(Path.GetFullPath(
                    "Assets/UpIzUpMini/Scripts/Interaction/PoliceOfficer.cs"));
                Expect(source.Contains("if (distToPlayer <= stopDistance)")
                       && source.Contains("TryBeginStrike(player)"),
                    "Police chase proximity is not wired to the strike start.");
                Expect(source.Contains("CurrentState == PoliceMovementState.Chase"),
                    "Police strike is not kept inside the chase state flow.");

                Debug.Log($"MINI-093 POLICE COMBAT PASS: first active contact reduced health {startingHealth:F0}->{afterFirstHit:F0}, one swing hit once, behind contact missed, and repeated police hits reached zero with one death event.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(officerGo);
                UnityEngine.Object.DestroyImmediate(victimGo);
            }
        }

        private static void ValidateSavedSceneDefaults()
        {
            EditorSceneManager.OpenScene(
                "Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);
            var savedOfficer = UnityEngine.Object.FindFirstObjectByType<PoliceOfficer>(
                FindObjectsInactive.Include);
            Expect(savedOfficer != null, "Saved GrandBayProof scene contains no PoliceOfficer.");

            var so = new SerializedObject(savedOfficer);
            Expect(so.FindProperty("strikeDamage").floatValue > 0f,
                "Saved-scene police did not receive the new default strike damage.");
            Expect(so.FindProperty("strikeWindupSeconds").floatValue > 0f,
                "Saved-scene police did not receive the new strike timing defaults.");
        }

        private static bool BeginStrike(PoliceOfficer officer, GameObject victim)
        {
            MethodInfo method = typeof(PoliceOfficer).GetMethod(
                "TryBeginStrike", BindingFlags.Instance | BindingFlags.NonPublic);
            return method != null && (bool)method.Invoke(officer, new object[] { victim });
        }

        private static void Invoke(object target, string method)
        {
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);
        }

        private static void SetField(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(target, value);
        }

        private static void Expect(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException($"MINI-093 POLICE COMBAT FAIL: {message}");
        }
    }
}
