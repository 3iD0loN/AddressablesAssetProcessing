using System.Collections.Generic;

using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    public class SimulatedAssetApplicator : IAssetApplicator
    {
        #region Properties
        public SimulatedAssetStore SimulatedAssetStore { get; } = new SimulatedAssetStore();
        #endregion

        #region Properties
        public IAssetStore AssetStore => SimulatedAssetStore;
        #endregion

        #region Methods
        public virtual void ApplyAsset(AddressableAssetSettings settings,
            string assetFilePath,
            AddressableAssetGroupTemplate group,
            string address,
            HashSet<string> labels)
        {
            SimulatedAssetStore.AddAsset(assetFilePath, group, address, labels);

            SimulatedAssetStore.AddGlobalLabels(labels);
        }
        #endregion
    }
}