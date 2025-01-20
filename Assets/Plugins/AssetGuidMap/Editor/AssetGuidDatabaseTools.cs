#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.U2D;

namespace AssetGuidMap.Editor
{
    public static class AssetGuidDatabaseTools
    {
        internal static bool UpdateDatabaseFromAddressables(string path)
        {
            var database = AssetDatabase.LoadAssetAtPath<BucketStorage>(path);
            Debug.Assert(database != null, "database != null");
            var bucketCopy = Copy(database.buckets);
            var report = new UpdateReport();

            if (!CollectExistingAddressablesGuids(out var existingAssetInfo, report))
            {
                Debug.LogError("please fix issues before you continue");
                return false;
            }

            if (!UpdateExistingNames(bucketCopy[0], existingAssetInfo, report))
            {
                report.Log();
                Debug.LogError("database update canceled!");
                return false;
            }
            
            if (!RemoveEntriesWithInvalidReferenceAndWithoutNameMatching(bucketCopy[0], existingAssetInfo, report))
            {
                report.Log();
                Debug.LogError("database update canceled!");
                return false;
            }

            if (!UpdateRemovedEntries(bucketCopy[0], existingAssetInfo, report))
            {
                report.Log();
                Debug.LogError("database update canceled!");
                return false;
            }
            AddNewEntries(bucketCopy[0], existingAssetInfo, report);

            if (!ValidateDuplicateNames(bucketCopy[0], report))
            {
                report.Log();
                Debug.LogError("database update canceled!");
                return false;
            }
        
            Sort(bucketCopy[0]);
            
            report.Log();
            
            bool result =  UpdateReportEditorWindow.Show(report);

            if (result)
            {
                database = AssetDatabase.LoadAssetAtPath<BucketStorage>(path);
                database.buckets = bucketCopy;
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssetIfDirty(database);
            }

            return result;
        }
        
        private static List<Bucket> Copy(List<Bucket> source)
        {
            var result = new List<Bucket>();
            foreach (var sourceBucket in source)
            {
                result.Add(sourceBucket.Copy());
            }

            return result;
        }
        

        private static bool CollectExistingAddressablesGuids(out List<AssetGuidItemEditorDescription> result, UpdateReport report)
        {
            result = new List<AssetGuidItemEditorDescription>();
            
            var addressableGroups = AddressableAssetSettingsDefaultObject.Settings.groups;
            foreach (var addressableGroup in addressableGroups)
            {
                foreach (var entry in addressableGroup.entries)
                {
                    var path = entry.AssetPath;
                    var guid = AssetDatabase.AssetPathToGUID(path);
                    var assetDescription = new AssetGuidItemEditorDescription()
                    {
                        Address        = entry.address,
                        Path           = entry.AssetPath,
                        Guid           = guid,
                        AssetReference = new AssetReference(guid)
                    };

                    if (AssetDatabase.IsValidFolder(path))
                    {
                        Debug.LogWarning($"Folder found {path}");
                        continue;
                    }

                    if (!assetDescription.AssetReference.RuntimeKeyIsValid())
                    {
                        Debug.LogWarning($"Invalid runtime key {path}");
                        continue;
                    }

                    if (assetDescription.Address != assetDescription.AssetReference.editorAsset.name)
                    {
                        report.Add(UpdateReportMessageType.Warning, $"Asset name '{path}' does not match address name '{assetDescription.Address}'");
                    }

                    if (string.IsNullOrEmpty(assetDescription.Address))
                    {
                        Debug.LogError($" Asset {path} address is null or empty");
                        continue;
                    }
                    
                    result.Add(assetDescription);
        
                    if (entry.MainAssetType == typeof(SpriteAtlas))
                        result.AddRange(CollectGuidsFromAtlas(entry));
                }
            }
            
            result.Sort((left, right) => left.Address.CompareTo(right.Address));
        
            return true;
        }
        
        private static List<AssetGuidItemEditorDescription> CollectGuidsFromAtlas(AddressableAssetEntry assetEntry)
        {
            Debug.Assert(assetEntry.MainAssetType == typeof(SpriteAtlas), "assetEntry.MainAssetType == typeof(SpriteAtlas)");

            var result = new List<AssetGuidItemEditorDescription>();
            
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(assetEntry.AssetPath);
            var packables = atlas.GetPackables();
            foreach (var obj in packables)
            {
                var path = AssetDatabase.GetAssetPath(obj);
                var guid = AssetDatabase.AssetPathToGUID(path);
                var assetRef = new AssetReference(guid);

                var assetDescription = new AssetGuidItemEditorDescription()
                {
                    Path = path,
                    Guid = guid,
                    Address = obj.name,
                    AssetReference = assetRef,
                };
                
                if (string.IsNullOrEmpty(assetDescription.Address))
                {
                    Debug.LogError($"Atlas item {path} address is null or empty");
                    continue;
                }
                
                if (!assetDescription.AssetReference.RuntimeKeyIsValid())
                    continue;
                
                result.Add(assetDescription);
            }
        
            return result;
        }

        private static bool UpdateExistingNames(Bucket bucket, List<AssetGuidItemEditorDescription> existing, UpdateReport report)
        {
            for (int i = bucket.Assets.Count - 1; i >= 0; i--)
            {
                var databaseAssetInfo = bucket.Assets[i];
                // if (!databaseAssetInfo.AssetReference.IsValid())
                //     continue;
                if (databaseAssetInfo.AssetReference == null)
                    continue;

                //try to find existing asset matching one in database by guid
                var existingAssetInfo = existing.Find(x => x.Guid.Equals(databaseAssetInfo.AssetReference.AssetGUID));
                bool exists = existingAssetInfo != null;
                if (!exists)
                    continue;

                //name haven't changed, all ok
                if (databaseAssetInfo.Address.Equals(existingAssetInfo.Address))
                    continue;

                int result = EditorUtility.DisplayDialogComplex("Asset guid update",
                    $"Update asset name from '{databaseAssetInfo.Address}' to '{existingAssetInfo.Address}'?", 
                    "yes", "no", "cancel");
        
                if (result == 0)
                {
                    report.Add(UpdateReportMessageType.Warning, $"Update asset name from '{databaseAssetInfo.Address}' to '{existingAssetInfo.Address}'");
                    databaseAssetInfo.Address = existingAssetInfo.Address;
                }
                else if (result == 1)
                {
                    report.Add(UpdateReportMessageType.Warning, $"Skip update asset name from '{databaseAssetInfo.Address}' to '{existingAssetInfo.Address}'");
                }
                else
                {
                    return false;
                }
            }
        
            return true;
        }
        
                
        private static bool RemoveEntriesWithInvalidReferenceAndWithoutNameMatching(Bucket bucket, List<AssetGuidItemEditorDescription> existing, UpdateReport report)
        {
            for (int i = bucket.Assets.Count - 1; i >= 0; i--)
            {
                var databaseAssetInfo = bucket.Assets[i];
                if (databaseAssetInfo.AssetReference.IsValid())
                    continue;
                
                var existingAssetInfo = existing.Find(x => x.Address.ToLower().Equals(databaseAssetInfo.Address.ToLower()));
                if (existingAssetInfo != null)
                    continue;
                
                bool result = EditorUtility.DisplayDialog("Asset guid update",
                    $"Asset '{databaseAssetInfo.Address}' removed. Apply?", 
                    "yes", "cancel");
        
                if (result)
                {
                    report.Add(UpdateReportMessageType.Warning, $"Remove asset name from '{databaseAssetInfo.Address}'");
                    bucket.Assets.RemoveAt(i);
                }
                else
                {
                    return false;
                }
            }
        
            return true;
        }

        private static bool UpdateRemovedEntries(Bucket bucket, List<AssetGuidItemEditorDescription> existing, UpdateReport report)
        {
            for (int i = bucket.Assets.Count - 1; i >= 0; i--)
            {
                var databaseAssetInfo = bucket.Assets[i];
                
                //try to find existing asset matching one in database by guid
                var existingAssetInfo = existing.Find(x => x.Guid == databaseAssetInfo.AssetReference.AssetGUID);
                bool exists = existingAssetInfo != null;
                if (exists)
                    continue;
        
                //asset is not present, try to find asset matching by name
                existingAssetInfo = existing.Find(x =>
                {
                    var assetName = x.Address;
                    if(assetName.Contains('.'))
                        assetName = assetName.Split('.')[0];
                    
                    return assetName == databaseAssetInfo.Address;
                });
                bool isAssetReplacedButNameRemained = existingAssetInfo != null;

                //deleted asset, but created another with same name. we can update entry in database
                if (isAssetReplacedButNameRemained)
                {
                    int result = EditorUtility.DisplayDialogComplex("Asset guid update",
                        $"Asset {databaseAssetInfo.Address} have been deleted, but new asset with same name found", 
                        "replace existing in database", "remove from database and add later as new", "cancel");
        
                    if (result == 0)
                    {
                        report.Add(UpdateReportMessageType.Warning, $"Replace asset '{databaseAssetInfo.Address}'");
                        databaseAssetInfo.AssetReference = existingAssetInfo.AssetReference;
                    }
                    else if (result == 1)
                    {
                        report.Add(UpdateReportMessageType.Warning, $"Remove asset '{databaseAssetInfo.Address}'");
                        bucket.Assets.RemoveAt(i);
                    }
                    else
                    {
                        return false;
                    }
                }
                //completely removed asset
                else
                {
                    report.Add(UpdateReportMessageType.Warning, $"Remove asset '{databaseAssetInfo.Address}'");
                    bucket.Assets.RemoveAt(i);
                }
            }
        
            return true;
        }
        
        private static bool AddNewEntries(Bucket bucket, List<AssetGuidItemEditorDescription> existing, UpdateReport report)
        {
            for (int i = 0; i < existing.Count; i++)
            {
                var existingAssetInfo = existing[i];
                var databaseAssetInfo = bucket.Assets.Find(x=>x.AssetReference.AssetGUID == existingAssetInfo.Guid);
                bool exists = databaseAssetInfo != null;
                if (exists)
                    continue;

                
                var newEntry = new GuidMapEntry()
                {
                    AssetReference = existingAssetInfo.AssetReference,
                    Address = existingAssetInfo.Address,
                    Guid = bucket.GetNextGuid()
                };
                
                bucket.Assets.Add(newEntry);

                report.Add(UpdateReportMessageType.Log, $"New asset '{newEntry.Address}' with id {newEntry.Guid.Value}");
            }
        
            return true;
        }
        
        private static bool ValidateDuplicateNames(Bucket bucket, UpdateReport report)
        {
            bool result = true;
            
            for (int i = 0; i < bucket.Assets.Count - 1; i++)
            {
                var left = bucket.Assets[i];
                
                for (int j = i + 1; j < bucket.Assets.Count; j++)
                {
                    var right = bucket.Assets[j];
                    if (left.Address != right.Address)
                        continue;
                    
                    string message = $"Duplicate asset '{left.Address}' found. Please fix and run again";
                    report.Add(UpdateReportMessageType.Error, message);
                    EditorUtility.DisplayDialog("Error", message, "cancel");
                    result = false;
                }
            }

            return result;
        }
        
        private static void Sort(Bucket bucket)
        {
            bucket.Assets.Sort((left, right) => left.Address.CompareTo(right.Address));
        }
        
        
        internal static bool UpdateDatabaseFromResources(string path)
        {
            var database = AssetDatabase.LoadAssetAtPath<BucketStorage>(path);
            Debug.Assert(database != null, "database != null");
            var bucketCopy = Copy(database.buckets);
            var report = new UpdateReport();

            if (!CollectExistingResourcesGuids(out var existingAssetInfo, report))
            {
                Debug.LogError("please fix issues before you continue");
                return false;
            }

            if (!UpdateExistingNames(bucketCopy[0], existingAssetInfo, report))
            {
                report.Log();
                Debug.LogError("database update canceled!");
                return false;
            }
        
            if (!UpdateRemovedEntries(bucketCopy[0], existingAssetInfo, report))
            {
                report.Log();
                Debug.LogError("database update canceled!");
                return false;
            }
            AddNewEntries(bucketCopy[0], existingAssetInfo, report);
        
            if (!ValidateDuplicateNames(bucketCopy[0], report))
            {
                report.Log();
                Debug.LogError("database update canceled!");
                return false;
            }
        
            Sort(bucketCopy[0]);
            
            report.Log();
            
            bool result =  UpdateReportEditorWindow.Show(report);

            if (result)
            {
                database = AssetDatabase.LoadAssetAtPath<BucketStorage>(path);
                database.buckets = bucketCopy;
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssetIfDirty(database);
            }

            return result;
        }
        
        
        private static bool CollectExistingResourcesGuids(out List<AssetGuidItemEditorDescription> result, UpdateReport report)
        {
            result = new List<AssetGuidItemEditorDescription>();

            const string pathPrefix = "Assets/Resources/";
            
            var assetGuids= AssetDatabase.FindAssets(null, new[] { pathPrefix });

            foreach (var guid in assetGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                
                if (asset == null)
                    continue;
                
                var name = asset.name;

                var assetDescription = new AssetGuidItemEditorDescription()
                {
                    Address        = name,
                    Path           = path,
                    Guid           = guid,
                    AssetReference = new AssetReference(guid)
                };

                if (AssetDatabase.IsValidFolder(path))
                {
                    Debug.LogWarning($"Folder found {path}");
                    continue;
                }
                
                if (!assetDescription.AssetReference.RuntimeKeyIsValid())
                {
                    Debug.LogWarning($"Invalid runtime key {path}");
                    continue;
                }

                var relativeName = assetDescription.Path;
                relativeName = relativeName.Remove(0, pathPrefix.Length);
                int index = relativeName.IndexOf(".");
                if (index >= 0)
                    relativeName = relativeName.Substring(0, index);
                assetDescription.Address = relativeName;

                // if (assetDescription.Address != assetDescription.AssetReference.editorAsset.name)
                // {
                //     report.Add(UpdateReportMessageType.Warning, $"Asset name '{path}' does not match address name '{assetDescription.Address}'");
                //     continue;
                // }

                if (string.IsNullOrEmpty(assetDescription.Address))
                {
                    Debug.LogError($" Asset {path} address is null or empty");
                    continue;
                }
                    
                result.Add(assetDescription);
                
            }
            
            result.Sort((left, right) => left.Address.CompareTo(right.Address));
        
            return true;
        }

    }
}
#endif