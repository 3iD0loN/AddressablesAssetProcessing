using UnityEditor;
using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    using USP.MetaAddressables;

    public class AddressablesAssetStore : AssetStore
    {
        #region Properties
        /// <summary>
        /// A value indicating whether the source of the assets can be written to or not.
        /// </summary>
        public override bool IsReadOnly => false;

        public AddressableAssetSettings Settings { get; }
        #endregion

        #region 
        public AddressablesAssetStore(AddressableAssetSettings settings)
        {
            Settings = settings;

            AddGlobalLabels();

            foreach (AddressableAssetGroup group in settings.groups)
            {
                foreach (AddressableAssetEntry entry in group.entries)
                {
                    AddAsset(entry);
                }
            }
        }
        public virtual void AddAsset(string assetFilePath)
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
            userData = MetaAddressables.UserData.Create(Settings, guid);

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
                    string message = $"Collision. Attempting to add asset: {entry.AssetPath} to asset store: {this}, but an entry already exists. Pass in overwrite parameter if this was intended.";

                    throw new System.Exception(message);
                }
            }

            userData = new MetaAddressables.UserData(entry);

            dataByAssetPath[entry.AssetPath] = userData;
        }

        public void AddGlobalLabels()
        {
            base.AddGlobalLabels(Settings.GetLabels());
        }
        #endregion
    }
}