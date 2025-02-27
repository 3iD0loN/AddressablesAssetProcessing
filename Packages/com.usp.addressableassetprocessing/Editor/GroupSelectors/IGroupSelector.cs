using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    public interface IGroupSelector<T>
    {
        #region Methods
        AddressableAssetGroupTemplate Select(T key);
        #endregion
    }
}
