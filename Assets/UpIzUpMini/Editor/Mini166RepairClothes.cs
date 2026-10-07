using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UpIzUpMini.Character;
namespace UpIzUpMini.EditorTools {
public static partial class Mini166Repair {
 public static void Preview()=>Produce(false);
 public static void Integrate()=>Produce(true);
 static void Produce(bool save){
  try{
   Directory.CreateDirectory(Out);Directory.CreateDirectory(Art);AssetDatabase.Refresh();
   if(!File.Exists(Out+"/GrandBayProof-before.unity"))File.Copy(Scene,Out+"/GrandBayProof-before.unity");
   EditorSceneManager.OpenScene(Scene);
   foreach(string who in new[]{"Franki","Sacat"})BuildCharacter(GameObject.Find(who));
   AssetDatabase.SaveAssets();
   if(save){EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());Debug.Log("MINI166_REPAIR_INTEGRATE_PASS");}
   RenderAll();Debug.Log("MINI166_REPAIR_PREVIEW_PASS");EditorApplication.Exit(0);
  }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
 }
 static void BuildCharacter(GameObject root){
  bool sacat=root.name=="Sacat";fitOriginal=null;fitGarment=null;sacatOriginalTorso=false;var renderers=root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
  SkinnedMeshRenderer Find(string name)=>renderers.First(r=>r.name==name);
  var sourceR=Find(sacat?"Ch06":"Ch28_Hoody");var shirt=new Surface(root,sourceR);
  var pants=sacat?shirt:new Surface(root,Find("Ch28_Pants"));var shoes=sacat?shirt:new Surface(root,Find("Ch28_Sneakers"));
  var head=sacat?shirt:new Surface(root,Find("Ch28_Hair"));
  var body=new Shape(shirt,shirt.triangles.Length);var upper=new List<List<V>>();var lower=new List<List<V>>();
  float waist=shirt.Rest("Hips").y+.032f,ankle=pants.Rest("LeftFoot").y+.10f;
  if(sacat){
   int headIndex=shirt.Bone("Head");
   foreach(var face in shirt.Faces(0)){
    var center=(face[0].p+face[1].p+face[2].p)/3;
    bool protectedSkin=face.All(v=>v.w.boneIndex0==headIndex&&v.w.weight0>.55f)||Mathf.Abs(center.x)>.635f;
    if(protectedSkin){body.Poly(face,0);continue;}
    upper.Add(Clip(face,p=>p.y-(sacat?waist-.07f:waist),true));var below=Clip(face,p=>p.y-waist,false);lower.Add(Clip(below,p=>p.y-ankle,true));
   }
   for(int s=1;s<shirt.triangles.Length;s++)foreach(var f in shirt.Faces(s))body.Poly(Clip(f,p=>p.y-ankle,true),s);
   var br=Bind(root,"WardrobeBody",shirt);br.sharedMesh=Asset(body.Mesh(root.name+"_Body"),root.name+"_Body.asset");br.sharedMaterials=shirt.materials;
   sourceR.enabled=false;
  }else{
   upper.AddRange(shirt.Faces(0));lower.AddRange(pants.Faces(0));sourceR.enabled=false;Find("Ch28_Pants").enabled=false;Find("Ch28_Sneakers").enabled=false;
   var originalBody=Find("Ch28_Body");var bodySource=new Surface(root,originalBody);var cleanBody=new Shape(bodySource,bodySource.materials.Length);for(int sub=0;sub<bodySource.triangles.Length;sub++)foreach(var f in bodySource.Faces(sub))cleanBody.Poly(Clip(f,p=>p.y-ankle,true),sub);var br=Bind(root,"WardrobeBody",bodySource);br.sharedMesh=Asset(cleanBody.Mesh("Franki_Body"),"Franki_Body.asset");br.sharedMaterials=bodySource.materials;originalBody.enabled=false;
  }
  foreach(var old in renderers.Where(r=>r.name.Contains("HatOverlay")||r.name.Contains("ShirtOverlay")))old.enabled=false;
  if(sacat){
   // Use the repaired continuous garment topology, fitted by corresponding
   // rest bones, instead of preserving Ch06's fused hoodie pockets and skirt.
   var originalSilhouette=shirt;fitOriginal=null;fitGarment=null;
   var franki=GameObject.Find("Franki");var fr=franki.GetComponentsInChildren<SkinnedMeshRenderer>(true);
   shirt=Retarget(new Surface(franki,fr.First(r=>r.name=="Ch28_Hoody")),shirt);
   pants=Retarget(new Surface(franki,fr.First(r=>r.name=="Ch28_Pants")),pants);
   // MINI-196: Sacat's shirt keeps his ORIGINAL torso faces (natural sloped shoulders, raglan sleeves, original skin weights); only the pants come from Franki's retargeted garment
   lower=pants.Faces(0).ToList();sacatOriginalTorso=true;
  }
  var skin=Matte(root.name+"_Skin",sacat?new Color(.34f,.205f,.135f):new Color(.412f,.243f,.153f));
  var fabric=Matte("Cotton",Color.white);var denim=Matte("Denim",Color.white);var trim=Matte("Stitch",new Color(.48f,.42f,.29f));var detail=Matte("Pearl",new Color(.78f,.79f,.75f));
  var w=root.GetComponent<OutfitWardrobe>();if(!w)w=root.AddComponent<OutfitWardrobe>();var pieces=new List<OutfitPiece>();var bindings=new List<OutfitBinding>();
  void Piece(string id,string label,OutfitSlot slot,Shape shape,Material[] mats,int[] tint){pieces.Add(new OutfitPiece{id=id,label=label,slot=slot,mesh=shape==null?null:Asset(shape.Mesh(root.name+"_"+id),root.name+"_"+id+".asset"),materials=mats,tintSlots=tint});}
  bindings.Add(new OutfitBinding{slot=OutfitSlot.Shirt,renderer=Bind(root,"WardrobeShirt",shirt)});
  Piece("shirt_tee_mike","Mike Crew Tee",OutfitSlot.Shirt,Shirt(shirt,upper,false),new[]{fabric,skin,detail,trim},new[]{0});
  Piece("shirt_polo_lacos","Lacos Polo",OutfitSlot.Shirt,Shirt(shirt,upper,true),new[]{fabric,skin,detail,trim},new[]{0});
  bindings.Add(new OutfitBinding{slot=OutfitSlot.Pants,renderer=Bind(root,"WardrobePants",pants)});
  Piece("pants_jeans","Straight Jeans",OutfitSlot.Pants,Pants(pants,lower,0),new[]{denim,skin,trim,detail},new[]{0});
  Piece("pants_trousers","Tailored Trousers",OutfitSlot.Pants,Pants(pants,lower,1),new[]{fabric,skin,trim,detail},new[]{0});
  Piece("pants_shorts_denim","Denim Shorts",OutfitSlot.Pants,Pants(pants,lower,2),new[]{denim,skin,trim,detail},new[]{0});
  bindings.Add(new OutfitBinding{slot=OutfitSlot.Hat,renderer=Bind(root,"WardrobeHat",head)});
  Piece("hat_none","No Hat",OutfitSlot.Hat,null,Array.Empty<Material>(),Array.Empty<int>());
  Piece("hat_lacos","Lacos Curved Cap",OutfitSlot.Hat,Cap(head),new[]{fabric,Matte("CapSeams",new Color(.17f,.19f,.18f)),detail,trim},new[]{0});
  bindings.Add(new OutfitBinding{slot=OutfitSlot.Shoes,renderer=Bind(root,"WardrobeShoes",shoes)});
  var shoeMats=new[]{Matte("ShoeUpper",Color.white,.14f),Matte("Sole",new Color(.8f,.8f,.76f)),Matte("Rubber",new Color(.035f,.04f,.044f)),Matte("ShoeAccent",new Color(.55f,.035f,.04f),.2f),Matte("Laces",new Color(.78f,.79f,.77f))};
  Piece("shoes_mike90","Mike 90",OutfitSlot.Shoes,Shoes(shoes,0),shoeMats,new[]{0});
  Piece("shoes_mike97","Mike 97",OutfitSlot.Shoes,Shoes(shoes,1),shoeMats,new[]{0});
  Piece("shoes_mike270","Mike 270",OutfitSlot.Shoes,Shoes(shoes,2),shoeMats,new[]{0});
  w.pieces=pieces.ToArray();w.bindings=bindings.ToArray();w.defaults=sacat
   ?new[]{new OutfitChoice{itemId="shirt_polo_lacos",colour=4},new OutfitChoice{itemId="pants_trousers",colour=2},new OutfitChoice{itemId="hat_none",colour=2},new OutfitChoice{itemId="shoes_mike97",colour=1}}
   :new[]{new OutfitChoice{itemId="shirt_tee_mike",colour=0},new OutfitChoice{itemId="pants_jeans",colour=5},new OutfitChoice{itemId="hat_none",colour=2},new OutfitChoice{itemId="shoes_mike90",colour=2}};KeepInstalledAirMax(root,w);w.Restore(null);EditorUtility.SetDirty(w);
  File.WriteAllLines(Out+"/"+root.name+"-budget.txt",pieces.Select(p=>p.id+" vertices="+(p.mesh?p.mesh.vertexCount:0)+" triangles="+(p.mesh?p.mesh.triangles.Length/3:0)+" materials="+p.materials.Length));
 }
 static Shape Shirt(Surface s,List<List<V>> faces,bool polo){
  var result=new Shape(s);var neck=s.Rest("Neck");float sleeve=Mathf.Lerp(Mathf.Abs(s.Rest("LeftArm").x),Mathf.Abs(s.Rest("LeftForeArm").x),.46f);
  float opening=neck.y+.45f*(s.Rest("Head").y-neck.y);
  foreach(var face in faces){if(face.Count<3)continue;
   var f=face.Select(v=>{
    if(v.p.y>opening&&Mathf.Abs(v.p.x)<.15f&&new Vector2(v.p.x,v.p.z-neck.z).magnitude>.062f){var p=v.p;p.y=opening;v.p=p;}
    if(!sacatOriginalTorso&&v.p.y<neck.y-.12f&&Mathf.Abs(v.p.x)<.205f){float t=Mathf.InverseLerp(s.Rest("Hips").y,neck.y-.12f,v.p.y);float rx=Mathf.Lerp(.151f,.20f,t),rz=Mathf.Lerp(.088f,.112f,t);float angle=Mathf.Atan2(v.p.x/rx,(v.p.z+.01f)/rz);v.p=new Vector3(Mathf.Sin(angle)*rx,v.p.y,Mathf.Cos(angle)*rz-.01f);v.n=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle)).normalized;}
    // Restore a convex neck-to-deltoid transition after the legacy hood flatten.
    // Keep the neckline, sleeve ends, weights and topology intact.
    if(sacatOriginalTorso){
     // the original hoodie's hood is an extra layer over the upper back: ease those vertices forward onto a smooth back-of-shoulder to back-of-neck slope
     float yb=neck.y-.16f;
     if(Mathf.Abs(v.p.x)<.2f&&v.p.y>yb&&v.p.z<neck.z-.02f){
      float t=Mathf.SmoothStep(0,1,Mathf.InverseLerp(yb,neck.y+.05f,v.p.y));
      float zb=Mathf.Lerp(-.125f,neck.z-.07f,t);
      float edge=1f-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.14f,.21f,Mathf.Abs(v.p.x)));
      var p2=v.p;p2.z=Mathf.Lerp(p2.z,Mathf.Max(p2.z,zb),edge);v.p=p2;
     }
     return v;}
    var before=v.p;v.p=ShoulderForm(before,s);
    const float step=.0001f;
    var jac=Matrix4x4.identity;
    jac.SetColumn(0,(ShoulderForm(before+Vector3.right*step,s)-ShoulderForm(before-Vector3.right*step,s))/(2*step));
    jac.SetColumn(1,(ShoulderForm(before+Vector3.up*step,s)-ShoulderForm(before-Vector3.up*step,s))/(2*step));
    jac.SetColumn(2,(ShoulderForm(before+Vector3.forward*step,s)-ShoulderForm(before-Vector3.forward*step,s))/(2*step));
    v.n=jac.inverse.transpose.MultiplyVector(v.n).normalized;
    return v;}).ToList();
   var arms=Clip(f,p=>Mathf.Abs(p.x)-sleeve,true);result.Poly(arms,1);var torso=Clip(f,p=>Mathf.Abs(p.x)-sleeve,false);
   // Only the neck opening exposes skin. A horizontal cut across the whole
   // shoulder made the tee look like an off-shoulder shirt.
   float NeckDistance(Vector3 p)=>Mathf.Min(p.y-opening,.075f-Mathf.Abs(p.x));
   result.Poly(Clip(torso,NeckDistance,true),1);result.Poly(Clip(torso,NeckDistance,false),0);
  }
  int bone=s.Bone("Neck");
  // An actual folded collar, with two open pointed leaves on the chest.
  float rx=.064f,rz=.062f;
  for(int i=0;i<32;i++){float a=(i/32f)*Mathf.PI*2,b=((i+1)/32f)*Mathf.PI*2;Vector3 Ring(float t,float scale,float y)=>new Vector3(Mathf.Sin(t)*rx*scale,y,neck.z+Mathf.Cos(t)*rz*scale);
   if(polo){if(Mathf.Cos((a+b)/2)>.8f)continue;result.Quad(Ring(a,1,opening+.012f),Ring(b,1,opening+.012f),Ring(b,1.45f,opening-.025f),Ring(a,1.45f,opening-.025f),0,bone,true);}
   else result.Quad(Ring(a,1,opening+.001f),Ring(b,1,opening+.001f),Ring(b,1.13f,opening-.008f),Ring(a,1.13f,opening-.008f),0,bone,true);
  }
  if(polo){
   Vector3 OnChest(float x,float y){var near=result.v.Where(v=>Mathf.Abs(v.p.x-x)<.025f&&Mathf.Abs(v.p.y-y)<.025f).ToArray();return new Vector3(x,y,(near.Length>0?near.Max(v=>v.p.z):Front(s,x,y))+.006f);}
   for(int sign=-1;sign<=1;sign+=2){result.Quad(OnChest(sign*.012f,opening+.01f),OnChest(sign*.057f,opening+.006f),OnChest(sign*.083f,opening-.053f),OnChest(sign*.027f,opening-.07f),0,-1,true);}
   float z=Front(s,0,opening-.095f)+.004f;result.Quad(new Vector3(-.009f,opening-.025f,z),new Vector3(.009f,opening-.025f,z),new Vector3(.009f,opening-.14f,z),new Vector3(-.009f,opening-.14f,z),0,-1,true);
   for(int i=0;i<3;i++){float y=opening-.048f-i*.03f;result.Tube(new List<Vector3>{new Vector3(-.003f,y,z+.002f),new Vector3(.003f,y,z+.002f)},.003f,2);}
  }
  // Small stitched chest wordmark geometry, separate from tint/skin.
  Word(result,polo?"LACOS":"MIKE",new Vector3(.065f,opening-.145f,Front(s,.065f,opening-.145f)+.003f),.007f,2);
  return result;
 }
 static Vector3 ShoulderForm(Vector3 p,Surface s){
  var neck=s.Rest("Neck");var arm=s.Rest("LeftArm");float x=Mathf.Abs(p.x),width=Mathf.Abs(arm.x);
  float inner=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.07f,.125f,x));
  float outer=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(width*.82f,width*1.45f,x));
  float height=Mathf.SmoothStep(0,1,Mathf.InverseLerp(arm.y-.095f,arm.y+.055f,p.y));
  float shoulder=inner*outer*height;
  float traps=Mathf.Exp(-Mathf.Pow((x-width*.55f)/(width*.38f),2));
  p.y+=shoulder*(.005f+.016f*traps);
  p.z+=(p.z-neck.z)*shoulder*.12f;
  p.x+=Mathf.Sign(p.x)*shoulder*.006f*(1-traps);
  p=DeltoidForm(p,s);
  p=BicepForm(p,s);
  p=ChestForm(p,s,neck);
  return p;
 }
 // User: "the shoulders and still too straight" - the trapezius slope above
 // handles the NECK-to-shoulder transition (small x, near the spine); the
 // sleeve cap itself (outer shoulder, at the arm bone's own root, t near 0
 // along the shoulder-elbow segment) was still a straight cone down to the
 // sleeve hem with no deltoid roundness. Peaks early (t=.08) and fades out
 // well before BicepForm's peak (t=.52) so the two don't overlap/compound.
 static Vector3 DeltoidForm(Vector3 p,Surface s){
  var shoulder=s.Rest("LeftArm");var elbow=s.Rest("LeftForeArm");
  float x=Mathf.Abs(p.x);float x0=Mathf.Abs(shoulder.x),x1=Mathf.Abs(elbow.x);
  if(Mathf.Abs(x1-x0)<.001f)return p;
  float t=Mathf.InverseLerp(x0,x1,x);
  if(t<-.1f||t>.5f)return p;
  // User: "drop the edges of the shoulders some more, like the round parts
  // towards the end, leave the other parts up" - the inner side (toward the
  // neck/shoulder point, t<.08) keeps its original wide falloff; the outer
  // side (toward the elbow, t>.08 - the round "end" of the cap) decays
  // faster so that part sits lower, without touching the peak itself.
  float sigma=t<.08f?.11f:.065f;
  float belly=Mathf.Exp(-Mathf.Pow((t-.08f)/sigma,2));
  float taper=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.08f,.02f,t));
  float bulge=belly*taper;
  p.y+=bulge*.016f;
  p.x+=Mathf.Sign(p.x)*bulge*.014f;
  p.z+=bulge*.012f;
  return p;
 }
 // MINI-166: "i want to make the characters look more muscular" / "just a
 // little more defined and muscular". Subtle bicep bulge on the exposed
 // short-sleeve arm skin - centred about a third of the way from shoulder
 // to elbow (the visual peak of a flexed/built bicep), tapered to zero at
 // both the shoulder seam and the elbow so it blends into unmoved geometry
 // rather than creating a visible seam. Deliberately small: the shoulder
 // round's own history (an initial steep trap lift was rejected) is the
 // reason to start conservative here too.
 static Vector3 BicepForm(Vector3 p,Surface s){
  var shoulder=s.Rest("LeftArm");var elbow=s.Rest("LeftForeArm");
  float sign=Mathf.Sign(p.x);float x=Mathf.Abs(p.x);
  float x0=Mathf.Abs(shoulder.x),x1=Mathf.Abs(elbow.x);
  if(Mathf.Abs(x1-x0)<.001f)return p;
  float t=Mathf.InverseLerp(x0,x1,x);
  if(t<-.1f||t>1.1f)return p;
  // Peak stays at t=.52 (just past the ~.46 sleeve hem, on EXPOSED skin) -
  // an earlier attempt centred at t=.32 sat entirely under the sleeve
  // fabric and, at any magnitude large enough to see, ballooned the whole
  // cap sleeve into a sphere. Confirmed by an exaggerated diagnostic render.
  // Real anatomy (Proko/Artists Network): the deltoid inserts ~1/3 down the
  // upper arm with NO gap before the bicep belly starts - but DeltoidForm's
  // own approved shape (its user-requested "dropped edge") already fades
  // out by ~t=.24-.28. Rather than re-tune the approved deltoid shape,
  // widen only the RISING (inner, t<.52) side of this Gaussian so it picks
  // up earlier and meets the deltoid's tail - the peak itself, and its
  // falloff toward the elbow, are unchanged, so the balloon risk above is
  // not reintroduced (only a partial, not peak, value now reaches under
  // the fabric near the sleeve hem).
  float sigmaIn=.24f,sigmaOut=.12f;
  float sigma=t<.52f?sigmaIn:sigmaOut;
  float belly=Mathf.Exp(-Mathf.Pow((t-.52f)/sigma,2));
  float taper=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.05f,.08f,t))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.88f,1.05f,t)));
  float bulge=belly*taper;
  // More mass on top/front (biceps proper) than underneath (triceps get a
  // smaller share) for a rounder, slightly flexed silhouette.
  float upFront=p.y>shoulder.y?1f:.6f;
  p.y+=bulge*.028f*upFront;
  p.z+=bulge*.024f;
  return p;
 }
 // Subtle pec/chest fullness under the shirt fabric - two shallow lobes
 // either side of the sternum line, well below the collar and above the
 // stomach, so it does not interact with the collar/neckline geometry.
 static Vector3 ChestForm(Vector3 p,Surface s,Vector3 neck){
  // First pass at .6 magnitude / neck.y-.10 centre (confirmed reachable by
  // an exaggerated diagnostic render) produced two round, high, breast-like
  // lobes near the collarbone - wrong shape and too high. Moved the centre
  // lower/wider (flatter pec, not a sphere) and cut magnitude by ~85%.
  float x=Mathf.Abs(p.x);
  float lobe=Mathf.Exp(-Mathf.Pow((x-.08f)/.06f,2));
  float height=Mathf.Exp(-Mathf.Pow((p.y-(neck.y-.135f))/.075f,2));
  float chest=lobe*height;
  p.z+=(p.z-neck.z)*chest*.09f;
  return p;
 }
 static float Front(Surface s,float x,float y){var nearby=s.vertices.Where(v=>Mathf.Abs(v.p.x-x)<.028f&&Mathf.Abs(v.p.y-y)<.035f).ToArray();return nearby.Length>0?nearby.Max(v=>v.p.z):.12f;}
 static Shape Pants(Surface s,List<List<V>> faces,int style){
  var result=new Shape(s);float knee=s.Rest("LeftLeg").y,hip=s.Rest("LeftUpLeg").y,foot=s.Rest("LeftFoot").y,cut=Mathf.Lerp(hip,knee,.83f);
  foreach(var face in faces){var f=style==2?Clip(face,p=>p.y-cut,true):face;if(f.Count<3)continue;
   f=f.Select(v=>{if(v.p.y<hip-.055f){var center=LegCenter(s,Mathf.Sign(v.p.x),v.p.y);var d=v.p-center;float radius=Mathf.Lerp(style==1?.047f:.038f,.066f,Mathf.InverseLerp(foot,hip-.055f,v.p.y));float angle=Mathf.Atan2(d.x,d.z);var smooth=center+new Vector3(Mathf.Sin(angle)*radius,0,Mathf.Cos(angle)*radius);v.p=Vector3.Lerp(v.p,smooth,style==1?.85f:.6f);v.n=Vector3.Lerp(v.n,new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle)),.6f).normalized;}return v;}).ToList();result.Poly(f,0);
  }
  if(style==2)foreach(int sign in new[]{-1,1}){
   int leg=s.Bone(sign<0?"LeftLeg":"RightLeg"),thigh=s.Bone(sign<0?"LeftUpLeg":"RightUpLeg");
   V Point(float y,float angle){var center=LegCenter(s,sign,y);float t=Mathf.InverseLerp(foot,knee,y),radius=Mathf.Lerp(.027f,.046f,Mathf.Sin(t*Mathf.PI*.65f));if(y>knee)radius=.047f;float mix=Mathf.SmoothStep(0,1,Mathf.InverseLerp(knee-.07f,knee+.10f,y));return new V{p=center+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius*.95f),n=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)),uv=new Vector2(angle/6.283f,t),w=new BoneWeight{boneIndex0=leg,weight0=1-mix,boneIndex1=thigh,weight1=mix}};}
   for(int row=0;row<14;row++)for(int i=0;i<24;i++){float a=Mathf.Lerp(foot-.015f,cut+.02f,row/14f),b=Mathf.Lerp(foot-.015f,cut+.02f,(row+1)/14f),u=i*Mathf.PI/12,z=(i+1)*Mathf.PI/12;result.Poly(new List<V>{Point(a,u),Point(b,u),Point(b,z),Point(a,z)},1);}
  }
  if(style!=1){float top=s.Rest("Hips").y-.065f;foreach(int sign in new[]{-1,1}){float x=sign*.09f,z=-.091f;result.Tube(new List<Vector3>{new Vector3(x-.023f,top,z),new Vector3(x-.02f,top-.06f,z-.01f),new Vector3(x,top-.071f,z-.014f),new Vector3(x+.02f,top-.06f,z-.01f),new Vector3(x+.023f,top,z)},.0011f,2);}}
  return result;
 }
 static Vector3 LegCenter(Surface s,float sign,float y){string side=sign<0?"Left":"Right";var hip=s.Rest(side+"UpLeg");var knee=s.Rest(side+"Leg");var foot=s.Rest(side+"Foot");return y>=knee.y?Vector3.Lerp(knee,hip,Mathf.InverseLerp(knee.y,hip.y,y)):Vector3.Lerp(foot,knee,Mathf.InverseLerp(foot.y,knee.y,y));}
}
}
