using System;
using System.Collections.Generic;
using System.Threading;
using AssetGuidMap;
using AssetLoadingManager.CacheStrategies;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Plugins.AssetLoadingManager.CacheStrategies
{
    public class DefaultAssetProvider<TAsset> : IAssetProvider<TAsset> where TAsset : Object
    {
        private bool IsLocal(AssetGuid id) => Bucket.GetBucketId(id) == 0;
        
        public bool IsLoading(string id) => false;
        public bool IsLoading(AssetGuid id) => false;
        public bool IsAvailable(string assetId) => false;
        public bool IsAvailable(AssetGuid id) => false;

        
        public void PreserveAssetAsync(string assetId) => throw new NotImplementedException();
        public void PreserveAssetAsync(AssetGuid guid) => throw new NotImplementedException();
        
        
        public bool TryGetAsset(string assetId, out Object asset) => throw new NotImplementedException();
        public bool TryGetAsset(AssetGuid id, out Object asset) => throw new NotImplementedException();
        public bool TryGetAsset(string assetId, out TAsset asset) => throw new NotImplementedException();
        public bool TryGetAsset(AssetGuid id, out TAsset asset) => throw new NotImplementedException();
        
        
        public void PreloadAssetAsync(string assetId, Action onComplete, Action onFailed) => throw new NotImplementedException();
        public void PreloadAssetAsync(AssetGuid id, Action onComplete, Action onFailed) => throw new NotImplementedException();
        public void PreloadAssetsAsync(List<string> assetIds, Action onComplete, Action onFailed) => throw new NotImplementedException();
        public void PreloadAssetsAsync(List<AssetGuid> id, Action onComplete, Action onFailed) => throw new NotImplementedException();
      

        public void Log() { }
        public void UnloadAll() { }
        
        
        public void LoadAssetAsync(string assetId, Action<Object> onComplete, Action onFailed) => LoadAssetAsyncInternal(assetId.ToAssetGuid(), onComplete);
        public void LoadAssetAsync(string assetId, Action<TAsset> onComplete, Action onFailed) => LoadAssetAsyncInternal(assetId.ToAssetGuid(), onComplete);
        public void LoadAssetAsync(AssetGuid id, Action<Object> onComplete, Action onFailed) => LoadAssetAsyncInternal(id, onComplete);
        public void LoadAssetAsync(AssetGuid id, Action<TAsset> onComplete, Action onFailed) => LoadAssetAsyncInternal(id, onComplete);
        

        public void LoadAssetsAsync(List<AssetGuid> id, Action<List<TAsset>> onComplete, Action onFailed) => LoadAssetsAsyncInternal(id, onComplete);
        
        public void Unload(TAsset asset)
        {
            Resources.UnloadAsset(asset);
        }

        public void LoadAssetsAsync(List<string> assetIds, Action<List<TAsset>> onComplete, Action onFailed) => LoadAssetsAsyncInternal(Convert(assetIds), onComplete);

        public void LoadAssetAsync(List<AssetGuid> id, Action<List<Object>> onComplete, Action onFailed) => LoadAssetsAsyncInternal(id,
            assets =>
            {
                var result = new List<Object>(assets.Count);
                result.AddRange(assets);
                onComplete?.Invoke(result);
            });
        public void LoadAssetAsync(List<string> assetId, Action<List<Object>> onComplete, Action onFailed)=> LoadAssetsAsyncInternal(Convert(assetId),
            assets =>
            {
                var result = new List<Object>(assets.Count);
                result.AddRange(assets);
                onComplete?.Invoke(result);
            });
        

        private static List<AssetGuid> Convert(List<string> assetNames)
        {
            var result = new List<AssetGuid>(assetNames.Count);
            foreach (var name in assetNames)
                result.Add(name.ToAssetGuid());
            return result;
        }

        private void LoadAssetsAsyncInternal(List<AssetGuid> guids, Action<List<TAsset>> onComplete)
        {
            int remaining = guids.Count;
            var result = new List<TAsset>(guids.Count);
            for (int i = 0; i < guids.Count; i++)
            {
                int index = i;
                var guid = guids[index];
                result.Add(null);
                LoadAssetAsyncInternal(guid, asset =>
                {                    
                    result[index] = asset;
                    remaining--;
                    if (remaining == 0)
                        onComplete?.Invoke(result);
                });
            }
        }
        
        private void LoadAssetAsyncInternal(AssetGuid guid, Action<TAsset> onComplete)
        {
            if (IsLocal(guid))
                LoadAssetLocal(guid, onComplete);
            else
                LoadAssetRemote(guid, onComplete);
        }
        
        private void LoadAssetLocal(AssetGuid guid, Action<TAsset> onComplete)
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "CurrentThread.ManagedThreadId != 1");
            
            var handle = Resources.LoadAsync<TAsset>(guid.ToAssetName());
            
            handle.completed += x =>
            {
                if (!x.isDone || handle.asset == null)
                    Debug.LogError($"Local asset {guid.Value} {guid.ToAssetName()} download failed");
                onComplete?.Invoke(handle.asset as TAsset);
            };
        }

        private void LoadAssetRemote(AssetGuid guid, Action<TAsset> onComplete)
        {
            var handle = Addressables.LoadAssetAsync<TAsset>(guid.ToAssetName());
            handle.Completed += x =>
            {
                if (x.Status != AsyncOperationStatus.Succeeded ||
                    x.Result == null)
                    Debug.LogError($"Addressable {guid.ToAssetName()} download failed");
                onComplete?.Invoke(handle.Result);
            };
        }
        
#if LOG_MISSING_ASSETS
        public void StopLoggingMissingAssets()
        {
        }

        public HashSet<string> GetMissingAssets()
        {
            return new HashSet<string>();
        }
        
        public HashSet<string> GetMissingBundles()
        {
            return new HashSet<string>();
        }
#endif
    }
}
