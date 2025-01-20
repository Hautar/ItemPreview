#if UNITY_EDITOR
using UnityEngine.AddressableAssets;

namespace AssetGuidMap.Editor
{
    internal class AssetGuidItemEditorDescription
    {
        public string         Address;
        public string         Path;
        public string         Guid;
        public AssetReference AssetReference;
    }
}

#endif
