using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    public class ComparisonEntryFactory
    {
        #region Properties
        public AddressableAssetSettings Settings { get; }

        public string AssetFilePath { get; }

        public CombinedAssetApplicator AssetApplicationMethod { get; }

        public ComparisonEntry ComparisonEntry
        {
            get;
            private set;
        }
        #endregion

        #region Methods
        public ComparisonEntryFactory(AddressableAssetSettings settings, string assetFilePath,
            CombinedAssetApplicator assetApplicationMethod)
        {
            Settings = settings;
            AssetFilePath = assetFilePath;
            AssetApplicationMethod = assetApplicationMethod;
        }

        public void Create()
        {
            ComparisonEntry = ComparisonEntries.CreateEntry(Settings, AssetApplicationMethod, AssetFilePath);
        }
        #endregion
    }
}