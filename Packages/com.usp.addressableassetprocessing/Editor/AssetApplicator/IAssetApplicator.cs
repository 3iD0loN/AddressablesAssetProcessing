using System.Collections.Generic;

using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    public interface IAssetApplicator
    {
        public void Apply(AddressableAssetSettings settings,
            string assetFilePath,
            AddressableAssetGroupTemplate group,
            string address,
            HashSet<string> labels);
    }
}