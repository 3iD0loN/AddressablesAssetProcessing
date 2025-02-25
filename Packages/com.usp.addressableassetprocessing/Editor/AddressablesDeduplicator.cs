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
        if (duplicatedImplicitRoot == null || (duplicatedImplicitRoot != null && !duplicatedImplicitRoot.IsValid))
        {
            return;
        }

        var groups = new HashSet<MetaAddressables.GroupData>(MetaAddressables.GroupData.Comparer.ByNameAndHash);
        PopulateGroups(duplicatedImplicitRoot.DependentAssets, groups);

        // Apply the asset file path to the group selector to set the appropriate group template.
        groupSelector.Apply(groups);

        var labels = new HashSet<string>{ "Shared Resources" };
        foreach (AssetInfo dependentAsset in duplicatedImplicitRoot.DependentAssets)
        {
            labels.UnionWith(dependentAsset.Labels);
        }

        SetAddressableAsset(duplicatedImplicitRoot.FilePath, labels);

        SetLabels(settings, labels);
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

    private static void SetAddressableAsset(string assetFilePath, HashSet<string> labels)
    {
        // Get the user data associated with the asset file path.
        MetaAddressables.UserData userData = MetaAddressables.Read(assetFilePath);

        // If there was no valid user data associated with the asset file path, then:
        if (userData == null)
        {
            // Do nothing else.
            return;
        }

        // Union the labels that were extracted with the current ones associated with the asset.
        userData.Asset.Labels.UnionWith(labels);

        // Take the current address of the asset and simplify it.
        userData.Asset.Address = MetaAddressables.AssetData.SimplifyAddress(userData.Asset.Address);

        // Generate Addressables groups from the Meta file.
        // This is done before saving MetaAddressables to file in case we find groups that already match.
        MetaAddressables.Generate(userData);

        // Save to MetaAddressables changes.
        MetaAddressables.Write(assetFilePath, userData);
    }

    private static void SetLabels(AddressableAssetSettings settings, HashSet<string> labels)
    {
        // If there are valid settings, then:
        if (settings == null)
        {
            return;
        }

        // For every label extracted from the asset path, perform the following:
        foreach (var label in labels)
        {
            // Attempt to add the label to the global list.
            settings.AddLabel(label);
        }
    }
    #endregion
}
