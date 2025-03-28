using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using USP.AddressablesBuildGraph;

namespace USP.AddressablesAssetProcessing
{
    using USP.MetaAddressables;

    public class AddressablesDeduplicator
    {
        public const string DeduplicatedKey = "Shared Resources";

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

                    bool found = AddressablesLookup.GroupsAndHashesByGuids.TryGetValue(groupGuid, out (int, AddressableAssetGroup Group) value);

                    if (!found)
                    {
                        break;
                    }

                    var groupData = new MetaAddressables.GroupData(value.Group);

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
                _groupExtractor.Extract(groups, ref result);
            }
            #endregion
        }

        private class AddressExtractor : IExtractor<AssetInfo, string>
        {
            #region Fields
            private IExtractor<string, string> _groupExtractor;
            #endregion

            #region Methods
            public AddressExtractor(IExtractor<string, string> groupExtractor)
            {
                _groupExtractor = groupExtractor;
            }

            public void Extract(AssetInfo asset, ref string result)
            {
                _groupExtractor.Extract(asset.FilePath, ref result);
            }
            #endregion
        }

        private class KeyExtractor : IExtractor<AssetInfo, HashSet<string>>
        {
            #region Methods
            public void Extract(AssetInfo asset, ref HashSet<string> result)
            {
                result.Add(DeduplicatedKey);
                foreach (AssetInfo dependentAsset in asset.DependentAssets)
                {
                    result.UnionWith(dependentAsset.Labels);
                }
            }
            #endregion
        }
        #endregion

        #region Static Methods
        public static void ProcessAssets(
            IExtractor<HashSet<MetaAddressables.GroupData>, AddressableAssetGroupTemplate> groupDataExtractor,
            IExtractor<string, string> addressFilepathExtractor,
            IAssetApplicator assetApplicator)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;

            var groupExtractor = new GroupExtractor(groupDataExtractor);
            var addressExtractor = new AddressExtractor(addressFilepathExtractor);
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

                    ProcessAsset(settings, duplicatedImplicitRoot, groupExtractor, addressExtractor, labelExtractor, assetApplicator);
                }
            }
        }

        private static bool IsValidDuplicateImplicitRoot(AssetInfo asset)
        {
            return asset.IsValid && asset.IsDuplicate && asset.IsImplicitRoot;
        }

        private static void ProcessAsset(AddressableAssetSettings settings,
            AssetInfo asset,
            IExtractor<AssetInfo, AddressableAssetGroupTemplate> groupExtractor,
            IExtractor<AssetInfo, string> addressExtractor,
            IExtractor<AssetInfo, HashSet<string>> labelExtractor,
            IAssetApplicator assetApplicator)
        {
            // Extract the group template from the asset.
            AddressableAssetGroupTemplate group = null;
            groupExtractor.Extract(asset, ref group);

            string address = null;
            addressExtractor.Extract(asset, ref address);

            // Extract labels from the asset.
            var labels = new HashSet<string>();
            labelExtractor.Extract(asset, ref labels);

            assetApplicator.ApplyAsset(settings, asset.FilePath, group, address, labels);
        }
        #endregion
    }
}