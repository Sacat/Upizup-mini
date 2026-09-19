using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace UpIzUpMini.EditorTools {
public static class Mini173LalayRevision {
 const string Scene="Assets/UpIzUpMini/Scenes/GrandBayProof_ExpansionImport.unity", Out="Logs/Tasks/MINI-173", Art="Assets/UpIzUpMini/Art/Environment/Mini173";
 static Material trim,glass,roof,baseMat,rail;
 public static void Apply(){try{
 Directory.CreateDirectory(Art);Directory.CreateDirectory(Out);AssetDatabase.Refresh();
 var scene=EditorSceneManager.OpenScene(Scene);
 trim=Mat("CreamTrim",new Color(.84f,.81f,.69f));glass=Mat("ShadedWindows",new Color(.08f,.17f,.20f));roof=Mat("MetalRoof",new Color(.40f,.29f,.24f));baseMat=Mat("Masonry",new Color(.40f,.40f,.35f));rail=Mat("Railings",new Color(.67f,.72f,.72f));
 var old=GameObject.Find("MINI173_LalayHomes");if(old)Object.DestroyImmediate(old);
 var root=new GameObject("MINI173_LalayHomes");
 var houses=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Lalay_Shanty_")).OrderBy(t=>t.name).ToArray();
 int n=0;var report=new List<string>();
 foreach(var h in houses){h.gameObject.SetActive(false);var go=new GameObject(h.name.Replace("Lalay_Shanty_","Lalay_Home_"));go.transform.SetParent(root.transform);go.transform.SetPositionAndRotation(h.position,h.rotation);
 // Compressed street lots: keep new width at 2.7m to avoid the old 6-12m FBX footprints crossing neighbours.
 BuildHouse(go.transform,n++);report.Add(h.name+" => "+go.name+" at "+h.position);}
 var field=GameObject.Find("GenevaField").transform;
 string[] remove={"PitchLine","GoalPost","GoalCrossbar","GoalNet","MownStripe"};int removed=0;
 foreach(var t in field.GetComponentsInChildren<Transform>().Where(t=>remove.Any(p=>t.name.StartsWith(p))).ToArray()){Object.DestroyImmediate(t.gameObject);removed++;}
 // Existing tiny stand rows ran uphill toward spectators' view: reverse row offsets, keep footprint and six tiers.
 var surface=field.Find("GenevaPlayingSurface");
 foreach(string kind in new[]{"CommunityStandStep","CommunityStandSeat"}){
 var parts=field.GetComponentsInChildren<Transform>().Where(t=>t.name==kind).ToArray();
 foreach(var side in new[]{-1,1}){var rows=parts.Where(t=>Mathf.Sign(Vector3.Dot(t.position-surface.position,surface.right))==side).OrderBy(t=>t.position.y).ToArray();
 for(int i=0;i<rows.Length;i++){var p=rows[i].position;float current=Vector3.Dot(p-surface.position,surface.right);float target=side*(23f+i*.75f+(kind=="CommunityStandSeat"?.22f:0));rows[i].position=p+surface.right*(target-current);}}
 }
 report.Add("replacementHomes="+n+" removedFootballObjects="+removed);
 if(n<1)throw new Exception("No shanty targets found");
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllLines(Out+"/Changes.txt",report);Debug.Log("MINI173_APPLY_PASS "+report.Last());EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 static void BuildHouse(Transform p,int index){
 var colors=new[]{new Color(.68f,.77f,.65f),new Color(.82f,.70f,.48f),new Color(.65f,.76f,.79f),new Color(.80f,.61f,.53f),new Color(.76f,.74f,.66f)};
 Material wall=Mat("Plaster"+(index%5),colors[index%5]);float w=2.65f,d=3.6f,h=index%3==0?5.1f:2.7f;
 Box(p,"StonePlinth",new Vector3(0,-.10f,0),new Vector3(w+.08f,.65f,d),baseMat,true);
 Box(p,"PlasterBody",new Vector3(0,h*.5f,0),new Vector3(w,h,d),wall,true);
 Box(p,"Eaves",new Vector3(0,h+.04f,0),new Vector3(w+.22f,.15f,d+.26f),trim);
 // Two sloped metal roof planes with a ridge, actual 3D silhouette in every view.
 float angle=17f, half=(w+.3f)*.5f, rise=Mathf.Tan(angle*Mathf.Deg2Rad)*half;
 foreach(int side in new[]{-1,1}){var r=Box(p,"RoofPlane",new Vector3(side*half*.5f,h+rise*.5f+.12f,0),new Vector3(half/Mathf.Cos(angle*Mathf.Deg2Rad),.09f,d+.35f),roof);r.localRotation=Quaternion.Euler(0,0,-side*angle);}
 for(int floor=0;floor<(h>3?2:1);floor++){
 float y=floor*2.55f;
 Box(p,"VerandaSlab",new Vector3(0,y+.08f,2.13f),new Vector3(w,.16f,.7f),trim,true);
 Box(p,"Door",new Vector3(-.55f,y+.97f,1.82f),new Vector3(.62f,1.78f,.06f),glass);
 Window(p,new Vector3(.60f,y+1.4f,1.84f),Quaternion.identity);
 foreach(int side in new[]{-1,1}){Window(p,new Vector3(side*(w*.5f+.035f),y+1.4f,-.45f),Quaternion.Euler(0,side*90,0));Box(p,"VerandaColumn",new Vector3(side*(w*.5f-.1f),y+1.25f,2.35f),new Vector3(.13f,2.5f,.13f),trim);}
 Box(p,"PorchLintel",new Vector3(0,y+2.42f,2.2f),new Vector3(w,.14f,.75f),trim);
 if(floor>0){Box(p,"BalconyRail",new Vector3(0,y+.90f,2.43f),new Vector3(w,.075f,.06f),rail);for(int i=0;i<9;i++)Box(p,"Baluster",new Vector3(-w*.45f+i*w*.9f/8,y+.48f,2.43f),new Vector3(.04f,.83f,.04f),rail);}
 }
 for(int step=0;step<2;step++)Box(p,"EntryStep",new Vector3(-.55f,-.15f-step*.15f,2.6f+step*.22f),new Vector3(.86f,.15f,.35f),baseMat,true);
 // Merge decorative primitives by shared material: bounded draw calls, colliders only on body/slabs/steps.
 var fs=p.GetComponentsInChildren<MeshFilter>();foreach(var group in fs.GroupBy(f=>f.GetComponent<Renderer>().sharedMaterial).ToArray()){
 var mesh=new Mesh{name=p.name+"_"+group.Key.name};mesh.CombineMeshes(group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=p.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray());string path=Art+"/"+mesh.name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved){EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);mesh=saved;}else AssetDatabase.CreateAsset(mesh,path);
 var g=new GameObject(group.Key.name);g.transform.SetParent(p,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=group.Key;g.isStatic=true;
 foreach(var f in group){Object.DestroyImmediate(f.GetComponent<Renderer>());Object.DestroyImmediate(f);}
 }
 }
 static void Window(Transform p,Vector3 at,Quaternion rot){var t=new GameObject("WindowFrame").transform;t.SetParent(p,false);t.localPosition=at;t.localRotation=rot;Box(t,"Frame",Vector3.zero,new Vector3(.82f,1.05f,.07f),trim);Box(t,"Glass",new Vector3(0,0,.045f),new Vector3(.65f,.87f,.04f),glass);Box(t,"Mullion",new Vector3(0,0,.075f),new Vector3(.045f,.88f,.04f),trim);Box(t,"Sill",new Vector3(0,-.52f,.06f),new Vector3(.93f,.09f,.20f),trim);}
 static Transform Box(Transform p,string name,Vector3 pos,Vector3 size,Material mat,bool collision=false){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(p,false);go.transform.localPosition=pos;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;if(!collision)Object.DestroyImmediate(go.GetComponent<Collider>());return go.transform;}
 static Material Mat(string name,Color color){string path=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.enableInstancing=true;EditorUtility.SetDirty(m);return m;}
 public static void Render(){EditorSceneManager.OpenScene(Scene);Directory.CreateDirectory(Out+"/Renders");RenderSettings.fog=false;foreach(var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))c.enabled=false;var cam=new GameObject("Evidence").AddComponent<Camera>();cam.farClipPlane=1500;cam.nearClipPlane=.03f;
 Shot(cam,"Lalay",new Vector3(26,22,-134),new Vector3(12,10,-157),false,55);
 Shot(cam,"Geneva",new Vector3(318,74,4),new Vector3(273,13,57),false,55);
 Shot(cam,"Bay",new Vector3(377,99,-153),new Vector3(269,2,-73),false,60);
 Shot(cam,"FullMap",new Vector3(100,460,-60),new Vector3(100,0,-60),true,275);EditorApplication.Exit(0);}
 public static void Shot(Camera c,string name,Vector3 pos,Vector3 target,bool ortho,float size){c.transform.position=pos;c.transform.LookAt(target);c.orthographic=ortho;c.orthographicSize=size;c.fieldOfView=size;var rt=new RenderTexture(1400,1000,24);c.targetTexture=rt;c.Render();RenderTexture.active=rt;var tex=new Texture2D(1400,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1400,1000),0,0);tex.Apply();File.WriteAllBytes(Out+"/Renders/"+name+".png",tex.EncodeToPNG());c.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);}
}}
