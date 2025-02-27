using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    public interface IGroupSelector<T> : IKeyExtractor<T, AddressableAssetGroupTemplate>
    {
    }
}
