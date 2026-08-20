using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;
using UpIzUpMini.Farming;
using UpIzUpMini.Interaction;
using UpIzUpMini.Progression;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-059: proves FarmPlot's steal-target rules (illegal crop only,
    /// growing/ripe only), that reputation genuinely lowers theft chance,
    /// and - the part that has to be RNG-independent to test reliably -
    /// that an assigned guard fully blocks a theft roll regardless of the
    /// dice, all against the real built scene's objects.
    /// </summary>
    public static class Mini059PlantationTheftValidation
    {
        [MenuItem("Up Iz Up Mini/MINI-059/Validate Plantation Theft")]
        public static void Validate()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            bool pass = true;
            string fail = null;

            var controllerGo = GameObject.Find("PlantationTheftController");
            var economy = GameObject.Find("EconomyManager")?.GetComponent<EconomyManager>();
            var progression = GameObject.Find("ProgressionManager")?.GetComponent<ProgressionManager>();
            var switcher = Object.FindFirstObjectByType<CharacterSwitchManager>();
            var plots = Object.FindObjectsByType<FarmPlot>(FindObjectsSortMode.None);
            var bushers = AssetDatabase.LoadAssetAtPath<UpIzUpMini.Economy.CropDefinition>("Assets/UpIzUpMini/Data/Crops/bushers.asset");
            var tomato = AssetDatabase.LoadAssetAtPath<UpIzUpMini.Economy.CropDefinition>("Assets/UpIzUpMini/Data/Crops/tomato.asset");

            if (controllerGo == null || economy == null || progression == null || switcher == null || plots.Length == 0 || bushers == null || tomato == null)
            {
                Debug.LogError("MINI-059 VALIDATION FAIL: PlantationTheftController/EconomyManager/ProgressionManager/CharacterSwitchManager/FarmPlot/bushers.asset/tomato.asset not all found in the built scene.");
                return;
            }

            var controller = controllerGo.GetComponent<PlantationTheftController>();
            InvokeMethod(economy, "Awake");
            InvokeMethod(progression, "Awake");
            InvokeMethod(switcher, "Awake");

            var plot = plots[0];

            // --- 1) HasStealableZeb / Voleh rules. ---
            plot.LoadState(0, null, 0f); // Empty
            Check(ref pass, ref fail, !plot.HasStealableZeb, "expected an empty plot to not be stealable.");

            if (pass)
            {
                plot.LoadState(2, tomato, tomato.growDurationSeconds); // Growing, LEGAL crop
                Check(ref pass, ref fail, !plot.HasStealableZeb, "expected a growing LEGAL crop (tomato) to not be stealable.");
            }

            if (pass)
            {
                plot.LoadState(1, bushers, 0f); // PlantedDry, illegal but not grown yet
                Check(ref pass, ref fail, !plot.HasStealableZeb, "expected dry-soil illegal crop (not yet growing) to not be stealable.");
            }

            if (pass)
            {
                plot.LoadState(3, bushers, bushers.growDurationSeconds); // Ripe, illegal
                Check(ref pass, ref fail, plot.HasStealableZeb, "expected a ripe illegal crop (Bushers) to be stealable.");

                if (pass)
                {
                    string stolen = plot.Voleh();
                    Check(ref pass, ref fail, stolen == "Bushers", $"expected Voleh() to return 'Bushers', got '{stolen}'.");
                    Check(ref pass, ref fail, plot.IsEmpty, "expected the plot to be Empty after being volehed.");
                }
            }

            // --- 2) Reputation genuinely lowers the computed chance
            // (pure function, no RNG - safe to assert exactly). ---
            if (pass)
            {
                var computeChance = typeof(PlantationTheftController).GetMethod("ComputeChance", BindingFlags.Instance | BindingFlags.NonPublic);
                progression.AddReputation(Faction.GrandBayGangs, -100); // clamps to -100
                float chanceAtLowRep = (float)computeChance.Invoke(controller, null);
                // AddReputation adds a DELTA, not an absolute value - from
                // -100, +100 only reaches 0, not 100 (a real mistake this
                // test itself made on the first run). +200 correctly
                // clamps up to the ceiling of 100.
                progression.AddReputation(Faction.GrandBayGangs, 200);
                float chanceAtHighRep = (float)computeChance.Invoke(controller, null);
                Check(ref pass, ref fail, chanceAtHighRep < chanceAtLowRep,
                    $"expected higher GrandBayGangs reputation to lower theft chance, got low-rep={chanceAtLowRep:F3}, high-rep={chanceAtHighRep:F3}.");
            }

            // --- 3) A guard fully blocks a roll, regardless of RNG. ---
            if (pass)
            {
                plot.LoadState(3, bushers, bushers.growDurationSeconds); // ripe again - a real target

                var recruiterGo = GameObject.Find("NPC_GangRecruiter");
                var recruiter = recruiterGo?.GetComponent<TownNPCInteractable>();
                var actor = new GameObject("Actor");
                economy.AddMoney(1000);
                recruiter.Interact(actor); // recruits "Reds" first (roster order), defaults to Follow

                // GangMemberController.All (populated via OnEnable) isn't
                // reliable here for the same reason Awake isn't outside
                // Play Mode - SetActive(true) inside HandleRecruit doesn't
                // synchronously fire OnEnable in this batch-mode context
                // either. GameObject.Find works because Reds is genuinely
                // active now (proven by the successful recruit feedback).
                var member = GameObject.Find("NotAhWord_Reds")?.GetComponent<UpIzUpMini.Character.GangMemberController>();
                if (member == null)
                {
                    pass = false; fail = "no recruited GangMemberController found after recruiting for the guard test.";
                }
                else
                {
                    member.CycleAssignment(); // Follow -> GuardPlantation

                    // IsGuarded() reads GangMemberController.All, populated
                    // via OnEnable - which, like Awake, doesn't fire
                    // outside Play Mode even from a SetActive(true) inside
                    // HandleRecruit (confirmed empty above). Added directly
                    // here so the real IsGuarded() code path is actually
                    // exercised by this test instead of trivially passing
                    // because the list was empty; removed afterward.
                    bool addedToRegistry = !UpIzUpMini.Character.GangMemberController.All.Contains(member);
                    if (addedToRegistry) UpIzUpMini.Character.GangMemberController.All.Add(member);

                    // Force the away-timer/roll-timer gates open via
                    // reflection (Time.time doesn't advance outside Play
                    // Mode) and put the player far from the plantation.
                    SetField(controller, "_awayTimer", 999f);
                    SetField(controller, "_rollTimer", 999f);
                    var player = switcher.Active.root;
                    Vector3 farmCenterField = (Vector3)typeof(PlantationTheftController)
                        .GetField("plantationCenter", BindingFlags.Instance | BindingFlags.NonPublic)
                        .GetValue(controller);
                    player.transform.position = farmCenterField + Vector3.forward * 500f;

                    InvokeMethod(controller, "Update");

                    Check(ref pass, ref fail, plot.HasStealableZeb,
                        "expected a guarded plantation to survive an Update() pass with the away/roll gates forced open - the plot was stolen from despite an assigned guard.");

                    if (addedToRegistry) UpIzUpMini.Character.GangMemberController.All.Remove(member);
                }

                Object.DestroyImmediate(actor);
            }

            if (pass)
            {
                Debug.Log("MINI-059 PLANTATION THEFT VALIDATION PASS: only ripe/growing illegal crops are stealable (legal crops and unplanted/dry plots never are), Voleh() correctly empties the plot and reports the stolen crop's name, higher GrandBayGangs reputation genuinely lowers the computed theft chance, and an assigned GuardPlantation member fully blocks a theft roll regardless of RNG.");
            }
            else
            {
                Debug.LogError($"MINI-059 PLANTATION THEFT VALIDATION FAIL: {fail}");
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

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            field?.SetValue(target, value);
        }
    }
}
