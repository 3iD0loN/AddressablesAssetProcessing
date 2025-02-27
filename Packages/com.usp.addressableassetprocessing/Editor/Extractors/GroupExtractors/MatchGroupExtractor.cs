using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    using USP.MetaAddressables;

    public class MatchGroupExtractor : MappedGroupExtractor<string, string>
    {
        #region Properties
        public string MatchPattern { get; set; }

        public Func<Match, string, string> MatchToKey { get; set; }
        #endregion

        #region Methods
        protected override string GetInternalKey(string assetFilePath)
        {
            // If there is no valid match pattern or valid selector, then:
            if (string.IsNullOrEmpty(MatchPattern) || MatchToKey == null)
            {
                // Return an invalid group template. Do nothing else.
                return default;
            }

            // Otherwise, both the match pattern and selector are valid.

            // Match the patterns against the asset file name.
            Match match = Regex.Match(assetFilePath, MatchPattern);

            // Select the key associated with match.
            return MatchToKey(match, assetFilePath);
        }
        #endregion
    }
}