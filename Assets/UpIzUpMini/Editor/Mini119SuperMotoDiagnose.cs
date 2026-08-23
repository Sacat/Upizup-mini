using Gadd420;
using UnityEditor;
using UnityEngine;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-119, user: "the bike when down flat so i couldnt ride to
    /// test." Read-only diagnostic - instantiates the built
    /// TMAX_560_SuperMoto prefab in isolation and checks every reference
    /// RB_Controller actually needs to stand up straight, rather than
    /// guessing at the cause.
    /// </summary>
    public static class Mini119SuperMotoDiagnose
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Diagnose SuperMoto Prefab (read-only)")]
        public static void Diagnose()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UpIzUpMini/Vehicles/TMAX_560_SuperMoto.prefab");
            if (prefab == null) { Debug.LogError("MINI-119 DIAGNOSE: prefab not found."); return; }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = new Vector3(0f, 5f, 0f);
            instance.transform.rotation = Quaternion.identity;

            var rb = instance.GetComponent<Rigidbody>();
            var gadd = instance.GetComponent<RB_Controller>();
            var input = instance.GetComponent<GaddInputAdapter>();
            var facade = instance.GetComponent<TmaxBikeController>();

            Debug.Log($"MINI-119 DIAGNOSE: Rigidbody present={rb != null}, mass={(rb != null ? rb.mass : -1)}, constraints={(rb != null ? rb.constraints.ToString() : "n/a")}");
            Debug.Log($"MINI-119 DIAGNOSE: RB_Controller present={gadd != null}");
            Debug.Log($"MINI-119 DIAGNOSE: GaddInputAdapter present={input != null}");
            Debug.Log($"MINI-119 DIAGNOSE: TmaxBikeController facade present={facade != null}");

            if (gadd != null)
            {
                Debug.Log($"MINI-119 DIAGNOSE: cog assigned={gadd.cog != null}" + (gadd.cog != null ? $" at local {gadd.cog.localPosition}" : ""));
                Debug.Log($"MINI-119 DIAGNOSE: forkPivot assigned={gadd.forkPivot != null}");
                Debug.Log($"MINI-119 DIAGNOSE: wheelColliders array length={(gadd.wheelColliders != null ? gadd.wheelColliders.Length : -1)}");
                if (gadd.wheelColliders != null)
                {
                    for (int i = 0; i < gadd.wheelColliders.Length; i++)
                    {
                        var wc = gadd.wheelColliders[i];
                        if (wc == null) { Debug.LogError($"MINI-119 DIAGNOSE: wheelColliders[{i}] is NULL - this alone would cause a NullReferenceException every frame and likely leave the bike uncontrolled/falling."); continue; }
                        Debug.Log($"MINI-119 DIAGNOSE: wheelColliders[{i}] '{wc.name}' localPos={wc.transform.localPosition} radius={wc.radius} suspensionDistance={wc.suspensionDistance}");
                    }
                }
                Debug.Log($"MINI-119 DIAGNOSE: wheels[] (visual mesh refs) length={(gadd.wheels != null ? gadd.wheels.Length : -1)}, any null={(gadd.wheels != null && System.Array.Exists(gadd.wheels, w => w == null))}");
            }

            var allColliders = instance.GetComponentsInChildren<Collider>(true);
            int wheelColliderCount = 0, triggerCount = 0, solidCount = 0;
            foreach (var c in allColliders)
            {
                if (c is WheelCollider) wheelColliderCount++;
                else if (c.isTrigger) triggerCount++;
                else solidCount++;
            }
            Debug.Log($"MINI-119 DIAGNOSE: total colliders={allColliders.Length} (wheelColliders={wheelColliderCount}, other triggers={triggerCount}, other SOLID (non-trigger)={solidCount})");

            // A solid, non-wheel, non-trigger collider low on the bike body
            // would double up with the WheelColliders' own ground contact
            // and could easily be exactly what knocks it flat on spawn.
            foreach (var c in allColliders)
            {
                if (c is WheelCollider || c.isTrigger) continue;
                Debug.Log($"MINI-119 DIAGNOSE: SOLID collider '{c.gameObject.name}' type={c.GetType().Name} bounds.center(world)={c.bounds.center} bounds.size={c.bounds.size}");
            }

            var ragdollMgr = instance.GetComponentInChildren<RagdollManager>(true);
            var crashCtrl = instance.GetComponent<CrashController>();
            Debug.Log($"MINI-119 DIAGNOSE: RagdollManager present={ragdollMgr != null}, CrashController present={crashCtrl != null}" + (crashCtrl != null ? $", decelerationSpeedForCrash={crashCtrl.decelerationSpeedForCrash}" : ""));

            Object.DestroyImmediate(instance);
            Debug.Log("MINI-119 DIAGNOSE COMPLETE.");
        }
    }
}
