using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;

namespace UpIzUpMini.EditorTools
{
    public static class Mini159CheckAccessories
    {
        const string Out = "Logs/Tasks/MINI-159";

        [MenuItem("Up Iz Up Mini/MINI-159/Check Cap+Shades Placement")]
        public static void Run()
        {
            System.IO.Directory.CreateDirectory(Out);
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var key = new GameObject("k").AddComponent<Light>();
            key.type = LightType.Directional; key.intensity = 1.7f; key.transform.rotation = Quaternion.Euler(35, -35, 0);
            var camera = new GameObject("cam").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.82f, .82f, .85f);
            camera.nearClipPlane = .02f; camera.farClipPlane = 200; camera.fieldOfView = 28;

            float offsetX = 0f;
            foreach (var name in new[] { "Sacat", "Franki" })
            {
                var root = GameObject.Find(name);
                if (root == null) continue;
                var eq = root.GetComponentInChildren<CharacterEquipment>(true);
                if (eq == null) { Debug.LogWarning($"MINI159ACC: no CharacterEquipment on {name}"); continue; }

                Vector3 originalPos = root.transform.position;
                root.transform.position = new Vector3(offsetX, 300f, 0f);
                offsetX += 3f;

                eq.SetTrialItem("cap_mike", true);
                eq.SetTrialItem("shades_ray", true);

                var bodyRenderer = root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .FirstOrDefault(r => r.name == "Ch06" || r.name == "Ch28_Body");
                var animator = root.GetComponentInChildren<Animator>(true);
                var head = animator.GetBoneTransform(HumanBodyBones.Head);
                Vector3 focus = head.position;
                Capture(camera, focus + Vector3.forward * -0.6f + Vector3.up * 0.05f, focus, $"{Out}/Acc-{name}-Head-Front.png");
                Capture(camera, focus + Vector3.right * 0.6f + Vector3.up * 0.05f, focus, $"{Out}/Acc-{name}-Head-Side.png");

                eq.SetTrialItem("cap_mike", false);
                eq.SetTrialItem("shades_ray", false);
                root.transform.position = originalPos;
            }

            DestroyImmediate(camera.gameObject);
            DestroyImmediate(key.gameObject);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void Capture(Camera camera, Vector3 position, Vector3 target, string path)
        {
            camera.transform.position = position;
            camera.transform.LookAt(target);
            var rt = new RenderTexture(700, 700, 24) { antiAliasing = 4 };
            var old = RenderTexture.active;
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(700, 700, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 700, 700), 0, 0);
            tex.Apply();
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = old;
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
        }

        static void DestroyImmediate(Object o) => Object.DestroyImmediate(o);
    }
}
