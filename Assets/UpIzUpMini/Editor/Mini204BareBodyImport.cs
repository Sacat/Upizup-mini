using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-204: imports the cloud lane's final bare bodies (MINI-197: SacatBare_04NeckTorso, FrankiBare) into an isolated candidate folder as Humanoid,
    /// builds candidate prefabs with one shared Standard material, and writes an import report plus fixed-camera renders. Never touches any game scene.
    /// </summary>
    public static class Mini204BareBodyImport
    {
        const string Src = "Docs/CharacterPipeline/MINI-197/BareBody";
        const string Dst = "Assets/UpIzUpMini/Art/Characters/Modular/Bare";
        const string Out = "Logs/Tasks/MINI-204";

        static readonly (string name, string fbx, string png)[] Bodies =
        {
            ("SacatBare", Src + "/04-NeckTorso/SacatBare_04NeckTorso.fbx", Src + "/04-NeckTorso/SacatBare_04NeckTorso_BaseColor.png"),
            ("FrankiBare", Src + "/05-Franki/FrankiBare.fbx", Src + "/05-Franki/FrankiBare_BaseColor.png"),
        };

        [MenuItem("Up Iz Up Mini/MINI-204/Import Bare Bodies")]
        public static void Run()
        {
            try
            {
                Directory.CreateDirectory(Dst); Directory.CreateDirectory(Out);
                var report = new StringBuilder();
                foreach (var b in Bodies) { AssetDatabase.DeleteAsset(Dst + "/" + b.name + ".fbx"); }   // fresh importer settings every run
                foreach (var b in Bodies)
                {
                    File.Copy(b.fbx, Dst + "/" + b.name + ".fbx", true); File.Copy(b.png, Dst + "/" + b.name + "_BaseColor.png", true);
                }
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                foreach (var b in Bodies)
                {
                    string tex = Dst + "/" + b.name + "_BaseColor.png", fbx = Dst + "/" + b.name + ".fbx";
                    var ti = (TextureImporter)AssetImporter.GetAtPath(tex); ti.sRGBTexture = true; ti.maxTextureSize = 1024; ti.mipmapEnabled = true; ti.SaveAndReimport();
                    var mi = (ModelImporter)AssetImporter.GetAtPath(fbx);
                    mi.globalScale = 1f; mi.useFileScale = true; mi.importNormals = ModelImporterNormals.Import; mi.materialImportMode = ModelImporterMaterialImportMode.None;
                    mi.animationType = ModelImporterAnimationType.Human; mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel; mi.importAnimation = false;
                    mi.SaveAndReimport();
                    // Mecanim auto-mapped Head to NeckTwist02 (a twist bone); the real head joint is CC_Base_Head, and the chest chain needs Spine02 as UpperChest
                    {
                        var hd = mi.humanDescription; var list = hd.human.ToList();
                        void SetBone(string humanName, string boneName) { int ix = list.FindIndex(x => x.humanName == humanName); var hb = new HumanBone { humanName = humanName, boneName = boneName, limit = new HumanLimit { useDefaultValues = true } }; if (ix >= 0) list[ix] = hb; else list.Add(hb); }
                        SetBone("Neck", "CC_Base_NeckTwist01"); SetBone("Head", "CC_Base_Head"); list.RemoveAll(x => (x.humanName == "LeftEye" || x.humanName == "RightEye" || x.humanName == "Jaw") && x.boneName == "CC_Base_Head");
                        hd.human = list.ToArray(); mi.humanDescription = hd; mi.SaveAndReimport();
                    }
                    var avatar = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<Avatar>().FirstOrDefault();
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
                    report.AppendLine("== " + b.name);
                    report.AppendLine("avatar valid=" + (avatar != null && avatar.isValid) + " human=" + (avatar != null && avatar.isHuman));
                    var desc = mi.humanDescription; report.AppendLine("mapped human bones=" + desc.human.Length);
                    string[] required = { "Hips", "Spine", "Chest", "UpperChest", "Neck", "Head", "Left Upper Leg", "Left Lower Leg", "Left Foot", "Left Upper Arm", "Left Lower Arm", "Left Hand", "Right Upper Leg", "Right Lower Leg", "Right Foot", "Right Upper Arm", "Right Lower Arm", "Right Hand", "Left Index Proximal", "Left Thumb Proximal", "Left Little Proximal", "Right Index Proximal", "Right Thumb Proximal", "Right Little Proximal" };
                    foreach (var r in required) { var h = desc.human.FirstOrDefault(x => x.humanName == r); report.AppendLine("  " + r + " -> " + (h.boneName ?? "MISSING")); }
                    var rends = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    int tris = 0; foreach (var r in rends) tris += r.sharedMesh.triangles.Length / 3;
                    var bounds = new Bounds(rends[0].bounds.center, Vector3.zero); foreach (var r in rends) bounds.Encapsulate(r.bounds);
                    report.AppendLine("meshes=" + string.Join(",", rends.Select(r => r.name)) + " tris=" + tris + " boundsCentre=" + bounds.center.ToString("F3") + " size=" + bounds.size.ToString("F3") + " rootScale=" + model.transform.localScale + " rootRot=" + model.transform.eulerAngles);
                    // candidate prefab with a shared material
                    string matPath = Dst + "/" + b.name + "_Skin.mat"; var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (mat == null) { mat = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(mat, matPath); }
                    mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(tex); mat.color = Color.white; mat.SetFloat("_Glossiness", .22f); mat.SetFloat("_Metallic", 0f); EditorUtility.SetDirty(mat);
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
                    foreach (var r in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { var ms = new Material[r.sharedMaterials.Length]; for (int i = 0; i < ms.Length; i++) ms[i] = mat; r.sharedMaterials = ms; r.updateWhenOffscreen = true; }
                    PrefabUtility.SaveAsPrefabAsset(inst, Dst + "/" + b.name + ".prefab"); UnityEngine.Object.DestroyImmediate(inst);
                }
                AssetDatabase.SaveAssets();
                File.WriteAllText(Out + "/import_report.txt", report.ToString());
                RenderSheet();
                Debug.Log("MINI204_IMPORT_PASS");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        static void RenderSheet()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var light = new GameObject("L").AddComponent<Light>(); light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(35, 30, 0); light.intensity = 1.1f;
            RenderSettings.ambientLight = new Color(.6f, .6f, .6f);
            foreach (var b in Bodies)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Dst + "/" + b.name + ".prefab"); var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                var cam = new GameObject("c").AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.6f, .72f, .85f); cam.fieldOfView = 24;
                Vector3 c = new Vector3(0, .95f, 0); string[] names = { "front", "side", "back", "tq" };
                Vector3[] dirs = { Vector3.forward, Vector3.right, Vector3.back, (Vector3.forward + Vector3.right).normalized };
                for (int i = 0; i < 4; i++)
                {
                    cam.transform.position = c + dirs[i] * 5.2f; cam.transform.LookAt(c);
                    var rt = new RenderTexture(560, 900, 24); cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
                    var tx = new Texture2D(560, 900, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 560, 900), 0, 0); tx.Apply();
                    File.WriteAllBytes(Out + "/" + b.name + "_" + names[i] + ".png", tx.EncodeToPNG()); RenderTexture.active = null; cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(rt);
                }
                UnityEngine.Object.DestroyImmediate(cam.gameObject); UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
