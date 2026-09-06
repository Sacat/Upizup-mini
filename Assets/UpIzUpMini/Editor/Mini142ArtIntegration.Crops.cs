using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UpIzUpMini.Farming;
using UpIzUpMini.Economy;
using Object=UnityEngine.Object;

namespace UpIzUpMini.EditorTools
{
    public static partial class Mini142ArtIntegration
    {
        static Material cannabisMaterial;
        static void ImportCannabis()
        {
            foreach(var name in new[]{"CannabisAlbedo","CannabisNormal"})
            {
                string path=Art+"/"+name+".png";File.Copy(Out+"/Export/"+name+".png",path,true);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
                var ti=(TextureImporter)AssetImporter.GetAtPath(path);bool normal=name.Contains("Normal");
                ti.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
                ti.sRGBTexture=!normal;ti.mipmapEnabled=true;ti.filterMode=FilterMode.Bilinear;
                ti.wrapMode=TextureWrapMode.Clamp;ti.maxTextureSize=1024;
                ti.textureCompression=TextureImporterCompression.Compressed;ti.SaveAndReimport();
            }
            cannabisMaterial=AssetDatabase.LoadAssetAtPath<Material>(Art+"/CannabisBaked.mat");
            if(cannabisMaterial==null){cannabisMaterial=new Material(Shader.Find("UpIzUpMini/ApprovedBakedCrop"));AssetDatabase.CreateAsset(cannabisMaterial,Art+"/CannabisBaked.mat");}
            cannabisMaterial.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/CannabisAlbedo.png");
            cannabisMaterial.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/CannabisNormal.png"));
            cannabisMaterial.color=Color.white;cannabisMaterial.enableInstancing=true;
            foreach(var name in new[]{"CannabisGreen","CannabisPurple"})
            {
                var model=JsonUtility.FromJson<Model>(File.ReadAllText(Out+"/Export/"+name+".json"));models[name]=model;
                ImportParts(name,model.parts);if(model.lods!=null)foreach(var lod in model.lods)ImportParts(name+"_"+lod.name,lod.parts);
            }
        }
        static CropStageVisual CreateCannabis(FarmPlot plot,bool purple)
        {
            string key=purple?"CannabisPurple":"CannabisGreen";
            var root=new GameObject(key+"Visual_MINI142");root.transform.SetParent(plot.transform,false);
            var scale=plot.transform.lossyScale;root.transform.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
            root.transform.position=plot.transform.position+Vector3.up*(Mathf.Abs(scale.y)*.5f+.005f);
            var plant=new GameObject("Plant");plant.transform.SetParent(root.transform,false);
            var fruit=new List<Renderer>();var pistils=new List<Renderer>();var lods=new List<LOD>();
            var model=models[key];var levels=new List<Detail>{new Detail{name="LOD0",parts=model.parts}};
            if(model.lods!=null)levels.AddRange(model.lods);
            foreach(var level in levels)
            {
                string prefix=key+(level.name=="LOD0"?"":"_"+level.name);var renderers=new List<Renderer>();
                var group=new GameObject(level.name);group.transform.SetParent(plant.transform,false);
                foreach(var part in level.parts.OrderBy(p=>p.name))
                {
                    var mat=part.material=="palette"?paletteMaterial:cannabisMaterial;
                    var r=MeshObject(part.name,group.transform,meshes[prefix+"_"+part.name],mat);renderers.Add(r);
                    if(part.name.StartsWith("Fruit"))fruit.Add(r);
                    if(part.name.StartsWith("Pistil"))pistils.Add(r);
                }
                lods.Add(new LOD(level.name=="LOD0"?.09f:.008f,renderers.ToArray()));
            }
            var lg=plant.AddComponent<LODGroup>();lg.SetLODs(lods.ToArray());lg.RecalculateBounds();
            var visual=root.AddComponent<CropStageVisual>();var so=new SerializedObject(visual);
            so.FindProperty("plantRoot").objectReferenceValue=plant.transform;
            so.FindProperty("plantRenderer").objectReferenceValue=plant.GetComponentInChildren<Renderer>();
            so.FindProperty("fullScale").floatValue=1;
            foreach(var tuple in new[]{("fruitRenderers",fruit),("pistilRenderers",pistils)})
            {
                var a=so.FindProperty(tuple.Item1);a.arraySize=tuple.Item2.Count;
                for(int i=0;i<a.arraySize;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=tuple.Item2[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();root.SetActive(false);return visual;
        }
        static void PatchCannabis()
        {
            var illegal=AssetDatabase.FindAssets("t:CropDefinition").Select(g=>AssetDatabase.LoadAssetAtPath<CropDefinition>(AssetDatabase.GUIDToAssetPath(g))).Where(c=>c!=null&&c.isIllegal).ToArray();
            int count=0;
            foreach(var plot in Object.FindObjectsByType<FarmPlot>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(plot.transform.Find("CannabisGreenVisual_MINI142")!=null)continue;
                var green=CreateCannabis(plot,false);var purple=CreateCannabis(plot,true);
                var so=new SerializedObject(plot);var old=so.FindProperty("weedVisual").objectReferenceValue as CropStageVisual;
                if(old!=null){old.SetVisible(false);old.name="Legacy_"+old.name+"_MINI142";}
                so.FindProperty("weedVisual").objectReferenceValue=green;
                var array=so.FindProperty("cropVisuals");
                foreach(var crop in illegal)
                {
                    int index=-1;for(int i=0;i<array.arraySize;i++)if(array.GetArrayElementAtIndex(i).FindPropertyRelative("cropId").stringValue==crop.cropId)index=i;
                    if(index<0){index=array.arraySize;array.InsertArrayElementAtIndex(index);}
                    var entry=array.GetArrayElementAtIndex(index);
                    var previous=entry.FindPropertyRelative("visual").objectReferenceValue as CropStageVisual;if(previous!=null)previous.SetVisible(false);
                    entry.FindPropertyRelative("cropId").stringValue=crop.cropId;
                    entry.FindPropertyRelative("visual").objectReferenceValue=crop.cropId.Contains("purple")?purple:green;
                }
                so.ApplyModifiedPropertiesWithoutUndo();count++;
            }
            Debug.Log("MINI142 CANNABIS wired plots="+count+" existing strain IDs="+illegal.Length+"; no growth/economy code changed");
        }
        static void CaptureCannabis()
        {
            var plot=Object.FindObjectsByType<FarmPlot>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).OrderBy(p=>p.transform.position.z).First();
            foreach(string key in new[]{"CannabisGreen","CannabisPurple"})
            {
                var t=plot.transform.Find(key+"Visual_MINI142");if(t==null)throw new Exception("Missing review buds");
                var crop=AssetDatabase.FindAssets("t:CropDefinition").Select(g=>AssetDatabase.LoadAssetAtPath<CropDefinition>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault(c=>c.cropId==(key=="CannabisPurple"?"purple":"bushers"));
                t.gameObject.SetActive(true);var v=t.GetComponent<CropStageVisual>();
                if(crop==null)throw new Exception("Missing real crop definition for screenshot");
                v.ApplyStage(3,crop.unripeColor,crop.ripeColor,crop.secondaryRipeColor);
                var p=t.position;
                Capture("After-"+key+"-InFarm-REVIEW",p+new Vector3(1.6f,1.6f,-2.6f),p+Vector3.up*.95f);
                t.gameObject.SetActive(false);
            }
        }
        public static void ReviewBuds()
        {
            Review();ImportCannabis();PatchCannabis();CheckCrops();AssetDatabase.SaveAssets();
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene(),ReviewScene);
            CaptureCannabis();Debug.Log("MINI142 BAKED BUD REVIEW PASS; live scene remains unchanged");
        }
    }
}
