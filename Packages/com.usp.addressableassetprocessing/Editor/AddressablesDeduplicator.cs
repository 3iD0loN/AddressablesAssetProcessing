using System.Linq;

using UnityEngine;

using UnityEditor;
using UnityEditor.AddressableAssets.Build.BuildPipelineTasks;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.Build.Pipeline;

using USP.AddressablesBuildGraph;
using UnityEditor.VersionControl;
using UnityEditor.AddressableAssets;
using USP.MetaAddressables;
using USP.AddressablesAssetProcessing;


public class AddressablesDeduplicator
{
    #region Static Methods
    [MenuItem("Tools/Addressables Deduplication")]
    private static void Run()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var buildInfo = AddressablesBuildInfo.Create(settings);

        if (buildInfo == null)
        {
            return;
        }

        var duplicatedImplicitRoots = from asset in buildInfo.Assets
                                                 where asset.IsDuplicate && asset.IsImplicitRoot
                                                 select asset;

        foreach (AssetInfo duplicatedImplicitRoot in duplicatedImplicitRoots)
        {
            string assetFilePath = duplicatedImplicitRoot.FilePath;

            // Apply the asset file path to the group selector to set the appropriate group template.
            //groupSelector.Apply(assetFilePath);

            MetaAddressables.UserData userData = MetaAddressables.Read(assetFilePath);
        }
    }
    #endregion
}