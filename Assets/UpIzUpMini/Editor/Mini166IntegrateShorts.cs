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
    /// <summary>MINI-166 Denim Shorts, Franki: adds a third Pants piece
    /// bound to the same Ch28_Pants renderer as Jeans/Trousers. Reuses the
    /// exact bone-remap-by-name technique proven for the Shirt slot (this
    /// was the actual bug there, not this pattern itself).</summary>
    public static class Mini166IntegrateShorts
    {
        const string Scene = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string Garments = "Assets/UpIzUpMini/Art/Characters/Garments";
        const string Out = "Logs/Tasks/MINI-166";

        static void Require(bool ok, string msg) { if (!ok) throw new Exception("MINI166SHORTS: " + msg); }

        [MenuItem("Up Iz Up Mini/MINI-166/Preview Shorts (no save)")]
        public static void Preview() => Run(false);
        [MenuItem("Up Iz Up Mini/MINI-166/Integrate Shorts")]
        public static void Integrate() => Run(true);

        static void Run(bool save)
        {
            Directory.CreateDirectory(Out);
            EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);

            var root = GameObject.Find("Franki");
            var animatorForCut = root.GetComponentInChildren<Animator>(true);
            var livePants = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "Ch28_Pants");

            // ABANDONED: re-exporting through Blender/FBX and bone-remapping
            // the result (the same pattern proven for the Shirt slot)
            // produced a mesh that passed EVERY data-level check (bounds,
            // bindposes, boneWeights, submesh/material correspondence,
            // normals, GPU-skinning warmup timing) yet rendered as
            // completely invisible - confirmed NOT a bones/renderer issue
            // since re-selecting Jeans immediately after the same bone
            // remap rendered correctly. Root cause not found despite
            // exhaustive checks (see MINI-166.md). Switched approach
            // entirely: split the ALREADY-WORKING live mesh's own submeshes
            // directly in C#, never touching Blender/FBX for this piece -
            // eliminates that whole class of risk.
            var upLeg = animatorForCut.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            var lowLeg = animatorForCut.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            // Same shorts-length fraction as the abandoned Blender attempt
            // (62% from hip toward knee), measured in the renderer's OWN
            // local space via BakeMesh instead of world space.
            var hipLocal = livePants.transform.InverseTransformPoint(upLeg.position);
            var kneeLocal = livePants.transform.InverseTransformPoint(lowLeg.position);
            float cutY = hipLocal.y - 0.62f * (hipLocal.y - kneeLocal.y);
            Debug.Log($"MINI166SHORTS: hipLocal={hipLocal} kneeLocal={kneeLocal} cutY={cutY}");

            var baseMesh = livePants.sharedMesh; // the ALREADY-WORKING Jeans/Trousers mesh
            var baked = new Mesh();
            livePants.BakeMesh(baked); // posed positions in the renderer's own local space - same space cutY is in
            var bakedVerts = baked.vertices;
            var allTris = baseMesh.GetTriangles(0);

            var skinTris = new System.Collections.Generic.List<int>();
            var fabricTris = new System.Collections.Generic.List<int>();
            for (int i = 0; i < allTris.Length; i += 3)
            {
                int a = allTris[i], b = allTris[i + 1], c = allTris[i + 2];
                float avgY = (bakedVerts[a].y + bakedVerts[b].y + bakedVerts[c].y) / 3f;
                var target = avgY < cutY ? skinTris : fabricTris;
                target.Add(a); target.Add(b); target.Add(c);
            }
            Debug.Log($"MINI166SHORTS: classified {skinTris.Count / 3} skin tris, {fabricTris.Count / 3} fabric tris of {allTris.Length / 3} total");
            Require(skinTris.Count > 60, "too few skin triangles - check the cut height");

            var shortsMesh = UnityEngine.Object.Instantiate(baseMesh);
            shortsMesh.name = "Franki_Pants_ShortsDenim_CSharp";
            shortsMesh.subMeshCount = 2;
            shortsMesh.SetTriangles(fabricTris, 0);
            shortsMesh.SetTriangles(skinTris, 1);

            // Franki's real skin tone, already sampled and proven correct
            // by the abandoned Blender attempt (from Ch28_Body's own
            // Head-bone-dominant texture region) - reused directly.
            var skinMat = new Material(Shader.Find("Standard")) { name = "Franki_Shorts_LegSkin_CSharp" };
            skinMat.color = new Color(0.412f, 0.243f, 0.153f);
            var fabricMat = livePants.sharedMaterial; // keep the exact existing pants fabric material/colour

            var shortsPiece = new OutfitPiece { id = "pants_shorts_denim", label = "Denim Shorts", slot = OutfitSlot.Pants, mesh = shortsMesh, materials = new[] { fabricMat, skinMat }, tintSlots = new[] { 0 } };

            var wardrobe = root.GetComponent<OutfitWardrobe>();
            Require(wardrobe != null, "Franki has no OutfitWardrobe - run the Pants slot first");
            var byId = wardrobe.pieces.ToDictionary(p => p.id);
            byId[shortsPiece.id] = shortsPiece;
            wardrobe.pieces = byId.Values.ToArray();
            // binding already exists (Jeans/Trousers use the same Ch28_Pants renderer) - just make sure it's still there
            Require(wardrobe.bindings.Any(b => b.slot == OutfitSlot.Pants), "Pants binding missing - run the Pants slot integration first");

            Debug.Log($"MINI166SHORTS: Franki added Denim Shorts piece to existing Pants binding ({shortsMesh.vertexCount}v)");

            if (save)
            {
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                Debug.Log("MINI166_SHORTS_INTEGRATE_PASS saved");
            }
            else
            {
                RenderPreview(root);
                Debug.Log("MINI166_SHORTS_PREVIEW_PASS not saved");
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
            Capture(camera, root.transform.position + Vector3.up * 2f, root.transform.position, $"{Out}/_warmup8.png");

            bool selectOk = wardrobe.Select("pants_shorts_denim", 0);
            // The warmup above ran BEFORE this mesh swap - warm again so
            // GPU skinning actually reflects the newly assigned mesh before
            // the real capture (same "render once before trusting a swap"
            // lesson this project has hit before, just missed the ordering
            // here specifically).
            Capture(camera, root.transform.position + Vector3.up * 2f, root.transform.position, $"{Out}/_warmup8b.png");
            var pantsRdbg = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "Ch28_Pants");
            Debug.Log($"MINI166SHORTS: Select returned {selectOk}; pantsR.enabled={pantsRdbg.enabled} sharedMesh={pantsRdbg.sharedMesh?.name} verts={pantsRdbg.sharedMesh?.vertexCount} materials={string.Join(",", pantsRdbg.sharedMaterials.Select(m => m?.name))} bones.Length={pantsRdbg.bones.Length} bounds={pantsRdbg.bounds}");
            var baked = new Mesh();
            pantsRdbg.BakeMesh(baked);
            var bv = baked.vertices;
            Debug.Log($"MINI166SHORTS: baked mesh bounds(renderer-local)={baked.bounds} vertex[0]={bv[0]} vertex[{bv.Length/2}]={bv[bv.Length/2]} vertex[{bv.Length-1}]={bv[bv.Length-1]} pantsRdbg.transform.position={pantsRdbg.transform.position} pantsRdbg.transform.lossyScale={pantsRdbg.transform.lossyScale}");
            foreach (var m in pantsRdbg.sharedMaterials)
            {
                string colorStr = m.HasProperty("_Color") ? m.color.ToString() : "n/a";
                string modeStr = m.HasProperty("_Mode") ? m.GetFloat("_Mode").ToString() : "n/a";
                Debug.Log($"MINI166SHORTS: material={m.name} shader={m.shader.name} color={colorStr} renderQueue={m.renderQueue} mode={modeStr}");
            }
            var bodyR = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "Ch28_Body");
            var b = bodyR.bounds;
            Capture(camera, b.center - root.transform.forward * (b.size.magnitude * 1.1f) + Vector3.up * 0.1f, b.center, $"{Out}/Franki-Shorts-Full-A.png");
            Capture(camera, b.center + root.transform.forward * (b.size.magnitude * 1.1f) + Vector3.up * 0.1f, b.center, $"{Out}/Franki-Shorts-Full-B.png");
            var pantsR = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "Ch28_Pants");
            var pb = pantsR.bounds;
            Capture(camera, pb.center - root.transform.forward * 0.6f + Vector3.up * 0.05f, pb.center, $"{Out}/Franki-Shorts-Legs.png");

            root.transform.position = originalPos;
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
            UnityEngine.Object.DestroyImmediate(key.gameObject);
            UnityEngine.Object.DestroyImmediate(fill.gameObject);
        }

        static void Sample(Animator a, AnimationClip clip, float t)
        {
            var graph = PlayableGraph.Create("Mini166Shorts");
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
