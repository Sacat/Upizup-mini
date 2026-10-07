using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-196 Play Mode renders of Sacat (and Franki) on open ground: original Ch06 mesh vs the wardrobe body, bare and dressed. Never saves.</summary>
    [InitializeOnLoad]
    public static class Mini196SacatPlayRender
    {
        const string Out = "Logs/Tasks/MINI-196/Play";
        static Mini196SacatPlayRender() { if (SessionState.GetBool("Mini196Play", false)) EditorApplication.playModeStateChanged += Changed; }

        [MenuItem("Up Iz Up Mini/MINI-196/Play Render Characters")]
        public static void Run()
        {
            Directory.CreateDirectory(Out); SessionState.SetBool("Mini196Play", true);
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity");
            EditorApplication.playModeStateChanged += Changed; EditorApplication.EnterPlaymode();
        }

        static void Changed(PlayModeStateChange s)
        {
            if (s == PlayModeStateChange.EnteredPlayMode) { start = EditorApplication.timeSinceStartup; step = 0; EditorApplication.update += Tick; }
            if (s == PlayModeStateChange.EnteredEditMode) { SessionState.SetBool("Mini196Play", false); EditorApplication.playModeStateChanged -= Changed; if (Application.isBatchMode) EditorApplication.Exit(0); }
        }

        static double start; static int step; static int frames;
        static readonly string[] Names = { "Sacat", "Franki" };

        static void Tick()
        {
            frames++;
            double t = EditorApplication.timeSinceStartup - start;
            if (t < 5) return;
            if (step == 0)
            {
                // an open spot: the Geneva field slab
                Vector3 spot = new Vector3(270f, 0f, 55f);
                foreach (var n in Names)
                {
                    var go = GameObject.Find(n); if (go == null) continue;
                    RaycastHit hit; float y = Physics.Raycast(spot + Vector3.up * 80f, Vector3.down, out hit, 200f) ? hit.point.y : 6f;
                    var cc = go.GetComponent<CharacterController>(); if (cc != null) cc.enabled = false;
                    go.transform.position = new Vector3(spot.x + (n == "Sacat" ? 0 : 6f), y + .05f, spot.z); go.transform.rotation = Quaternion.identity;
                    if (cc != null) cc.enabled = true;
                    var pc = go.GetComponent<PlayerController>(); if (pc != null) pc.enabled = false;
                }
                step = 1; frames = 0; return;
            }
            if (frames < 20) return;
            if (step == 1) { Capture("cur"); step = 2; frames = 0; return; }
            if (step == 2)
            {
                foreach (var n in Names) SetSet(n, true);
                step = 3; frames = 0; return;
            }
            if (step == 3 && frames > 10) { Capture("orig"); step = 4; frames = 0; return; }
            if (step == 4) { foreach (var n in Names) SetSet(n, false); step = 5; EditorApplication.update -= Tick; Debug.Log("MINI196_PLAY_DONE"); EditorApplication.ExitPlaymode(); }
        }

        static void SetSet(string name, bool original)
        {
            var go = GameObject.Find(name); if (go == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.name;
                bool orig = n.StartsWith("Ch") && r.transform.parent != null && r.transform.parent.name == "Visual";
                bool wardrobe = n.StartsWith("Wardrobe");
                if (orig) r.enabled = original; else if (wardrobe && n != "WardrobeHat") r.enabled = !original;
            }
        }

        static void Capture(string tag)
        {
            foreach (var n in Names)
            {
                var go = GameObject.Find(n); if (go == null) continue;
                var cam = new GameObject("c").AddComponent<Camera>(); cam.fieldOfView = 26; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.6f, .72f, .85f);
                bool zoom = Environment.GetEnvironmentVariable("MINI196_ZOOM") == "1"; Vector3 c = go.transform.position + Vector3.up * (zoom ? 1.2f : .95f);
                string[] views = { "front", "side", "back", "back34" }; Vector3[] dirs = { go.transform.forward, go.transform.right, -go.transform.forward, (-go.transform.forward + go.transform.right).normalized };
                for (int i = 0; i < 4; i++)
                {
                    cam.transform.position = c + dirs[i] * (zoom ? 2.3f : 5.5f); cam.transform.LookAt(c);
                    var rt = new RenderTexture(700, 1000, 24); cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
                    var tx = new Texture2D(700, 1000, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 700, 1000), 0, 0); tx.Apply();
                    File.WriteAllBytes(Out + "/" + n + "_" + tag + "_" + views[i] + ".png", tx.EncodeToPNG()); RenderTexture.active = null; cam.targetTexture = null; UnityEngine.Object.Destroy(rt);
                }
                UnityEngine.Object.Destroy(cam.gameObject);
            }
        }
    }
}
