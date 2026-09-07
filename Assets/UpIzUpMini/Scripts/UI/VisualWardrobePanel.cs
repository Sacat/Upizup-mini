using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UpIzUpMini.Character;

namespace UpIzUpMini.UI
{
    // No scene regeneration or character clones with live gameplay scripts.
    public sealed class VisualWardrobePanel : MonoBehaviour
    {
        static VisualWardrobePanel instance;
        CharacterEquipment wearer;
        List<string> original;
        GameObject stage,model;
        Camera previewCamera;
        RenderTexture texture;
        readonly List<Mesh> meshes=new List<Mesh>();
        readonly List<Canvas> hiddenCanvases=new List<Canvas>();
        float oldTime,yaw;
        bool oldCursor;
        CursorLockMode oldLock;
        int category;
        string who;
        GUIStyle title,label,small,button;
        readonly string[] categories={"Accessories","Shirts","Pants","Hats","Shoes"};
        public static bool IsOpen=>instance!=null&&instance.wearer!=null;
        public static void Open(CharacterEquipment equipment)
        {
            if(equipment==null||IsOpen||Time.timeScale==0)return;
            if(instance==null)instance=new GameObject("Visual Wardrobe").AddComponent<VisualWardrobePanel>();
            instance.Begin(equipment);
        }
        void Begin(CharacterEquipment equipment)
        {
            wearer=equipment;original=wearer.CaptureTrialItems();
            who=CharacterSwitchManager.Instance?.Active.displayName??"Character";
            oldTime=Time.timeScale;Time.timeScale=0;
            oldCursor=Cursor.visible;oldLock=Cursor.lockState;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            category=0;yaw=180;
            foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if(canvas.enabled){hiddenCanvases.Add(canvas);canvas.enabled=false;}
            stage=new GameObject("Wardrobe render stage");stage.transform.position=new Vector3(10000,-10000,10000);
            texture=new RenderTexture(512,640,24){antiAliasing=2};texture.Create();
            previewCamera=new GameObject("Wardrobe camera").AddComponent<Camera>();previewCamera.transform.SetParent(stage.transform,false);
            previewCamera.enabled=false;previewCamera.cullingMask=1<<31;previewCamera.clearFlags=CameraClearFlags.SolidColor;previewCamera.backgroundColor=new Color(.09f,.12f,.15f);previewCamera.targetTexture=texture;previewCamera.nearClipPlane=.01f;
            foreach(float angle in new[]{-35f,145f})
            {
                var light=new GameObject("Wardrobe light").AddComponent<Light>();light.transform.SetParent(stage.transform,false);light.type=LightType.Directional;light.intensity=angle<0?1.2f:.6f;light.cullingMask=1<<31;light.transform.rotation=Quaternion.Euler(30,angle,0);
            }
            RebuildPreview();
        }
        void RebuildPreview()
        {
            if(model!=null){model.SetActive(false);Destroy(model);}
            foreach(var mesh in meshes)Destroy(mesh);meshes.Clear();
            model=new GameObject("Static character preview");model.transform.SetParent(stage.transform,false);
            Bounds bounds=new Bounds();bool found=false;
            foreach(var r in wearer.GetComponentsInChildren<Renderer>())
            {
                if(!r.enabled||!r.gameObject.activeInHierarchy||r is ParticleSystemRenderer||r is TrailRenderer||r is LineRenderer)continue;
                // Only first LOD renders in the portrait; skip duplicate lower meshes.
                var group=r.GetComponentInParent<LODGroup>();
                if(group!=null&&group.GetLODs().Length>0&&!group.GetLODs()[0].renderers.Contains(r))continue;
                Mesh mesh=null;
                if(r is SkinnedMeshRenderer skinned){mesh=new Mesh();skinned.BakeMesh(mesh);}
                else
                {
                    var filter=r.GetComponent<MeshFilter>();
                    if(filter!=null&&filter.sharedMesh!=null)
                    {
                        // GPU-only accessory meshes need no Read/Write copy. Preserve
                        // their local transform, rather than reading unavailable vertices.
                        var transformMatrix=wearer.transform.worldToLocalMatrix*r.transform.localToWorldMatrix;
                        var partStatic=new GameObject(r.name);partStatic.layer=31;partStatic.transform.SetParent(model.transform,false);
                        partStatic.transform.localPosition=transformMatrix.GetColumn(3);partStatic.transform.localRotation=transformMatrix.rotation;partStatic.transform.localScale=transformMatrix.lossyScale;
                        partStatic.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;
                        var staticRenderer=partStatic.AddComponent<MeshRenderer>();staticRenderer.sharedMaterials=r.sharedMaterials;
                        var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);staticRenderer.SetPropertyBlock(block);
                    }
                    continue;
                }
                if(mesh==null)continue;
                var matrix=wearer.transform.worldToLocalMatrix*r.transform.localToWorldMatrix;
                var vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++)vertices[i]=matrix.MultiplyPoint3x4(vertices[i]);mesh.vertices=vertices;
                var normals=mesh.normals;var normalMatrix=matrix.inverse.transpose;for(int i=0;i<normals.Length;i++)normals[i]=normalMatrix.MultiplyVector(normals[i]).normalized;mesh.normals=normals;mesh.RecalculateBounds();
                meshes.Add(mesh);
                var part=new GameObject(r.name);part.layer=31;part.transform.SetParent(model.transform,false);part.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=part.AddComponent<MeshRenderer>();renderer.sharedMaterials=r.sharedMaterials;
                var properties=new MaterialPropertyBlock();r.GetPropertyBlock(properties);renderer.SetPropertyBlock(properties);
                if(!found){bounds=mesh.bounds;found=true;}else bounds.Encapsulate(mesh.bounds);
            }
            if(!found)return;
            model.transform.localPosition=-bounds.center;
            model.transform.localRotation=Quaternion.Euler(0,yaw,0);
            // Rotate around body centre, not world feet.
            model.transform.localPosition=-(model.transform.localRotation*bounds.center);
            previewCamera.orthographic=true;previewCamera.orthographicSize=Mathf.Max(.8f,bounds.size.y*.58f);
            previewCamera.transform.localPosition=new Vector3(0,0,4);previewCamera.transform.LookAt(stage.transform.position);
            previewCamera.Render();
        }
        void Select(int item)
        {
            wearer.SetTrialItem(CharacterEquipment.TrialItemIds[item],true);RebuildPreview();
        }
        public void Close(bool apply)
        {
            if(wearer==null)return;
            if(!apply)wearer.RestoreTrialItems(original);
            wearer=null;Time.timeScale=oldTime;Cursor.lockState=oldLock;Cursor.visible=oldCursor;
            foreach(var canvas in hiddenCanvases)if(canvas!=null)canvas.enabled=true;
            hiddenCanvases.Clear();
            if(stage!=null){stage.SetActive(false);Destroy(stage);}
            foreach(var mesh in meshes)Destroy(mesh);meshes.Clear();
            if(texture!=null){texture.Release();Destroy(texture);texture=null;}
        }
        void OnDestroy(){Close(false);if(instance==this)instance=null;}
        void Update()
        {
            if(wearer!=null&&CharacterSwitchManager.Instance!=null&&CharacterSwitchManager.Instance.Active.root!=wearer.gameObject)Close(false);
        }
        void OnGUI()
        {
            if(wearer==null)return;
            if(title==null)
            {
                title=new GUIStyle(GUI.skin.label){fontSize=30,fontStyle=FontStyle.Bold};
                label=new GUIStyle(GUI.skin.label){fontSize=19,wordWrap=true};
                small=new GUIStyle(GUI.skin.label){fontSize=15,wordWrap=true};
                button=new GUIStyle(GUI.skin.button){fontSize=18,wordWrap=true};
            }
            int depth=GUI.depth;GUI.depth=-32000;
            var previousColor=GUI.color;GUI.color=Color.white;
            var old=GUI.matrix;var safe=Screen.safeArea;float scale=Mathf.Min(safe.width/1100f,safe.height/700f);
            GUI.matrix=Matrix4x4.identity;
            GUI.color=new Color(.055f,.065f,.08f,1f);
            GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);
            GUI.color=Color.white;
            GUI.matrix=Matrix4x4.TRS(new Vector3(safe.x+(safe.width-1100*scale)/2,Screen.height-safe.yMax+(safe.height-700*scale)/2,0),Quaternion.identity,new Vector3(scale,scale,1));
            GUI.Label(new Rect(30,20,700,45),"WARDROBE  /  "+who.ToUpperInvariant(),title);
            GUI.Label(new Rect(30,66,950,28),"FREE TEST MODE  •  No purchases needed  •  Changes last for this session",small);
            GUI.DrawTexture(new Rect(30,112,410,500),texture,ScaleMode.ScaleToFit);
            if(GUI.Button(new Rect(55,620,165,40),"Rotate left",button)){yaw-=30;RebuildPreview();}
            if(GUI.Button(new Rect(245,620,165,40),"Rotate right",button)){yaw+=30;RebuildPreview();}
            for(int i=0;i<categories.Length;i++)
                if(GUI.Button(new Rect(465+i*121,110,116,46),categories[i],button))category=i;
            GUI.Label(new Rect(470,175,570,32),categories[category],title);
            if(category==0)
            {
                GUI.Label(new Rect(470,220,570,45),"Choose an available accessory to see it on your character.",label);
                for(int i=0;i<CharacterEquipment.TrialItemIds.Length;i++)
                    if(GUI.Button(new Rect(470,280+i*68,575,56),CharacterEquipment.TrialItemLabels[i]+"  /  TRY ON",button))Select(i);
                if(GUI.Button(new Rect(470,490,575,40),"Restore opening outfit",button)){wearer.RestoreTrialItems(original);RebuildPreview();}
                GUI.Label(new Rect(470,542,575,48),"Approved gold watch fit stays unchanged. Cap and shades are existing prototype models.",small);
            }
            else
            {
                string[] descriptions={"","Lacos polo / Mike T-shirt","Long jeans / denim shorts / trousers","Lacos curved-brim cap","Mike 90 / Mike 97"};
                GUI.Label(new Rect(470,235,570,60),descriptions[category],label);
                GUI.Label(new Rect(470,310,570,110),"IN PRODUCTION\nThe approved new models are not fitted yet. Existing clothes remain on the character.",label);
                GUI.Label(new Rect(470,445,570,90),"Colour selection will be enabled when these garments have colour masks. This is not a working clothing swap yet.",small);
            }
            if(GUI.Button(new Rect(665,622,175,45),"Cancel",button))Close(false);
            if(GUI.Button(new Rect(855,622,190,45),"Apply try-ons",button))Close(true);
            GUI.matrix=old;GUI.color=previousColor;
        }
        // Explicit automated LIVE player evidence path; never runs during normal play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void ProofBootstrap()
        {
            var args=System.Environment.GetCommandLineArgs();int index=System.Array.IndexOf(args,"-wardrobe-proof");
            if(index<0||index+1>=args.Length)return;
            Application.runInBackground=true;
            instance=new GameObject("Visual Wardrobe Proof").AddComponent<VisualWardrobePanel>();instance.StartCoroutine(instance.CaptureProof(args[index+1]));
        }
        IEnumerator CaptureProof(string path)
        {
            yield return new WaitForSecondsRealtime(3);
            var equipment=CharacterSwitchManager.Instance?.Active.root.GetComponent<CharacterEquipment>();
            if(equipment==null){Debug.LogError("WARDROBE_PROOF_NO_CHARACTER");Application.Quit(1);yield break;}
            Begin(equipment);Select(0);
            yield return new WaitForSecondsRealtime(1);
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSecondsRealtime(2);
            Close(false);Debug.Log("WARDROBE_LIVE_PROOF_COMPLETE");Application.Quit();
        }
    }
}
