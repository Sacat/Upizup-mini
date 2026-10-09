using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-205: swaps Sacat/Franki's visuals for the imported bare bodies (MINI-204) in the NEW-MAP scene only, and rebuilds the whole wardrobe on the new
    /// CC_Base rig: shirts/trousers are offset shells of the bare body (so they always fit and are skinned with the body's own weights), shoes/hats/headphones
    /// are retargeted or regenerated from the new bones, every scene reference to the old Animator/skeleton is repointed through the Humanoid bone map.
    /// New assets use a "Bare_" name so the live scene's old-rig garments are never overwritten.
    /// </summary>
    public static partial class Mini166Repair
    {
        static bool bareRig;
        const string BareDir = "Assets/UpIzUpMini/Art/Characters/Modular/Bare";
        static readonly string[] BareHuman = { "Hips", "Spine", "Chest", "UpperChest", "Neck", "Head", "LeftShoulder", "LeftUpperArm", "LeftLowerArm", "LeftHand", "RightShoulder", "RightUpperArm", "RightLowerArm", "RightHand", "LeftUpperLeg", "LeftLowerLeg", "LeftFoot", "LeftToes", "RightUpperLeg", "RightLowerLeg", "RightFoot", "RightToes", "LeftEye", "RightEye", "Jaw" };

        [MenuItem("Up Iz Up Mini/MINI-205/Swap In Bare Bodies (new-map scene)")]
        public static void SwapBareBodies()
        {
            try
            {
                string scenePath = Environment.GetEnvironmentVariable("MINI205_SCENE") ?? "Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity";
                string who = Environment.GetEnvironmentVariable("MINI205_WHO") ?? "Sacat,Franki";
                Directory.CreateDirectory("Backups/MINI-205-pre-bare"); string bk = "Backups/MINI-205-pre-bare/" + Path.GetFileName(scenePath);
                if (!File.Exists(bk)) File.Copy(scenePath, bk);
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                Directory.CreateDirectory(Out); Directory.CreateDirectory(Art);
                foreach (var n in who.Split(',')) SwapOne(n.Trim());
                AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                Debug.Log("MINI205_SWAP_PASS " + who);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); bareRig = false; if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        // ---- surfaces -------------------------------------------------------------------------------------------------------
        static Surface MergeBare(GameObject root, SkinnedMeshRenderer[] rs)
        {
            var parts = rs.Select(r => new Surface(root, r)).ToArray(); var merged = new Surface { root = root, bones = parts[0].bones, binds = parts[0].binds, materials = parts[0].materials };
            var verts = new List<V>(); var tris = new List<int[]>();
            foreach (var p in parts) { int b = verts.Count; verts.AddRange(p.vertices); tris.Add(p.triangles.SelectMany(t => t).Select(i => i + b).ToArray()); }
            merged.vertices = verts.ToArray(); merged.triangles = tris.ToArray(); return merged;
        }

        static List<List<V>> Faces(Surface s, int part) { var t = s.triangles[part]; var list = new List<List<V>>(); for (int i = 0; i < t.Length; i += 3) list.Add(new List<V> { s.vertices[t[i]], s.vertices[t[i + 1]], s.vertices[t[i + 2]] }); return list; }
        static V Push(V v, float d) { v.p += v.n * d; return v; }

        static Mesh SurfaceToMesh(Surface s, string name)
        {
            var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(s.vertices.Select(v => v.p).ToList()); mesh.SetNormals(s.vertices.Select(v => v.n).ToList()); mesh.SetUVs(0, s.vertices.Select(v => v.uv).ToList());
            mesh.boneWeights = s.vertices.Select(v => v.w).ToArray(); mesh.bindposes = s.binds; mesh.subMeshCount = s.triangles.Length;
            for (int i = 0; i < s.triangles.Length; i++) mesh.SetTriangles(s.triangles[i], i);
            mesh.RecalculateBounds(); return mesh;
        }

        // ---- garments ---------------------------------------------------------------------------------------------------------
        static List<List<V>> BareShirtFaces(Surface s)
        {
            float hipsY = s.Rest("Hips").y, hem = hipsY + .02f; var neck = s.Rest("Neck"); var result = new List<List<V>>();
            foreach (int part in BareTorsoParts)
                foreach (var f0 in Faces(s, part))
                {
                    var f = f0.Select(v => { float k = Mathf.Lerp(.011f, .007f, Mathf.InverseLerp(hem, hem + .30f, v.p.y)); return Push(v, k); }).ToList();
                    f = Clip(f, p => p.y - hem, true); if (f.Count < 3) continue;
                    float neckLow = neck.y - .075f; if (f.Average(v => v.p.y) > neckLow) { f = Clip(f, p => new Vector2(p.x, p.z - neck.z).magnitude - .062f, true); if (f.Count < 3) continue; }   // neck hole only up at the neck, never through the spine
                    result.Add(f);
                }
            return result;
        }

        static int[] BareTorsoParts = new int[0], BareLegParts = new int[0];

        static List<List<V>> BarePantsFaces(Surface s, int style)
        {
            float hipsY = s.Rest("Hips").y, top = hipsY + .045f, knee = s.Rest("LeftLeg").y, hip = s.Rest("LeftUpLeg").y, ankle = s.Rest("LeftFoot").y + .035f, cut = Mathf.Lerp(hip, knee, .83f);
            var result = new List<List<V>>();
            foreach (int part in BareLegParts.Concat(BareTorsoParts))
                foreach (var f0 in Faces(s, part))
                {
                    float thick = style == 0 ? .015f : style == 1 ? .024f : .017f;
                    var f = f0.Select(v => { float flare = style == 1 ? (1f - Mathf.InverseLerp(ankle, knee, v.p.y)) * .006f * Mathf.Clamp01(Mathf.InverseLerp(hip, knee, v.p.y) * 0f + 1f) : (style == 0 ? (1f - Mathf.InverseLerp(ankle, knee, v.p.y)) * .003f : 0f); return Push(v, thick + Mathf.Max(0f, flare)); }).ToList();
                    f = Clip(f, p => p.y - top, false); if (f.Count < 3) continue;                       // keep below the waist line
                    f = Clip(f, p => p.y - (style == 2 ? cut : ankle), true); if (f.Count < 3) continue; // keep above the hem
                    // torso faces must only contribute below the waist line; legs keep everything above the hem
                    result.Add(f);
                }
            return result;
        }

        // ---- the swap ---------------------------------------------------------------------------------------------------------
        static void SwapOne(string who)
        {
            var root = GameObject.Find(who); if (root == null) throw new Exception(who + " missing");
            var oldVisual = root.transform.Find("Visual"); if (oldVisual == null) throw new Exception(who + " has no Visual");
            var oldAnimator = oldVisual.GetComponent<Animator>(); var oldPose = oldVisual.GetComponent<UpIzUpMini.Combat.FirearmPose>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BareDir + "/" + who + "Bare.prefab"); if (prefab == null) throw new Exception("bare prefab missing for " + who);

            // 1. new visual
            var body = (GameObject)PrefabUtility.InstantiatePrefab(prefab); PrefabUtility.UnpackPrefabInstance(body, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            body.name = "Visual_Bare"; body.transform.SetParent(root.transform, false); body.transform.localPosition = oldVisual.localPosition; body.transform.localRotation = oldVisual.localRotation; body.transform.localScale = oldVisual.localScale;
            var newAnimator = body.GetComponent<Animator>() ?? body.AddComponent<Animator>();
            var avatar = AssetDatabase.LoadAllAssetsAtPath(BareDir + "/" + who + "Bare.fbx").OfType<Avatar>().First();
            newAnimator.avatar = avatar; newAnimator.runtimeAnimatorController = oldAnimator.runtimeAnimatorController; newAnimator.applyRootMotion = false; newAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate; newAnimator.updateMode = oldAnimator.updateMode;
            var newPose = body.AddComponent<UpIzUpMini.Combat.FirearmPose>(); if (oldPose != null) EditorUtility.CopySerialized(oldPose, newPose);

            // 2. surfaces
            var rs = body.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r => !r.name.EndsWith("_Hair")).ToArray();
            BareTorsoParts = Enumerable.Range(0, rs.Length).Where(i => rs[i].name.EndsWith("_Torso") || rs[i].name.EndsWith("_Arms")).ToArray();
            BareLegParts = Enumerable.Range(0, rs.Length).Where(i => rs[i].name.EndsWith("_Legs")).ToArray();
            var surf = MergeBare(root, rs);
            // the old shoes/hat/headphones meshes, read BEFORE the old rig is removed
            var oldShoesR = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "WardrobeShoes");
            var oldHeadphones = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(r => r.name == "HeadphonesAccessory");
            var airMeshes = new Dictionary<string, Mesh>(); foreach (string model in new[] { "90", "97" }) { var m = AssetDatabase.LoadAssetAtPath<Mesh>(AirArt + "/" + who + "_AM" + model + ".asset"); if (m != null) airMeshes[model] = m; }
            Surface headphonesBare = null; Mesh headphonesMesh = null; Material[] headphonesMats = null;
            if (oldHeadphones != null)
            {
                var from = new Surface(root, oldHeadphones); headphonesBare = Retarget(from, surf); headphonesMats = oldHeadphones.sharedMaterials;
                headphonesMesh = Asset(SurfaceToMesh(headphonesBare, who + "Bare_Headphones"), who + "Bare_Headphones.asset");
            }
            var shoeBare = new Dictionary<string, Mesh>();
            foreach (var kv in airMeshes) { var from = new Surface(root, oldShoesR, kv.Value); shoeBare[kv.Key] = Asset(SurfaceToMesh(Retarget(from, surf), who + "Bare_AM" + kv.Key), who + "Bare_AM" + kv.Key + ".asset"); }

            // 3. wardrobe on the new rig
            bareRig = true; sacatBareArms = true; sacatBareTorso = false; sacatOriginalTorso = false;
            try
            {
                var shirtFaces = BareShirtFaces(surf);
                var skin = AssetDatabase.LoadAssetAtPath<Material>(Art + "/" + who + "_Skin.mat") ?? Matte(who + "_Skin", who == "Sacat" ? new Color(.34f, .205f, .135f) : new Color(.412f, .243f, .153f));
                var fabric = Matte("Cotton", Color.white); var denim = Matte("Denim", Color.white); var trim = Matte("Stitch", new Color(.48f, .42f, .29f)); var detail = Matte("Pearl", new Color(.78f, .79f, .75f));
                var w = root.GetComponent<OutfitWardrobe>(); var pieces = new List<OutfitPiece>(); var bindings = new List<OutfitBinding>(); var prevDefaults = w.defaults;
                void Piece(string id, string label, OutfitSlot slot, Shape shape, Material[] mats, int[] tint) { pieces.Add(new OutfitPiece { id = id, label = label, slot = slot, mesh = shape == null ? null : Asset(shape.Mesh(who + "Bare_" + id), who + "Bare_" + id + ".asset"), materials = mats, tintSlots = tint }); }
                void RemoveOld(string name) { var t = root.transform.Find(name); if (t != null) UnityEngine.Object.DestroyImmediate(t.gameObject); }
                foreach (string n in new[] { "WardrobeBody", "WardrobeShirt", "WardrobePants", "WardrobeHat", "WardrobeShoes", "WardrobeArms", "WardrobeTorso" }) RemoveOld(n);
                bindings.Add(new OutfitBinding { slot = OutfitSlot.Shirt, renderer = Bind(root, "WardrobeShirt", surf) });
                Piece("shirt_tee_mike", "Mike Crew Tee", OutfitSlot.Shirt, Shirt(surf, shirtFaces, false), new[] { fabric, skin, detail, trim }, new[] { 0 });
                Piece("shirt_polo_lacos", "Lacos Polo", OutfitSlot.Shirt, Shirt(surf, shirtFaces, true), new[] { fabric, skin, detail, trim }, new[] { 0 });
                bindings.Add(new OutfitBinding { slot = OutfitSlot.Pants, renderer = Bind(root, "WardrobePants", surf) });
                Piece("pants_jeans", "Straight Jeans", OutfitSlot.Pants, ShellShape(surf, BarePantsFaces(surf, 0)), new[] { denim, skin, trim, detail }, new[] { 0 });
                Piece("pants_trousers", "Tailored Trousers", OutfitSlot.Pants, ShellShape(surf, BarePantsFaces(surf, 1)), new[] { fabric, skin, trim, detail }, new[] { 0 });
                Piece("pants_shorts_denim", "Denim Shorts", OutfitSlot.Pants, ShellShape(surf, BarePantsFaces(surf, 2)), new[] { denim, skin, trim, detail }, new[] { 0 });
                bindings.Add(new OutfitBinding { slot = OutfitSlot.Hat, renderer = Bind(root, "WardrobeHat", surf) });
                Piece("hat_none", "No Hat", OutfitSlot.Hat, null, Array.Empty<Material>(), Array.Empty<int>());
                Piece("hat_lacos", "Lacos Curved Cap", OutfitSlot.Hat, Cap(surf), new[] { fabric, Matte("CapSeams", new Color(.17f, .19f, .18f)), detail, trim }, new[] { 0 });
                bindings.Add(new OutfitBinding { slot = OutfitSlot.Shoes, renderer = Bind(root, "WardrobeShoes", surf) });
                var shoeMats = new[] { Matte("ShoeUpper", Color.white, .14f), Matte("Sole", new Color(.8f, .8f, .76f)), Matte("Rubber", new Color(.035f, .04f, .044f)), Matte("ShoeAccent", new Color(.55f, .035f, .04f), .2f), Matte("Laces", new Color(.78f, .79f, .77f)) };
                Piece("shoes_mike90", "Mike 90", OutfitSlot.Shoes, Shoes(surf, 0), shoeMats, new[] { 0 });
                Piece("shoes_mike97", "Mike 97", OutfitSlot.Shoes, Shoes(surf, 1), shoeMats, new[] { 0 });
                Piece("shoes_mike270", "Mike 270", OutfitSlot.Shoes, Shoes(surf, 2), shoeMats, new[] { 0 });
                foreach (var kv in shoeBare)
                {
                    var piece = pieces.Single(p => p.id == "shoes_mike" + kv.Key); piece.mesh = kv.Value;
                    piece.materials = new[] { AssetDatabase.LoadAssetAtPath<Material>(AirArt + "/AM" + kv.Key + "_0.mat"), AssetDatabase.LoadAssetAtPath<Material>(AirArt + "/AM" + kv.Key + "_1.mat"), AssetDatabase.LoadAssetAtPath<Material>(AirArt + "/AM" + kv.Key + "_2.mat"), skin }; piece.tintSlots = new[] { 0 };
                }
                w.pieces = pieces.ToArray(); w.bindings = bindings.ToArray(); w.defaults = prevDefaults;
                // headphones accessory as a sibling renderer on the new rig
                GameObject newHeadphones = null;
                if (headphonesMesh != null)
                {
                    var hp = Bind(root, "HeadphonesAccessory_Bare", surf); hp.sharedMesh = headphonesMesh; hp.sharedMaterials = headphonesMats; hp.updateWhenOffscreen = true; newHeadphones = hp.gameObject;
                }
                // 4. repoint references from the old rig
                RepointReferences(root, oldVisual.gameObject, oldAnimator, newAnimator, oldHeadphones != null ? oldHeadphones.gameObject : null, newHeadphones, oldPose, newPose);
                // 5. remove the old rig, rename the new visual
                UnityEngine.Object.DestroyImmediate(oldVisual.gameObject);
                body.name = "Visual";
                foreach (var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)) r.updateWhenOffscreen = true;
                w.Restore(null); EditorUtility.SetDirty(w);
            }
            finally { bareRig = false; sacatBareArms = false; }
        }

        static Shape ShellShape(Surface s, List<List<V>> faces) { var shape = new Shape(s); foreach (var f in faces) shape.Poly(f, 0); return shape; }

        // ---- reference repointing ---------------------------------------------------------------------------------------------
        static void RepointReferences(GameObject root, GameObject oldVisual, Animator oldA, Animator newA, GameObject oldHp, GameObject newHp, Component oldPose, Component newPose)
        {
            var map = new Dictionary<UnityEngine.Object, UnityEngine.Object> { { oldA, newA }, { oldVisual, newA.gameObject }, { oldVisual.transform, newA.transform } };
            if (oldPose != null) map[oldPose] = newPose; if (oldHp != null && newHp != null) { map[oldHp] = newHp; map[oldHp.transform] = newHp.transform; }
            // old skeleton transforms -> new bones through the Humanoid bone ids
            foreach (HumanBodyBones hb in Enum.GetValues(typeof(HumanBodyBones))) { if (hb == HumanBodyBones.LastBone) continue; var o = oldA.GetBoneTransform(hb); var n = newA.GetBoneTransform(hb); if (o != null && n != null) { map[o] = n; map[o.gameObject] = n.gameObject; } }
            int fixedRefs = 0; var unresolved = new List<string>();
            foreach (var mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb == null || mb.transform.IsChildOf(oldVisual.transform)) continue;
                var so = new SerializedObject(mb); var it = so.GetIterator(); bool changed = false;
                while (it.Next(true))
                {
                    if (it.propertyType != SerializedPropertyType.ObjectReference || it.objectReferenceValue == null) continue;
                    var v = it.objectReferenceValue;
                    if (map.TryGetValue(v, out var nv)) { it.objectReferenceValue = nv; changed = true; fixedRefs++; }
                    else
                    {
                        Transform tv = v as Transform ?? (v as GameObject)?.transform ?? (v as Component)?.transform;
                        if (tv != null && tv != oldVisual.transform && tv.IsChildOf(oldVisual.transform)) unresolved.Add(mb.GetType().Name + "." + it.propertyPath + " -> " + v.name);
                    }
                }
                if (changed) so.ApplyModifiedPropertiesWithoutUndo();
            }
            Debug.Log("MINI205 repointed " + fixedRefs + " references; unresolved: " + (unresolved.Count == 0 ? "none" : string.Join("; ", unresolved.ToArray())));
        }
    }
}
