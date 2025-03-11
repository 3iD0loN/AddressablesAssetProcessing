using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
#if ENABLE_METAADDRESSABLES
    using USP.MetaAddressables;
#endif

    public class CombinedAssetApplicator : SimulatedAssetApplicator
    {
        #region Properties
        public AddressablesAssetStore AddressablesAssetStore { get; }

#if ENABLE_METAADDRESSABLES
        public MetaAddressablesAssetStore MetaAddressablesAssetStore { get; }
#endif
        #endregion

        #region Methods
        public CombinedAssetApplicator(AddressablesAssetStore addressablesAssetStore = null)
        {
            AddressablesAssetStore = addressablesAssetStore ?? new AddressablesAssetStore();
#if ENABLE_METAADDRESSABLES
            MetaAddressablesAssetStore = new MetaAddressablesAssetStore();
#endif
        }

        public override void ApplyAsset(AddressableAssetSettings settings,
            string assetFilePath,
            AddressableAssetGroupTemplate group,
            string address,
            HashSet<string> labels)
        {
            base.ApplyAsset(settings, assetFilePath, group, address, labels);

#if ENABLE_METAADDRESSABLES
            MetaAddressablesAssetStore.AddAsset(assetFilePath);
            MetaAddressablesAssetStore.AddGlobalLabels(labels);
#endif

            AddressablesAssetStore.AddAsset(settings, assetFilePath);
        }

        public override void ApplyGlobal(AddressableAssetSettings settings,
            HashSet<string> labels)
        {
            AddressablesAssetStore.CreateGlobalLabels(settings);
        }
        #endregion
    }
}