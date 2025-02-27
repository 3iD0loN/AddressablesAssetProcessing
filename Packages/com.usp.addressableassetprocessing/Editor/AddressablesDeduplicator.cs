using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using USP.AddressablesAssetProcessing;
using USP.AddressablesBuildGraph;
using USP.MetaAddressables;

public class AddressablesDeduplicator
{
    #region Types
    private class GroupExtractor : IExtractor<AssetInfo, AddressableAssetGroupTemplate>
    {
        #region Static Methods
        private static void PopulateGroups(HashSet<AssetInfo> assetInfoSet, HashSet<MetaAddressables.GroupData> groupSet)
        {
            foreach (AssetInfo dependentAsset in assetInfoSet)
            {
                PopulateGroups(dependentAsset, groupSet);
            }
        }

        private static void PopulateGroups(AssetInfo assetInfo, HashSet<MetaAddressables.GroupData> groupSet)
        {
            foreach (AssetBundleInfo bundleInfo in assetInfo.Bundles)
            {
                var groupGuid = bundleInfo.Group.Guid;

                bool found = AddressablesGroupLookup.GroupsByGuids.TryGetValue(groupGuid, out AddressableAssetGroup group);

                if (!found)
                {
                    break;
                }

                var groupData = new MetaAddressables.GroupData(group);

                groupSet.Add(groupData);
            }
        }
        #endregion

        #region Fields
        private IExtractor<HashSet<MetaAddressables.GroupData>, AddressableAssetGroupTemplate> _groupExtractor;
        #endregion

        #region Methods
        public GroupExtractor(IExtractor<HashSet<MetaAddressables.GroupData>, AddressableAssetGroupTemplate> groupExtractor)
        {
            _groupExtractor = groupExtractor;
        }

        public void Extract(AssetInfo asset, ref AddressableAssetGroupTemplate result)
        {
            var groups = new HashSet<MetaAddressables.GroupData>(MetaAddressables.GroupData.Comparer.ByNameAndHash);
            PopulateGroups(asset.DependentAssets, groups);

            // Apply the asset file path to the group selector to set the appropriate group template.
            AddressableAssetGroupTemplate group = null;
            _groupExtractor.Extract(groups, ref group);
        }
        #endregion
    }

    private class KeyExtractor : IExtractor<AssetInfo, HashSet<string>>
    {
        public void Extract(AssetInfo asset, ref HashSet<string> result)
        {
            result.Add("Shared Resources");
            foreach (AssetInfo dependentAsset in asset.DependentAssets)
            {
                result.UnionWith(dependentAsset.Labels);
            }
        }
    }
    #endregion

    #region Static Methods
    public static void ProcessAssets(
        IExtractor<HashSet<MetaAddressables.GroupData>, AddressableAssetGroupTemplate> groupDataExtractor,
        IExtractor<string, string> addressExtractor)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;

        var groupExtractor = new GroupExtractor(groupDataExtractor);

        var labelExtractor = new KeyExtractor();

        const int MaxIterations = 10;
        for (int i = 0; i < MaxIterations; ++i)
        {
            var buildInfo = AddressablesBuildInfo.Create(settings);

            if (buildInfo == null)
            {
                return;
            }

            var duplicatedImplicitRoots = from asset in buildInfo.Assets
                                          where IsValidDuplicateImplicitRoot(asset)
                                          select asset;

            if (duplicatedImplicitRoots.Count() == 0)
            {
                break;
            }

            foreach (AssetInfo duplicatedImplicitRoot in duplicatedImplicitRoots)
            {
                if (duplicatedImplicitRoot == null ||
                    (duplicatedImplicitRoot != null && !duplicatedImplicitRoot.IsValid))
                {
                    return;
                }

                ProcessAsset(settings, duplicatedImplicitRoot, groupExtractor, addressExtractor, labelExtractor);
            }
        }
    }

    private static bool IsValidDuplicateImplicitRoot(AssetInfo asset)
    {
        return asset.IsValid && asset.IsDuplicate && asset.IsImplicitRoot;
    }

    private static void ProcessAsset(AddressableAssetSettings settings, 
        AssetInfo duplicatedImplicitRoot,
        IExtractor<AssetInfo, AddressableAssetGroupTemplate> groupExtractor,
        IExtractor<string, string> addressExtractor,
        IExtractor<AssetInfo, HashSet<string>> labelExtractor)
    {
        // Apply the asset file path to the group selector to set the appropriate group template.
        AddressableAssetGroupTemplate group = null;
        groupExtractor.Extract(duplicatedImplicitRoot, ref group);

        string address = null;
        addressExtractor.Extract(duplicatedImplicitRoot.FilePath, ref address);

        // Extract labels from the folder path.
        var labels = new HashSet<string>();
        labelExtractor.Extract(duplicatedImplicitRoot, ref labels);

        MetaAddressablesProcessing.SetAddressableAsset(duplicatedImplicitRoot.FilePath, group, address, labels);

        MetaAddressablesProcessing.SetGlobalLabels(settings, labels);
    }
    #endregion
}
