namespace AssetGuidMap
{
    public static class AssetGuidExtensions
    {
        public static string ToAssetName(this AssetGuid assetGuid) => AssetGuidManager.Instance.GetAssetName(assetGuid);
        
        public static bool TryGetAssetName(this AssetGuid assetGuid, out string assetName) =>
            AssetGuidManager.Instance.TryGetAssetName(assetGuid, out assetName);
        
        public static AssetGuid ToAssetGuid(this string assetName) => AssetGuidManager.Instance.GetAssetId(assetName);

        public static bool TryGetAssetGuid(this string assetName, out AssetGuid assetGuid)
        {
            if (AssetGuidManager.Instance != null)
                return AssetGuidManager.Instance.TryGetAssetId(assetName, out assetGuid);
            
            assetGuid = default;
            return false;
        }
    }
}
