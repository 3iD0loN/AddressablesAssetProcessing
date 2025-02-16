using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesAssetProcessing
{
    using USP.MetaAddressables;

    public class MatchGroupSelector : MappedGroupSelector<string, string>
    {
        #region Properties
        public string MatchPattern { get; set; }

        public Func<Match, string, string> Transform { get; set; }
        #endregion

        #region Methods
        protected override string Select(string assetFilePath)
        {
            // If there is no valid match pattern or valid selector, then:
            if (string.IsNullOrEmpty(MatchPattern) || Transform == null)
            {
                // Return an invalid group template. Do nothing else.
                return default;
            }

            // Otherwise, there is a valid match pattern and selector.

            // Match the patterns against the asset file name.
            Match match = Regex.Match(assetFilePath, MatchPattern);

            // Select the key associated with match.
            return Transform(match, assetFilePath);
        }
        #endregion
    }
}