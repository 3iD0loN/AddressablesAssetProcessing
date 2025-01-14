using System.Collections.Generic;
using System.Linq;

using UnityEditor.AddressableAssets;

namespace USP.AddressablesAssetProcessing
{
    using USP.MetaAddressables;

    public static class AddressablesProcessing
    {
        #region Static Fields
        public static List<MetaAddressables.UserData> processedUserDatas = new List<MetaAddressables.UserData>();

        public static HashSet<string> proccessedlabels = new HashSet<string>();
        #endregion

        #region Static Methods
        public static void ProcessAssets(IEnumerable<string> assetFilePaths,
            IGroupSelector groupSelector,
            IKeyExtractor<string, HashSet<string>> labelExtractor)
        {
            processedUserDatas.Clear();
            proccessedlabels.Clear();

            foreach (string assetFilePath in assetFilePaths)
            {
                ProcessAsset(assetFilePath, groupSelector, labelExtractor);
            }
        }

        public static void ProcessAsset(string assetFilePath,
            IGroupSelector groupSelector,
            IKeyExtractor<string, HashSet<string>> labelExtractor)
        {
            groupSelector.Apply(assetFilePath);

            MetaAddressables.UserData userData = MetaAddressables.Read(assetFilePath);

            if (userData == null)
            {
                return;
            }

            // Extract labels from the folder path.
            var extractedLabels = new HashSet<string>();
            labelExtractor.Extract(assetFilePath, extractedLabels);

            // Add the extracted labels to the labels associated with the asset.
            extractedLabels.UnionWith(userData.Asset.Labels);
            proccessedlabels.UnionWith(extractedLabels);
            userData.Asset.Labels = extractedLabels.ToList();

            userData.Asset.Address = MetaAddressables.AssetData.SimplifyAddress(userData.Asset.Address);

            processedUserDatas.Add(userData);

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var labels = settings.GetLabels();
            var labelsSet = new HashSet<string>(labels);
            labelsSet.UnionWith(extractedLabels);
            foreach (var label in labelsSet)
            {
                settings.AddLabel(label);
            }

            // Generate Addressables groups from the Meta file.
            // This is done before saving MetaAddressables to file in case we find groups that already match.
            MetaAddressables.Generate(userData);

            // Save to MetaAddressables changes.
            MetaAddressables.Write(assetFilePath, userData);
        }
        #endregion
    }
}
