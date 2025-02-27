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
        public static string FormatTypes(params Type[] assetTypes)
        {
            var fileExtensions = GetFileExtensions(assetTypes);
            string combined = string.Join("|", fileExtensions);

            return $"\\.({combined})";
        }

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
        private string _internalMatchPattern;
        #endregion

        #region Properties
        private string AssetTypeMatchPattern { get; set; }
        #endregion

        #region Methods
        public TypedAssetPathCollector(params Type[] assetTypes) :
            this(".*{0}",
            FormatTypes(assetTypes))
        {
        }

        public TypedAssetPathCollector(
            string matchPattern = null,
            string assetTypeMatchPattern = null,
            string ignorePattern = null,
            SearchOption searchOptions = SearchOption.AllDirectories) :
            base(matchPattern, ignorePattern, searchOptions)
        {
            AssetTypeMatchPattern = assetTypeMatchPattern;
        }

        protected override string GetMatchPattern()
        {
            if (!_recacheMatch)
            {
                return _internalMatchPattern;
            }

            if (string.IsNullOrEmpty(AssetTypeMatchPattern))
            {
                _internalMatchPattern = MatchPattern;
            }
            else if (string.IsNullOrEmpty(MatchPattern))
            {
                _internalMatchPattern = AssetTypeMatchPattern;
            }
            else
            {
                _internalMatchPattern = string.Format(MatchPattern, AssetTypeMatchPattern);
            }

            _recacheMatch = false;

            return _internalMatchPattern;
        }
        #endregion
    }
}