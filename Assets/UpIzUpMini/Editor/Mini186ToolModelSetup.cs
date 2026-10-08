using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UpIzUpMini.Character;
using UpIzUpMini.Combat;

namespace UpIzUpMini.EditorTools
{
    /// <summary>Targeted visual replacement for MINI-186. Never runs a world builder.</summary>
    public static class Mini186ToolModelSetup
    {
        const string ScenePath = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string ModelPath = "Assets/UpIzUpMini/Art/Weapons/Mini186/LalayTool_3000tri.fbx";
        const string TexturePath = "Assets/UpIzUpMini/Art/Weapons/Mini186/LalayTool_1024.png";
        const string MaterialPath = "Assets/UpIzUpMini/Art/Weapons/Mini186/LalayTool_Modern.mat";

        [MenuItem("Up Iz Up Mini/MINI-186/Inspect Imported Model")]
        public static void Inspect()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (prefab == null) throw new InvalidOperationException("MINI-186 FBX was not imported.");
            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = filter.sharedMesh;
                Debug.Log($"MINI-186 MODEL: object={filter.name}, localPosition={filter.transform.localPosition}, localEuler={filter.transform.localEulerAngles}, localScale={filter.transform.localScale}, bounds={mesh.bounds}, vertices={mesh.vertexCount}, triangles={mesh.triangles.Length / 3}");
            }
        }

        [MenuItem("Up Iz Up Mini/MINI-186/Apply Modern Tool Model")]
        public static void Apply()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (prefab == null || texture == null) throw new InvalidOperationException("MINI-186 model or texture is missing.");
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "LalayTool_Modern" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.shader = shader;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            var tint = new Color(0.62f, 0.66f, 0.69f, 1f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
            material.SetFloat("_Metallic", 0.08f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.35f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.35f);
            EditorUtility.SetDirty(material);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var players = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (players.Length != 2) throw new InvalidOperationException($"Expected two players, found {players.Length}; scene was not saved.");
            foreach (var player in players)
            {
                var controller = player.GetComponent<FirearmController>();
                var holder = player.transform.Find("Mini183_Sidearm");
                if (controller == null || holder == null) throw new InvalidOperationException($"MINI-183 firearm wiring missing on {player.name}; scene was not saved.");

                foreach (string obsolete in new[] { "Slide", "Grip", "Muzzle", "LalayTool_Visual" })
                {
                    var old = holder.Find(obsolete);
                    if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                }
                var visual = new GameObject("LalayTool_Visual").transform;
                visual.SetParent(holder, false);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                model.transform.SetParent(visual, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
                // The mesh is bbox-centred; explicit child anchors preserve its measured
                // grip and muzzle points independently of the character's wrist bone.
                visual.localPosition = Vector3.zero;
                visual.localRotation = Quaternion.identity;
                visual.localScale = Vector3.one;
                var gripAnchor = new GameObject("GripAnchor").transform;
                gripAnchor.SetParent(visual, false);
                gripAnchor.localPosition = new Vector3(0f, -0.035f, -0.045f);
                var backstrapAnchor = new GameObject("BackstrapAnchor").transform;
                backstrapAnchor.SetParent(visual, false);
                backstrapAnchor.localPosition = new Vector3(0f, -0.005f, -0.075f);
                var shotOrigin = holder.Find("ShotOrigin") ?? new GameObject("ShotOrigin").transform;
                shotOrigin.SetParent(visual, false);
                shotOrigin.localPosition = new Vector3(0f, 0.012f, 0.095f);
                var renderers = visual.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) throw new InvalidOperationException("Imported model has no renderers; scene was not saved.");
                foreach (var renderer in renderers)
                {
                    renderer.sharedMaterial = material;
                    renderer.enabled = false;
                }
                var data = new SerializedObject(controller);
                data.FindProperty("gripAnchor").objectReferenceValue = gripAnchor;
                data.FindProperty("backstrapAnchor").objectReferenceValue = backstrapAnchor;
                data.FindProperty("muzzle").objectReferenceValue = shotOrigin;
                var prop = data.FindProperty("heldWeaponRenderers");
                prop.arraySize = renderers.Length;
                for (int i = 0; i < renderers.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("MINI-186 APPLY PASS: modern Hi3D model replaced placeholder visuals on two players.");
        }

        [MenuItem("Up Iz Up Mini/MINI-186/Verify Modern Tool Model")]
        public static void Verify()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var players = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (players.Length != 2) throw new InvalidOperationException($"MINI-186 expected two players, found {players.Length}.");
            foreach (var player in players)
            {
                var holder = player.transform.Find("Mini183_Sidearm");
                var visual = holder != null ? holder.Find("LalayTool_Visual") : null;
                var controller = player.GetComponent<FirearmController>();
                if (controller == null || visual == null || visual.GetComponentsInChildren<MeshRenderer>(true).Length != 1
                    || holder.Find("Slide") != null || holder.Find("Grip") != null || holder.Find("Muzzle") != null)
                    throw new InvalidOperationException($"MINI-186 visual wiring failed for {player.name}.");
                var filter = visual.GetComponentInChildren<MeshFilter>(true);
                if (filter == null || filter.sharedMesh == null || filter.sharedMesh.triangles.Length / 3 > 3000)
                    throw new InvalidOperationException($"MINI-186 mesh budget failed for {player.name}.");
            }
            Debug.Log("MINI-186 VERIFY PASS: both players have one modern model, no placeholder cubes.");
        }

        [MenuItem("Up Iz Up Mini/MINI-186/Render Held Tool Proof")]
        public static void RenderProof()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var player = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None).First();
            var holder = player.transform.Find("Mini183_Sidearm");
            var animator = player.GetComponentInChildren<Animator>(true);
            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var gameCamera = Camera.main;
            if (holder == null || hand == null || gameCamera == null) throw new InvalidOperationException("Held preview needs firearm, hand and main camera.");
            holder.SetPositionAndRotation(hand.position + gameCamera.transform.forward * 0.025f - Vector3.up * 0.03f,
                Quaternion.LookRotation(gameCamera.transform.forward, Vector3.up));
            foreach (var renderer in holder.GetComponentsInChildren<Renderer>(true)) renderer.enabled = true;

            var cameraObject = new GameObject("MINI-186 Proof Camera");
            var proof = cameraObject.AddComponent<Camera>();
            proof.clearFlags = CameraClearFlags.SolidColor;
            proof.backgroundColor = new Color(0.13f, 0.15f, 0.18f);
            proof.orthographic = true;
            proof.orthographicSize = 0.29f;
            proof.nearClipPlane = 0.01f;
            proof.farClipPlane = 15f;
            var focus = holder.position + holder.forward * 0.09f + Vector3.up * 0.02f;
            cameraObject.transform.position = focus - holder.forward * 0.48f + holder.right * 0.28f + Vector3.up * 0.17f;
            cameraObject.transform.LookAt(focus);
            var lightObject = new GameObject("MINI-186 Proof Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(35f, -35f, 0f);
            var rt = new RenderTexture(1024, 768, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            proof.targetTexture = rt;
            proof.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var image = new Texture2D(1024, 768, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1024, 768), 0, 0);
            image.Apply();
            var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs/Tasks/MINI-186/Unity-Held-Preview.png");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, image.EncodeToPNG());
            RenderTexture.active = previous;
            proof.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(lightObject);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            Debug.Log("MINI-186 RENDER PASS: " + path);
        }
    }
}
