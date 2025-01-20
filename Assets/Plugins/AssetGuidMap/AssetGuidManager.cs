using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AssetGuidMap
{
    public class AssetGuidManager
    {
        private static AssetGuidManager instance;

        public static AssetGuidManager Instance => instance;

        public static bool IsInitialized => instance != null;

        private AssetGuidDatabase database;

        private AssetGuidManager()
        {
        }

        public static void InitializeAsync(List<string> localPaths, List<string> remotePaths, Action onComplete = null,
            Action onFailed = null)
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == 1, "Thread.CurrentThread.ManagedThreadId == 1");

            if (instance != null)
            {
                Debug.LogError("AssetGuidManager already initialized");
                return;
            }

            var buckets = new List<Bucket>();

            try
            {                
                foreach (var path in localPaths)
                {
                    var scriptableObject = Resources.Load<BucketStorage>(path);
                    buckets.AddRange(scriptableObject.buckets);
                }
                
                instance = new AssetGuidManager();
                instance.database = new AssetGuidDatabase(buckets);
                onComplete?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                onFailed?.Invoke();
            }
        }

        internal string GetAssetName(AssetGuid assetGuid)
        {
            if (!assetGuid.IsValid)
                return string.Empty;
            
            if (database.NameOverId.TryGetValue(assetGuid, out var result))
                return result;
            
            Debug.LogError($"Guid {assetGuid.Value} missing asset name in database");
            return String.Empty;
        }

        internal AssetGuid GetAssetId(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
                return AssetGuid.Invalid;
            
            if (database.IdOverName.TryGetValue(assetName, out var result))
                return result;
            
            Debug.LogError($"Asset {assetName} have no guid in database");
            return AssetGuid.Invalid;
        }

        internal bool TryGetAssetName(AssetGuid assetGuid, out string result)
        {
            if (!assetGuid.IsValid)
            {
                result = string.Empty;
                return false;
            } 
            
            return database.NameOverId.TryGetValue(assetGuid, out result);
        }

        internal bool TryGetAssetId(string assetName, out AssetGuid result)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                result = AssetGuid.Invalid;
                return false;
            }
            
            return database.IdOverName.TryGetValue(assetName, out result);
        }

        internal bool ContainsAsset(AssetGuid assetGuid) => database.NameOverId.ContainsKey(assetGuid);

        internal bool ContainsAsset(string assetName) => database.IdOverName.ContainsKey(assetName);
        
#if UNITY_EDITOR
        public static void EditorClear()
        {
            Debug.Log("<color=yellow>AssetGuidManager clear</color>");
            instance = null;
        }
#endif
    }
}
