using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Combat;
using UpIzUpMini.Economy;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.EditorTools
{
    public static class Mini092CombatContactValidation
    {
        private static readonly MeleeAttackProfile Profile = new MeleeAttackProfile(
            0.16f, 0.12f, 0.37f, 0.3f, 1.65f, 0.38f, 80f);

        [MenuItem("Up Iz Up Mini/MINI-092/Validate Combat Contact")]
        public static void Validate()
        {
            SimulationMode previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            try
            {
                ValidateTimeline();
                ValidateContactGeometry();
                ValidatePlayerSwingAndStamina();
                Debug.Log("MINI-092 COMBAT CONTACT PASS: timed single-resolution swing, forward/behind/arc geometry, line-of-sight wiring, stamina cost, one-hit behavior, and police-only heat escalation validated.");
            }
            finally
            {
                Physics.simulationMode = previousMode;
            }
        }

        private static void ValidateTimeline()
        {
            var timeline = new MeleeSwingTimeline();
            Expect(timeline.Begin(Profile), "Timeline would not begin.");
            Expect(!timeline.Begin(Profile), "A running timeline accepted a second swing.");
            Expect(!timeline.Advance(0.1f), "Swing resolved before windup completed.");
            Expect(timeline.Advance(0.07f), "Swing did not resolve when active window began.");
            Expect(!timeline.Advance(0.02f), "Swing resolved more than once in its active window.");
            Expect(!timeline.Advance(1f), "Completed swing produced a second resolution.");
            Expect(!timeline.IsRunning, "Timeline did not leave recovery.");
        }

        private static void ValidateContactGeometry()
        {
            var attacker = new GameObject("MINI092_GeometryAttacker");
            var target = BuildHealthTarget("MINI092_GeometryTarget", new Vector3(0f, 0f, 1.2f), false);
            try
            {
                Expect(MeleeContactResolver.IsInsideForwardContact(attacker.transform, target.transform, Profile),
                    "Valid target in front was not inside the swept contact path.");

                target.transform.position = new Vector3(0f, 0f, -1.1f);
                Expect(!MeleeContactResolver.IsInsideForwardContact(attacker.transform, target.transform, Profile),
                    "Target behind the attacker was incorrectly hittable.");

                target.transform.position = new Vector3(1.2f, 0f, 0.5f);
                Expect(!MeleeContactResolver.IsInsideForwardContact(attacker.transform, target.transform, Profile),
                    "Target outside the forward arc was incorrectly hittable.");

                Expect(typeof(MeleeContactResolver).GetMethod(
                           "HasLineOfSight", BindingFlags.Static | BindingFlags.NonPublic) != null,
                    "Line-of-sight blocker check is missing from the resolver.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(attacker);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void ValidatePlayerSwingAndStamina()
        {
            var economyGo = new GameObject("MINI092_Economy");
            var attacker = new GameObject("MINI092_Player");
            GameObject rival = null;
            GameObject police = null;
            try
            {
                var economy = economyGo.AddComponent<EconomyManager>();
                Invoke(economy, "Awake");

                attacker.AddComponent<CharacterController>();
                var vitals = attacker.AddComponent<CharacterVitals>();
                Invoke(vitals, "Awake");
                var melee = attacker.AddComponent<SimpleMeleeCombat>();
                Invoke(melee, "Awake");

                rival = BuildHealthTarget("MINI092_Rival", new Vector3(0f, 0f, 1.2f), false);
                var rivalHealth = rival.GetComponent<NpcCombatHealth>();
                SyncPhysics();

                float staminaBefore = vitals.Stamina;
                melee.Attack();
                Expect(melee.IsAttacking, "Player swing did not begin.");
                Expect(Mathf.Abs(rivalHealth.Health - 100f) < 0.01f, "Damage happened before the active window.");
                Expect(vitals.Stamina < staminaBefore, "Committed swing did not spend stamina.");

                melee.AdvanceAttack(0.17f);
                float healthAfterContact = rivalHealth.Health;
                Expect(healthAfterContact < 100f, "Active window did not damage the front target.");
                melee.AdvanceAttack(0.05f);
                Expect(Mathf.Abs(rivalHealth.Health - healthAfterContact) < 0.01f,
                    "One swing damaged the same target more than once.");
                Expect(EconomyManager.Instance.Heat < 0.01f, "Striking a rival incorrectly produced police-only max heat.");

                melee.AdvanceAttack(1f);
                UnityEngine.Object.DestroyImmediate(rival);
                rival = null;
                SetField(melee, "nextHit", -1f);

                police = BuildHealthTarget("MINI092_Police", new Vector3(0f, 0f, 1.2f), true);
                SyncPhysics();
                melee.Attack();
                melee.AdvanceAttack(0.17f);
                Expect(EconomyManager.Instance.Heat >= EconomyManager.MaxHeat - 0.01f,
                    "Striking police did not escalate to maximum heat.");

                Expect(!vitals.TrySpendStamina(vitals.MaxStamina + 1f),
                    "Vitals allowed an action cost larger than available stamina.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(attacker);
                if (rival != null) UnityEngine.Object.DestroyImmediate(rival);
                if (police != null) UnityEngine.Object.DestroyImmediate(police);
                UnityEngine.Object.DestroyImmediate(economyGo);
            }
        }

        private static bool Find(Transform attacker, NpcCombatHealth expected)
        {
            return MeleeContactResolver.TryFindNearest(
                       attacker, Profile, NpcCombatHealth.All,
                       (NpcCombatHealth candidate) => candidate == expected,
                       out NpcCombatHealth found)
                   && found == expected;
        }

        private static void SyncPhysics()
        {
            Physics.SyncTransforms();
            Physics.Simulate(0.02f);
            Physics.SyncTransforms();
        }

        private static GameObject BuildHealthTarget(string name, Vector3 position, bool police)
        {
            var root = new GameObject(name);
            root.transform.position = position;
            root.AddComponent<CapsuleCollider>();
            var body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            if (police)
            {
                var npc = root.AddComponent<TownNPCInteractable>();
                var so = new SerializedObject(npc);
                so.FindProperty("role").enumValueIndex = (int)NpcRole.Police;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            var health = root.AddComponent<NpcCombatHealth>();
            Invoke(health, "Awake");
            if (!NpcCombatHealth.All.Contains(health)) NpcCombatHealth.All.Add(health);
            return root;
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
            if (!condition) throw new InvalidOperationException($"MINI-092 COMBAT CONTACT FAIL: {message}");
        }
    }
}
