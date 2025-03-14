using System.Collections.Generic;

using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    public interface IAssetApplicator
    {
        void ApplyAsset(AddressableAssetSettings settings,
            string assetFilePath,
            AddressableAssetGroupTemplate group,
            string address,
            HashSet<string> labels);
    }
}