#if MINI206_DRAFT
// =====================================================================================================================
// MINI-206 DRAFT - written in the cloud design lane, NEVER COMPILED OR RUN. Enable by adding the scripting define
// MINI206_DRAFT (Player Settings > Other Settings > Scripting Define Symbols) on the local machine, review, test on a copy.
// =====================================================================================================================
using UnityEngine;
using Gadd420;

namespace UpIzUpMini.Vehicles.Draft
{
    /// <summary>
    /// Puts the rebuilt TMAX visual parts (MINI-206 Stage 1 FBX naming contract) on the SuperMoto physics rig
    /// (TMAX_560_SuperMoto.prefab: Gadd420 RB_Controller + SuperMoto assists), so the TMAX rides with the physics the owner
    /// likes while looking like a TMAX.
    ///
    /// Add to the root of a COPY of TMAX_560_SuperMoto, assign <see cref="tmaxVisual"/> (an instance of the Stage 1 FBX
    /// parented under the bike root, origin on the ground between the tyres), press Play. At Awake it:
    ///  1. hides the SuperMoto's own body meshes (Mesh_GRP) - colliders, wheel colliders, rider rig and assists stay;
    ///  2. re-parents TMAX_RearWheel onto the rig's RearWheelPos and TMAX_FrontWheel onto FrontWheelPos (world pose kept),
    ///     so RB_Controller.SetWheelMeshPos drives them (it moves wheels[i] to the WheelCollider pose and spins around X);
    ///  3. steers the TMAX fork assembly around its REAL raked steering axis. RB_Controller.HandleSteering writes
    ///     forkPivot.localRotation = Euler(~0, steer, ~0) (it passes quaternion components as Euler angles, so in practice a
    ///     pure local-Y yaw). We therefore give the fork its own two-level frame: RakeFrame (on the head bearing, tilted by
    ///     the rake angle) -> SteerYaw (copies the vendor fork pivot's local Y yaw each LateUpdate) -> TMAX_FrontForkAssembly;
    ///  4. moves the front wheel visual under the fork so it turns with the bars, while its height still follows the
    ///     WheelCollider suspension (FrontWheelPos is a child of the vendor Fork_Pivot, which only yaws).
    /// Physics, inputs, wheelie/upright assists, crash and rider IK are the SuperMoto's and are not touched.
    /// </summary>
    [DisallowMultipleComponent]
    public class TmaxSuperMotoVisualBinder : MonoBehaviour
    {
        [Tooltip("Instance of the MINI-206 TMAX FBX, child of this bike root.")]
        public Transform tmaxVisual;
        [Tooltip("Steering head rake (degrees from vertical, top tilted back). TMAX 560: about 25-26.")]
        public float rakeDegrees = 26f;
        [Tooltip("Hide the SuperMoto's own body meshes (Mesh_GRP).")]
        public bool hideSuperMotoBody = true;

        RB_Controller _rb;
        Transform _vendorForkPivot, _steerYaw;
        float _baseYaw;

        void Awake()
        {
            _rb = GetComponent<RB_Controller>();
            if (_rb == null || tmaxVisual == null) { Debug.LogError("[MINI-206 binder] needs RB_Controller and tmaxVisual"); enabled = false; return; }

            if (hideSuperMotoBody)
            {
                var meshGroup = transform.Find("Mesh_GRP");
                if (meshGroup != null) foreach (var r in meshGroup.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            }

            Transform fork = Find(tmaxVisual, "TMAX_FrontForkAssembly");
            Transform front = Find(tmaxVisual, "TMAX_FrontWheel");
            Transform rear = Find(tmaxVisual, "TMAX_RearWheel");
            if (fork == null || front == null || rear == null) { Debug.LogError("[MINI-206 binder] TMAX FBX parts missing (naming contract)"); enabled = false; return; }

            // wheels: the vendor moves/spins wheels[0]=rear, wheels[1]=front. Put the TMAX wheel meshes under those holders.
            // The vendor snaps each holder to its WheelCollider pose every frame, so the WheelColliders themselves must sit on
            // the TMAX axles (set on the prefab copy, see MINI-206-riding.md step 3). Report a mismatch instead of hiding it.
            GameObject rearHolder = _rb.wheels[0], frontHolder = _rb.wheels[1];
            CheckAxle("rear", _rb.wheelColliders[0], rear);
            CheckAxle("front", _rb.wheelColliders[1], front);
            rear.SetParent(rearHolder.transform, true);

            // fork: RakeFrame on the head bearing (= the fork assembly pivot from the FBX), SteerYaw under it.
            _vendorForkPivot = _rb.forkPivot.transform;
            _baseYaw = _vendorForkPivot.localEulerAngles.y;
            var rake = new GameObject("TMAX_RakeFrame").transform;
            rake.SetParent(tmaxVisual, false);
            rake.position = fork.position;
            rake.rotation = tmaxVisual.rotation * Quaternion.Euler(-rakeDegrees, 0f, 0f); // top of the axis tilted back
            _steerYaw = new GameObject("TMAX_SteerYaw").transform;
            _steerYaw.SetParent(rake, false);
            fork.SetParent(_steerYaw, true);

            // the front wheel must steer with the fork but keep its WheelCollider height: its holder lives under the vendor
            // Fork_Pivot (yaw only). Parenting the mesh to the holder keeps suspension + spin; the vendor yaw is about the
            // bike's vertical, the TMAX axis is raked - at 30 deg of steer the difference at the contact patch is about
            // 1 cm, acceptable for a scooter. (If it shows, drive the holder's yaw from _steerYaw instead.)
            front.SetParent(frontHolder.transform, true);
        }

        void LateUpdate()
        {
            if (_steerYaw == null || _vendorForkPivot == null) return;
            float yaw = Mathf.DeltaAngle(_baseYaw, _vendorForkPivot.localEulerAngles.y);
            _steerYaw.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        static void CheckAxle(string label, WheelCollider wc, Transform wheel)
        {
            if (wc == null) return;
            wc.GetWorldPose(out Vector3 pos, out _);
            float d = Vector3.Distance(pos, wheel.position);
            if (d > 0.02f) Debug.LogWarning("[MINI-206 binder] " + label + " WheelCollider is " + d.ToString("0.000") + " m from the TMAX axle - move the collider (radius ~0.29 m) onto TMAX_" + (label == "rear" ? "Rear" : "Front") + "Wheel.");
        }

        static Transform Find(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
    }
}
#endif
