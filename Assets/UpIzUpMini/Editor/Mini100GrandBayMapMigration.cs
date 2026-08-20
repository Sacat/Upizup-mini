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
            foreach (string rootName in farmRoots) MoveRootBoundsCentre(gameplayScene, rootName, new Vector3(85.5f, 0f, -129.5f));

            // Map remaining visible gameplay roots from the former synthetic road onto
            // the approved Lalay spine. Components and save-facing object names stay intact.
            foreach (GameObject root in gameplayScene.GetRootGameObjects())
            {
                if (root == null || root == s_world || farmRoots.Contains(root.name) || root.name == "MooredBoat") continue;
                if (!TryVisualBounds(root, out Bounds bounds)) continue;
                if (root.GetComponentInChildren<Canvas>(true) != null) continue;
                Vector3 target = MapOldWorldPointToLalay(bounds.center);
                MoveRootByBoundsCentre(root, target);
            }

            MoveNamed(gameplayScene, "LalayHouse", new Vector3(48.5f, 0f, -154.5f));
            MoveNamed(gameplayScene, "LalayEstate", new Vector3(25f, 0f, -151f));
            MoveNamed(gameplayScene, "Sacat", new Vector3(48.5f, 0f, -159f));
            MoveNamed(gameplayScene, "Franki", new Vector3(50.2f, 0f, -160.2f));
            MoveNamed(gameplayScene, "NPC_RastaMentor", new Vector3(78f, 0f, -122f));
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
