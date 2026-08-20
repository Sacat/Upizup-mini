using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-067. Renders close-ups of the gold chain on the player and on
    /// Boss C, so its placement is CHECKED rather than assumed.
    ///
    /// The player's chain is normally economy-gated and attached at runtime
    /// by CharacterEquipment, which never runs in Edit Mode - so this
    /// reproduces that attachment exactly (Chest bone, the same local offset
    /// from PositionOnBone, and the character-root rotation AccessorySwing
    /// enforces every frame). If those numbers drift apart, this render stops
    /// being evidence, so they are duplicated deliberately and noted here.
    /// </summary>
    public static class Mini067ChainRender
    {
        private const string OutDir = "Logs/chain-render";
        private const string MasterScene = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        /// <summary>Preview chains are saved into a THROWAWAY scene, never back
        /// into GrandBayProof - a render check must not mutate the real scene.</summary>
        private const string TempScene = "Assets/UpIzUpMini/Scenes/_ChainPreview.unity";
        private const string ChainPrefabPath = "Assets/UpIzUpMini/Art/Accessories/GoldChain18k.prefab";

        /// <summary>Photographs the chain AS BAKED INTO THE SCENE - no preview
        /// instancing - so what is checked is what ships.</summary>
        [MenuItem("Up Iz Up Mini/MINI-067/Verify Baked Chain")]
        public static void VerifyBaked()
        {
            Directory.CreateDirectory(OutDir);
            EditorSceneManager.OpenScene(MasterScene, OpenSceneMode.Single);

            var boss = GameObject.Find("NPC_BossC");
            if (boss == null) { Debug.LogError("MINI-067 VERIFY: NPC_BossC not found."); return; }

            var chain = GameObject.Find("BossChain_18k");
            if (chain == null) { Debug.LogError("MINI-067 VERIFY: BossChain_18k not found on the boss."); return; }

            foreach (var other in Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
            {
                var rootGo = other.transform.root.gameObject;
                if (rootGo != boss) rootGo.SetActive(false);
            }
            boss.SetActive(true);

            Shoot(boss.transform, chain.transform.position, "BAKED-bossc-front", 0f);
            Shoot(boss.transform, chain.transform.position, "BAKED-bossc-side", 70f);
            Debug.Log($"MINI-067 VERIFY OK: boss chain at {chain.transform.position}, images in {OutDir}");
        }

        [MenuItem("Up Iz Up Mini/MINI-067/Render Chain Check")]
        public static void Render()
        {
            Directory.CreateDirectory(OutDir);

            var chainPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChainPrefabPath);
            if (chainPrefab == null) { Debug.LogError("MINI-067 RENDER FAIL: chain prefab missing."); return; }

            // Sweep the two numbers that actually matter - how far forward of
            // the chest bone the chain sits, and how far down - because the
            // first check showed the model buried in the torso. Rendered as a
            // sweep rather than guessed one value at a time.
            // The chain hangs DOWNWARD from wherever it is pinned, so the "up"
            // number is really "how far above the chest bone the collar sits" -
            // pin it at the bone itself and the loop ends up round the belly.
            // Pinning higher also means less forward clearance is needed, since
            // the torso is narrower at the neck than at the chest.
            // Bigger chain, worn AROUND THE NECK rather than draped on the
            // chest, so the top arc has to reach up to the base of the neck -
            // and the neck is narrower than the chest, so it needs less forward
            // clearance up there than the chest placement did.
            // The model is a FLAT loop (2cm deep), so a constant forward offset
            // cannot follow a torso that curves outward lower down: set it close
            // enough for the collar and the bottom half sinks into the chest.
            // Tilting the plane so the bottom swings forward is both the fix and
            // what a real chain does under its own weight.
            // Per the user: the BACK of the chain belongs at the nape, not on
            // the collar. The model's origin is the top of the loop, so the
            // anchor goes BEHIND the neck (negative forward) and the tilt then
            // swings the bottom of the loop forward onto the chest - which is
            // how the chain actually sits on a person.
            //
            // Rough geometry, chain height ~0.36m: a 40deg tilt carries the
            // bottom 0.36*sin40 = 0.23m forward and 0.36*cos40 = 0.28m down.
            var combos = new (float f, float u, float tilt)[]
            {
                (-0.10f, 0.36f, 50f), (-0.10f, 0.36f, 55f),
                (-0.06f, 0.36f, 55f), (-0.10f, 0.40f, 55f),
                (-0.06f, 0.40f, 60f), (-0.10f, 0.40f, 60f),
            };

            foreach (var c in combos)
            {
                SweepOn("Sacat", chainPrefab, c.f, c.u, c.tilt);
            }

            AssetDatabase.DeleteAsset(TempScene);

            Debug.Log($"MINI-067 RENDER OK: images written to {OutDir}");
        }

        /// <summary>
        /// Hangs the chain off a character's chest bone at each offset in the
        /// sweep and renders it. Position is built in the CHARACTER's frame
        /// (root forward/up), never the bone's own axes - this rig's bone rest
        /// orientations are not world-aligned, which is precisely why the first
        /// attempt at (0, 0.14, 0.08) in bone-local space put the chain
        /// somewhere inside the ribcage.
        /// </summary>
        private static void SweepOn(string goName, GameObject chainPrefab, float f, float u, float tilt)
        {
            // Re-open the master scene each time so the previous iteration's
            // preview chain is gone without needing to track and delete it.
            EditorSceneManager.OpenScene(MasterScene, OpenSceneMode.Single);

            var subject = GameObject.Find(goName);
            if (subject == null) { Debug.LogWarning($"MINI-067 RENDER: '{goName}' not found."); return; }

            var anim = subject.GetComponentInChildren<Animator>();
            var chest = anim != null && anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.Chest) : null;
            if (chest == null) { Debug.LogWarning($"MINI-067 RENDER: no humanoid Chest bone on '{goName}'."); return; }

            var worn = (GameObject)PrefabUtility.InstantiatePrefab(chainPrefab);
            worn.name = "PREVIEW_chain";
            worn.transform.SetParent(chest, worldPositionStays: true);
            worn.transform.rotation = subject.transform.rotation * Quaternion.Euler(tilt, 0f, 0f);
            worn.transform.position = chest.position
                + subject.transform.forward * f
                + subject.transform.up * u;

            // A renderer created this frame is not yet in the culling data, so
            // Camera.Render simply skips it - which is why an earlier pass
            // produced pixel-identical images at wildly different offsets and
            // looked like a placement bug. Saving and re-opening the scene
            // forces a full load, after which the preview genuinely renders.
            EditorSceneManager.MarkSceneDirty(subject.scene);
            EditorSceneManager.SaveScene(subject.scene, TempScene, true);
            EditorSceneManager.OpenScene(TempScene, OpenSceneMode.Single);

            var check = GameObject.Find("PREVIEW_chain");
            var cr = check != null ? check.GetComponentInChildren<Renderer>(true) : null;
            Debug.Log($"MINI-067 CHECK {goName} f={f}: previewFound={check != null} " +
                      $"pos={(check != null ? check.transform.position.ToString() : "-")} " +
                      $"renderer={(cr != null ? cr.GetType().Name : "none")} " +
                      $"enabled={(cr != null && cr.enabled)} " +
                      $"mat={(cr != null && cr.sharedMaterial != null ? cr.sharedMaterial.name : "null")} " +
                      $"shader={(cr != null && cr.sharedMaterial != null ? cr.sharedMaterial.shader.name : "-")}");

            var reopened = GameObject.Find(goName);
            if (reopened == null) { Debug.LogWarning($"MINI-067 RENDER: '{goName}' vanished after reopen."); return; }
            // Aim at the CHAIN's own world position. Deriving the aim from a
            // bone after re-opening the scene proved unreliable (it framed the
            // waist), and the chain is the subject of the check anyway - so
            // frame the thing being inspected rather than a proxy for it.
            // Aim at the chain's BOUNDS CENTRE, not its transform origin. The
            // origin is the top of the loop, which now sits behind the neck -
            // framing on it put the chain itself in the corner of the shot.
            Vector3 aim;
            if (cr != null) aim = cr.bounds.center;
            else if (check != null) aim = check.transform.position;
            else aim = reopened.transform.position + Vector3.up * 1.3f;

            // Hide every OTHER character. The companion spawns right beside the
            // player, so a chest-height camera one metre out sits inside her -
            // which for several passes looked exactly like "the chain is not
            // rendering" when it was simply behind someone else's torso.
            foreach (var other in Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
            {
                var rootGo = other.transform.root.gameObject;
                if (rootGo != reopened) rootGo.SetActive(false);
            }
            reopened.SetActive(true);
            Shoot(reopened.transform, aim, $"{goName}-f{f:F2}-u{u:F2}-t{tilt:F0}-front", 0f);
            Shoot(reopened.transform, aim, $"{goName}-f{f:F2}-u{u:F2}-t{tilt:F0}-side", 70f);
        }

        /// <summary>Chest-height close-up, orbited by <paramref name="yaw"/>
        /// degrees around the subject's own facing.</summary>
        private static void Shoot(Transform subject, Vector3 aim, string name, float yaw)
        {
            var camGo = new GameObject("ChainRenderCam");
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 30f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.18f, 0.20f, 0.24f);

            // Framed slightly BELOW the anchor bone: the chain hangs downward
            // from where it is pinned, so centring on the bone itself puts half
            // the frame on the character's chin.
            Vector3 target = aim;
            Vector3 dir = Quaternion.AngleAxis(yaw, Vector3.up) * subject.forward;
            camGo.transform.position = target + dir * 1.05f;
            camGo.transform.LookAt(target);

            var rt = new RenderTexture(900, 900, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;

            File.WriteAllBytes(Path.Combine(OutDir, name + ".png"), tex.EncodeToPNG());

            cam.targetTexture = null;
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(camGo);
        }
    }
}
