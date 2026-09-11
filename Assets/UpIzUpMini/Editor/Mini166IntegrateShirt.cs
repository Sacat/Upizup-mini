using System;
using System.Collections.Generic;
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
    /// <summary>MINI-166 Shirt slot, round 1: wire OutfitWardrobe onto both
    /// characters for the Shirt slot only, with two real designs each
    /// (Tee/Polo). Franki: full mesh swap on the existing Ch28_Hoody
    /// renderer (bone-remap-by-name, same proven technique as MINI-159/
    /// 161/163). Sacat: Ch06 is fused (cannot safely mesh-swap it this
    /// round - see mini166_shirt_collar.py header) so the Shirt slot binds
    /// a NEW small overlay renderer (collar+placket+buttons) toggled
    /// enabled=false for Tee (unchanged base look) / true for Polo. Does
    /// NOT touch Ch06 itself.</summary>
    public static class Mini166IntegrateShirt
    {
        const string Scene = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string Garments = "Assets/UpIzUpMini/Art/Characters/Garments";
        const string Out = "Logs/Tasks/MINI-166";

        static void Require(bool ok, string msg) { if (!ok) throw new Exception("MINI166SHIRT: " + msg); }

        [MenuItem("Up Iz Up Mini/MINI-166/Preview Shirt Slot (no save)")]
        public static void Preview() => Run(false);
        [MenuItem("Up Iz Up Mini/MINI-166/Integrate Shirt Slot")]
        public static void Integrate() => Run(true);

        static GameObject ImportHumanoid(string path)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            Require(importer != null, "missing " + path);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.SaveAndReimport();
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Require(go != null, "source failed to load " + path);
            return go;
        }

        static void Run(bool save)
        {
            Directory.CreateDirectory(Out);
            EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);

            var frankiRoot = GameObject.Find("Franki");
            var sacatRoot = GameObject.Find("Sacat");
            Require(frankiRoot != null, "no Franki in scene");
            Require(sacatRoot != null, "no Sacat in scene");

            IntegrateFranki(frankiRoot);
            IntegrateSacat(sacatRoot);

            if (save)
            {
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                Debug.Log("MINI166_SHIRT_INTEGRATE_PASS saved");
            }
            else
            {
                RenderPreview(frankiRoot, "Franki");
                RenderPreview(sacatRoot, "Sacat");
                Debug.Log("MINI166_SHIRT_PREVIEW_PASS not saved");
            }

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static string GetPath(Transform t)
        {
            var parts = new List<string>();
            while (t != null) { parts.Add(t.name); t = t.parent; }
            parts.Reverse();
            return string.Join("/", parts);
        }

        static Dictionary<string, Transform> BonesByName(GameObject root)
        {
            var d = new Dictionary<string, Transform>();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (!d.ContainsKey(t.name)) d[t.name] = t;
            return d;
        }

        static void IntegrateFranki(GameObject root)
        {
            var teeSrc = ImportHumanoid($"{Garments}/Franki_Shirt_Tee_Mike.fbx");
            var poloSrc = ImportHumanoid($"{Garments}/Franki_Shirt_Polo_Lacos.fbx");
            var liveBones = BonesByName(root);
            var liveHoody = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "Ch28_Hoody");

            var teePiece = new OutfitPiece { id = "shirt_tee_mike", label = "Mike Crew Tee", slot = OutfitSlot.Shirt, tintSlots = new[] { 0 } };
            var poloPiece = new OutfitPiece { id = "shirt_polo_lacos", label = "Lacos Polo", slot = OutfitSlot.Shirt, tintSlots = new[] { 0 } };
            ExtractMeshMaterials(teeSrc, out teePiece.mesh, out teePiece.materials);
            ExtractMeshMaterials(poloSrc, out poloPiece.mesh, out poloPiece.materials);

            // BUG FOUND BY RENDER (both pieces came out invisible): swapping
            // sharedMesh alone is not enough - each new source FBX export can
            // reorder its own vertex-group/bone list independently of the
            // LIVE renderer's existing `bones` array (from the older MINI-161
            // integration), so the new mesh's boneWeights indices pointed at
            // the wrong live bones and the skin collapsed to nothing. Fix:
            // remap the live renderer's bones ONCE to the bone order this
            // round's source uses (tee and polo share that order - polo was
            // built by duplicating tee's mesh object in the same Blender
            // session, so its vertex-group list is identical).
            var teeSrcR = teeSrc.GetComponentsInChildren<SkinnedMeshRenderer>(true).First();
            var newBones = new Transform[teeSrcR.bones.Length];
            for (int i = 0; i < teeSrcR.bones.Length; i++)
            {
                Require(liveBones.TryGetValue(teeSrcR.bones[i].name, out var b), "no live Franki bone " + teeSrcR.bones[i].name);
                newBones[i] = b;
            }
            liveHoody.bones = newBones;
            if (teeSrcR.rootBone != null && liveBones.TryGetValue(teeSrcR.rootBone.name, out var rb))
                liveHoody.rootBone = rb;
            Debug.Log($"MINI166SHIRT: Franki rootBone src={teeSrcR.rootBone?.name} -> live={liveHoody.rootBone?.name} pos={liveHoody.rootBone?.position}");
            for (int i = 0; i < newBones.Length; i++)
                Debug.Log($"MINI166SHIRT: bone[{i}] {teeSrcR.bones[i].name} -> world={newBones[i].position} localBindDiag={teeSrcR.sharedMesh.bindposes[i].GetColumn(3)}");
            var poloSrcR = poloSrc.GetComponentsInChildren<SkinnedMeshRenderer>(true).First();
            Require(poloSrcR.bones.Length == teeSrcR.bones.Length, $"tee/polo bone count mismatch tee={teeSrcR.bones.Length} polo={poloSrcR.bones.Length} - pieces need separate bone remaps, not a shared renderer.bones array");
            for (int i = 0; i < poloSrcR.bones.Length; i++)
                Require(poloSrcR.bones[i].name == teeSrcR.bones[i].name, $"tee/polo bone order differs at index {i}: {teeSrcR.bones[i].name} vs {poloSrcR.bones[i].name}");

            var wardrobe = root.GetComponent<OutfitWardrobe>();
            if (wardrobe == null) wardrobe = root.AddComponent<OutfitWardrobe>();
            wardrobe.pieces = MergePieces(wardrobe.pieces, teePiece, poloPiece);
            wardrobe.bindings = MergeBindings(wardrobe.bindings, new OutfitBinding { slot = OutfitSlot.Shirt, renderer = liveHoody });
            wardrobe.defaults = MergeDefaults(wardrobe.defaults, wardrobe.pieces, OutfitSlot.Shirt, new OutfitChoice { itemId = "shirt_tee_mike", colour = 0 });

            Debug.Log($"MINI166SHIRT: Franki bound Ch28_Hoody, pieces=tee({teePiece.mesh.vertexCount}v)/polo({poloPiece.mesh.vertexCount}v)");
        }

        static void IntegrateSacat(GameObject root)
        {
            var overlaySrc = ImportHumanoid($"{Garments}/Sacat_Shirt_Polo_Overlay.fbx");
            var liveBones = BonesByName(root);
            var ch06 = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "Ch06");

            // Build (or reuse) a dedicated overlay child renderer under the
            // same parent as Ch06 - never touches Ch06 itself.
            var overlayGo = root.transform.Find("Sacat_ShirtOverlay");
            SkinnedMeshRenderer overlayR;
            if (overlayGo == null)
            {
                var go = new GameObject("Sacat_ShirtOverlay");
                go.transform.SetParent(ch06.transform.parent, false);
                overlayR = go.AddComponent<SkinnedMeshRenderer>();
            }
            else
            {
                overlayR = overlayGo.GetComponent<SkinnedMeshRenderer>();
            }

            var srcR = overlaySrc.GetComponentsInChildren<SkinnedMeshRenderer>(true).First();
            Debug.Log($"MINI166SHIRT: Sacat srcR bones.Length={srcR.bones.Length} mesh.bindposes.Length={srcR.sharedMesh.bindposes.Length} boneWeights.Length={srcR.sharedMesh.boneWeights.Length} verts={srcR.sharedMesh.vertexCount}");
            var bw0 = srcR.sharedMesh.boneWeights[0];
            Debug.Log($"MINI166SHIRT: Sacat vertex0 boneIndex0={bw0.boneIndex0} weight0={bw0.weight0} -> srcR.bones[{bw0.boneIndex0}].name={srcR.bones[bw0.boneIndex0]?.name} srcVertexPos={srcR.sharedMesh.vertices[0]} bindpose={srcR.sharedMesh.bindposes[bw0.boneIndex0]}");
            var neckIdx = Array.FindIndex(srcR.bones, b => b != null && b.name == "mixamorig9:Neck");
            Debug.Log($"MINI166SHIRT: Sacat mixamorig9:Neck is at srcR.bones index={neckIdx}");
            Debug.Log($"MINI166SHIRT: Sacat srcR.sharedMesh.bounds={srcR.sharedMesh.bounds} (as-imported, no manipulation)");
            var vv = srcR.sharedMesh.vertices;
            for (int i = 0; i < vv.Length; i += Math.Max(1, vv.Length / 6))
                Debug.Log($"MINI166SHIRT: Sacat vertex[{i}]={vv[i]}");

            // ROOT CAUSE FOUND: srcR's own mesh data is completely normal
            // (vertex0 near origin (0,0,0.02), boneIndex0=4 correctly maps
            // to "mixamorig9:Neck", bindpose is identity-like) - exactly the
            // same shape as Franki's working data. Two earlier "clever"
            // rebuilds (matrix-remap, then bake-to-local-space) were solving
            // a problem that didn't exist in the source data and made things
            // worse. The actual bug was the plain bone-name remap done the
            // SAME way as Franki's (proven) - just never actually tested in
            // isolation because a leftover line kept overwriting rootBone
            // back to Hips every time. Reverting to that exact proven
            // pattern now, with the rootBone bug fixed for real.
            var newBones = new Transform[srcR.bones.Length];
            for (int i = 0; i < srcR.bones.Length; i++)
            {
                Require(liveBones.TryGetValue(srcR.bones[i].name, out var b), "no live Sacat bone " + srcR.bones[i].name);
                newBones[i] = b;
            }
            overlayR.sharedMesh = srcR.sharedMesh;
            overlayR.sharedMaterials = srcR.sharedMaterials;
            overlayR.bones = newBones;
            overlayR.rootBone = newBones[neckIdx]; // the mesh is weighted to Neck alone; do not default to Hips
            overlayR.enabled = false; // Tee default = base Ch06 look, unchanged

            var teePiece = new OutfitPiece { id = "shirt_tee_mike", label = "Mike Crew Tee (base)", slot = OutfitSlot.Shirt, mesh = null, materials = Array.Empty<Material>() };
            var poloPiece = new OutfitPiece { id = "shirt_polo_lacos", label = "Lacos Polo (collar overlay)", slot = OutfitSlot.Shirt, tintSlots = new[] { 0 } };
            poloPiece.mesh = srcR.sharedMesh;
            poloPiece.materials = srcR.sharedMaterials;

            var wardrobe = root.GetComponent<OutfitWardrobe>();
            if (wardrobe == null) wardrobe = root.AddComponent<OutfitWardrobe>();
            wardrobe.pieces = MergePieces(wardrobe.pieces, teePiece, poloPiece);
            wardrobe.bindings = MergeBindings(wardrobe.bindings, new OutfitBinding { slot = OutfitSlot.Shirt, renderer = overlayR });
            wardrobe.defaults = MergeDefaults(wardrobe.defaults, wardrobe.pieces, OutfitSlot.Shirt, new OutfitChoice { itemId = "shirt_tee_mike", colour = 0 });

            Debug.Log($"MINI166SHIRT: Sacat bound Sacat_ShirtOverlay (new child, Ch06 untouched), overlay tris={srcR.sharedMesh.triangles.Length / 3}");
        }

        static void ExtractMeshMaterials(GameObject prefab, out Mesh mesh, out Material[] materials)
        {
            var r = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).First();
            mesh = r.sharedMesh;
            materials = r.sharedMaterials;
        }

        static OutfitPiece[] MergePieces(OutfitPiece[] existing, params OutfitPiece[] add)
        {
            var byId = existing.ToDictionary(p => p.id);
            foreach (var p in add) byId[p.id] = p;
            return byId.Values.ToArray();
        }

        static OutfitBinding[] MergeBindings(OutfitBinding[] existing, params OutfitBinding[] add)
        {
            var list = existing.Where(b => !add.Any(a => a.slot == b.slot)).ToList();
            list.AddRange(add);
            return list.ToArray();
        }

        static OutfitChoice[] MergeDefaults(OutfitChoice[] existing, OutfitPiece[] pieces, OutfitSlot slot, OutfitChoice value)
        {
            // BUG FOUND: an earlier version returned `new[]{value}` here,
            // wholesale REPLACING the defaults array - fine the first time
            // (Shirt was the only slot), but would silently wipe every other
            // slot's default the next time Shirt integration is re-run.
            // OutfitChoice itself carries no slot, so resolve it via the
            // piece it names: drop any existing default whose itemId
            // belongs to THIS slot's own piece set, keep everything else.
            var thisSlotIds = pieces.Where(p => p.slot == slot).Select(p => p.id).ToHashSet();
            var kept = (existing ?? Array.Empty<OutfitChoice>()).Where(c => c != null && !thisSlotIds.Contains(c.itemId)).ToList();
            kept.Add(value);
            return kept.ToArray();
        }

        static void RenderPreview(GameObject root, string tag)
        {
            var wardrobe = root.GetComponent<OutfitWardrobe>();
            wardrobe.Select("shirt_polo_lacos", 0);

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
            bool poseBroken = Environment.GetEnvironmentVariable("MINI166_SKIP_POSE") == "1";
            if (!poseBroken)
            {
                var idle = AssetDatabase.LoadAllAssetsAtPath("Assets/UpIzUpMini/Art/Animations/Stand--Idle.anim.fbx").OfType<AnimationClip>().First(c => !c.name.StartsWith("__"));
                Sample(animator, idle, idle.length * 0.4f);
            }
            Capture(camera, root.transform.position + Vector3.up * 2f, root.transform.position, $"{Out}/_warmup_{tag}.png");

            // Frame off the named base body renderer (proven pattern from
            // MINI-163's Live-Franki renders) - NOT "first enabled renderer",
            // which can pick shoes/hands and produce a wildly wrong frame.
            foreach (var rr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                Debug.Log($"MINI166SHIRT: {tag} ALL renderer={rr.name} enabled={rr.enabled} mesh={rr.sharedMesh?.name} verts={rr.sharedMesh?.vertexCount} bounds.center={rr.bounds.center} bounds.size={rr.bounds.size} localBounds.center={rr.localBounds.center} localBounds.size={rr.localBounds.size} rootBone={rr.rootBone?.name} boneCount={rr.bones.Length}");
            var bodyR = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "Ch28_Body" || r.name == "Ch06");
            var shirtR = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "Ch28_Hoody" || r.name == "Sacat_ShirtOverlay");
            var b = bodyR.bounds;
            Debug.Log($"MINI166SHIRT: {tag} bodyR={bodyR.name} bounds.center={b.center} bounds.size={b.size} shirtR={shirtR.name} enabled={shirtR.enabled} shirtBounds={shirtR.bounds}");
            Debug.Log($"MINI166SHIRT: {tag} root.forward={root.transform.forward} root.rotation={root.transform.rotation.eulerAngles} root.lossyScale={root.transform.lossyScale}");
            Capture(camera, b.center + new Vector3(0, 0.1f, -3f), b.center, $"{Out}/{tag}-Polo-Full-A.png");
            Capture(camera, b.center + new Vector3(0, 0.1f, 3f), b.center, $"{Out}/{tag}-Polo-Full-B.png");
            var chestPoint = Vector3.Lerp(b.min, b.max, 0.5f) + Vector3.up * (b.size.y * 0.18f);
            Capture(camera, chestPoint + new Vector3(0, 0, -1f), chestPoint, $"{Out}/{tag}-Polo-Chest-A.png");
            Capture(camera, chestPoint + new Vector3(0, 0, 1f), chestPoint, $"{Out}/{tag}-Polo-Chest-B.png");
            Capture(camera, bodyR.bounds.center + new Vector3(0, 0, -1f), bodyR.bounds.center, $"{Out}/{tag}-AtBodyCenter.png");
            Capture(camera, shirtR.bounds.center + new Vector3(0, 0, -1f), shirtR.bounds.center, $"{Out}/{tag}-AtShirtCenter.png");

            root.transform.position = originalPos;
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
            UnityEngine.Object.DestroyImmediate(key.gameObject);
            UnityEngine.Object.DestroyImmediate(fill.gameObject);
        }

        static void Sample(Animator a, AnimationClip clip, float t)
        {
            var graph = PlayableGraph.Create("Mini166Shirt");
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
