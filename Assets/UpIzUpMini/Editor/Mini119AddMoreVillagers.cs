using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Combat;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-119 follow-up, user: "make some the villager walk
    /// through highland road more and also more villagers on lalay...
    /// lalay would be the entire road" and "highland to start from
    /// after the bridge" (confirmed: Bridge_03_user_highland_lalay_inroad,
    /// the only bridge actually named for this transition and the
    /// closest one to the farm - see Mini119BridgeLocateCheck's
    /// read-only survey).
    ///
    /// Additive-only, same convention as Mini119AttachNpcRagdoll: patches
    /// the LIVE GrandBayProof scene directly with new Villager NPCs
    /// rather than re-running the full Phase B world builder, which
    /// would risk the same manual-placement damage documented there.
    /// Each new villager gets the exact same treatment
    /// Mini011PhaseBSetup's generic Villager patrol path now gives them
    /// (HumanoidAnimationManager + NpcCombatHealth + NpcRagdoll, per
    /// this session's earlier ragdoll-on-hit work) plus a PatrolNPC
    /// walking a short stretch of the real road.
    ///
    /// Placement: the bridge sits at world X ~81 (Z ~-147). Lalay (west
    /// of the bridge, "the entire road" - from the Dog Life block past
    /// the market to the bridge) gets 4 new villagers; Highland (east of
    /// the bridge, the farm road) gets 3.
    /// Delete after use.</summary>
    public static class Mini119AddMoreVillagers
    {
        private static readonly string[] Models =
        {
            "Assets/Floreswa/Models/male01_2.fbx",
            "Assets/Floreswa/Models/male02_1.fbx",
            "Assets/Floreswa/Models/male02_3.fbx",
            "Assets/Floreswa/Models/male03_2.fbx",
            "Assets/Floreswa/Models/male01_3.fbx",
            "Assets/Floreswa/Models/male02_2.fbx",
            "Assets/Floreswa/Models/male03_3.fbx",
        };

        // (name, x, z, patrol half-length along the road, forward.x, forward.z)
        private static readonly (string name, float x, float z, float half, float fx, float fz)[] LalaySpots =
        {
            ("NPC_Villager_LalayWest",   -52f, -152.5f, 9f, 1f, 0.05f),
            ("NPC_Villager_LalayMarket",  -6f, -140.0f, 9f, 1f, 0.05f),
            ("NPC_Villager_LalayMid",     26f, -156.0f, 9f, 1f, 0.05f),
            ("NPC_Villager_LalayEast",    64f, -150.0f, 9f, 1f, 0.05f),
        };

        // MINI-119 follow-up fix, user: "i dont want people through the
        // farm are where the hedge is inside." NPC_Villager_HighlandFarm
        // originally sat at (116,-128), confirmed INSIDE
        // HighlandFarmPrivacyBushes' hedge rectangle via
        // Mini119FarmHedgeCheck - moved to (103,-144), just outside the
        // farm's own entrance gap, patrolling side-to-side there instead
        // of through the interior. (Also fixed directly on the live
        // scene's already-placed instance via
        // Mini119FixHighlandFarmVillager - this source update only
        // matters for a future full rebuild.)
        private static readonly (string name, float x, float z, float half, float fx, float fz)[] HighlandSpots =
        {
            ("NPC_Villager_HighlandInroad",  92f, -144f, 8f, 0.6f, 0.8f),
            ("NPC_Villager_HighlandFarm",   103f, -144f, 8f, 0.9f, -0.44f),
            ("NPC_Villager_HighlandUpper",  128f, -108f, 8f, 0.2f, 1f),
        };

        [MenuItem("Up Iz Up Mini/MINI-119/Add More Villagers - Lalay+Highland (one-off)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            // MINI-119 follow-up fix: this scene has no Unity Terrain
            // component at all (confirmed via Mini119BridgeLocateCheck -
            // the ground is collider-based, same as VehicleSpawnController's
            // own GroundSnap technique). SampleHeight(Terrain,...) doesn't
            // apply here; ground height is found by raycast instead.

            // Reuse the exact controller already driving every other
            // NPC's idle/walk blend, rather than rebuilding one.
            var referenceVillager = GameObject.Find("NPC_Villager");
            var referenceAnimator = referenceVillager != null ? referenceVillager.GetComponentInChildren<Animator>() : null;
            var animController = referenceAnimator != null ? referenceAnimator.runtimeAnimatorController : null;
            if (animController == null)
            {
                Debug.LogError("MINI-119 ADD VILLAGERS: couldn't find a reference Animator controller from NPC_Villager - aborting.");
                return;
            }

            var instantiateCharacter = typeof(Mini011PhaseBSetup).GetMethod("InstantiateCharacter", BindingFlags.NonPublic | BindingFlags.Static);
            var addAnimManager = typeof(Mini011PhaseBSetup).GetMethod("AddHumanoidAnimationManager", BindingFlags.NonPublic | BindingFlags.Static);

            int added = 0;
            added += PlaceGroup(LalaySpots, animController, instantiateCharacter, addAnimManager, "Lalay");
            added += PlaceGroup(HighlandSpots, animController, instantiateCharacter, addAnimManager, "Highland");

            Debug.Log($"MINI-119 ADD VILLAGERS: added {added} new villager NPCs (skipped any whose name already existed).");

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("MINI-119 ADD VILLAGERS: scene saved.");
        }

        private static Vector3 GroundSnap(Vector3 pos)
        {
            if (Physics.Raycast(pos + Vector3.up * 40f, Vector3.down, out RaycastHit hit, 90f))
                return hit.point;
            return pos;
        }

        private static int PlaceGroup(
            (string name, float x, float z, float half, float fx, float fz)[] spots,
            RuntimeAnimatorController animController,
            MethodInfo instantiateCharacter, MethodInfo addAnimManager,
            string zoneLabel)
        {
            int count = 0;
            for (int i = 0; i < spots.Length; i++)
            {
                var (name, x, z, half, fx, fz) = spots[i];
                if (GameObject.Find(name) != null)
                {
                    Debug.Log($"MINI-119 ADD VILLAGERS: {name} already exists - skipping (idempotent).");
                    continue;
                }

                Vector3 pos = GroundSnap(new Vector3(x, 20f, z));
                Vector3 forward = new Vector3(fx, 0f, fz).normalized;

                var go = new GameObject(name);
                go.transform.position = pos;
                go.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

                string modelPath = Models[i % Models.Length];
                var visual = (GameObject)instantiateCharacter.Invoke(null, new object[] { modelPath, go.transform, animController, null, false, 1f });

                var npc = go.AddComponent<TownNPCInteractable>();
                var so = new SerializedObject(npc);
                so.FindProperty("role").enumValueIndex = (int)NpcRole.Villager;
                so.FindProperty("npcName").stringValue = name.Replace("NPC_", "");
                so.ApplyModifiedPropertiesWithoutUndo();

                var cc = go.AddComponent<CharacterController>();
                cc.center = new Vector3(0f, 0.95f, 0f);
                cc.height = 1.85f;
                cc.radius = 0.32f;

                var animationManager = (HumanoidAnimationManager)addAnimManager.Invoke(null, new object[] { go, visual.GetComponentInChildren<Animator>() });
                var combatHealth = go.AddComponent<NpcCombatHealth>();
                var ragdoll = go.AddComponent<NpcRagdoll>();
                var chSo = new SerializedObject(combatHealth);
                chSo.FindProperty("animationManager").objectReferenceValue = animationManager;
                chSo.FindProperty("ragdoll").objectReferenceValue = ragdoll;
                chSo.ApplyModifiedPropertiesWithoutUndo();

                var patrol = go.AddComponent<PatrolNPC>();
                Vector3 a = GroundSnap(new Vector3(pos.x + forward.x * half, 20f, pos.z + forward.z * half));
                Vector3 b = GroundSnap(new Vector3(pos.x - forward.x * half, 20f, pos.z - forward.z * half));
                var pso = new SerializedObject(patrol);
                var wp = pso.FindProperty("waypoints");
                wp.arraySize = 2;
                wp.GetArrayElementAtIndex(0).vector3Value = a;
                wp.GetArrayElementAtIndex(1).vector3Value = b;
                pso.FindProperty("reactsToHeat").boolValue = false;
                pso.ApplyModifiedPropertiesWithoutUndo();

                Debug.Log($"MINI-119 ADD VILLAGERS: placed {name} ({zoneLabel}) at {pos}, patrol {a}<->{b}.");
                count++;
            }
            return count;
        }
    }
}
