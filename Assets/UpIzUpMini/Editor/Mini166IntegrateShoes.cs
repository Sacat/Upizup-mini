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
    /// <summary>MINI-166 Shoes slot, round 1: Mike 90 (white) and Mike 97
    /// (black) for Franki - colour clones of the existing Ch28_Sneakers
    /// mesh, same low-risk pattern as the Pants slot. NOTE: Ch28_Sneakers
    /// shares its material ("Ch28_body") with the skin renderer - cloning
    /// into a NEW Material instance (not mutating the shared asset) keeps
    /// this from recolouring anything else. Real distinct 90-vs-97 panel
    /// geometry is future work, same honest scoping as Pants' Jeans/
    /// Trousers - colour-only this round, documented, not oversold.</summary>
    public static class Mini166IntegrateShoes
    {
        const string Scene = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string Out = "Logs/Tasks/MINI-166";

        [MenuItem("Up Iz Up Mini/MINI-166/Preview Shoes Slot (no save)")]
        public static void Preview() => Run(false);
        [MenuItem("Up Iz Up Mini/MINI-166/Integrate Shoes Slot")]
        public static void Integrate() => Run(true);

        static void Run(bool save)
        {
            Directory.CreateDirectory(Out);
            EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);

            var root = GameObject.Find("Franki");
            var shoesR = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "Ch28_Sneakers");
            var baseMat = shoesR.sharedMaterial;

            var mike90Mat = new Material(baseMat) { name = "Franki_Mike90" };
            mike90Mat.color = new Color(0.93f, 0.93f, 0.93f); // white
            var mike97Mat = new Material(baseMat) { name = "Franki_Mike97" };
            mike97Mat.color = new Color(0.07f, 0.07f, 0.08f); // black

            var mike90Piece = new OutfitPiece { id = "shoes_mike90", label = "Mike 90", slot = OutfitSlot.Shoes, mesh = shoesR.sharedMesh, materials = new[] { mike90Mat }, tintSlots = Array.Empty<int>() };
            var mike97Piece = new OutfitPiece { id = "shoes_mike97", label = "Mike 97", slot = OutfitSlot.Shoes, mesh = shoesR.sharedMesh, materials = new[] { mike97Mat }, tintSlots = Array.Empty<int>() };

            var wardrobe = root.GetComponent<OutfitWardrobe>();
            if (wardrobe == null) wardrobe = root.AddComponent<OutfitWardrobe>();
            var byId = wardrobe.pieces.ToDictionary(p => p.id);
            byId[mike90Piece.id] = mike90Piece; byId[mike97Piece.id] = mike97Piece;
            wardrobe.pieces = byId.Values.ToArray();
            var bindings = wardrobe.bindings.Where(b => b.slot != OutfitSlot.Shoes).ToList();
            bindings.Add(new OutfitBinding { slot = OutfitSlot.Shoes, renderer = shoesR });
            wardrobe.bindings = bindings.ToArray();
            var thisSlotIds = new System.Collections.Generic.HashSet<string> { "shoes_mike90", "shoes_mike97" };
            var defaults = wardrobe.defaults.Where(c => c != null && !thisSlotIds.Contains(c.itemId)).ToList();
            defaults.Add(new OutfitChoice { itemId = "shoes_mike90", colour = 0 }); // white matches the currently shipped default
            wardrobe.defaults = defaults.ToArray();

            Debug.Log($"MINI166SHOES: Franki bound Ch28_Sneakers, pieces=mike90/mike97 ({shoesR.sharedMesh.vertexCount}v, mesh unchanged - colour only)");

            if (save)
            {
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                Debug.Log("MINI166_SHOES_INTEGRATE_PASS saved");
            }
            else
            {
                RenderPreview(root);
                Debug.Log("MINI166_SHOES_PREVIEW_PASS not saved");
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
            Capture(camera, root.transform.position + Vector3.up * 2f, root.transform.position, $"{Out}/_warmup6.png");

            var footR = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "Ch28_Sneakers");

            wardrobe.Select("shoes_mike90", 0);
            var b90 = footR.bounds;
            Capture(camera, b90.center + new Vector3(0.4f, 0.15f, -0.4f), b90.center, $"{Out}/Franki-Mike90-Feet.png");
            wardrobe.Select("shoes_mike97", 0);
            var b97 = footR.bounds;
            Capture(camera, b97.center + new Vector3(0.4f, 0.15f, -0.4f), b97.center, $"{Out}/Franki-Mike97-Feet.png");

            var bodyR = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "Ch28_Body");
            Capture(camera, bodyR.bounds.center + new Vector3(0, 0.1f, -3f), bodyR.bounds.center, $"{Out}/Franki-Mike97-Full.png");

            root.transform.position = originalPos;
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
            UnityEngine.Object.DestroyImmediate(key.gameObject);
            UnityEngine.Object.DestroyImmediate(fill.gameObject);
        }

        static void Sample(Animator a, AnimationClip clip, float t)
        {
            var graph = PlayableGraph.Create("Mini166Shoes");
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
