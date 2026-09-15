using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace UpIzUpMini.EditorTools {
public static partial class Mini168Expansion {
 static readonly Vector3 roundCentre=new Vector3(279.14f,0,-66.62f);
 static readonly Vector3 fieldCentre=new Vector3(273.2f,0,57f);
 static readonly Vector3 fieldRight=new Vector3(.93f,0,.36f).normalized;
 static readonly Vector3 fieldForward=new Vector3(-.36f,0,.93f).normalized;
 static readonly List<Vector3[]> genevaRoutes=new List<Vector3[]>();
 static float fieldY,ringY;
 static Vector3 FP(float x,float z,float y=0) {return fieldCentre+fieldRight*x+fieldForward*z+Vector3.up*(fieldY+y);}
 static void Remove(Transform parent,string name){var tr=parent.Find(name);if(tr)Object.DestroyImmediate(tr.gameObject);}
 static void Surface(string name,Vector3[] line,float width,Material material,bool collide=true) {
  var v=new List<Vector3>();var t=new List<int>();
  for(int i=0;i<line.Length;i++) {
   bool closed=Vector3.Distance(line[0],line[line.Length-1])<.01f;
   var dir=closed&&(i==0||i==line.Length-1)?line[1]-line[line.Length-2]:line[Mathf.Min(i+1,line.Length-1)]-line[Mathf.Max(0,i-1)];dir.y=0;
   var side=Vector3.Cross(Vector3.up,dir.normalized)*width*.5f;
   v.Add(line[i]-side);v.Add(line[i]+side);
   if(i>0){int k=i*2;t.AddRange(new[]{k-2,k,k-1,k-1,k,k+1});}
  }
  Remove(root,name);MeshObject(name,SaveMesh(name,v,t),material,root,collide);
 }
 // Clip the inherited bay surface at one exact cross-section. The source asset is never edited.
 static Vector3 ClipBay(Transform map) {
  var old=map.GetComponentsInChildren<MeshFilter>(true).First(f=>f.name=="MBRoad_way_22917921");
  var source=old.sharedMesh;var sv=source.vertices;var st=source.triangles;
  var vertices=new List<Vector3>();var triangles=new List<int>();var seam=new List<Vector3>();
  const float cut=250;
  for(int i=0;i<st.Length;i+=3) {
   var poly=new List<Vector3>{old.transform.TransformPoint(sv[st[i]]),old.transform.TransformPoint(sv[st[i+1]]),old.transform.TransformPoint(sv[st[i+2]])};
   var output=new List<Vector3>();
   for(int j=0;j<3;j++) {
    var a=poly[j];var b=poly[(j+1)%3];bool ia=a.x<=cut,ib=b.x<=cut;
    if(ia)output.Add(a);
    if(ia!=ib){var q=Vector3.Lerp(a,b,(cut-a.x)/(b.x-a.x));output.Add(q);seam.Add(q);}
   }
   for(int j=1;j+1<output.Count;j++){int k=vertices.Count;vertices.AddRange(new[]{output[0],output[j],output[j+1]});triangles.AddRange(new[]{k,k+1,k+2});}
  }
  if(seam.Count<2)throw new Exception("No bay road cross-section at x250");
  Remove(root,"GenevaRetainedBayRoad");
  MeshObject("GenevaRetainedBayRoad",SaveMesh("GenevaRetainedBayRoad",vertices,triangles),asphalt,root,true);
  old.gameObject.SetActive(false);
  var low=seam.OrderBy(p=>p.z).First();var high=seam.OrderBy(p=>p.z).Last();
  var centre=(low+high)*.5f;
  File.WriteAllText(Out+"/GENEVA-BAY-SEAM.txt","Source bay mesh clipped at world X250; seam "+low+" to "+high+"; centre "+centre+"; original disabled, shared mesh retained.\n");
  return centre;
 }
 public static void BuildGeneva() {
  Mini168GrandBayExpansionCopy.Validate();
  var scene=EditorSceneManager.OpenScene(Scene);var map=GameObject.Find("MapLab_GrandBayExpansionCopy").transform;
  root=map.Find("MINI168_Expansion");
  asphalt=AssetDatabase.LoadAssetAtPath<Material>(Art+"/ExpansionAsphalt.mat");
  concrete=AssetDatabase.LoadAssetAtPath<Material>(Art+"/ExpansionConcrete.mat");
  green=Mat("GenevaGrass",new Color(.21f,.40f,.14f));
  var before=root.Find("ExpansionRoad_4").GetComponent<MeshFilter>().sharedMesh.vertices;
  var a=(before[0]+before[1])*.5f;var b=(before[2]+before[3])*.5f;
  float slope=(b.y-a.y)/(b.z-a.z);
  Func<Vector3,float> oldGrade=p=>a.y+(p.z-a.z)*slope;
  var bay=ClipBay(map);TrimBayEdges(map);ringY=bay.y;
  var data=JsonUtility.FromJson<Data>(File.ReadAllText("Assets/UpIzUpMini/Maps/GrandBayPhase1MapData.json"));
  Func<string,Vector3[]> source=id=>data.roads.First(r=>r.id==id).points.Select(p=>new Vector3(p.x*data.compression,0,p.z*data.compression)).ToArray();
  var main=Resample(source("way/548578022").Where(p=>p.z<=90));
  int split=Array.FindIndex(main,p=>p.x>=170);float tail=0;
  for(int i=split+1;i<main.Length;i++)tail+=Vector3.Distance(main[i-1],main[i]);
  float joinY=Mathf.Max(ringY+7.5f,oldGrade(main[split])-tail*.065f);
  float distance=0;
  for(int i=0;i<main.Length;i++){if(i>split)distance+=Vector2.Distance(new Vector2(main[i-1].x,main[i-1].z),new Vector2(main[i].x,main[i].z));main[i].y=i<=split?oldGrade(main[i]):Mathf.Lerp(oldGrade(main[split]),joinY,distance/(tail-4));}
  genevaRoutes.Clear();genevaRoutes.Add(main.Skip(split).ToArray());
  Surface("ExpansionRoad_4",main,6.2f,asphalt);
  var coastSource=source("way/440101884").ToList();coastSource.RemoveAt(0);
  coastSource.Insert(0,roundCentre+Vector3.forward*8.5f);
  var coast=Resample(coastSource);float length=0;for(int i=1;i<coast.Length;i++)length+=Vector3.Distance(coast[i-1],coast[i]);
  float travelled=0;for(int i=0;i<coast.Length;i++){if(i>0)travelled+=Vector2.Distance(new Vector2(coast[i-1].x,coast[i-1].z),new Vector2(coast[i].x,coast[i].z));coast[i].y=Mathf.Lerp(ringY,joinY,(travelled-4)/(length-8));}
  Surface("ExpansionRoad_8",coast,6.2f,asphalt);genevaRoutes.Add(coast);
  var ring=new Vector3[65];for(int i=0;i<65;i++){float angle=i*Mathf.PI*2/64;ring[i]=roundCentre+new Vector3(Mathf.Cos(angle)*8.5f,ringY,Mathf.Sin(angle)*8.5f);}
  Surface("ExpansionRoad_9",ring,7,asphalt);genevaRoutes.Add(ring);
  var arrival=roundCentre+new Vector3(-6.0104f,ringY,-6.0104f);
  var approach=Resample(new[]{bay,Vector3.Lerp(bay,arrival,.45f),arrival});
  Surface("ExpansionRoad_10",approach,6.2f,asphalt);genevaRoutes.Add(approach);
  // Cover the clipped inherited cross-section exactly; taper to standard road width.
  var retained=root.Find("GenevaRetainedBayRoad").GetComponent<MeshFilter>().sharedMesh.vertices;
  var ends=retained.Where(p=>Mathf.Abs(p.x-250)<.001f).OrderBy(p=>p.z).ToArray();
  var lane=root.Find("ExpansionRoad_10").GetComponent<MeshFilter>();var lv=lane.sharedMesh.vertices;
  var dir=approach[1]-approach[0];dir.y=0;var side=Vector3.Cross(Vector3.up,dir.normalized);
  var low=ends.First();var high=ends.Last();if(Vector3.Dot(low-bay,side)<0){lv[0]=low;lv[1]=high;}else{lv[0]=high;lv[1]=low;}
  var lm=SaveMesh("ExpansionRoad_10",lv.ToList(),lane.sharedMesh.triangles.ToList());lane.sharedMesh=lm;lane.GetComponent<MeshCollider>().sharedMesh=lm;
  fieldY=joinY-.8f;
  GradeGenevaTerrain();
  Remove(root,"GenevaField");BuildField();
  Remove(root,"GenevaRoundaboutIsland");
  var island=GameObject.CreatePrimitive(PrimitiveType.Cylinder);island.name="GenevaRoundaboutIsland";island.transform.SetParent(root,false);island.transform.position=roundCentre+Vector3.up*(ringY+.12f);island.transform.localScale=new Vector3(9.7f,.12f,9.7f);island.GetComponent<Renderer>().sharedMaterial=concrete;
  var turf=GameObject.CreatePrimitive(PrimitiveType.Cylinder);turf.name="IslandGrass";turf.transform.SetParent(island.transform,false);turf.transform.localPosition=new Vector3(0,1.1f,0);turf.transform.localScale=new Vector3(.91f,.12f,.91f);turf.GetComponent<Renderer>().sharedMaterial=green;
  OrientSchoolToMainRoad(root);
  EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");AssetDatabase.SaveAssets();
  File.WriteAllLines(Out+"/GENEVA-BUILD.txt",new[]{"Geneva copy revision; real source topology, artistic sports details.","Bay seam elevation="+ringY+" school/coastal junction elevation="+joinY+" field elevation="+fieldY,"Roundabout centre="+roundCentre+" centreline radius8.5m width7m; 64segments.","Field marked pitch34x56m, goals6x2.44m; compressed artistic dimensions.","Road4 eastern tail restored; coast8, ring9, bay approach10. Source bay disabled with unmodified shared asset.","Actual walking/driving/NPC/mobile tests NOT performed."});
  ValidateGeometry();ValidateGeneva();Capture();Debug.Log("MINI168_GENEVA_BUILD_PASS");
 }
 static void GradeGenevaTerrain() {
  var terrain=root.Find("ExpansionTerrain").GetComponent<MeshFilter>();
  string baseline=Art+"/GenevaTerrainBaseline.asset";
  var original=AssetDatabase.LoadAssetAtPath<Mesh>(baseline);
  if(!original){original=Object.Instantiate(terrain.sharedMesh);AssetDatabase.CreateAsset(original,baseline);}
  var list=original.vertices.ToList();var triangles=original.triangles.ToList();
  if(original.bounds.max.x<340) {
   float edgeX=original.bounds.max.x;
   var edge=Enumerable.Range(0,list.Count).Where(i=>Mathf.Abs(list[i].x-edgeX)<.01f&&list[i].z>=-112).OrderBy(i=>list[i].z).ToArray();
   if(edge.Length<2)throw new Exception("Terrain east boundary missing");
   int columns=Mathf.CeilToInt((340-edgeX)/2);
   var rows=new int[edge.Length,columns+1];
   for(int row=0;row<edge.Length;row++){
    rows[row,0]=edge[row];
    for(int col=1;col<=columns;col++){var p=list[edge[row]];p.x=Mathf.Lerp(edgeX,340,col/(float)columns);rows[row,col]=list.Count;list.Add(p);}
   }
   for(int row=0;row<edge.Length-1;row++)for(int col=0;col<columns;col++){
    int a=rows[row,col],b=rows[row+1,col],c=rows[row,col+1],d=rows[row+1,col+1];triangles.AddRange(new[]{a,b,c,c,b,d});
   }
  }
  var v=list.ToArray();
  for(int k=0;k<v.Length;k++){
   var p=v[k];float nearest=999,height=p.y;
   foreach(var route in genevaRoutes)for(int i=1;i<route.Length;i++){float d=Distance(p,route[i-1],route[i],out float t);if(d<nearest){nearest=d;height=Mathf.Lerp(route[i-1].y,route[i].y,t)-.18f;}}
   float blend=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((nearest-5)/20));
   var delta=p-fieldCentre;float x=Vector3.Dot(delta,fieldRight),z=Vector3.Dot(delta,fieldForward);
   float edge=Mathf.Max(Mathf.Abs(x)-21,Mathf.Abs(z)-32);
   if(edge<18)p.y=Mathf.Lerp(fieldY-.08f,p.y,Mathf.SmoothStep(0,1,Mathf.Max(0,edge)/18));
   p.y=Mathf.Lerp(p.y,height,blend);
   if(edge<=0 && nearest>5)p.y=fieldY-.08f;
   v[k]=p;
  }
  var mesh=SaveMesh("ExpansionTerrain",v.ToList(),triangles);terrain.sharedMesh=mesh;terrain.GetComponent<MeshCollider>().sharedMesh=mesh;
 }
 static void BuildField() {
  var field=new GameObject("GenevaField").transform;field.SetParent(root,false);
  var grass=Box("GenevaPlayingSurface",FP(0,0,-.02f),new Vector3(42,.12f,64),green,field);grass.transform.rotation=Quaternion.LookRotation(fieldForward);
  var stripe=Mat("GenevaGrassStripe",new Color(.24f,.44f,.16f));var white=Mat("GenevaMarking",new Color(.93f,.94f,.86f));var metal=Mat("GenevaGoalFrame",new Color(.8f,.82f,.8f));
  for(int i=0;i<8;i+=2){var band=Box("MownStripe",FP(0,-24.5f+i*7,.045f),new Vector3(34,.008f,7),stripe,field);band.transform.rotation=Quaternion.LookRotation(fieldForward);Object.DestroyImmediate(band.GetComponent<Collider>());}
  Action<string,Vector3,Vector3,float,Material> beam=(name,a,b,width,mat)=>{var go=Box(name,(a+b)*.5f,new Vector3(width,width,Vector3.Distance(a,b)),mat,field);go.transform.rotation=Quaternion.LookRotation(b-a);if(name=="PitchLine"||name=="GoalNet")Object.DestroyImmediate(go.GetComponent<Collider>());};
  Action<float,float,float,float> mark=(x,z,u,w)=>beam("PitchLine",FP(x,z,.06f),FP(u,w,.06f),.12f,white);
  mark(-17,-28,17,-28);mark(-17,28,17,28);mark(-17,-28,-17,28);mark(17,-28,17,28);mark(-17,0,17,0);
  for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48,b=(i+1)*Mathf.PI*2/48;mark(Mathf.Cos(a)*5,Mathf.Sin(a)*5,Mathf.Cos(b)*5,Mathf.Sin(b)*5);}
  foreach(int end in new[]{-1,1}) {
   mark(-10,28*end,-10,18*end);mark(10,28*end,10,18*end);mark(-10,18*end,10,18*end);
   mark(-5,28*end,-5,24*end);mark(5,28*end,5,24*end);mark(-5,24*end,5,24*end);
   for(int side=-1;side<=1;side+=2)beam("GoalPost",FP(side*3,28*end,.08f),FP(side*3,28*end,2.52f),.13f,metal);
   beam("GoalCrossbar",FP(-3,28*end,2.52f),FP(3,28*end,2.52f),.13f,metal);
   for(float x=-3;x<=3;x+=.5f){beam("GoalNet",FP(x,30*end,.08f),FP(x,30*end,2.52f),.025f,white);beam("GoalNet",FP(x,28*end,2.52f),FP(x,30*end,2.52f),.025f,white);}
   for(float y=.08f;y<=2.52f;y+=.4f)beam("GoalNet",FP(-3,30*end,y),FP(3,30*end,y),.025f,white);
  }
  // Modest sideline benches are artistic dressing, not a claim of surveyed stands.
  for(int i=0;i<3;i++){var bench=Box("SidelineBench",FP(19.3f,-12+i*12,.5f),new Vector3(.8f,.18f,6),concrete,field);bench.transform.rotation=Quaternion.LookRotation(fieldForward);foreach(int sign in new[]{-1,1}){var leg=Box("BenchSupport",FP(19.3f,-12+i*12+sign*2,.25f),new Vector3(.7f,.5f,.25f),concrete,field);leg.transform.rotation=Quaternion.LookRotation(fieldForward);}}
 }
 static void TrimBayEdges(Transform map) {
  foreach(var f in map.GetComponentsInChildren<MeshFilter>(true).ToArray()) {
   if(f.transform.IsChildOf(root)||!f.gameObject.activeInHierarchy||!f.name.Contains("way_22917921")||
      !(f.name.Contains("Sidewalk")||f.name.Contains("KerbRamp")||f.name.Contains("Frontage"))||!f.sharedMesh)continue;
   var sv=f.sharedMesh.vertices.Select(p=>f.transform.TransformPoint(p)).ToArray();if(!sv.Any(p=>p.x>250))continue;
   var st=f.sharedMesh.triangles;var v=new List<Vector3>();var t=new List<int>();
   for(int i=0;i<st.Length;i+=3){
    var poly=new[]{sv[st[i]],sv[st[i+1]],sv[st[i+2]]};var output=new List<Vector3>();
    for(int j=0;j<3;j++){var a=poly[j];var b=poly[(j+1)%3];if(a.x<=250)output.Add(a);if((a.x<=250)!=(b.x<=250))output.Add(Vector3.Lerp(a,b,(250-a.x)/(b.x-a.x)));}
    for(int j=1;j+1<output.Count;j++){int k=v.Count;v.AddRange(new[]{output[0],output[j],output[j+1]});t.AddRange(new[]{k,k+1,k+2});}
   }
   string name="GenevaTrim_"+f.name;Remove(root,name);if(v.Count>0)MeshObject(name,SaveMesh(name,v,t),f.GetComponent<Renderer>().sharedMaterial,root,f.GetComponent<Collider>()!=null);f.gameObject.SetActive(false);
  }
 }
 public static void ValidateGeneva() {
  Mini168GrandBayExpansionCopy.Validate();var map=GameObject.Find("MINI168_Expansion").transform;
  Func<int,Vector3[]> centres=n=>{var v=map.Find("ExpansionRoad_"+n).GetComponent<MeshFilter>().sharedMesh.vertices;return Enumerable.Range(0,v.Length/2).Select(i=>(v[2*i]+v[2*i+1])*.5f).ToArray();};
  var main=centres(4);var coast=centres(8);var ring=centres(9);var bay=centres(10);
  if(Vector3.Distance(main.Last(),coast.Last())>.05f)throw new Exception("School/coastal endpoint gap");
  if(ring.Min(p=>Vector3.Distance(p,coast.First()))>.05f || ring.Min(p=>Vector3.Distance(p,bay.Last()))>.05f)throw new Exception("Ring endpoint gap");
  if(!map.Find("GenevaField/GenevaPlayingSurface"))throw new Exception("Field missing");
  if(map.Find("SchoolRearWing").position.z>=school.z)throw new Exception("School faces away from main road");
  Physics.SyncTransforms();
  var ground=map.Find("ExpansionTerrain").GetComponent<MeshCollider>();
  var pitch=map.Find("GenevaField/GenevaPlayingSurface");
  for(int x=-18;x<=18;x+=6)for(int z=-28;z<=28;z+=7){
   var p=pitch.TransformPoint(new Vector3(x/42f,.5f,z/64f));
   if(!ground.Raycast(new Ray(p+Vector3.up*10,Vector3.down),out var h,50)||Mathf.Abs(p.y-h.point.y)>.18f)throw new Exception("Field pad unsupported/buried at "+p);
  }
  foreach(int n in new[]{4,8,9,10})foreach(var p in centres(n)){
   var terrain=map.Find("ExpansionTerrain").GetComponent<MeshCollider>();
   if(!terrain.Raycast(new Ray(p+Vector3.up*50,Vector3.down),out var hit,100))throw new Exception("Road outside terrain");
   if(hit.point.y>p.y+.08f)throw new Exception("Terrain above road "+n+" at "+p);
  }
  File.WriteAllText(Out+"/GENEVA-VALIDATION.txt","PASS: school/coast exact shared endpoint; ring/approach joins <5cm; 63 field support samples within18cm; school rear south; road centre terrain clearance; protected hashes/build exclusion. Static checks only.\n");
  Debug.Log("MINI168_GENEVA_VALIDATION_PASS");
 }
 public static void SurveyGeneva() {
  EditorSceneManager.OpenScene(Scene);
  var lines=new List<string>();
  foreach(var f in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)) {
   if(!f.sharedMesh || !(f.name.Contains("Road")||f.name.Contains("road")))continue;
   var b=f.GetComponent<Renderer>()?.bounds;
   if(b.HasValue && b.Value.max.x>245)lines.Add(f.name+" bounds="+b.Value+" collider="+(f.GetComponent<MeshCollider>()!=null));
  }
  var target=new Vector3(268,0,-81);
  foreach(var c in Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None)) {
   if(!c.name.Contains("Road")&&!c.name.Contains("road"))continue;
   if(c.Raycast(new Ray(target+Vector3.up*500,Vector3.down),out var hit,1000))lines.Add("BAY SAMPLE "+c.name+" "+hit.point);
  }
  File.WriteAllLines(Out+"/GENEVA-SURVEY.txt",lines); Debug.Log(string.Join("\n",lines)); Debug.Log("MINI168_SURVEY_PASS");
 }
 public static void DiagnoseGeneva() {
  EditorSceneManager.OpenScene(Scene);Physics.SyncTransforms();var lines=new List<string>();
  var f=GameObject.Find("ExpansionRoad_4").GetComponent<MeshFilter>();var v=f.sharedMesh.vertices;
  for(int i=0;i<v.Length;i+=10){var p=(v[i]+v[i+1])*.5f;if(p.x<170)continue;lines.Add("ROAD "+p+" normal="+f.sharedMesh.normals[i]);
   foreach(var c in Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None))if(c.Raycast(new Ray(p+Vector3.up*200,Vector3.down),out var hit,400))lines.Add(" hit "+c.name+" "+hit.point+" enabled="+c.gameObject.activeInHierarchy);
  }
  foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)) {
   var bounds=r.bounds;if(bounds.min.x<=200&&bounds.max.x>=200&&bounds.min.z<=53&&bounds.max.z>=53) {
    lines.Add("RENDERER "+r.name+" bounds="+bounds+" material="+r.sharedMaterial?.name+" collider="+r.GetComponent<Collider>()+" staticBatch="+r.isPartOfStaticBatch);
    if(!r.GetComponent<MeshCollider>()){var m=r.GetComponent<MeshFilter>();if(m&&m.sharedMesh){var c=r.gameObject.AddComponent<MeshCollider>();c.sharedMesh=m.sharedMesh;Physics.SyncTransforms();if(c.Raycast(new Ray(new Vector3(200,300,53),Vector3.down),out var h,500))lines.Add(" OCCLUDER HIT "+h.point);Object.DestroyImmediate(c);}}
   }
  }
  var camera=new GameObject("DiagnosticCamera").AddComponent<Camera>();camera.farClipPlane=1000;Shot(camera,"09-Junction-Diagnostic",new Vector3(210,110,60),new Vector3(210,0,60),true,65);
  File.WriteAllLines(Out+"/GENEVA-DIAGNOSIS.txt",lines);Debug.Log(string.Join("\n",lines));
 }
}
}
