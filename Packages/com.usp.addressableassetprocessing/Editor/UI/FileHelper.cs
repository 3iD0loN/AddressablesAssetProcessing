using UnityEngine;

using UnityEditor;

using Object = UnityEngine.Object;

public static class FileHelper
{
    public static T Load<T>(string assetPath) where T : Object
    {
        const string WindowfilePath = "Packages\\com.usp.addressableassetprocessing\\Editor\\UI\\";

        return AssetDatabase.LoadAssetAtPath<T>(WindowfilePath + assetPath);
    }

    public static T LoadRequired<T>(string assetPath) where T : Object
    {
        T result = Load<T>(assetPath);

        if (!result)
        {
            Debug.LogError($"Unable to find required resource '{assetPath}'");
        }

        return result;
    }
}
