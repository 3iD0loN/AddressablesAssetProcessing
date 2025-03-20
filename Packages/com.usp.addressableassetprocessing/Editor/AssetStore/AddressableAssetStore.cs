using System.Collections.Generic;

using UnityEditor;
using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    using USP.MetaAddressables;
    using static Codice.Client.Common.EventTracking.TrackFeatureUseEvent.Features.DesktopGUI.Filters;

    public class AddressablesAssetStore : AssetStore
    {
        #region Properties
        /// <summary>
        /// A value indicating whether the source of the assets can be written to or not.
        /// </summary>
        public override bool IsReadOnly => false;
        #endregion

        #region Methods
        public virtual void AddAsset(AddressableAssetSettings settings, string assetFilePath)
        {
            bool found = dataByAssetPath.TryGetValue(assetFilePath, out MetaAddressables.UserData userData);

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

            dataByAssetPath.Add(assetFilePath, userData);
        }

        public virtual void AddAsset(AddressableAssetEntry entry, bool overwrite = false)
        {
            bool found = dataByAssetPath.TryGetValue(entry.AssetPath, out MetaAddressables.UserData userData);

            if (found)
            {
                if (!overwrite)
                {
                    throw new System.Exception("Collision!");
                }
            }

            userData = new MetaAddressables.UserData(entry);

            dataByAssetPath.Add(entry.AssetPath, userData);
        }

        public void AddGlobalLabels(AddressableAssetSettings settings)
        {
            base.AddGlobalLabels(settings.GetLabels());
        }
        #endregion
    }
}