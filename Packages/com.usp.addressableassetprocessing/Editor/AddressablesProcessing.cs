using System;
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
            // Select the asset file path to the group selector to set the appropriate group template.
            AddressableAssetGroupTemplate group = groupSelector.Select(assetFilePath);

            // Extract labels from the folder path.
            var extractedLabels = new HashSet<string>();
            labelExtractor.Extract(assetFilePath, extractedLabels);

            MetaAddressablesProcessing.SetAddressableAsset(assetFilePath, group, MetaAddressables.AssetData.SimplifyAddress, extractedLabels);

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            MetaAddressablesProcessing.SetGlobalLabels(settings, extractedLabels);
        }
        #endregion
    }
}
