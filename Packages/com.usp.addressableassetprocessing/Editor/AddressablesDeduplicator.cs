using System.Linq;

using UnityEngine;

using UnityEditor;
using UnityEditor.AddressableAssets.Build.BuildPipelineTasks;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.Build.Pipeline;

using USP.AddressablesBuildGraph;


public class AddressablesDeduplicator
{
    #region Static Methods
    [MenuItem("Tools/Addressables Deduplication")]
    private static void Run()
    {
        ReturnCode exitCode = AddressableBuildSpoof.GetExtractData(
            out AddressableAssetsBuildContext aaBuildContext,
            out ExtractDataTask extractDataTask);

        if (exitCode < ReturnCode.Success)
        {
            return;
        }

        var buildInfo = AddressablesBuildInfo.Create(aaBuildContext, extractDataTask.WriteData);

        var duplicatedImplicitInflectionAssets = from asset in buildInfo.Assets
                where asset.IsDuplicate && !asset.IsAddressable && asset.AssetDependents.Any(x => x.IsAddressable)
                select asset;

        var duplicatedImplicitInflectionAssetArray = duplicatedImplicitInflectionAssets.ToArray();

        Debug.Log("x");
    }
    #endregion
}