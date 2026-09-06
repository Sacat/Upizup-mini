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
        [Serializable] class Part { public string name; public string material; public Vector3[] vertices; public Vector3[] normals; public int[] triangles; public Vector2[] uv; }
        [Serializable] class Detail { public string name; public Part[] parts; }
        [Serializable] class Model { public string name; public Part[] parts; public Vector3 dimensions; public Detail[] lods; }
        static Dictionary<string,Model> models=new Dictionary<string,Model>();
        static Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();
        static Material paletteMaterial,fruitMaterial;
        const string ReviewScene="Assets/UpIzUpMini/Scenes/GrandBayProof_Mini142Review.unity";
        static bool GroundName(Collider c) => c.name=="Copernicus_GLO30_Terrain" || c.name.StartsWith("Lalay_Frontage_") || c.name.Contains("GroundPlatform") || c.name.StartsWith("Terrain_");
        static float GroundHeight(Vector3 p,float fallback)
        {
            float y=float.NegativeInfinity;
            foreach(var hit in Physics.RaycastAll(new Vector3(p.x,1000,p.z),Vector3.down,1500))
                if(GroundName(hit.collider)) y=Mathf.Max(y,hit.point.y);
            return float.IsNegativeInfinity(y)?fallback:y;
        }
        static void ImportModels()
        {
            Directory.CreateDirectory(Art);
            File.Copy(Out+"/Export/palette.png",Art+"/palette.png",true);
            AssetDatabase.Refresh();
            var ti=(TextureImporter)AssetImporter.GetAtPath(Art+"/palette.png");
            ti.textureType=TextureImporterType.Default;ti.sRGBTexture=true;ti.mipmapEnabled=false;ti.filterMode=FilterMode.Point;
            ti.textureCompression=TextureImporterCompression.Uncompressed;ti.maxTextureSize=128;ti.SaveAndReimport();
            paletteMaterial=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Palette.mat");
            if(paletteMaterial==null){paletteMaterial=new Material(Shader.Find("UpIzUpMini/ApprovedArtDiffuse"));AssetDatabase.CreateAsset(paletteMaterial,Art+"/Palette.mat");}
            paletteMaterial.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/palette.png");paletteMaterial.color=Color.white;paletteMaterial.enableInstancing=true;
            fruitMaterial=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Fruit.mat");
            if(fruitMaterial==null){fruitMaterial=new Material(Shader.Find("UpIzUpMini/ApprovedArtDiffuse"));AssetDatabase.CreateAsset(fruitMaterial,Art+"/Fruit.mat");}
            fruitMaterial.color=Color.white;fruitMaterial.enableInstancing=true;
            models.Clear();meshes.Clear();
            foreach(string name in new[]{"HouseOneStorey","HouseTwoStorey","GrassTuft","Carrot","Banana"})
            {
                var m=JsonUtility.FromJson<Model>(File.ReadAllText(Out+"/Export/"+name+".json"));models[name]=m;
                ImportParts(name,m.parts);
                if(m.lods!=null) foreach(var lod in m.lods)ImportParts(name+"_"+lod.name,lod.parts);
            }
            AssetDatabase.SaveAssets();
        }
        static void ImportParts(string prefix,Part[] parts)
        {
            foreach(var p in parts)
            {
                string key=prefix+"_"+p.name,path=Art+"/"+key+".asset";
                var me=AssetDatabase.LoadAssetAtPath<Mesh>(path);bool fresh=me==null;
                if(fresh)me=new Mesh();else me.Clear();
                me.name=key;me.indexFormat=p.vertices.Length>65535?UnityEngine.Rendering.IndexFormat.UInt32:UnityEngine.Rendering.IndexFormat.UInt16;
                me.vertices=p.vertices;me.triangles=p.triangles;me.normals=p.normals;me.uv=p.uv;me.RecalculateBounds();me.RecalculateTangents();
                if(fresh)AssetDatabase.CreateAsset(me,path);else EditorUtility.SetDirty(me);
                meshes[key]=me;
            }
        }
        static MeshRenderer MeshObject(string name,Transform parent,Mesh mesh,Material mat)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=mat;return r;
        }
        static int PatchHouses()
        {
            int count=0;
            foreach(var t in All().Where(t=>(t.name.StartsWith("Lalay_House_")||t.name.StartsWith("Highland_House_"))&&t.gameObject.activeInHierarchy).ToArray())
            {
                if(t.Find("ApprovedHouse_MINI142")!=null)continue;
                var body=t.Find("Body");if(body==null||body.GetComponent<Renderer>()==null)continue;
                // Only plain generated residences. Never touch gameplay properties or NPC/store roots.
                if(t.GetComponentsInChildren<MonoBehaviour>(true).Any(m=>m!=null && !(m is Unity.AI.Navigation.NavMeshModifier)))continue;
                bool two=t.name.Contains("TwoStorey");string key=two?"HouseTwoStorey":"HouseOneStorey";
                var model=models[key];var originalBounds=body.GetComponent<Renderer>().bounds;
                var root=new GameObject("ApprovedHouse_MINI142");root.transform.SetParent(t,false);
                root.transform.localRotation=Quaternion.Euler(0,180,0);
                // Fit porch and roof within the original core footprint so no new road encroachment.
                root.transform.localScale=new Vector3(body.localScale.x/model.dimensions.x,body.localScale.y/(two?5.8f:3.05f),body.localScale.z/model.dimensions.z);
                float baseY=originalBounds.min.y;
                float sampled=GroundHeight(t.position,baseY);
                if(Mathf.Abs(sampled-baseY)<2f)baseY=Mathf.Max(baseY,sampled+.01f);
                root.transform.position=new Vector3(t.position.x,baseY,t.position.z);
                foreach(var r in t.GetComponentsInChildren<Renderer>(true))r.enabled=false;
                foreach(var c in t.GetComponentsInChildren<Collider>(true))c.enabled=false;
                var lod0=MeshObject("House_LOD0",root.transform,meshes[key+"_Body"],paletteMaterial);
                var lod1=MeshObject("House_LOD1",root.transform,meshes[key+"_LOD1_Body"],paletteMaterial);
                var group=root.AddComponent<LODGroup>();group.SetLODs(new[]{new LOD(.11f,new Renderer[]{lod0}),new LOD(.008f,new Renderer[]{lod1})});group.RecalculateBounds();
                var box=root.AddComponent<BoxCollider>();float h=two?5.5f:2.75f;
                box.center=new Vector3(0,.30f+h*.5f,0);box.size=new Vector3(5.6f,h,4.7f);
                // Visible base with matching collision. Door itself stays a facade on these generic houses.
                var baseBox=root.AddComponent<BoxCollider>();baseBox.center=new Vector3(0,.16f,0);baseBox.size=new Vector3(5.74f,.32f,4.84f);
                count++;
            }
            Debug.Log("MINI142 HOUSES "+count+" generic residences fitted; special properties untouched");
            return count;
        }
        static CropStageVisual CreateCrop(FarmPlot plot,string id)
        {
            var root=new GameObject(id=="banana"?"BananaVisual_MINI142":"CarrotVisual_MINI142");
            root.transform.SetParent(plot.transform,false);
            var ps=plot.transform.lossyScale;root.transform.localScale=new Vector3(1/ps.x,1/ps.y,1/ps.z);
            // Soil top, not the plot's heavily scaled local Y.
            root.transform.position=new Vector3(plot.transform.position.x,plot.transform.position.y+Mathf.Abs(ps.y)*.5f+.005f,plot.transform.position.z);
            var plant=new GameObject("Plant");plant.transform.SetParent(root.transform,false);
            string model=id=="banana"?"Banana":"Carrot";var fruit=new List<Renderer>();
            foreach(var p in models[model].parts)
            {
                bool isFruit=id=="banana"?p.name=="Fruit":p.name=="Body";
                var r=MeshObject(p.name,plant.transform,meshes[model+"_"+p.name],isFruit?fruitMaterial:paletteMaterial);
                if(isFruit)fruit.Add(r);
            }
            var v=root.AddComponent<CropStageVisual>();var so=new SerializedObject(v);
            so.FindProperty("plantRoot").objectReferenceValue=plant.transform;so.FindProperty("plantRenderer").objectReferenceValue=plant.GetComponentInChildren<Renderer>();
            so.FindProperty("fullScale").floatValue=1;var a=so.FindProperty("fruitRenderers");a.arraySize=fruit.Count;
            for(int i=0;i<fruit.Count;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=fruit[i];
            so.ApplyModifiedPropertiesWithoutUndo();root.SetActive(false);return v;
        }
        static int PatchCrops()
        {
            int count=0;
            foreach(var plot in Object.FindObjectsByType<FarmPlot>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                foreach(var id in new[]{"carrot","banana"})
                {
                    var so=new SerializedObject(plot);var a=so.FindProperty("cropVisuals");int index=-1;
                    for(int i=0;i<a.arraySize;i++)if(a.GetArrayElementAtIndex(i).FindPropertyRelative("cropId").stringValue==id)index=i;
                    if(index>=0)
                    {
                        var old=a.GetArrayElementAtIndex(index).FindPropertyRelative("visual").objectReferenceValue as CropStageVisual;
                        if(old!=null&&old.name.EndsWith("_MINI142"))continue;
                        if(old!=null){old.SetVisible(false);old.name="Legacy_"+old.name+"_MINI142";}
                    }
                    if(index<0){index=a.arraySize;a.InsertArrayElementAtIndex(index);}
                    var visual=CreateCrop(plot,id);
                    var entry=a.GetArrayElementAtIndex(index);entry.FindPropertyRelative("cropId").stringValue=id;entry.FindPropertyRelative("visual").objectReferenceValue=visual;
                    so.ApplyModifiedPropertiesWithoutUndo();count++;
                }
            }
            Debug.Log("MINI142 CROP REGISTRY "+count+" visuals wired; tomato/weed and save IDs retained");
            return count;
        }
        static int AddGrass()
        {
            if(GameObject.Find("ApprovedGrass_MINI142")!=null)return 0;
            var root=new GameObject("ApprovedGrass_MINI142");var random=new System.Random(142);
            var chunks=new Dictionary<Vector2Int,List<CombineInstance>>();
            int count=0;var mesh=meshes["GrassTuft_Foliage"];
            foreach(var t in All().Where(t=>t.name=="ApprovedHouse_MINI142").ToArray())
            {
                var shell=t.GetComponent<BoxCollider>();
                for(int i=0;i<10;i++)
                {
                    // At side/back corners only: never in the front approach to a house.
                    float x=(i%2==0?-1:1)*(2.9f+(float)random.NextDouble()*.35f),z=-1.7f+(float)random.NextDouble()*3.8f;
                    var p=t.TransformPoint(new Vector3(x,0,z));p.y=GroundHeight(p,t.position.y);
                    var hits=Physics.RaycastAll(p+Vector3.up*30,Vector3.down,60);
                    if(hits.Any(h=>IsRoad(h.collider)||h.collider.name.Contains("Sidewalk")||h.collider.name.Contains("Shore")||h.collider.name.Contains("Beach")))continue;
                    if(Physics.OverlapSphere(p+Vector3.up*.14f,.12f).Any(c=>!GroundName(c)&&c!=shell&&c.name!="Copernicus_GLO30_Terrain"))continue;
                    var key=new Vector2Int(Mathf.FloorToInt(p.x/20),Mathf.FloorToInt(p.z/20));
                    if(!chunks.ContainsKey(key))chunks[key]=new List<CombineInstance>();
                    float scale=.8f+(float)random.NextDouble()*.6f;
                    chunks[key].Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(p+Vector3.up*.008f,Quaternion.Euler(0,(float)random.NextDouble()*360,0),Vector3.one*scale)});
                    count++;
                }
            }
            foreach(var kv in chunks)
            {
                var me=new Mesh{name="Grass_"+kv.Key.x+"_"+kv.Key.y};me.CombineMeshes(kv.Value.ToArray(),true,true);me.RecalculateBounds();
                string path=Art+"/"+me.name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(old!=null){EditorUtility.CopySerialized(me,old);Object.DestroyImmediate(me);me=old;}else AssetDatabase.CreateAsset(me,path);
                var r=MeshObject(me.name,root.transform,me,paletteMaterial);r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                var lod=r.gameObject.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.035f,new Renderer[]{r})});lod.RecalculateBounds();
            }
            Debug.Log("MINI142 GRASS "+count+" tufts in "+chunks.Count+" chunks; zero colliders, distance culled");
            return count;
        }
        static void RepairRoadPreview()
        {
            if(GameObject.Find("DogLifeJunction_MINI142")!=null)return;
            var origin=new Vector3(-63.293f,8.878f,-144.473f);
            // Measured road edges turn sharply at the shared centre. Cover the exposed inside wedge with a graded round junction.
            var points=new List<Vector3>{origin};const int segments=48;const float radius=4.30f;
            for(int i=0;i<=segments;i++)
            {
                float a=i*Mathf.PI*2/segments;var p=origin+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);
                p.y=origin.y-.015f*(p.x-origin.x)+.015f*Mathf.Max(0,p.z-origin.z);
                points.Add(p);
            }
            var tris=new List<int>();for(int i=1;i<=segments;i++){tris.Add(0);tris.Add(i+1);tris.Add(i);}
            // Lift the planar join 1 cm above the older ribbons to avoid flicker; grade, not a tall floating slab.
            for(int i=0;i<points.Count;i++)points[i]+=Vector3.up*.012f;
            var me=new Mesh{name="DogLifeJunctionMesh"};me.SetVertices(points);me.SetTriangles(tris,0);me.RecalculateNormals();me.RecalculateBounds();
            var path=Art+"/DogLifeJunction.asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old!=null){EditorUtility.CopySerialized(me,old);Object.DestroyImmediate(me);me=old;}else AssetDatabase.CreateAsset(me,path);
            var road=GameObject.Find("Road_way_22917921");var r=MeshObject("DogLifeJunction_MINI142",road.transform.parent,me,road.GetComponent<Renderer>().sharedMaterial);
            r.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);r.gameObject.AddComponent<MeshCollider>().sharedMesh=me;
            var legacy=GameObject.Find("JunctionPatch_22917921_23042701");if(legacy!=null)legacy.SetActive(false);
            Physics.SyncTransforms();
        }
        static void CheckCrops()
        {
            foreach(var plot in Object.FindObjectsByType<FarmPlot>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            foreach(var visual in plot.GetComponentsInChildren<CropStageVisual>(true).Where(v=>v.name.EndsWith("_MINI142")&&!v.name.StartsWith("Legacy")))
            {
                var so=new SerializedObject(visual);var plant=so.FindProperty("plantRoot").objectReferenceValue as Transform;
                if(plant==null)throw new Exception("Missing crop root");
                var a=so.FindProperty("fruitRenderers");
                for(int stage=0;stage<4;stage++)
                {
                    visual.ApplyStage(stage,Color.green,Color.yellow);
                    for(int i=0;i<a.arraySize;i++)
                    {
                        var r=a.GetArrayElementAtIndex(i).objectReferenceValue as Renderer;
                        if(r==null||!r.transform.IsChildOf(plant))throw new Exception("Fruit detached from growth root");
                        if(r.gameObject.activeSelf!=(stage>=2))throw new Exception("Wrong fruit visibility");
                    }
                }
                visual.SetVisible(false);
            }
            Debug.Log("MINI142 CROP STAGE PASS: attachment, growth and fruit visibility all four stages");
        }
        public static void Review()
        {
            Directory.CreateDirectory(Out);ImportModels();
            EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);Physics.SyncTransforms();
            if(!File.Exists(Out+"/GrandBayProof-Before-MINI142.unity"))File.Copy(ScenePath,Out+"/GrandBayProof-Before-MINI142.unity");
            int house=PatchHouses();if(house==0)throw new Exception("No residential models fitted");Physics.SyncTransforms();PatchCrops();AddGrass();CheckCrops();RepairRoadPreview();SmoothDogLifeSeams();
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),ReviewScene);
            Capture("After-DogLife-Junction-REVIEW",new Vector3(-47,28,-169),new Vector3(-62,9,-145));
            Capture("After-Lalay-Houses-REVIEW",new Vector3(-20,19,-169),new Vector3(-6,12,-151));
            Capture("After-HouseFront-REVIEW",new Vector3(-21,13,-151),new Vector3(-15,10,-160));
            CaptureCrops();
            Debug.Log("MINI142 REVIEW PASS houses="+house+"; live scene unchanged");
        }
        static void CaptureCrops()
        {
            var plots=Object.FindObjectsByType<FarmPlot>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).OrderBy(p=>p.transform.position.z).ThenBy(p=>p.transform.position.x).Take(2).ToArray();
            for(int i=0;i<plots.Length;i++)
            {
                var name=i==0?"BananaVisual_MINI142":"CarrotVisual_MINI142";var t=plots[i].transform.Find(name);if(t==null)continue;
                t.gameObject.SetActive(true);var v=t.GetComponent<CropStageVisual>();
                v.ApplyStage(3,Color.green,i==0?new Color(.91f,.82f,.23f):new Color(.88f,.38f,.06f));
                var p=t.position;
                Capture("After-"+(i==0?"Banana":"Carrot")+"-InFarm-REVIEW",p+new Vector3(3.5f,3.0f,-4.5f),p+Vector3.up*(i==0?1.25f:.25f));
                t.gameObject.SetActive(false);
            }
        }
        static Dictionary<string,Vector3> ProtectedPositions()
        {
            var result=new Dictionary<string,Vector3>();
            foreach(var t in All())
            {
                if(!t.GetComponents<MonoBehaviour>().Any(m=>m!=null&&!(m is CropStageVisual)))continue;
                result[PathOf(t)]=t.position;
            }
            return result;
        }
        public static void ApplyReviewedAndBuild()
        {
            // User requested continuation/build after seeing the Unity house preview.
            // Never regenerate the canonical world: only promote this checked additive review.
            string backup=Out+"/GrandBayProof-Before-MINI142.unity";
            using(var sha=System.Security.Cryptography.SHA256.Create())
            {
                if(!File.Exists(backup)||!sha.ComputeHash(File.ReadAllBytes(ScenePath)).SequenceEqual(sha.ComputeHash(File.ReadAllBytes(backup))))
                    throw new Exception("Canonical scene changed since review backup. Preserve user edits and re-review; refusing promotion.");
            }
            EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);var before=ProtectedPositions();
            EditorSceneManager.OpenScene(ReviewScene,OpenSceneMode.Single);Physics.SyncTransforms();var after=ProtectedPositions();
            foreach(var pair in before)
                if(!after.TryGetValue(pair.Key,out var position)||Vector3.Distance(position,pair.Value)>.001f)
                    throw new Exception("Protected gameplay placement changed: "+pair.Key);
            int houses=All().Count(t=>t.name=="ApprovedHouse_MINI142");
            int crops=All().Count(t=>t.name=="CannabisGreenVisual_MINI142");
            if(houses!=86||crops!=14)throw new Exception("Incomplete art review: houses="+houses+" cannabis plots="+crops);
            CheckCrops();ValidateDogLifeDriveLines();AssetDatabase.SaveAssets();
            if(!EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),ScenePath))throw new Exception("Canonical scene save failed");
            File.WriteAllText(Out+"/applied-validation.txt","PASS: "+before.Count+" protected gameplay transforms unchanged; 86 houses;14 cannabis plot bindings; crop stages and local road lanes passed.\nBackup="+backup+"\nApplied="+DateTime.UtcNow.ToString("O"));
            CaptureCannabis();
            Debug.Log("MINI142 LIVE APPLY PASS protected transforms="+before.Count+"; building saved canonical scene");
            Mini001Build.BuildWindowsPlayer();
        }
    }
}
