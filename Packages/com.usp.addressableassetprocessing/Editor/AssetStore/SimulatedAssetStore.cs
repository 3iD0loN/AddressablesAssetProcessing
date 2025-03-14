using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
#if ENABLE_METAADDRESSABLES
    using USP.MetaAddressables;

    public class SimulatedAssetStore : AssetStore
    {
        #region Properties
        /// <summary>
        /// A value indicating whether the source of the assets can be written to or not.
        /// </summary>
        public override bool IsReadOnly => true;
        #endregion

        #region Methods
        public void AddAsset(string assetFilePath,
            AddressableAssetGroupTemplate group,
            string address,
            HashSet<string> labels)
        {
            bool found = dataByAssetPath.TryGetValue(assetFilePath, out MetaAddressables.UserData userData);

            if (found)
            {
                return;
            }

            // Get the asset GUID associated with the asset file path.
            string guid = AssetDatabase.AssetPathToGUID(assetFilePath);

            var assetData = new MetaAddressables.AssetData(guid, address, labels, false);
            var groupData = new MetaAddressables.GroupData(group);
            userData = new MetaAddressables.UserData(assetData, groupData);

            dataByAssetPath.Add(assetFilePath, userData);
        }
        #endregion
    }
#endif
}