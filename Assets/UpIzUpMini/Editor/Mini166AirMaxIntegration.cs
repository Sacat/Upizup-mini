using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UpIzUpMini.Character;
namespace UpIzUpMini.EditorTools {
public static partial class Mini166Repair {
 const string AirArt="Assets/UpIzUpMini/Art/Characters/Garments/AirMax166";
 const string AirOut="Logs/Tasks/MINI-166/AirMaxIntegration";
 [Serializable] class AirData { public float[] positions,normals,uv;public int[] opaqueTint,opaqueFixed,air; }
 static void AirRequire(bool value,string why){if(!value)throw new Exception("AIRMAX: "+why);}
 static string OtherOutfits(OutfitWardrobe w)=>string.Join("|",w.pieces.Where(p=>p.id!="shoes_mike90"&&p.id!="shoes_mike97").Select(p=>JsonUtility.ToJson(p)))+string.Join("|",w.defaults.Where(d=>!d.itemId.StartsWith("shoes_")).Select(JsonUtility.ToJson));
 public static void AirMaxInstall(){try{
  Directory.CreateDirectory(AirOut);AssetDatabase.Refresh();EditorSceneManager.OpenScene(Scene);
  if(!File.Exists(AirOut+"/GrandBayProof-before-airmax.unity"))File.Copy(Scene,AirOut+"/GrandBayProof-before-airmax.unity");
  var report=new List<string>();
  foreach(string who in new[]{"Franki","Sacat"}){
   var root=GameObject.Find(who);var w=root.GetComponent<OutfitWardrobe>();string before=OtherOutfits(w);
   var r=w.bindings.Single(b=>b.slot==OutfitSlot.Shoes).renderer;var src=new Surface(root,r);
   AirRequire(r.transform.localPosition.sqrMagnitude<1e-8f&&Quaternion.Angle(r.transform.localRotation,Quaternion.identity)<.01f&&Vector3.Distance(r.transform.localScale,Vector3.one)<.001f,"expected root-space shoe renderer");
   foreach(string model in new[]{"90","97"}){
    var data=JsonUtility.FromJson<AirData>(File.ReadAllText(AirArt+"/AM"+model+".json"));var p=w.pieces.Single(x=>x.id=="shoes_mike"+model);
    var mesh=FitAir(root,src,data,model,report);p.mesh=StoreAirMesh(mesh,AirArt+"/"+who+"_AM"+model+".asset");p.materials=AirMaterials(model,who);p.tintSlots=new[]{0};
   }
   // New-game characters show the reviewed colourways. Existing saved colour choices still override defaults.
   foreach(var d in w.defaults.Where(d=>d.itemId.StartsWith("shoes_"))){d.itemId=who=="Franki"?"shoes_mike90":"shoes_mike97";d.colour=1;}
   w.Restore(null);AirRequire(OtherOutfits(w)==before,"non-shoe wardrobe changed for "+who);EditorUtility.SetDirty(w);EditorUtility.SetDirty(r);
  }
  AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());File.WriteAllLines(AirOut+"/fit-budget.txt",report);
  Debug.Log("MINI166_AIRMAX_INSTALL_PASS: both characters, existing IDs, 270/non-shoe pieces preserved");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 static Mesh FitAir(GameObject root,Surface src,AirData d,string model,List<string> report){
  int count=d.positions.Length/3;AirRequire(d.normals.Length==d.positions.Length&&d.uv.Length==count*2,"export channels");
  var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var bw=new List<BoneWeight>();var groups=new[]{new List<int>(),new List<int>(),new List<int>(),new List<int>()};
  foreach(string side in new[]{"Left","Right"}){
   int offset=vertices.Count;var foot=src.Rest(side+"Foot");var toe=src.Rest(side+"ToeBase");int bone=src.Bone(side+"Foot");
   var forward=toe-foot;forward.y=0;forward.Normalize();var right=Vector3.Cross(Vector3.up,forward).normalized;
   float lengthScale=1.02f,heightScale=Mathf.Clamp(foot.y/.104f,1.0f,1.4f);var origin=new Vector3(foot.x,.0038f,foot.z)+forward*.085f*lengthScale;
   for(int i=0;i<count;i++){
    var v=new Vector3(d.positions[i*3],d.positions[i*3+1],d.positions[i*3+2]);var n=new Vector3(d.normals[i*3],d.normals[i*3+1],d.normals[i*3+2]);
    vertices.Add(origin+forward*(v.x*lengthScale)+right*v.y+Vector3.up*(v.z*heightScale));normals.Add((forward*(n.x/lengthScale)+right*n.y+Vector3.up*(n.z/heightScale)).normalized);uv.Add(new Vector2(d.uv[i*2],d.uv[i*2+1]));bw.Add(new BoneWeight{boneIndex0=bone,weight0=1});
   }
   var input=new[]{d.opaqueTint,d.opaqueFixed,d.air};for(int s=0;s<3;s++)groups[s].AddRange(input[s].Select(i=>i+offset));
   // Low collars reveal anatomy hidden by the former tall shoes. Continue the ankle
   // under the existing hem with skin, blending Foot to Leg rather than moving clothes.
   int ankleStart=vertices.Count;int legBone=src.Bone(side+"Leg");var knee=src.Rest(side+"Leg");
   const int rings=6,sides=20;
   for(int ring=0;ring<=rings;ring++){
    float t=ring/(float)rings;float y=foot.y-.042f+t*.16f;float along=(y-foot.y)/Mathf.Max(.1f,knee.y-foot.y);var center=Vector3.LerpUnclamped(foot,knee,along);center.y=y;
    for(int j=0;j<sides;j++){float angle=j*Mathf.PI*2/sides;var n=right*Mathf.Cos(angle)+forward*Mathf.Sin(angle);vertices.Add(center+right*(Mathf.Cos(angle)*Mathf.Lerp(.027f,.035f,t))+forward*(Mathf.Sin(angle)*Mathf.Lerp(.031f,.039f,t)));normals.Add(n);uv.Add(Vector2.zero);float legWeight=Mathf.SmoothStep(0,1,t);bw.Add(new BoneWeight{boneIndex0=bone,weight0=1-legWeight,boneIndex1=legBone,weight1=legWeight});}
   }
   for(int ring=0;ring<rings;ring++)for(int j=0;j<sides;j++){int aa=ankleStart+ring*sides+j,bb=ankleStart+ring*sides+(j+1)%sides,cc=bb+sides,dd=aa+sides;groups[3].AddRange(new[]{aa,dd,cc,aa,cc,bb});}
   report.Add(root.name+" AM"+model+" "+side+" foot="+foot.ToString("F5")+" toe="+toe.ToString("F5")+" forward="+forward.ToString("F5")+" heightScale="+heightScale+" lengthScale="+lengthScale);
  }
  var mesh=new Mesh{name=root.name+"_AM"+model,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.boneWeights=bw.ToArray();mesh.bindposes=src.binds;mesh.subMeshCount=4;for(int s=0;s<4;s++)mesh.SetTriangles(groups[s],s);mesh.RecalculateBounds();mesh.RecalculateTangents();report.Add(mesh.name+" pairVertices="+mesh.vertexCount+" pairTriangles="+mesh.triangles.Length/3+" bounds="+mesh.bounds);return mesh;
 }
 static Mesh StoreAirMesh(Mesh mesh,string path){var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!old){AssetDatabase.CreateAsset(mesh,path);return mesh;}old.Clear();old.indexFormat=mesh.indexFormat;old.vertices=mesh.vertices;old.normals=mesh.normals;old.uv=mesh.uv;old.tangents=mesh.tangents;old.boneWeights=mesh.boneWeights;old.bindposes=mesh.bindposes;old.subMeshCount=mesh.subMeshCount;for(int s=0;s<mesh.subMeshCount;s++)old.SetTriangles(mesh.GetTriangles(s),s);old.RecalculateBounds();UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(old);return old;}
 static Material[] AirMaterials(string model,string who){
  string texpath=AirArt+"/AM"+model+"Palette.png";var importer=(TextureImporter)AssetImporter.GetAtPath(texpath);importer.sRGBTexture=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(texpath);
  var result=new Material[4];for(int slot=0;slot<3;slot++){
   string path=AirArt+"/AM"+model+"_"+slot+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);bool create=!m;if(create)m=new Material(Shader.Find("Standard")){name="AM"+model+"_"+slot};m.mainTexture=tex;m.color=Color.white;m.SetFloat("_Glossiness",slot==0?(model=="97"?.38f:.2f):.22f);m.SetFloat("_Metallic",slot==0&&model=="97"?.28f:0);
   if(slot==1){m.SetFloat("_SpecularHighlights",0);m.SetFloat("_GlossyReflections",0);m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");m.EnableKeyword("_GLOSSYREFLECTIONS_OFF");}
   if(slot==2){m.mainTexture=null;m.color=new Color(.60f,.72f,.73f,.36f);m.SetFloat("_Glossiness",.85f);m.SetFloat("_Mode",3);m.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.One);m.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);m.SetInt("_ZWrite",0);m.EnableKeyword("_ALPHAPREMULTIPLY_ON");m.renderQueue=3000;}
   if(create)AssetDatabase.CreateAsset(m,path);else EditorUtility.SetDirty(m);result[slot]=m;
  }result[3]=AssetDatabase.LoadAssetAtPath<Material>(Art+"/"+who+"_Skin.mat");AirRequire(result[3],"existing skin material missing");return result;
 }
 // Broad legacy regeneration must preserve the explicitly approved replacement shoes.
 static void KeepInstalledAirMax(GameObject root,OutfitWardrobe w){
  foreach(string model in new[]{"90","97"}){
   var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(AirArt+"/"+root.name+"_AM"+model+".asset");if(!mesh)continue;
   var piece=w.pieces.Single(p=>p.id=="shoes_mike"+model);piece.mesh=mesh;piece.materials=new[]{AssetDatabase.LoadAssetAtPath<Material>(AirArt+"/AM"+model+"_0.mat"),AssetDatabase.LoadAssetAtPath<Material>(AirArt+"/AM"+model+"_1.mat"),AssetDatabase.LoadAssetAtPath<Material>(AirArt+"/AM"+model+"_2.mat"),AssetDatabase.LoadAssetAtPath<Material>(Art+"/"+root.name+"_Skin.mat")};piece.tintSlots=new[]{0};
   foreach(var choice in w.defaults.Where(d=>d.itemId==piece.id))choice.colour=1;
  }
 }
 public static void AirMaxProofBefore()=>AirMaxProof("Before");
 public static void AirMaxProofAfter()=>AirMaxProof("After");
 public static void AirMaxProofFinalStatic()=>AirMaxProof("FinalStatic");
 static void AirMaxProof(string phase){try{
  Directory.CreateDirectory(Out+"/AirMax"+phase);EditorSceneManager.OpenScene(Scene);RenderSettings.fog=false;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.5f,.5f,.5f);
  foreach(var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))l.enabled=false;
  var key=new GameObject("AirMax key").AddComponent<Light>();key.type=LightType.Directional;key.intensity=.9f;key.transform.rotation=Quaternion.Euler(35,-35,0);key.cullingMask=1<<31;
  var camera=new GameObject("AirMax camera").AddComponent<Camera>();camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.21f,.24f);camera.orthographic=true;camera.nearClipPlane=.01f;camera.farClipPlane=20;
  var logs=new List<string>();
  foreach(string who in new[]{"Franki","Sacat"}){
   var root=GameObject.Find(who);root.SetActive(true);var oldpos=root.transform.position;var oldrot=root.transform.rotation;root.transform.SetPositionAndRotation(new Vector3(0,300,0),Quaternion.identity);foreach(var r in root.GetComponentsInChildren<Renderer>(true))r.gameObject.layer=31;
   var animator=root.GetComponentInChildren<Animator>(true);animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.applyRootMotion=false;animator.Rebind();var w=root.GetComponent<OutfitWardrobe>();w.Restore(null);
   foreach(string anim in (phase!="After"?new[]{"Stand--Idle.anim.fbx"}:new[]{"Stand--Idle.anim.fbx","Locomotion--Walk_N.anim.fbx","Locomotion--Run_N.anim.fbx"})){
    var clip=AssetDatabase.LoadAllAssetsAtPath("Assets/UpIzUpMini/Art/Animations/"+anim).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));var graph=PlayableGraph.Create("AirMax motion");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var playable=AnimationClipPlayable.Create(graph,clip);var output=AnimationPlayableOutput.Create(graph,"pose",animator);output.SetSourcePlayable(playable);graph.Play();
    bool idle=anim.StartsWith("Stand");string tag=idle?"Idle":anim.Contains("Walk")?"Walk":"Run";
    foreach(string model in new[]{"90","97"}){
     w.Select("shoes_mike"+model,1);int frames=idle?1:12;
     for(int frame=0;frame<frames;frame++){
      playable.SetTime(idle?.7:frame*clip.length/frames);graph.Evaluate(.001f);var shoes=w.bindings.Single(b=>b.slot==OutfitSlot.Shoes).renderer;var baked=WardrobePreviewMesh.Bake(shoes);AirRequire(baked.vertices.All(v=>!float.IsNaN(v.x)&&!float.IsInfinity(v.y)),"nonfinite skin");var vv=baked.vertices;int half=vv.Length/2;var left=BoundsOf(vv.Take(half).ToArray());var right=BoundsOf(vv.Skip(half).ToArray());AirRequire(left.size.magnitude<.65f&&right.size.magnitude<.65f&&left.size.magnitude>.1f,"individual shoe pose bounds "+who+model+tag+frame+" left="+left+" right="+right);UnityEngine.Object.DestroyImmediate(baked);
      camera.orthographicSize=idle?.43f:1.0f;Capture(camera,root,idle?new Vector3(2,.45f,2):new Vector3(2,1.2f,3),idle?new Vector3(0,.19f,.04f):new Vector3(0,.9f,0),"AirMax"+phase+"/"+who+"-AM"+model+"-"+tag+"-"+frame.ToString("D2"));
      if(idle){camera.orthographicSize=.98f;Capture(camera,root,new Vector3(2,1.25f,4),new Vector3(0,.9f,0),"AirMax"+phase+"/"+who+"-AM"+model+"-Full");camera.orthographicSize=.29f;Capture(camera,root,new Vector3(3,.17f,0),new Vector3(0,.13f,.02f),"AirMax"+phase+"/"+who+"-AM"+model+"-Side");}
     }
    }graph.Destroy();logs.Add(who+" "+tag+" both shoes finite, bounded");
   }
   root.transform.SetPositionAndRotation(oldpos,oldrot);foreach(var r in root.GetComponentsInChildren<Renderer>(true))r.gameObject.layer=0;
  }
  File.WriteAllLines(AirOut+"/"+phase+"-motion.txt",logs);Debug.Log("MINI166_AIRMAX_PROOF_PASS "+phase);EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
}
