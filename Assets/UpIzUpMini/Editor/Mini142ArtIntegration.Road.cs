using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UpIzUpMini.EditorTools
{
    public static partial class Mini142ArtIntegration
    {
        // Measured existing west-Lalay grade. Only repair intersecting ribbons here;
        // no terrain, building, sidewalk, road X/Z or special-role relocation.
        static float WestGrade(float z) => 8.878f + (z+144.473f)*.01488f;
        static Vector2 WestEdges(float z)
        {
            var go=GameObject.Find("Road_way_23042701");var mesh=go.GetComponent<MeshFilter>().sharedMesh;
            var vertices=mesh.vertices.Select(v=>go.transform.TransformPoint(v)).ToArray();var triangles=mesh.triangles;
            float lo=float.PositiveInfinity,hi=float.NegativeInfinity;
            for(int i=0;i<triangles.Length;i+=3)for(int j=0;j<3;j++)
            {
                var a=vertices[triangles[i+j]];var b=vertices[triangles[i+(j+1)%3]];
                if(Mathf.Abs(a.z-b.z)<.0001f||z<Mathf.Min(a.z,b.z)||z>Mathf.Max(a.z,b.z))continue;
                float x=Mathf.Lerp(a.x,b.x,(z-a.z)/(b.z-a.z));lo=Mathf.Min(lo,x);hi=Mathf.Max(hi,x);
            }
            if(float.IsInfinity(lo))throw new Exception("Missing authoritative road section "+z);
            return new Vector2(lo,hi);
        }
        static void SmoothDogLifeSeams()
        {
            foreach(var name in new[]{"Road_way_387239000","Road_Connector_00_way_387239000_way_23042701"})
            {
                var go=GameObject.Find(name);if(go==null)throw new Exception("Missing surveyed road "+name);
                var mf=go.GetComponent<MeshFilter>();var original=mf.sharedMesh;
                var mesh=Object.Instantiate(original);mesh.name=name+"_MINI142";var vertices=mesh.vertices;int changed=0;
                for(int i=0;i<vertices.Length;i++)
                {
                    var p=go.transform.TransformPoint(vertices[i]);
                    if(p.z < -140 || p.z > -124)continue;
                    var edges=WestEdges(p.z);float centreX=(edges.x+edges.y)*.5f;
                    float distance=Mathf.Abs(p.x-centreX);
                    float blend=name.Contains("Connector_00")?1:1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(3.3f,7.5f,distance));
                    if(blend<=0)continue;
                    p.y=Mathf.Lerp(p.y,WestGrade(p.z),blend);
                    vertices[i]=go.transform.InverseTransformPoint(p);changed++;
                }
                mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();
                string path=Art+"/"+mesh.name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(saved!=null){EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);mesh=saved;}
                else AssetDatabase.CreateAsset(mesh,path);
                mf.sharedMesh=mesh;var collider=go.GetComponent<MeshCollider>();
                if(collider==null)throw new Exception("Road missing mesh collision "+name);
                collider.sharedMesh=mesh;
                Debug.Log("MINI142 SEAM "+name+" adjusted vertices="+changed+"; X/Z preserved");
            }
            Physics.SyncTransforms();ValidateDogLifeDriveLines();
        }
        static void ValidateDogLifeDriveLines()
        {
            var lines=new List<string>();float maximumStep=0;int count=0;
            foreach(float lane in new[]{-1.5f,0,1.5f})
            {
                float previous=float.NaN;
                for(float z=-143;z<=-125;z+=.25f)
                {
                    var edges=WestEdges(z);float x=Mathf.Lerp(edges.x,edges.y,.5f+lane/6.2f);
                    var hits=Physics.RaycastAll(new Vector3(x,100,z),Vector3.down,150).Where(h=>IsRoad(h.collider)).OrderByDescending(h=>h.point.y).ToArray();
                    if(hits.Length==0)throw new Exception("Road collision gap "+x+","+z);
                    float y=hits[0].point.y;
                    if(!float.IsNaN(previous))maximumStep=Mathf.Max(maximumStep,Mathf.Abs(y-previous));
                    previous=y;count++;lines.Add($"lane={lane:F1} x={x:F3} z={z:F3} y={y:F3} {hits[0].collider.name}");
                }
            }
            lines.Insert(0,$"Samples={count}; maximum height change per 0.25 m={maximumStep:F4} m; three drive lines, west Lalay overlap only.");
            File.WriteAllLines(Out+"/road-after-drive-lines.txt",lines);
            if(maximumStep>.045f)throw new Exception("Unexpected road step "+maximumStep);
            Debug.Log("MINI142 ROAD COLLISION PASS samples="+count+" maximum step="+maximumStep);
        }
    }
}
