using System.Collections.Generic;

using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    using UnityEditorInternal;
    using USP.MetaAddressables;

    public class AddressablesAssetApplicator : IAssetApplicator<AddressablesAssetStore>
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
        public AddressablesAssetStore AssetStore { get; }

        IAssetStore IAssetApplicator.AssetStore => AssetStore;
        #endregion

        #region Methods
        public AddressablesAssetApplicator(AddressablesAssetStore assetStore = null)
        {
            AssetStore = assetStore ?? new AddressablesAssetStore();
        }

        public void ApplyAsset(AddressableAssetSettings settings,
            string assetFilePath,
            AddressableAssetGroupTemplate groupTemplate,
            string address,
            HashSet<string> labels)
        {
            AddressableAssetGroup group = MetaAddressables.GroupData.Create(settings, groupTemplate);
            AddressableAssetEntry entry = MetaAddressables.AssetData.CreateOrMove(settings, assetFilePath, group, address, labels);
            AssetStore.AddAsset(entry);

            SetGlobalLabels(settings, labels);
            AssetStore.AddGlobalLabels(labels);
        }

        public void ApplyAsset(AddressableAssetSettings settings, MetaAddressables.UserData userData)
        {
            AddressableAssetGroup group = MetaAddressables.GroupData.Create(settings, userData.Group);
            AddressableAssetEntry entry = MetaAddressables.AssetData.CreateOrMove(settings, group, userData.Asset);
            AssetStore.AddAsset(entry);

            SetGlobalLabels(settings, userData.Asset.Labels);
            AssetStore.AddGlobalLabels(userData.Asset.Labels);
        }
        #endregion
    }
}
