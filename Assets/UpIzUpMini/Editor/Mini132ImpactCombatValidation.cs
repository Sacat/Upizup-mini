using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UpIzUpMini.Combat;
using UpIzUpMini.Interaction;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    public static class Mini132ImpactCombatValidation
    {
        [MenuItem("Up Iz Up Mini/MINI-132/Validate Impact Combat")]
        public static void Validate()
        {
            Expect(MeleeMoveLibrary.FrankiDamageMultiplier > 1f,
                "Franki is not stronger than Sacat.");
            Expect(MeleeMoveLibrary.FrankiImpactMultiplier > 1f,
                "Franki has no heavier impact.");

            ValidateNpcChain(NpcFighterStyle.Police, 14f);
            ValidateNpcChain(NpcFighterStyle.Gang, 14f);
            Expect(NpcCombatHealth.PolicyForRole(NpcRole.FarmShop) == NpcDefeatPolicy.RecoverAndReturnToPost,
                "Farm seller is not protected/recovering.");
            Expect(NpcCombatHealth.PolicyForRole(NpcRole.Buyer) == NpcDefeatPolicy.RecoverAndReturnToPost,
                "Buyer is not protected/recovering.");
            Expect(NpcCombatHealth.PolicyForRole(NpcRole.Police) == NpcDefeatPolicy.FadeAndRespawnAtHome,
                "Police does not use world respawn.");

            var vehicle = new GameObject("MINI132_Vehicle");
            try
            {
                vehicle.AddComponent<Rigidbody>();
                var responder = VehicleImpactResponder.Ensure(vehicle);
                Expect(responder != null, "Vehicle responder could not be installed.");
                Expect(ReferenceEquals(responder, VehicleImpactResponder.Ensure(vehicle)),
                    "Vehicle responder duplicated itself.");
                Expect(responder.DamageForSpeed(16f) > responder.DamageForSpeed(5f),
                    "Vehicle damage does not increase with impact speed.");
            }
            finally { UnityEngine.Object.DestroyImmediate(vehicle); }

            string root = Path.GetFullPath("Assets/UpIzUpMini/Scripts/Vehicles");
            foreach (string file in new[] { "BikeInteractable.cs", "CarInteractable.cs", "SuperMotoVehicleInteractable.cs" })
            {
                string source = File.ReadAllText(Path.Combine(root, file));
                Expect(source.Contains("VehicleImpactResponder.Ensure(gameObject)"),
                    file + " is not wired to shared collision damage.");
            }

            Debug.Log("MINI-132 IMPACT COMBAT PASS: Franki strength, slower/weaker NPC combo tables, all vehicle adapters, speed-scaled damage, seller recovery, and ambient police respawn policies validated.");
        }

        static void ValidateNpcChain(NpcFighterStyle style, float maxDamage)
        {
            Expect(NpcCombatMoveLibrary.Count(style) >= 3, style + " has no real combination.");
            string first = null;
            bool varied = false;
            for (int i = 0; i < NpcCombatMoveLibrary.Count(style); i++)
            {
                var move = NpcCombatMoveLibrary.Get(style, i);
                Expect(move.damage > 0f && move.damage <= maxDamage,
                    style + " damage exceeds the weaker-NPC budget.");
                Expect(move.TotalSeconds >= .9f, style + " attacks too quickly.");
                if (i == 0) first = move.id;
                else if (!string.Equals(first, move.id, StringComparison.Ordinal)) varied = true;
            }
            Expect(varied, style + " repeats only one animation.");
        }

        static void Expect(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("MINI-132: " + message);
        }
    }
}
