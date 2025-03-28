using System.Collections.Generic;

using UnityEditor;
using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
#if ENABLE_METAADDRESSABLES
    using USP.MetaAddressables;
#endif

    public class CombinedAssetApplicator : IAssetApplicator
    {
        #region Properties
        public SimulatedAssetApplicator SimulatedAssetApplicator { get; }

        public AddressablesAssetApplicator AddressablesAssetApplicator { get; }

#if ENABLE_METAADDRESSABLES
        public MetaAddressablesAssetApplicator MetaAddressablesAssetApplicator { get; }
#endif
        public IAssetStore AssetStore => SimulatedAssetApplicator.AssetStore;
        #endregion

        #region Methods
        public CombinedAssetApplicator(AddressablesAssetStore addressablesAssetStore = null)
        {
            SimulatedAssetApplicator = new SimulatedAssetApplicator();

#if ENABLE_METAADDRESSABLES
            MetaAddressablesAssetApplicator = new MetaAddressablesAssetApplicator();
#endif

            AddressablesAssetApplicator = new AddressablesAssetApplicator(addressablesAssetStore);
        }

        public void ApplyAsset(AddressableAssetSettings settings,
            string assetFilePath,
            AddressableAssetGroupTemplate group,
            string address,
            HashSet<string> labels)
        {
            SimulatedAssetApplicator.ApplyAsset(settings, assetFilePath, group, address, labels);

            AddAsset(settings, assetFilePath, labels);
        }

        public void ApplyAsset(AddressableAssetSettings settings, MetaAddressables.UserData userData)
        {
            var assetFilePath = AssetDatabase.GUIDToAssetPath(userData.Asset.Guid);

            SimulatedAssetApplicator.ApplyAsset(settings, userData, assetFilePath);

            AddAsset(settings, assetFilePath, userData.Asset.Labels);
        }

        private void AddAsset(AddressableAssetSettings settings, string assetFilePath, ISet<string> labels)
        {
#if ENABLE_METAADDRESSABLES
            var metaAddressablesAssetStore = MetaAddressablesAssetApplicator.AssetStore;
            metaAddressablesAssetStore.AddAsset(assetFilePath);
            metaAddressablesAssetStore.AddGlobalLabels(labels);
#endif

            var addressablesAssetStore = AddressablesAssetApplicator.AssetStore;
            addressablesAssetStore.AddAsset(assetFilePath);
            addressablesAssetStore.AddGlobalLabels();
        }
        #endregion
    }
}