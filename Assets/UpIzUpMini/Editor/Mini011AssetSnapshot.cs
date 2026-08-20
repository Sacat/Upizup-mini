using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// Renders a camera view of a temporary scene straight to a PNG file
    /// from batch mode, so imported assets can be visually inspected without
    /// depending on a focused desktop window or the Editor Play-mode bug
    /// documented elsewhere in this project. Must be run WITHOUT
    /// -nographics (a real graphics device is required to render).
    /// </summary>
    public static class Mini011AssetSnapshot
    {
        [MenuItem("Up Iz Up Mini/MINI-011/Snapshot Demo City Sample")]
        public static void SnapshotDemoCity()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            light.intensity = 1.2f;

            string[] prefabPaths =
            {
                "Assets/Versatile Studio Assets/Demo City By Versatile Studio/Prefabs/small_house_1.prefab",
                "Assets/Versatile Studio Assets/Demo City By Versatile Studio/Prefabs/small_house_2.prefab",
                "Assets/Versatile Studio Assets/Demo City By Versatile Studio/Prefabs/small_house_3.prefab",
                "Assets/Versatile Studio Assets/Demo City By Versatile Studio/Prefabs/mid_house_1.prefab",
                "Assets/Versatile Studio Assets/Demo City By Versatile Studio/Prefabs/mid_house_2.prefab",
            };

            float x = 0f;
            foreach (var path in prefabPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogWarning($"Snapshot: could not load {path}");
                    continue;
                }
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.position = new Vector3(x, 0f, 0f);
                x += 8f;
            }

            var camGo = new GameObject("SnapshotCamera");
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.position = new Vector3(x / 2f, 8f, -14f);
            camGo.transform.LookAt(new Vector3(x / 2f, 1.5f, 0f));
            cam.fieldOfView = 55f;
            cam.farClipPlane = 200f;

            RenderAndSave(cam, "demo-city-houses.png");
        }

        [MenuItem("Up Iz Up Mini/MINI-011/Snapshot GrandBayProof Scene")]
        public static void SnapshotGrandBayProof()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var mainCamGo = GameObject.FindWithTag("MainCamera");
            if (mainCamGo == null)
            {
                Debug.LogError("Snapshot: no MainCamera found in GrandBayProof.");
                return;
            }
            var cam = mainCamGo.GetComponent<Camera>();

            // The follow camera positions itself relative to the player in
            // LateUpdate/Play mode; in edit mode we approximate that pose
            // once here so the snapshot matches what the player will see.
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                cam.transform.position = player.transform.position + new Vector3(0f, 6.8f, -7f);
                cam.transform.LookAt(player.transform.position + Vector3.up * 1.4f);
            }

            RenderAndSave(cam, "grandbay-proof-overview.png");
        }

        [MenuItem("Up Iz Up Mini/MINI-011/Snapshot GrandBayProof Overview")]
        public static void SnapshotGrandBayProofOverview()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var camGo = new GameObject("OverviewCamera");
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.position = new Vector3(160f, 140f, 30f);
            camGo.transform.LookAt(new Vector3(160f, 0f, 150f));
            cam.fieldOfView = 60f;
            cam.farClipPlane = 600f;

            RenderAndSave(cam, "grandbay-overview-wide.png");
        }

        [MenuItem("Up Iz Up Mini/MINI-011/Snapshot GrandBayProof Mid Overview")]
        public static void SnapshotGrandBayProofMid()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var camGo = new GameObject("MidOverviewCamera");
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.position = new Vector3(115f, 34f, 55f);
            camGo.transform.LookAt(new Vector3(135f, 5f, 95f));
            cam.fieldOfView = 60f;
            cam.farClipPlane = 400f;

            RenderAndSave(cam, "grandbay-overview-mid.png");
        }

        [MenuItem("Up Iz Up Mini/MINI-011/Snapshot Pause Menu")]
        public static void SnapshotPauseMenu()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            // GameObject.Find only searches active objects, and the panel
            // starts inactive - look it up via the canvas instead.
            var canvasEarly = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            var panel = canvasEarly != null ? canvasEarly.transform.Find("PausePanel") : null;
            if (panel != null) panel.gameObject.SetActive(true);
            else Debug.LogWarning("Snapshot: PausePanel not found under canvas.");

            var mainCamGo = GameObject.FindWithTag("MainCamera");
            var player = GameObject.FindWithTag("Player");
            if (mainCamGo != null && player != null)
            {
                mainCamGo.transform.position = player.transform.position + new Vector3(0f, 6.8f, -7f);
                mainCamGo.transform.LookAt(player.transform.position + Vector3.up * 1.4f);
            }

            var cam = mainCamGo != null ? mainCamGo.GetComponent<Camera>() : null;
            if (cam == null)
            {
                Debug.LogError("Snapshot: no MainCamera found.");
                return;
            }

            // Screen Space - Overlay canvases don't get captured by
            // Camera.Render() to a texture (they draw directly to the
            // screen backbuffer, bypassing any camera) - switch to
            // Screen Space - Camera just for this diagnostic snapshot so
            // the UI is actually visible in the rendered PNG. Not saved.
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
            }

            RenderAndSave(cam, "grandbay-pause-menu.png");
        }

        [MenuItem("Up Iz Up Mini/MINI-011/Snapshot Gameplay HUD")]
        public static void SnapshotGameplayHud()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var mainCamGo = GameObject.FindWithTag("MainCamera");
            var player = GameObject.FindWithTag("Player");
            if (mainCamGo == null) { Debug.LogError("Snapshot: no MainCamera."); return; }
            var cam = mainCamGo.GetComponent<Camera>();

            if (player != null)
            {
                mainCamGo.transform.position = player.transform.position + new Vector3(0f, 6.8f, -7f);
                mainCamGo.transform.LookAt(player.transform.position + Vector3.up * 1.4f);
            }

            // Same Overlay-canvas caveat as SnapshotPauseMenu - switch every
            // canvas in the scene to Camera space just for this render.
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
            }

            RenderAndSave(cam, "grandbay-gameplay-hud.png");
        }

        [MenuItem("Up Iz Up Mini/MINI-016/Snapshot Walk Pose")]
        public static void SnapshotWalkPose()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            // Sample mid-stride of the actual walk clip onto the two
            // protagonists. This is the real test of retargeting quality:
            // a bad avatar shows up as bent/twisted legs here.
            var walk = LoadFirstClip("Assets/UpIzUpMini/Art/Animations/Locomotion--Walk_N.anim.fbx");
            if (walk == null) { Debug.LogError("Snapshot: walk clip missing"); return; }

            var franki = GameObject.Find("Franki");
            var sacat = GameObject.Find("Sacat");
            foreach (var go in new[] { franki, sacat })
            {
                if (go == null) continue;
                var animator = go.GetComponentInChildren<Animator>();
                if (animator == null) { Debug.LogWarning($"Snapshot: no animator on {go.name}"); continue; }
                Debug.Log($"Snapshot: {go.name} avatar human={animator.avatar != null && animator.avatar.isHuman}");
                walk.SampleAnimation(animator.gameObject, 0.45f);
            }

            Vector3 target = franki != null ? franki.transform.position : Vector3.zero;

            var camGo = new GameObject("WalkCam");
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.position = target + new Vector3(3.2f, 1.35f, -3.6f);
            camGo.transform.LookAt(target + Vector3.up * 1.0f);
            cam.fieldOfView = 45f;
            cam.farClipPlane = 200f;

            RenderAndSave(cam, "walk-pose.png");
        }

        [MenuItem("Up Iz Up Mini/MINI-014/Snapshot Mission Marker")]
        public static void SnapshotMissionMarker()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var idle = LoadFirstClip("Assets/UpIzUpMini/Art/Animations/Stand--Idle.anim.fbx");
            int posed = 0;
            foreach (var animator in Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (animator.runtimeAnimatorController == null)
                {
                    Debug.LogWarning($"Snapshot: {animator.name} has NO controller.");
                    continue;
                }
                if (idle != null) { idle.SampleAnimation(animator.gameObject, 1.0f); posed++; }
            }
            Debug.Log($"Snapshot: posed {posed} character(s) with the new idle clip.");

            // Place the marker where the first objective with a marker points.
            var marker = GameObject.Find("ObjectiveMarker");
            var farmShop = GameObject.Find("NPC_FarmShop");
            if (marker != null && farmShop != null)
            {
                marker.transform.position = farmShop.transform.position;
            }

            Vector3 target = farmShop != null ? farmShop.transform.position : Vector3.zero;

            var camGo = new GameObject("MarkerCam");
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.position = target + new Vector3(-6f, 5.5f, -11f);
            camGo.transform.LookAt(target + Vector3.up * 1.6f);
            cam.fieldOfView = 55f;
            cam.farClipPlane = 300f;

            // Overlay canvases don't capture to a RenderTexture.
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
            }

            RenderAndSave(cam, "mission-marker.png");
        }

        [MenuItem("Up Iz Up Mini/MINI-012/Snapshot NPC Stance")]
        public static void SnapshotNpcStance()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            // Animators don't evaluate outside Play mode, so the idle clip
            // is sampled onto every character explicitly - otherwise this
            // would render the model's authored T-pose and tell us nothing
            // about whether the animator is actually wired up.
            var idle = LoadFirstClip(
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Idles/HumanM@Idle01.fbx");
            if (idle == null) Debug.LogWarning("Snapshot: idle clip not found.");

            int sampled = 0;
            foreach (var animator in Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (animator.runtimeAnimatorController == null)
                {
                    Debug.LogWarning($"Snapshot: {animator.name} has NO animator controller (would T-pose).");
                    continue;
                }
                if (idle != null)
                {
                    idle.SampleAnimation(animator.gameObject, 0.4f);
                    sampled++;
                }
            }
            Debug.Log($"Snapshot: sampled idle onto {sampled} animator(s).");

            var shop = GameObject.Find("NPC_PoliceShops") ?? GameObject.Find("NPC_Police");
            var buyer = GameObject.Find("NPC_Buyer");
            Vector3 target = shop != null ? shop.transform.position
                : (buyer != null ? buyer.transform.position : Vector3.zero);

            var camGo = new GameObject("NpcCam");
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.position = target + new Vector3(0f, 3.2f, -9f);
            camGo.transform.LookAt(target + Vector3.up * 1.1f);
            cam.fieldOfView = 50f;
            cam.farClipPlane = 300f;

            RenderAndSave(cam, "npc-stance.png");
        }

        private static AnimationClip LoadFirstClip(string fbxPath)
        {
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            {
                if (a is AnimationClip clip && !clip.name.StartsWith("__preview__")) return clip;
            }
            return null;
        }

        [MenuItem("Up Iz Up Mini/MINI-012/Snapshot Farm")]
        public static void SnapshotFarm()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var farm = GameObject.Find("MontineFarm");
            if (farm == null) { Debug.LogError("Snapshot: MontineFarm not found."); return; }

            // Reveal a few plots at different growth stages so the crop
            // visuals can actually be judged.
            var plots = Object.FindObjectsByType<UpIzUpMini.Farming.FarmPlot>(FindObjectsSortMode.None);
            System.Array.Sort(plots, (a, b) => string.CompareOrdinal(a.name, b.name));
            var tomato = AssetDatabase.LoadAssetAtPath<UpIzUpMini.Economy.CropDefinition>(
                "Assets/UpIzUpMini/Data/Crops/tomato.asset");
            for (int i = 0; i < plots.Length; i++)
            {
                int stage = Mathf.Min(3, i / 2);
                plots[i].LoadState(stage >= 3 ? 3 : 2, tomato,
                    tomato != null ? tomato.growDurationSeconds * (stage / 3f) : 0f);
            }

            var camGo = new GameObject("FarmCam");
            var cam = camGo.AddComponent<Camera>();
            Vector3 c = farm.transform.position;
            camGo.transform.position = c + new Vector3(-13f, 9f, -13f);
            camGo.transform.LookAt(c + Vector3.up * 0.5f);
            cam.fieldOfView = 55f;
            cam.farClipPlane = 500f;

            RenderAndSave(cam, "montine-farm.png");
        }

        [MenuItem("Up Iz Up Mini/MINI-050/Snapshot Banana Crop")]
        public static void SnapshotBananaCrop()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            // Find the first plot and force a ripe banana onto it so the
            // MINI-050 per-crop banana-tree visual (not the old tomato
            // fallback) is actually visible in the frame.
            var plots = Object.FindObjectsByType<UpIzUpMini.Farming.FarmPlot>(FindObjectsSortMode.None);
            if (plots.Length == 0) { Debug.LogError("Snapshot: no FarmPlot found."); return; }

            var banana = AssetDatabase.LoadAssetAtPath<UpIzUpMini.Economy.CropDefinition>(
                "Assets/UpIzUpMini/Data/Crops/banana.asset");
            if (banana == null) { Debug.LogError("Snapshot: banana.asset missing."); return; }

            plots[0].LoadState(3, banana, banana.growDurationSeconds);

            var camGo = new GameObject("BananaCam");
            var cam = camGo.AddComponent<Camera>();
            Vector3 c = plots[0].transform.position;
            camGo.transform.position = c + new Vector3(-2.2f, 1.4f, -2.2f);
            camGo.transform.LookAt(c + Vector3.up * 0.4f);
            cam.fieldOfView = 50f;
            cam.farClipPlane = 120f;

            RenderAndSave(cam, "mini050-banana-crop.png");
        }

        [MenuItem("Up Iz Up Mini/MINI-051/Snapshot Weed Bud Closeup")]
        public static void SnapshotWeedBudCloseup()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var plots = Object.FindObjectsByType<UpIzUpMini.Farming.FarmPlot>(FindObjectsSortMode.None);
            if (plots.Length == 0) { Debug.LogError("Snapshot: no FarmPlot found."); return; }

            var purple = AssetDatabase.LoadAssetAtPath<UpIzUpMini.Economy.CropDefinition>(
                "Assets/UpIzUpMini/Data/Crops/purple.asset");
            if (purple == null) { Debug.LogError("Snapshot: purple.asset missing."); return; }

            plots[0].LoadState(3, purple, purple.growDurationSeconds);

            var camGo = new GameObject("WeedBudCam");
            var cam = camGo.AddComponent<Camera>();
            Vector3 c = plots[0].transform.position;
            camGo.transform.position = c + new Vector3(-1.1f, 1.9f, -1.1f);
            camGo.transform.LookAt(c + Vector3.up * 1.4f);
            cam.fieldOfView = 35f;
            cam.farClipPlane = 120f;

            RenderAndSave(cam, "mini051-weed-bud-closeup.png");
        }

        [MenuItem("Up Iz Up Mini/MINI-051/Snapshot Weed Strains Side By Side")]
        public static void SnapshotWeedStrainsSideBySide()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var plots = Object.FindObjectsByType<UpIzUpMini.Farming.FarmPlot>(FindObjectsSortMode.None);
            if (plots.Length < 4) { Debug.LogError("Snapshot: need at least 4 FarmPlots."); return; }

            string[] strainIds = { "bushers", "black_sugar", "purple", "blue_cheese" };
            for (int i = 0; i < strainIds.Length && i < plots.Length; i++)
            {
                var crop = AssetDatabase.LoadAssetAtPath<UpIzUpMini.Economy.CropDefinition>(
                    $"Assets/UpIzUpMini/Data/Crops/{strainIds[i]}.asset");
                if (crop == null) { Debug.LogError($"Snapshot: {strainIds[i]}.asset missing."); continue; }
                plots[i].LoadState(3, crop, crop.growDurationSeconds);
            }

            Vector3 center = Vector3.zero;
            for (int i = 0; i < strainIds.Length && i < plots.Length; i++)
                center += plots[i].transform.position;
            center /= Mathf.Min(strainIds.Length, plots.Length);

            var camGo = new GameObject("WeedStrainsCam");
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.position = center + new Vector3(0f, 6.5f, -8.5f);
            camGo.transform.LookAt(center + Vector3.up * 0.6f);
            cam.fieldOfView = 45f;
            cam.farClipPlane = 200f;

            RenderAndSave(cam, "mini051-weed-strains-side-by-side.png");
        }

        // MINI-055: split into two menu items rather than one method calling
        // RenderAndSave twice - RenderAndSave calls EditorApplication.Exit(0)
        // right after the first save when running in batch mode (by design,
        // so every other one-shot snapshot method here terminates cleanly),
        // so a second call in the same invocation would never actually run.
        [MenuItem("Up Iz Up Mini/MINI-055/Snapshot Boss C Closeup")]
        public static void SnapshotBossCCloseup()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var bossC = GameObject.Find("NPC_BossC");
            if (bossC == null) { Debug.LogError("Snapshot: NPC_BossC not found."); return; }
            Vector3 c = bossC.transform.position;

            // Standing where the boss is facing TOWARD (position + his own
            // forward * distance) and looking back at him shows his front
            // - two earlier attempts (an arbitrary offset, then the sign
            // flipped the wrong way) both rendered the back of his shirt.
            var closeGo = new GameObject("BossCCloseCam");
            var closeCam = closeGo.AddComponent<Camera>();
            closeGo.transform.position = c + bossC.transform.forward * 1.3f + Vector3.up * 1.5f;
            closeGo.transform.LookAt(c + Vector3.up * 1.4f);
            closeCam.fieldOfView = 35f;
            closeCam.farClipPlane = 120f;
            RenderAndSave(closeCam, "mini055-boss-c-closeup.png");
        }

        [MenuItem("Up Iz Up Mini/MINI-055/Snapshot Boss C Wide")]
        public static void SnapshotBossCWide()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var bossC = GameObject.Find("NPC_BossC");
            if (bossC == null) { Debug.LogError("Snapshot: NPC_BossC not found."); return; }
            Vector3 c = bossC.transform.position;

            var wideGo = new GameObject("BossCWideCam");
            var wideCam = wideGo.AddComponent<Camera>();
            wideGo.transform.position = c + new Vector3(-10f, 4.5f, -10f);
            wideGo.transform.LookAt(c + Vector3.up * 0.5f);
            wideCam.fieldOfView = 60f;
            wideCam.farClipPlane = 150f;
            RenderAndSave(wideCam, "mini055-boss-c-wide.png");
        }

        [MenuItem("Up Iz Up Mini/MINI-056/Snapshot Rasta Mentor")]
        public static void SnapshotRastaMentor()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var mentor = GameObject.Find("NPC_RastaMentor");
            if (mentor == null) { Debug.LogError("Snapshot: NPC_RastaMentor not found."); return; }
            Vector3 c = mentor.transform.position;

            var camGo = new GameObject("RastaMentorCam");
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.position = c + mentor.transform.forward * 2.2f + Vector3.up * 1.5f;
            camGo.transform.LookAt(c + Vector3.up * 1.3f);
            cam.fieldOfView = 45f;
            cam.farClipPlane = 120f;

            RenderAndSave(cam, "mini056-rasta-mentor.png");
        }

        [MenuItem("Up Iz Up Mini/MINI-057/Snapshot Real Police Closeup")]
        public static void SnapshotRealPoliceCloseup()
        {
            // Kept (not removed) after MINI-057's police-cap fix - this is
            // the exact view that caught the pre-existing floating-cap bug
            // affecting every officer, not just Normy; worth keeping as a
            // quick visual regression check for ApplyPoliceUniform.
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);
            var officer = GameObject.Find("NPC_Police");
            if (officer == null) { Debug.LogError("Snapshot: NPC_Police not found."); return; }
            Vector3 c = officer.transform.position;
            var camGo = new GameObject("PoliceDebugCam");
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.position = c + officer.transform.forward * 2.2f + Vector3.up * 1.5f;
            camGo.transform.LookAt(c + Vector3.up * 1.3f);
            cam.fieldOfView = 45f;
            cam.farClipPlane = 120f;
            RenderAndSave(cam, "mini057-real-police-closeup.png");
        }

        [MenuItem("Up Iz Up Mini/MINI-057/Snapshot Normy")]
        public static void SnapshotNormy()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var normy = GameObject.Find("NPC_Normy");
            if (normy == null) { Debug.LogError("Snapshot: NPC_Normy not found."); return; }
            Vector3 c = normy.transform.position;

            var camGo = new GameObject("NormyCam");
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.position = c + normy.transform.forward * 2.2f + Vector3.up * 1.5f;
            camGo.transform.LookAt(c + Vector3.up * 1.3f);
            cam.fieldOfView = 45f;
            cam.farClipPlane = 120f;

            RenderAndSave(cam, "mini057-normy.png");
        }

        [MenuItem("Up Iz Up Mini/MINI-058/Snapshot Dog Life")]
        public static void SnapshotDogLife()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var spawnerGo = GameObject.Find("DogLifeSpawner");
            if (spawnerGo == null) { Debug.LogError("Snapshot: DogLifeSpawner not found."); return; }

            // Pool members start inactive (real distance-based pooling) -
            // force one on for the shot the same way crop snapshots force
            // a plant to ripe.
            var poolField = typeof(UpIzUpMini.Interaction.RivalGangSpawner).GetField("_pool",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var pool = poolField?.GetValue(spawnerGo.GetComponent<UpIzUpMini.Interaction.RivalGangSpawner>()) as System.Collections.Generic.List<GameObject>;
            if (pool == null || pool.Count == 0) { Debug.LogError("Snapshot: Dog Life pool is empty."); return; }
            foreach (var member in pool) member.SetActive(true);

            Vector3 c = pool[0].transform.position;
            var camGo = new GameObject("DogLifeCam");
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.position = c + new Vector3(0f, 4f, -6f);
            camGo.transform.LookAt(c + Vector3.up * 1f);
            cam.fieldOfView = 55f;
            cam.farClipPlane = 120f;

            RenderAndSave(cam, "mini058-dog-life.png");
        }

        [MenuItem("Up Iz Up Mini/MINI-058/Snapshot Chevy By Boss C")]
        public static void SnapshotChevyByBossC()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var chevy = GameObject.Find("NotAhWord_Chevy");
            var bossC = GameObject.Find("NPC_BossC");
            if (chevy == null || bossC == null) { Debug.LogError("Snapshot: NotAhWord_Chevy or NPC_BossC not found."); return; }

            Vector3 mid = (chevy.transform.position + bossC.transform.position) * 0.5f;
            var camGo = new GameObject("ChevyBossCCam");
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.position = mid + new Vector3(0f, 4.5f, -8f);
            camGo.transform.LookAt(mid + Vector3.up * 1f);
            cam.fieldOfView = 55f;
            cam.farClipPlane = 120f;

            RenderAndSave(cam, "mini058-chevy-by-bossc.png");
        }

        /// <summary>MINI-060 follow-up-2: confirms the recruiter's
        /// reposition next to Boss C/Chevy (previously overlapping Dog
        /// Life's block) - "bring him to the boss C by chevy area."</summary>
        [MenuItem("Up Iz Up Mini/MINI-060/Snapshot Gang Recruiter By Boss C")]
        public static void SnapshotGangRecruiterByBossC()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var recruiter = GameObject.Find("NPC_GangRecruiter");
            var chevy = GameObject.Find("NotAhWord_Chevy");
            var bossC = GameObject.Find("NPC_BossC");
            if (recruiter == null || chevy == null || bossC == null)
            {
                Debug.LogError("Snapshot: NPC_GangRecruiter, NotAhWord_Chevy, or NPC_BossC not found.");
                return;
            }

            Vector3 mid = (recruiter.transform.position + chevy.transform.position + bossC.transform.position) / 3f;
            var camGo = new GameObject("GangRecruiterBossCCam");
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.position = mid + new Vector3(0f, 9f, -16f);
            camGo.transform.LookAt(mid + Vector3.up * 1f);
            cam.fieldOfView = 60f;
            cam.farClipPlane = 150f;

            RenderAndSave(cam, "mini060-gang-recruiter-by-bossc.png");
        }

        [MenuItem("Up Iz Up Mini/MINI-058/Snapshot Gang Member")]
        public static void SnapshotGangMember()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var recruiter = GameObject.Find("NPC_GangRecruiter")?.GetComponent<UpIzUpMini.Interaction.TownNPCInteractable>();
            var actor = GameObject.Find("Sacat");
            var economy = GameObject.Find("EconomyManager")?.GetComponent<UpIzUpMini.Economy.EconomyManager>();
            if (recruiter == null || actor == null || economy == null)
            {
                Debug.LogError("Snapshot: NPC_GangRecruiter, Sacat, or EconomyManager not found.");
                return;
            }

            // Same as every other validation/snapshot in this project:
            // AddComponent-time scene objects don't run Awake outside Play
            // Mode, so EconomyManager.Instance/Money would otherwise be
            // unset and the recruit silently fails.
            typeof(UpIzUpMini.Economy.EconomyManager).GetMethod("Awake",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(economy, null);
            // MINI-060 follow-up-2: recruitCost raised 150 -> 2000/member.
            economy.AddMoney(2000);

            recruiter.Interact(actor); // recruits the first roster member

            var chevy = GameObject.Find("NotAhWord_Chevy");
            if (chevy == null) { Debug.LogError("Snapshot: NotAhWord_Chevy not found after recruiting."); return; }

            Vector3 c = chevy.transform.position;
            var camGo = new GameObject("GangMemberCam");
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.position = c + new Vector3(0f, 1.6f, -2.5f);
            camGo.transform.LookAt(c + Vector3.up * 1f);
            cam.fieldOfView = 45f;
            cam.farClipPlane = 120f;

            RenderAndSave(cam, "mini058-gang-member.png");
        }

        [MenuItem("Up Iz Up Mini/MINI-060/Snapshot Boat Man")]
        public static void SnapshotGardeyZafeh()
        {
            // MINI-060 follow-up: Gardey Zafeh no longer has her own NPC -
            // the boat man now handles her reveal/readings too (see
            // TownNPCInteractable.InteractGardeyZafeh). Retargeted rather
            // than deleted, so the menu path stays under a stable name.
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var gardey = GameObject.Find("NPC_BoatMan");
            if (gardey == null) { Debug.LogError("Snapshot: NPC_BoatMan not found."); return; }
            Vector3 c = gardey.transform.position;

            var camGo = new GameObject("GardeyZafehCam");
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.position = c + gardey.transform.forward * 2.5f + Vector3.up * 1.5f;
            camGo.transform.LookAt(c + Vector3.up * 1.3f);
            cam.fieldOfView = 50f;
            cam.farClipPlane = 120f;

            RenderAndSave(cam, "mini060-boatman.png");
        }

        [MenuItem("Up Iz Up Mini/MINI-012/Snapshot Coast")]
        public static void SnapshotCoast()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var jetty = GameObject.Find("CoastAndJetty");
            if (jetty == null) { Debug.LogError("Snapshot: CoastAndJetty not found."); return; }

            var deck = GameObject.Find("JettyDeck");
            Vector3 target = deck != null ? deck.transform.position : jetty.transform.position;

            var camGo = new GameObject("CoastCam");
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.position = target + new Vector3(18f, 14f, -26f);
            camGo.transform.LookAt(target);
            cam.fieldOfView = 58f;
            cam.farClipPlane = 500f;

            RenderAndSave(cam, "coast-jetty.png");
        }

        [MenuItem("Up Iz Up Mini/MINI-012/Snapshot Decimated Crops")]
        public static void SnapshotDecimatedCrops()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            light.intensity = 1.3f;

            string[] meshPaths =
            {
                "Assets/UpIzUpMini/Art/CropMeshes/TomatoPlant_LOD.asset",
                "Assets/UpIzUpMini/Art/CropMeshes/WeedPlant_LOD.asset",
            };

            // The source scans are authored at ~2cm tall, so normalize each
            // to a realistic ~1m plant height for inspection.
            const float targetHeight = 1f;
            float x = 0f;
            foreach (var path in meshPaths)
            {
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (mesh == null) { Debug.LogWarning($"Snapshot: missing {path}"); continue; }

                float scale = targetHeight / Mathf.Max(0.0001f, mesh.bounds.size.y);
                var go = new GameObject(mesh.name);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = new Material(Shader.Find("Standard")) { color = new Color(0.3f, 0.55f, 0.25f) };
                go.transform.position = new Vector3(x, 0f, 0f);
                go.transform.localScale = Vector3.one * scale;

                x += 1.2f;
            }

            var camGo = new GameObject("SnapshotCamera");
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.position = new Vector3(x / 2f - 0.6f, 0.6f, -2.2f);
            camGo.transform.LookAt(new Vector3(x / 2f - 0.6f, 0.45f, 0f));
            cam.fieldOfView = 55f;
            cam.farClipPlane = 100f;

            RenderAndSave(cam, "decimated-crops.png");
        }

        [MenuItem("Up Iz Up Mini/MINI-011/Snapshot Shanty Town Sample")]
        public static void SnapshotShantyTown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            light.intensity = 1.2f;

            string[] prefabPaths =
            {
                "Assets/ArteriaShantyTown/ShantyTown1/shanty1.fbx",
                "Assets/ArteriaShantyTown/ShantyTown1/shanty5.fbx",
                "Assets/ArteriaShantyTown/ShantyTown1/shanty10.fbx",
                "Assets/ArteriaShantyTown/ShantyTown2_Buildings/BuildingA/BuildingA.fbx",
                "Assets/ArteriaShantyTown/ShantyTown2_Buildings/BuildingE/BuildingE.fbx",
            };

            float x = 0f;
            foreach (var path in prefabPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogWarning($"Snapshot: could not load {path}");
                    continue;
                }
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.position = new Vector3(x, 0f, 0f);
                x += 8f;
            }

            var camGo = new GameObject("SnapshotCamera");
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.position = new Vector3(x / 2f, 6f, -12f);
            camGo.transform.LookAt(new Vector3(x / 2f, 1.5f, 0f));
            cam.fieldOfView = 60f;
            cam.farClipPlane = 200f;

            RenderAndSave(cam, "shanty-town-houses.png");
        }

        [MenuItem("Up Iz Up Mini/MINI-011/Snapshot Character Pack Sample")]
        public static void SnapshotCharacterPack()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            light.intensity = 1.2f;

            string[] modelPaths =
            {
                "Assets/Floreswa/Models/male01_1.fbx",
                "Assets/Floreswa/Models/male02_1.fbx",
            };

            float x = 0f;
            foreach (var path in modelPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogWarning($"Snapshot: could not load {path}");
                    continue;
                }
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.position = new Vector3(x, 0f, 0f);
                x += 2f;
            }

            var camGo = new GameObject("SnapshotCamera");
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.position = new Vector3(x / 2f, 1.6f, -4f);
            camGo.transform.LookAt(new Vector3(x / 2f, 1f, 0f));
            cam.fieldOfView = 45f;
            cam.farClipPlane = 100f;

            RenderAndSave(cam, "character-pack-sample.png");
        }

        private static void RenderAndSave(Camera cam, string fileName)
        {
            const int width = 1280;
            const int height = 720;

            var rt = new RenderTexture(width, height, 24);
            cam.targetTexture = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);

            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            string outDir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Snapshots");
            Directory.CreateDirectory(outDir);
            string outPath = Path.Combine(outDir, fileName);
            File.WriteAllBytes(outPath, png);

            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);

            Debug.Log($"Snapshot saved: {outPath}");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
    }
}
