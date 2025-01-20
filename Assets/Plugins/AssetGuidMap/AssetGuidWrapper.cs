namespace AssetGuidMap
{
    public class AssetGuidWrapper
    {
        private readonly string name;
        private AssetGuid guid;

        public AssetGuidWrapper(string assetName)
        {
            name = assetName;
        }

        public static implicit operator AssetGuid(AssetGuidWrapper x) => x.Get();

        public AssetGuid Get()
        {
            if (guid.IsValid) return guid;

            if (name.TryGetAssetGuid(out guid))
                return guid;

            return default;
        }
    }
}