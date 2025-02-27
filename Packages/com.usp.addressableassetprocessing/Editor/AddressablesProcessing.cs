using System.Collections.Generic;

using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    public static class AddressablesProcessing
    {
        #region Static Methods
        public static void ProcessAssets(IEnumerable<string> assetFilePaths,
            IExtractor<string, AddressableAssetGroupTemplate> groupExtractor,
            IExtractor<string, string> addressExtractor,
            IExtractor<string, HashSet<string>> labelExtractor)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;

            foreach (string assetFilePath in assetFilePaths)
            {
                ProcessAsset(settings, assetFilePath, groupExtractor, addressExtractor, labelExtractor);
            }
        }

        public static void ProcessAsset(AddressableAssetSettings settings, 
            string assetFilePath,
            IExtractor<string, AddressableAssetGroupTemplate> groupExtractor,
            IExtractor<string, string> addressExtractor,
            IExtractor<string, HashSet<string>> labelExtractor)
        {
            // Select the asset file path to the group selector to set the appropriate group template.
            AddressableAssetGroupTemplate group = null;
            groupExtractor.Extract(assetFilePath, ref group);

            string address = null;
            addressExtractor.Extract(assetFilePath, ref address);

            // Extract labels from the folder path.
            var labels = new HashSet<string>();
            labelExtractor.Extract(assetFilePath, ref labels);

            MetaAddressablesProcessing.SetAddressableAsset(assetFilePath, group, address, labels);

            MetaAddressablesProcessing.SetGlobalLabels(settings, labels);
        }
        #endregion
    }
}
