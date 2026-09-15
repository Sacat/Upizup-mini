using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace UpIzUpMini.EditorTools {
public static partial class Mini168Expansion {
 const string Scene="Assets/UpIzUpMini/Scenes/MapLab_GrandBayExpansionCopy.unity";
 const string Out="Docs/Maps/dm-dom-grand-bay-expansion-v1/Evidence";
 const string Art="Assets/UpIzUpMini/Maps/Regions/dm-dom-grand-bay-expansion-v1/Generated";
 const string Houses="Assets/UpIzUpMini/Art/Environment/Mini142";
 [Serializable] class Pt {public float x,z;}
 [Serializable] class Road {public string id,name,roadClass;public Pt[] points;}
 [Serializable] class Data {public float compression;public Road[] roads;}
 static readonly string[] IDs={"way/361079570","way/132984917","way/132984918","way/180962509","way/548578022","way/180962510","way/132984928"};
 static readonly List<Vector3[]> routes=new List<Vector3[]>();
 static readonly List<Vector3> housePositions=new List<Vector3>();
 static readonly List<Vector3> oldRoadPoints=new List<Vector3>();
 static Transform root;
 static MeshCollider originalGround;
 static MethodInfo heightMethod;
 static float baseY;
 static Material asphalt,ground,concrete,roof,wall,green;
 static readonly Vector3 school=new Vector3(319.58f/3,0,147.89f/3);
 static float Distance(Vector3 p,Vector3 a,Vector3 b,out float t) {p.y=a.y=b.y=0;var d=b-a;t=d.sqrMagnitude>0?Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude):0;return Vector3.Distance(p,a+d*t);}
 static float Raw(float x,float z) {
  if(originalGround!=null && originalGround.Raycast(new Ray(new Vector3(x,500,z),Vector3.down),out var hit,1000))return hit.point.y;
  return (float)heightMethod.Invoke(null,new object[]{x,z});
 }
 static float Grade(float x,float z) {
  return baseY+(z+104.1467f)*.055f;
 }
 static float Ground(float x,float z) {
  float raw=Raw(x,z),d=999;var p=new Vector3(x,0,z);
  foreach(var line in routes)for(int i=1;i<line.Length;i++)d=Mathf.Min(d,Distance(p,line[i-1],line[i],out _));
  float result=Mathf.Lerp(Grade(x,z)-.18f,raw,Mathf.SmoothStep(0,1,Mathf.Clamp01((d-8)/85)));
  // A broad settlement terrace, grading back into the source relief at its perimeter.
  float edge=Mathf.Max(Mathf.Max(-110-x,x-200),Mathf.Max(-65-z,z-95));
  float terraceBlend=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((edge+10)/35));
  result=Mathf.Lerp(result,Grade(x,z)-.18f,terraceBlend);
  float campus=Mathf.Max(Mathf.Abs(x-school.x)-20,Mathf.Abs(z-school.z)-13);
  if(campus<8)result=Mathf.Lerp(Grade(school.x,school.z)-.08f,result,Mathf.SmoothStep(0,1,Mathf.Max(0,campus)/8));
  return result;
 }
 static GameObject MeshObject(string name,Mesh mesh,Material mat,Transform parent,bool collision) {
  var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;
  if(collision)go.AddComponent<MeshCollider>().sharedMesh=mesh;return go;
 }
 static Mesh SaveMesh(string name,List<Vector3> v,List<int> t) {
  var m=new Mesh{name=name,indexFormat=v.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();
  string path=Art+"/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
  if(old){EditorUtility.CopySerialized(m,old);Object.DestroyImmediate(m);return old;}AssetDatabase.CreateAsset(m,path);return m;
 }
 static Material Mat(string name,Color color) {
  string path=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetFloat("_Glossiness",.12f);m.enableInstancing=true;return m;
 }
 static GameObject Box(string name,Vector3 p,Vector3 size,Material mat,Transform parent) {
  var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.position=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;return go;
 }
 static Vector3[] Resample(IEnumerable<Vector3> source) {
  var a=source.ToArray();var r=new List<Vector3>();for(int i=1;i<a.Length;i++){int n=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a[i-1],a[i])/2));for(int j=0;j<n;j++)r.Add(Vector3.Lerp(a[i-1],a[i],j/(float)n));}r.Add(a.Last());return r.ToArray();
 }
 static void Ribbon(string name,Vector3[] line,float width) {
  var v=new List<Vector3>();var t=new List<int>();
  for(int i=0;i<line.Length;i++){var dir=line[Mathf.Min(i+1,line.Length-1)]-line[Mathf.Max(0,i-1)];dir.y=0;var side=Vector3.Cross(Vector3.up,dir.normalized)*width/2;
   foreach(int sign in new[]{-1,1}){var p=line[i]+side*sign;p.y=Grade(p.x,p.z);v.Add(p);}
   if(i>0){int k=i*2;t.AddRange(new[]{k-2,k,k-1,k-1,k,k+1});}}
  MeshObject(name,SaveMesh(name,v,t),asphalt,root,true);
 }
 public static void BuildAndCapture() { BuildGeneva(); }
 // Historical first-pass constructor. Geneva revisions patch the saved review copy instead.
 static void BuildInitialAndCapture() {
  Directory.CreateDirectory(Out);Directory.CreateDirectory(Art);AssetDatabase.Refresh();
  Mini168GrandBayExpansionCopy.Validate();
  var scene=EditorSceneManager.OpenScene(Scene);
  var map=GameObject.Find("MapLab_GrandBayExpansionCopy");
  var previous=map.transform.Find("MINI168_Expansion");if(previous)Object.DestroyImmediate(previous.gameObject);
  root=new GameObject("MINI168_Expansion").transform;root.SetParent(map.transform,false);
  originalGround=map.GetComponentsInChildren<MeshCollider>(true).First(c=>c.name=="Copernicus_GLO30_Terrain");
  originalGround.gameObject.SetActive(true);
  Physics.SyncTransforms();
  var legacyType=typeof(UpIzUpMini.Editor.Mini095LalayMapLabSetup);var flags=BindingFlags.NonPublic|BindingFlags.Static;
  var load=legacyType.GetMethod("LoadHeightData",flags).Invoke(null,null);legacyType.GetField("s_heightData",flags).SetValue(null,load);legacyType.GetField("s_compression",flags).SetValue(null,1f/3);heightMethod=legacyType.GetMethod("RawHeightAt",flags);
  routes.Clear();housePositions.Clear();oldRoadPoints.Clear();
  foreach(var mf in map.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("MBRoad_")||f.name.StartsWith("Road_"))) {
   if(!mf.sharedMesh)continue;foreach(var v in mf.sharedMesh.vertices)oldRoadPoints.Add(mf.transform.TransformPoint(v));}
  baseY=Raw(-64.47f,-104.1467f)+.65f;
  var data=JsonUtility.FromJson<Data>(File.ReadAllText("Assets/UpIzUpMini/Maps/GrandBayPhase1MapData.json"));
  var report=new List<string>{"MINI168 Berekua to high school - first visual review copy","Source: OSM 2026-08-20; Copernicus GLO30; one-third horizontal compression.","Selected roads preserve OSM samples; outer Grand Bay Road truncated at z=90 and x=180 game metres.","School campus buildings and residential lots are artistic massing, not surveyed footprints."};
  foreach(string id in IDs){var road=data.roads.First(r=>r.id==id);var pts=road.points.Select(p=>new Vector3(p.x*data.compression,0,p.z*data.compression)).ToList();
   if(id=="way/548578022"){pts=pts.Where(p=>p.z<=90).ToList();}
   var line=Resample(pts);routes.Add(line);report.Add(id+" samples="+line.Length);}
  // Explicit approximate entrance lane from the mapped main road to the campus gate.
  var gate=new Vector3(school.x,0,school.z+13);var near=routes[4].OrderBy(p=>(p-gate).sqrMagnitude).First();routes.Add(Resample(new[]{near,gate}));
  // Mapped coast continuation joins the restored school-road endpoint exactly.
  var coast=data.roads.First(r=>r.id=="way/440101884");
  routes.Add(Resample(coast.points.Select(p=>new Vector3(p.x*data.compression,0,p.z*data.compression))));
  // Ring enlargement is deliberate at compressed scale; approach joins need saved-scene QA.
  var ring=new List<Vector3>();
  for(int i=0;i<=64;i++){float a=i*Mathf.PI*2/64;ring.Add(new Vector3(279.14f+8.5f*Mathf.Cos(a),0,-66.62f+8.5f*Mathf.Sin(a)));}
  routes.Add(ring.ToArray());
  asphalt=Mat("ExpansionAsphalt",new Color(.16f,.17f,.17f));ground=Mat("ExpansionGround",new Color(.24f,.38f,.19f));concrete=Mat("ExpansionConcrete",new Color(.59f,.57f,.49f));roof=Mat("CampusRoof",new Color(.26f,.38f,.42f));wall=Mat("CampusWall",new Color(.86f,.79f,.62f));green=Mat("Vegetation",new Color(.18f,.36f,.14f));
  // Replace only the copy's terrain with a distinct generated mesh; original remains disabled for rollback.
  var bounds=originalGround.bounds;float minX=Mathf.Min(bounds.min.x,-125),maxX=Mathf.Max(bounds.max.x,230),minZ=bounds.min.z,maxZ=Mathf.Max(bounds.max.z,110);
  var verts=new List<Vector3>();var tris=new List<int>();int nx=Mathf.CeilToInt((maxX-minX)/2),nz=Mathf.CeilToInt((maxZ-minZ)/2);
  for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++){float wx=Mathf.Lerp(minX,maxX,x/(float)nx),wz=Mathf.Lerp(minZ,maxZ,z/(float)nz);verts.Add(new Vector3(wx,Ground(wx,wz),wz));}
  for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){int k=z*(nx+1)+x;tris.AddRange(new[]{k,k+nx+1,k+1,k+1,k+nx+1,k+nx+2});}
  MeshObject("ExpansionTerrain",SaveMesh("ExpansionTerrain",verts,tris),ground,root,true);
  for(int i=0;i<routes.Count;i++)Ribbon("ExpansionRoad_"+i,routes[i],i==0||i==4?6.2f:4.8f);
  // Preserve the mapped campus centroid, but make its unknown footprint visibly an approximate model.
  float sy=Grade(school.x,school.z);
  Box("SchoolCourtyard",new Vector3(school.x,sy-.02f,school.z),new Vector3(40,.12f,26),concrete,root);
  for(int wing=0;wing<2;wing++){float x=school.x+(wing==0?-14:14);Box("SchoolTeachingWing_"+wing,new Vector3(x,sy+2.2f,school.z),new Vector3(8,4.4f,21),wall,root);Box("SchoolMetalRoof_"+wing,new Vector3(x,sy+4.5f,school.z),new Vector3(9,.3f,22),roof,root);
   for(int row=0;row<7;row++)Box("SchoolWindow",new Vector3(x+(wing==0?4.05f:-4.05f),sy+2.5f,school.z-8+row*2.6f),new Vector3(.12f,1.2f,1.6f),asphalt,root);}
  Box("SchoolRearWing",new Vector3(school.x,sy+2.2f,school.z-10),new Vector3(23,4.4f,6),wall,root);
  Box("SchoolRearRoof",new Vector3(school.x,sy+4.5f,school.z-10),new Vector3(24,.3f,7),roof,root);
  OrientSchoolToMainRoad(root);
  var palette=AssetDatabase.LoadAssetAtPath<Material>(Houses+"/Palette.mat");
  int count=0;
  foreach(var line in routes.Take(7)) {
   float carry=0;for(int i=1;i<line.Length;i++){carry+=Vector3.Distance(line[i-1],line[i]);if(carry<15)continue;carry=0;
    var dir=(line[i]-line[i-1]).normalized;var side=Vector3.Cross(Vector3.up,dir);
    foreach(int sign in new[]{-1,1}){var p=line[i]+side*(10+count%3)*sign;if(p.z<-55||p.z>80||Mathf.Abs(p.x-school.x)<29&&Mathf.Abs(p.z-school.z)<24)continue;
     if(housePositions.Any(q=>Vector3.Distance(q,p)<10))continue;
     float clearance=999;foreach(var route in routes)for(int j=1;j<route.Length;j++)clearance=Mathf.Min(clearance,Distance(p,route[j-1],route[j],out _));if(clearance<8)continue;
     float h=Ground(p.x,p.z),maxH=h,minH=h;foreach(float dx in new[]{-4f,4f})foreach(float dz in new[]{-4f,4f}){float value=Ground(p.x+dx,p.z+dz);maxH=Mathf.Max(maxH,value);minH=Mathf.Min(minH,value);}if(maxH-minH>2.3f)continue;
     string type=count%4==0?"HouseTwoStorey":"HouseOneStorey";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Houses+"/"+type+"_Body.asset");
     var go=MeshObject("ExpansionHouse_"+count,mesh,palette,root,false);p.y=maxH+.03f-mesh.bounds.min.y;go.transform.position=p;go.transform.rotation=Quaternion.LookRotation(-side*sign)*Quaternion.Euler(0,180,0);
     var col=go.AddComponent<BoxCollider>();col.center=mesh.bounds.center;col.size=mesh.bounds.size;
     Box("HouseFoundation_"+count,new Vector3(p.x,(maxH+minH)/2,p.z),new Vector3(6.4f,Mathf.Max(.16f,maxH-minH+.15f),6.4f),concrete,root);
     housePositions.Add(new Vector3(p.x,0,p.z));count++;
    }
   }
  }
  report.Add("New houses="+count+"; campus approximate; road surfaces collidable; driving NOT TESTED.");
  report.Add("Road grade model: continuous 5.5 percent northward plane; broad settlement terrace. Old-to-new connection driving QA pending.");
  // Hide phase-one future-exit barriers only in this proposal where the roads now continue.
  var barriers=map.transform.Find("PhaseOne_Boundaries");if(barriers)barriers.gameObject.SetActive(false);
  var labels=map.transform.Find("Labels");if(labels)labels.gameObject.SetActive(false);
  // Old schematic waterways outside the accepted district floated over newly graded land.
  // Retain them disabled in this copy until a terrain-conforming water pass is built.
  foreach(var tr in map.GetComponentsInChildren<Transform>(true))
   if(tr.name.StartsWith("Waterway_"))tr.gameObject.SetActive(false);
  originalGround.gameObject.SetActive(false);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  File.WriteAllLines(Out+"/BUILD-REPORT.txt",report);
  Capture();
  Mini168GrandBayExpansionCopy.Validate();
  Debug.Log("MINI168_EXPANSION_PASS roads="+routes.Count+" houses="+count+" copy only");
 }
 public static void Capture() {
  EditorSceneManager.OpenScene(Scene);
  foreach(var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))c.enabled=false;
  RenderSettings.fog=false;RenderSettings.ambientLight=new Color(.65f,.68f,.72f);
  var camera=new GameObject("MINI168_RenderCamera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.64f,.78f,.86f);camera.farClipPlane=1600;camera.nearClipPlane=.1f;
  Shot(camera,"01-Overview",new Vector3(440,410,-490),new Vector3(100,12,-55),true,270);
  Shot(camera,"02-Expansion-Neighbourhood",new Vector3(180,105,-90),new Vector3(35,15,5),false,52);
  Shot(camera,"03-High-School",new Vector3(145,65,105),new Vector3(107,17,49),false,48);
  var field=GameObject.Find("GenevaPlayingSurface");
  if(field){
   var centre=field.transform.position;
   Shot(camera,"06-Geneva-Playing-Field",centre+new Vector3(85,95,-90),centre,false,48);
   var island=GameObject.Find("GenevaRoundaboutIsland").transform.position;
   Shot(camera,"07-Roundabout",island+new Vector3(35,40,-47),island,false,48);
   Shot(camera,"08-Coast-School-Loop",new Vector3(425,260,-210),new Vector3(205,12,5),false,53);
  }
  var road=GameObject.Find("ExpansionRoad_0");var rv=road.GetComponent<MeshFilter>().sharedMesh.vertices;
  int index=Mathf.Clamp(rv.Length/2,2,rv.Length-24);index-=index%2;
  var eye=(rv[index]+rv[index+1])*.5f+Vector3.up*1.75f;
  var ahead=(rv[index+20]+rv[index+21])*.5f+Vector3.up*1.75f;
  Shot(camera,"04-Berekua-Road",eye,ahead,false,65);
  Object.DestroyImmediate(camera.gameObject);
 }
 public static void ValidateGeometry() {
  Mini168GrandBayExpansionCopy.Validate();
  var scene=EditorSceneManager.OpenScene(Scene);
  var expansion=GameObject.Find("MINI168_Expansion");
  if(!expansion)throw new Exception("Expansion root missing");
  int roadCount=0,triangles=0;float maximumGrade=0;
  var lines=new List<string>();
  foreach(var filter in expansion.GetComponentsInChildren<MeshFilter>()){
   var mesh=filter.sharedMesh;if(!mesh)throw new Exception("Missing mesh "+filter.name);
   foreach(var v in mesh.vertices)if(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.x)||float.IsInfinity(v.y)||float.IsInfinity(v.z))throw new Exception("Invalid vertex "+filter.name);
   triangles+=mesh.triangles.Length/3;
   if(!filter.name.StartsWith("ExpansionRoad_"))continue;
   roadCount++;
   if(filter.GetComponent<MeshCollider>()?.sharedMesh!=mesh)throw new Exception("Road collision mismatch");
   var vertices=mesh.vertices;float grade=0;
   for(int i=2;i<vertices.Length;i+=2){var a=(vertices[i-2]+vertices[i-1])*.5f;var b=(vertices[i]+vertices[i+1])*.5f;float length=Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z));if(length>.01f)grade=Mathf.Max(grade,Mathf.Abs(a.y-b.y)/length*100);}
   maximumGrade=Mathf.Max(maximumGrade,grade);lines.Add(filter.name+" maximum centreline grade percent="+grade.ToString("F2"));
  }
  if(roadCount!=11)throw new Exception("Expected eleven expansion road colliders");
  if(maximumGrade>8)throw new Exception("Expansion road grade exceeds 8 percent: "+maximumGrade);
  lines.Add("Triangles in expansion meshes, counting house instances="+triangles);
  lines.Add("Protected operation hashes unchanged; eleven road colliders match rendered meshes.");
  lines.Add("Actual walking, driving, NPC navigation and mobile performance NOT TESTED.");
  lines.Add("Maximum observed road grade percent="+maximumGrade.ToString("F2"));
  File.WriteAllLines(Out+"/GEOMETRY-VALIDATION.txt",lines);
  Debug.Log("MINI168_GEOMETRY_CHECK_PASS roads="+roadCount+" triangles="+triangles+" maxGrade="+maximumGrade);
 }
 public static void FinalReview() {ValidateGeometry();ValidateGeneva();Capture();Debug.Log("MINI168_FINAL_REVIEW_PASS");}
 // Idempotent 180-degree courtyard flip. The symmetric teaching wings stay in place.
 // Rebuild the campus-only entrance instead of rotating the surrounding road network.
 static void OrientSchoolToMainRoad(Transform expansion) {
  var courtyard=expansion.Find("SchoolCourtyard");
  if(!courtyard)throw new Exception("School courtyard missing");
  foreach(string name in new[]{"SchoolRearWing","SchoolRearRoof"}) {
   var part=expansion.Find(name);if(!part)throw new Exception(name+" missing");
   var p=part.position;p.z=school.z-10;part.position=p;
  }
  var main=expansion.Find("ExpansionRoad_4").GetComponent<MeshFilter>();
  var mv=main.sharedMesh.vertices;Vector3 start=Vector3.zero;float nearest=float.MaxValue;
  for(int i=0;i<mv.Length;i+=2){
   var p=main.transform.TransformPoint((mv[i]+mv[i+1])*.5f);
   float d=new Vector2(p.x-school.x,p.z-(school.z+13)).sqrMagnitude;
   if(d<nearest){nearest=d;start=p;}
  }
  var end=new Vector3(school.x,courtyard.GetComponent<Renderer>().bounds.max.y+.025f,school.z+1);
  var delta=end-start;delta.y=0;
  if(delta.magnitude<1)throw new Exception("School approach too short");
  float grade=Mathf.Abs(end.y-start.y)/delta.magnitude*100;
  if(grade>8)throw new Exception("School entrance grade exceeds 8 percent: "+grade);
  var side=Vector3.Cross(Vector3.up,delta.normalized)*2.4f;
  var vertices=new List<Vector3>{start-side,start+side,end-side,end+side};
  var mesh=SaveMesh("ExpansionRoad_7",vertices,new List<int>{0,2,1,1,2,3});
  var lane=expansion.Find("ExpansionRoad_7");
  lane.GetComponent<MeshFilter>().sharedMesh=mesh;lane.GetComponent<MeshCollider>().sharedMesh=mesh;
  File.WriteAllLines(Out+"/SCHOOL-FLIP.txt",new[]{
   "User: flip school so open courtyard faces main road.",
   "Open side now +Z/north; rear wing moved from centroid Z+10 to Z-10.",
   "Main road: ExpansionRoad_4 (way/548578022). School centroid and side wings retained.",
   "Entrance width 4.8m; measured grade percent="+grade.ToString("F2"),
   "Road connection="+start.ToString("F3")+" courtyard endpoint="+end.ToString("F3")});
 }
 public static void FlipSchool() {
  // The live scene may legitimately change under another task. Verify this operation
  // against its own before/after hash, never overwrite the historical source baseline.
  const string live="Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
  const string source="Assets/UpIzUpMini/Scenes/MapLab_MBRoad_LalayHighlandProof.unity";
  var liveBefore=File.ReadAllBytes(live);var sourceBefore=File.ReadAllBytes(source);
  var scene=EditorSceneManager.OpenScene(Scene);
  var expansion=GameObject.Find("MINI168_Expansion").transform;
  OrientSchoolToMainRoad(expansion);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  if(!liveBefore.SequenceEqual(File.ReadAllBytes(live))||!sourceBefore.SequenceEqual(File.ReadAllBytes(source)))
   throw new Exception("Protected scene changed during school revision");
  Capture();
  Debug.Log("MINI168_SCHOOL_FLIP_PASS: open courtyard faces main road; protected scenes unchanged during operation.");
 }
 static void Shot(Camera c,string name,Vector3 p,Vector3 target,bool ortho,float size) {
  c.transform.position=p;c.transform.LookAt(target);c.orthographic=ortho;c.orthographicSize=size;c.fieldOfView=ortho?50:size;
  var rt=new RenderTexture(1600,1000,24);c.targetTexture=rt;c.Render();RenderTexture.active=rt;var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();File.WriteAllBytes(Out+"/"+name+".png",tex.EncodeToPNG());c.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);
 }
}
}
