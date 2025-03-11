using System.Collections.Generic;

using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    public class SimulatedAssetApplicator : IAssetApplicator
    {
        #region Properties
        public SimulatedAssetStore SimulatedAssetStore { get; } = new SimulatedAssetStore();
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

        public virtual void ApplyGlobal(AddressableAssetSettings settings, 
            HashSet<string> labels)
        {}
        #endregion
    }
}