using System;
using AssetLoadingManager;
using ItemPreview.Contracts;
using ItemPreview.DataRecognition;
using UnityEngine;

namespace ItemPreview
{
    public class ItemPreviewService : IItemPreviewService
    {
        private ItemPreviewApi api;

        private readonly IHashScheme hashScheme;
        private readonly ItemPreviewSettings settings;

        public ItemPreviewService(IHashScheme hashScheme, ItemPreviewSettings settings)
        {
            this.hashScheme = hashScheme;
            this.settings   = settings;
        }

        public void InitializeAsync(Action onComplete, Action onFailed)
        {
            Debug.Assert(AssetManager.Instance != null, "AssetManager.Instance != null");
            
            if (api != null)
            {
                onComplete?.Invoke();
                return;
            }

            api = new ItemPreviewApi(hashScheme, settings);
            api.Initialize(onComplete, onFailed);
        }

        public void Update()
        {
            api?.Update();
        }

        public void GetPreviewAsync(string objectAssetName,
                                    Action<Sprite> onComplete,
                                    Action onFailed)
        {
            if (api == null)
            {
                onFailed?.Invoke();
                return;
            }
            
            api.GetPreviewAsync(objectAssetName, onComplete, onFailed);
        }

        public void ClearRuntimeCache()
        {
            api?.ClearRuntimeCache();
        }
    }
}