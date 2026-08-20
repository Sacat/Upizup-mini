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
        private static readonly Vector2[] LalaySpine =
        {
            new Vector2(-63.29f, -144.47f), new Vector2(-0.82f, -153.07f),
            new Vector2(24.11f, -156.50f), new Vector2(45f, -159.94f),
            new Vector2(77.30f, -164.11f), new Vector2(110.37f, -168.64f),
            new Vector2(126.17f, -170.94f), new Vector2(136.67f, -174.64f),
            new Vector2(142.24f, -184.04f), new Vector2(150.86f, -190.48f),
            new Vector2(163.62f, -197.30f), new Vector2(176.30f, -198.82f),
            new Vector2(190.52f, -201.72f)
        };
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
                if (root == null || root == s_world || farmRoots.Contains(root.name) || root.name == "MooredBoat" || root.name == "BossC_SUV") continue;
                if (!TryVisualBounds(root, out Bounds bounds)) continue;
                if (root.GetComponentInChildren<Canvas>(true) != null) continue;
                if (root.GetComponent<TownNPCInteractable>() != null || root.name.StartsWith("Market_", StringComparison.Ordinal)) continue;
                Vector3 target = MapOldWorldPointToLalay(bounds.center);
                MoveRootByBoundsCentre(root, target);
            }

            PlaceLalayGameplay(gameplayScene);
            PlaceRoadsideBuilding(gameplayScene, "LalayHouse", 48f, 1, 15.5f, 8f);
            PlaceRoadsideBuilding(gameplayScene, "LalayEstate", 24f, -1, 17f, 9f);
            MoveVehicleAlongRoad("BossC_SUV", 145f, -1, 9.2f);
            PlaceGangBlocks();
            PlaceChurchAndJettyRoles(gameplayScene, oldBoat);
            MoveNamed(gameplayScene, "Sacat", new Vector3(48.5f, 0f, -159f));
            MoveNamed(gameplayScene, "Franki", new Vector3(50.2f, 0f, -160.2f));
            MoveNamed(gameplayScene, "NPC_RastaMentor", new Vector3(108f, 0f, -144f));
            if (oldBoat != null)
            {
                SceneManager.MoveGameObjectToScene(oldBoat, gameplayScene);
            }

            Vector3 highlandSpawn = Ground(new Vector3(125f, 0f, -115f)) + Vector3.up * 0.15f;
            foreach (SafehouseInteractable safehouse in UnityEngine.Object.FindObjectsByType<SafehouseInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                SerializedObject so = new SerializedObject(safehouse);
                SerializedProperty property = so.FindProperty("spawnPoint");
                Vector3 safehousePoint = Ground(safehouse.transform.position) + Vector3.up * 0.15f;
                if (property != null) property.vector3Value = safehousePoint;
                so.ApplyModifiedPropertiesWithoutUndo();
                if (safehouse.transform.root.name == "FarmSafehouse") highlandSpawn = safehousePoint;
            }
            CharacterSwitchManager switcher = UnityEngine.Object.FindFirstObjectByType<CharacterSwitchManager>(FindObjectsInactive.Include);
            if (switcher != null)
            {
                SerializedObject so = new SerializedObject(switcher);
                SerializedProperty property = so.FindProperty("safehouseSpawn");
                if (property != null) property.vector3Value = highlandSpawn;
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
                Vector3 stallTarget = RoadsidePosition(shop.x, shop.side, 14.0f);
                ClearHouseLots(stallTarget, 7.5f);
                MoveAnywhere(shop.stall, stallTarget, road);
                MoveAnywhere(shop.npc, RoadsidePosition(shop.x, shop.side, 10.2f), stallTarget);
            }

            // Other stationary sellers/story contacts use the same roadside-lot rule.
            PlaceWalkingLalayNpc("NPC_Normy", 38f, 1);
            PlaceRoadsideNpc("NPC_Vagrant", 70f, -1, true);
            PlaceRoadsideNpc("NPC_BlackMarket", 91f, -1, true);
            PlaceWalkingLalayNpc("NPC_BossJ", 116f, 1);
            PlaceRoadsideNpc("NPC_BossC", 145f, -1, true);
            PlaceRoadsideNpc("NPC_GangRecruiter", 169f, -1, true);
            PlaceRoadsideNpc("NPC_Villager", 18f, -1, false);
            PlaceRoadsideNpc("NPC_Police", 43f, -1, false);
            PlaceRoadsideNpc("NPC_PoliceShops", 102f, -1, false);

            foreach (PoliceOfficer officer in UnityEngine.Object.FindObjectsByType<PoliceOfficer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Vector3 a = RoadsidePosition(18f, -1, 7.0f);
                Vector3 b = RoadsidePosition(132f, -1, 7.0f);
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
            Vector3 target = RoadsidePosition(x, side, clearLot ? 11.2f : 7.0f);
            if (clearLot) ClearHouseLots(target, 5.5f);
            MoveAnywhere(name, target, road);
        }

        private static void PlaceWalkingLalayNpc(string name, float x, int side)
        {
            Vector3 centre = RoadsidePosition(x, side, 7.0f);
            MoveAnywhere(name, centre, RoadCentre(x));
            Transform npc = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(transform => transform.name == name);
            PatrolNPC patrol = npc != null ? npc.GetComponent<PatrolNPC>() : null;
            if (patrol == null) return;
            patrol.SetWaypoints(new[]
            {
                RoadsidePosition(x - 9f, side, 7.0f),
                RoadsidePosition(x + 9f, side, 7.0f)
            });
        }

        private static void PlaceGangBlocks()
        {
            Vector3 dogLifeCentre = RoadsidePosition(-32f, -1, 11.5f);
            RivalGangSpawner spawner = UnityEngine.Object.FindFirstObjectByType<RivalGangSpawner>(FindObjectsInactive.Include);
            spawner?.SetBlockCentre(dogLifeCentre);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * 0.5f;
                Vector3 position = Ground(dogLifeCentre + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 3.2f);
                MoveAnywhere($"NPC_DogLife_{i}", position, RoadCentre(-32f));
            }

            Vector3 playerBlock = RoadsidePosition(154f, -1, 12.0f);
            PlaceRoadsideNpc("NPC_GangRecruiter", 154f, -1, true);
            string[] names = { "Zoomy", "Deluxe", "Draco", "Rio" };
            for (int i = 0; i < names.Length; i++)
            {
                Vector3 position = Ground(playerBlock + new Vector3((i - 1.5f) * 1.8f, 0f, -2.2f));
                Transform member = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(transform => transform.name == $"NotAhWord_{names[i]}");
                if (member == null) continue;
                MoveRootByBoundsCentre(member.gameObject, position);
                GangMemberController controller = member.GetComponent<GangMemberController>();
                controller?.SetHomePosition(playerBlock);
                controller?.SetGuardPosition(HighlandFarmCentre);
            }
        }

        private static void PlaceChurchAndJettyRoles(Scene gameplayScene, GameObject boat)
        {
            Transform church = s_world?.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transform => transform.name == "GrandBay_Catholic_Church_Graybox");
            Vector3 priestPosition = church != null
                ? Ground(church.TransformPoint(new Vector3(4f, 0f, -12f)))
                : Ground(new Vector3(197f, 0f, -181f));
            MoveNamed(gameplayScene, "NPC_Brakes", priestPosition);
            MoveNamed(gameplayScene, "NPC_BoatMan", Ground(new Vector3(222f, 0f, -181f)));
            if (boat != null) MoveRootByBoundsCentre(boat, Ground(new Vector3(250f, 0f, -181f)));
        }

        private static Vector3 RoadCentre(float x)
        {
            if (x <= LalaySpine[0].x) return Ground(new Vector3(x, 0f, LalaySpine[0].y));
            for (int i = 1; i < LalaySpine.Length; i++)
            {
                if (x > LalaySpine[i].x) continue;
                float t = Mathf.InverseLerp(LalaySpine[i - 1].x, LalaySpine[i].x, x);
                return Ground(new Vector3(x, 0f, Mathf.Lerp(LalaySpine[i - 1].y, LalaySpine[i].y, t)));
            }
            return Ground(new Vector3(x, 0f, LalaySpine[LalaySpine.Length - 1].y));
        }

        private static Vector3 RoadsidePosition(float x, int sideSign, float offset)
        {
            Vector3 road = RoadCentre(x);
            Vector3 before = RoadCentre(x - 1f);
            Vector3 after = RoadCentre(x + 1f);
            Vector3 forward = after - before;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 side = new Vector3(-forward.z, 0f, forward.x);
            return Ground(road + side * sideSign * offset);
        }

        private static void PlaceRoadsideBuilding(Scene scene, string name, float x, int side, float offset, float clearRadius)
        {
            Vector3 target = RoadsidePosition(x, side, offset);
            ClearHouseLots(target, clearRadius);
            MoveNamed(scene, name, target);
        }

        private static void MoveVehicleAlongRoad(string name, float x, int side, float offset)
        {
            Transform vehicle = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(transform => transform.name == name);
            if (vehicle == null) return;
            Vector3 target = RoadsidePosition(x, side, offset);
            // On a sharp bend the sideways sidewalk offset also changes X.
            // Use the visible vehicle centre's target X, not the road query X,
            // so its long axis follows the road beside the actual parking lot.
            Vector3 before = RoadCentre(target.x - 2f);
            Vector3 after = RoadCentre(target.x + 2f);
            Vector3 forward = after - before; forward.y = 0f;
            if (forward.sqrMagnitude > 0.01f) vehicle.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            // Rotate first: changing the long vehicle's orientation changes
            // its bounds centre, so positioning before rotation could leave
            // the visible Rover skewed relative to the intended sidewalk lot.
            MoveRootByBoundsCentre(vehicle.gameObject, target);
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

        private static void ClearHouseLots(Vector3 target, float maximumDistance)
        {
            Transform houses = s_world?.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transform => transform.name == "Lalay_Dense_House_Massing");
            if (houses == null || houses.childCount == 0) return;
            // A shop or story contact replaces one residential lot. Removing every
            // house in a radius produced large unnatural holes along dense Lalay.
            Transform nearest = houses.Cast<Transform>()
                .Where(child => HorizontalDistance(child.position, target) <= maximumDistance)
                .OrderBy(child => HorizontalDistance(child.position, target))
                .FirstOrDefault();
            if (nearest != null) UnityEngine.Object.DestroyImmediate(nearest.gameObject);
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
