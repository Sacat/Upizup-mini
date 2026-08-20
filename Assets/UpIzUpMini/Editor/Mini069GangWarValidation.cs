using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Combat;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-069 static validation: the rebound controls, the phone call key,
    /// Chevy's position, and that BOTH gangs are actually equipped to fight.
    ///
    /// The brawl itself is behaviour over time and cannot be exercised here -
    /// Play Mode does not tick in this batch environment (documented since
    /// MINI-001). What this proves is that every fighter has the component,
    /// on the correct side, with a stand-down rule set - i.e. that a Play Mode
    /// test is worth running at all.
    /// </summary>
    public static class Mini069GangWarValidation
    {
        [MenuItem("Up Iz Up Mini/MINI-069/Validate Gang War + Controls")]
        public static void Validate()
        {
            bool pass = true;
            string fail = null;

            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            // --- Controls -------------------------------------------------
            // MINI-080 removed the parked test TMAX from the built scene
            // (IncludeParkedTestVehicles = false) so testing doesn't leave a
            // free bike sitting by the safehouse. The bike's control wiring
            // still needs checking, so fall back to the TMAX_560 prefab
            // itself - that is what any purchased/spawned bike is cloned
            // from, so checking it is at least as strong a guarantee.
            var bike = Object.FindFirstObjectByType<Vehicles.BikeInteractable>();
            GameObject bikePrefab = null;
            if (bike == null)
            {
                bikePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UpIzUpMini/Vehicles/TMAX_560.prefab");
                if (bikePrefab != null) bike = bikePrefab.GetComponent<Vehicles.BikeInteractable>();
            }
            Check(ref pass, ref fail, bike != null, "no BikeInteractable in the scene or on TMAX_560.prefab.");
            if (bike != null)
            {
                var so = new SerializedObject(bike);
                Check(ref pass, ref fail,
                    so.FindProperty("wheelieKey").enumValueIndex == (int)KeyCode.E ||
                    so.FindProperty("wheelieKey").intValue == (int)KeyCode.E,
                    "the wheelie key is not E.");
                Check(ref pass, ref fail,
                    so.FindProperty("mountKey").intValue == (int)KeyCode.F ||
                    so.FindProperty("mountKey").enumValueIndex == (int)KeyCode.F,
                    "the mount key is not F.");
                Check(ref pass, ref fail,
                    so.FindProperty("dismountKey").intValue == (int)KeyCode.F ||
                    so.FindProperty("dismountKey").enumValueIndex == (int)KeyCode.F,
                    "the dismount key is not F.");
                // The look keys were removed outright, not rebound - if these
                // ever come back they will have quietly re-taken Q and E.
                Check(ref pass, ref fail, so.FindProperty("lookLeftKey") == null,
                    "lookLeftKey is back on BikeInteractable - it should be gone, not rebound.");
                Check(ref pass, ref fail, so.FindProperty("lookRightKey") == null,
                    "lookRightKey is back on BikeInteractable - it should be gone, not rebound.");
            }

            var phone = Object.FindFirstObjectByType<CellPhoneController>();
            Check(ref pass, ref fail, phone != null, "no CellPhoneController in the scene.");
            if (phone != null)
            {
                var pso = new SerializedObject(phone);
                Check(ref pass, ref fail, pso.FindProperty("callKey").intValue == (int)KeyCode.Q,
                    "the cell phone call key is not Q.");
            }

            // --- Chevy beside the recruiter -------------------------------
            var recruiter = GameObject.Find("NPC_GangRecruiter");
            Check(ref pass, ref fail, recruiter != null, "NPC_GangRecruiter not found.");

            GangMemberController chevy = null;
            foreach (var m in Object.FindObjectsByType<GangMemberController>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (m.MemberName == "Zoomy") { chevy = m; break; }
            }
            Check(ref pass, ref fail, chevy != null, "Zoomy not found among the Not Ah Word roster.");
            if (chevy != null && recruiter != null)
            {
                float d = Vector3.Distance(chevy.transform.position, recruiter.transform.position);
                Check(ref pass, ref fail, d <= 6f,
                    $"Zoomy is {d:F1}m from the recruiter - he is meant to stand next to him.");
            }

            // --- Both gangs can fight -------------------------------------
            int notAhWord = 0, dogLife = 0;
            foreach (var b in Object.FindObjectsByType<FactionBrawler>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (b.Allegiance == FactionBrawler.Side.NotAhWord) notAhWord++;
                else dogLife++;
            }

            // Two main characters plus the four-strong recruit roster.
            Check(ref pass, ref fail, notAhWord >= 6,
                $"expected at least 6 Not Ah Word brawlers (2 mains + 4 roster), found {notAhWord}.");
            Check(ref pass, ref fail, dogLife > 0,
                "no Dog Life member has a FactionBrawler - they could not fight back.");

            // The stand-down rule the user asked for, checked on a real instance
            // rather than trusting the field default.
            var anyBrawler = Object.FindFirstObjectByType<FactionBrawler>(FindObjectsInactive.Include);
            if (anyBrawler != null)
            {
                var bso = new SerializedObject(anyBrawler);
                Check(ref pass, ref fail, bso.FindProperty("standDownWhenEnemiesLeft").intValue == 1,
                    "standDownWhenEnemiesLeft is not 1 - the 'stop when one rival is left' rule would not fire.");
                Check(ref pass, ref fail,
                    bso.FindProperty("chaseRange").floatValue > bso.FindProperty("engageRange").floatValue,
                    "chaseRange must exceed engageRange or nobody actually chases.");
            }

            if (pass)
                Debug.Log("MINI-069 VALIDATION PASS: wheelie is E, mount/dismount is F, the dead look-keys are gone, the phone call is Q, Chevy stands beside the recruiter, and both gangs carry FactionBrawler (Not Ah Word on the mains + roster, Dog Life on the rivals) with chaseRange > engageRange and the stand-down-at-one rule set. NOT covered: the fight itself - who wins, whether the chase reads well, whether the stand-down looks right - which needs Play Mode, since Update() never ticks in this batch environment.");
            else
                Debug.LogError($"MINI-069 VALIDATION FAIL: {fail}");
        }

        private static void Check(ref bool pass, ref string fail, bool condition, string message)
        {
            if (pass && !condition) { pass = false; fail = message; }
        }
    }
}
