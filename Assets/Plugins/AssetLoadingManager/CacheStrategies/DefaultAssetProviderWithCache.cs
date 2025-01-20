using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AssetGuidMap;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace AssetLoadingManager.CacheStrategies
{
    public class DefaultAssetProviderWithCache<TAsset> : IAssetProvider<TAsset> where TAsset : Object
    {
        protected readonly Dictionary<AssetGuid, TAsset>           PersistentPrototypes = new(100);
        protected readonly Dictionary<AssetGuid, TAsset>           RemotePrototypes     = new (500);
        protected readonly Dictionary<AssetGuid, TAsset>           LocalPrototypes      = new (100);
        protected readonly Dictionary<AssetGuid, List<Action>>     PendingIds           = new (100);

#if LOG_MISSING_ASSETS
        private readonly HashSet<string> missingAssets = new();
        private readonly HashSet<string> missingBundles = new();
#endif

        protected TAsset ErrorAsset;

        public bool  IsLoading(string assetId)   => IsLoading(assetId.ToAssetGuid());
        public bool  IsLoading(AssetGuid id)     => PendingIds.ContainsKey(id);
        public bool  IsAvailable(string assetId) => IsAvailable(assetId.ToAssetGuid());
        public bool  IsAvailable(AssetGuid id)
        {
            if (PersistentPrototypes.TryGetValue(id, out var asset) || 
                   RemotePrototypes.TryGetValue(id, out asset) ||
                   LocalPrototypes.TryGetValue(id, out asset))
            {
                if (asset == null)
                {
                    Debug.LogError($"asset {id.ToAssetName()} have been destroyed");
                    //this can be dangerous
                    // PersistentPrototypes.Remove(id);
                    // RemotePrototypes.Remove(id);
                    // LocalPrototypes.Remove(id);
                }
            }

            return asset != null;
        }

        public bool  IsLocal(AssetGuid id)       => Bucket.GetBucketId(id) == 0;

        public DefaultAssetProviderWithCache(string errorAssetName = null)
        {
            if (errorAssetName == null)
                return;
            
            ErrorAsset = Resources.Load<TAsset>(errorAssetName);
            Debug.Assert(ErrorAsset != null, "ErrorAsset != null");
            PersistentPrototypes[AssetGuid.Invalid] = ErrorAsset;
        }

        public bool TryGetAsset(string assetId, out Object asset)
        {
            TAsset typedAsset;
            bool result = TryGetAsset(assetId, out typedAsset);
            asset = typedAsset;
            return result;
        }

        public bool TryGetAsset(string assetId, out TAsset asset)
        {
            var guid = assetId.ToAssetGuid();
            return TryHandleAssetAvailable(guid, out asset);
        }

        public bool TryGetAsset(AssetGuid id, out TAsset asset)
        {
            return TryHandleAssetAvailable(id, out asset);
        }

        public bool TryGetAsset(AssetGuid id, out Object asset)
        {
            bool result = TryHandleAssetAvailable(id, out var typedAsset);
            asset = typedAsset;
            return result;
        }

        public void PreloadAssetAsync(string assetId, Action onComplete, Action onFailed) => 
            PreloadAssetAsyncInternal(assetId.ToAssetGuid(), onComplete);

        public void PreloadAssetsAsync(List<string> assetIds, Action onComplete, Action onFailed) => 
            PreloadAssetsAsyncInternal(Convert(assetIds), onComplete);
        
        public void PreloadAssetAsync(AssetGuid id, Action onComplete, Action onFailed) =>
            PreloadAssetAsyncInternal(id, onComplete);

        public void PreloadAssetsAsync(List<AssetGuid> id, Action onComplete, Action onFailed) =>
            PreloadAssetsAsyncInternal(id, onComplete);

        public void LoadAssetAsync(string assetId, Action<Object> onComplete, Action onFailed) =>
            LoadAssetAsyncInternal(assetId.ToAssetGuid(), asset => onComplete?.Invoke(asset));

        public void LoadAssetAsync(string assetId, Action<TAsset> onComplete, Action onFailed) =>
            LoadAssetAsyncInternal(assetId.ToAssetGuid(), onComplete);

        public void LoadAssetAsync(List<AssetGuid> ids, Action<List<Object>> onComplete, Action onFailed) =>
            LoadAssetAsyncInternal(ids, assets =>
            {
                var result = new List<Object>(assets.Count);
                result.AddRange(assets);
                onComplete?.Invoke(result);
            });
        
        public void LoadAssetAsync(List<string> assetId, Action<List<Object>> onComplete, Action onFailed) =>
            LoadAssetAsyncInternal(Convert(assetId), assets =>
            {
                var result = new List<Object>(assets.Count);
                result.AddRange(assets);
                onComplete?.Invoke(result);
            });
        
        public void LoadAssetAsync(AssetGuid id, Action<TAsset> onComplete, Action onFailed) =>
            LoadAssetAsyncInternal(id, onComplete);

        public void LoadAssetAsync(AssetGuid id, Action<Object> onComplete, Action onFailed) =>
            LoadAssetAsyncInternal(id, asset => onComplete?.Invoke(asset));
        
        
        public void LoadAssetsAsync(List<AssetGuid> id, Action<List<TAsset>> onComplete, Action onFailed) =>
            LoadAssetAsyncInternal(id, onComplete);

        public void LoadAssetsAsync(List<string> assetIds, Action<List<TAsset>> onComplete, Action onFailed) =>
            LoadAssetAsyncInternal(Convert(assetIds), onComplete);

        private void LoadAssetAsyncInternal(List<AssetGuid> assetId, Action<List<TAsset>> onComplete)
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "Thread.CurrentThread.ManagedThreadId == 1");
            
            var result = new List<TAsset>();
            //don't need thread safety here because everything runs from main
            int pendingCounter = assetId.Count;

            for (int i = 0; i < assetId.Count; i++)
            {
                int index = i;
                result.Add(null);
                var guid = assetId[i];

                LoadAssetAsyncInternal(guid, asset => OnAssetLoaded(index, asset));
            }

            void OnAssetLoaded(int index, TAsset asset)
            {
                result[index] = asset;
                pendingCounter--;
                
                if(pendingCounter == 0)
                    onComplete?.Invoke(result);
            }
        }
        
        private void PreloadAssetsAsyncInternal(List<AssetGuid> assetId, Action onComplete)
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "Thread.CurrentThread.ManagedThreadId == 1");
            
            //don't need thread safety here because everything runs from main
            int pendingCounter = assetId.Count;

            for (int i = 0; i < assetId.Count; i++)
            {
                var guid = assetId[i];
                PreloadAssetAsyncInternal(guid, OnAssetPreloaded);
            }

            void OnAssetPreloaded()
            {
                pendingCounter--;
                
                if(pendingCounter == 0)
                    onComplete?.Invoke();
            }
        }

        private void LoadAssetAsyncInternal(AssetGuid guid, Action<TAsset> onComplete)
        {
            PreloadAssetAsyncInternal(guid,OnAssetAvailable(guid, onComplete));
        }
        
        private void PreloadAssetAsyncInternal(AssetGuid guid, Action onComplete)
        {
            if (IsAvailable(guid))
                onComplete?.Invoke();
            else if (IsLoading(guid))
                PendingIds[guid].Add(onComplete);
            else if (IsLocal(guid))
                LoadAssetLocal(guid, onComplete);
            else
                LoadAssetRemote(guid, onComplete);
        }

        protected virtual async void LoadAssetRemote(AssetGuid guid, Action onComplete)
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "CurrentThread.ManagedThreadId != 1");
            
            PendingIds.Add(guid, new List<Action>() { onComplete });
            var key = ConvertAddressableAssetName(guid.ToAssetName());
            
#if LOG_MISSING_ASSETS
            await LogIfMissing(key, typeof(TAsset));
#endif
            
            var handle = Addressables.LoadAssetAsync<TAsset>(key);
            handle.Completed += x =>
            {
                Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "CurrentThread.ManagedThreadId != 1");
                
                TAsset prototype = default;
                if (x.Status != AsyncOperationStatus.Succeeded ||
                    x.Result == null)
                {
                    Debug.LogError($"Addressable {guid.ToAssetName()} download failed. Create stub object {typeof(TAsset).Name}");
                    prototype = ErrorAsset;
                    PersistentPrototypes[guid] = prototype;
                }
                else
                {
                    prototype = x.Result;
                    RemotePrototypes[guid] = prototype;
                }
                
                Debug.Assert(prototype != null, $"prototype != null {guid.ToAssetName()}");
                OnAssetPrototypeLoaded(guid, prototype);

                foreach (var action in PendingIds[guid])
                    action?.Invoke();

                PendingIds.Remove(guid);
            };
        }

        protected virtual string ConvertAddressableAssetName(string assetName) => assetName;
        

        protected virtual void LoadAssetLocal(AssetGuid guid, Action onComplete)
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "CurrentThread.ManagedThreadId != 1");
            
            PendingIds.Add(guid, new List<Action>() { onComplete });

            var handle = Resources.LoadAsync<TAsset>(guid.ToAssetName());

            handle.completed += x =>
            {
                Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "CurrentThread.ManagedThreadId != 1");
                
                TAsset prototype = handle.asset as TAsset;
                if (prototype == null)
                {
                    Debug.LogError($"Local asset {guid.Value} {guid.ToAssetName()} download failed. Create stub object {typeof(TAsset).Name}");
                    prototype = ErrorAsset;
                    PersistentPrototypes[guid] = prototype;
                }
                else
                {
                    LocalPrototypes[guid] = prototype;
                }

                Debug.Assert(prototype != null, $"prototype != null {guid.Value} {guid.ToAssetName()}");
                OnAssetPrototypeLoaded(guid, prototype);
                
                foreach (var action in PendingIds[guid])
                    action?.Invoke();

                PendingIds.Remove(guid);
            };
        }

        protected virtual void OnAssetPrototypeLoaded(AssetGuid guid, TAsset asset)
        {
            
        }

        protected Action OnAssetAvailable(AssetGuid guid, Action<TAsset> onComplete)
        {
            return () =>
            {
                TryHandleAssetAvailable(guid, out var result);
                onComplete?.Invoke(result);
            };
        }

        private bool TryGetPrototype(AssetGuid guid, out TAsset result)
        {
            if (!RemotePrototypes.TryGetValue(guid, out result))
                if (!LocalPrototypes.TryGetValue(guid, out result))
                    if (!PersistentPrototypes.TryGetValue(guid, out result))
                    {
                        Debug.LogError($"Cant find asset for guid {guid} {guid.ToAssetName()}");
                        result = default;
                        return false;
                    }
            
            Debug.Assert(result != null, $"result != null {RemotePrototypes.ContainsKey(guid)} {LocalPrototypes.ContainsKey(guid)} {PersistentPrototypes.ContainsKey(guid)}");
            return true;
        }

        protected virtual bool TryHandleAssetAvailable(AssetGuid guid, out TAsset result)
        {
            if (!TryGetPrototype(guid, out var prototype))
            {
                Debug.LogError($"TryHandleAssetAvailable failed. AssetId: {guid.Value} {guid.ToAssetName()}");
                result = default;
                return false;
            }

            Debug.Assert(prototype != null, $"prototype != null {guid}");

            result = prototype;
            return true;
        }

        public void PreserveAssetAsync(string assetName) => PreserveAssetAsync(assetName.ToAssetGuid());

        public void PreserveAssetAsync(AssetGuid guid)
        {
            if (PersistentPrototypes.ContainsKey(guid))
                return;
            
            if (LocalPrototypes.TryGetValue(guid, out var asset))
            {
                LocalPrototypes.Remove(guid);
                PersistentPrototypes.Add(guid, asset);
                return;
            }

            if (RemotePrototypes.TryGetValue(guid, out asset))
            {
                RemotePrototypes.Remove(guid);
                PersistentPrototypes.Add(guid, asset);
                return;
            }

            PreloadAssetAsyncInternal(guid, () => PreserveAssetAsync(guid));
        }
        
        private static List<AssetGuid> Convert(List<string> assetNames)
        {
            var result = new List<AssetGuid>(assetNames.Count);
            foreach (var name in assetNames)
                result.Add(name.ToAssetGuid());
            return result;
        }

        public void Log()
        {

        }

        //TODO: properly release local assets. now we dont care about them, but once we'll have local cache for items
        public void UnloadAll()
        {
            var pendingEntries = new Dictionary<AssetGuid, TAsset>();
            
            foreach (var keyValue in RemotePrototypes)
            {
                Debug.Assert(!PendingIds.ContainsKey(keyValue.Key), $"!PendingIds.ContainsKey({keyValue.Key.ToAssetName()})");
                if (PendingIds.ContainsKey(keyValue.Key))
                    pendingEntries[keyValue.Key] = keyValue.Value;
                else
                    Addressables.Release(keyValue.Value);
            }
            
            RemotePrototypes.Clear();
            foreach (var keyValue in pendingEntries)
            {
                RemotePrototypes[keyValue.Key] = keyValue.Value;
            }
        }

        public void Unload(TAsset asset)
        {
            throw new NotImplementedException();
        }

#if LOG_MISSING_ASSETS
        public HashSet<string> GetMissingAssets() => missingAssets;
        public HashSet<string> GetMissingBundles() => missingBundles;
        public void StopLoggingMissingAssets() => stopLogMissingAssets = true;

        private bool stopLogMissingAssets;

        protected async Task LogIfMissing(object assetKey, Type assetType)
        {
            if (stopLogMissingAssets)
                return;
            
            var sourceBundles = GetSourceBundles(assetKey, assetType);
            if (sourceBundles.Count == 0)
                return;

            var intersection = missingBundles.Intersect(sourceBundles);
            bool isInMissingBundle = intersection.Any();
            bool isMissing = await GetIsMissingAsync(assetKey);

            if (isMissing || isInMissingBundle)
            {
                missingAssets.Add(assetKey.ToString());

                var sourceBundlesString = new StringBuilder();
                foreach (var sourceBundle in sourceBundles)
                {
                    missingBundles.Add(sourceBundle);
                    sourceBundlesString.Append(sourceBundle + ", ");
                }
                
                if (sourceBundlesString.Length > 2)
                {
                    sourceBundlesString.Remove(sourceBundlesString.Length - 2, 2);
                }

                Debug.LogWarning($"Downloaded missing asset: {assetKey} from {sourceBundlesString}");
            }
        }
        
        private async Task<bool> GetIsMissingAsync(object key)
        {
            var handle = Addressables.GetDownloadSizeAsync(key);
            await handle.Task;
            return handle.Result > 0;
        }

        private HashSet<string> GetSourceBundles(object assetKey, Type assetType)
        {
            const StringComparison invariantPolicy = StringComparison.InvariantCultureIgnoreCase;

            HashSet<string> result = new();
            foreach (var locator in Addressables.ResourceLocators)
            {
                if (locator.Locate(assetKey, assetType, out var locations))
                {
                    foreach (var location in locations)
                    {
                        var bundles = location.Dependencies
                            .Where(t => t.InternalId.Contains("bundle", invariantPolicy))
                            // it is intended if asset depends on one of those
                            .Where(resourceLocation => !resourceLocation.InternalId.Contains("Essentials", invariantPolicy)
                                                       && !resourceLocation.InternalId.Contains("Settings", invariantPolicy)
                                                       && !resourceLocation.InternalId.Contains("unitybuiltinshaders", invariantPolicy))
                            .ToList();
                        
                        for (int k = 0; k < bundles.Count; k++)
                        {
                            // for easier browsing
                            var newId = bundles[k].InternalId.Replace('\\', '/');
                            newId = newId.Split('/')[^1];
                            
                            result.Add(newId);
                        }
                    }
                }
            }
            return result;
        }
#endif
    }
}
