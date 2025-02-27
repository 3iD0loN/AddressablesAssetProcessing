using UnityEngine;

public static class MetaAddressablesProcessing
{
    public static void SetAddressableAsset(string assetFilePath, AddressableAssetGroupTemplate group, Func<string, string> setAddress, HashSet<string> labels)
    {
        // MetaAddressables data cration will default to using this group if there is no metadata associated. 
        MetaAddressables.factory.ActiveGroupTemplate = group;

        // Get the user data associated with the asset file path.
        MetaAddressables.UserData userData = MetaAddressables.Read(assetFilePath);

        // If there was no valid user data associated with the asset file path, then:
        if (userData == null)
        {
            // Do nothing else.
            return;
        }

        // Union the labels that were extracted with the current ones associated with the asset.
        userData.Asset.Labels.UnionWith(labels);

        // Take the current address of the asset and simplify it.
        userData.Asset.Address = setAddress?.Invoke(userData.Asset.Address);

        // Generate Addressables groups from the Meta file.
        // This is done before saving MetaAddressables to file in case we find groups that already match.
        MetaAddressables.Generate(userData);

        // Save to MetaAddressables changes.
        MetaAddressables.Write(assetFilePath, userData);
    }

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
}
