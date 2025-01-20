using System;
using System.Collections.Generic;
using System.Threading;
using AssetGuidMap;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace AssetLoadingManager.CacheStrategies
{
    public class GameObjectProviderWithCache : DefaultAssetProviderWithCache<GameObject>
    {
        private Scene poolScene;
        private const string CacheSceneName = "GameObjectCache";

        public GameObjectProviderWithCache(string fallbackAssetName) : base(fallbackAssetName)
        {
            var activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(CacheSceneName, LoadSceneMode.Additive);
            poolScene = SceneManager.GetSceneByName(CacheSceneName);
            SceneManager.SetActiveScene(activeScene);
        }

        protected override async void LoadAssetRemote(AssetGuid guid, Action onComplete)
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "CurrentThread.ManagedThreadId != 1");
            
            PendingIds.Add(guid, new List<Action>() { onComplete });
            var key = guid.ToAssetName();
            
#if LOG_MISSING_ASSETS
            await LogIfMissing(key, typeof(GameObject));
#endif

            var handle = Addressables.InstantiateAsync(key);
            handle.Completed += x =>
            {
                Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "CurrentThread.ManagedThreadId != 1");
                
                GameObject prototype = default;
                if (x.Status != AsyncOperationStatus.Succeeded ||
                    x.Result == null)
                {
                    Debug.LogError($"Addressable {guid.ToAssetName()} download failed. Create stub object {nameof(GameObject)}");
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

        protected override void LoadAssetLocal(AssetGuid guid, Action onComplete)
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "CurrentThread.ManagedThreadId != 1");
            
            PendingIds.Add(guid, new List<Action>() { onComplete });

            var handle = Resources.LoadAsync<GameObject>(guid.ToAssetName());

            handle.completed += x =>
            {
                Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "CurrentThread.ManagedThreadId != 1");
                
                GameObject prototype = handle.asset as GameObject;
                if (prototype == null)
                {
                    Debug.LogError($"Local asset {guid.ToAssetName()} download failed. Create stub object {nameof(GameObject)}");
                    prototype = ErrorAsset;
                    PersistentPrototypes[guid] = prototype;
                }
                else
                {
                    LocalPrototypes[guid] = prototype;
                }
                
                Debug.Assert(prototype != null, $"prototype != null {guid.ToAssetName()}");
                OnAssetPrototypeLoaded(guid, prototype);
                
                foreach (var action in PendingIds[guid])
                    action?.Invoke();

                PendingIds.Remove(guid);
            };
        }

        protected override void OnAssetPrototypeLoaded(AssetGuid guid, GameObject asset)
        {
            Debug.Assert(poolScene.isLoaded, "poolScene.isLoaded");
            Debug.Assert(poolScene.IsValid(), "poolScene.IsValid()");
            asset.SetActive(false);
            if (asset.scene != poolScene)
                SceneManager.MoveGameObjectToScene(asset, poolScene);
        }

        protected override bool TryHandleAssetAvailable(AssetGuid guid, out GameObject result)
        {
            if (!base.TryHandleAssetAvailable(guid, out var prototype))
            {
                result = default;
                return false;
            }

            Debug.Assert(prototype != null, $"prototype != null {guid}");

            var activeScene = SceneManager.GetActiveScene();
            result = GameObject.Instantiate(prototype);
            result.SetActive(true);
            if (result.scene != activeScene)
                SceneManager.MoveGameObjectToScene(result, activeScene);

            return true;
        }
    }
}