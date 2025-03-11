using System.Collections.Generic;

using UnityEditor;
using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    using USP.MetaAddressables;

    public class AddressablesAssetStore : AssetStore
    {
        #region Methods
        public virtual void AddAsset(AddressableAssetSettings settings, string assetFilePath)
        {
            bool found = DataByAssetPath.TryGetValue(assetFilePath, out MetaAddressables.UserData userData);

            if (found)
            {
                return;
            }

            // Get the asset GUID associated with the asset file path.
            string guid = AssetDatabase.AssetPathToGUID(assetFilePath);

            // Attempt to find an Addressable asset entry that is associated with the asset GUID.
            // If there is, then the asset is already Addressable.
            userData = MetaAddressables.UserData.Create(settings, guid);

            // If the asset is already Addressable, then: 
            if (userData == null)
            {
                return;
            }

            DataByAssetPath.Add(assetFilePath, userData);
        }

        public virtual void AddAsset(AddressableAssetEntry entry)
        {
            bool found = DataByAssetPath.TryGetValue(entry.AssetPath, out MetaAddressables.UserData userData);

            if (found)
            {
                return;
            }

            userData = new MetaAddressables.UserData(entry);

            DataByAssetPath.Add(entry.AssetPath, userData);
        }

        public void CreateGlobalLabels(AddressableAssetSettings settings)
        {
            AddGlobalLabels(settings.GetLabels());
        }
        #endregion
    }
}