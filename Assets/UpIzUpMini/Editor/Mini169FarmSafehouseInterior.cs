using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-169: "fix up the first safehouse into a walkable
    /// house". Survey (Mini169SurveySafehouses) found FarmSafehouse_Building
    /// is ALREADY a real 3-walled room with real BoxColliders and a bed
    /// (Floor/Roof/BackWall/SideWallL/SideWallR, all primitive Cubes on the
    /// "SafehouseWall"/"SafehouseFrame"/"SafehouseRoof"/"SafehouseBed"
    /// materials) - it was already structurally walkable, just open-fronted
    /// with no door, reading as a lean-to shed rather than a house. This
    /// tool adds a real front wall with a walkable doorway, matching the
    /// EXACT existing primitive/material convention rather than introducing
    /// a new construction technique. SafehouseInteractable's menu is purely
    /// distance-based (not raycast/line-of-sight), so no interaction-script
    /// change is needed - the room was already fully usable from inside.</summary>
    public static class Mini169FarmSafehouseInterior
    {
        [MenuItem("Up Iz Up Mini/MINI-169/Build Farm Safehouse Front Wall")]
        public static void Build()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);
            var building = GameObject.Find("FarmSafehouse_Building");
            if (building == null) { Debug.LogError("MINI169: FarmSafehouse_Building not found"); if (Application.isBatchMode) EditorApplication.Exit(1); return; }

            // Reuse the existing SafehouseWall material (read off BackWall)
            // rather than creating a new one - keeps the new wall visually
            // identical to the three that already exist.
            var backWall = building.transform.Find("BackWall");
            var wallMat = backWall.GetComponent<MeshRenderer>().sharedMaterial;

            // Remove a previous run's wall/lintel so this tool is repeatable
            // (Preview/Integrate pattern used throughout this project).
            foreach (var name in new[] { "FrontWallL", "FrontWallR", "DoorLintel" })
            {
                var old = building.transform.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }

            // Matches BackWall's convention exactly: same Z-thickness (.25),
            // same height (2.90) and Y-centre (1.45), mirrored to the front
            // edge (+2.30 instead of BackWall's -2.30). Door gap is 1.2m
            // wide (comfortable walk-through), centred on X like the room.
            const float doorHalfWidth = 0.6f, roomHalfWidth = 2.60f, wallHeight = 2.90f, wallY = 1.45f, wallThick = 0.25f, frontZ = 2.30f;
            float segWidth = roomHalfWidth - doorHalfWidth; // 2.0m each side
            float segCentre = doorHalfWidth + segWidth / 2f; // 1.6m from centre

            MakeWallCube("FrontWallL", building.transform, new Vector3(-segCentre, wallY, frontZ), new Vector3(segWidth, wallHeight, wallThick), wallMat);
            MakeWallCube("FrontWallR", building.transform, new Vector3(segCentre, wallY, frontZ), new Vector3(segWidth, wallHeight, wallThick), wallMat);
            // Lintel above the doorway (2.2m clearance - well above the
            // character controller's height) so the opening reads as a real
            // doorway, not a garage-width gap to the roofline. Wall spans
            // Y=[0, wallHeight] (wallY is its centre), so the lintel fills
            // Y=[doorClearance, wallHeight].
            const float doorClearance = 2.2f;
            float lintelHeight = wallHeight - doorClearance;
            float lintelCentreY = doorClearance + lintelHeight / 2f;
            MakeWallCube("DoorLintel", building.transform, new Vector3(0f, lintelCentreY, frontZ), new Vector3(doorHalfWidth * 2f, lintelHeight, wallThick), wallMat);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("MINI169_FARM_INTERIOR_PASS saved");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void MakeWallCube(string name, Transform parent, Vector3 localPos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            // BoxCollider already added by CreatePrimitive, matching every
            // sibling wall's collider setup exactly.
        }
    }
}
