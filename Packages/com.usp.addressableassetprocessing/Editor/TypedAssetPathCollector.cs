using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class TypedAssetPathCollector : AssetPathCollector
{
    #region Static Methods
    protected static string[] GetFileExtensions(IEnumerable<Type> types)
    {
        var result = new string[types.Count()];

        int i = 0;
        foreach (var type in types)
        {
            string extension = GetFileExtension(type);

            result[i] = extension;

            ++i;
        }

        return result;
    }

    protected static string GetFileExtension(Type type)
    {
        if (type == typeof(Texture))
        {
            return "png$|psd$";
        }
        else if (type == typeof(Animation))
        {
            return "anim$";
        }
        else if (type == typeof(Mesh))
        {
            return "fbx$";
        }
        else if (type == typeof(Material))
        {
            return "mat$";
        }
        else if (type == typeof(GameObject))
        {
            return "prefab$";
        }

        return string.Empty;
    }
    #endregion

    #region Fields
    private string _assetTypeMatchPattern;

    private string _internalMatchPattern;
    #endregion

    #region Methods
    public TypedAssetPathCollector(params Type[] assetTypes)
    {
        SearchOptions = SearchOption.AllDirectories;

        var fileExtensions = GetFileExtensions(assetTypes);
        string combined = string.Join("|", fileExtensions);

        _assetTypeMatchPattern = $"\\.({combined})";
        MatchPattern = ".*{0}";
        _recacheMatch = true;
    }

    protected override string GetMatchPattern()
    {
        if (_recacheMatch)
        {
            if (string.IsNullOrEmpty(_assetTypeMatchPattern))
            {
                _internalMatchPattern = MatchPattern;
            }
            else if (string.IsNullOrEmpty(MatchPattern))
            {
                _internalMatchPattern = _assetTypeMatchPattern;
            }
            else
            {
                _internalMatchPattern = string.Format(MatchPattern, _assetTypeMatchPattern);
            }

            _recacheMatch = false;
        }

        return _internalMatchPattern;
    }
    #endregion
}