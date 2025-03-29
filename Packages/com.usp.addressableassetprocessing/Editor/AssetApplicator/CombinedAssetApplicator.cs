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

#if ENABLE_METAADDRESSABLES
        public MetaAddressablesAssetApplicator FileProcessingToMetaFileApplicator { get; }

        public MetaAddressablesAssetApplicator MetaFileToAddressablesApplicator { get; }
#endif

        public IAssetStore AssetStore => SimulatedAssetApplicator.AssetStore;

#if ENABLE_METAADDRESSABLES
        public MetaAddressablesAssetStore MetaAddressablesAssetStore { get; }
#endif

        public AddressablesAssetStore AddressablesAssetStore { get; }
        #endregion

        #region Methods
        public CombinedAssetApplicator(AddressablesAssetStore addressablesAssetStore = null)
        {
            SimulatedAssetApplicator = new SimulatedAssetApplicator();

#if ENABLE_METAADDRESSABLES
            this.MetaAddressablesAssetStore = new MetaAddressablesAssetStore();

            FileProcessingToMetaFileApplicator = new MetaAddressablesAssetApplicator(MetaAddressablesAssetStore, null, false, true);
            MetaFileToAddressablesApplicator = new MetaAddressablesAssetApplicator(MetaAddressablesAssetStore, addressablesAssetStore, true);
#endif

            this.AddressablesAssetStore = addressablesAssetStore;
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

            // Add entries from the respective representations.
            AddAsset(settings, assetFilePath, userData.Asset.Labels);
        }

        private void AddAsset(AddressableAssetSettings settings, string assetFilePath, ISet<string> labels)
        {
#if ENABLE_METAADDRESSABLES
            MetaAddressablesAssetStore.AddAsset(assetFilePath);
            MetaAddressablesAssetStore.AddGlobalLabels(labels);
#endif

            AddressablesAssetStore.AddAsset(assetFilePath);
            AddressablesAssetStore.AddGlobalLabels();
        }
        #endregion
    }
}