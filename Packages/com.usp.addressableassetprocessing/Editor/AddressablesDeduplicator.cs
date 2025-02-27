using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using USP.AddressablesAssetProcessing;
using USP.AddressablesBuildGraph;
using USP.MetaAddressables;
using static UnityEditor.AddressableAssets.Build.Layout.BuildLayout.Bundle.BundleDependency;

public class AddressablesDeduplicator
{
    #region Static Methods
    public static void ProcessAssets(IGroupSelector<HashSet<MetaAddressables.GroupData>> groupSelector)
    {
        const int MaxIterations = 10;
        for (int i = 0; i < MaxIterations; ++i)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
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
                ProcessAsset(settings, groupSelector, duplicatedImplicitRoot);
            }
        }
    }

    private static bool IsValidDuplicateImplicitRoot(AssetInfo asset)
    {
        return asset.IsValid && asset.IsDuplicate && asset.IsImplicitRoot;
    }

    private static void ProcessAsset(AddressableAssetSettings settings,
        IGroupSelector<HashSet<MetaAddressables.GroupData>> groupSelector,
        AssetInfo duplicatedImplicitRoot)
    {
        if (duplicatedImplicitRoot == null ||
            (duplicatedImplicitRoot != null && !duplicatedImplicitRoot.IsValid))
        {
            return;
        }

        var groups = new HashSet<MetaAddressables.GroupData>(MetaAddressables.GroupData.Comparer.ByNameAndHash);
        PopulateGroups(duplicatedImplicitRoot.DependentAssets, groups);

        // Apply the asset file path to the group selector to set the appropriate group template.
        AddressableAssetGroupTemplate group = groupSelector.Select(groups);

        string address = MetaAddressables.AssetData.SimplifyAddress(duplicatedImplicitRoot.FilePath);

        var labels = new HashSet<string>{ "Shared Resources" };
        foreach (AssetInfo dependentAsset in duplicatedImplicitRoot.DependentAssets)
        {
            labels.UnionWith(dependentAsset.Labels);
        }

        MetaAddressablesProcessing.SetAddressableAsset(duplicatedImplicitRoot.FilePath, group, address, labels);

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        MetaAddressablesProcessing.SetGlobalLabels(settings, labels);
    }

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
}
