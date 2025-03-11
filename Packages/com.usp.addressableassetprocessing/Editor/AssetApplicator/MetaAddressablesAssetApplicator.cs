using System.Collections.Generic;

using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
#if ENABLE_METAADDRESSABLES
    using USP.MetaAddressables;

    public class MetaAddressablesAssetApplicator : IAssetApplicator
    {
        #region Static Methods
        public static void SetAddressableAsset(AddressableAssetSettings settings, string assetFilePath, 
            AddressableAssetGroupTemplate group, string address, HashSet<string> labels)
        {
            if (MetaAddressables.Factory is MetaAddressables.CreationFactory factory)
            {
                // MetaAddressables data creation will default to using this group if there is no metadata associated. 
                factory.ActiveGroupTemplate = group;
            }

            // Get the user data associated with the asset file path.
            // If there is no entry, then create one using the factory, but don't commit it to file yet.
            MetaAddressables.UserData userData = MetaAddressables.Read(assetFilePath, MetaAddressables.Factory);

            // If there was no valid user data associated with the asset file path, then:
            if (userData == null)
            {
                // Do nothing else.
                return;
            }

            // Union the labels that were extracted with the current ones associated with the asset.
            userData.Asset.Labels.UnionWith(labels);

            // Take the current address of the asset and simplify it.
            userData.Asset.Address = address;

            // Generate Addressables groups from the Meta file.
            // This is done before saving MetaAddressables to file in case we find groups that already match.
            MetaAddressables.Generate(ref userData, settings);

            // Save to MetaAddressables changes.
            MetaAddressables.Write(assetFilePath, userData);
        }
        #endregion

        #region Methods
        public void ApplyAsset(AddressableAssetSettings settings,
            string assetFilePath,
            AddressableAssetGroupTemplate group,
            string address,
            HashSet<string> labels)
        {
            SetAddressableAsset(settings, assetFilePath, group, address, labels);

            AddressablesAssetApplicator.SetGlobalLabels(settings, labels);
        }

        public void ApplyGlobal(AddressableAssetSettings settings,
            HashSet<string> labels)
        {}
        #endregion
    }
#endif
}