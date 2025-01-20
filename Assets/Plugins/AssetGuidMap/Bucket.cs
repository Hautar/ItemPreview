using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR && ODIN_INSPECTOR
using UnityEditor;
#endif

namespace AssetGuidMap
{
    [Serializable]
    public class Bucket
    {
        public const int MaxCount = 10;
        public const int Capacity = (int)((AssetGuid.MaxValue - (AssetGuid.Invalid + 1)) / MaxCount);

        private static uint[] start;
        private static uint[] end;
        
        [SerializeField]
        internal string       Name;
        [SerializeField]
        internal int          Id = -1;
        [SerializeField]
        internal uint         GuidCounter;
#if UNITY_EDITOR && RESOURCES_REPOSITORY
        [Searchable]
#endif
        [SerializeField]
        internal List<GuidMapEntry> Assets;

        public static int GetBucketId(AssetGuid guid) => (int)(guid.Value / Capacity);
        
        static Bucket()
        {
            uint from = AssetGuid.Invalid + 1;
            uint to   = AssetGuid.MaxValue;


            start = new uint[MaxCount];
            end   = new uint[MaxCount];

            start[0] = from;
            end[0]   = start[0] + Capacity;

            for (int i = 1; i < MaxCount; i++)
            {
                start[i] = end[i - 1] + 1;
                end[i]   = start[i] + Capacity;
            }
        }

        internal AssetGuid GetNextGuid()
        {
            GuidCounter++;
            var freeGuid = start[Id] + GuidCounter;
            if (freeGuid > end[Id])
                throw new ArgumentException();

            return freeGuid;
        }

        internal bool IsValid()
        {
            if (Id < 0 || Id >= MaxCount)
                return false;

            for (int i = 0; i < Assets.Count - 1; i++)
            {
                for (int j = i + 1; j < Assets.Count; j++)
                {
#if UNITY_EDITOR
                    if (Assets[i].AssetReference == Assets[j].AssetReference)
                    {
                        Debug.LogError($"duplicate asset reference {i} {j}: {Assets[i].AssetReference}");
                        return false;
                    }
#endif

                    if (Assets[i].Guid == Assets[j].Guid)
                    {
                        Debug.LogError($"duplicate asset guids {i} {j}: {Assets[i].Guid}");
                        return false;
                    }

                    if (Assets[i].Address == Assets[j].Address)
                    {
                        Debug.LogError($"duplicate asset names {i} {j}: {Assets[i].Address}");
                        return false;
                    }
                }
            }

            return true;
        }

        internal Bucket Copy()
        {
            var result = new Bucket()
            {
                Id          = Id,
                Name        = Name,
                GuidCounter = GuidCounter,
                Assets      = new List<GuidMapEntry>()
            };

            for (int i = 0; i < Assets.Count; i++)
            {
                result.Assets.Add(Assets[i].Copy());
            }

            return result;
        }
        
#if UNITY_EDITOR
        // [Button]
        // public void UpdateAutomatic()
        // {
        //     AssetGuidDatabaseTools.UpdateDatabase(database);
        //     AssetDatabase.SaveAssets();
        // }
        
        // [Button]
        // public void UpdateManual()
        // {
        //     AssetGuidDatabaseTools.UpdateDatabaseManual(database);
        //     AssetDatabase.SaveAssets();            
        // }
        
        // [Button]
        // internal void Clear()
        // {
        //     Assets = new List<GuidMapEntry>();
        //     AssetDatabase.SaveAssets();
        // }
#endif
    }
}
