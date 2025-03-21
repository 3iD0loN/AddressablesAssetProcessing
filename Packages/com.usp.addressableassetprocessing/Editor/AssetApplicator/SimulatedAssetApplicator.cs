using System.Collections.Generic;

using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    using USP.MetaAddressables;

    public class SimulatedAssetApplicator : IAssetApplicator<SimulatedAssetStore>
    {
        #region Properties
        public SimulatedAssetStore AssetStore { get; } = new SimulatedAssetStore();

        IAssetStore IAssetApplicator.AssetStore => AssetStore;
        #endregion

        #region Methods
        public virtual void ApplyAsset(AddressableAssetSettings settings,
            string assetFilePath,
            AddressableAssetGroupTemplate group,
            string address,
            HashSet<string> labels)
        {
            AssetStore.AddAsset(assetFilePath, group, address, labels);

            AssetStore.AddGlobalLabels(labels);
        }

        public virtual void ApplyAsset(AddressableAssetSettings settings, MetaAddressables.UserData userData)
        {
            AssetStore.AddAsset(userData, true);

            AssetStore.AddGlobalLabels(userData.Asset.Labels);
        }
        #endregion
    }
}