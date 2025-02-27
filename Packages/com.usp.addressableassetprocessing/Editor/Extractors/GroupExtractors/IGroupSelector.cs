using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    public interface IGroupSelector<T> : IExtractor<T, AddressableAssetGroupTemplate>
    {
    }
}
