using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Combat;

namespace UpIzUpMini.EditorTools
{
    public static class Mini188HandFitProof
    {
        const string ScenePath = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string ClipPath = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@CombatIdle1H01.fbx";
        const string MaterialPath = "Assets/UpIzUpMini/Art/Weapons/Mini187/LalayTool_CoyoteSand.mat";

        [MenuItem("Up Iz Up Mini/MINI-188/Verify Saved Web Grip")]
        public static void VerifySavedWebGrip()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var clip = AssetDatabase.LoadAllAssetsAtPath(ClipPath).OfType<AnimationClip>()
                .FirstOrDefault(candidate => !candidate.name.StartsWith("__preview", StringComparison.OrdinalIgnoreCase));
            if (clip == null) throw new InvalidOperationException("MINI-188 proof clip missing.");
            var players = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (players.Length != 2) throw new InvalidOperationException($"Expected two players; got {players.Length}.");
            foreach (var player in players)
            {
                var animator = player.GetComponentInChildren<Animator>(true);
                clip.SampleAnimation(animator.gameObject, Mathf.Min(.5f, clip.length * .5f));
                var root = player.transform.Find("Mini183_Sidearm");
                var grip = root.Find("LalayTool_Visual/GripAnchor");
                var backstrap = root.Find("LalayTool_Visual/BackstrapAnchor");
                var muzzle = root.Find("LalayTool_Visual/ShotOrigin");
                var controller = player.GetComponent<FirearmController>();
                var data = new SerializedObject(controller);
                if (data.FindProperty("gripAnchor").objectReferenceValue != grip ||
                    data.FindProperty("backstrapAnchor").objectReferenceValue != backstrap ||
                    data.FindProperty("muzzle").objectReferenceValue != muzzle)
                    throw new InvalidOperationException($"Saved anchor references missing on {player.name}.");
                var wrist = animator.GetBoneTransform(HumanBodyBones.RightHand);
                var middle = animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate);
                var thumb = animator.GetBoneTransform(HumanBodyBones.RightThumbProximal);
                var index = animator.GetBoneTransform(HumanBodyBones.RightIndexProximal);
                FirearmController.PlaceAtPalm(root, grip, wrist, middle, animator.transform.forward);
                Vector3 web = (thumb.position + index.position) * .5f;
                float oldWebGap = Vector3.Distance(web, backstrap.position);
                FirearmController.PlaceAtHand(root, grip, backstrap, wrist, middle, thumb, index, animator.transform.forward);
                float webGap = Vector3.Distance(web, backstrap.position);
                float fingerGap = Vector3.Distance(middle.position, grip.position);
                Vector3 muzzleLocal = root.InverseTransformPoint(muzzle.position);
                if (webGap >= oldWebGap || fingerGap > .015f || Mathf.Abs(muzzleLocal.z - .095f) > .001f)
                    throw new InvalidOperationException($"Reference fit failed on {player.name}: web={webGap:F4}, oldWeb={oldWebGap:F4}, finger={fingerGap:F4}, muzzle={muzzleLocal}.");
                Debug.Log($"MINI-188 VERIFY {player.name}: webGap {oldWebGap:F4}->{webGap:F4}m, fingerGap={fingerGap:F4}m, muzzleLocal={muzzleLocal}");
            }
            Debug.Log("MINI-188 VERIFY PASS: two saved character rigs, three named anchors, improved web proximity and retained finger wrap; scene not saved.");
        }

        [MenuItem("Up Iz Up Mini/MINI-188/Render Web Grip Comparison")]
        public static void RenderComparison()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var clip = AssetDatabase.LoadAllAssetsAtPath(ClipPath).OfType<AnimationClip>()
                .FirstOrDefault(candidate => !candidate.name.StartsWith("__preview", StringComparison.OrdinalIgnoreCase));
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (clip == null || material == null) throw new InvalidOperationException("MINI-188 proof dependencies missing.");
            var output = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs/Tasks/MINI-188");
            Directory.CreateDirectory(output);
            foreach (var player in UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var animator = player.GetComponentInChildren<Animator>(true);
                clip.SampleAnimation(animator.gameObject, Mathf.Min(.5f, clip.length * .5f));
                var wrist = animator.GetBoneTransform(HumanBodyBones.RightHand);
                var middle = animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate);
                var thumb = animator.GetBoneTransform(HumanBodyBones.RightThumbProximal);
                var index = animator.GetBoneTransform(HumanBodyBones.RightIndexProximal);
                if (wrist == null || middle == null || thumb == null || index == null)
                    throw new InvalidOperationException($"Finger mapping missing on {player.name}.");
                var root = player.transform.Find("Mini183_Sidearm");
                var visual = root.Find("LalayTool_Visual");
                var grip = visual.Find("GripAnchor");
                var gun = visual.GetComponentInChildren<MeshRenderer>(true);
                var originalMaterial = gun.sharedMaterial;
                gun.sharedMaterial = material;
                gun.enabled = true;
                var layers = player.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled)
                    .Select(r => r.gameObject).Distinct().ToDictionary(go => go, go => go.layer);
                foreach (var item in layers) item.Key.layer = 30;
                FirearmController.PlaceAtPalm(root, grip, wrist, middle, animator.transform.forward);
                Capture(animator, wrist.position, Path.Combine(output, $"{player.name}-FingerAnchor.png"));
                Vector3 fingerRoot = root.position;

                var backstrap = visual.Find("BackstrapAnchor");
                bool temporaryBackstrap = backstrap == null;
                if (temporaryBackstrap)
                {
                    backstrap = new GameObject("TemporaryBackstrapAnchor").transform;
                    backstrap.SetParent(visual, false);
                    backstrap.localPosition = new Vector3(0f, -.005f, -.075f);
                }
                Vector3 web = (thumb.position + index.position) * .5f;
                Quaternion rotation = Quaternion.LookRotation(animator.transform.forward, Vector3.up);
                Vector3 backstrapLocal = root.InverseTransformPoint(backstrap.position);
                root.SetPositionAndRotation(web - rotation * backstrapLocal, rotation);
                Vector3 webRoot = root.position;
                Debug.Log($"MINI-188 {player.name}: webToBackstrap={Vector3.Distance(web, backstrap.position):F4}m, middleToGrip={Vector3.Distance(middle.position, grip.position):F4}m, webLocal={root.InverseTransformPoint(web)}");
                Capture(animator, wrist.position, Path.Combine(output, $"{player.name}-WebAnchor.png"));
                root.position = Vector3.Lerp(fingerRoot, webRoot, .25f);
                Capture(animator, wrist.position, Path.Combine(output, $"{player.name}-Blend25.png"));
                FirearmController.PlaceAtHand(root, grip, backstrap, wrist, middle, thumb, index, animator.transform.forward);
                float webGap = Vector3.Distance(web, backstrap.position);
                float fingerGap = Vector3.Distance(middle.position, grip.position);
                Debug.Log($"MINI-188 FIT {player.name}: webGap={webGap:F4}m fingerGap={fingerGap:F4}m muzzleLocal={root.InverseTransformPoint(visual.Find("ShotOrigin").position)}");
                Capture(animator, wrist.position, Path.Combine(output, $"{player.name}-ReferenceFit.png"));
                root.position = Vector3.Lerp(fingerRoot, webRoot, .5f);
                Capture(animator, wrist.position, Path.Combine(output, $"{player.name}-Blend50.png"));
                if (temporaryBackstrap) UnityEngine.Object.DestroyImmediate(backstrap.gameObject);
                gun.sharedMaterial = originalMaterial;
                gun.enabled = false;
                foreach (var item in layers) item.Key.layer = item.Value;
            }
            Debug.Log("MINI-188 COMPARISON PASS: current and upper-backstrap web alignments rendered; scene not saved.");
        }

        static void Capture(Animator animator, Vector3 wrist, string file)
        {
            var cameraObject = new GameObject("MINI-188 Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.15f, .17f, .19f);
            camera.cullingMask = 1 << 30;
            camera.orthographic = true;
            camera.orthographicSize = .32f;
            camera.nearClipPlane = .01f;
            camera.farClipPlane = 10f;
            Vector3 focus = wrist + animator.transform.forward * .1f + Vector3.up * .01f;
            cameraObject.transform.position = focus + animator.transform.forward * .75f + animator.transform.right * .55f + Vector3.up * .2f;
            cameraObject.transform.LookAt(focus);
            var lightObject = new GameObject("MINI-188 Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.cullingMask = 1 << 30;
            lightObject.transform.rotation = Quaternion.Euler(35f, -35f, 0f);
            var rt = new RenderTexture(1000, 800, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            camera.targetTexture = rt;
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var image = new Texture2D(1000, 800, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1000, 800), 0, 0);
            image.Apply();
            File.WriteAllBytes(file, image.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(lightObject);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }
}
