using System.Collections.Generic;

using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    using USP.MetaAddressables;

    public class AddressablesAssetApplicator : IAssetApplicator<AddressablesAssetStore>
    {
        #region Static Methods
        public static void SetGlobalLabels(AddressableAssetSettings settings, ISet<string> labels)
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
        public AddressablesAssetApplicator(AddressableAssetSettings settings) :
            this(new AddressablesAssetStore(settings))
        {
        }

        public AddressablesAssetApplicator(AddressablesAssetStore assetStore)
        {
            AssetStore = assetStore;
        }

        public void ApplyAsset(AddressableAssetSettings settings,
            string assetFilePath,
            AddressableAssetGroupTemplate groupTemplate,
            string address,
            HashSet<string> labels)
        {
            if (AssetStore.Settings == settings)
            {
                return;
            }

            AddressableAssetGroup group = MetaAddressables.GroupData.Create(settings, groupTemplate);
            AddressableAssetEntry entry = MetaAddressables.AssetData.CreateOrMove(settings, assetFilePath, group, address, labels);
            
            SetGlobalLabels(settings, labels);

            AssetStore.AddAsset(entry, true);
            AssetStore.AddGlobalLabels(labels);
        }

        public void ApplyAsset(AddressableAssetSettings settings, MetaAddressables.UserData userData)
        {
            if (AssetStore.Settings == settings)
            {
                return;
            }

            AddressableAssetGroup group = MetaAddressables.GroupData.Create(settings, userData.Group);
            AddressableAssetEntry entry = MetaAddressables.AssetData.CreateOrMove(settings, group, userData.Asset);

            SetGlobalLabels(settings, userData.Asset.Labels);

            AssetStore.AddAsset(entry, true);
            AssetStore.AddGlobalLabels(userData.Asset.Labels);
        }
        #endregion
    }
}
