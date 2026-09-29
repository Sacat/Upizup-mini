using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;
namespace UpIzUpMini.EditorTools {
/// <summary>
/// MINI-178: give Franki Sacat's approved double-loop chain, scaled to Franki's body.
/// Measure  - Play Mode: logs Sacat's fitted chain relative to his nape/chest and both rigs' proportions.
/// Preview  - Play Mode: equips Franki with a runtime profile derived from Sacat's, renders front/side/back.
/// Apply    - edit mode: writes FrankiChainPlacement.asset and assigns it to Franki in the expansion scene.
/// Sacat's profile, prefab and fit algorithm are never modified.
/// </summary>
[InitializeOnLoad] public static class Mini178FrankiChain {
 const string Scene="Assets/UpIzUpMini/Scenes/GrandBayProof_ExpansionImport.unity",Out="Logs/Tasks/MINI-178",Key="Mini178Mode",ProfilePath="Assets/UpIzUpMini/Data/Equipment/FrankiChainPlacement.asset",SacatProfilePath="Assets/UpIzUpMini/Data/Equipment/SacatChainPlacement.asset";
 static double next;static bool failed;
 static Mini178FrankiChain(){if(!string.IsNullOrEmpty(SessionState.GetString(Key,"")))EditorApplication.playModeStateChanged+=Changed;}
 public static void Measure(){Start("Measure");}
 public static void Preview(){Start("Preview");}
 public static void Sweep(){Start("Sweep");}
 public static void Save(){Start("Save");}
 public static void Verify(){Start("Verify");}
 const int SaveVariant=2; // M5: +.025 forward (after compensating the 3.5 cm neckward correction CharacterEquipment applies), no extra lift
 // Candidate corrections applied on top of the proportional mapping: (label, forward shift m, up shift m, pitch degrees about right axis).
 // Base root offset stays at the saved V4 (back .015, up .03). Each variant additionally shifts the LOWER loop
 // (GoldChain18k/world/geometry_0) in that node's local units (1 unit ~ 3.6 cm at the profile's chain scale).
 static readonly (string label,float dz,float dy,float pitch,float l0y,float l0z)[] Variants={("N0 saved M5 (+.025, up 0)",.025f,0f,0f,0f,0f),("N1 up .015",.025f,.015f,0f,0f,0f),("N2 up .025",.025f,.025f,0f,0f,0f),("N3 up .035",.025f,.035f,0f,0f,0f),("N4 up .025, back to +.010",.010f,.025f,0f,0f,0f)};
 static System.Collections.IEnumerator co;static double wake;static float pdz,pdy,ppitch,pl0y,pl0z;static bool keepPlaying;
 static void Start(string mode){Directory.CreateDirectory(Out+"/Renders");SessionState.SetString(Key,mode);EditorSceneManager.OpenScene(Scene);EditorApplication.playModeStateChanged+=Changed;EditorApplication.EnterPlaymode();}
 static void Changed(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode){next=EditorApplication.timeSinceStartup+3;EditorApplication.update+=Run;}if(s==PlayModeStateChange.EnteredEditMode){var mode=SessionState.GetString(Key,"");SessionState.SetString(Key,"");if(mode=="Save"&&!failed){try{AssignInScene();}catch(Exception e){failed=true;Debug.LogException(e);}}EditorApplication.Exit(failed?1:0);}}
 // Edit mode: point Franki's CharacterEquipment.chainPlacement at the saved asset in the expansion scene only.
 static void AssignInScene(){var scene=EditorSceneManager.OpenScene(Scene);var profile=AssetDatabase.LoadAssetAtPath<AccessoryPlacementProfile>(ProfilePath);if(!profile)throw new Exception("Franki profile asset missing");
  var eq=UnityEngine.Object.FindObjectsByType<CharacterEquipment>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(e=>e.name=="Franki");if(!eq)throw new Exception("Franki equipment missing");
  var so=new SerializedObject(eq);var prop=so.FindProperty("chainPlacement");if(prop==null)throw new Exception("chainPlacement field missing");prop.objectReferenceValue=profile;so.ApplyModifiedProperties();
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("MINI178_ASSIGN_PASS");}

 // ---- geometry helpers: everything is expressed in the CHARACTER frame (right/up/forward), origin = neck bone.
 static Vector3 F(Transform ch,Vector3 origin,Vector3 p){var d=p-origin;return new Vector3(Vector3.Dot(d,ch.right),Vector3.Dot(d,ch.up),Vector3.Dot(d,ch.forward));}
 static Vector3 FromFrame(Transform ch,Vector3 origin,Vector3 f){return origin+ch.right*f.x+ch.up*f.y+ch.forward*f.z;}
 static void Bounds3(IEnumerable<Vector3> pts,out Vector3 min,out Vector3 max){min=Vector3.one*1e9f;max=Vector3.one*-1e9f;foreach(var p in pts){min=Vector3.Min(min,p);max=Vector3.Max(max,p);}}
 static IEnumerable<Vector3> ChainVerts(Transform chain,Transform ch,Vector3 origin){foreach(var mf in chain.GetComponentsInChildren<MeshFilter>()){var m=mf.sharedMesh;if(!m||!m.isReadable)continue;foreach(var v in m.vertices)yield return F(ch,origin,mf.transform.TransformPoint(v));}}
 static string V(Vector3 v){return "("+v.x.ToString("F3")+", "+v.y.ToString("F3")+", "+v.z.ToString("F3")+")";}

 class Rig{public string name;public CharacterEquipment eq;public Animator anim;public Transform ch,neck,chest,head,lUp,rUp,hips;}
 static Rig GetRig(string name){var eq=UnityEngine.Object.FindObjectsByType<CharacterEquipment>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(e=>e.name==name);if(!eq)throw new Exception("Missing "+name);eq.gameObject.SetActive(true);var a=eq.GetComponentInChildren<Animator>();return new Rig{name=name,eq=eq,anim=a,ch=eq.transform,neck=a.GetBoneTransform(HumanBodyBones.Neck),chest=a.GetBoneTransform(HumanBodyBones.Chest),head=a.GetBoneTransform(HumanBodyBones.Head),lUp=a.GetBoneTransform(HumanBodyBones.LeftUpperArm),rUp=a.GetBoneTransform(HumanBodyBones.RightUpperArm),hips=a.GetBoneTransform(HumanBodyBones.Hips)};}
 static void Equip(Rig r){typeof(CharacterEquipment).GetField("alwaysEquipped",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(r.eq,new[]{"chain_gold"});r.eq.RefreshEquipment();}
 static Transform ChainOf(Rig r){var c=r.eq.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Equip_chain_gold");if(!c)throw new Exception("Chain missing on "+r.name);return c;}
 static void Proportions(Rig r,List<string> L){var o=r.neck.position;
  L.Add("== "+r.name+" ==");
  L.Add("character pos="+r.ch.position+" scale="+r.ch.lossyScale+" euler="+r.ch.eulerAngles);
  L.Add("neck-from-feet up="+(r.neck.position.y-r.ch.position.y).ToString("F3")+" chest-from-feet up="+(r.chest.position.y-r.ch.position.y).ToString("F3")+" head-from-feet up="+(r.head.position.y-r.ch.position.y).ToString("F3"));
  L.Add("chest relative to neck (right,up,fwd)="+V(F(r.ch,o,r.chest.position))+" neck-chest vertical="+(r.neck.position.y-r.chest.position.y).ToString("F3"));
  L.Add("shoulder width (upper-arm to upper-arm along right)="+Mathf.Abs(Vector3.Dot(r.lUp.position-r.rUp.position,r.ch.right)).ToString("F3"));
  L.Add("chest bone lossyScale="+r.chest.lossyScale+" localEuler-rel-character="+(Quaternion.Inverse(r.ch.rotation)*r.chest.rotation).eulerAngles);
 }
 static void ChainReport(Rig r,Transform chain,List<string> L){var o=r.neck.position;
  L.Add("chain root: world="+chain.position+" frame-from-neck="+V(F(r.ch,o,chain.position))+" localPos="+chain.localPosition+" localEuler="+chain.localEulerAngles+" localScale="+chain.localScale+" lossyScale="+chain.lossyScale);
  L.Add("chain rotation relative to character (euler)="+(Quaternion.Inverse(r.ch.rotation)*chain.rotation).eulerAngles);
  var all=ChainVerts(chain,r.ch,o).ToList();Bounds3(all,out var mn,out var mx);
  L.Add("chain ALL verts="+all.Count+" min(from neck)="+V(mn)+" max="+V(mx)+" size="+V(mx-mn));
  int i=0;foreach(var mf in chain.GetComponentsInChildren<MeshFilter>()){var pts=new List<Vector3>();var m=mf.sharedMesh;if(m&&m.isReadable)foreach(var v in m.vertices)pts.Add(F(r.ch,o,mf.transform.TransformPoint(v)));if(pts.Count==0)continue;Bounds3(pts,out var a,out var b);
   Vector3 c=Vector3.zero;foreach(var p in pts)c+=p;c/=pts.Count;
   var rt=pts.Where(q=>q.z<0).OrderByDescending(q=>q.y).FirstOrDefault();
   L.Add("  loop["+i+"] "+mf.transform.name+" path="+Path(mf.transform,chain)+" verts="+pts.Count+" min="+V(a)+" max="+V(b)+" centroid="+V(c)+" lossy="+mf.transform.lossyScale+" REAR-TOP="+V(rt));i++;}
  // rear-top and lowest points: how close the chain gets to the nape and how low it hangs
  var rear=all.Where(p=>p.z<0).OrderByDescending(p=>p.y).FirstOrDefault();var low=all.OrderBy(p=>p.y).First();
  L.Add("highest rear vertex (nape side)="+V(rear)+" lowest vertex="+V(low));
 }
 // Body/shirt outer surface depth (character forward axis, relative to the neck bone) at several heights
 // above/below the neck, x = 0. Uses the same baked-collider technique as ChainGarmentFit.
 static List<MeshCollider> BakeSurfaces(Rig r,Transform chain,List<Mesh> baked){var list=new List<MeshCollider>();foreach(var s in r.ch.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.enabled&&s.gameObject.activeInHierarchy)){if(!s.sharedMesh||s.transform.IsChildOf(chain))continue;var m=new Mesh();s.BakeMesh(m);baked.Add(m);var t=new GameObject("Mini178Surface");t.hideFlags=HideFlags.HideAndDontSave;t.transform.SetPositionAndRotation(s.transform.position,s.transform.rotation);t.transform.localScale=s.transform.lossyScale;var c=t.AddComponent<MeshCollider>();c.sharedMesh=m;list.Add(c);}Physics.SyncTransforms();return list;}
 static void SurfaceProfile(Rig r,Transform chain,List<string> L){var baked=new List<Mesh>();var cols=BakeSurfaces(r,chain,baked);var o=r.neck.position;
  try{L.Add("surface profile "+r.name+" (height from neck : rear z / front z, relative to neck bone, character forward)");
   foreach(float h in new[]{.10f,.08f,.06f,.04f,.02f,0f,-.03f,-.06f,-.09f,-.12f,-.16f,-.20f,-.24f,-.27f}){var basePt=o+r.ch.up*h;
    float rear=float.NaN,front=float.NaN;
    var rr=new Ray(basePt-r.ch.forward*.6f,r.ch.forward);float best=float.PositiveInfinity;foreach(var c in cols)if(c.Raycast(rr,out var hit,1.2f)&&hit.distance<best){best=hit.distance;rear=F(r.ch,o,hit.point).z;}
    var fr=new Ray(basePt+r.ch.forward*.6f,-r.ch.forward);best=float.PositiveInfinity;foreach(var c in cols)if(c.Raycast(fr,out var hit,1.2f)&&hit.distance<best){best=hit.distance;front=F(r.ch,o,hit.point).z;}
    L.Add("  h="+h.ToString("F2")+"  rear="+rear.ToString("F3")+"  front="+front.ToString("F3"));}
  }finally{foreach(var c in cols)UnityEngine.Object.DestroyImmediate(c.gameObject);foreach(var m in baked)UnityEngine.Object.DestroyImmediate(m);}}
 static string Path(Transform t,Transform root){var s=t.name;for(var p=t.parent;p!=null&&p!=root;p=p.parent)s=p.name+"/"+s;return s;}

 static void Run(){if(EditorApplication.timeSinceStartup<next)return;EditorApplication.update-=Run;var mode=SessionState.GetString(Key,"");
  try{var L=new List<string>();var sacat=GetRig("Sacat");var franki=GetRig("Franki");
   Equip(sacat);Proportions(sacat,L);ChainReport(sacat,ChainOf(sacat),L);
   Proportions(franki,L);
   if(mode=="Sweep"){co=SweepRoutine(sacat,franki,L);wake=0;keepPlaying=true;EditorApplication.update+=Tick;return;}
   if(mode=="Measure"){Equip(franki);ChainReport(franki,ChainOf(franki),L);SurfaceProfile(sacat,ChainOf(sacat),L);SurfaceProfile(franki,ChainOf(franki),L);File.WriteAllLines(Out+"/Measure.txt",L);Debug.Log("MINI178_MEASURE_PASS");}
   else if(mode=="Verify"){
    var prof=typeof(CharacterEquipment).GetField("chainPlacement",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(franki.eq) as AccessoryPlacementProfile;
    L.Add("VERIFY Franki chainPlacement from saved scene = "+(prof?AssetDatabase.GetAssetPath(prof):"NULL"));
    Equip(franki);ChainReport(franki,ChainOf(franki),L);
    L.Add("VERIFY Franki chain loops="+ChainOf(franki).GetComponentsInChildren<MeshFilter>().Length);
    File.WriteAllLines(Out+"/Verify.txt",L);RenderSet(sacat,"Verify-Sacat");RenderSet(franki,"Verify-Franki");Debug.Log("MINI178_VERIFY_PASS");}
   else if(mode=="Save"){
    var v=Variants[SaveVariant];pdz=v.dz;pdy=v.dy;ppitch=v.pitch;pl0y=v.l0y;pl0z=v.l0z;L.Add("SAVING variant: "+v.label);
    var profile=BuildProfile(sacat,franki,L);
    var existing=AssetDatabase.LoadAssetAtPath<AccessoryPlacementProfile>(ProfilePath);
    if(existing){EditorUtility.CopySerialized(profile,existing);profile=existing;EditorUtility.SetDirty(existing);}else AssetDatabase.CreateAsset(profile,ProfilePath);
    AssetDatabase.SaveAssets();
    typeof(CharacterEquipment).GetField("chainPlacement",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(franki.eq,profile);
    Equip(franki);ChainReport(franki,ChainOf(franki),L);File.WriteAllLines(Out+"/Save.txt",L);
    RenderSet(sacat,"Final-Sacat");RenderSet(franki,"Final-Franki");Debug.Log("MINI178_SAVE_PASS");}
   else if(mode=="Preview"){
    var profile=BuildProfile(sacat,franki,L);
    typeof(CharacterEquipment).GetField("chainPlacement",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(franki.eq,profile);
    Equip(franki);var chain=ChainOf(franki);ChainReport(franki,chain,L);
    File.WriteAllLines(Out+"/Preview.txt",L);
    RenderSet(sacat,"Sacat");RenderSet(franki,"Franki");Debug.Log("MINI178_PREVIEW_PASS");}
  }catch(Exception e){failed=true;keepPlaying=false;Debug.LogException(e);}finally{if(!keepPlaying)EditorApplication.ExitPlaymode();}}

 // Derive Franki's profile from Sacat's: same fitted children (double loop, same link transforms),
 // root pose placed by mapping Sacat's chain onto Franki with the nape->chest height ratio.
 static AccessoryPlacementProfile BuildProfile(Rig sacat,Rig franki,List<string> L){
  var src=AssetDatabase.LoadAssetAtPath<AccessoryPlacementProfile>(SacatProfilePath);if(!src)throw new Exception("Sacat profile missing");
  var chain=ChainOf(sacat);var so=sacat.neck.position;var fo=franki.neck.position;
  float sH=sacat.neck.position.y-sacat.chest.position.y,fH=franki.neck.position.y-franki.chest.position.y;float k=fH/sH;
  L.Add("PROFILE nape->chest vertical: Sacat="+sH.ToString("F4")+" Franki="+fH.ToString("F4")+" k="+k.ToString("F4"));
  // Sacat's chain root in character frame, scaled to Franki, then converted to Franki's chest-bone local space.
  var fr=F(sacat.ch,so,chain.position)*k;var worldPos=FromFrame(franki.ch,fo,fr)+franki.ch.forward*pdz+franki.ch.up*pdy;
  // Keep only Sacat's PITCH relative to his body. His ~5.7 deg yaw / ~1.2 deg roll compensate for his own torso twist and
  // made Franki's chain lopsided (left side stayed low, right side rode up the neck).
  var relRot=Quaternion.Inverse(sacat.ch.rotation)*chain.rotation;var relE=relRot.eulerAngles;L.Add("Sacat chain rotation rel. character euler="+relE+" -> using pitch only");
  var worldRot=Quaternion.AngleAxis(ppitch,franki.ch.right)*franki.ch.rotation*Quaternion.Euler(relE.x,0f,0f);
  L.Add("PROFILE variant dz="+pdz+" dy="+pdy+" pitch="+ppitch);
  var p=ScriptableObject.CreateInstance<AccessoryPlacementProfile>();p.useManualPlacement=true;
  p.localPosition=franki.chest.InverseTransformPoint(worldPos);
  p.localEulerAngles=(Quaternion.Inverse(franki.chest.rotation)*worldRot).eulerAngles;
  // Scale: keep Sacat's world scale ratio times k, expressed against Franki's chest bone lossy scale.
  var sw=chain.lossyScale;var target=sw*k;var cs=franki.chest.lossyScale;
  p.localScale=new Vector3(target.x/cs.x,target.y/cs.y,target.z/cs.z);
  p.fittedChildren=src.fittedChildren.Select(c=>new AccessoryPlacementProfile.ChildTransformPose{relativePath=c.relativePath,localPosition=c.localPosition,localEulerAngles=c.localEulerAngles,localScale=c.localScale}).ToList();
  var l0=p.fittedChildren.FirstOrDefault(c=>c.relativePath=="GoldChain18k/world/geometry_0");if(l0==null)throw new Exception("geometry_0 pose missing");
  l0.localPosition+=new Vector3(0,pl0y,pl0z);L.Add("PROFILE lower-loop shift y="+pl0y+" z="+pl0z+" -> geometry_0 localPosition="+l0.localPosition);
  L.Add("PROFILE localPosition="+p.localPosition+" localEuler="+p.localEulerAngles+" localScale="+p.localScale);
  return p;
 }
 static void Tick(){if(EditorApplication.timeSinceStartup<wake)return;try{if(!co.MoveNext()){EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();return;}wake=EditorApplication.timeSinceStartup+(float)co.Current;}catch(Exception e){failed=true;Debug.LogException(e);EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();}}
 static void SetEquipped(Rig r,bool on){typeof(CharacterEquipment).GetField("alwaysEquipped",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(r.eq,on?new[]{"chain_gold"}:new string[0]);r.eq.RefreshEquipment();}
 static Texture2D Thumb(Rig r,string view){var focus=(r.neck.position+r.chest.position)*.5f;Vector3 pos,target=focus;
  if(view=="front")pos=focus+r.ch.forward*1.15f+Vector3.up*.12f;else if(view=="side")pos=focus+r.ch.right*1.05f+r.ch.forward*.12f;else{pos=r.neck.position-r.ch.forward*.85f+Vector3.up*.10f;target=r.neck.position-Vector3.up*.10f;}
  var c=new GameObject("Mini178Cam").AddComponent<Camera>();c.nearClipPlane=.01f;c.farClipPlane=100;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.38f,.42f,.45f);c.transform.position=pos;c.transform.LookAt(target);c.fieldOfView=40;
  var rt=new RenderTexture(400,333,24);c.targetTexture=rt;c.Render();RenderTexture.active=rt;var tex=new Texture2D(400,333,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,400,333),0,0);tex.Apply();c.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(c.gameObject);return tex;}
 static System.Collections.IEnumerator SweepRoutine(Rig sacat,Rig franki,List<string> L){
  int rows=1+Variants.Length;var sheet=new Texture2D(1200,333*rows,TextureFormat.RGB24,false);int row=0;
  void AddRow(Rig r){int y=(rows-1-row)*333;int x=0;foreach(var v in new[]{"front","side","back"}){var t=Thumb(r,v);sheet.SetPixels(x,y,400,333,t.GetPixels());UnityEngine.Object.DestroyImmediate(t);x+=400;}row++;}
  Equip(sacat);yield return 1.5f;AddRow(sacat);L.Add("row 0 = Sacat (reference), columns front/side/back");
  foreach(var v in Variants){
   SetEquipped(franki,false);yield return .5f;
   pdz=v.dz;pdy=v.dy;ppitch=v.pitch;pl0y=v.l0y;pl0z=v.l0z;var profile=BuildProfile(sacat,franki,L);
   typeof(CharacterEquipment).GetField("chainPlacement",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(franki.eq,profile);
   SetEquipped(franki,true);yield return 1.5f;
   L.Add("row "+row+" = "+v.label);ChainReport(franki,ChainOf(franki),L);AddRow(franki);}
  sheet.Apply();File.WriteAllBytes(Out+"/Renders/Sweep-sheet.png",sheet.EncodeToPNG());File.WriteAllLines(Out+"/Sweep.txt",L);Debug.Log("MINI178_SWEEP_PASS");}
 static void RenderSet(Rig r,string n){var focus=(r.neck.position+r.chest.position)*.5f;var c=new GameObject("Mini178Cam").AddComponent<Camera>();c.nearClipPlane=.01f;c.farClipPlane=100;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.38f,.42f,.45f);
  Shot(c,n+"-front",focus+r.ch.forward*1.15f+Vector3.up*.12f,focus);
  Shot(c,n+"-side",focus+r.ch.right*1.05f+r.ch.forward*.12f,focus);
  Shot(c,n+"-back",r.neck.position-r.ch.forward*.85f+Vector3.up*.10f,r.neck.position-Vector3.up*.10f);
  UnityEngine.Object.DestroyImmediate(c.gameObject);}
 static void Shot(Camera c,string n,Vector3 pos,Vector3 target){c.transform.position=pos;c.transform.LookAt(target);c.fieldOfView=40;var rt=new RenderTexture(1200,1000,24);c.targetTexture=rt;c.Render();RenderTexture.active=rt;var tex=new Texture2D(1200,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1200,1000),0,0);tex.Apply();File.WriteAllBytes(Out+"/Renders/"+n+".png",tex.EncodeToPNG());c.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);}
}}
