using System;
using System.Collections.Generic;
using AssetGuidMap;
using Object = UnityEngine.Object;

namespace AssetLoadingManager.CacheStrategies
{
    public interface IAssetProvider
    {
        public bool IsLoading(string id);
        public bool IsAvailable(string assetId);
        public bool TryGetAsset(string assetId, out Object asset);
        public void LoadAssetAsync(string assetId, Action<Object> onComplete, Action onFailed);
        public void LoadAssetAsync(List<string> assetId, Action<List<Object>> onComplete, Action onFailed);
        public void PreserveAssetAsync(string assetId);
        
        public bool IsLoading(AssetGuid id);
        public bool IsAvailable(AssetGuid id);
        public bool TryGetAsset(AssetGuid id, out Object asset);
        public void LoadAssetAsync(AssetGuid id, Action<Object> onComplete, Action onFailed);
        public void LoadAssetAsync(List<AssetGuid> id, Action<List<Object>> onComplete, Action onFailed);

        public void PreserveAssetAsync(AssetGuid guid);
        
                
        public void Log();
        void UnloadAll();
        
#if LOG_MISSING_ASSETS
        public HashSet<string> GetMissingBundles();
        public HashSet<string> GetMissingAssets();
        public void StopLoggingMissingAssets();
#endif
    }
    
    public interface IAssetProvider<TAsset> : IAssetProvider where TAsset : Object
    {
        public bool TryGetAsset(string assetId, out TAsset asset);
        public void PreloadAssetAsync(string assetId, Action onComplete, Action onFailed);
        public void PreloadAssetsAsync(List<string> assetIds, Action onComplete, Action onFailed);
        public void LoadAssetAsync(string assetId, Action<TAsset> onComplete, Action onFailed);
        public void LoadAssetsAsync(List<string> assetIds, Action<List<TAsset>> onComplete, Action onFailed);
        
        public bool TryGetAsset(AssetGuid id, out TAsset asset);
        public void PreloadAssetAsync(AssetGuid id, Action onComplete, Action onFailed);
        public void PreloadAssetsAsync(List<AssetGuid> id, Action onComplete, Action onFailed);
        public void LoadAssetAsync(AssetGuid id, Action<TAsset> onComplete, Action onFailed);
        public void LoadAssetsAsync(List<AssetGuid> id, Action<List<TAsset>> onComplete, Action onFailed);

        public void Unload(TAsset asset);
    }
}
