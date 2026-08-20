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
    /// <summary>
    /// MINI-046. Exercises both halves of "stronger when two main
    /// characters together" against real code: CompanionCombatAssist
    /// actually landing a hit on a nearby officer while eligible (and
    /// correctly declining while farming), and SimpleMeleeCombat.Attack()
    /// applying the together-damage multiplier only when the companion is
    /// actually close enough and free to help. Reads NpcCombatHealth's
    /// private `health` field via reflection since it exposes no public
    /// getter - the only way to observe how much damage a hit actually
    /// applied. Update()/Awake() are invoked via reflection (edit mode
    /// doesn't tick MonoBehaviour lifecycle methods on its own) and
    /// Input.GetKeyDown can't be faked headlessly, which is exactly why
    /// SimpleMeleeCombat.Attack() was split out from Update() for this.
    /// </summary>
    public static class Mini046CompanionCombatValidation
    {
        [MenuItem("Up Iz Up Mini/MINI-046/Run Companion Combat Validation")]
        public static void Run()
        {
            var economyGo = new GameObject("TestEconomy");
            var economy = economyGo.AddComponent<EconomyManager>();
            InvokeMethod(economy, "Awake");

            var switchGo = new GameObject("TestSwitch");
            var switcher = switchGo.AddComponent<CharacterSwitchManager>();

            var (player, playerAnim) = BuildCharacter("TestPlayer");
            var meleeCombat = player.AddComponent<SimpleMeleeCombat>();
            SetPrivateField(meleeCombat, "animationManager", playerAnim);

            var (companion, companionAnim) = BuildCharacter("TestCompanion");
            var followController = companion.AddComponent<FollowController>();
            var farmhand = companion.AddComponent<FarmhandController>();
            var vitals = companion.AddComponent<CharacterVitals>();
            InvokeMethod(vitals, "Awake");
            var assist = companion.AddComponent<CompanionCombatAssist>();
            SetPrivateField(assist, "animationManager", companionAnim);
            SetPrivateField(assist, "followController", followController);
            SetPrivateField(assist, "farmhand", farmhand);
            SetPrivateField(assist, "vitals", vitals);
            InvokeMethod(assist, "Awake");

            var slots = new[]
            {
                new CharacterSlot { displayName = "Player", root = player, vitals = null },
                new CharacterSlot { displayName = "Companion", root = companion, vitals = vitals },
            };
            typeof(CharacterSwitchManager).GetField("slots", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(switcher, slots);
            InvokeMethod(switcher, "Awake");
            InvokeMethod(switcher, "Start"); // ActiveIndex=0 -> player controlled, companion follows

            // --- CompanionCombatAssist: must not attack while farming. ---
            var officer = BuildOfficer("TestOfficer1", position: companion.transform.position + Vector3.forward * 1f);
            followController.FollowingEnabled = true;
            farmhand.SetWorking(true);
            InvokeMethod(assist, "Update");
            float healthAfterFarmingBlocked = GetHealth(officer);
            if (healthAfterFarmingBlocked < 99.9f)
                throw new Exception($"MINI-046 validation: companion attacked while farming (officer health {healthAfterFarmingBlocked}, expected 100 untouched).");

            // --- Now eligible: must land a hit on the in-range officer. ---
            farmhand.SetWorking(false);
            InvokeMethod(assist, "Update"); // rescans officers
            InvokeMethod(assist, "Update"); // commits the timed swing (cooldown already clear)
            Physics.SyncTransforms();
            assist.AdvanceAttack(0.25f); // crosses windup into the single active contact window
            float healthAfterAssist = GetHealth(officer);
            if (healthAfterAssist > 90f)
                throw new Exception($"MINI-046 validation: eligible companion did not land a hit on an in-range officer (health {healthAfterAssist}, expected a real drop from 100).");

            // --- SimpleMeleeCombat.CalculateAppliedDamage(): base damage with the companion far away/locked. ---
            switcher.SetLocked(1, true); // pretend companion is away on the Guadeloupe run
            float soloDamage = meleeCombat.CalculateAppliedDamage();

            // --- CalculateAppliedDamage(): boosted with the companion nearby and free. ---
            switcher.SetLocked(1, false);
            companion.transform.position = player.transform.position + Vector3.right * 1.5f; // well within togetherRange
            float togetherDamage = meleeCombat.CalculateAppliedDamage();

            if (togetherDamage <= soloDamage * 1.1f)
                throw new Exception($"MINI-046 validation: together-bonus not applied - solo damage {soloDamage}, companion-nearby damage {togetherDamage} (expected meaningfully more, ~1.5x).");

            // --- And it must turn back off once out of range again. ---
            switcher.SetLocked(1, false);
            companion.transform.position = player.transform.position + Vector3.right * 50f; // far outside togetherRange
            float outOfRangeDamage = meleeCombat.CalculateAppliedDamage();
            if (outOfRangeDamage > soloDamage * 1.1f)
                throw new Exception($"MINI-046 validation: together-bonus still applied with the companion 50m away (damage {outOfRangeDamage}, expected back down to solo {soloDamage}).");

            Debug.Log($"MINI-046 COMPANION COMBAT VALIDATION PASS: companion declines to fight while farming, lands a real hit once eligible, and the player's own damage is {togetherDamage} together vs. {soloDamage} solo (~{togetherDamage / soloDamage:F2}x) - and drops back to solo once the companion is far away again.");

            UnityEngine.Object.DestroyImmediate(player);
            UnityEngine.Object.DestroyImmediate(companion);
            UnityEngine.Object.DestroyImmediate(officer);
            UnityEngine.Object.DestroyImmediate(switchGo);
            UnityEngine.Object.DestroyImmediate(economyGo);
        }

        private static (GameObject root, HumanoidAnimationManager anim) BuildCharacter(string name)
        {
            var root = new GameObject(name);
            root.AddComponent<CharacterController>();
            var anim = root.AddComponent<HumanoidAnimationManager>();
            InvokeMethod(anim, "Awake"); // no baked states/animator; PlayAction will just no-op harmlessly
            return (root, anim);
        }

        private static GameObject BuildOfficer(string name, Vector3 position)
        {
            var root = new GameObject(name);
            root.transform.position = position;
            // The real scene carries a CharacterController; this focused
            // harness uses a simple collider and explicitly registers the
            // health component because Edit Mode does not run OnEnable in
            // exactly the same way as a built player.
            root.AddComponent<CapsuleCollider>();
            var npc = root.AddComponent<TownNPCInteractable>();
            var so = new SerializedObject(npc);
            so.FindProperty("role").enumValueIndex = (int)NpcRole.Police;
            so.ApplyModifiedPropertiesWithoutUndo();
            var health = root.AddComponent<NpcCombatHealth>();
            InvokeMethod(health, "Awake");
            if (!NpcCombatHealth.All.Contains(health)) NpcCombatHealth.All.Add(health);
            return root;
        }

        private static float GetHealth(GameObject officerRoot)
        {
            var health = officerRoot.GetComponent<NpcCombatHealth>();
            var field = typeof(NpcCombatHealth).GetField("health", BindingFlags.Instance | BindingFlags.NonPublic);
            return (float)field.GetValue(health);
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(target, value);
        }

        private static void InvokeMethod(object target, string name)
        {
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);
        }
    }
}
