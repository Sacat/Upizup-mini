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
    ///  3. steers TMAX_FrontForkAssembly and TMAX_Handlebar about their own local Y (the FBX aligns it with the real 25 deg raked
    ///     steering axis), copying the yaw RB_Controller.HandleSteering writes onto the vendor fork pivot each LateUpdate;
    ///  4. hangs the front wheel visual on the vendor front holder so it steers and follows the WheelCollider suspension.
    /// Physics, inputs, wheelie/upright assists, crash and rider IK are the SuperMoto's and are not touched.
    /// </summary>
    [DisallowMultipleComponent]
    public class TmaxSuperMotoVisualBinder : MonoBehaviour
    {
        [Tooltip("Instance of the MINI-206 TMAX FBX, child of this bike root.")]
        public Transform tmaxVisual;
        [Tooltip("Hide the SuperMoto's own body meshes (Mesh_GRP).")]
        public bool hideSuperMotoBody = true;

        RB_Controller _rb;
        Transform _vendorForkPivot;
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

            // fork + handlebar: the MINI-206 FBX already gives both parts a local frame whose Y axis IS the 25 deg raked steering
            // axis (fork pivot on the head bearing, handlebar pivot on the bar clamp), so steering is a plain local-Y rotation.
            _vendorForkPivot = _rb.forkPivot.transform;
            _baseYaw = _vendorForkPivot.localEulerAngles.y;
            _fork = fork; _forkBase = fork.localRotation;
            _bar = Find(tmaxVisual, "TMAX_Handlebar"); if (_bar != null) _barBase = _bar.localRotation;
            // the front wheel steers with the vendor holder (yaw about the bike vertical); at 30 deg of steer it differs from the
            // raked fork by about 1 cm at the contact patch - acceptable for a scooter.
            front.SetParent(frontHolder.transform, true);
        }

        Transform _fork, _bar; Quaternion _forkBase, _barBase;

        void LateUpdate()
        {
            if (_vendorForkPivot == null) return;
            float yaw = Mathf.DeltaAngle(_baseYaw, _vendorForkPivot.localEulerAngles.y);
            if (_fork != null) _fork.localRotation = _forkBase * Quaternion.Euler(0f, yaw, 0f);
            if (_bar != null) _bar.localRotation = _barBase * Quaternion.Euler(0f, yaw, 0f);
        }

        static void CheckAxle(string label, WheelCollider wc, Transform wheel)
        {
            if (wc == null) return;
            wc.GetWorldPose(out Vector3 pos, out _);
            float d = Vector3.Distance(pos, wheel.position);
            if (d > 0.02f) Debug.LogWarning("[MINI-206 binder] " + label + " WheelCollider is " + d.ToString("0.000") + " m from the TMAX axle - move the collider (radius 0.304 m front / 0.254 m rear) onto TMAX_" + (label == "rear" ? "Rear" : "Front") + "Wheel.");
        }

        static Transform Find(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
    }
}
#endif
