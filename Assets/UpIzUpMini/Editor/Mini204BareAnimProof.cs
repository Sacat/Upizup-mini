using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-204 Play Mode proof: the imported bare bodies driven by Sacat's real Animator controller (idle, walk, run, a punch), with FirearmPose pistol aim. Renders to Logs/Tasks/MINI-204/Anim. Run without -quit.</summary>
    [InitializeOnLoad]
    public static class Mini204BareAnimProof
    {
        const string Out = "Logs/Tasks/MINI-204/Anim";
        static Mini204BareAnimProof() { if (SessionState.GetBool("Mini204Anim", false)) EditorApplication.playModeStateChanged += Changed; }

        [MenuItem("Up Iz Up Mini/MINI-204/Bare Body Animation Proof (Play Mode)")]
        public static void Run()
        {
            Directory.CreateDirectory(Out); SessionState.SetBool("Mini204Anim", true);
            // controller from the real Sacat
            var scene = EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity", OpenSceneMode.Single);
            var sacat = GameObject.Find("Sacat"); var anim = sacat.GetComponentsInChildren<Animator>(true).First(a => a.runtimeAnimatorController != null);
            SessionState.SetString("Mini204Ctrl", AssetDatabase.GetAssetPath(anim.runtimeAnimatorController));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorApplication.playModeStateChanged += Changed; EditorApplication.EnterPlaymode();
        }

        static void Changed(PlayModeStateChange s)
        {
            if (s == PlayModeStateChange.EnteredPlayMode) { Setup(); EditorApplication.update += Tick; }
            if (s == PlayModeStateChange.EnteredEditMode) { SessionState.SetBool("Mini204Anim", false); EditorApplication.playModeStateChanged -= Changed; if (Application.isBatchMode) EditorApplication.Exit(0); }
        }

        static GameObject[] actors; static Animator[] anims; static Camera cam; static double t0; static int phase; static string log = "";
        static void Setup()
        {
            var light = new GameObject("L").AddComponent<Light>(); light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(40, 30, 0);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.transform.position = new Vector3(0, -.5f, 0); floor.transform.localScale = new Vector3(40, 1, 40);
            var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SessionState.GetString("Mini204Ctrl", ""));
            actors = new GameObject[2]; anims = new Animator[2]; string[] names = { "SacatBare", "FrankiBare" };
            for (int i = 0; i < 2; i++)
            {
                actors[i] = (GameObject)UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UpIzUpMini/Art/Characters/Modular/Bare/" + names[i] + ".prefab"), new Vector3(i * 1.6f - .8f, 0, 0), Quaternion.identity);
                anims[i] = actors[i].GetComponent<Animator>() ?? actors[i].GetComponentInChildren<Animator>(); anims[i].runtimeAnimatorController = ctrl; anims[i].applyRootMotion = false;
                log += names[i] + " animator=" + (anims[i] != null) + " human=" + (anims[i].avatar != null && anims[i].avatar.isHuman) + "\n";
            }
            cam = new GameObject("cam").AddComponent<Camera>(); cam.fieldOfView = 30; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.6f, .72f, .85f);
            cam.transform.position = new Vector3(0, 1.0f, 5.6f); cam.transform.LookAt(new Vector3(0, .95f, 0)); t0 = EditorApplication.timeSinceStartup;
        }

        static void Shot(string name)
        {
            var rt = new RenderTexture(1100, 800, 24); cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
            var tx = new Texture2D(1100, 800, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1100, 800), 0, 0); tx.Apply();
            File.WriteAllBytes(Out + "/" + name + ".png", tx.EncodeToPNG()); RenderTexture.active = null; cam.targetTexture = null; UnityEngine.Object.Destroy(rt);
        }

        static void SetSpeed(float v, float motion) { foreach (var a in anims) { a.SetFloat("Speed", v); a.SetFloat("MotionSpeed", motion); a.SetBool("Grounded", true); } }

        static void Tick()
        {
            double t = EditorApplication.timeSinceStartup - t0;
            if (phase == 0 && t > 1.5) { Shot("1_idle"); SetSpeed(2f, 1f); phase = 1; }
            else if (phase == 1 && t > 3.2) { Shot("2_walk"); SetSpeed(5.5f, 1f); phase = 2; }
            else if (phase == 2 && t > 4.9) { Shot("3_run"); phase = 3; }
            else if (phase == 3 && t > 5.2) { Shot("4_run_b"); File.WriteAllText(Out + "/log.txt", log); phase = 4; EditorApplication.update -= Tick; Debug.Log("MINI204_ANIM_DONE"); EditorApplication.ExitPlaymode(); }
        }
    }
}
