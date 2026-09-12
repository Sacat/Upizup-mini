using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace UpIzUpMini.EditorTools {
public static partial class Mini166Repair {
 struct V {public Vector3 p,n;public Vector2 uv;public BoneWeight w;}
 static BoneWeight Blend(BoneWeight a,BoneWeight b,float t){
  var d=new Dictionary<int,float>();
  Action<int,float> add=(i,f)=>{if(f>0)d[i]=(d.ContainsKey(i)?d[i]:0)+f;};
  add(a.boneIndex0,a.weight0*(1-t));add(a.boneIndex1,a.weight1*(1-t));add(a.boneIndex2,a.weight2*(1-t));add(a.boneIndex3,a.weight3*(1-t));
  add(b.boneIndex0,b.weight0*t);add(b.boneIndex1,b.weight1*t);add(b.boneIndex2,b.weight2*t);add(b.boneIndex3,b.weight3*t);
  var v=d.OrderByDescending(x=>x.Value).Take(4).ToArray();float sum=v.Sum(x=>x.Value);var w=new BoneWeight();
  if(v.Length>0){w.boneIndex0=v[0].Key;w.weight0=v[0].Value/sum;}if(v.Length>1){w.boneIndex1=v[1].Key;w.weight1=v[1].Value/sum;}if(v.Length>2){w.boneIndex2=v[2].Key;w.weight2=v[2].Value/sum;}if(v.Length>3){w.boneIndex3=v[3].Key;w.weight3=v[3].Value/sum;}return w;
 }
 static V Lerp(V a,V b,float t)=>new V{p=Vector3.Lerp(a.p,b.p,t),n=Vector3.Lerp(a.n,b.n,t).normalized,uv=Vector2.Lerp(a.uv,b.uv,t),w=Blend(a.w,b.w,t)};
 static List<V> Clip(List<V> poly,Func<Vector3,float> distance,bool positive){
  var result=new List<V>();if(poly.Count==0)return result;
  V a=poly[poly.Count-1];float da=distance(a.p)*(positive?1:-1);
  bool Inside(float d)=>positive?d>=0:d>0;
  foreach(var b in poly){float db=distance(b.p)*(positive?1:-1);if(Inside(da)!=Inside(db))result.Add(Lerp(a,b,da/(da-db)));if(Inside(db))result.Add(b);a=b;da=db;}return result;
 }
 class Surface {
  public V[] vertices;public int[][] triangles;public Transform[] bones;public Matrix4x4[] binds;public Material[] materials;public GameObject root;
  public Surface(){}
  public Surface(GameObject character,SkinnedMeshRenderer r,Mesh overrideMesh=null){
   root=character;var mesh=overrideMesh?overrideMesh:r.sharedMesh;var m=character.transform.worldToLocalMatrix*r.transform.localToWorldMatrix;var norm=m.inverse.transpose;
   var p=mesh.vertices;var n=mesh.normals;var uv=mesh.uv;var bw=mesh.boneWeights;
   vertices=p.Select((v,i)=>new V{p=m.MultiplyPoint3x4(v),n=norm.MultiplyVector(n[i]).normalized,uv=uv.Length==p.Length?uv[i]:Vector2.zero,w=bw[i]}).ToArray();
   triangles=Enumerable.Range(0,mesh.subMeshCount).Select(mesh.GetTriangles).ToArray();bones=r.bones;binds=mesh.bindposes.Select(b=>b*m.inverse).ToArray();materials=r.sharedMaterials;
  }
  public int Bone(string suffix){int i=Array.FindIndex(bones,b=>b&&b.name.EndsWith(":"+suffix));if(i<0)throw new Exception("Missing bone "+suffix);return i;}
  public Vector3 Rest(string suffix)=>binds[Bone(suffix)].inverse.MultiplyPoint3x4(Vector3.zero);
  public BoneWeight Weight(Vector3 p){float best=float.MaxValue;BoneWeight w=default;foreach(var v in vertices){float d=(v.p-p).sqrMagnitude;if(d<best){best=d;w=v.w;}}return w;}
  public IEnumerable<List<V>> Faces(int sub){var t=triangles[sub];for(int i=0;i<t.Length;i+=3)yield return new List<V>{vertices[t[i]],vertices[t[i+1]],vertices[t[i+2]]};}
 }
 static Surface Retarget(Surface from,Surface target){
  var map=from.bones.Select(b=>Array.FindIndex(target.bones,t=>t&&t.name.Split(':').Last()==b.name.Split(':').Last())).ToArray();if(map.Any(i=>i<0))throw new Exception("Retarget bone missing");
  var matrices=Enumerable.Range(0,map.Length).Select(i=>target.binds[map[i]].inverse*from.binds[i]).ToArray();
  var result=new Surface{root=target.root,bones=target.bones,binds=target.binds,triangles=from.triangles,materials=from.materials};
  result.vertices=from.vertices.Select(v=>{var w=v.w;var p=v.p;var n=v.n;v.p=matrices[w.boneIndex0].MultiplyPoint3x4(p)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(p)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(p)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(p)*w.weight3;v.n=(matrices[w.boneIndex0].MultiplyVector(n)*w.weight0+matrices[w.boneIndex1].MultiplyVector(n)*w.weight1+matrices[w.boneIndex2].MultiplyVector(n)*w.weight2+matrices[w.boneIndex3].MultiplyVector(n)*w.weight3).normalized;w.boneIndex0=map[w.boneIndex0];w.boneIndex1=map[w.boneIndex1];w.boneIndex2=map[w.boneIndex2];w.boneIndex3=map[w.boneIndex3];v.w=w;return v;}).ToArray();return result;
 }
 class Shape {
  public List<V> v=new List<V>();public List<int>[] tris;Dictionary<V,int> map=new Dictionary<V,int>();public Surface source;
  public Shape(Surface s,int slots=4){source=s;tris=Enumerable.Range(0,slots).Select(_=>new List<int>()).ToArray();}
  int Add(V a){if(map.TryGetValue(a,out int i))return i;i=v.Count;v.Add(a);map.Add(a,i);return i;}
  public void Poly(List<V> p,int slot){for(int i=1;i+1<p.Count;i++){if(Vector3.Cross(p[i].p-p[0].p,p[i+1].p-p[0].p).sqrMagnitude<1e-15f)continue;tris[slot].Add(Add(p[0]));tris[slot].Add(Add(p[i]));tris[slot].Add(Add(p[i+1]));}}
  public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int slot,int bone=-1,bool both=false){
   var n=Vector3.Cross(b-a,c-a).normalized;var points=new[]{a,b,c,d};var poly=points.Select((p,i)=>new V{p=p,n=n,uv=new Vector2((i==1||i==2)?1:0,i>=2?1:0),w=bone>=0?new BoneWeight{boneIndex0=bone,weight0=1}:source.Weight(p)}).ToList();Poly(poly,slot);if(both){poly.Reverse();for(int i=0;i<poly.Count;i++){var x=poly[i];x.n=-x.n;poly[i]=x;}Poly(poly,slot);}
  }
  public void Tube(List<Vector3> points,float radius,int slot,int bone=-1,int sides=6){
   for(int k=0;k+1<points.Count;k++){var tangent=(points[k+1]-points[k]).normalized;var axis=Vector3.Cross(tangent,Mathf.Abs(tangent.y)>.9f?Vector3.right:Vector3.up).normalized;var other=Vector3.Cross(tangent,axis);
    for(int j=0;j<sides;j++){float a=j*Mathf.PI*2/sides,b=(j+1)*Mathf.PI*2/sides;Vector3 u=radius*(axis*Mathf.Cos(a)+other*Mathf.Sin(a)),z=radius*(axis*Mathf.Cos(b)+other*Mathf.Sin(b));Quad(points[k]+u,points[k]+z,points[k+1]+z,points[k+1]+u,slot,bone);}
   }
  }
  public Mesh Mesh(string name){var mesh=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(v.Select(x=>x.p).ToList());mesh.SetNormals(v.Select(x=>x.n).ToList());mesh.SetUVs(0,v.Select(x=>x.uv).ToList());mesh.boneWeights=v.Select(x=>x.w).ToArray();mesh.bindposes=source.binds;mesh.subMeshCount=tris.Length;for(int i=0;i<tris.Length;i++)mesh.SetTriangles(tris[i],i);mesh.RecalculateBounds();return mesh;}
 }
 static T Asset<T>(T obj,string name) where T:UnityEngine.Object {
  string path=Art+"/"+name;var old=AssetDatabase.LoadAssetAtPath<T>(path);if(old){
   if(old is Mesh destination && obj is Mesh source){destination.Clear();destination.indexFormat=source.indexFormat;destination.vertices=source.vertices;destination.normals=source.normals;destination.uv=source.uv;destination.boneWeights=source.boneWeights;destination.bindposes=source.bindposes;destination.subMeshCount=source.subMeshCount;for(int i=0;i<source.subMeshCount;i++)destination.SetTriangles(source.GetTriangles(i),i);destination.RecalculateBounds();}
   else EditorUtility.CopySerialized(obj,old);
   UnityEngine.Object.DestroyImmediate(obj);EditorUtility.SetDirty(old);return old;}AssetDatabase.CreateAsset(obj,path);return obj;
 }
 static Material Matte(string name,Color colour,float gloss=.08f){var mat=new Material(Shader.Find("Standard")){name=name,color=colour};mat.SetFloat("_Glossiness",gloss);mat.SetFloat("_Metallic",0);
  if(name=="Cotton"||name=="Denim"){
   var tex=new Texture2D(128,128,TextureFormat.RGB24,true){name=name+"Weave",wrapMode=TextureWrapMode.Repeat};var pixels=new Color[128*128];for(int y=0;y<128;y++)for(int x=0;x<128;x++){float weave=((x+y*2)%5==0)?.91f:1f;float value=weave*(.92f+.08f*Mathf.PerlinNoise(x*.12f,y*.12f));pixels[y*128+x]=new Color(value,value,value);}tex.SetPixels(pixels);tex.Apply();mat.mainTexture=Asset(tex,name+"Weave.asset");mat.mainTextureScale=Vector2.one*(name=="Denim"?8:5);
  }
  return Asset(mat,name+".mat");}
 static SkinnedMeshRenderer Bind(GameObject root,string name,Surface source){
  var child=root.transform.Find(name);if(!child){child=new GameObject(name).transform;child.SetParent(root.transform,false);}var r=child.GetComponent<SkinnedMeshRenderer>();if(!r)r=child.gameObject.AddComponent<SkinnedMeshRenderer>();r.bones=source.bones;r.rootBone=source.bones[0];r.updateWhenOffscreen=true;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;return r;
 }
}
}
