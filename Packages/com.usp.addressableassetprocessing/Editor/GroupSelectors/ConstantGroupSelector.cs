using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    using USP.MetaAddressables;

    public class ConstantGroupSelector : IGroupSelector<string>
    {
        #region Properties
        public AddressableAssetGroupTemplate GroupTemplate { get; set; }
        #endregion

        #region Methods
        public ConstantGroupSelector(AddressableAssetGroupTemplate groupTemplate)
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