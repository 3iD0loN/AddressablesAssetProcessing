using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    using USP.MetaAddressables;

    public class ConstantGroupExtractor : IGroupExtractor<string>
    {
        #region Properties
        public AddressableAssetGroupTemplate GroupTemplate { get; set; }
        #endregion

        #region Methods
        public ConstantGroupExtractor(AddressableAssetGroupTemplate groupTemplate)
        {
            GroupTemplate = groupTemplate;
        }

        public void Extract(string assetFileName, ref AddressableAssetGroupTemplate value)
        {
            value = GroupTemplate;
        }
        #endregion
    }
}