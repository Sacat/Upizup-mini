using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace UpIzUpMini.Character {
/// <summary>Fits the existing welded necklace smoothly to the worn torso at equip time.
/// Source meshes stay untouched. Not a cloth simulation or an animation collision solver.</summary>
public sealed class ChainGarmentFit : MonoBehaviour {
 readonly List<Mesh> owned=new List<Mesh>();
 public static void Fit(GameObject chain,Transform character,Animator animator){
 if(character.name!="Sacat" && character.name!="Franki")return;
 var neck=animator.GetBoneTransform(HumanBodyBones.Neck);if(!neck)return;
 var fit=chain.AddComponent<ChainGarmentFit>();var surfaces=new List<MeshCollider>();var baked=new List<Mesh>();
 try {
 foreach(var r in character.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled && r.gameObject.activeInHierarchy)){
 if(!r.sharedMesh || r.transform.IsChildOf(chain.transform))continue;
 var m=new Mesh();r.BakeMesh(m);baked.Add(m);var temp=new GameObject("ChainFitTemporarySurface");temp.hideFlags=HideFlags.HideAndDontSave;temp.transform.SetPositionAndRotation(r.transform.position,r.transform.rotation);temp.transform.localScale=r.transform.lossyScale;var col=temp.AddComponent<MeshCollider>();col.sharedMesh=m;surfaces.Add(col);
 }
 Physics.SyncTransforms();int fitted=0;
 foreach(var filter in chain.GetComponentsInChildren<MeshFilter>()){
 var source=filter.sharedMesh;if(!source || !source.isReadable){Debug.Log("CHAIN_UNREADABLE "+filter.name);continue;}var v=source.vertices;var world=v.Select(p=>filter.transform.TransformPoint(p)).ToArray();
// This imported necklace is a welded loop, not disconnected link objects.
 // Sample a smooth displacement field around its projected oval; interpolate
 // it per vertex so no artificial cuts appear between neighbouring samples.
 Vector3 mid=Vector3.zero;foreach(var p in world)mid+=p;mid/=world.Length;
 float rx=.001f,ry=.001f;foreach(var p in world){rx=Mathf.Max(rx,Mathf.Abs(Vector3.Dot(p-mid,character.right)));ry=Mathf.Max(ry,Mathf.Abs(Vector3.Dot(p-mid,character.up)));}
 const int samples=48;var centers=new Vector3[samples];var count=new int[samples];var phase=new float[v.Length];
 for(int i=0;i<v.Length;i++){var d=world[i]-mid;float a=Mathf.Atan2(Vector3.Dot(d,character.up)/ry,Vector3.Dot(d,character.right)/rx);phase[i]=(a+Mathf.PI)/(2*Mathf.PI)*samples;int bin=Mathf.FloorToInt(phase[i])%samples;centers[bin]+=world[i];count[bin]++;}
 var shiftField=new Vector3[samples];
 for(int bin=0;bin<samples;bin++){
 if(count[bin]==0)continue;Vector3 center=centers[bin]/count[bin];Vector3 axis=center;axis-=character.forward*Vector3.Dot(center-neck.position,character.forward);bool back=center.y>neck.position.y-.025f && Vector3.Dot(center-neck.position,character.forward)<0;Vector3 radial=character.forward*(back?-1:1);
 var ray=new Ray(axis+radial*.75f,-radial);float nearest=float.PositiveInfinity;Vector3 surface=Vector3.zero;bool hitAny=false;
 foreach(var col in surfaces)if(col.Raycast(ray,out var hit,1.5f) && hit.distance<nearest){nearest=hit.distance;surface=hit.point;hitAny=true;}
 if(!hitAny)continue;
 float radius=.006f;
float amount=Mathf.Clamp(Vector3.Dot(surface-center,radial)+radius+.005f,-.10f,.10f);shiftField[bin]=radial*amount;fitted++;
 }
 for(int i=0;i<v.Length;i++){float f=phase[i]-.5f;int b=Mathf.FloorToInt(f);float blend=f-b;Vector3 shift=Vector3.Lerp(shiftField[(b+samples)%samples],shiftField[(b+1+samples)%samples],blend);v[i]=filter.transform.InverseTransformPoint(world[i]+shift);}
 var copy=Instantiate(source);copy.name=source.name+"_GarmentFit";copy.vertices=v;copy.RecalculateBounds();copy.RecalculateNormals();filter.sharedMesh=copy;fit.owned.Add(copy);
 }
 Debug.Log("ChainGarmentFit "+character.name+" fittedArcSamples="+fitted);
 }finally{foreach(var c in surfaces){c.enabled=false;Destroy(c.gameObject);}foreach(var m in baked)Destroy(m);}
 }
 void OnDestroy(){foreach(var m in owned)if(m)Destroy(m);}
}}



