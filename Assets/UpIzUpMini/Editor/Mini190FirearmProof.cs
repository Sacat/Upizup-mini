using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Combat;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-190 Play Mode proof for pistol handling. Runs the real game (so the Animator IK pass and FirearmPose actually execute), swaps the
    /// character's IPistolUser for a scripted one, and renders fixed-camera frames of: holstered, draw, aimed, recoil peak, reload phases and low
    /// ready for Sacat, Franki and an NPC dummy. Also writes numeric measurements (hand-to-gun distances) to Logs/Tasks/MINI-190.
    /// Pattern matches Mini166PlayModeAccessoryFit (SessionState survives the domain reload). NEVER saves the scene.
    /// </summary>
    [InitializeOnLoad]
    public static class Mini190FirearmProof
    {
        const string Out = "Logs/Tasks/MINI-190";
        const string Scene = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string VisualPrefab = "Assets/UpIzUpMini/Prefabs/Weapons/Mini190_SidearmVisual.prefab";
        static Mini190FirearmProof() { if (SessionState.GetBool("Mini190Proof", false)) EditorApplication.playModeStateChanged += Changed; }

        class ScriptedUser : IPistolUser
        {
            public bool aiming, lowReady, reloading; public float progress; public Vector3 dir = Vector3.forward; public Transform root;
            public bool IsAiming { get { return aiming; } }
            public bool IsLowReady { get { return lowReady; } }
            public bool IsReloading { get { return reloading; } }
            public float ReloadProgress { get { return progress; } }
            public Vector3 AimDirection { get { return dir; } }
            public Transform WeaponRoot { get { return root; } }
        }

        [MenuItem("Up Iz Up Mini/MINI-190/Pistol Handling Proof (Play Mode)")]
        public static void Run()
        {
            Directory.CreateDirectory(Out + "/Renders");
            EnsureVisualPrefab();
            SessionState.SetBool("Mini190Proof", true);
            EditorSceneManager.OpenScene(Scene);
            EditorApplication.playModeStateChanged += Changed;
            EditorApplication.EnterPlaymode();
        }

        public static void EnsureVisualPrefab()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(VisualPrefab));
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UpIzUpMini/Art/Weapons/Mini186/LalayTool_3000tri.fbx");
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/UpIzUpMini/Art/Weapons/Mini186/LalayTool_Modern.mat");
            if (model == null) throw new Exception("Lalay Tool model missing");
            var root = new GameObject("LalayTool_Visual");
            var m = (GameObject)PrefabUtility.InstantiatePrefab(model); m.transform.SetParent(root.transform, false);
            foreach (var r in m.GetComponentsInChildren<Renderer>()) r.sharedMaterial = material;
            void Anchor(string n, Vector3 p) { var a = new GameObject(n).transform; a.SetParent(root.transform, false); a.localPosition = p; }
            Anchor("GripAnchor", new Vector3(0f, -.035f, -.045f)); Anchor("BackstrapAnchor", new Vector3(0f, -.005f, -.075f));
            Anchor("ShotOrigin", new Vector3(0f, .012f, .095f)); Anchor("SupportAnchor", new Vector3(-.022f, -.048f, -.012f));
            PrefabUtility.SaveAsPrefabAsset(root, VisualPrefab); UnityEngine.Object.DestroyImmediate(root); AssetDatabase.SaveAssets();
        }

        static void Changed(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode) { start = EditorApplication.timeSinceStartup + 3f; EditorApplication.update += Tick; }
            if (state == PlayModeStateChange.EnteredEditMode) { SessionState.SetBool("Mini190Proof", false); EditorApplication.playModeStateChanged -= Changed; if (Application.isBatchMode) EditorApplication.Exit(0); }
        }

        static double start; static int step = -1; static double stepAt; static List<Action> script; static List<float> waits;
        static ScriptedUser user; static FirearmPose pose; static Animator anim; static Camera cam; static RenderTexture rt; static string tag; static List<string> log = new List<string>();
        static List<(string name, GameObject go)> subjects = new List<(string, GameObject)>(); static int subjectIndex;

        static void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now < start) return;
            try
            {
                if (script == null) { Begin(); return; }
                if (step >= script.Count) { Finish(); return; }
                if (step >= 0 && now < stepAt) return;
                step++;
                if (step >= script.Count) { Finish(); return; }
                script[step](); stepAt = now + waits[step];
            }
            catch (Exception e) { Debug.LogException(e); log.Add("EXCEPTION " + e.Message); Finish(); }
        }

        static void Begin()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefab);
            foreach (var name in new[] { "Sacat", "Franki" })
            {
                var pc = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(p => p.name == name);
                if (pc == null) { log.Add("missing " + name); continue; }
                if (!pc.gameObject.activeInHierarchy) pc.gameObject.SetActive(true);
                pc.IsControlled = false;   // freeze input; the scripted user drives the pose
                subjects.Add((name, pc.gameObject));
            }
            // NPC dummy: the first town NPC with a humanoid animator
            var npc = UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(a => a.isHuman && a.GetComponentInParent<PlayerController>() == null && a.transform.root.name != "Sacat" && a.GetComponentInParent<UpIzUpMini.Interaction.TownNPCInteractable>() != null);
            if (npc != null)
            {
                var host = npc.GetComponentInParent<UpIzUpMini.Interaction.TownNPCInteractable>().gameObject;
                var u = host.GetComponent<NpcPistolUser>() ?? host.AddComponent<NpcPistolUser>(); u.Configure(prefab);
                subjects.Add(("NPC_" + host.name, host));
            }
            cam = new GameObject("Mini190Cam").AddComponent<Camera>(); cam.gameObject.tag = "MainCamera"; cam.fieldOfView = 38; cam.nearClipPlane = .05f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.6f, .72f, .85f);
            foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) if (c != cam) c.enabled = false;
            rt = new RenderTexture(900, 900, 24); cam.targetTexture = rt;
            script = new List<Action>(); waits = new List<float>();
            foreach (var s in subjects) AddSubject(s.name, s.go, prefab);
            script.Add(() => { }); waits.Add(.2f);
            Debug.Log("MINI190 proof subjects: " + string.Join(", ", subjects.Select(s => s.name)));
        }

        static void Add(Action a, float wait) { script.Add(a); waits.Add(wait); }

        static void AddSubject(string name, GameObject go, GameObject prefab)
        {
            Add(() =>
            {
                tag = name; anim = go.GetComponentInChildren<Animator>(true); pose = anim.GetComponent<FirearmPose>() ?? anim.gameObject.AddComponent<FirearmPose>();
                var fc = go.GetComponent<FirearmController>(); Transform weapon;
                if (fc != null) weapon = go.transform.Find("Mini183_Sidearm");
                else
                {
                    var npcUser = go.GetComponent<NpcPistolUser>(); weapon = npcUser.WeaponRoot;
                }
                user = new ScriptedUser { root = weapon, dir = Flat(anim.transform.forward) };
                pose.User = user;
                int fingers = 0; foreach (HumanBodyBones b in System.Enum.GetValues(typeof(HumanBodyBones))) if (b >= HumanBodyBones.LeftThumbProximal && b <= HumanBodyBones.RightLittleDistal && anim.GetBoneTransform(b) != null) fingers++;
                log.Add($"{name}: animator avatar={(anim.avatar != null ? anim.avatar.name : "none")} weaponRoot={(weapon != null ? weapon.name : "NONE")} fingerBonesMapped={fingers} scale={anim.transform.lossyScale} animForward={anim.transform.forward} rootForward={go.transform.forward}");
            }, .6f);
            Add(() => Snap("holstered"), .4f);
            Add(() => { user.aiming = true; }, .10f);
            Add(() => Snap("draw_mid"), .60f);
            Add(() => Snap("aim"), .05f);
            Add(() => { pose.NotifyShot(); }, .075f);
            Add(() => Snap("recoil_peak"), .6f);
            Add(() => { user.aiming = false; user.lowReady = true; }, .55f);
            Add(() => Snap("low_ready"), .1f);
            Add(() => { user.lowReady = false; user.aiming = true; }, .6f);
            foreach (var p in new[] { .12f, .3f, .5f, .7f, .84f, .96f })
            {
                float pp = p;
                Add(() => { user.reloading = true; user.progress = pp; }, .22f);
                Add(() => Snap("reload_" + Mathf.RoundToInt(pp * 100)), .05f);
            }
            Add(() => { user.reloading = false; user.progress = 0; }, .5f);
            Add(() => { Measure(); user.aiming = false; }, .6f);
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v.sqrMagnitude < 1e-4f ? Vector3.forward : v.normalized; }

        static void Measure()
        {
            var weapon = user.root; if (weapon == null || anim == null) return;
            var hand = anim.GetBoneTransform(HumanBodyBones.RightHand); var left = anim.GetBoneTransform(HumanBodyBones.LeftHand);
            var grip = weapon.Find("LalayTool_Visual/GripAnchor"); var support = weapon.Find("LalayTool_Visual/SupportAnchor");
            var mid = anim.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate); var lmid = anim.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate);
            log.Add($"{tag}: aimBlend={pose.AimBlend:F2} rightMiddle-to-grip={(mid != null && grip != null ? Vector3.Distance(mid.position, grip.position) * 1000f : -1):F0}mm leftMiddle-to-supportAnchor={(lmid != null && support != null ? Vector3.Distance(lmid.position, support.position) * 1000f : -1):F0}mm leftWrist-to-supportAnchor={(left != null && support != null ? Vector3.Distance(left.position, support.position) * 1000f : -1):F0}mm");
        }

        static void Snap(string label)
        {
            if (anim == null) return;
            log.Add($"{tag} {label}: gunDir={pose.GunDirection} aimBlend={pose.AimBlend:F2} aim={user.aiming} low={user.lowReady}");
            var chest = anim.GetBoneTransform(HumanBodyBones.RightHand); Vector3 c = anim.transform.position + Vector3.up * 1.32f;
            Vector3 f = Flat(anim.transform.forward), r = Vector3.Cross(Vector3.up, f);
            var views = new[] { ("front34", c + f * 1.9f + r * 1.1f + Vector3.up * .15f), ("side", c + r * 2.2f + f * .2f), ("back34", c - f * 1.6f - r * .9f + Vector3.up * .3f) };
            var w = user != null && user.root != null ? user.root.position : c;
            var list = new List<(string, Vector3, Vector3)> { (views[0].Item1, views[0].Item2, c + f * .25f + r * -.05f), (views[1].Item1, views[1].Item2, c + f * .25f + r * -.05f), (views[2].Item1, views[2].Item2, c + f * .25f + r * -.05f),
                ("handsR", w + r * .55f + f * .1f + Vector3.up * .12f, w), ("handsL", w - r * .5f + f * .15f + Vector3.up * .1f, w), ("handsFront", w + f * .75f + r * .25f + Vector3.up * .08f, w) };
            foreach (var (vn, pos, look) in list)
            {
                cam.transform.position = pos; cam.transform.LookAt(look); cam.Render();
                var tex = new Texture2D(900, 900, TextureFormat.RGB24, false); var old = RenderTexture.active; RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 900, 900), 0, 0); tex.Apply(); RenderTexture.active = old;
                File.WriteAllBytes($"{Out}/Renders/{tag}_{label}_{vn}.png", tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
            }
        }

        static void Finish()
        {
            EditorApplication.update -= Tick;
            File.WriteAllLines(Out + "/ProofLog.txt", log);
            foreach (var l in log) Debug.Log("MINI190 " + l);
            Debug.Log("MINI190_PROOF_DONE");
            EditorApplication.ExitPlaymode();
        }
    }
}
