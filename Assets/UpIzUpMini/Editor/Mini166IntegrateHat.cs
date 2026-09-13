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
    /// <summary>MINI-166 Hat slot, round 1: a real selectable Lacos cap with
    /// a "None" option (reveals hair), for BOTH characters. Pure C# combined
    /// primitive mesh (crown + peak, matching BuildCap()'s already-fixed
    /// geometry/offset from this session's accessory-fit pass) baked into
    /// the Head bone's own local space with an identity bindpose and a
    /// single-bone rigid skin - the exact technique that fixed Sacat's Shirt
    /// overlay earlier this session. No Blender/FBX step, so none of that
    /// pipeline's risk applies here.</summary>
    public static class Mini166IntegrateHat
    {
        const string Scene = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string Out = "Logs/Tasks/MINI-166";
        const float UpOffset = 0.16f; // matches CharacterEquipment.PositionOnBone's fixed cap offset

        [MenuItem("Up Iz Up Mini/MINI-166/Preview Hat Slot (no save)")]
        public static void Preview() => Run(false);
        [MenuItem("Up Iz Up Mini/MINI-166/Integrate Hat Slot")]
        public static void Integrate() => Run(true);

        static void Run(bool save)
        {
            Directory.CreateDirectory(Out);
            EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);

            // Sacat's Hat initially looked broken (cap at ear height) in a
            // first render - traced to the RENDER TOOL, not the placement:
            // a fixed world-space camera offset doesn't account for his
            // ~277.8-degree root rotation, so "Front" was actually viewing
            // the back of his head at a steep angle. The bake itself was
            // already proven pose-independent and produced IDENTICAL
            // head-local bounds for both characters. Fixed the camera to use
            // root.transform.forward/right, confirmed the cap sits correctly
            // on both. See MINI-166.md for the full trace.
            var characters = new[] { "Franki", "Sacat" };
            foreach (var name in characters)
                IntegrateCharacter(GameObject.Find(name), name);

            if (save)
            {
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                Debug.Log("MINI166_HAT_INTEGRATE_PASS saved");
            }
            else
            {
                foreach (var name in characters)
                    RenderPreview(GameObject.Find(name), name);
                Debug.Log("MINI166_HAT_PREVIEW_PASS not saved");
            }

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void IntegrateCharacter(GameObject root, string tag)
        {
            var animator = root.GetComponentInChildren<Animator>(true);
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            var bodyR = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == (tag == "Sacat" ? "Ch06" : "Ch28_Body"));

            // Build the combined crown+peak mesh exactly as BuildCap() does,
            // in cap-ROOT local space first.
            var crownMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(0.2f, 0.12f, 0.2f));
            var peakMatrix = Matrix4x4.TRS(new Vector3(0f, -0.01f, 0.11f), Quaternion.identity, new Vector3(0.18f, 0.02f, 0.12f));
            var sphereMesh = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx") ?? BuiltinMesh(PrimitiveType.Sphere);
            var cubeMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx") ?? BuiltinMesh(PrimitiveType.Cube);
            var cylinderMesh = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx") ?? BuiltinMesh(PrimitiveType.Cylinder);

            // MINI-166 back strap/eyelets: real geometry carrying over the
            // shape/placement language validated on the concept "DA"
            // snapback cap (Tools/ArtPreview/da_snapback_cap_preview.py,
            // Blender concept only) - built directly here in cap-root local
            // space, same as crown/peak above, no Blender/FBX round trip
            // (this project's own MINI-166 Denim Shorts history: 9 rounds
            // chasing an unexplained Blender/FBX bug before switching to
            // pure C# - avoided entirely by building primitives directly).
            // The crown is an ellipsoid (sphere scaled 0.2/0.12/0.2, so
            // radii rx=rz=0.1, ry=0.06) - "front" is +Z (the peak sits at
            // z=+0.11), so "back" is -Z, matching the concept cap's own
            // convention.
            const float rx = 0.1f, ry = 0.06f, rz = 0.1f;
            Vector3 EllipsoidPoint(float degFromFront, float yFrac)
            {
                // yFrac in [-1,1] (equator..pole); degFromFront measured
                // around Y from the +Z (front) axis.
                float ringScale = Mathf.Sqrt(Mathf.Max(0f, 1f - yFrac * yFrac));
                float rad = degFromFront * Mathf.Deg2Rad;
                return new Vector3(rx * ringScale * Mathf.Sin(rad), ry * yFrac, rz * ringScale * Mathf.Cos(rad));
            }

            // Eyelets: 4 small rivets low on the crown sides (real caps
            // place them near the band, not the crown top), symmetric
            // about the front/back axis.
            var eyeletAngles = new[] { 75f, 105f, -75f, -105f };
            var eyeletCombines = new CombineInstance[eyeletAngles.Length];
            for (int i = 0; i < eyeletAngles.Length; i++)
            {
                var p = EllipsoidPoint(eyeletAngles[i], -0.18f);
                var outward = new Vector3(p.x, 0, p.z).normalized;
                var rot = Quaternion.FromToRotation(Vector3.up, outward);
                eyeletCombines[i] = new CombineInstance { mesh = cylinderMesh, transform = Matrix4x4.TRS(p, rot, new Vector3(0.016f, 0.007f, 0.016f)) };
            }
            var eyeletsMesh = new Mesh(); eyeletsMesh.CombineMeshes(eyeletCombines, true, true);

            // Back strap + two snap details, at the back of the crown
            // (degFromFront=180) near the base, matching the concept cap's
            // placement.
            var strapCenter = EllipsoidPoint(180f, -0.55f);
            var strapOutward = new Vector3(strapCenter.x, 0, strapCenter.z).normalized;
            var strapRot = Quaternion.LookRotation(strapOutward, Vector3.up);
            var strapMatrix = Matrix4x4.TRS(strapCenter + strapOutward * 0.004f, strapRot, new Vector3(0.085f, 0.016f, 0.006f));

            var snapCombines = new CombineInstance[2];
            for (int i = 0; i < 2; i++)
            {
                var sideDeg = i == 0 ? 165f : -165f; // either side of dead-back, along the strap
                var p = EllipsoidPoint(sideDeg, -0.55f);
                var outward = new Vector3(p.x, 0, p.z).normalized;
                var rot = Quaternion.FromToRotation(Vector3.up, outward);
                snapCombines[i] = new CombineInstance { mesh = cylinderMesh, transform = Matrix4x4.TRS(p + outward * 0.006f, rot, new Vector3(0.014f, 0.003f, 0.014f)) };
            }
            var snapsMesh = new Mesh(); snapsMesh.CombineMeshes(snapCombines, true, true);

            // Cap-root world placement, matching PositionOnBone's already-
            // fixed cap_mike offset: anchor(Head) + character-up * 0.16, no
            // forward offset, rotation = character root (not head bone) so
            // the crown doesn't look tilted at odd head-bone rest angles.
            var capRootWorld = Matrix4x4.TRS(head.position + root.transform.up * UpOffset, root.transform.rotation, Vector3.one);
            var headInv = head.worldToLocalMatrix;
            var toHead = headInv * capRootWorld;

            var combine = new CombineInstance[5];
            combine[0] = new CombineInstance { mesh = sphereMesh, transform = toHead * crownMatrix };
            combine[1] = new CombineInstance { mesh = cubeMesh, transform = toHead * peakMatrix };
            combine[2] = new CombineInstance { mesh = eyeletsMesh, transform = toHead };
            combine[3] = new CombineInstance { mesh = cubeMesh, transform = toHead * strapMatrix };
            combine[4] = new CombineInstance { mesh = snapsMesh, transform = toHead };
            var combined = new Mesh { name = $"{tag}_Hat_Lacos" };
            combined.CombineMeshes(combine, false, true); // false = keep submeshes separate (each part stays a distinct material)
            combined.RecalculateBounds();
            combined.RecalculateNormals();
            Debug.Log($"MINI166HAT: {tag} head.position={head.position} head.rotation={head.rotation.eulerAngles} head.lossyScale={head.lossyScale} capRootWorld.pos={capRootWorld.GetColumn(3)} combined.bounds(head-local)={combined.bounds} root.rotation={root.transform.rotation.eulerAngles}");
            Debug.Log($"MINI166HAT: {tag} animator.hasTransformHierarchy={animator.hasTransformHierarchy} head.parent={head.parent?.name} head.childCount={head.childCount} head active={head.gameObject.activeInHierarchy} bodyR.rootBone={bodyR.rootBone?.name} bodyR.bones.Length={bodyR.bones.Length}");

            var crownMat = new Material(Shader.Find("Standard")) { name = $"{tag}_HatCrown", color = new Color(0.08f, 0.16f, 0.30f) };
            var peakMat = new Material(Shader.Find("Standard")) { name = $"{tag}_HatPeak", color = new Color(0.08f, 0.16f, 0.30f) * 0.8f };
            var eyeletMat = new Material(Shader.Find("Standard")) { name = $"{tag}_HatEyelet", color = new Color(0.16f, 0.16f, 0.17f) };
            eyeletMat.SetFloat("_Metallic", 0.6f); eyeletMat.SetFloat("_Glossiness", 0.45f);
            var strapMat = new Material(Shader.Find("Standard")) { name = $"{tag}_HatStrap", color = new Color(0.05f, 0.08f, 0.14f) };

            var hatGo = root.transform.Find($"{tag}_HatOverlay");
            SkinnedMeshRenderer hatR;
            if (hatGo == null)
            {
                var go = new GameObject($"{tag}_HatOverlay");
                go.transform.SetParent(bodyR.transform.parent, false);
                hatR = go.AddComponent<SkinnedMeshRenderer>();
            }
            else hatR = hatGo.GetComponent<SkinnedMeshRenderer>();

            hatR.sharedMesh = combined;
            hatR.sharedMaterials = new[] { crownMat, peakMat, eyeletMat, strapMat, eyeletMat };
            hatR.bones = new[] { head };
            hatR.rootBone = head;
            combined.bindposes = new[] { Matrix4x4.identity };
            var weights = new BoneWeight[combined.vertexCount];
            for (int i = 0; i < weights.Length; i++) weights[i] = new BoneWeight { boneIndex0 = 0, weight0 = 1f };
            combined.boneWeights = weights;
            hatR.enabled = false; // default None - hair stays visible until a piece is chosen

            var nonePiece = new OutfitPiece { id = "hat_none", label = "No Hat", slot = OutfitSlot.Hat, mesh = null, materials = System.Array.Empty<Material>() };
            var capPiece = new OutfitPiece { id = "hat_lacos", label = "Lacos Cap", slot = OutfitSlot.Hat, mesh = combined, materials = new[] { crownMat, peakMat, eyeletMat, strapMat, eyeletMat }, tintSlots = new[] { 0, 1, 3 } };

            var wardrobe = root.GetComponent<OutfitWardrobe>();
            if (wardrobe == null) wardrobe = root.AddComponent<OutfitWardrobe>();
            var byId = wardrobe.pieces.ToDictionary(p => p.id);
            byId[nonePiece.id] = nonePiece; byId[capPiece.id] = capPiece;
            wardrobe.pieces = byId.Values.ToArray();
            var bindings = wardrobe.bindings.Where(b => b.slot != OutfitSlot.Hat).ToList();
            bindings.Add(new OutfitBinding { slot = OutfitSlot.Hat, renderer = hatR });
            wardrobe.bindings = bindings.ToArray();
            var thisSlotIds = new System.Collections.Generic.HashSet<string> { "hat_none", "hat_lacos" };
            var defaults = wardrobe.defaults.Where(c => c != null && !thisSlotIds.Contains(c.itemId)).ToList();
            defaults.Add(new OutfitChoice { itemId = "hat_none", colour = 0 }); // default off - matches current shipped look, opt-in only
            wardrobe.defaults = defaults.ToArray();

            Debug.Log($"MINI166HAT: {tag} bound {tag}_HatOverlay to Head bone (rigid single-bone), pieces=none/lacos ({combined.vertexCount}v)");
        }

        static Mesh BuiltinMesh(PrimitiveType type)
        {
            var go = GameObject.CreatePrimitive(type);
            var mesh = go.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(go);
            return mesh;
        }

        static void RenderPreview(GameObject root, string tag)
        {
            var wardrobe = root.GetComponent<OutfitWardrobe>();
            wardrobe.Select("hat_lacos", 0);

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
            camera.nearClipPlane = .02f; camera.farClipPlane = 200; camera.fieldOfView = 30;

            Vector3 originalPos = root.transform.position;
            root.transform.position = new Vector3(500, 300, 0);
            var animator = root.GetComponentInChildren<Animator>(true);
            animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var idle = AssetDatabase.LoadAllAssetsAtPath("Assets/UpIzUpMini/Art/Animations/Stand--Idle.anim.fbx").OfType<AnimationClip>().First(c => !c.name.StartsWith("__"));
            Sample(animator, idle, idle.length * 0.4f);
            Capture(camera, root.transform.position + Vector3.up * 2f, root.transform.position, $"{Out}/_warmup5_{tag}.png");

            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            // BUG FOUND: fixed world-space offsets don't account for a
            // rotated root (Sacat's is ~277.8deg on Y) - "Front" ended up
            // viewing the BACK of his head at a steep angle, making a
            // correctly-placed cap look wrong (misdiagnosed as a placement
            // bug the first time). Use root.transform.forward/right so
            // "Front" actually means front for any character.
            // Second attempt still showed the back of the head - sign was
            // backwards: to SEE a face you stand where it's looking TOWARD
            // (further along +forward from the focus point), not behind it.
            var focus = head.position + Vector3.up * 0.03f;
            Capture(camera, focus + root.transform.forward * 0.45f + Vector3.up * 0.05f, focus, $"{Out}/{tag}-Hat-Front.png");
            Capture(camera, focus + root.transform.right * 0.45f + Vector3.up * 0.05f, focus, $"{Out}/{tag}-Hat-Side.png");
            // New this pass: a real back-view capture, since the strap/snap
            // detail being added sits at the back of the crown and neither
            // existing camera would ever show it.
            Capture(camera, focus - root.transform.forward * 0.45f + Vector3.up * 0.08f, focus, $"{Out}/{tag}-Hat-Back.png");
            // Tighter close-up on the crown/back-of-head seam specifically -
            // needed to tell the new strap apart from Sacat's separate
            // headphones accessory (MINI-165), which also has rear geometry
            // near the same area.
            Capture(camera, focus - root.transform.forward * 0.22f + Vector3.up * 0.12f, focus + Vector3.up * 0.06f, $"{Out}/{tag}-Hat-BackCloseup.png");

            root.transform.position = originalPos;
            Object.DestroyImmediate(camera.gameObject);
            Object.DestroyImmediate(key.gameObject);
            Object.DestroyImmediate(fill.gameObject);
        }

        static void Sample(Animator a, AnimationClip clip, float t)
        {
            var graph = PlayableGraph.Create("Mini166Hat");
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
            Object.DestroyImmediate(tex); Object.DestroyImmediate(rt);
        }
    }
}
