using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-124. Reversible TMAX wheel/steering visual upgrade. The first gate is
    /// deliberately read-only: measure both prefab hierarchies before selecting
    /// source meshes or authoring any fit values.
    /// </summary>
    public static class Mini124TmaxWheelSteeringUpgrade
    {
        private const string TmaxPrefabPath = "Assets/UpIzUpMini/Vehicles/TMAX_560.prefab";
        private const string SuperMotoPrefabPath = "Assets/MotorbikePhysicsTool/Prefabs/Bikes/SuperMoto.prefab";
        private const string BlackMaterialPath = "Assets/UpIzUpMini/Vehicles/TmaxWheelBlack.mat";
        private const string EvidenceDirectory = "Logs/Tasks/MINI-124";
        private const string SteeringPivotName = "MINI124_SteeringPivot";
        private const string WheelMeshName = "SuperMotoBlackWheel";

        [MenuItem("Up Iz Up Mini/MINI-124/01 Inspect TMAX and SuperMoto Sources")]
        public static void InspectSources()
        {
            Directory.CreateDirectory(EvidenceDirectory);
            var report = new StringBuilder(32768);
            report.AppendLine("MINI-124 source hierarchy inspection");
            report.AppendLine($"Generated: {DateTime.Now:O}");
            report.AppendLine();

            AppendPrefab(report, "TMAX TARGET", TmaxPrefabPath);
            AppendPrefab(report, "SUPERMOTO REFERENCE", SuperMotoPrefabPath);

            string outputPath = Path.Combine(EvidenceDirectory, "source-hierarchy.txt");
            File.WriteAllText(outputPath, report.ToString());
            Debug.Log($"MINI-124 source inspection written to {outputPath}");
        }

        [MenuItem("Up Iz Up Mini/MINI-124/02 Capture Before")]
        public static void CaptureBefore()
        {
            CapturePrefabEvidence("before", false);
        }

        [MenuItem("Up Iz Up Mini/MINI-124/03 Build Wheel and Steering Upgrade")]
        public static void BuildUpgrade()
        {
            Directory.CreateDirectory(EvidenceDirectory);
            string backupPath = Path.Combine(EvidenceDirectory, "TMAX_560-before-MINI-124.prefab.txt");
            if (!File.Exists(backupPath))
                File.Copy(TmaxPrefabPath, backupPath, false);

            GameObject tmax = PrefabUtility.LoadPrefabContents(TmaxPrefabPath);
            GameObject superMoto = PrefabUtility.LoadPrefabContents(SuperMotoPrefabPath);
            if (tmax == null || superMoto == null)
                throw new InvalidOperationException("MINI-124 could not load one or both vehicle prefabs.");

            try
            {
                Transform visualLean = Require(tmax.transform, "VisualLeanRoot");
                Transform frontWheel = Require(tmax.transform, "FrontWheel");
                Transform rearWheel = Require(tmax.transform, "RearWheel");
                Transform leftGrip = Require(tmax.transform, "HandlebarLeft");
                Transform rightGrip = Require(tmax.transform, "HandlebarRight");
                WheelCollider frontCollider = Require(tmax.transform, "FrontWheelCollider").GetComponent<WheelCollider>();
                WheelCollider rearCollider = Require(tmax.transform, "RearWheelCollider").GetComponent<WheelCollider>();
                TmaxWheelVisuals wheelVisuals = tmax.GetComponent<TmaxWheelVisuals>();
                if (frontCollider == null || rearCollider == null || wheelVisuals == null)
                    throw new InvalidOperationException("MINI-124 TMAX physics/visual baseline is incomplete.");

                float massBefore = tmax.GetComponent<Rigidbody>().mass;
                float frontRadiusBefore = frontCollider.radius;
                float rearRadiusBefore = rearCollider.radius;

                Transform sourceFrontWheel = Require(superMoto.transform, "Torus");
                Transform sourceRearWheel = Require(superMoto.transform, "Torus.001");
                Transform sourceForkPivot = Require(superMoto.transform, "Fork_Pivot");
                Transform sourceFrontAxle = Require(sourceForkPivot, "FrontWheelPos");
                Transform sourceHandlebar = Require(sourceForkPivot, "Cylinder");
                Transform sourceFork = Require(sourceForkPivot, "Cylinder.005");

                Material blackMaterial = GetOrCreateBlackMaterial();
                ReplaceWheelMesh(frontWheel, sourceFrontWheel, frontCollider.radius, blackMaterial);
                ReplaceWheelMesh(rearWheel, sourceRearWheel, rearCollider.radius, blackMaterial);

                Transform oldPivot = FindDirect(visualLean, SteeringPivotName);
                if (oldPivot != null)
                {
                    if (leftGrip.IsChildOf(oldPivot)) leftGrip.SetParent(tmax.transform, true);
                    if (rightGrip.IsChildOf(oldPivot)) rightGrip.SetParent(tmax.transform, true);
                    UnityEngine.Object.DestroyImmediate(oldPivot.gameObject);
                }

                var pivotObject = new GameObject(SteeringPivotName);
                Transform steeringPivot = pivotObject.transform;
                steeringPivot.SetParent(visualLean, false);
                Vector3 gripMidWorld = (leftGrip.position + rightGrip.position) * 0.5f;
                steeringPivot.localPosition = visualLean.InverseTransformPoint(gripMidWorld);
                steeringPivot.localRotation = Quaternion.identity;

                var fitObject = new GameObject("SuperMotoForkHandlebarFit");
                Transform fit = fitObject.transform;
                fit.SetParent(steeringPivot, false);

                Vector3 sourceAxleVector = sourceForkPivot.InverseTransformPoint(sourceFrontAxle.position);
                Vector3 targetAxleVector = visualLean.InverseTransformPoint(frontWheel.position) - steeringPivot.localPosition;
                float longitudinalScale = targetAxleVector.magnitude / Mathf.Max(0.001f, sourceAxleVector.magnitude);
                float targetGripSpan = Vector3.Distance(leftGrip.position, rightGrip.position) / Mathf.Max(0.001f, tmax.transform.lossyScale.x);
                Mesh handlebarMesh = sourceHandlebar.GetComponent<MeshFilter>().sharedMesh;
                float sourceBarWidth = Mathf.Max(0.001f, handlebarMesh.bounds.size.x);
                float lateralScale = targetGripSpan / sourceBarWidth;

                fit.localPosition = Vector3.zero;
                fit.localRotation = Quaternion.FromToRotation(sourceAxleVector.normalized, targetAxleVector.normalized);
                fit.localScale = new Vector3(lateralScale, longitudinalScale, longitudinalScale);
                CopyVisualMesh(sourceFork, fit, blackMaterial);
                CopyVisualMesh(sourceHandlebar, fit, blackMaterial);

                wheelVisuals.ConfigureVisuals(frontCollider, rearCollider, frontWheel, rearWheel, steeringPivot, leftGrip, rightGrip);

                if (!Mathf.Approximately(tmax.GetComponent<Rigidbody>().mass, massBefore)
                    || !Mathf.Approximately(frontCollider.radius, frontRadiusBefore)
                    || !Mathf.Approximately(rearCollider.radius, rearRadiusBefore))
                    throw new InvalidOperationException("MINI-124 detected an unintended physics change; prefab was not saved.");

                PrefabUtility.SaveAsPrefabAsset(tmax, TmaxPrefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("MINI-124 built reversible black SuperMoto-reference wheels and TMAX steering assembly without changing physics.");
            }
            finally
            {
                if (tmax != null) PrefabUtility.UnloadPrefabContents(tmax);
                if (superMoto != null) PrefabUtility.UnloadPrefabContents(superMoto);
            }
        }

        [MenuItem("Up Iz Up Mini/MINI-124/04 Validate and Capture After")]
        public static void ValidateAndCaptureAfter()
        {
            ValidatePrefab();
            CapturePrefabEvidence("after", true);
        }

        [MenuItem("Up Iz Up Mini/MINI-124/05 Capture Steering and Wheel Motion Frames")]
        public static void CaptureMotionFrames()
        {
            string frameDirectory = Path.Combine(EvidenceDirectory, "motion-frames");
            Directory.CreateDirectory(frameDirectory);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TmaxPrefabPath);
            GameObject bike = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            bike.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            if (bike.TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            foreach (MonoBehaviour behaviour in bike.GetComponentsInChildren<MonoBehaviour>(true))
                behaviour.enabled = false;

            Transform pivot = Require(bike.transform, SteeringPivotName);
            Transform frontWheel = Require(bike.transform, "FrontWheel");
            Transform rearWheel = Require(bike.transform, "RearWheel");

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var groundMaterial = new Material(shader);
            if (groundMaterial.HasProperty("_BaseColor")) groundMaterial.SetColor("_BaseColor", new Color(0.16f, 0.18f, 0.2f));
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(2f, 1f, 2f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.57f, 0.62f);
            var lightObject = new GameObject("PreviewKeyLight", typeof(Light));
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.shadows = LightShadows.None;
            lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

            var cameraObject = new GameObject("MINI124_MotionCamera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.transform.position = new Vector3(2.25f, 1.15f, 2.85f);
            camera.transform.LookAt(new Vector3(0f, 0.55f, 0.62f));
            camera.fieldOfView = 38f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.065f, 0.08f);
            camera.farClipPlane = 50f;

            const int frameCount = 48;
            for (int frame = 0; frame < frameCount; frame++)
            {
                float phase = frame / (float)(frameCount - 1);
                float steer = Mathf.Sin(phase * Mathf.PI * 2f) * 25f;
                float spin = phase * 900f;
                pivot.localRotation = Quaternion.Euler(0f, steer, 0f);
                frontWheel.localRotation = Quaternion.Euler(0f, steer, 0f) * Quaternion.Euler(spin, 0f, 0f);
                rearWheel.localRotation = Quaternion.Euler(spin, 0f, 0f);
                RenderCameraToFile(camera, Path.Combine(frameDirectory, $"frame-{frame:000}.png"), 960, 600);
            }

            UnityEngine.Object.DestroyImmediate(groundMaterial);
            Debug.Log($"MINI-124 captured {frameCount} steering/wheel-spin motion frames in {frameDirectory}.");
        }

        public static void ValidatePrefab()
        {
            Directory.CreateDirectory(EvidenceDirectory);
            var report = new StringBuilder();
            GameObject root = PrefabUtility.LoadPrefabContents(TmaxPrefabPath);
            try
            {
                Transform front = Require(root.transform, "FrontWheel");
                Transform rear = Require(root.transform, "RearWheel");
                Transform pivot = Require(root.transform, SteeringPivotName);
                Transform left = Require(root.transform, "HandlebarLeft");
                Transform right = Require(root.transform, "HandlebarRight");
                WheelCollider frontCollider = Require(root.transform, "FrontWheelCollider").GetComponent<WheelCollider>();
                WheelCollider rearCollider = Require(root.transform, "RearWheelCollider").GetComponent<WheelCollider>();
                TmaxWheelVisuals wheelVisuals = root.GetComponent<TmaxWheelVisuals>();

                Require(front, WheelMeshName);
                Require(rear, WheelMeshName);
                if (left.parent != root.transform || right.parent != root.transform)
                    throw new InvalidOperationException("Established root-level grip target paths were not preserved.");
                if (frontCollider == null || rearCollider == null)
                    throw new InvalidOperationException("WheelColliders are missing.");
                if (wheelVisuals == null)
                    throw new InvalidOperationException("TmaxWheelVisuals is missing.");
                if (!Mathf.Approximately(root.GetComponent<Rigidbody>().mass, 480f))
                    throw new InvalidOperationException("TMAX mass changed from the verified 480kg baseline.");
                const float verifiedRadius = 0.30352196f;
                if (Mathf.Abs(frontCollider.radius - verifiedRadius) > 0.00001f
                    || Mathf.Abs(rearCollider.radius - verifiedRadius) > 0.00001f)
                    throw new InvalidOperationException("TMAX WheelCollider radii changed from the verified baseline.");

                Vector3 leftBefore = left.position;
                Vector3 rightBefore = right.position;
                Quaternion pivotBefore = pivot.localRotation;
                InvokePrivate(wheelVisuals, "Awake");
                frontCollider.steerAngle = 22f;
                InvokePrivate(wheelVisuals, "LateUpdate");
                float actualSteer = Mathf.Abs(Mathf.DeltaAngle(0f, pivot.localEulerAngles.y));
                if (Mathf.Abs(actualSteer - 22f) > 0.2f)
                    throw new InvalidOperationException($"Steering pivot followed {actualSteer:0.##} degrees instead of 22 degrees.");
                if (Vector3.Distance(leftBefore, left.position) < 0.01f
                    || Vector3.Distance(rightBefore, right.position) < 0.01f)
                    throw new InvalidOperationException("One or both grip targets did not move with the steering pivot.");
                frontCollider.steerAngle = 0f;
                pivot.localRotation = pivotBefore;

                report.AppendLine("MINI-124 focused prefab validation: PASS");
                report.AppendLine($"Front black wheel mesh: {AnimationUtility.CalculateTransformPath(Require(front, WheelMeshName), root.transform)}");
                report.AppendLine($"Rear black wheel mesh: {AnimationUtility.CalculateTransformPath(Require(rear, WheelMeshName), root.transform)}");
                report.AppendLine($"Steering pivot: {AnimationUtility.CalculateTransformPath(pivot, root.transform)}");
                report.AppendLine("Grip targets retain established root paths and are driven by steering pivot: PASS");
                report.AppendLine("22-degree steering propagation to pivot and both grip targets: PASS");
                report.AppendLine("Rigidbody mass 480kg unchanged: PASS");
                report.AppendLine("WheelCollider radii unchanged: PASS");
                File.WriteAllText(Path.Combine(EvidenceDirectory, "validation.txt"), report.ToString());
                Debug.Log(report.ToString());
            }
            finally
            {
                if (root != null) PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ReplaceWheelMesh(Transform wheelPivot, Transform sourceWheel, float colliderRadius, Material blackMaterial)
        {
            Transform existing = FindDirect(wheelPivot, WheelMeshName);
            if (existing != null)
                UnityEngine.Object.DestroyImmediate(existing.gameObject);

            Mesh sourceMesh = sourceWheel.GetComponent<MeshFilter>().sharedMesh;
            var wheelObject = new GameObject(WheelMeshName, typeof(MeshFilter), typeof(MeshRenderer));
            Transform wheel = wheelObject.transform;
            wheel.SetParent(wheelPivot, false);
            float sourceRadius = Mathf.Max(sourceMesh.bounds.extents.y, sourceMesh.bounds.extents.z);
            float fitScale = colliderRadius * 1.08f / Mathf.Max(0.001f, sourceRadius);
            wheel.localScale = Vector3.one * fitScale;
            wheel.localPosition = -Vector3.Scale(sourceMesh.bounds.center, wheel.localScale);
            wheel.localRotation = Quaternion.identity;
            wheelObject.GetComponent<MeshFilter>().sharedMesh = sourceMesh;
            wheelObject.GetComponent<MeshRenderer>().sharedMaterial = blackMaterial;

            Transform oldDisc = FindDirect(wheelPivot, "WheelDiscVisual");
            if (oldDisc != null && oldDisc.TryGetComponent(out Renderer discRenderer))
                discRenderer.enabled = false;
        }

        private static void CopyVisualMesh(Transform source, Transform destinationParent, Material material)
        {
            MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
                throw new InvalidOperationException($"Source visual {source.name} has no mesh.");

            var copy = new GameObject(source.name + "_TMAX", typeof(MeshFilter), typeof(MeshRenderer));
            copy.transform.SetParent(destinationParent, false);
            copy.transform.localPosition = source.localPosition;
            copy.transform.localRotation = source.localRotation;
            copy.transform.localScale = source.localScale;
            copy.GetComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;
            copy.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Material GetOrCreateBlackMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(BlackMaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = "TmaxWheelBlack" };
                AssetDatabase.CreateAsset(material, BlackMaterialPath);
            }

            Color black = new Color(0.012f, 0.014f, 0.018f, 1f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", black);
            if (material.HasProperty("_Color")) material.SetColor("_Color", black);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.35f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.28f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CapturePrefabEvidence(string label, bool demonstrateSteering)
        {
            Directory.CreateDirectory(EvidenceDirectory);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TmaxPrefabPath);
            if (prefab == null) throw new InvalidOperationException("TMAX prefab not found for capture.");
            GameObject bike = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            bike.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            if (bike.TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            foreach (MonoBehaviour behaviour in bike.GetComponentsInChildren<MonoBehaviour>(true))
                behaviour.enabled = false;

            Transform pivot = FindDeep(bike.transform, SteeringPivotName);
            Transform frontWheel = FindDeep(bike.transform, "FrontWheel");
            Transform rearWheel = FindDeep(bike.transform, "RearWheel");
            if (demonstrateSteering && pivot != null && frontWheel != null && rearWheel != null)
            {
                pivot.localRotation = Quaternion.Euler(0f, -24f, 0f);
                frontWheel.localRotation = Quaternion.Euler(70f, -24f, 0f);
                rearWheel.localRotation = Quaternion.Euler(115f, 0f, 0f);
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var groundMaterial = new Material(shader);
            if (groundMaterial.HasProperty("_BaseColor")) groundMaterial.SetColor("_BaseColor", new Color(0.16f, 0.18f, 0.2f));
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "PreviewGround";
            ground.transform.position = new Vector3(0f, 0f, 0f);
            ground.transform.localScale = new Vector3(2f, 1f, 2f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.57f, 0.62f);
            var lightObject = new GameObject("PreviewKeyLight", typeof(Light));
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.shadows = LightShadows.None;
            lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

            CaptureCamera(bike, new Vector3(2.9f, 1.65f, -3.25f), new Vector3(0f, 0.72f, 0f), $"TMAX-MINI124-{label}-three-quarter.png");
            CaptureCamera(bike, new Vector3(2.25f, 1.15f, 2.85f), new Vector3(0f, 0.55f, 0.62f), $"TMAX-MINI124-{label}-front-steering.png");

            UnityEngine.Object.DestroyImmediate(groundMaterial);
            Debug.Log($"MINI-124 {label} evidence captured in {EvidenceDirectory}.");
        }

        private static void CaptureCamera(GameObject bike, Vector3 cameraPosition, Vector3 target, string fileName)
        {
            var cameraObject = new GameObject("MINI124_EvidenceCamera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.transform.position = cameraPosition;
            camera.transform.LookAt(target);
            camera.fieldOfView = 38f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.065f, 0.08f);
            camera.farClipPlane = 50f;
            RenderCameraToFile(camera, Path.Combine(EvidenceDirectory, fileName), 1280, 800);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }

        private static void RenderCameraToFile(Camera camera, string outputPath, int width, int height)
        {
            var renderTexture = new RenderTexture(width, height, 24);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(renderTexture);
        }

        private static Transform Require(Transform root, string name)
        {
            Transform found = FindDeep(root, name);
            if (found == null)
                throw new InvalidOperationException($"Required transform '{name}' was not found under '{root.name}'.");
            return found;
        }

        private static Transform FindDirect(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
                if (parent.GetChild(i).name == name) return parent.GetChild(i);
            return null;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
                throw new MissingMethodException(target.GetType().FullName, methodName);
            method.Invoke(target, null);
        }

        private static void AppendPrefab(StringBuilder report, string title, string assetPath)
        {
            report.AppendLine($"===== {title}: {assetPath} =====");
            GameObject root = PrefabUtility.LoadPrefabContents(assetPath);
            if (root == null)
            {
                report.AppendLine("ERROR: prefab could not be loaded.");
                return;
            }

            try
            {
                AppendTransform(report, root.transform, root.transform, 0);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            report.AppendLine();
        }

        private static void AppendTransform(StringBuilder report, Transform root, Transform current, int depth)
        {
            string indent = new string(' ', depth * 2);
            string path = AnimationUtility.CalculateTransformPath(current, root);
            if (string.IsNullOrEmpty(path)) path = "<root>";

            report.Append(indent)
                .Append(path)
                .Append(" | localPos=").Append(Format(current.localPosition))
                .Append(" localEuler=").Append(Format(current.localEulerAngles))
                .Append(" localScale=").Append(Format(current.localScale));

            var meshFilter = current.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Mesh mesh = meshFilter.sharedMesh;
                report.Append(" | Mesh=").Append(mesh.name)
                    .Append(" verts=").Append(mesh.vertexCount)
                    .Append(" meshBounds=").Append(Format(mesh.bounds.size));
            }

            var skinned = current.GetComponent<SkinnedMeshRenderer>();
            if (skinned != null && skinned.sharedMesh != null)
            {
                report.Append(" | SkinnedMesh=").Append(skinned.sharedMesh.name)
                    .Append(" verts=").Append(skinned.sharedMesh.vertexCount);
            }

            var renderer = current.GetComponent<Renderer>();
            if (renderer != null)
            {
                report.Append(" | worldBoundsCenter=").Append(Format(renderer.bounds.center))
                    .Append(" worldBoundsSize=").Append(Format(renderer.bounds.size))
                    .Append(" materials=[");
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    if (i > 0) report.Append(", ");
                    report.Append(renderer.sharedMaterials[i] != null ? renderer.sharedMaterials[i].name : "NULL");
                }
                report.Append(']');
            }

            var wheel = current.GetComponent<WheelCollider>();
            if (wheel != null)
            {
                report.Append(" | WheelCollider radius=").Append(wheel.radius.ToString("0.####"))
                    .Append(" center=").Append(Format(wheel.center));
            }

            report.AppendLine();
            for (int i = 0; i < current.childCount; i++)
                AppendTransform(report, root, current.GetChild(i), depth + 1);
        }

        private static string Format(Vector3 value) =>
            $"({value.x:0.####},{value.y:0.####},{value.z:0.####})";
    }
}
