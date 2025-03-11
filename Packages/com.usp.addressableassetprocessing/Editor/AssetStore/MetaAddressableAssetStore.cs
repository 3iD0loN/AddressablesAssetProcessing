using System.Collections.Generic;

using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
#if ENABLE_METAADDRESSABLES
    using USP.MetaAddressables;

    public class MetaAddressablesAssetStore : AssetStore
    {
        #region Methods
        public virtual void AddAsset(string assetFilePath)
        {
            bool found = DataByAssetPath.TryGetValue(assetFilePath, out MetaAddressables.UserData userData);

            if (found)
            {
                return;
            }

            userData = MetaAddressables.Read(assetFilePath);

            if (userData == null)
            {
                return;
            }

            DataByAssetPath.Add(assetFilePath, userData);
        }
        #endregion
    }
#endif
}