using System;
using UnityEngine.AddressableAssets;

namespace AssetGuidMap
{
    [Serializable]
    internal class GuidMapEntry
    {
#if UNITY_EDITOR
        public AssetReference AssetReference;
#endif
        public AssetGuid      Guid;
        public string         Address;

        public GuidMapEntry Copy()
        {
            return new GuidMapEntry()
            {
#if UNITY_EDITOR
                AssetReference = AssetReference,
#endif
                Guid = Guid,
                Address = Address
            };
        }
    }
}
