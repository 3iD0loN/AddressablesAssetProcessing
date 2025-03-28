using System.Collections.Generic;

using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    using DocumentFormat.OpenXml.Presentation;
    using UnityEditor;

#if ENABLE_METAADDRESSABLES
    using USP.MetaAddressables;

    public class MetaAddressablesAssetApplicator : IAssetApplicator<MetaAddressablesAssetStore>
    {
        #region Properties
        public MetaAddressablesAssetStore AssetStore { get; }

        public AddressablesAssetStore AddressablesAssetStore { get; }

        IAssetStore IAssetApplicator.AssetStore => AssetStore;

        /// <summary>
        /// A value indicating whether the applying the asset should also generate an Addressable asset entry in addition to the metafile entry.
        /// </summary>
        public bool GenerateAddressables => AddressablesAssetStore != null;

        /// <summary>
        /// A value indicating whether the asset store can should tolerate collisions.
        /// </summary>
        public bool TolerateAssetOverwrite { get; }

        public bool UseGroupOverwrite { get; }
        #endregion

        #region Methods
        public MetaAddressablesAssetApplicator(MetaAddressablesAssetStore assetStore = null,
            AddressablesAssetStore addressablesAssetStore = null, bool tolerateAssetOverwrite = false, bool forceGroupOverwrite = false)
        {
            AssetStore = assetStore ?? new MetaAddressablesAssetStore();
            AddressablesAssetStore = addressablesAssetStore;
            TolerateAssetOverwrite = tolerateAssetOverwrite;
            UseGroupOverwrite = forceGroupOverwrite;
        }

        public void ApplyAsset(AddressableAssetSettings settings,
            string assetFilePath,
            AddressableAssetGroupTemplate groupTemplate,
            string address,
            HashSet<string> labels)
        {
            if (MetaAddressables.Factory is MetaAddressables.CreationFactory factory)
            {
                // MetaAddressables data creation will default to using this group if there is no metadata associated. 
                factory.Settings = settings;
                factory.ActiveGroupTemplate = groupTemplate;
            }

            // Get the user data associated with the asset file path.
            // If there is no entry, then create one using the factory, but don't commit it to file yet.
            MetaAddressables.UserData userData = MetaAddressables.Read(assetFilePath, MetaAddressables.Factory);

            // If there was no valid user data associated with the asset file path, then:
            if (userData == null)
            {
                // Return invalid data. Do nothing else.
                return;
            }

            if (UseGroupOverwrite)
            {
                string groupName = GetGroupName(userData);
                if (!StringComparer.Ordinal.Equals(groupName, groupTemplate.Name))
                {
                    userData.Group = new MetaAddressables.GroupData(groupTemplate);
                    UnityEngine.Debug.LogWarning($"The group template is {groupTemplate.Name} but the user data read is {groupName} for asset {assetFilePath}");
                }
            }

            // Replace the current set of labels and add them to the set. 
            userData.Asset.Labels.Clear();
            userData.Asset.Labels.UnionWith(labels);

            // Take the current address of the asset and simplify it.
            userData.Asset.Address = address;

            if (GenerateAddressables)
            {
                // Generate Addressables groups from the Meta file.
                // This is done before saving MetaAddressables to file in case we find groups that already match,
                // which will correct the Group member to the better match.
                MetaAddressables.Generate(ref userData, settings);

                AddressablesAssetStore.AddAsset(userData, assetFilePath, TolerateAssetOverwrite);
                AddressablesAssetStore.AddGlobalLabels(userData.Asset.Labels);
            }

            // Save to MetaAddressables changes.
            MetaAddressables.Write(assetFilePath, userData);

            AssetStore.AddAsset(userData, assetFilePath, TolerateAssetOverwrite);
            AssetStore.AddGlobalLabels(userData.Asset.Labels);
        }

        public void ApplyAsset(AddressableAssetSettings settings, MetaAddressables.UserData userData)
        {
            // Find the asset file path associated with the asset guid.
            var assetFilePath = AssetDatabase.GUIDToAssetPath(userData.Asset.Guid);

            if (GenerateAddressables)
            {
                // Generate Addressables groups from the Meta file.
                // This is done before saving MetaAddressables to file in case we find groups that already match,
                // which will correct the Group member to the better match.
                MetaAddressables.Generate(ref userData, settings);

                AddressablesAssetStore.AddAsset(userData, assetFilePath, TolerateAssetOverwrite);
                AddressablesAssetStore.AddGlobalLabels(userData.Asset.Labels);
            }

            // Save to MetaAddressables changes.
            MetaAddressables.Write(assetFilePath, userData);

            AssetStore.AddAsset(userData, assetFilePath, TolerateAssetOverwrite);
            AssetStore.AddGlobalLabels(userData.Asset.Labels);
        }

        private static string GetGroupName(MetaAddressables.UserData userData)
        {
            string name = userData.Group.Name;
            if (string.IsNullOrEmpty(name))
            {
                bool found = AddressablesLookup.GroupsAndHashesByGuids.TryGetValue(userData.Group.Guid, out AddressableAssetGroup g);
                if (found)
                {
                    return g.Name;
                }

                UnityEngine.Debug.LogError("things are bad like you wouldn't believe, man...");
            }

            return null;
        }
        #endregion
    }
#endif
}