using System.Collections.Generic;

using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    using DocumentFormat.OpenXml.Spreadsheet;
    using USP.MetaAddressables;

    public static class AddressablesProcessing
    {
        #region Static Methods
        public static void ProcessAssets(IEnumerable<string> assetFilePaths,
            IGroupSelector<string> groupSelector,
            IExtractor<string, HashSet<string>> labelExtractor)
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
            IExtractor<string, HashSet<string>> labelExtractor)
        {
            // Select the asset file path to the group selector to set the appropriate group template.
            AddressableAssetGroupTemplate group = null;
            groupSelector.Extract(assetFilePath, ref group);

            string address = MetaAddressables.AssetData.SimplifyAddress(assetFilePath);

            // Extract labels from the folder path.
            var extractedLabels = new HashSet<string>();
            labelExtractor.Extract(assetFilePath, ref extractedLabels);

            MetaAddressablesProcessing.SetAddressableAsset(assetFilePath, group, address, extractedLabels);

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            MetaAddressablesProcessing.SetGlobalLabels(settings, extractedLabels);
        }
        #endregion
    }
}
