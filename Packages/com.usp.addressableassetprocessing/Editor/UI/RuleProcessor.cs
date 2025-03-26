using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    using USP.MetaAddressables;

    public interface IRuleProcessor
    {
        #region Properties
        AddressableAssetSettings Settings { get; }

        string AssetFilePath { get; }

        RuleSettings Rules { get; }

        MetaAddressables.UserData ProcessedData { get; }
        #endregion

        #region Methods
        void ProcessRules();
        #endregion
    }

    public class RuleProcessor : IRuleProcessor
    {
        #region Properties
        public AddressableAssetSettings Settings { get; }

        public string AssetFilePath { get; }

        public RuleSettings Rules { get; }

        public MetaAddressables.UserData ProcessedData
        {
            get
            {
                // After processing, the processed asest is located in the asset store.
                Rules.AssetApplicationMethod.AssetStore.DataByAssetPath.TryGetValue(AssetFilePath,
                    out MetaAddressables.UserData result);

                return result;
            }
        }

        public ComparisonEntry ComparisonEntry
        {
            get
            {
                return ComparisonEntries.CreateEntry(Settings, Rules.AssetApplicationMethod as CombinedAssetApplicator, AssetFilePath);
            }
        }
        #endregion

        #region Methods
        public RuleProcessor(AddressableAssetSettings settings, RuleSettings rules) :
            this(settings, string.Empty, rules)
        {
        }

        public RuleProcessor(AddressableAssetSettings settings, string assetFilePath, RuleSettings rules)
        {
            Settings = settings;
            AssetFilePath = assetFilePath;
            Rules = rules;
        }

        public void ProcessRules()
        {
            if (string.IsNullOrEmpty(AssetFilePath))
            {
                return;
            }

            AddressablesProcessing.ProcessAsset(Settings,
                AssetFilePath,
                Rules.GroupExtractionMethod,
                Rules.AddressExtractionMethod,
                Rules.LabelExtractionMethod,
                Rules.AssetApplicationMethod);
        }
        #endregion
    }
}
