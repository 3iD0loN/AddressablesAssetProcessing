using System.Collections.Generic;

using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    public class AddressablesAssetApplicator : IAssetApplicator
    {
        #region Static Methods
        public static void SetGlobalLabels(AddressableAssetSettings settings, HashSet<string> labels)
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

        #region Methods
        public void Apply(AddressableAssetSettings settings,
            string assetFilePath,
            AddressableAssetGroupTemplate group,
            string address,
            HashSet<string> labels)
        {
            SetGlobalLabels(settings, labels);
        }
        #endregion
    }
}
