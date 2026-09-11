using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UpIzUpMini.Character;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-166 Pants slot, round 1: Jeans (indigo) and Trousers
    /// (charcoal) for Franki - pure colour clones of the existing, already
    /// motion-proven Ch28_Pants mesh, zero new geometry, zero Blender/FBX
    /// step. Denim Shorts deliberately NOT attempted this round - real
    /// lower-leg skin coverage under a knee-length hem was not conclusively
    /// verified (see MINI-166.md), and this project does not ship geometry
    /// it hasn't actually looked at. Sacat's Pants slot also NOT wired this
    /// round - his pants are a material region on the fused Ch06 mesh
    /// (confirmed: real continuous leg-shaped geometry under the current
    /// paint, not flared cloth) and binding OutfitWardrobe's Pants slot to
    /// Ch06 directly would risk disabling his whole body via Select()'s
    /// mesh==null-disables-renderer rule - needs its own careful approach,
    /// not reused blind from Franki's pattern.</summary>
    public static class Mini166IntegratePants
    {
        const string Scene = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string Out = "Logs/Tasks/MINI-166";

        [MenuItem("Up Iz Up Mini/MINI-166/Preview Pants Slot (no save)")]
        public static void Preview() => Run(false);
        [MenuItem("Up Iz Up Mini/MINI-166/Integrate Pants Slot")]
        public static void Integrate() => Run(true);

        static void Run(bool save)
        {
            Directory.CreateDirectory(Out);
            EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);

            var root = GameObject.Find("Franki");
            var pantsR = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "Ch28_Pants");
            var baseMat = pantsR.sharedMaterial;

            var jeansMat = new Material(baseMat) { name = "Franki_Jeans" };
            jeansMat.color = new Color(0.16f, 0.22f, 0.42f); // indigo denim
            var trousersMat = new Material(baseMat) { name = "Franki_Trousers" };
            trousersMat.color = new Color(0.10f, 0.10f, 0.11f); // charcoal - matches the already-approved MINI-157 default

            var jeansPiece = new OutfitPiece { id = "pants_jeans", label = "Jeans", slot = OutfitSlot.Pants, mesh = pantsR.sharedMesh, materials = new[] { jeansMat }, tintSlots = Array.Empty<int>() };
            var trousersPiece = new OutfitPiece { id = "pants_trousers", label = "Trousers", slot = OutfitSlot.Pants, mesh = pantsR.sharedMesh, materials = new[] { trousersMat }, tintSlots = Array.Empty<int>() };

            var wardrobe = root.GetComponent<OutfitWardrobe>();
            if (wardrobe == null) wardrobe = root.AddComponent<OutfitWardrobe>();
            var byId = wardrobe.pieces.ToDictionary(p => p.id);
            byId[jeansPiece.id] = jeansPiece;
            byId[trousersPiece.id] = trousersPiece;
            wardrobe.pieces = byId.Values.ToArray();
            var bindings = wardrobe.bindings.Where(b => b.slot != OutfitSlot.Pants).ToList();
            bindings.Add(new OutfitBinding { slot = OutfitSlot.Pants, renderer = pantsR });
            wardrobe.bindings = bindings.ToArray();
            // defaults is a flat single-choice array in this scaffold today
            // (documented limitation - see MINI-166.md multi-slot defaults
            // note); Shirt's default already occupies it, so append rather
            // than replace only if Shirt isn't already using it.
            var defaults = wardrobe.defaults.ToList();
            defaults.Add(new OutfitChoice { itemId = "pants_trousers", colour = 0 }); // trousers (charcoal) matches the current approved look
            wardrobe.defaults = defaults.ToArray();

            Debug.Log($"MINI166PANTS: Franki bound Ch28_Pants, pieces=jeans/trousers ({pantsR.sharedMesh.vertexCount}v, mesh unchanged - colour only)");

            if (save)
            {
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                Debug.Log("MINI166_PANTS_INTEGRATE_PASS saved");
            }
            else
            {
                RenderPreview(root);
                Debug.Log("MINI166_PANTS_PREVIEW_PASS not saved");
            }

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void RenderPreview(GameObject root)
        {
            var wardrobe = root.GetComponent<OutfitWardrobe>();

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.62f, .67f, .75f);
            RenderSettings.ambientEquatorColor = new Color(.35f, .39f, .45f);
            RenderSettings.ambientGroundColor = new Color(.22f, .20f, .18f);
            RenderSettings.fog = false;
            var key = new GameObject("k").AddComponent<Light>();
            key.type = LightType.Directional; key.intensity = 1.7f; key.transform.rotation = Quaternion.Euler(35, -35, 0);
            var fill = new GameObject("f").AddComponent<Light>();
            fill.type = LightType.Directional; fill.intensity = .8f; fill.transform.rotation = Quaternion.Euler(25, 145, 0);
            var camera = new GameObject("cam").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.82f, .82f, .85f);
            camera.nearClipPlane = .02f; camera.farClipPlane = 200; camera.fieldOfView = 32;

            Vector3 originalPos = root.transform.position;
            root.transform.position = new Vector3(500, 300, 0);
            var animator = root.GetComponentInChildren<Animator>(true);
            animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var idle = AssetDatabase.LoadAllAssetsAtPath("Assets/UpIzUpMini/Art/Animations/Stand--Idle.anim.fbx").OfType<AnimationClip>().First(c => !c.name.StartsWith("__"));
            Sample(animator, idle, idle.length * 0.4f);
            Capture(camera, root.transform.position + Vector3.up * 2f, root.transform.position, $"{Out}/_warmup4.png");

            var bodyR = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "Ch28_Body");
            var b = bodyR.bounds;

            wardrobe.Select("pants_jeans", 0);
            Capture(camera, b.center + new Vector3(0, 0.1f, -3f), b.center, $"{Out}/Franki-Jeans-Full.png");
            wardrobe.Select("pants_trousers", 0);
            Capture(camera, b.center + new Vector3(0, 0.1f, -3f), b.center, $"{Out}/Franki-Trousers-Full.png");

            root.transform.position = originalPos;
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
            UnityEngine.Object.DestroyImmediate(key.gameObject);
            UnityEngine.Object.DestroyImmediate(fill.gameObject);
        }

        static void Sample(Animator a, AnimationClip clip, float t)
        {
            var graph = PlayableGraph.Create("Mini166Pants");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var p = AnimationClipPlayable.Create(graph, clip);
            var o = AnimationPlayableOutput.Create(graph, "Pose", a);
            o.SetSourcePlayable(p);
            graph.Play(); p.SetTime(t); graph.Evaluate(0); graph.Destroy();
        }

        static void Capture(Camera camera, Vector3 position, Vector3 target, string path)
        {
            camera.transform.position = position; camera.transform.LookAt(target);
            var rt = new RenderTexture(700, 700, 24) { antiAliasing = 4 };
            var old = RenderTexture.active; camera.targetTexture = rt; camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(700, 700, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 700, 700), 0, 0); tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = old;
            UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
