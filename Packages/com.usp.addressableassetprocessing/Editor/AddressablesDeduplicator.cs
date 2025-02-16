using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using USP.AddressablesAssetProcessing;
using USP.AddressablesBuildGraph;
using USP.MetaAddressables;

//*/
public class AddressablesDeduplicator
{
    #region Static Methods
    [MenuItem("Tools/Addressables Deduplication")]
    private static void Run()
    {
        var x = new Dictionary<int, List<AddressableAssetGroupTemplate>>();

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        foreach (AddressableAssetGroupTemplate groupTemplate in settings.GroupTemplateObjects)
        {
            if (groupTemplate == null)
            {
                return;
            }

            var y = new MetaAddressables.GroupData(groupTemplate);
            bool found = x.TryGetValue(y.GetHashCode(),
                out List<AddressableAssetGroupTemplate> groupTemplateList);

            if (!found)
            {
                groupTemplateList = new List<AddressableAssetGroupTemplate>();
                x.Add(y.GetHashCode(), groupTemplateList);
            }

            groupTemplateList.Add(groupTemplate);
        }
    }

    public static void ProcessAssets(IGroupSelector<HashSet<MetaAddressables.GroupData>> groupSelector)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var buildInfo = AddressablesBuildInfo.Create(settings);

        if (buildInfo == null)
        {
            return;
        }

        var duplicatedImplicitRoots = from asset in buildInfo.Assets
                                      where asset.IsValid && asset.IsDuplicate && asset.IsImplicitRoot
                                      select asset;

        foreach (AssetInfo duplicatedImplicitRoot in duplicatedImplicitRoots)
        {
            ProcessAsset(settings, groupSelector, duplicatedImplicitRoot);
        }
    }

    private static void ProcessAsset(AddressableAssetSettings settings,
        IGroupSelector<HashSet<MetaAddressables.GroupData>> groupSelector,
        AssetInfo duplicatedImplicitRoot)
    {
        string assetFilePath = duplicatedImplicitRoot.FilePath;

        var groups = new HashSet<MetaAddressables.GroupData>(MetaAddressables.GroupData.Comparer.ByNameAndHash);
        PopulateGroups(duplicatedImplicitRoot.DependentAssets, groups);

        // Apply the asset file path to the group selector to set the appropriate group template.
        groupSelector.Apply(groups);

        //MetaAddressables.UserData userData = MetaAddressables.Read(assetFilePath);
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
