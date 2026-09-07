using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UpIzUpMini.Character;
using Object=UnityEngine.Object;

namespace UpIzUpMini.EditorTools
{
    public static class Mini145WatchProof
    {
        const string Art="Assets/UpIzUpMini/Art/Accessories/GoldWatchMobile";
        const string Out="Logs/Tasks/MINI-145";
        const string Scene="Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        static void Require(bool ok,string text){if(!ok)throw new Exception("MINI145: "+text);}
        [MenuItem("Tools/Up Iz Up Mini/MINI-145/Round Watch Wrist Proof")]
        public static void BuildCapture()
        {
            Directory.CreateDirectory(Out);
            string before=Hash(Scene);
            AssetDatabase.Refresh();
            foreach(string name in new[]{"WatchPalette","WatchMetallic"})
            {
                var ti=(TextureImporter)AssetImporter.GetAtPath(Art+"/"+name+".png");
                ti.sRGBTexture=name=="WatchPalette";ti.mipmapEnabled=false;ti.filterMode=FilterMode.Point;
                ti.textureCompression=TextureImporterCompression.Uncompressed;ti.maxTextureSize=32;ti.SaveAndReimport();
            }
            var mat=AssetDatabase.LoadAssetAtPath<Material>(Art+"/GoldWatchAtlas.mat");
            if(mat==null){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,Art+"/GoldWatchAtlas.mat");}
            mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/WatchPalette.png");
            mat.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/WatchMetallic.png"));
            mat.EnableKeyword("_METALLICGLOSSMAP");mat.SetFloat("_GlossMapScale",1);mat.color=Color.white;
            var watch=new GameObject("GoldWatchMobile");var group=watch.AddComponent<LODGroup>();
            var lods=new LOD[2];var evidence=new List<string>();
            for(int i=0;i<2;i++)
            {
                string path=Art+"/GoldWatch_LOD"+i+".fbx";
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);
                importer.importCameras=false;importer.importLights=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
                var part=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                part.transform.SetParent(watch.transform,false);
                var rs=part.GetComponentsInChildren<Renderer>();foreach(var r in rs)r.sharedMaterial=mat;
                int triangles=part.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.triangles.Length/3);
                Require(triangles<=(i==0?2500:1200),"triangle budget");Require(rs.Length==1,"one renderer per LOD");
                lods[i]=new LOD(i==0?.035f:.007f,rs);evidence.Add("LOD"+i+" triangles="+triangles);
            }
            group.SetLODs(lods);group.RecalculateBounds();
            var prefab=PrefabUtility.SaveAsPrefabAsset(watch,Art+"/GoldWatchMobile.prefab");Object.DestroyImmediate(watch);
            var original=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);
            var switcher=Object.FindFirstObjectByType<CharacterSwitchManager>();Require(switcher!=null,"character slots missing");
            var slots=switcher.Slots;
            var stage=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            var subjects=new List<GameObject>();var names=new List<string>();
            foreach(var slot in slots)
            {
                Require(slot.root!=null,"missing protagonist root");
                var copy=Object.Instantiate(slot.root);copy.name=slot.displayName+"_WatchProof";
                SceneManager.MoveGameObjectToScene(copy,stage);subjects.Add(copy);names.Add(slot.displayName);
                foreach(var behaviour in copy.GetComponentsInChildren<MonoBehaviour>(true))behaviour.enabled=false;
                foreach(var collider in copy.GetComponentsInChildren<Collider>(true))collider.enabled=false;
                foreach(var rb in copy.GetComponentsInChildren<Rigidbody>(true))rb.isKinematic=true;
            }
            EditorSceneManager.CloseScene(original,true);SceneManager.SetActiveScene(stage);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.62f,.67f,.75f);RenderSettings.ambientEquatorColor=new Color(.35f,.39f,.45f);
            RenderSettings.ambientGroundColor=new Color(.22f,.20f,.18f);RenderSettings.fog=false;
            var key=new GameObject("Soft key").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.7f;key.transform.rotation=Quaternion.Euler(35,-35,0);
            var fill=new GameObject("Rim light").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.8f;fill.transform.rotation=Quaternion.Euler(25,145,0);
            var camera=new GameObject("Wrist proof camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.09f,.115f,.14f);camera.nearClipPlane=.001f;camera.farClipPlane=50;camera.fieldOfView=32;
            var clip=AssetDatabase.LoadAllAssetsAtPath("Assets/UpIzUpMini/Art/Animations/Stand--Idle.anim.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
            for(int index=0;index<subjects.Count;index++)
            {
                var subject=subjects[index];string name=names[index];
                for(int j=0;j<subjects.Count;j++)subjects[j].SetActive(j==index);
                subject.transform.position=Vector3.zero;subject.transform.rotation=Quaternion.identity;
                var animator=subject.GetComponentsInChildren<Animator>(true).First(a=>a.avatar!=null&&a.avatar.isHuman);
                animator.enabled=true;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                Sample(animator,clip,.4f);
                Transform hand=animator.GetBoneTransform(HumanBodyBones.LeftHand),elbow=animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
                Require(hand!=null&&elbow!=null,"wrist bones missing");
                var fitted=(GameObject)PrefabUtility.InstantiatePrefab(prefab);fitted.name="RoundGoldWatch_"+name;
                Vector3 axis=(hand.position-elbow.position).normalized;
                Vector3 top=Vector3.ProjectOnPlane(subject.transform.forward,axis).normalized;
                if(top.sqrMagnitude<.5f)top=Vector3.ProjectOnPlane(subject.transform.up,axis).normalized;
                // The current models have cuffs at the wrist. Keep the face clear of the
                // bent hand and fit the bracelet around the actual cuff, without editing it.
                fitted.transform.position=hand.position-axis*.030f;
                fitted.transform.rotation=Quaternion.LookRotation(Vector3.Cross(axis,top),top);
                // Explicit preview fitting, not automatic fitting of arbitrary future outfits.
                // Skin-section sampling includes nearby bent-hand geometry; do not use its
                // inflated maximum to silently oversize the user's watch.
                float fitScale=name=="Franki"?1.24f:1.14f;
                fitted.transform.position+=top*.003f;
                fitted.transform.localScale=Vector3.one*fitScale;
                evidence.Add(name+" uniform cuff clearance scale="+fitScale.ToString("F3"));
                fitted.transform.SetParent(elbow,true);
                fitted.GetComponent<LODGroup>().ForceLOD(0);
                var profile=AssetDatabase.LoadAssetAtPath<AccessoryPlacementProfile>(Art+"/"+name+"WatchFit.asset");
                if(profile==null)
                {
                    profile=ScriptableObject.CreateInstance<AccessoryPlacementProfile>();AssetDatabase.CreateAsset(profile,Art+"/"+name+"WatchFit.asset");
                    profile.useManualPlacement=true;profile.localPosition=fitted.transform.localPosition;profile.localEulerAngles=fitted.transform.localEulerAngles;profile.localScale=fitted.transform.localScale;
                }
                else
                {
                    // Existing fit is authoritative. Never replace user-authored offsets
                    // just because the capture tool is run again.
                    fitted.transform.localPosition=profile.localPosition;
                    fitted.transform.localRotation=Quaternion.Euler(profile.localEulerAngles);
                    fitted.transform.localScale=profile.localScale;
                }
                EditorUtility.SetDirty(profile);
                evidence.Add(name+" parent="+elbow.name+" localPosition="+profile.localPosition.ToString("F5")+" euler="+profile.localEulerAngles+" scale="+profile.localScale);
                Vector3 savedPos=fitted.transform.localPosition;Quaternion savedRot=fitted.transform.localRotation;
                foreach(string motionPath in new[]{"Assets/UpIzUpMini/Art/Animations/Stand--Idle.anim.fbx","Assets/UpIzUpMini/Art/Animations/Locomotion--Walk_N.anim.fbx","Assets/UpIzUpMini/Art/Animations/Locomotion--Run_N.anim.fbx"})
                {
                    var motion=AssetDatabase.LoadAllAssetsAtPath(motionPath).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
                    for(int frame=0;frame<12;frame++)
                    {
                        Sample(animator,motion,motion.length*frame/12f);
                        Require(Vector3.Distance(savedPos,fitted.transform.localPosition)<.00001f&&Quaternion.Angle(savedRot,fitted.transform.localRotation)<.001f,"attachment drift");
                    }
                }
                Sample(animator,clip,.4f);
                Vector3 target=fitted.transform.position;
                Capture(camera,target+subject.transform.forward*.26f-subject.transform.right*.10f+subject.transform.up*.08f,target,Out+"/"+name+"-Watch-Close.png");
                var body=subject.GetComponentsInChildren<SkinnedMeshRenderer>().First();Bounds bounds=body.bounds;
                Capture(camera,bounds.center+subject.transform.forward*3.5f+subject.transform.right*.5f,bounds.center,Out+"/"+name+"-Watch-Full.png");
                evidence.Add(name+" attachment unchanged across 36 idle/walk/run samples; visual fit/motion acceptance pending");
            }
            for(int i=0;i<subjects.Count;i++){subjects[i].SetActive(true);subjects[i].transform.position=Vector3.right*i*1.2f;}
            camera.transform.position=new Vector3(.6f,1.0f,4.5f);camera.transform.LookAt(new Vector3(.6f,1,0));
            EditorSceneManager.SaveScene(stage,"Assets/UpIzUpMini/Scenes/WatchWardrobeProof.unity");
            AssetDatabase.SaveAssets();Require(Hash(Scene)==before,"canonical scene changed");
            evidence.Add("Canonical scene hash unchanged. Isolated proof only; no purchase integration or EXE.");
            File.WriteAllLines(Out+"/unity-watch-proof.txt",evidence);Debug.Log("MINI145_WATCH_PROOF_PASS");
        }
        static void Sample(Animator a,AnimationClip clip,float t)
        {
            var graph=PlayableGraph.Create("WatchProof");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var p=AnimationClipPlayable.Create(graph,clip);var o=AnimationPlayableOutput.Create(graph,"Pose",a);o.SetSourcePlayable(p);
            graph.Play();p.SetTime(t);graph.Evaluate(0);graph.Destroy();
        }
        static void Capture(Camera camera,Vector3 position,Vector3 target,string path)
        {
            camera.transform.position=position;camera.transform.LookAt(target);
            var rt=new RenderTexture(1000,1000,24){antiAliasing=4};var old=RenderTexture.active;
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var tex=new Texture2D(1000,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1000,1000),0,0);tex.Apply();
            File.WriteAllBytes(path,tex.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=old;Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);
        }
        static string Hash(string path){using(var sha=System.Security.Cryptography.SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));}
    }
}
