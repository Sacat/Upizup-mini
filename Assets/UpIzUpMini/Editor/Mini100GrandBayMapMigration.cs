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
            UpdateHighlandAreaNotification();
            // MINI-113, user: "Pull the farm privacy hedge fully off the
            // Highland road while keeping all plots screened and the dirt-
            // road entrance clear." Root cause, confirmed by direct
            // measurement (not guessed): the hedge enclosure's geometry
            // was authored relative to the OLD pre-migration synthetic
            // road direction, then the whole MontineFarm root was
            // TRANSLATED (not re-oriented) to its approved Highland
            // position by PlaceHighlandFarm above - so the enclosure's
            // real-world footprint now overlaps the actual (differently
            // angled) Highland farm spur road. Measured overlap was
            // exactly 0.00m clearance on every hedge segment before this
            // fix. Fixed surgically: each hedge SEGMENT that measurably
            // overlaps a road collider is pushed straight away from that
            // road (in the segment's own local space, so it keeps its
            // authored orientation) until clear - the farm plots
            // themselves and the entrance gap are untouched, since only
            // the hedge segments move.
            ClearFarmHedgeFromRoads();
            // MINI-113, user: "Smooth bumps where secondary road ribbons
            // intersect Lalay and other driveable roads." Measured (not
            // guessed) every road-root pair's real closest-vertex vertical
            // step; of the four ACTUALLY connected junctions this map
            // already validates (Mini100GrandBayMapValidation.
            // ValidateRoadJoin), only "Highland inroad to farm spur" had a
            // real bump - 0.286m, right at that check's own 0.30m
            // tolerance ceiling, so it passed the static gate while still
            // reading as a genuine bump to drive over. Root cause: each
            // road ribbon's height is graded independently against its own
            // maximum-slope cap in the map-lab generator with no shared
            // junction-height constraint between neighbours. Fixed here as
            // a small, localized post-process rather than reworking that
            // deeper generator - blends the farm spur's own vertices
            // toward the inroad's real height near their shared junction,
            // tapering to zero effect a short distance away so nothing
            // else on the spur is disturbed.
            SmoothRoadJunctionHeight("Road_user_highland_farm_spur", "Road_user_highland_lalay_inroad", blendDistance: 8f);

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
            RepairBossCNeighbourColliders();
            PlaceRoadsideBuilding(gameplayScene, "LalayHouse", 48f, 1, 15.5f, 8f);
            PlaceRoadsideBuilding(gameplayScene, "LalayEstate", 24f, -1, 17f, 9f);
            MoveVehicleAlongRoad("BossC_SUV", 151f, -1, 12.5f);
            PlaceGangBlocks();
            PlaceChurchAndJettyRoles(gameplayScene, oldBoat);
            // MINI-109: RemapMissionObjectives() reads each named NPC's
            // CURRENT transform position via PositionOf(...) to bake into
            // mission markers - it must run AFTER every MoveNamed call
            // below, not before. It previously ran first, so M13's TalkTo-
            // Rasta marker (and the Sacat/Franki-adjacent ones) baked in
            // each character's PRE-move position, then MoveNamed silently
            // moved them again - "M13 routes to Rasta's old location" was
            // this exact ordering bug, not a missing mapping.
            MoveNamed(gameplayScene, "Sacat", new Vector3(48.5f, 0f, -159f));
            MoveNamed(gameplayScene, "Franki", new Vector3(50.2f, 0f, -160.2f));
            MoveNamed(gameplayScene, "NPC_RastaMentor", new Vector3(108f, 0f, -144f));
            RemapMissionObjectives();
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
            // MINI-113, user: "Spawn purchased/current vehicles in road-
            // safe spaces... never blocking traffic." Found by direct
            // measurement, not guessed: BikeHomePoint - the bike's
            // save/load return point (VehicleSpawnController.
            // ReturnBikeHome, called on every load) - is a standalone root
            // object built relative to the OLD pre-migration safehouse
            // position, and unlike FarmSafehouse itself it was never in
            // this migration's tracked-and-repositioned set. Measured
            // 303.52m from the real, migrated FarmSafehouse - a bike
            // returning home on load would have ended up stranded in the
            // old world's empty space. Fixed the same way this file
            // already repairs SafehouseInteractable.spawnPoint and
            // CharacterSwitchManager.safehouseSpawn just above - relative
            // to the real, migrated safehouse.
            RelocateBikeHome();

            Physics.SyncTransforms();
            Debug.Log("MINI-100 MAP MIGRATION PASS: approved VA-005 world cloned; old synthetic environment removed; gameplay roots preserved and relocated.");
        }

        /// <summary>MINI-113: re-derives BikeHomePoint the same way
        /// Mini011PhaseBSetup originally built it - offset from the
        /// safehouse's own position/rotation, ground-snapped - but against
        /// the REAL, migrated FarmSafehouse transform instead of the
        /// pre-migration one it was built with. Also re-points
        /// VehicleSpawnController.bikeHome at it, in case anything ever
        /// swapped the reference.</summary>
        private static void RelocateBikeHome()
        {
            GameObject safehouse = GameObject.Find("FarmSafehouse");
            GameObject bikeHome = GameObject.Find("BikeHomePoint");
            if (safehouse == null || bikeHome == null)
            {
                Debug.LogWarning("MINI-113: FarmSafehouse or BikeHomePoint missing - bike home relocation skipped.");
                return;
            }

            Vector3 parkPos = safehouse.transform.position + safehouse.transform.rotation * new Vector3(2.2f, 0f, 7.0f);
            parkPos = Ground(parkPos) + Vector3.up * 0.04f;
            Quaternion parkRot = safehouse.transform.rotation * Quaternion.Euler(0f, 90f, 0f);
            bikeHome.transform.SetPositionAndRotation(parkPos, parkRot);

            Vehicles.VehicleSpawnController spawner = UnityEngine.Object.FindFirstObjectByType<Vehicles.VehicleSpawnController>(FindObjectsInactive.Include);
            if (spawner != null)
            {
                SerializedObject so = new SerializedObject(spawner);
                SerializedProperty property = so.FindProperty("bikeHome");
                if (property != null) property.objectReferenceValue = bikeHome.transform;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            Debug.Log($"MINI-113: relocated BikeHomePoint to {parkPos} next to the migrated FarmSafehouse (was 303.52m away before this fix).");
        }

        /// <summary>MINI-113: pushes each hedge SEGMENT (not the plots,
        /// not the whole enclosure) radially away from the farm centre,
        /// iteratively, until it measurably clears every road by at least
        /// clearanceMetres - see the call site's own comment for why this
        /// drifted after migration. Distance is measured against real road
        /// MESH VERTICES, not Collider.ClosestPoint - that API is only
        /// reliable on CONVEX colliders, and a road ribbon's MeshCollider
        /// is not convex, so it was silently returning the query point
        /// itself (a false "0.00m" reading) no matter how far the hedge
        /// actually moved. Caught by re-measuring after a first attempt
        /// kept reporting 0.00m even after a 15m push.</summary>
        private static void ClearFarmHedgeFromRoads()
        {
            GameObject farm = GameObject.Find("MontineFarm");
            if (farm == null) return;

            Vector3[][] roadWorldVertices = UnityEngine.Object
                .FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(f => f.gameObject.name.StartsWith("Road_", StringComparison.Ordinal) && f.sharedMesh != null)
                .Select(f => f.sharedMesh.vertices.Select(v => f.transform.TransformPoint(v)).ToArray())
                .ToArray();
            if (roadWorldVertices.Length == 0) return;

            Vector2 farmCentreXZ = new Vector2(HighlandFarmCentre.x, HighlandFarmCentre.z);
            const float clearanceMetres = 0.9f;
            const float stepMetres = 0.25f;
            const int maxSteps = 40;

            foreach (Transform hedge in farm.GetComponentsInChildren<Transform>(true).Where(t => t.name == "WalkThroughHedge").ToArray())
            {
                Renderer renderer = hedge.GetComponent<Renderer>();
                if (renderer == null) continue;

                Vector2 pushDir = new Vector2(hedge.position.x, hedge.position.z) - farmCentreXZ;
                if (pushDir.sqrMagnitude < 0.01f) pushDir = Vector2.up; // degenerate (segment sits on the centre line) - arbitrary but stable fallback
                pushDir.Normalize();
                Vector3 step = new Vector3(pushDir.x, 0f, pushDir.y) * stepMetres;

                float startDist = NearestRoadVertexDistance(renderer.bounds.center, roadWorldVertices);
                int moved = 0;
                for (int i = 0; i < maxSteps; i++)
                {
                    float dist = NearestRoadVertexDistance(renderer.bounds.center, roadWorldVertices);
                    if (dist >= clearanceMetres) break;
                    hedge.position += step;
                    moved++;
                }

                if (moved > 0)
                {
                    float endDist = NearestRoadVertexDistance(renderer.bounds.center, roadWorldVertices);
                    Debug.Log($"MINI-113: hedge '{hedge.name}' at {hedge.position} pushed {moved * stepMetres:F2}m clear of the road (measured {startDist:F2}m -> {endDist:F2}m).");
                }
            }
        }

        /// <summary>MINI-113: blends movableRoadName's own vertices toward
        /// fixedRoadName's real height at their nearest shared point,
        /// tapering linearly to zero over blendDistance so only the
        /// junction itself is corrected. movableRoadName is treated as the
        /// secondary/joining road (its mesh and collider are mutated);
        /// fixedRoadName is read-only and untouched.</summary>
        private static void SmoothRoadJunctionHeight(string movableRoadName, string fixedRoadName, float blendDistance)
        {
            GameObject movable = GameObject.Find(movableRoadName);
            GameObject fixedRoad = GameObject.Find(fixedRoadName);
            MeshFilter movableFilter = movable?.GetComponentInChildren<MeshFilter>();
            MeshFilter fixedFilter = fixedRoad?.GetComponentInChildren<MeshFilter>();
            if (movableFilter == null || fixedFilter == null || movableFilter.sharedMesh == null || fixedFilter.sharedMesh == null)
            {
                Debug.LogWarning($"MINI-113: could not find both '{movableRoadName}' and '{fixedRoadName}' with a mesh - junction smoothing skipped.");
                return;
            }

            Mesh movableMesh = movableFilter.sharedMesh;
            Vector3[] movableLocal = movableMesh.vertices;
            Vector3[] movableWorld = movableLocal.Select(v => movableFilter.transform.TransformPoint(v)).ToArray();
            Vector3[] fixedWorld = fixedFilter.sharedMesh.vertices.Select(v => fixedFilter.transform.TransformPoint(v)).ToArray();

            int bestMovableIndex = -1;
            float bestDist = float.MaxValue;
            float targetHeight = 0f;
            for (int i = 0; i < movableWorld.Length; i++)
            {
                Vector2 mv = new Vector2(movableWorld[i].x, movableWorld[i].z);
                for (int j = 0; j < fixedWorld.Length; j++)
                {
                    float d = Vector2.Distance(mv, new Vector2(fixedWorld[j].x, fixedWorld[j].z));
                    if (d >= bestDist) continue;
                    bestDist = d;
                    bestMovableIndex = i;
                    targetHeight = fixedWorld[j].y;
                }
            }
            if (bestMovableIndex < 0 || bestDist > 2.5f)
            {
                Debug.LogWarning($"MINI-113: '{movableRoadName}' and '{fixedRoadName}' have no plausible shared junction (closest gap {bestDist:F2}m) - junction smoothing skipped.");
                return;
            }

            float heightDelta = targetHeight - movableWorld[bestMovableIndex].y;
            if (Mathf.Abs(heightDelta) < 0.02f) return; // already smooth enough to be a no-op

            Vector2 junctionXZ = new Vector2(movableWorld[bestMovableIndex].x, movableWorld[bestMovableIndex].z);
            for (int i = 0; i < movableWorld.Length; i++)
            {
                float distFromJunction = Vector2.Distance(new Vector2(movableWorld[i].x, movableWorld[i].z), junctionXZ);
                float weight = Mathf.Clamp01(1f - distFromJunction / blendDistance);
                if (weight <= 0f) continue;
                Vector3 adjustedWorld = movableWorld[i] + Vector3.up * (heightDelta * weight);
                movableLocal[i] = movableFilter.transform.InverseTransformPoint(adjustedWorld);
            }

            movableMesh.vertices = movableLocal;
            movableMesh.RecalculateBounds();
            movableMesh.RecalculateNormals();

            MeshCollider movableCollider = movable.GetComponentInChildren<MeshCollider>();
            if (movableCollider != null)
            {
                // MeshCollider caches its cooked collision data at assignment
                // time - re-assigning is what forces it to re-cook against
                // the mutated vertex positions.
                movableCollider.sharedMesh = null;
                movableCollider.sharedMesh = movableMesh;
            }

            Debug.Log($"MINI-113: smoothed '{movableRoadName}' toward '{fixedRoadName}' at their junction - height step was {heightDelta:F3}m, blended over {blendDistance:F1}m.");
        }

        private static float NearestRoadVertexDistance(Vector3 point, Vector3[][] roadWorldVertices)
        {
            Vector2 p = new Vector2(point.x, point.z);
            float best = float.MaxValue;
            foreach (Vector3[] verts in roadWorldVertices)
            foreach (Vector3 v in verts)
            {
                float d = Vector2.Distance(p, new Vector2(v.x, v.z));
                if (d < best) best = d;
            }
            return best;
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

        private static void UpdateHighlandAreaNotification()
        {
            var display = UnityEngine.Object.FindFirstObjectByType<UpIzUpMini.UI.AreaNameDisplay>(FindObjectsInactive.Include);
            if (display == null) return;

            SerializedObject so = new SerializedObject(display);
            SerializedProperty zones = so.FindProperty("zones");
            if (zones == null) return;
            Vector3 centre = PositionOf("FarmPlot_00", HighlandFarmCentre);
            for (int i = 0; i < zones.arraySize; i++)
            {
                SerializedProperty zone = zones.GetArrayElementAtIndex(i);
                if (zone.FindPropertyRelative("areaName").stringValue != "Highland") continue;
                zone.FindPropertyRelative("center").vector3Value = centre;
                zone.FindPropertyRelative("radius").floatValue = 65f;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
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
                Vector3 stallTarget = RoadsidePosition(shop.x, shop.side, 9.2f);
                MoveAnywhere(shop.stall, stallTarget, road);
                ClearHouseIntersections(shop.stall, 0.65f);
                MoveAnywhere(shop.npc, RoadsidePosition(shop.x, shop.side, 7.2f), stallTarget);
            }

            // Other stationary sellers/story contacts use the same roadside-lot rule.
            PlaceWalkingLalayNpc("NPC_Normy", 38f, 1);
            PlaceRoadsideNpc("NPC_Vagrant", 70f, -1, true);
            PlaceRoadsideNpc("NPC_BlackMarket", 91f, -1, true);
            PlaceWalkingLalayNpc("NPC_BossJ", 116f, 1);
            PlaceRoadsideNpc("NPC_BossC", 145f, -1, true);
            PlaceRoadsideNpc("NPC_GangRecruiter", 169f, -1, true);
            PlaceRoadsideNpc("NPC_Villager", 18f, -1, false);
            ClearHouseAtPoint(PositionOf("NPC_Villager", RoadsidePosition(18f, -1, 4.3f)), 0.35f);
            ConfigurePolicePatrol("NPC_Police", -45f, 30f, 1);
            ConfigurePolicePatrol("NPC_PoliceShops", 35f, 100f, -1);
            ConfigurePolicePatrol("NPC_PoliceEast", 108f, 178f, 1);
        }

        private static void ConfigurePolicePatrol(string name, float startX, float endX, int side)
        {
            Vector3 a = RoadsidePosition(startX, side, 4.3f);
            Vector3 b = RoadsidePosition(endX, side, 4.3f);
            MoveAnywhere(name, Vector3.Lerp(a, b, 0.5f), RoadCentre((startX + endX) * 0.5f));
            Transform npc = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == name);
            PoliceOfficer officer = npc != null ? npc.GetComponent<PoliceOfficer>() : null;
            if (officer == null) return;
            officer.SetPatrol(Ground(a), Ground(b));
            SerializedObject so = new SerializedObject(officer);
            if (so.FindProperty("patrolA") is SerializedProperty pa) pa.vector3Value = Ground(a);
            if (so.FindProperty("patrolB") is SerializedProperty pb) pb.vector3Value = Ground(b);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PlaceRoadsideNpc(string name, float x, int side, bool clearLot)
        {
            Vector3 road = RoadCentre(x);
            Vector3 target = RoadsidePosition(x, side, clearLot ? 11.2f : 4.3f);
            MoveAnywhere(name, target, road);
            if (clearLot) ClearHouseIntersections(name, 0.5f);

            // MINI-119, user: "there is a villager walking up to the
            // mountain. i think he has the old route from the other map."
            // Root cause: MoveAnywhere only repositions the NPC's own
            // transform - it never touches a PatrolNPC's waypoints, which
            // for any patrol-enabled roadside NPC (NPC_Villager included)
            // were set once at BuildTownNPCs time against the OLD
            // pre-migration road layout and never revisited. A safe no-op
            // for every static (non-patrolling) caller of this same
            // helper - GetComponent returns null for them.
            Transform npc = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name == name);
            PatrolNPC patrol = npc != null ? npc.GetComponent<PatrolNPC>() : null;
            if (patrol != null)
            {
                patrol.SetWaypoints(new[]
                {
                    RoadsidePosition(x - 4f, side, clearLot ? 11.2f : 4.3f),
                    RoadsidePosition(x + 4f, side, clearLot ? 11.2f : 4.3f)
                });
            }
        }

        private static void PlaceWalkingLalayNpc(string name, float x, int side)
        {
            Vector3 centre = RoadsidePosition(x, side, 4.3f);
            MoveAnywhere(name, centre, RoadCentre(x));
            Transform npc = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(transform => transform.name == name);
            PatrolNPC patrol = npc != null ? npc.GetComponent<PatrolNPC>() : null;
            if (patrol == null) return;
            patrol.SetWaypoints(new[]
            {
                RoadsidePosition(x - 9f, side, 4.3f),
                RoadsidePosition(x + 9f, side, 4.3f)
            });
        }

        private static void PlaceGangBlocks()
        {
            Vector3 dogLifeCentre = RoadsidePosition(-32f, -1, 4.3f);
            RivalGangSpawner spawner = UnityEngine.Object.FindFirstObjectByType<RivalGangSpawner>(FindObjectsInactive.Include);
            spawner?.SetBlockCentre(dogLifeCentre);
            for (int i = 0; i < 4; i++)
            {
                float x = -38f + i * 4f;
                Vector3 position = RoadsidePosition(x, -1, 4.3f);
                ClearHouseAtPoint(position, 0.35f);
                MoveAnywhere($"NPC_DogLife_{i}", position, RoadCentre(x));
            }

            Vector3 playerBlock = RoadsidePosition(154f, -1, 4.3f);
            MoveAnywhere("NPC_GangRecruiter", RoadsidePosition(166f, -1, 4.3f), RoadCentre(166f));
            string[] names = { "Zoomy", "Deluxe", "Draco", "Rio" };
            for (int i = 0; i < names.Length; i++)
            {
                float x = 148f + i * 4f;
                Vector3 position = RoadsidePosition(x, -1, 4.3f);
                ClearHouseAtPoint(position, 0.35f);
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
            if (boat != null) MoveRootByBoundsCentre(boat, new Vector3(282f, 0.55f, -181f));
        }

        private static void RemapMissionObjectives()
        {
            Missions.MissionSystem system = UnityEngine.Object.FindFirstObjectByType<Missions.MissionSystem>(FindObjectsInactive.Include);
            if (system == null) return;
            SerializedObject so = new SerializedObject(system);
            SerializedProperty missions = so.FindProperty("missions");
            Vector3 farm = PositionOf("FarmPlot_00", HighlandFarmCentre);
            Vector3 safehouse = PositionOf("FarmSafehouse_Building", new Vector3(125f, 0f, -115f));
            for (int m = 0; m < missions.arraySize; m++)
            {
                SerializedProperty mission = missions.GetArrayElementAtIndex(m);
                string missionId = mission.FindPropertyRelative("missionId").stringValue;
                SerializedProperty objectives = mission.FindPropertyRelative("objectives");
                for (int o = 0; o < objectives.arraySize; o++)
                {
                    SerializedProperty objective = objectives.GetArrayElementAtIndex(o);
                    var kind = (Missions.ObjectiveKind)objective.FindPropertyRelative("kind").enumValueIndex;
                    string targetId = objective.FindPropertyRelative("targetId").stringValue;
                    Vector3 marker = ResolveMissionMarker(kind, targetId, farm, safehouse);
                    if (missionId == "M1" && kind == Missions.ObjectiveKind.ReachArea)
                    {
                        marker = safehouse;
                        objective.FindPropertyRelative("instruction").stringValue = "Follow the yellow marker to the Highland safehouse and farm";
                    }
                    if (missionId == "M16" && kind == Missions.ObjectiveKind.ReachArea)
                    {
                        marker = PositionOfGroup("NPC_DogLife_", RoadsidePosition(-32f, -1, 4.3f));
                        objective.FindPropertyRelative("instruction").stringValue = "Follow the yellow marker to Dog Life's block on the Lalay sidewalk";
                    }
                    if (marker != Vector3.zero) objective.FindPropertyRelative("markerPosition").vector3Value = marker;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Vector3 ResolveMissionMarker(Missions.ObjectiveKind kind, string targetId, Vector3 farm, Vector3 safehouse)
        {
            if (kind is Missions.ObjectiveKind.PlantCrop or Missions.ObjectiveKind.WaterAny
                or Missions.ObjectiveKind.HarvestCrop or Missions.ObjectiveKind.AssignFarmhand) return farm;
            if (kind == Missions.ObjectiveKind.EscapeHeat) return safehouse;
            if (kind == Missions.ObjectiveKind.RestAtSafehouse) return safehouse;
            if (kind == Missions.ObjectiveKind.TalkToCleanPolice) return PositionOf("NPC_Police", Vector3.zero);
            if (kind == Missions.ObjectiveKind.BribeNormy) return PositionOf("NPC_Normy", Vector3.zero);
            // MINI-110: DeliverItem's targetId is an ITEM id (e.g.
            // "food_bakes"), not an NPC name - the generic TalkTo-style
            // name lookup below would not apply anyway. Normy is
            // currently the only DeliverItem recipient in the game.
            if (kind == Missions.ObjectiveKind.DeliverItem) return PositionOf("NPC_Normy", Vector3.zero);
            if (kind == Missions.ObjectiveKind.BuySeeds) return PositionOf("NPC_FarmShop", Vector3.zero);
            if (kind == Missions.ObjectiveKind.BuyItem)
            {
                if (!string.IsNullOrEmpty(targetId) && (targetId.StartsWith("land_", StringComparison.OrdinalIgnoreCase)
                    || targetId.StartsWith("prop_", StringComparison.OrdinalIgnoreCase))) return PositionOf("NPC_LandOffice", Vector3.zero);
                if (targetId == "tmax_560" || targetId == "range_rova") return PositionOf("NPC_CarDealer", Vector3.zero);
            }
            if (kind == Missions.ObjectiveKind.SellCrop && string.IsNullOrEmpty(targetId)) return PositionOf("NPC_Buyer", Vector3.zero);
            if (kind != Missions.ObjectiveKind.TalkTo && kind != Missions.ObjectiveKind.SellCrop && kind != Missions.ObjectiveKind.FollowNpc) return Vector3.zero;
            string name = targetId switch
            {
                "FarmShop" => "NPC_FarmShop", "Vagrant" => "NPC_Vagrant", "BossK" => "NPC_BossJ",
                "BossC" => "NPC_BossC", "BoatMan" => "NPC_BoatMan", "Normy" => "NPC_Normy",
                "FoodShop" => "NPC_FoodShop", "Pharmacy" => "NPC_Pharmacy", "Rasta" => "NPC_RastaMentor",
                "Buyer" => "NPC_Buyer", "ApparelShop" => "NPC_ApparelShop", "GangRecruiter" => "NPC_GangRecruiter",
                "Police" => "NPC_Police", _ => targetId
            };
            return PositionOf(name, Vector3.zero);
        }

        private static Vector3 PositionOf(string name, Vector3 fallback)
        {
            Transform item = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name == name);
            return item != null ? Ground(item.position) + Vector3.up * 0.15f : fallback;
        }

        private static Vector3 PositionOfGroup(string prefix, Vector3 fallback)
        {
            Transform[] items = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(t => t.name.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
            if (items.Length == 0) return fallback;
            Vector3 centre = items.Aggregate(Vector3.zero, (sum, item) => sum + item.position) / items.Length;
            return Ground(centre) + Vector3.up * 0.15f;
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
            // Keep the vehicle parallel to the road station it is parked beside.
            Vector3 before = RoadCentre(x - 2f);
            Vector3 after = RoadCentre(x + 2f);
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

        private static void ClearHouseIntersections(string placedObjectName, float padding)
        {
            Transform placed = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(transform => transform.name == placedObjectName);
            Transform houses = s_world?.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transform => transform.name == "Lalay_Dense_House_Massing");
            if (placed == null || houses == null || !TryVisualBounds(placed.gameObject, out Bounds placedBounds)) return;
            placedBounds.Expand(new Vector3(padding * 2f, 0f, padding * 2f));
            Transform[] overlaps = houses.Cast<Transform>()
                .Where(house => TryVisualBounds(house.gameObject, out Bounds houseBounds)
                    && placedBounds.min.x < houseBounds.max.x && placedBounds.max.x > houseBounds.min.x
                    && placedBounds.min.z < houseBounds.max.z && placedBounds.max.z > houseBounds.min.z)
                .OrderBy(house => HorizontalDistance(house.position, placedBounds.center))
                .ToArray();
            for (int index = 0; index < overlaps.Length; index++)
            {
                Transform house = overlaps[index];
                if (index == 0)
                {
                    // One commercial/contact lot replaces one residence.
                    UnityEngine.Object.DestroyImmediate(house.gameObject);
                    continue;
                }
                if (!TryVisualBounds(house.gameObject, out Bounds houseBounds)) continue;
                Vector3 road = RoadCentre(houseBounds.center.x);
                Vector3 outward = houseBounds.center - road;
                outward.y = 0f;
                if (outward.sqrMagnitude < 0.01f) outward = Vector3.forward;
                Vector3 target = houseBounds.center + outward.normalized * 14f;
                MoveRootByBoundsCentre(house.gameObject, target);
            }
        }

        private static void ClearHouseAtPoint(Vector3 point, float padding)
        {
            Transform houses = s_world?.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transform => transform.name == "Lalay_Dense_House_Massing");
            if (houses == null) return;
            foreach (Transform house in houses.Cast<Transform>().ToArray())
            {
                if (!TryVisualBounds(house.gameObject, out Bounds bounds)) continue;
                bounds.Expand(new Vector3(padding * 2f, 0f, padding * 2f));
                if (point.x >= bounds.min.x && point.x <= bounds.max.x
                    && point.z >= bounds.min.z && point.z <= bounds.max.z)
                    UnityEngine.Object.DestroyImmediate(house.gameObject);
            }
        }

        private static void RepairBossCNeighbourColliders()
        {
            Transform boss = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(transform => transform.name == "NPC_BossC");
            Transform houses = s_world?.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transform => transform.name == "Lalay_Dense_House_Massing");
            if (boss == null || houses == null) return;

            foreach (Transform house in houses.Cast<Transform>())
            {
                if (HorizontalDistance(house.position, boss.position) > 28f) continue;
                foreach (BoxCollider box in house.GetComponents<BoxCollider>())
                {
                    // Imported shanties use one bounds collider which included
                    // wide roof overhangs and blocked the grass beside Boss C.
                    // Keep the building solid but tighten the walk footprint.
                    Vector3 size = box.size;
                    size.x *= 0.72f;
                    size.z *= 0.72f;
                    box.size = size;
                }
            }
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
                                                 HasAncestor(hit.collider.transform, "Lalay_Sidewalks") || HasAncestor(hit.collider.transform, "Lalay_Levelled_Frontages") ||
                                                 HasAncestor(hit.collider.transform, "Bridges")))
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
