using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    public interface IGroupExtractor<T> : IExtractor<T, AddressableAssetGroupTemplate>
    {
    }
}
