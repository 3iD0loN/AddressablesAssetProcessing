using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    public class RuleSettings
    {
        #region Properties
        public IExtractor<string, AddressableAssetGroupTemplate> GroupExtractionMethod { get; set; }

        public IExtractor<string, string> AddressExtractionMethod { get; set; }

        public IExtractor<string, HashSet<string>> LabelExtractionMethod { get; set; }

        public IAssetApplicator AssetApplicationMethod { get; set; }
        #endregion
    }
}