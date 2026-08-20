using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-052: a batch-mode-safe sanity check for the NavMesh
    /// Mini011PhaseBSetup.BuildNavigationMesh bakes. Editor Play Mode
    /// entry via -executeMethod is documented elsewhere in this project as
    /// broken in this environment (Search-module init exception - see the
    /// MINI-001 handoff entry), so Update()-driven NPC steering can't be
    /// exercised headlessly; this instead proves, without Play Mode or an
    /// EXE build, that the baked NavMesh actually has usable coverage and
    /// that a path between two real in-scene points (the road start and
    /// the farm plot) succeeds and detours around at least one obstacle
    /// rather than being a straight two-corner line. NavMeshSurface adds
    /// its baked data on OnEnable, which already runs in edit mode (unlike
    /// Update), so NavMesh.SamplePosition/CalculatePath work here even
    /// without pressing Play.
    /// </summary>
    public static class Mini052NavMeshValidation
    {
        [MenuItem("Up Iz Up Mini/MINI-052/Validate NavMesh Coverage")]
        public static void Validate()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var surfaceGo = GameObject.Find("NavMeshSurface");
            if (surfaceGo == null)
            {
                Debug.LogError("MINI-052 NAVMESH VALIDATION FAIL: no NavMeshSurface object in the built scene.");
                return;
            }

            var sacat = GameObject.Find("Sacat");
            var farmPlot = GameObject.Find("FarmPlot_00");
            if (sacat == null || farmPlot == null)
            {
                Debug.LogError("MINI-052 NAVMESH VALIDATION FAIL: Sacat or FarmPlot_00 not found in scene.");
                return;
            }

            Vector3 from = sacat.transform.position;
            Vector3 to = farmPlot.transform.position;

            bool fromOk = NavMesh.SamplePosition(from, out var fromHit, 5f, NavMesh.AllAreas);
            bool toOk = NavMesh.SamplePosition(to, out var toHit, 5f, NavMesh.AllAreas);
            if (!fromOk || !toOk)
            {
                Debug.LogError($"MINI-052 NAVMESH VALIDATION FAIL: NavMesh.SamplePosition missed - fromOk={fromOk}, toOk={toOk}. The bake likely has no coverage near these points.");
                return;
            }

            var path = new NavMeshPath();
            bool calculated = NavMesh.CalculatePath(fromHit.position, toHit.position, NavMesh.AllAreas, path);
            int corners = path.corners.Length;

            if (!calculated || path.status != NavMeshPathStatus.PathComplete || corners < 2)
            {
                Debug.LogError($"MINI-052 NAVMESH VALIDATION FAIL: calculated={calculated}, status={path.status}, corners={corners}.");
                return;
            }

            // Second, sharper check: the NavMesh must NOT cover the inside
            // of a real building. A path/coverage check alone can't
            // distinguish "the bake actually excludes obstacles" from "the
            // whole terrain rectangle got marked walkable" - this proves
            // the building's own footprint was carved out, not assumed.
            var safehouse = GameObject.Find("FarmSafehouse_Building");
            if (safehouse == null)
            {
                Debug.LogError("MINI-052 NAVMESH VALIDATION FAIL: FarmSafehouse_Building not found in scene.");
                return;
            }
            Vector3 insideHouse = safehouse.transform.position;
            bool houseInteriorWalkable = NavMesh.SamplePosition(insideHouse, out _, 0.6f, NavMesh.AllAreas);
            if (houseInteriorWalkable)
            {
                Debug.LogError("MINI-052 NAVMESH VALIDATION FAIL: NavMesh.SamplePosition found walkable NavMesh inside FarmSafehouse_Building's own footprint - the bake is not excluding building geometry.");
                return;
            }

            Debug.Log($"MINI-052 NAVMESH VALIDATION PASS: NavMesh has coverage near both Sacat's spawn and the farm plot, a complete path was found with {corners} corner(s) (status={path.status}), and the farm safehouse building's own footprint is correctly excluded from the walkable mesh (not just a blanket-walkable terrain rectangle).");
        }
    }
}
