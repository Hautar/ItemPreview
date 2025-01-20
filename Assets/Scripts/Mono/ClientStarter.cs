using System;
using System.Collections.Generic;
using AssetGuidMap;
using AssetLoadingManager;
using AssetLoadingManager.CacheStrategies;
using ItemPreview;
using ItemPreview.DataRecognition;
using Tasks;
using UnityEngine;

namespace Mono
{
    public class ClientStarter : MonoBehaviour
    {
        private ItemPreviewService itemPreviewService;
        
        private static readonly List<string> SampleAssetPaths = new()
        {
            "Prefabs/donut_001",
            "Prefabs/drink_001",
            "Prefabs/drink_002",
            "Prefabs/drink_003",
            "Prefabs/drink_004",
            "Prefabs/eggplant_001",
            "Prefabs/fish_001",
            "Prefabs/fork_001",
            "Prefabs/ice_cream_dish_001",
            "Prefabs/peach_001",
            "Prefabs/Plate_001",
            "Prefabs/sandwich_001",
            "Prefabs/sausages_001",
            "Prefabs/sushi_dish_001",
            "Prefabs/tomato_001",
            "Prefabs/wineglass_001",
        };
        
        private void Start()
        {
            SequentialAction
                .Create("Load Sample Project", () => Debug.Log("Voila"), () => Debug.Log("Smt failed"))
                .Then(InitializeAssetDatabaseAsync)
                .Then(InitializeAssetManager)
                .Then(LoadServices)
                .Then(SchedulePreviewCreation)
                .Run();
        }

        private void Update()
        {
            itemPreviewService?.Update();
        }

        private void InitializeAssetManager()
        {
            if (AssetManager.Instance != null)
                return;
        
            var gameObjectProvider = new GameObjectProviderWithCache("Prefabs/Pref_CubePrimitive");
            AssetManager.Initialize(new Dictionary<Type, IAssetProvider>()
            {
                {typeof(GameObject), gameObjectProvider},
            });
        }
        
        private void InitializeAssetDatabaseAsync(Action onComplete, Action onFailed)
        {
            if (AssetGuidManager.IsInitialized)
                return;

            AssetGuidManager.InitializeAsync(
                new List<string>() { "Settings/View/LocalAssetsBucketStorage" },
                null,
                onComplete,
                onFailed
            );
        }

        private void LoadServices(Action onComplete, Action onFailed)
        {
            itemPreviewService = new ItemPreviewService(new HashScheme(), new ItemPreviewSettings());
            itemPreviewService.InitializeAsync(onComplete, onFailed);
        }

        private void SchedulePreviewCreation()
        {
            foreach (var assetPath in SampleAssetPaths)
            {
                itemPreviewService.GetPreviewAsync(assetPath, _ => { }, () =>
                {
                    Debug.LogWarning($"Preview creation failed: {assetPath}");
                });
            }
        }
    }
}