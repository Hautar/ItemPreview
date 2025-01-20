using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using AssetGuidMap;
using AssetLoadingManager.CacheStrategies;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace AssetLoadingManager
{
    public class AssetManager
    {
        private static AssetManager instance;
        public static AssetManager Instance => instance;
        
        private readonly Dictionary<Type, IAssetProvider> assetCacheOverType = new ();

        public static void Initialize(Dictionary<Type, IAssetProvider> cacheStrategies)
        {
            if (instance != null)
            {
                Debug.LogError("AssetManager already intialized");
                return;
            }
            
            instance = new AssetManager();
            foreach (var keyValue in cacheStrategies)
            {
                instance.assetCacheOverType.Add(keyValue.Key, keyValue.Value);
            }
        }

        public bool IsLoading<TAsset>(string assetId) where TAsset : Object
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "Thread.CurrentThread.ManagedThreadId == 1");
            
            if (assetCacheOverType.TryGetValue(typeof(TAsset), out var cache))
                return cache.IsLoading(assetId);

            return false;
        }

        /// <summary>
        /// use IsLoading<TAsset> if possible
        /// </summary>
        public bool IsLoading(string assetId)
        {
           Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "Thread.CurrentThread.ManagedThreadId == 1");
            
            foreach (var keyValue in assetCacheOverType)
            {
                var cache = keyValue.Value;
                if (cache.IsLoading(assetId))
                    return true;
            }

            return false;
        }
        
        public bool IsAvailable<TAsset>(string assetId) where TAsset : Object
        {
           Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "Thread.CurrentThread.ManagedThreadId == 1");
            
            if (assetCacheOverType.TryGetValue(typeof(TAsset), out var cache))
                return cache.IsAvailable(assetId);

            return false;
        }
        
        public bool IsAvailable<TAsset>(AssetGuid assetId) where TAsset : Object
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "Thread.CurrentThread.ManagedThreadId == 1");
            
            if (assetCacheOverType.TryGetValue(typeof(TAsset), out var cache))
                return cache.IsAvailable(assetId);

            return false;
        }
        
        /// <summary>
        /// use IsAvailable<TAsset> if possible
        /// </summary>
        public bool IsAvailable(string assetId)
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "Thread.CurrentThread.ManagedThreadId == 1");
            
            foreach (var keyValue in assetCacheOverType)
            {
                var cache = keyValue.Value;
                if (cache.IsAvailable(assetId))
                    return true;
            }

            return false;
        }

        public void PreloadAssetAsync<TAsset>(string assetId, Action onComplete = null, Action onFailed = null) where TAsset : Object
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "Thread.CurrentThread.ManagedThreadId == 1");

            if (!assetCacheOverType.TryGetValue(typeof(TAsset), out var cache))
            {
                cache = new DefaultAssetProviderWithCache<TAsset>();
                assetCacheOverType.Add(typeof(TAsset), cache);
            }

            var typedCache = (IAssetProvider<TAsset>)cache;
            typedCache.PreloadAssetAsync(assetId, onComplete, onFailed);
        }

        public void LoadAssetAsync<TAsset>(string assetId, Action<TAsset> onComplete = null, Action onFailed = null) where TAsset : Object
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "Thread.CurrentThread.ManagedThreadId == 1");

            if (!assetCacheOverType.TryGetValue(typeof(TAsset), out var cache))
            {
                cache = new DefaultAssetProviderWithCache<TAsset>();
                assetCacheOverType.Add(typeof(TAsset), cache);
            }
            
            var typedCache = (IAssetProvider<TAsset>)cache;
            typedCache.LoadAssetAsync(assetId, onComplete, onFailed);
        }
        
        public void PreloadAssetsAsync<TAsset>(List<string> assetIds, Action onComplete = null, Action onFailed = null) where TAsset : Object
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "Thread.CurrentThread.ManagedThreadId == 1");

            if (!assetCacheOverType.TryGetValue(typeof(TAsset), out var cache))
            {
                cache = new DefaultAssetProviderWithCache<TAsset>();
                assetCacheOverType.Add(typeof(TAsset), cache);
            }

            var typedCache = (IAssetProvider<TAsset>)cache;
            typedCache.PreloadAssetsAsync(assetIds, onComplete, onFailed);
        }
        
        public void LoadAssetsAsync<TAsset>(List<string> assetId, Action<List<TAsset>> onComplete = null, Action onFailed = null) where TAsset : Object
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "Thread.CurrentThread.ManagedThreadId == 1");

            if (!assetCacheOverType.TryGetValue(typeof(TAsset), out var cache))
            {
                cache = new DefaultAssetProviderWithCache<TAsset>();
                assetCacheOverType.Add(typeof(TAsset), cache);
            }
            
            var typedCache = (IAssetProvider<TAsset>)cache;
            typedCache.LoadAssetsAsync(assetId, onComplete, onFailed);
        }

        public void PreloadAssetAsync<TAsset>(AssetGuid id, Action onComplete = null, Action onFailed = null)
            where TAsset : Object
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "Thread.CurrentThread.ManagedThreadId == 1");

            if (!assetCacheOverType.TryGetValue(typeof(TAsset), out var cache))
            {
                cache = new DefaultAssetProviderWithCache<TAsset>();
                assetCacheOverType.Add(typeof(TAsset), cache);
            }
            
            var typedCache = (IAssetProvider<TAsset>)cache;
            typedCache.PreloadAssetAsync(id, onComplete, onFailed);
        }
        
        public void LoadAssetAsync<TAsset>(AssetGuid assetId, Action<TAsset> onComplete = null, Action onFailed = null) where TAsset : Object
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "Thread.CurrentThread.ManagedThreadId == 1");

            if (!assetCacheOverType.TryGetValue(typeof(TAsset), out var cache))
            {
                cache = new DefaultAssetProviderWithCache<TAsset>();
                assetCacheOverType.Add(typeof(TAsset), cache);
            }
            
            var typedCache = (IAssetProvider<TAsset>)cache;
            typedCache.LoadAssetAsync(assetId, onComplete, onFailed);
        }

        public void PreloadAssetsAsync<TAsset>(List<AssetGuid> ids, Action onComplete = null, Action onFailed = null)
            where TAsset : Object
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "Thread.CurrentThread.ManagedThreadId == 1");

            if (!assetCacheOverType.TryGetValue(typeof(TAsset), out var cache))
            {
                cache = new DefaultAssetProviderWithCache<TAsset>();
                assetCacheOverType.Add(typeof(TAsset), cache);
            }
            
            var typedCache = (IAssetProvider<TAsset>)cache;
            typedCache.PreloadAssetsAsync(ids, onComplete, onFailed);
        }
        
        public void LoadAssetsAsync<TAsset>(List<AssetGuid> assetId, Action<List<TAsset>> onComplete = null, Action onFailed = null) where TAsset : Object
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "Thread.CurrentThread.ManagedThreadId == 1");

            if (!assetCacheOverType.TryGetValue(typeof(TAsset), out var cache))
            {
                cache = new DefaultAssetProviderWithCache<TAsset>();
                assetCacheOverType.Add(typeof(TAsset), cache);
            }
            
            var typedCache = (IAssetProvider<TAsset>)cache;
            typedCache.LoadAssetsAsync(assetId, onComplete, onFailed);
        }

        public bool TryGetAsset<TAsset>(string assetId, out TAsset asset) where TAsset: Object
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "Thread.CurrentThread.ManagedThreadId == 1");
            
            if (!assetCacheOverType.TryGetValue(typeof(TAsset), out var cache))
            {
                cache = new DefaultAssetProviderWithCache<TAsset>();
                assetCacheOverType.Add(typeof(TAsset), cache);
            }
            
            var typedCache = (IAssetProvider<TAsset>)cache;
            return typedCache.TryGetAsset(assetId, out asset);
        }
        
        public bool TryGetAsset<TAsset>(AssetGuid assetId, out TAsset asset) where TAsset: Object
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "Thread.CurrentThread.ManagedThreadId == 1");
            
            if (!assetCacheOverType.TryGetValue(typeof(TAsset), out var cache))
            {
                cache = new DefaultAssetProviderWithCache<TAsset>();
                assetCacheOverType.Add(typeof(TAsset), cache);
            }
            
            var typedCache = (IAssetProvider<TAsset>)cache;
            return typedCache.TryGetAsset(assetId, out asset);
        }

        public void UnloadAllAssets()
        {
            Stopwatch timer = Stopwatch.StartNew();
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "Thread.CurrentThread.ManagedThreadId == 1");
            
            foreach (var keyValue in assetCacheOverType)
                keyValue.Value.UnloadAll();
            
            Resources.UnloadUnusedAssets();

            Debug.Log($"Unload all assets duration: {timer.Elapsed.TotalMilliseconds}ms");
        }

        public void UnloadAsset<TAsset>(TAsset asset) where TAsset : Object
        {
            Debug.Log($"Unloading assets {typeof(TAsset)}");
            
            foreach (var keyValue in assetCacheOverType)
            {
                if (keyValue.Key == typeof(TAsset))
                {
                    var assetProvider = keyValue.Value as IAssetProvider<TAsset>;
                    assetProvider.Unload(asset);
                    break;
                }
            }
            
            Resources.UnloadUnusedAssets();
        }
        
        public void UnloadAssets<TAsset>() where TAsset : Object
        {
            Debug.Log($"Unloading assets {typeof(TAsset)}");
            
            foreach (var keyValue in assetCacheOverType)
            {
                if (keyValue.Key == typeof(TAsset))
                {
                    var assetProvider = keyValue.Value;
                    assetProvider.UnloadAll();
                }
            }
            
            Resources.UnloadUnusedAssets();
        }
        
        public void PreserveAssetAsync<TAsset>(AssetGuid assetId) where TAsset : Object
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "Thread.CurrentThread.ManagedThreadId == 1");
        
            if (!assetCacheOverType.TryGetValue(typeof(TAsset), out var cache))
            {
                cache = new DefaultAssetProviderWithCache<TAsset>();
                assetCacheOverType.Add(typeof(TAsset), cache);
            }
            
            var typedCache = (IAssetProvider<TAsset>)cache;
            typedCache.PreserveAssetAsync(assetId);
        }
        
        public void PreserveAssetAsync<TAsset>(string assetId) where TAsset : Object
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "Thread.CurrentThread.ManagedThreadId == 1");

            if (!assetCacheOverType.TryGetValue(typeof(TAsset), out var cache))
            {
                cache = new DefaultAssetProviderWithCache<TAsset>();
                assetCacheOverType.Add(typeof(TAsset), cache);
            }
            
            var typedCache = (IAssetProvider<TAsset>)cache;
            typedCache.PreserveAssetAsync(assetId);
        }
        
#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/Debug/Log Asset Cache")]
        private static void Log()
        {
            foreach (var keyValue in instance.assetCacheOverType)
            {
                Debug.Log(keyValue.Key.Name);
                keyValue.Value.Log();
            }
        }
#endif
        
#if LOG_MISSING_ASSETS
        public void StopLoggingMissingAssets()
        {
            foreach (var providerOverType in assetCacheOverType)
                providerOverType.Value.StopLoggingMissingAssets();
        }

        public HashSet<string> GetMissingAssets()
        {
            HashSet<string> result = new();
            foreach (var providerOverType in assetCacheOverType)
            {
                var provider = providerOverType.Value;
                
                var missingAssets = provider.GetMissingAssets();
                foreach (var missingAsset in missingAssets)
                {
                    result.Add(missingAsset);
                }
            }
            return result;
        }
        
        public HashSet<string> GetMissingBundles()
        {
            HashSet<string> result = new();
            foreach (var providerOverType in assetCacheOverType)
            {
                var provider = providerOverType.Value;

                var missingBundles = provider.GetMissingBundles();
                foreach (var missingBundle in missingBundles)
                {
                    result.Add(missingBundle);
                }
            }
            return result;
        }
#endif
    }
}
