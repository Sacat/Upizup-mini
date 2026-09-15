using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    public static class Mini169RenderSafehouse
    {
        const string Out = "Logs/Tasks/MINI-169";
        [MenuItem("Up Iz Up Mini/MINI-169/Render Farm Safehouse")]
        public static void Run()
        {
            Directory.CreateDirectory(Out);
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);
            var building = GameObject.Find("FarmSafehouse_Building");
            Vector3 center = building.transform.position;
            // Front faces local +Z, which after the building's 268.66deg Y
            // rotation is this world-space forward direction.
            Vector3 front = building.transform.rotation * Vector3.forward;

            var key = new GameObject("k").AddComponent<Light>();
            key.type = LightType.Directional; key.intensity = 1.6f; key.transform.rotation = Quaternion.Euler(40, -30, 0);
            var fill = new GameObject("f").AddComponent<Light>();
            fill.type = LightType.Directional; fill.intensity = .7f; fill.transform.rotation = Quaternion.Euler(20, 150, 0);
            var camera = new GameObject("cam").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.55f, .65f, .75f);
            camera.nearClipPlane = .02f; camera.farClipPlane = 300; camera.fieldOfView = 55;

            // Exterior: stand out front, look at the doorway.
            Capture(camera, center + front * 7f + Vector3.up * 1.8f, center + Vector3.up * 1.6f, "FarmSafehouse-Exterior-Front");
            // Interior: from outside the door looking in and slightly down,
            // pulled back enough to clear the spawned character standing
            // right at the doorway (Sacat's default spawn point coincides
            // with this door).
            Vector3 insideNearDoor = center + front * 4.2f + Vector3.up * 2.4f;
            Vector3 bed = center - front * 0.9f + Vector3.up * 0.6f;
            Capture(camera, insideNearDoor, bed, "FarmSafehouse-Interior-FromDoor");
            // Outside-to-inside 3/4 angle, showing both the doorway gap and the room behind it.
            Vector3 side = building.transform.rotation * Vector3.right;
            Capture(camera, center + front * 5f + side * 4f + Vector3.up * 2.2f, center + Vector3.up * 1.4f, "FarmSafehouse-ThreeQuarter");

            Object.DestroyImmediate(camera.gameObject); Object.DestroyImmediate(key.gameObject); Object.DestroyImmediate(fill.gameObject);
            Debug.Log("MINI169_RENDER_PASS");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void Capture(Camera camera, Vector3 pos, Vector3 target, string name)
        {
            camera.transform.position = pos; camera.transform.LookAt(target);
            var rt = new RenderTexture(1000, 750, 24) { antiAliasing = 4 };
            var old = RenderTexture.active; camera.targetTexture = rt; camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1000, 750, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1000, 750), 0, 0); tex.Apply();
            File.WriteAllBytes($"{Out}/{name}.png", tex.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = old;
            Object.DestroyImmediate(tex); Object.DestroyImmediate(rt);
        }
    }
}
