using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    using USP.MetaAddressables;

    public class MatchGroupSelector : IGroupSelector<string>
    {
        #region Properties
        public string MatchPattern { get; set; }

        public Dictionary<string, AddressableAssetGroupTemplate> GroupTemplateByKey
        {
            get;
            set;
        }

        public Func<Match, string, string> Select { get; set; }
        #endregion

        #region Methods
        public MatchGroupSelector()
        {
            GroupTemplateByKey = new Dictionary<string, AddressableAssetGroupTemplate>();
        }

        public void Apply(string assetFilePath)
        {
            MetaAddressables.factory.ActiveGroupTemplate = Process(assetFilePath);
        }

        private AddressableAssetGroupTemplate Process(string assetFilePath)
        {
            // If there is no valid match pattern or valid selector, then:
            if (string.IsNullOrEmpty(MatchPattern) || Select == null)
            {
                // Return an invalid group template. Do nothing else.
                return default;
            }

            // Otherwise, there is a valid match pattern and selector.

            // Match the patterns against the asset file name.
            Match match = Regex.Match(assetFilePath, MatchPattern);

            // Select the key associated with match.
            string key = Select(match, assetFilePath);

            // Get template associated with the key.
            return Get(key);
        }

        public AddressableAssetGroupTemplate Get(string key)
        {
            bool found = GroupTemplateByKey.TryGetValue(key,
                    out AddressableAssetGroupTemplate result);

            if (found)
            {
                return result;
            }

            return default;
        }

        public void Set(string key, AddressableAssetGroupTemplate value)
        {
            GroupTemplateByKey.Add(key, value);
        }
        #endregion
    }
}