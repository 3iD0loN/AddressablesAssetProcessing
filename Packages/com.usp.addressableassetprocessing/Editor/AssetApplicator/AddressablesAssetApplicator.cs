using System.Collections.Generic;

using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    using USP.MetaAddressables;

    public class AddressablesAssetApplicator : IAssetApplicator
    {
        #region Static Methods
        public static void SetGlobalLabels(AddressableAssetSettings settings, HashSet<string> labels)
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

        #region Fields
        private AddressablesAssetStore assetStore = new AddressablesAssetStore();
        #endregion

        #region Properties
        public IAssetStore AssetStore => assetStore;
        #endregion

        #region Methods
        public void ApplyAsset(AddressableAssetSettings settings,
            string assetFilePath,
            AddressableAssetGroupTemplate groupTemplate,
            string address,
            HashSet<string> labels)
        {
            AddressableAssetGroup group = MetaAddressables.GroupData.Create(settings, groupTemplate);
            AddressableAssetEntry entry = MetaAddressables.AssetData.CreateOrMove(settings, assetFilePath, group, address, labels);
            assetStore.AddAsset(entry);

            SetGlobalLabels(settings, labels);
            assetStore.AddGlobalLabels(labels);
        }
        #endregion
    }
}
