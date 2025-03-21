using System.Collections.Generic;

using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    using USP.MetaAddressables;

    public interface IAssetApplicator
    {
        #region Properties
        IAssetStore AssetStore { get; }
        #endregion

        #region Methods
        void ApplyAsset(AddressableAssetSettings settings,
            string assetFilePath,
            AddressableAssetGroupTemplate group,
            string address,
            HashSet<string> labels);

        void ApplyAsset(AddressableAssetSettings settings, MetaAddressables.UserData userData);
        #endregion
    }

    public interface IAssetApplicator<T> : IAssetApplicator
        where T : IAssetStore
    {
        #region Properties
        new T AssetStore { get; }
        #endregion
    }
}