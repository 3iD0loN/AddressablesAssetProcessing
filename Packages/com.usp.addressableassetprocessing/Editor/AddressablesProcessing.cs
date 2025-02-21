using System.Collections.Generic;

using UnityEditor.AddressableAssets;

namespace USP.AddressablesAssetProcessing
{
    using UnityEditor.AddressableAssets.Settings;
    using USP.MetaAddressables;

    public static class AddressablesProcessing
    {
        #region Static Methods
        public static void ProcessAssets(IEnumerable<string> assetFilePaths,
            IGroupSelector<string> groupSelector,
            IKeyExtractor<string, HashSet<string>> labelExtractor)
        {
            foreach (string assetFilePath in assetFilePaths)
            {
                ProcessAsset(assetFilePath, groupSelector, labelExtractor);
            }
        }

        public static void ClearAsset(string assetFilePath)
        {
            MetaAddressables.Clear(assetFilePath);
        }

        public static void ProcessAsset(string assetFilePath,
            IGroupSelector<string> groupSelector,
            IKeyExtractor<string, HashSet<string>> labelExtractor)
        {
            // Apply the asset file path to the group selector to set the appropriate group template.
            groupSelector.Apply(assetFilePath);

            // Extract labels from the folder path.
            var extractedLabels = new HashSet<string>();
            labelExtractor.Extract(assetFilePath, extractedLabels);

            SetAddressableAsset(assetFilePath, extractedLabels);

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            SetLabels(settings, extractedLabels);
        }

        private static void SetAddressableAsset(string assetFilePath, HashSet<string> labels)
        {
            // Get the user data associated with the asset file path.
            MetaAddressables.UserData userData = MetaAddressables.Read(assetFilePath);

            // If there was no valid user data associated with the asset file path, then:
            if (userData == null)
            {
                // Do nothing else.
                return;
            }

            // Union the labels that were extracted with the current ones associated with the asset.
            userData.Asset.Labels.UnionWith(labels);

            // Take the current address of the asset and simplify it.
            userData.Asset.Address = MetaAddressables.AssetData.SimplifyAddress(userData.Asset.Address);

            // Generate Addressables groups from the Meta file.
            // This is done before saving MetaAddressables to file in case we find groups that already match.
            MetaAddressables.Generate(userData);

            // Save to MetaAddressables changes.
            MetaAddressables.Write(assetFilePath, userData);
        }

        private static void SetLabels(AddressableAssetSettings settings, HashSet<string> labels)
        {
            // If there are valid settings, then:
            if (settings == null)
            {
                return;
            }

            // For every label extracted from the asset path, perform the following:
            foreach (var label in labels)
            {
                // Attempt to add the label to the global list.
                settings.AddLabel(label);
            }
        }
        #endregion
    }
}
