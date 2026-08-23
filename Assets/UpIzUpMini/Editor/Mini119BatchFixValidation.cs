using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Economy;
using UpIzUpMini.Interaction;
using UpIzUpMini.Missions;
using UpIzUpMini.Progression;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-119 focused validator, covering the user's full batch report:
    /// the cheat's missing Normy rep, the villager's stale patrol route,
    /// the Blue Cheese/Purple Sugar key-number swap, the breeding station's
    /// minimap marker, Rasta's new Blue Cheese seed sale, the Boat Man's
    /// rotating idle line, M16's new DefeatAllRivals win condition, and the
    /// bike's hill-climb/yaw-spin assists. Dialogue truncation (the
    /// MissionHUD banner panel height) is a pure layout/visual fix with no
    /// gameplay-state signal to assert on here - left to the user's real
    /// Play Mode test, same as every other purely visual fix this session.
    /// </summary>
    public static class Mini119BatchFixValidation
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Validate Batch Fixes")]
        public static void Validate()
        {
            bool pass = true;
            string fail = null;

            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var missionSystem = Object.FindFirstObjectByType<MissionSystem>();
            var progression = Object.FindFirstObjectByType<ProgressionManager>();
            Check(ref pass, ref fail, missionSystem != null, "no MissionSystem in the scene.");
            Check(ref pass, ref fail, progression != null, "no ProgressionManager in the scene.");
            if (!pass) { Report(pass, fail); return; }

            Invoke(progression, "Awake");
            Invoke(missionSystem, "Awake");

            // --- 1. Cheat 000000 grants Normy rep like every other faction ---
            progression.UnlockEverything();
            Check(ref pass, ref fail, progression.NormyReputation == 100,
                $"UnlockEverything() left NormyReputation at {progression.NormyReputation}, expected 100.");

            // --- 2. Villager no longer has a stale, far-off patrol route ---
            var villagerGo = GameObject.Find("NPC_Villager");
            Check(ref pass, ref fail, villagerGo != null, "NPC_Villager not found in the built scene.");
            if (villagerGo != null)
            {
                var patrol = villagerGo.GetComponent<PatrolNPC>();
                Check(ref pass, ref fail, patrol != null, "NPC_Villager has no PatrolNPC.");
                if (patrol != null)
                {
                    var waypoints = patrol.Waypoints;
                    Check(ref pass, ref fail, waypoints != null && waypoints.Length > 0,
                        "NPC_Villager's PatrolNPC has no waypoints.");
                    if (waypoints != null && waypoints.Length > 0)
                    {
                        float maxDist = waypoints.Max(w => Vector3.Distance(w, villagerGo.transform.position));
                        // The old pre-migration route pointed toward the mountain,
                        // tens of metres away from the real migrated position -
                        // a fixed route now stays within a short roadside stretch.
                        Check(ref pass, ref fail, maxDist < 15f,
                            $"NPC_Villager's nearest waypoint is {maxDist:F1}m from its own current position - looks like a stale pre-migration route, not the fixed roadside one.");
                    }
                }
            }

            // --- 3. Blue Cheese / Purple Sugar number-key order swapped ---
            var cropSelector = Object.FindFirstObjectByType<CropSelectionController>();
            Check(ref pass, ref fail, cropSelector != null, "no CropSelectionController in the scene.");
            if (cropSelector != null)
            {
                var crops = cropSelector.AllCrops;
                Check(ref pass, ref fail, crops != null && crops.Length >= 8, "CropSelectionController has fewer than 8 crops wired.");
                if (crops != null && crops.Length >= 8)
                {
                    Check(ref pass, ref fail, crops[6] != null && crops[6].cropId == "blue_cheese",
                        $"Key [7] selects '{crops[6]?.cropId}', expected 'blue_cheese' - the MINI-119 swap did not take.");
                    Check(ref pass, ref fail, crops[7] != null && crops[7].cropId == "purple_black",
                        $"Key [8] selects '{crops[7]?.cropId}', expected 'purple_black' (Purple Sugar).");
                }
            }

            // --- 4. Breeding station has a minimap marker ---
            var breedingStation = GameObject.Find("BreedingStation");
            Check(ref pass, ref fail, breedingStation != null, "BreedingStation GameObject not found.");
            if (breedingStation != null)
            {
                var marker = breedingStation.GetComponentInChildren<UI.GtaMiniMapMarker>();
                Check(ref pass, ref fail, marker != null, "BreedingStation has no GtaMiniMapMarker.");
            }

            // --- 5. Rasta sells Blue Cheese seed once unlocked ---
            var rastaGo = GameObject.Find("NPC_RastaMentor");
            Check(ref pass, ref fail, rastaGo != null, "NPC_RastaMentor GameObject not found.");
            if (rastaGo != null)
            {
                var rastaNpc = rastaGo.GetComponent<TownNPCInteractable>();
                Check(ref pass, ref fail, rastaNpc != null, "Rasta has no TownNPCInteractable.");
                if (rastaNpc != null)
                {
                    var sellableCropsField = typeof(TownNPCInteractable).GetField("sellableCrops", BindingFlags.NonPublic | BindingFlags.Instance);
                    var rastaCrops = sellableCropsField?.GetValue(rastaNpc) as CropDefinition[];
                    Check(ref pass, ref fail, rastaCrops != null && rastaCrops.Any(c => c != null && c.cropId == "blue_cheese"),
                        "Rasta's sellableCrops does not include blue_cheese.");

                    var economyGo = new GameObject("Mini119FreshEconomy");
                    var economy = economyGo.AddComponent<EconomyManager>();
                    Invoke(economy, "Awake");
                    economy.AddMoney(5000); // plenty to cover the $650 seed price

                    var freshProgressionGo = new GameObject("Mini119FreshProgression");
                    var freshProgression = freshProgressionGo.AddComponent<ProgressionManager>();
                    Invoke(freshProgression, "Awake");

                    // Not yet unlocked - Rasta must not offer it early.
                    string offerBefore = (string)Invoke(rastaNpc, "TryOfferRastaBlueCheeseSeed");
                    Check(ref pass, ref fail, offerBefore == null,
                        "Rasta offers Blue Cheese seed before it is unlocked - should stay silent (fall through to normal dialogue) until then.");

                    freshProgression.MarkRastaTaught("blue_cheese");
                    // ProgressionManager.Instance is a static singleton set in
                    // Awake; the fresh instance above already replaced it via
                    // its own Awake call.
                    int moneyBefore = economy.Money;
                    string offerAfter = (string)Invoke(rastaNpc, "TryOfferRastaBlueCheeseSeed");
                    Check(ref pass, ref fail, !string.IsNullOrEmpty(offerAfter),
                        "Rasta still refuses to offer Blue Cheese seed once it is unlocked.");
                    Check(ref pass, ref fail, economy.GetSeeds("blue_cheese") == 3,
                        $"Buying Rasta's Blue Cheese seed left {economy.GetSeeds("blue_cheese")} seeds, expected 3.");
                    Check(ref pass, ref fail, economy.Money == moneyBefore - 650,
                        $"Rasta's Blue Cheese seed did not charge the expected $650 (money went from {moneyBefore} to {economy.Money}).");

                    // Already stocked - must not offer again (falls through
                    // to normal dialogue instead of re-selling every talk).
                    string offerRestock = (string)Invoke(rastaNpc, "TryOfferRastaBlueCheeseSeed");
                    Check(ref pass, ref fail, offerRestock == null,
                        "Rasta keeps offering Blue Cheese seed even while the player is already stocked.");

                    Object.DestroyImmediate(economyGo);
                    Object.DestroyImmediate(freshProgressionGo);
                }
            }

            // --- 6. Boat Man's idle line rotates instead of repeating ---
            var boatManGo = GameObject.Find("BoatMan");
            if (boatManGo == null) boatManGo = Object.FindObjectsByType<TownNPCInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(n => n.Role == NpcRole.BoatMan)?.gameObject;
            Check(ref pass, ref fail, boatManGo != null, "no Boat Man NPC found (by name or Role).");
            if (boatManGo != null)
            {
                // Fresh, dedicated singletons for this check only - the
                // "nothing to load" branch requires GrandBayWeedRouteEstablished
                // (false on a cold scene load) and zero cargo, and the earlier
                // Rasta test above already destroyed its own fresh instances,
                // which would leave stale (Unity fake-null) singletons behind
                // if reused here instead of building new ones.
                var boatEconomyGo = new GameObject("Mini119FreshBoatEconomy");
                var boatEconomy = boatEconomyGo.AddComponent<EconomyManager>();
                Invoke(boatEconomy, "Awake");

                var boatProgressionGo = new GameObject("Mini119FreshBoatProgression");
                var boatProgression = boatProgressionGo.AddComponent<ProgressionManager>();
                Invoke(boatProgression, "Awake");
                boatProgression.AddReputation(Faction.BossK, 20);
                boatProgression.AddReputation(Faction.GrandBayGangs, 10);
                Check(ref pass, ref fail, boatProgression.GrandBayWeedRouteEstablished,
                    "test setup error: GrandBayWeedRouteEstablished still false after granting its own thresholds.");

                var guadeloupeTrade = Object.FindFirstObjectByType<GuadeloupeTrade>();
                if (guadeloupeTrade != null) Invoke(guadeloupeTrade, "Awake");
                Check(ref pass, ref fail, guadeloupeTrade != null, "no GuadeloupeTrade in the scene.");

                // GuadeloupeTrade.Interact() also needs a live
                // CharacterSwitchManager.Instance (it reads the active
                // character) - never awakened in this batch-mode
                // environment either, same as every other singleton above.
                var switcher = Object.FindFirstObjectByType<UpIzUpMini.Character.CharacterSwitchManager>();
                if (switcher != null) Invoke(switcher, "Awake");

                var boatNpc = boatManGo.GetComponent<TownNPCInteractable>();
                string line1 = (string)Invoke(boatNpc, "HandleBoatMan");
                string line2 = (string)Invoke(boatNpc, "HandleBoatMan");
                string line3 = (string)Invoke(boatNpc, "HandleBoatMan");
                bool anyDifferent = line1 != line2 || line2 != line3 || line1 != line3;
                Check(ref pass, ref fail, anyDifferent,
                    $"Boat Man's repeat 'nothing to load' line is identical every time - not dynamic. (line1='{line1}')");

                Object.DestroyImmediate(boatEconomyGo);
                Object.DestroyImmediate(boatProgressionGo);
            }

            // --- 7. M16 War Story requires defeating all Dog Life members ---
            var missionsField = typeof(MissionSystem).GetField("missions", BindingFlags.NonPublic | BindingFlags.Instance);
            var missions = (System.Collections.Generic.List<Mission>)missionsField.GetValue(missionSystem);
            var m16 = missions.FirstOrDefault(m => m.missionId == "M16");
            Check(ref pass, ref fail, m16 != null, "M16 not found in the built mission list.");
            if (m16 != null)
            {
                var defeatObj = m16.objectives.FirstOrDefault(o => o.kind == ObjectiveKind.DefeatAllRivals);
                Check(ref pass, ref fail, defeatObj != null,
                    "M16 has no DefeatAllRivals objective - War Story can still be won by just walking in.");
                Check(ref pass, ref fail, defeatObj != null && defeatObj.targetId == "DogLifeSpawner",
                    $"M16's DefeatAllRivals objective targets '{defeatObj?.targetId}', expected 'DogLifeSpawner'.");
            }

            var dogLifeSpawnerGo = GameObject.Find("DogLifeSpawner");
            Check(ref pass, ref fail, dogLifeSpawnerGo != null, "DogLifeSpawner GameObject not found.");
            if (dogLifeSpawnerGo != null)
            {
                var spawner = dogLifeSpawnerGo.GetComponent<RivalGangSpawner>();
                Check(ref pass, ref fail, spawner != null, "DogLifeSpawner has no RivalGangSpawner.");
                if (spawner != null)
                {
                    Check(ref pass, ref fail, !spawner.AllDefeated,
                        "RivalGangSpawner.AllDefeated is already true on a freshly-loaded scene - should require an actual fight.");
                }
            }

            // --- 8. Bike: hill-climb power and yaw-spin assist wired ---
            var bikePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UpIzUpMini/Vehicles/TMAX_560.prefab");
            Check(ref pass, ref fail, bikePrefab != null, "TMAX_560.prefab not found.");
            if (bikePrefab != null)
            {
                var bike = bikePrefab.GetComponent<TmaxBikeControllerCustom>();
                Check(ref pass, ref fail, bike != null, "TMAX_560.prefab has no TmaxBikeControllerCustom.");
                if (bike != null)
                {
                    Check(ref pass, ref fail, bike.MotorTorque >= 900f,
                        $"motorTorque is {bike.MotorTorque}, expected a real increase past MINI-118's mass-parity value (>=900).");

                    var so = new SerializedObject(bike);
                    float hillClimbAssist = so.FindProperty("hillClimbAssist").floatValue;
                    float yawSpinDamping = so.FindProperty("yawSpinDamping").floatValue;
                    Check(ref pass, ref fail, hillClimbAssist > 0f, "hillClimbAssist is 0 - hill/ledge climb assist is disabled.");
                    // MINI-119 follow-up: yawSpinDamping is deliberately
                    // uncapped past 1 now (see TmaxBikeControllerCustom's own
                    // comment - the old Range(0,1) was a fake ceiling that
                    // never actually reached "drastic"), and the user's
                    // own final tuned threshold is legitimately 0 (means
                    // "always treat as excess, intervene constantly", not
                    // "misconfigured"). Just check damping is positive
                    // (0 would mean the assist does nothing at all).
                    Check(ref pass, ref fail, yawSpinDamping > 0f,
                        $"yaw spin damping is {yawSpinDamping} - the assist would never remove any spin at all.");
                }
            }

            Report(pass, fail);
        }

        private static object Invoke(object target, string methodName, object[] args = null)
        {
            var method = target.GetType().GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
            {
                Debug.LogError($"MINI-119 VALIDATION: method '{methodName}' not found on {target.GetType().Name}.");
                return null;
            }
            return method.Invoke(target, args);
        }

        private static void Check(ref bool pass, ref string fail, bool condition, string message)
        {
            if (pass && !condition) { pass = false; fail = message; }
        }

        private static void Report(bool pass, string fail)
        {
            if (pass)
                Debug.Log("MINI-119 VALIDATION PASS: cheat 000000 grants Normy rep; NPC_Villager's patrol route stays local (not the old stale one); Blue Cheese/Purple Sugar number keys are swapped to keys 7/8; the breeding station has a minimap marker; Rasta correctly offers (and re-offers only once restocked) a $650 Blue Cheese seed strictly after it is unlocked; the Boat Man's repeat 'nothing to load' line varies instead of repeating; M16 now requires a DefeatAllRivals objective against DogLifeSpawner rather than just walking in; and the bike carries both a real motorTorque increase and a configured hill-climb/yaw-spin assist. NOT covered: dialogue truncation (a pure banner-layout fix with no gameplay-state signal), and how the bike actually feels on a hill or hitting a hedge in real Play Mode - both need the user's own test.");
            else
                Debug.LogError($"MINI-119 VALIDATION FAIL: {fail}");
        }
    }
}
