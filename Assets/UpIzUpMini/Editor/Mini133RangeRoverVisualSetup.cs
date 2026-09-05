using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    public static class Mini133RangeRoverVisualSetup
    {
        private const string RoverPath = "Assets/UpIzUpMini/Art/Vehicles/RangeRover_Vehicle.prefab";
        private const string SuperMotoPath = "Assets/MotorbikePhysicsTool/Prefabs/Bikes/SuperMoto.prefab";
        private const string TrailPath = "Assets/MotorbikePhysicsTool/Prefabs/Skids/SkidTrail.prefab";
        private const string SmokePath = "Assets/MotorbikePhysicsTool/Prefabs/Skids/Smoke.prefab";
        private const string TyreMaterialPath = "Assets/UpIzUpMini/Art/Materials/BlackSuvWheel.mat";
        private const string RimMaterialPath = "Assets/UpIzUpMini/Art/Materials/RangeRoverRim.mat";
        private const string EvidenceDirectory = "Logs/Tasks/MINI-133";

        [MenuItem("Up Iz Up Mini/MINI-133/Build Validate Capture Range Rover")]
        public static void BuildValidateCapture()
        {
            Build();
            Mini133RangeRoverValidation.Validate();
            Capture();
        }

        public static void Build()
        {
            GameObject rover = PrefabUtility.LoadPrefabContents(RoverPath);
            GameObject source = PrefabUtility.LoadPrefabContents(SuperMotoPath);
            if (rover == null || source == null)
                throw new InvalidOperationException("MINI-133 could not load the Range Rover or SuperMoto reference prefab.");

            try
            {
                WheelCollider fl = RequireWheel(rover.transform, "FrontLeft");
                WheelCollider fr = RequireWheel(rover.transform, "FrontRight");
                WheelCollider rl = RequireWheel(rover.transform, "RearLeft");
                WheelCollider rr = RequireWheel(rover.transform, "RearRight");
                Transform sourceWheel = FindDeep(source.transform, "Torus");
                if (sourceWheel == null || sourceWheel.GetComponent<MeshFilter>()?.sharedMesh == null)
                    throw new InvalidOperationException("The approved SuperMoto reference wheel mesh was not found.");

                Material tyre = AssetDatabase.LoadAssetAtPath<Material>(TyreMaterialPath);
                if (tyre == null) throw new InvalidOperationException("BlackSuvWheel material is missing.");
                Material rim = GetOrCreateRimMaterial();

                Transform visualRoot = FindDirect(rover.transform, "MINI133_RangeRoverWheels");
                if (visualRoot != null) UnityEngine.Object.DestroyImmediate(visualRoot.gameObject);
                visualRoot = new GameObject("MINI133_RangeRoverWheels").transform;
                visualRoot.SetParent(rover.transform, false);

                Transform flVisual = BuildWheel(visualRoot, "Visual_FrontLeft", fl, sourceWheel, tyre, rim);
                Transform frVisual = BuildWheel(visualRoot, "Visual_FrontRight", fr, sourceWheel, tyre, rim);
                Transform rlVisual = BuildWheel(visualRoot, "Visual_RearLeft", rl, sourceWheel, tyre, rim);
                Transform rrVisual = BuildWheel(visualRoot, "Visual_RearRight", rr, sourceWheel, tyre, rim);

                var visuals = rover.GetComponent<RangeRoverWheelVisuals>();
                if (visuals == null) visuals = rover.AddComponent<RangeRoverWheelVisuals>();
                visuals.Configure(fl, fr, rl, rr, flVisual, frVisual, rlVisual, rrVisual);

                Transform oldOutlet = FindDirect(rover.transform, "RangeRover_ExhaustOutlet");
                if (oldOutlet != null) UnityEngine.Object.DestroyImmediate(oldOutlet.gameObject);
                Transform outlet = new GameObject("RangeRover_ExhaustOutlet").transform;
                outlet.SetParent(rover.transform, false);
                float estimatedLength = Mathf.Abs(rl.transform.localPosition.z) / 0.31f;
                outlet.localPosition = new Vector3(
                    rr.transform.localPosition.x * 0.78f,
                    rr.radius * 1.15f,
                    -estimatedLength * 0.485f);
                outlet.localRotation = Quaternion.LookRotation(Vector3.back, Vector3.up);

                GameObject trail = AssetDatabase.LoadAssetAtPath<GameObject>(TrailPath);
                GameObject smoke = AssetDatabase.LoadAssetAtPath<GameObject>(SmokePath);
                if (trail == null || smoke == null || smoke.GetComponent<ParticleSystem>() == null)
                    throw new InvalidOperationException("The approved skid/smoke effect prefabs are missing.");

                var effects = rover.GetComponent<RangeRoverRoadEffects>();
                if (effects == null) effects = rover.AddComponent<RangeRoverRoadEffects>();
                effects.Configure(rl, rr, outlet, trail.transform, smoke.GetComponent<ParticleSystem>());

                if (!Mathf.Approximately(rover.GetComponent<Rigidbody>().mass, Mini071RoverVehiclePrep.MassKg))
                    throw new InvalidOperationException("Range Rover mass changed; refusing to save.");

                PrefabUtility.SaveAsPrefabAsset(rover, RoverPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("MINI-133 BUILD PASS: four mapped visual wheels, front steering, twin rear marks and bounded tailpipe smoke added without handling changes.");
            }
            finally
            {
                if (rover != null) PrefabUtility.UnloadPrefabContents(rover);
                if (source != null) PrefabUtility.UnloadPrefabContents(source);
            }
        }

        private static Transform BuildWheel(
            Transform parent,
            string name,
            WheelCollider collider,
            Transform source,
            Material tyre,
            Material rim)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = collider.transform.localPosition;
            pivot.localRotation = Quaternion.identity;

            Mesh mesh = source.GetComponent<MeshFilter>().sharedMesh;
            var tyreObject = new GameObject("BlackTyre", typeof(MeshFilter), typeof(MeshRenderer));
            tyreObject.transform.SetParent(pivot, false);
            float sourceRadius = Mathf.Max(mesh.bounds.extents.y, mesh.bounds.extents.z);
            float radiusScale = collider.radius * 1.07f / Mathf.Max(0.001f, sourceRadius);
            float targetHalfWidth = collider.radius * 0.36f;
            float widthScale = targetHalfWidth / Mathf.Max(0.001f, mesh.bounds.extents.x);
            tyreObject.transform.localScale = new Vector3(widthScale, radiusScale, radiusScale);
            tyreObject.transform.localPosition = -Vector3.Scale(mesh.bounds.center, tyreObject.transform.localScale);
            tyreObject.GetComponent<MeshFilter>().sharedMesh = mesh;
            tyreObject.GetComponent<MeshRenderer>().sharedMaterial = tyre;

            // Five cheap metallic spokes make rotation readable; they are visual
            // children of the WheelCollider-driven pivot and have no colliders.
            for (int i = 0; i < 5; i++)
            {
                var spoke = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spoke.name = $"RimSpoke_{i + 1}";
                UnityEngine.Object.DestroyImmediate(spoke.GetComponent<Collider>());
                spoke.transform.SetParent(pivot, false);
                spoke.transform.localPosition = Vector3.zero;
                spoke.transform.localRotation = Quaternion.Euler(i * 72f, 0f, 0f);
                spoke.transform.localScale = new Vector3(targetHalfWidth * 1.15f, collider.radius * 0.085f, collider.radius * 1.18f);
                spoke.GetComponent<Renderer>().sharedMaterial = rim;
            }

            return pivot;
        }

        private static Material GetOrCreateRimMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(RimMaterialPath);
            if (material != null) return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { name = "RangeRoverRim" };
            Color color = new Color(0.16f, 0.17f, 0.19f, 1f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.72f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.46f);
            AssetDatabase.CreateAsset(material, RimMaterialPath);
            return material;
        }

        public static void Capture()
        {
            Directory.CreateDirectory(EvidenceDirectory);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoverPath);
            GameObject rover = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            rover.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            if (rover.TryGetComponent(out Rigidbody rb)) { rb.isKinematic = true; rb.useGravity = false; }
            foreach (MonoBehaviour behaviour in rover.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;

            RangeRoverWheelVisuals visuals = rover.GetComponent<RangeRoverWheelVisuals>();
            PoseForCapture(visuals.FrontLeftVisual, 27f, 62f);
            PoseForCapture(visuals.FrontRightVisual, 27f, 62f);
            PoseForCapture(visuals.RearLeftVisual, 0f, 105f);
            PoseForCapture(visuals.RearRightVisual, 0f, 105f);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var groundMaterial = new Material(shader);
            if (groundMaterial.HasProperty("_BaseColor")) groundMaterial.SetColor("_BaseColor", new Color(0.18f, 0.2f, 0.19f));
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(2f, 1f, 2f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.54f, 0.56f, 0.6f);
            var lightObject = new GameObject("PreviewLight", typeof(Light));
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.3f; light.shadows = LightShadows.None;
            lightObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            CaptureCamera(new Vector3(4.8f, 2.0f, 6.4f), new Vector3(0f, 0.72f, 0.5f), "range-rover-front-steering.png");
            CaptureCamera(new Vector3(4.8f, 1.7f, -6.2f), new Vector3(0f, 0.68f, -0.55f), "range-rover-rear-wheel-tailpipe.png");
            UnityEngine.Object.DestroyImmediate(groundMaterial);
            Debug.Log($"MINI-133 screenshots written to {EvidenceDirectory}.");
        }

        private static void PoseForCapture(Transform wheel, float steer, float spin)
        {
            if (wheel != null) wheel.localRotation = Quaternion.Euler(0f, steer, 0f) * Quaternion.Euler(spin, 0f, 0f);
        }

        private static void CaptureCamera(Vector3 position, Vector3 target, string fileName)
        {
            var cameraObject = new GameObject("EvidenceCamera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.transform.position = position;
            camera.transform.LookAt(target);
            camera.fieldOfView = 38f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.065f, 0.08f);
            var rt = new RenderTexture(1280, 800, 24);
            var texture = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, 1280, 800), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(EvidenceDirectory, fileName), texture.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }

        internal static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static Transform FindDirect(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
                if (parent.GetChild(i).name == name) return parent.GetChild(i);
            return null;
        }

        private static WheelCollider RequireWheel(Transform root, string name)
        {
            Transform found = FindDeep(root, name);
            WheelCollider wheel = found != null ? found.GetComponent<WheelCollider>() : null;
            if (wheel == null) throw new InvalidOperationException($"WheelCollider {name} is missing.");
            return wheel;
        }
    }
}
