#if UNITY_EDITOR
using System.IO;
using UnityEditor;


namespace AssetGuidMap.Editor
{
    public static class AssetGuidMapUpdateTool
    {
#if RESOURCES_REPOSITORY
        [MenuItem("Tools/Asset Map Update")]
        public static bool UpdateForResourceRepository()
        {
            return AssetGuidDatabaseTools.UpdateDatabaseFromAddressables(Path.Combine("Assets", "Content", "Common", "AddressableAssetsBucketStorage.asset"));
        }
#else
        [MenuItem("Tools/Asset Map Update")]
        public static bool UpdateForDotsRepository()
        {
            return AssetGuidDatabaseTools.UpdateDatabaseFromResources(Path.Combine("Assets", "Resources", "Settings", "View", "LocalAssetsBucketStorage.asset"));
        }
#endif
    }
}
#endif