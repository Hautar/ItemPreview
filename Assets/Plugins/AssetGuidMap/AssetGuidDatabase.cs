using System.Collections.Generic;
using UnityEngine;

namespace AssetGuidMap
{
    internal class AssetGuidDatabase
    {
        internal Dictionary<AssetGuid, string> NameOverId;
        internal Dictionary<string, AssetGuid> IdOverName;

        public AssetGuidDatabase(List<Bucket> buckets)
        {
            Debug.Assert(IsBucketIdsValid(buckets), "IsBucketIdsValid(buckets)");
            Debug.Assert(IsBucketsContentValid(buckets), "IsBucketsContentValid(buckets)");
            
            Initialize(buckets);

            //Validate();
        }

        private bool IsBucketIdsValid(List<Bucket> buckets)
        {
            for (int i = 0; i < buckets.Count; i++)
            {
                if (!buckets[i].IsValid())
                    return false;
            }
            
            for (int i = 0; i < buckets.Count - 1; i++)
            {
                for (int j = i + 1; j < buckets.Count; j++)
                {
                    if (buckets[i].Id == buckets[j].Id)
                        return false;
                }
            }

            return true;
        }
        
        private bool IsBucketsContentValid(List<Bucket> buckets)
        {
            for (int i = 0; i < buckets.Count; i++)
            {
                if (buckets[i].Id < 0 || buckets[i].Id >= Bucket.MaxCount)
                    return false;
            }
            
            for (int i = 0; i < buckets.Count - 1; i++)
            {
                for (int j = i + 1; j < buckets.Count; j++)
                {
                    if (buckets[i].Id == buckets[j].Id)
                        return false;
                }
            }
            
            for (int i = 0; i < buckets.Count - 1; i++)
            {
                for (int j = i + 1; j < buckets.Count; j++)
                {
                    if (buckets[i].Id == buckets[j].Id)
                        return false;
                }
            }

            return true;
        }

        private void Initialize(List<Bucket> buckets)
        {
            NameOverId = new Dictionary<AssetGuid, string>();
            IdOverName = new Dictionary<string, AssetGuid>();

            foreach (var bucket in buckets)
            {
                foreach (var guidMapEntry in bucket.Assets)
                {
                    var guid = guidMapEntry.Guid;
                    var name = guidMapEntry.Address;

                    Debug.Assert(!NameOverId.ContainsKey(guid), $"!NameOverId.ContainsKey({guid.Value})");
                    Debug.Assert(!IdOverName.ContainsKey(name), $"!IdOverName.ContainsKey({name})");
                    
                    NameOverId.Add(guid, name);
                    IdOverName.Add(name, guid);
                }
            }
        }

        public void Validate()
        {
            Debug.Assert(NameOverId.Count == IdOverName.Count, "NameOverId.Count == IdOverName.Count");

            foreach (var keyValue in NameOverId)
            {
                Debug.Assert(keyValue.Key.IsValid, "keyValue.Key.IsValid");
                Debug.Assert(!string.IsNullOrEmpty(keyValue.Value), "!string.IsNullOrEmpty(keyValue.Value)");
                Debug.Assert(IdOverName[keyValue.Value] == keyValue.Key, "IdOverName[keyValue.Value] == keyValue.Key");
            }
            
            foreach (var keyValue in IdOverName)
            {
                Debug.Assert(keyValue.Value.IsValid, "keyValue.Value.IsValid");
                Debug.Assert(!string.IsNullOrEmpty(keyValue.Key), "!string.IsNullOrEmpty(keyValue.Key)");
                Debug.Assert(NameOverId[keyValue.Value] == keyValue.Key, "NameOverId[keyValue.Value] == keyValue.Key");
            }
        }
    }
}
