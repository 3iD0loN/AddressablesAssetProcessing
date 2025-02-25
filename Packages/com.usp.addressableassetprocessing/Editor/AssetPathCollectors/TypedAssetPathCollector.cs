using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor;

namespace USP.AddressablesAssetProcessing
{
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
            if (type == typeof(Texture2D))
            {
                return "jpg$|png$|psd$|tga$|bmp$|tif$";
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
            else if (type == typeof(SceneAsset))
            {
                return "unity$";
            }
            else if (type == typeof(VisualTreeAsset))
            {
                return "uxml$";
            }
            else if (type == typeof(StyleSheet))
            {
                return "uss$";
            }
            else if (type == typeof(AudioClip))
            {
                return "aif$|wav$|mp3$|ogg$";
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
}