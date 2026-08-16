using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;
using UpIzUpMini.Farming;

namespace UpIzUpMini.EditorTools
{
    public static class Mini028FarmhandValidation
    {
        public static void Run()
        {
            var economyGo = new GameObject("TestEconomy");
            var economy = economyGo.AddComponent<EconomyManager>();
            typeof(EconomyManager).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(economy, null);
            var crop = ScriptableObject.CreateInstance<CropDefinition>();
            crop.cropId = "test_crop"; crop.displayName = "Test Crop"; crop.sellPrice = 1;

            var workerGo = new GameObject("TestWorker");
            workerGo.AddComponent<CharacterController>();
            var worker = workerGo.AddComponent<FarmhandController>();
            worker.SetWorking(true, crop);

            var plots = new FarmPlot[3];
            for (int i = 0; i < 3; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"TestPlot{i}"; go.transform.position = new Vector3(i * 2f, 0f, 0f);
                plots[i] = go.AddComponent<FarmPlot>();
                var so = new SerializedObject(plots[i]);
                so.FindProperty("soilRenderer").objectReferenceValue = go.GetComponent<Renderer>();
                so.ApplyModifiedPropertiesWithoutUndo();
                plots[i].LoadState((int)PlotState.Ripe, crop, crop.growDurationSeconds);
            }

            MethodInfo find = typeof(FarmhandController).GetMethod("FindPlotNeedingWork", BindingFlags.Instance | BindingFlags.NonPublic);
            FarmPlot first = (FarmPlot)find.Invoke(worker, null);
            first.FarmhandInteract(workerGo, crop);
            FarmPlot second = (FarmPlot)find.Invoke(worker, null);
            if (second == first) throw new Exception("Farmhand selected the harvested plot twice.");
            second.FarmhandInteract(workerGo, crop);
            FarmPlot mother = (FarmPlot)find.Invoke(worker, null);
            if (mother == null || mother == first || mother == second || !mother.IsRipe)
                throw new Exception($"Third selection invalid. first={first?.name}, second={second?.name}, third={mother?.name}, ripe={mother?.IsRipe}.");
            if (!mother.FarmhandCloneBeforeHarvest() || economy.GetSeeds(crop.cropId) != 2)
                throw new Exception("Clone mother did not produce two replacement seeds.");
            Debug.Log("MINI-028 FARMHAND VALIDATION PASS: harvested two, preserved/cloned third, produced 2 seeds.");

            UnityEngine.Object.DestroyImmediate(workerGo);
            foreach (var p in plots) if (p != null) UnityEngine.Object.DestroyImmediate(p.gameObject);
            UnityEngine.Object.DestroyImmediate(economyGo);
            UnityEngine.Object.DestroyImmediate(crop);
        }
    }
}
