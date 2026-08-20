using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UpIzUpMini.Character;
using UpIzUpMini.Farming;
using UpIzUpMini.Interaction;
using UpIzUpMini.Progression;

namespace UpIzUpMini.EditorTools
{
    /// <summary>Rollback-safe VA-005 environment migration for the generated gameplay scene.</summary>
    public static class Mini100GrandBayMapMigration
    {
        private const string MapLabScene = "Assets/UpIzUpMini/Scenes/MapLab_LalayHighland.unity";
        private static readonly Vector3 HighlandFarmCentre = new Vector3(114f, 0f, -129f);
        private static GameObject s_world;

        public static void ApplyToOpenScene(Scene gameplayScene)
        {
            if (!gameplayScene.IsValid()) throw new InvalidOperationException("MINI-100: gameplay scene is invalid.");
            GameObject oldBoat = DetachChild(GameObject.Find("CoastAndJetty"), "MooredBoat");
            foreach (string name in new[] { "GrandBayTerrain", "Sea", "InvisibleWorldBoundaries", "CoastAndJetty", "LalayRoad", "LalayHouses", "ShantyProps", "Vegetation", "MontineFarmPath" })
                DestroyRoot(gameplayScene, name);

            s_world = CloneApprovedWorld(gameplayScene);
            Physics.SyncTransforms();

            HashSet<string> farmRoots = new HashSet<string> { "MontineFarm", "FarmSafehouse", "BreedingStation" };
            PlaceHighlandFarm(gameplayScene);

            // Map remaining visible gameplay roots from the former synthetic road onto
            // the approved Lalay spine. Components and save-facing object names stay intact.
            foreach (GameObject root in gameplayScene.GetRootGameObjects())
            {
                if (root == null || root == s_world || farmRoots.Contains(root.name) || root.name == "MooredBoat") continue;
                if (!TryVisualBounds(root, out Bounds bounds)) continue;
                if (root.GetComponentInChildren<Canvas>(true) != null) continue;
                if (root.GetComponent<TownNPCInteractable>() != null || root.name.StartsWith("Market_", StringComparison.Ordinal)) continue;
                Vector3 target = MapOldWorldPointToLalay(bounds.center);
                MoveRootByBoundsCentre(root, target);
            }

            PlaceLalayGameplay(gameplayScene);
            MoveNamed(gameplayScene, "LalayHouse", RoadsidePosition(48f, 1, 9f));
            MoveNamed(gameplayScene, "LalayEstate", RoadsidePosition(24f, -1, 10f));
            MoveNamed(gameplayScene, "Sacat", new Vector3(48.5f, 0f, -159f));
            MoveNamed(gameplayScene, "Franki", new Vector3(50.2f, 0f, -160.2f));
            MoveNamed(gameplayScene, "NPC_RastaMentor", new Vector3(108f, 0f, -144f));
            MoveNamed(gameplayScene, "NPC_BoatMan", new Vector3(215f, 0f, -184f));
            if (oldBoat != null)
            {
                SceneManager.MoveGameObjectToScene(oldBoat, gameplayScene);
                MoveRootByBoundsCentre(oldBoat, new Vector3(265f, 0f, -186f));
            }

            Vector3 spawn = Ground(new Vector3(48.5f, 0f, -159f)) + Vector3.up * 0.15f;
            CharacterSwitchManager switcher = UnityEngine.Object.FindFirstObjectByType<CharacterSwitchManager>(FindObjectsInactive.Include);
            if (switcher != null)
            {
                SerializedObject so = new SerializedObject(switcher);
                SerializedProperty property = so.FindProperty("safehouseSpawn");
                if (property != null) property.vector3Value = spawn;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (SafehouseInteractable safehouse in UnityEngine.Object.FindObjectsByType<SafehouseInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                SerializedObject so = new SerializedObject(safehouse);
                SerializedProperty property = so.FindProperty("spawnPoint");
                if (property != null) property.vector3Value = Ground(safehouse.transform.position) + Vector3.up * 0.15f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            UpdatePlantationRiskCentre();

            Physics.SyncTransforms();
            Debug.Log("MINI-100 MAP MIGRATION PASS: approved VA-005 world cloned; old synthetic environment removed; gameplay roots preserved and relocated.");
        }

        private static void PlaceHighlandFarm(Scene gameplayScene)
        {
            // These used to share one bounds-centre target, stacking the safehouse,
            // plots and breeding station on the inroad. Each now owns a separate
            // position inside the flat Highland pad and stays clear of the spur.
            MoveRootBoundsCentre(gameplayScene, "MontineFarm", HighlandFarmCentre);
            MoveRootBoundsCentre(gameplayScene, "FarmSafehouse", new Vector3(125f, 0f, -115f));
            MoveRootBoundsCentre(gameplayScene, "BreedingStation", new Vector3(132f, 0f, -137f));
        }

        private static void PlaceLalayGameplay(Scene gameplayScene)
        {
            (string stall, string npc, float x, int side)[] shops =
            {
                ("Stall_FARM SHOP", "NPC_FarmShop", -8f, 1),
                ("Stall_PRODUCE BUYER", "NPC_Buyer", -8f, -1),
                ("Stall_FOOD", "NPC_FoodShop", 24f, 1),
                ("Stall_CLOTHES", "NPC_ApparelShop", 54f, 1),
                ("Stall_PHARMACY", "NPC_Pharmacy", 82f, 1),
                ("Stall_LAND AND SURVEYS", "NPC_LandOffice", 112f, -1),
                ("Stall_CAR DEALER", "NPC_CarDealer", 145f, 1),
            };

            foreach ((string stall, string npc, float x, int side) shop in shops)
            {
                Vector3 road = RoadCentre(shop.x);
                Vector3 stallTarget = RoadsidePosition(shop.x, shop.side, 8.4f);
                ClearNearestHouseLot(stallTarget, 9.5f);
                MoveAnywhere(shop.stall, stallTarget, road);
                MoveAnywhere(shop.npc, RoadsidePosition(shop.x, shop.side, 5.8f), stallTarget);
            }

            // Other stationary sellers/story contacts use the same roadside-lot rule.
            PlaceRoadsideNpc("NPC_Normy", 38f, 1, false);
            PlaceRoadsideNpc("NPC_Vagrant", 70f, -1, true);
            PlaceRoadsideNpc("NPC_BlackMarket", 91f, -1, true);
            PlaceRoadsideNpc("NPC_BossJ", 116f, 1, true);
            PlaceRoadsideNpc("NPC_BossC", 145f, -1, true);
            PlaceRoadsideNpc("NPC_GangRecruiter", 169f, -1, true);
            PlaceRoadsideNpc("NPC_Villager", 18f, -1, false);
            PlaceRoadsideNpc("NPC_Police", 43f, -1, false);
            PlaceRoadsideNpc("NPC_PoliceShops", 102f, -1, false);

            foreach (PoliceOfficer officer in UnityEngine.Object.FindObjectsByType<PoliceOfficer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Vector3 a = RoadsidePosition(18f, -1, 5.2f);
                Vector3 b = RoadsidePosition(132f, -1, 5.2f);
                officer.SetPatrol(Ground(a), Ground(b));
                SerializedObject so = new SerializedObject(officer);
                SerializedProperty patrolA = so.FindProperty("patrolA");
                SerializedProperty patrolB = so.FindProperty("patrolB");
                if (patrolA != null) patrolA.vector3Value = Ground(a);
                if (patrolB != null) patrolB.vector3Value = Ground(b);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void PlaceRoadsideNpc(string name, float x, int side, bool clearLot)
        {
            Vector3 road = RoadCentre(x);
            Vector3 target = RoadsidePosition(x, side, clearLot ? 7.6f : 5.4f);
            if (clearLot) ClearNearestHouseLot(target, 8.5f);
            MoveAnywhere(name, target, road);
        }

        private static Vector3 RoadCentre(float x)
        {
            // The approved phase-one Lalay spine is a mild bay-facing incline.
            // This compact frame keeps gameplay roles deterministic between builds.
            float z = -149f - (x + 48f) * 0.1765f;
            return Ground(new Vector3(x, 0f, z));
        }

        private static Vector3 RoadsidePosition(float x, int sideSign, float offset)
        {
            Vector3 road = RoadCentre(x);
            Vector3 forward = new Vector3(1f, 0f, -0.1765f).normalized;
            Vector3 side = new Vector3(-forward.z, 0f, forward.x);
            return Ground(road + side * sideSign * offset);
        }

        private static void MoveAnywhere(string name, Vector3 target, Vector3 lookAt)
        {
            Transform item = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(transform => transform.name == name);
            if (item == null) return;
            Vector3 forward = lookAt - target; forward.y = 0f;
            if (forward.sqrMagnitude > 0.01f) item.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            MoveRootByBoundsCentre(item.gameObject, target);
        }

        private static void ClearNearestHouseLot(Vector3 target, float maximumDistance)
        {
            Transform houses = s_world?.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transform => transform.name == "Lalay_Dense_House_Massing");
            if (houses == null || houses.childCount == 0) return;
            Transform nearest = houses.Cast<Transform>()
                .OrderBy(child => HorizontalDistance(child.position, target)).FirstOrDefault();
            if (nearest != null && HorizontalDistance(nearest.position, target) <= maximumDistance)
                UnityEngine.Object.DestroyImmediate(nearest.gameObject);
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b) =>
            Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        private static void UpdatePlantationRiskCentre()
        {
            FarmPlot firstPlot = UnityEngine.Object.FindObjectsByType<FarmPlot>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(plot => plot.name == "FarmPlot_00");
            if (firstPlot == null) return;

            Vector3 centre = firstPlot.transform.position;
            LandRiskController landRisk = UnityEngine.Object.FindFirstObjectByType<LandRiskController>(FindObjectsInactive.Include);
            if (landRisk != null)
            {
                SerializedObject so = new SerializedObject(landRisk);
                SerializedProperty property = so.FindProperty("plantationCenter");
                if (property != null) property.vector3Value = centre;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            PlantationTheftController theft = UnityEngine.Object.FindFirstObjectByType<PlantationTheftController>(FindObjectsInactive.Include);
            if (theft != null)
                theft.Configure(centre, UnityEngine.Object.FindObjectsByType<FarmPlot>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        }

        private static GameObject CloneApprovedWorld(Scene gameplayScene)
        {
            Scene source = EditorSceneManager.OpenScene(MapLabScene, OpenSceneMode.Additive);
            GameObject sourceRoot = source.GetRootGameObjects().FirstOrDefault(go => go.name == "MapLab_LalayHighland");
            if (sourceRoot == null) throw new InvalidOperationException("MINI-100: approved map-lab root missing.");
            SceneManager.SetActiveScene(gameplayScene);
            GameObject clone = UnityEngine.Object.Instantiate(sourceRoot);
            clone.name = "GrandBayPhase1_ApprovedWorld_VA005";
            SceneManager.MoveGameObjectToScene(clone, gameplayScene);
            EditorSceneManager.CloseScene(source, true);
            foreach (Transform item in clone.GetComponentsInChildren<Transform>(true).Where(t =>
                         t.name == "Labels" || t.name.StartsWith("Camera_", StringComparison.Ordinal) || t.name == "Directional Light" ||
                         t.name == "Farm_Plot_Starting_Active" || t.name == "Outline_Highland_First_Farm").ToArray())
                if (item != clone.transform) UnityEngine.Object.DestroyImmediate(item.gameObject);
            return clone;
        }

        private static Vector3 MapOldWorldPointToLalay(Vector3 old)
        {
            float t = Mathf.Clamp01(old.z / 320f);
            Vector3 a = new Vector3(-48f, 0f, -149f);
            Vector3 b = new Vector3(190f, 0f, -191f);
            Vector3 forward = (b - a).normalized;
            Vector3 side = new Vector3(-forward.z, 0f, forward.x);
            return Ground(Vector3.Lerp(a, b, t) + side * ((old.x - 130f) * 0.52f));
        }

        private static void MoveNamed(Scene scene, string name, Vector3 target)
        {
            GameObject root = FindRoot(scene, name);
            if (root == null) return;
            if (TryVisualBounds(root, out _)) MoveRootByBoundsCentre(root, target); else root.transform.position = Ground(target);
        }

        private static void MoveRootBoundsCentre(Scene scene, string name, Vector3 target)
        {
            GameObject root = FindRoot(scene, name);
            if (root != null) MoveRootByBoundsCentre(root, target);
        }

        private static void MoveRootByBoundsCentre(GameObject root, Vector3 target)
        {
            if (!TryVisualBounds(root, out Bounds bounds)) { root.transform.position = Ground(target); return; }
            Vector3 grounded = Ground(target);
            root.transform.position += new Vector3(grounded.x - bounds.center.x, grounded.y - bounds.min.y, grounded.z - bounds.center.z);
        }

        private static Vector3 Ground(Vector3 point)
        {
            point.y = 0f;
            if (s_world == null) return point;
            RaycastHit[] hits = Physics.RaycastAll(point + Vector3.up * 250f, Vector3.down, 500f);
            RaycastHit? best = hits.Where(hit => hit.collider != null && hit.collider.transform.IsChildOf(s_world.transform) &&
                                                (HasAncestor(hit.collider.transform, "Geography") || HasAncestor(hit.collider.transform, "Roads_OSM") ||
                                                 HasAncestor(hit.collider.transform, "Lalay_Sidewalks") || HasAncestor(hit.collider.transform, "Bridges")))
                                   .OrderByDescending(hit => hit.point.y).Cast<RaycastHit?>().FirstOrDefault();
            if (best.HasValue) point.y = best.Value.point.y;
            return point;
        }

        private static bool HasAncestor(Transform transform, string name)
        {
            while (transform != null) { if (transform.name == name) return true; transform = transform.parent; }
            return false;
        }

        private static bool TryVisualBounds(GameObject root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer)).ToArray();
            if (renderers.Length == 0) { bounds = default; return false; }
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return true;
        }

        private static GameObject FindRoot(Scene scene, string name) => scene.GetRootGameObjects().FirstOrDefault(go => go.name == name);
        private static void DestroyRoot(Scene scene, string name) { GameObject root = FindRoot(scene, name); if (root != null) UnityEngine.Object.DestroyImmediate(root); }
        private static GameObject DetachChild(GameObject root, string name)
        {
            if (root == null) return null;
            Transform child = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
            if (child == null) return null;
            child.SetParent(null, true);
            return child.gameObject;
        }
    }
}
