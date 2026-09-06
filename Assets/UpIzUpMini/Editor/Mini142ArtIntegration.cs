using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UpIzUpMini.Farming;
using Object = UnityEngine.Object;

namespace UpIzUpMini.EditorTools
{
    public static partial class Mini142ArtIntegration
    {
        const string ScenePath = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string Out = "Logs/Tasks/MINI-142";
        const string Art = "Assets/UpIzUpMini/Art/Environment/Mini142";
        static Transform[] All() => Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent)+"/"+t.name;
        static bool IsRoad(Collider c) => c.name.StartsWith("Road_") || c.name.StartsWith("JunctionPatch") || c.name.StartsWith("MBRoad") || c.name.Contains("BridgeDeck") || (PathOf(c.transform).Contains("/Bridges/") && c.name.Contains("Deck")) || c.name=="DogLifeJunction_MINI142";
        static Bounds BoundsOf(Transform t)
        {
            var rs=t.GetComponentsInChildren<Renderer>(true).Where(r=>r is MeshRenderer).ToArray();
            if(rs.Length==0)return new Bounds(t.position,Vector3.zero);
            Bounds b=rs[0].bounds; foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds); return b;
        }
        public static void RoadCheck()
        {
            EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);Physics.SyncTransforms();
            var lines=new List<string>();
            foreach(var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c=>PathOf(c.transform).Contains("Bridge_00")))
                lines.Add("BRIDGE "+PathOf(c.transform)+" "+c.bounds);
            for(float z=-145;z<=-99;z+=1)
            {
                float x=-63.293f-(z+144.473f)*.066f;
                var hits=Physics.RaycastAll(new Vector3(x,100,z),Vector3.down,200).OrderByDescending(h=>h.point.y);
                lines.Add($"X={x:F2} Z={z:F2} "+string.Join(" | ",hits.Where(h=>IsRoad(h.collider)||GroundName(h.collider)).Select(h=>$"{h.collider.name}:{h.point.y:F3}")));
            }
            File.WriteAllLines(Out+"/road-drive-line.txt",lines);
            Capture("Before-DogLife-BridgeApproach",new Vector3(-58,18,-126),new Vector3(-65,9.5f,-107));
            Debug.Log("MINI142 ROAD CHECK captured");
        }
        public static void Survey()
        {
            Directory.CreateDirectory(Out);
            EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single); Physics.SyncTransforms();
            var lines=new List<string>();
            var all=All();
            foreach(var t in all.Where(t=>t.name.StartsWith("Lalay_House_")||t.name.StartsWith("Highland_House_")).Take(12))
                lines.Add($"HOUSE {PathOf(t)} pos={t.position:F3} rot={t.eulerAngles:F2} scale={t.lossyScale:F3} bounds={BoundsOf(t)} children="+string.Join(",",t.Cast<Transform>().Select(c=>c.name)));
            foreach(var t in all.Where(t=>t.name.StartsWith("Road_")||t.name.StartsWith("JunctionPatch")||t.name.StartsWith("Bridge_")))
            {
                var b=BoundsOf(t);
                if(b.max.x < -80 || b.min.x>0 || b.max.z< -180 || b.min.z> -90)continue;
                lines.Add($"ROAD {PathOf(t)} active={t.gameObject.activeInHierarchy} bounds={b}");
                var mf=t.GetComponent<MeshFilter>();
                if(mf!=null&&mf.sharedMesh!=null)
                {
                    var near=mf.sharedMesh.vertices.Select(v=>t.TransformPoint(v)).Where(v=>v.x< -50&&v.x> -77&&v.z< -132&&v.z> -158).ToArray();
                    lines.Add("NEAR_VERTS "+string.Join(";",near.Select(v=>v.ToString("F3"))));
                }
            }
            var plots=Object.FindObjectsByType<FarmPlot>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            lines.Add("PLOTS "+plots.Length);
            foreach(var p in plots.Take(2))
            {
                lines.Add($"PLOT {PathOf(p.transform)} pos={p.transform.position} scale={p.transform.lossyScale}");
                foreach(var v in p.GetComponentsInChildren<CropStageVisual>(true)) lines.Add("VISUAL "+EditorJsonUtility.ToJson(v));
            }
            foreach(var t in all.Where(t=>t.name.Contains("DogLife")||t.name=="Stall_FARM SHOP"||t.name=="Stall_PRODUCE BUYER"))
                lines.Add($"ROLE {PathOf(t)} pos={t.position}");
            File.WriteAllLines(Out+"/survey.txt",lines);
            Capture("Before-DogLife-Junction", new Vector3(-47,28,-169), new Vector3(-62,9,-145), 1280,800);
            Capture("Before-Lalay-Houses",new Vector3(-20,19,-169),new Vector3(-6,12,-151),1280,800);
            Debug.Log("MINI142 SURVEY PASS "+lines.Count+" lines");
        }
        static void Capture(string name,Vector3 position,Vector3 target,int width=1280,int height=800)
        {
            var go=new GameObject("MINI142 Evidence Camera"); var cam=go.AddComponent<Camera>();
            cam.transform.position=position; cam.transform.LookAt(target);
            cam.fieldOfView=55; cam.nearClipPlane=.05f;cam.farClipPlane=1400;
            cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.36f,.52f,.60f);
            var rt=new RenderTexture(width,height,24);cam.targetTexture=rt;cam.Render();
            var old=RenderTexture.active;RenderTexture.active=rt;
            var tex=new Texture2D(width,height,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();
            File.WriteAllBytes(Out+"/"+name+".png",tex.EncodeToPNG());
            RenderTexture.active=old;cam.targetTexture=null;rt.Release();
            Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);Object.DestroyImmediate(go);
        }
    }
}
